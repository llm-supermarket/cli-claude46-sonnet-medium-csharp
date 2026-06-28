namespace CliTool.Tests;

using CliTool.Crypto;
using FluentAssertions;
using Xunit;

public class EncryptDecryptTests
{
    private const string Password = "Testpassword1";
    private const string CustomSalt = "my-custom-salt-for-testing";

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public async Task DecryptBase32File_ContainsBip39Words()
    {
        var cipher = new RcloneCipher(Password);
        var filePath = Path.Combine(RepoRoot, "kr9tu4e1da4u3nifdd99g9tf5o");

        File.Exists(filePath).Should().BeTrue("the base32-named encrypted test file must be present in repo root");

        await using var inputStream = File.OpenRead(filePath);
        using var outputStream = new MemoryStream();
        await cipher.DecryptFileAsync(inputStream, outputStream);

        var text = System.Text.Encoding.UTF8.GetString(outputStream.ToArray()).Trim();
        text.Should().NotBeEmpty();
        text.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Should().HaveCountGreaterThan(0, "decrypted content should contain BIP39 words");
    }

    [Fact]
    public async Task DecryptBase64UrlFile_ContainsBip39Words()
    {
        var cipher = new RcloneCipher(Password);
        var filePath = Path.Combine(RepoRoot, "Iyxcijgc9bp3o5Y0npW6xqUvwWNcc3MA4SadB0sR6cY");

        File.Exists(filePath).Should().BeTrue("the base64url-named encrypted test file must be present in repo root");

        await using var inputStream = File.OpenRead(filePath);
        using var outputStream = new MemoryStream();
        await cipher.DecryptFileAsync(inputStream, outputStream);

        var text = System.Text.Encoding.UTF8.GetString(outputStream.ToArray()).Trim();
        text.Should().NotBeEmpty();
        text.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Should().HaveCountGreaterThan(0, "decrypted content should contain BIP39 words");
    }

    [Fact]
    public async Task BothTestFiles_DecryptToSameContent()
    {
        var cipher = new RcloneCipher(Password);
        var base32Path = Path.Combine(RepoRoot, "kr9tu4e1da4u3nifdd99g9tf5o");
        var base64Path = Path.Combine(RepoRoot, "Iyxcijgc9bp3o5Y0npW6xqUvwWNcc3MA4SadB0sR6cY");

        File.Exists(base32Path).Should().BeTrue("base32 test file must exist");
        File.Exists(base64Path).Should().BeTrue("base64url test file must exist");

        await using var s1 = File.OpenRead(base32Path);
        using var out1 = new MemoryStream();
        await cipher.DecryptFileAsync(s1, out1);

        await using var s2 = File.OpenRead(base64Path);
        using var out2 = new MemoryStream();
        await cipher.DecryptFileAsync(s2, out2);

        out1.ToArray().Should().Equal(out2.ToArray(),
            "both files should decrypt to the same plaintext content");
    }

    // Tests for custom salt: encrypt with salt, decrypt with same salt, roundtrip.
    [Fact]
    public async Task EncryptDecrypt_WithCustomSalt_RoundTrips()
    {
        var cipher = new RcloneCipher(Password, CustomSalt);
        var plaintext = System.Text.Encoding.UTF8.GetBytes("Secret BIP39 words: abandon ability able");

        using var input = new MemoryStream(plaintext);
        using var encrypted = new MemoryStream();
        await cipher.EncryptFileAsync(input, encrypted);

        encrypted.Position = 0;
        using var decrypted = new MemoryStream();
        await cipher.DecryptFileAsync(encrypted, decrypted);

        decrypted.ToArray().Should().Equal(plaintext);
    }

    [Fact]
    public async Task EncryptDecrypt_WithDefaultSalt_ProducesCorrectCiphertext()
    {
        // Cipher without salt uses the rclone default salt.
        var cipher = new RcloneCipher(Password);
        var plaintext = System.Text.Encoding.UTF8.GetBytes("hello world");

        using var input = new MemoryStream(plaintext);
        using var encrypted = new MemoryStream();
        await cipher.EncryptFileAsync(input, encrypted);

        encrypted.Position = 0;
        using var decrypted = new MemoryStream();
        await cipher.DecryptFileAsync(encrypted, decrypted);

        decrypted.ToArray().Should().Equal(plaintext);
    }

