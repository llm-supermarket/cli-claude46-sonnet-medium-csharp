using System.CommandLine;
using CliTool.Commands;

var rootCommand = new RootCommand("rclone-compatible file encryption and decryption tool");

rootCommand.AddCommand(EncryptCommand.Build());
rootCommand.AddCommand(DecryptCommand.Build());

return await rootCommand.InvokeAsync(args);
