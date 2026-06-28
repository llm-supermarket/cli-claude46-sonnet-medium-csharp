namespace CliTool.Crypto;

using Sodium;

internal sealed class RcloneCipher
{
    private static readonly byte[] FileHeaderMagic = [0x52, 0x43, 0x4C, 0x4F, 0x4E, 0x45, 0x00, 0x00]; // "RCLONE\0\0"
    private const int NonceSize = 24;
    private const int BlockSize = 65536;
    private const int MacSize = 16;

    private readonly DerivedKeys _keys;

    internal RcloneCipher(string password, string? customSalt = null)
    {
        _keys = ScryptKeyDerivation.Derive(password, customSalt);
    }

    internal string EncryptFilename(string filename, FilenameEncoding encoding)
    {
        var encrypted = EmeAes.EncryptFilename(_keys.NameKey, _keys.NameTweak, filename);
        return encoding switch
        {
            FilenameEncoding.Base32 => Base32Encode(encrypted),
            FilenameEncoding.Base64Url => Base64UrlEncode(encrypted),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };
    }

    internal string DecryptFilename(string encryptedFilename, FilenameEncoding encoding)
    {
        var encrypted = encoding switch
        {
            FilenameEncoding.Base32 => Base32Decode(encryptedFilename),
            FilenameEncoding.Base64Url => Base64UrlDecode(encryptedFilename),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };
        return EmeAes.DecryptFilename(_keys.NameKey, _keys.NameTweak, encrypted);
    }

    internal async Task EncryptFileAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        var fileNonce = SecretBox.GenerateNonce();

        await output.WriteAsync(FileHeaderMagic, cancellationToken);
        await output.WriteAsync(fileNonce, cancellationToken);

        var buffer = new byte[BlockSize];
        ulong blockCounter = 0;
        int bytesRead;

        while ((bytesRead = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            var plaintext = buffer[..bytesRead];
            var blockNonce = BuildBlockNonce(fileNonce, blockCounter);
            var ciphertext = SecretBox.Create(plaintext, blockNonce, _keys.DataKey);
            await output.WriteAsync(ciphertext, cancellationToken);
            blockCounter++;
        }
    }

    internal async Task DecryptFileAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        var header = new byte[FileHeaderMagic.Length];
        await ReadExactAsync(input, header, cancellationToken);

        if (!header.AsSpan().SequenceEqual(FileHeaderMagic))
            throw new InvalidDataException("File does not have a valid rclone encryption header.");

        var fileNonce = new byte[NonceSize];
        await ReadExactAsync(input, fileNonce, cancellationToken);

        var encryptedBlockSize = BlockSize + MacSize;
        var buffer = new byte[encryptedBlockSize];
        ulong blockCounter = 0;

        while (true)
        {
            var bytesRead = await ReadUpToAsync(input, buffer, cancellationToken);
            if (bytesRead == 0) break;

            var encryptedBlock = buffer[..bytesRead];
            var blockNonce = BuildBlockNonce(fileNonce, blockCounter);
            var plaintext = SecretBox.Open(encryptedBlock, blockNonce, _keys.DataKey);
            await output.WriteAsync(plaintext, cancellationToken);
            blockCounter++;
        }
    }

    private static byte[] BuildBlockNonce(byte[] fileNonce, ulong blockIndex)
    {
        var nonce = (byte[])fileNonce.Clone();
        // Add blockIndex as LE uint64 to the nonce (treating it as a 24-byte LE integer)
        ushort carry = 0;
        for (var i = 0; i < 8; i++)
        {
            carry += (ushort)((ushort)nonce[i] + (byte)(blockIndex & 0xFF));
            blockIndex >>= 8;
            nonce[i] = (byte)carry;
            carry >>= 8;
        }
        // Propagate carry into bytes 8-23
        for (var i = 8; carry != 0 && i < NonceSize; i++)
        {
            carry += nonce[i];
            nonce[i] = (byte)carry;
            carry >>= 8;
        }
        return nonce;
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
            if (read == 0)
                throw new EndOfStreamException($"Expected {buffer.Length} bytes but got {totalRead}.");
            totalRead += read;
        }
    }

    private static async Task<int> ReadUpToAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
            if (read == 0) break;
            totalRead += read;
        }
        return totalRead;
    }

    // rclone uses base32 HEX encoding (RFC 4648): alphabet 0-9A-V (lowercase: 0-9a-v)
    private static string Base32Encode(byte[] data)
    {
        const string Alphabet = "0123456789abcdefghijklmnopqrstuv";
        var sb = new System.Text.StringBuilder();
        var bits = 0;
        var accumulator = 0;

        foreach (var b in data)
        {
            accumulator = (accumulator << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                sb.Append(Alphabet[(accumulator >> bits) & 0x1F]);
            }
        }

        if (bits > 0)
            sb.Append(Alphabet[(accumulator << (5 - bits)) & 0x1F]);

        return sb.ToString();
    }

    private static byte[] Base32Decode(string encoded)
    {
        var lower = encoded.ToLowerInvariant();
        var result = new System.Collections.Generic.List<byte>();
        var bits = 0;
        var accumulator = 0;

        foreach (var c in lower)
        {
            int value;
            if (c is >= '0' and <= '9')
                value = c - '0';
            else if (c is >= 'a' and <= 'v')
                value = c - 'a' + 10;
            else
                continue;

            accumulator = (accumulator << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                result.Add((byte)((accumulator >> bits) & 0xFF));
            }
        }

        return [.. result];
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string encoded)
    {
        var s = encoded.Replace('-', '+').Replace('_', '/');
        var padding = (4 - s.Length % 4) % 4;
        s += new string('=', padding);
        return Convert.FromBase64String(s);
    }
}

internal enum FilenameEncoding
{
    Base32,
    Base64Url
}
