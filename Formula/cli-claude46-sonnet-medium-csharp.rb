class CliClaude46SonnetMediumCsharp < Formula
  desc "CLI tool for rclone-compatible file encryption and decryption"
  homepage "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp"
  version "0.0.1"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp/releases/download/v0.0.1/cli-claude46-sonnet-medium-csharp-darwin-arm64.tar.gz"
      sha256 "placeholder"
    else
      url "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp/releases/download/v0.0.1/cli-claude46-sonnet-medium-csharp-darwin-amd64.tar.gz"
      sha256 "placeholder"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp/releases/download/v0.0.1/cli-claude46-sonnet-medium-csharp-linux-arm64.tar.gz"
      sha256 "placeholder"
    else
      url "https://github.com/llm-supermarket/cli-claude46-sonnet-medium-csharp/releases/download/v0.0.1/cli-claude46-sonnet-medium-csharp-linux-amd64.tar.gz"
      sha256 "placeholder"
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
