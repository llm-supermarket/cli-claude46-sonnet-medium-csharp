namespace CliTool.Tests;

using CliTool.Crypto;
using FluentAssertions;
using Xunit;

public class RcloneCipherTests
{
    private const string Password = "Testpassword1";

    [Fact]
    public void DecryptBase32Filename_ReturnsTestFileTxt()
    {
        var cipher = new RcloneCipher(Password);
        var encryptedFilename = "kr9tu4e1da4u3nifdd99g9tf5o";

        var result = cipher.DecryptFilename(encryptedFilename, FilenameEncoding.Base32);

        result.Should().Be("TEST_FILE.txt");
    }

    [Fact]
    public void DecryptBase64UrlFilename_ReturnsTestFileBase64Txt()
    {
        var cipher = new RcloneCipher(Password);
        var encryptedFilename = "Iyxcijgc9bp3o5Y0npW6xqUvwWNcc3MA4SadB0sR6cY";

        var result = cipher.DecryptFilename(encryptedFilename, FilenameEncoding.Base64Url);

        result.Should().Be("TEST_FILE BASE64.txt");
    }

    [Fact]
    public void EncryptThenDecryptFilename_RoundTrips()
    {
        var cipher = new RcloneCipher(Password);
        const string original = "hello_world.txt";

        var encrypted = cipher.EncryptFilename(original, FilenameEncoding.Base32);
        var decrypted = cipher.DecryptFilename(encrypted, FilenameEncoding.Base32);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void EncryptThenDecryptFilename_Base64Url_RoundTrips()
    {
        var cipher = new RcloneCipher(Password);
        const string original = "some-document.pdf";

        var encrypted = cipher.EncryptFilename(original, FilenameEncoding.Base64Url);
        var decrypted = cipher.DecryptFilename(encrypted, FilenameEncoding.Base64Url);

        decrypted.Should().Be(original);
    }

    [Theory]
    [InlineData("TEST_FILE.txt")]
    [InlineData("a")]
    [InlineData("longer_filename_with_underscores.txt")]
    public void FilenameRoundTrip_Base32_IsSymmetric(string filename)
    {
        var cipher = new RcloneCipher(Password);

        var encrypted = cipher.EncryptFilename(filename, FilenameEncoding.Base32);
        var decrypted = cipher.DecryptFilename(encrypted, FilenameEncoding.Base32);

        decrypted.Should().Be(filename);
    }

    [Theory]
    [InlineData("TEST_FILE.txt")]
    [InlineData("hello.bin")]
    [InlineData("document with spaces.pdf")]
    public void FilenameRoundTrip_Base64Url_IsSymmetric(string filename)
    {
        var cipher = new RcloneCipher(Password);

        var encrypted = cipher.EncryptFilename(filename, FilenameEncoding.Base64Url);
        var decrypted = cipher.DecryptFilename(encrypted, FilenameEncoding.Base64Url);

        decrypted.Should().Be(filename);
    }

    [Fact]
    public async Task EncryptThenDecryptFile_RoundTrips()
    {
        var cipher = new RcloneCipher(Password);
        var plaintext = System.Text.Encoding.UTF8.GetBytes("Hello, rclone encryption!");

        using var inputStream = new MemoryStream(plaintext);
        using var encryptedStream = new MemoryStream();
        await cipher.EncryptFileAsync(inputStream, encryptedStream);

        encryptedStream.Position = 0;
        using var decryptedStream = new MemoryStream();
        await cipher.DecryptFileAsync(encryptedStream, decryptedStream);

        decryptedStream.ToArray().Should().Equal(plaintext);
    }

    [Fact]
    public async Task EncryptThenDecryptLargeFile_RoundTrips()
    {
        var cipher = new RcloneCipher(Password);
        var plaintext = new byte[200_000];
        new Random(42).NextBytes(plaintext);

        using var inputStream = new MemoryStream(plaintext);
        using var encryptedStream = new MemoryStream();
        await cipher.EncryptFileAsync(inputStream, encryptedStream);

        encryptedStream.Position = 0;
        using var decryptedStream = new MemoryStream();
        await cipher.DecryptFileAsync(encryptedStream, decryptedStream);

        decryptedStream.ToArray().Should().Equal(plaintext);
    }
}
