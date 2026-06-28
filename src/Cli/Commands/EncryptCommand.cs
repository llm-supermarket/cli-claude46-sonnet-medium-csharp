namespace CliTool.Commands;

using System.CommandLine;
using CliTool.Crypto;
using CliTool.Helpers;

internal static class EncryptCommand
{
    internal static Command Build()
    {
        var cmd = new Command("encrypt", "Encrypt a file using rclone-compatible encryption");

        var inputOption = new Option<FileInfo>(
            aliases: ["-i", "--input-file"],
            description: "Input file to encrypt")
        { IsRequired = true };

        var outputOption = new Option<FileInfo?>(
            aliases: ["-o", "--output-file"],
            description: "Output file path (derived from encrypted filename if --filename-encoding is set)");

        var passwordOption = new Option<string?>(
            "--password",
            description: "Encryption password (insecure; prefer RCLONE_ENCRYPT_PASSWORD env var)");

        var saltOption = new Option<string?>(
            "--salt",
            description: "Custom salt (uses rclone default salt if omitted)");

        var encodingOption = new Option<string>(
            "--filename-encoding",
            getDefaultValue: () => string.Empty,
            description: "Encrypt the output filename using this encoding (base32 or base64url)");

        cmd.AddOption(inputOption);
        cmd.AddOption(outputOption);
        cmd.AddOption(passwordOption);
        cmd.AddOption(saltOption);
        cmd.AddOption(encodingOption);

        cmd.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForOption(inputOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var passwordFlag = context.ParseResult.GetValueForOption(passwordOption);
            var salt = context.ParseResult.GetValueForOption(saltOption);
            var encodingStr = context.ParseResult.GetValueForOption(encodingOption);

            var password = PasswordHelper.Resolve(passwordFlag);
            var cipher = new RcloneCipher(password, salt);

            FilenameEncoding? encoding = encodingStr?.ToLowerInvariant() switch
            {
                "base32" => FilenameEncoding.Base32,
                "base64url" => FilenameEncoding.Base64Url,
                "" or null => null,
                _ => throw new ArgumentException($"Unknown filename encoding: {encodingStr}. Use base32 or base64url.")
            };

            var outputFile = ResolveOutputFile(input, output, encoding, cipher, encrypt: true);

            await using var inputStream = input.OpenRead();
            await using var outputStream = outputFile.OpenWrite();
            await cipher.EncryptFileAsync(inputStream, outputStream, context.GetCancellationToken());

            Console.WriteLine($"Encrypted: {input.FullName} -> {outputFile.FullName}");
        });

        return cmd;
    }

    private static FileInfo ResolveOutputFile(
        FileInfo input,
        FileInfo? explicitOutput,
        FilenameEncoding? encoding,
        RcloneCipher cipher,
        bool encrypt)
    {
        if (explicitOutput is not null)
            return explicitOutput;

        if (encoding is not null)
        {
            var encryptedName = cipher.EncryptFilename(input.Name, encoding.Value);
            return new FileInfo(Path.Combine(input.DirectoryName ?? ".", encryptedName));
        }

        return new FileInfo(input.FullName + ".enc");
    }
}