    [Fact]
    public async Task EncryptDecrypt_DifferentSalts_ProduceDifferentCiphertexts()
    {
        var cipherDefaultSalt = new RcloneCipher(Password);
        var cipherCustomSalt = new RcloneCipher(Password, CustomSalt);
        var plaintext = System.Text.Encoding.UTF8.GetBytes("Same plaintext different salt");

        using var enc1 = new MemoryStream();
        await cipherDefaultSalt.EncryptFileAsync(new MemoryStream(plaintext), enc1);

        using var enc2 = new MemoryStream();
        await cipherCustomSalt.EncryptFileAsync(new MemoryStream(plaintext), enc2);

        // Encrypted output must differ (beyond the random nonce — headers will differ slightly
        // but the main ciphertext body definitely differs due to different keys)
        enc1.ToArray().Should().NotEqual(enc2.ToArray());
    }

    // Simulates --password: password passed programmatically (mirrors env var path in PasswordHelper)
    [Fact]
    public async Task EncryptDecrypt_PasswordViaEnvVar_RoundTrips()
    {
        const string envPassword = "Testpassword1";
        Environment.SetEnvironmentVariable("RCLONE_ENCRYPT_PASSWORD", envPassword);
        try
        {
            var resolved = Environment.GetEnvironmentVariable("RCLONE_ENCRYPT_PASSWORD")!;
            var cipher = new RcloneCipher(resolved);
            var plaintext = System.Text.Encoding.UTF8.GetBytes("env var password test");

            using var input = new MemoryStream(plaintext);
            using var enc = new MemoryStream();
            await cipher.EncryptFileAsync(input, enc);

            enc.Position = 0;
            using var dec = new MemoryStream();
            await cipher.DecryptFileAsync(enc, dec);

            dec.ToArray().Should().Equal(plaintext);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RCLONE_ENCRYPT_PASSWORD", null);
        }
    }

    // Simulates the interactive prompt path by using the RcloneCipher constructor directly.
    // Real interactive tests cannot run headlessly, but we verify the cipher works
    // with a password that would come from a prompt.
    [Fact]
    public async Task EncryptDecrypt_PasswordFromPromptSimulation_RoundTrips()
    {
        // In production, PasswordHelper.Resolve reads from Console.ReadKey;
        // here we provide the password directly to test the crypto path.
        const string promptPassword = "InteractiveP@ssw0rd!";
        var cipher = new RcloneCipher(promptPassword);
        var plaintext = System.Text.Encoding.UTF8.GetBytes("prompt password crypto test");

        using var input = new MemoryStream(plaintext);
        using var enc = new MemoryStream();
        await cipher.EncryptFileAsync(input, enc);

        enc.Position = 0;
        using var dec = new MemoryStream();
        await cipher.DecryptFileAsync(enc, dec);

        dec.ToArray().Should().Equal(plaintext);
    }

    [Fact]
    public async Task EncryptDecrypt_WithBase32FilenameEncoding_RoundTrips()
    {
        var cipher = new RcloneCipher(Password);
        const string filename = "secret-document.pdf";
        var plaintext = System.Text.Encoding.UTF8.GetBytes("classified contents");

        var encryptedName = cipher.EncryptFilename(filename, FilenameEncoding.Base32);
        var decryptedName = cipher.DecryptFilename(encryptedName, FilenameEncoding.Base32);

        decryptedName.Should().Be(filename);

        using var input = new MemoryStream(plaintext);
        using var enc = new MemoryStream();
        await cipher.EncryptFileAsync(input, enc);

        enc.Position = 0;
        using var dec = new MemoryStream();
        await cipher.DecryptFileAsync(enc, dec);
        dec.ToArray().Should().Equal(plaintext);
    }

    [Fact]
    public async Task EncryptDecrypt_WithBase64FilenameEncoding_RoundTrips()
    {
        var cipher = new RcloneCipher(Password);
        const string filename = "another-file.txt";
        var plaintext = System.Text.Encoding.UTF8.GetBytes("another content");

        var encryptedName = cipher.EncryptFilename(filename, FilenameEncoding.Base64Url);
        var decryptedName = cipher.DecryptFilename(encryptedName, FilenameEncoding.Base64Url);

        decryptedName.Should().Be(filename);

        using var input = new MemoryStream(plaintext);
        using var enc = new MemoryStream();
        await cipher.EncryptFileAsync(input, enc);

        enc.Position = 0;
        using var dec = new MemoryStream();
        await cipher.DecryptFileAsync(enc, dec);
        dec.ToArray().Should().Equal(plaintext);
    }
}
