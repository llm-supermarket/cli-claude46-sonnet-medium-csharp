namespace CliTool.Crypto;

using System.Security.Cryptography;

// EME (ECB-Mix-ECB) mode for AES, matching github.com/rfjakob/eme used by rclone.
// Reference: https://github.com/rfjakob/eme/blob/master/eme.go
internal static class EmeAes
{
    internal static byte[] EncryptFilename(byte[] key, byte[] tweak, string filename)
    {
        var plaintext = System.Text.Encoding.UTF8.GetBytes(filename);
        var padded = Pkcs7Pad(plaintext);
        return Transform(key, tweak, padded, encrypt: true);
    }

    internal static string DecryptFilename(byte[] key, byte[] tweak, byte[] ciphertext)
    {
        var padded = Transform(key, tweak, ciphertext, encrypt: false);
        var plaintext = Pkcs7Unpad(padded);
        return System.Text.Encoding.UTF8.GetString(plaintext);
    }

    // Transform implements EME-encrypt or EME-decrypt per the rfjakob/eme algorithm.
    // L[j] = 2^(j+1) * AES(K, 0); tweak is XOR'd into MP, not per-block.
    private static byte[] Transform(byte[] key, byte[] tweak, byte[] data, bool encrypt)
    {
        var m = data.Length / 16;
        if (m == 0 || data.Length % 16 != 0)
            throw new ArgumentException("Data length must be a positive multiple of 16 bytes.");

        // LTable[j] = 2^(j+1) * AES(K, 0)
        var lTable = TabulateL(key, m);

        // Phase 1: C[j] = AES(K, P[j] XOR L[j])
        var c = new byte[data.Length];
        var ppj = new byte[16];
        for (var j = 0; j < m; j++)
        {
            XorBlocks(ppj, data.AsSpan(j * 16, 16), lTable[j]);
            AesTransform(c.AsSpan(j * 16, 16), ppj, encrypt, key);
        }

        // Phase 2: MP = (XOR of all C[j]) XOR tweak
        var mp = new byte[16];
        XorBlocks(mp, c.AsSpan(0, 16), tweak);
        for (var j = 1; j < m; j++)
            XorBlocksInPlace(mp, c.AsSpan(j * 16, 16));

        // Phase 3: MC = AES(K, MP)
        var mc = new byte[16];
        AesTransform(mc, mp, encrypt, key);

        // Phase 4: M = MP XOR MC
        var mBuf = new byte[16];
        XorBlocks(mBuf, mp, mc);

        // Phase 5: CCC[j] = C[j] XOR 2^j * M  (for j=1..m-1)
        for (var j = 1; j < m; j++)
        {
            mBuf = MultByTwo(mBuf);
            XorBlocksInPlace(c.AsSpan(j * 16, 16), mBuf);
        }

        // Phase 6: CCC[0] = (XOR of CCC[1..m-1]) XOR tweak XOR MC
        var ccc0 = new byte[16];
        XorBlocks(ccc0, mc, tweak);
        for (var j = 1; j < m; j++)
            XorBlocksInPlace(ccc0, c.AsSpan(j * 16, 16));
        ccc0.CopyTo(c, 0);

        // Phase 7: out[j] = AES(K, CCC[j]) XOR L[j]
        var result = new byte[data.Length];
        for (var j = 0; j < m; j++)
        {
            AesTransform(result.AsSpan(j * 16, 16), c.AsSpan(j * 16, 16), encrypt, key);
            XorBlocksInPlace(result.AsSpan(j * 16, 16), lTable[j]);
        }

        return result;
    }

    // LTable[j] = 2^(j+1) * AES(K, 0) in GF(2^128) little-endian
    private static byte[][] TabulateL(byte[] key, int m)
    {
        var zero = new byte[16];
        var li = new byte[16];
        AesEcbEncrypt(li, zero, key);

        var table = new byte[m][];
        for (var i = 0; i < m; i++)
        {
            li = MultByTwo(li);
            table[i] = (byte[])li.Clone();
        }
        return table;
    }

    // GF(2^128) multiply by 2 with little-endian byte ordering (in[0] = LSB, in[15] = MSB).
    // Matches rfjakob/eme multByTwo exactly.
    private static byte[] MultByTwo(ReadOnlySpan<byte> src)
    {
        var dst = new byte[16];
        dst[0] = (byte)(src[0] * 2);
        // Constant-time feedback: if MSB of src[15] is set, XOR 135 (= 0x87, irreducible poly)
        dst[0] ^= (byte)(135 & (byte)(-(src[15] >> 7)));
        for (var j = 1; j < 16; j++)
        {
            dst[j] = (byte)(src[j] * 2);
            dst[j] += (byte)(src[j - 1] >> 7);
        }
        return dst;
    }

    private static void XorBlocks(Span<byte> dst, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        for (var i = 0; i < 16; i++)
            dst[i] = (byte)(a[i] ^ b[i]);
    }

    private static void XorBlocks(byte[] dst, ReadOnlySpan<byte> a, byte[] b)
    {
        for (var i = 0; i < 16; i++)
            dst[i] = (byte)(a[i] ^ b[i]);
    }

    private static void XorBlocksInPlace(Span<byte> dst, ReadOnlySpan<byte> src)
    {
        for (var i = 0; i < 16; i++)
            dst[i] ^= src[i];
    }

    private static void AesTransform(Span<byte> dst, ReadOnlySpan<byte> src, bool encrypt, byte[] key)
    {
        if (encrypt)
            AesEcbEncrypt(dst, src, key);
        else
            AesEcbDecrypt(dst, src, key);
    }

    private static void AesEcbEncrypt(Span<byte> dst, ReadOnlySpan<byte> src, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var encryptor = aes.CreateEncryptor();
        var result = encryptor.TransformFinalBlock(src.ToArray(), 0, 16);
        result.CopyTo(dst);
    }

    private static void AesEcbDecrypt(Span<byte> dst, ReadOnlySpan<byte> src, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var decryptor = aes.CreateDecryptor();
        var result = decryptor.TransformFinalBlock(src.ToArray(), 0, 16);
        result.CopyTo(dst);
    }

    private static byte[] Pkcs7Pad(byte[] data)
    {
        var padLen = 16 - (data.Length % 16);
        var padded = new byte[data.Length + padLen];
        data.CopyTo(padded, 0);
        for (var i = data.Length; i < padded.Length; i++)
            padded[i] = (byte)padLen;
        return padded;
    }

    private static byte[] Pkcs7Unpad(byte[] data)
    {
        if (data.Length == 0 || data.Length % 16 != 0)
            throw new InvalidOperationException("Invalid padded data.");
        var padLen = data[^1];
        if (padLen == 0 || padLen > 16)
            throw new InvalidOperationException("Invalid PKCS#7 padding.");
        for (var i = data.Length - padLen; i < data.Length; i++)
            if (data[i] != padLen)
                throw new InvalidOperationException("Invalid PKCS#7 padding byte.");
        return data[..^padLen];
    }
}
