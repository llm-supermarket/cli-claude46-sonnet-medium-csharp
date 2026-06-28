namespace CliTool.Crypto;

using Org.BouncyCastle.Crypto.Generators;

internal static class ScryptKeyDerivation
{
    private static readonly byte[] DefaultSalt =
    [
        0xA8, 0x0D, 0xF4, 0x3A, 0x8F, 0xBD, 0x03, 0x08,
        0xA7, 0xCA, 0xB8, 0x3E, 0x58, 0x1F, 0x86, 0xB1
    ];

    private const int N = 16384;
    private const int R = 8;
    private const int P = 1;
    private const int DkLen = 80;

    internal static DerivedKeys Derive(string password, string? customSalt = null)
    {
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
        var saltBytes = customSalt is null
            ? DefaultSalt
            : System.Text.Encoding.UTF8.GetBytes(customSalt);

        var output = SCrypt.Generate(passwordBytes, saltBytes, N, R, P, DkLen);

        var dataKey = output[0..32];
        var nameKey = output[32..64];
        var nameTweak = output[64..80];

        return new DerivedKeys(dataKey, nameKey, nameTweak);
    }
}

internal sealed record DerivedKeys(byte[] DataKey, byte[] NameKey, byte[] NameTweak);
