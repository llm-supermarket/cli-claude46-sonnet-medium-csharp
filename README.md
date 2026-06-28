# rclone-encrypt-claude46-sonnet-medium
A small CLI tool that encrypts and decrypts using the rclone encryption defaults. 

Rclone uses a custom salt if no salt is provided, which this tool will use by default. A few similar tools:

- https://github.com/rclone/rclone
- https://github.com/mcolatosti/rclonedecrypt
- https://github.com/br0kenpixel/rclone-rcc
- @fyears/rclone-crypt

Rclone encryption uses: 
- NaCl SecretBox (XSalsa20 + Poly1305) for the file contents.
- AES256 for the filenames.
- scrypt for keymaterial.

## Installation

**Scoop (Windows)**
```powershell
scoop bucket add cli-claude46-sonnet-medium-csharp https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp
scoop install cli-claude46-sonnet-medium-csharp
```

**Homebrew (macOS/Linux)**
```bash
brew tap llm-supermarket/cli-claude46-sonnet-medium-csharp https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp
brew install cli-claude46-sonnet-medium-csharp
```

## Usage

### Encrypt a file

```bash
# Prompt for password interactively (recommended)
cli-claude46-sonnet-medium-csharp encrypt -i plaintext.txt -o encrypted.bin

# Use an environment variable (safer than --password)
export RCLONE_ENCRYPT_PASSWORD="your-password"
cli-claude46-sonnet-medium-csharp encrypt -i plaintext.txt -o encrypted.bin

# Pass password directly (not recommended – visible in terminal history)
cli-claude46-sonnet-medium-csharp encrypt -i plaintext.txt -o encrypted.bin --password "your-password"

# Encrypt with a custom salt
cli-claude46-sonnet-medium-csharp encrypt -i plaintext.txt -o encrypted.bin --salt "my-custom-salt"

# Encrypt filename using base32 encoding (output filename is the encrypted name)
cli-claude46-sonnet-medium-csharp encrypt -i plaintext.txt --filename-encoding base32

# Encrypt filename using base64url encoding
cli-claude46-sonnet-medium-csharp encrypt -i plaintext.txt --filename-encoding base64url
```

### Decrypt a file

```bash
# Decrypt with interactive password prompt
cli-claude46-sonnet-medium-csharp decrypt -i encrypted.bin -o plaintext.txt

# Decrypt using environment variable
export RCLONE_ENCRYPT_PASSWORD="your-password"
cli-claude46-sonnet-medium-csharp decrypt -i encrypted.bin -o plaintext.txt

# Decrypt a file whose name is a base32-encoded rclone-encrypted filename
cli-claude46-sonnet-medium-csharp decrypt -i kr9tu4e1da4u3nifdd99g9tf5o --filename-encoding base32

# Decrypt a file whose name is a base64url-encoded rclone-encrypted filename
cli-claude46-sonnet-medium-csharp decrypt -i Iyxcijgc9bp3o5Y0npW6xqUvwWNcc3MA4SadB0sR6cY --filename-encoding base64url

# Decrypt with a custom salt
cli-claude46-sonnet-medium-csharp decrypt -i encrypted.bin -o plaintext.txt --salt "my-custom-salt"
```

## Flags

| Flag | Short | Description |
|------|-------|-------------|
| `--input-file` | `-i` | *(required)* Input file path |
| `--output-file` | `-o` | Output file path (derived from filename if `--filename-encoding` is set) |
| `--password` | | Password (insecure; prefer `RCLONE_ENCRYPT_PASSWORD` env var) |
| `--salt` | | Custom salt (uses rclone default salt if omitted) |
| `--filename-encoding` | | `base32` or `base64url` – encrypt/decrypt the filename itself |

## Security notes

- **Do not use `--password` on the command line.** The password will appear in your terminal history and in `ps` output. Use the `RCLONE_ENCRYPT_PASSWORD` environment variable or let the tool prompt you interactively.
- The default salt (`\xA8\x0D\xF4…`) matches rclone's built-in default. To maximise security, provide a unique `--salt` value.
- Files are encrypted in 64 KB blocks using NaCl SecretBox (XSalsa20-Poly1305), so tampering with any block is detected.

## Compatibility

Output files are byte-for-byte compatible with rclone's `crypt` backend configured with the same password and salt.
