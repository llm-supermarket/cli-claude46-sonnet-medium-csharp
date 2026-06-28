class CliClaude46SonnetMediumCsharp < Formula
  desc "CLI tool for rclone-compatible file encryption and decryption"
  homepage "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp"
  version "0.0.1"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp/releases/download/v0.0.1/cli-claude46-sonnet-medium-csharp-darwin-arm64.tar.gz"
      sha256 "5ee1a466bbf36f93c8a3a226a0f96ae35f3f0991bddf43e96f7f6a32192f4cd2"
    else
      url "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp/releases/download/v0.0.1/cli-claude46-sonnet-medium-csharp-darwin-amd64.tar.gz"
      sha256 "79638dde0dcc63359e5e66bcb17607797e11fe2db4e9452cdf428498d5fa9cee"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp/releases/download/v0.0.1/cli-claude46-sonnet-medium-csharp-linux-arm64.tar.gz"
      sha256 "eaa216849aea3cc018047e07063614503432ee5e0aec3d0fbd2427adf143f804"
    else
      url "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp/releases/download/v0.0.1/cli-claude46-sonnet-medium-csharp-linux-amd64.tar.gz"
      sha256 "ea68a4f1d17c548558cf492e0eabcd78b421460c2ca9c8eb8b664de08dcb9344"
    end
  end

  def install
    bin.install "cli-claude46-sonnet-medium-csharp-darwin-arm64" => "cli-claude46-sonnet-medium-csharp" if OS.mac? && Hardware::CPU.arm?
    bin.install "cli-claude46-sonnet-medium-csharp-darwin-amd64" => "cli-claude46-sonnet-medium-csharp" if OS.mac? && !Hardware::CPU.arm?
    bin.install "cli-claude46-sonnet-medium-csharp-linux-arm64" => "cli-claude46-sonnet-medium-csharp" if OS.linux? && Hardware::CPU.arm?
    bin.install "cli-claude46-sonnet-medium-csharp-linux-amd64" => "cli-claude46-sonnet-medium-csharp" if OS.linux? && !Hardware::CPU.arm?
  end

  test do
    assert_match "cli-claude46-sonnet-medium-csharp", shell_output("#{bin}/cli-claude46-sonnet-medium-csharp --version 2>&1")
  end
end