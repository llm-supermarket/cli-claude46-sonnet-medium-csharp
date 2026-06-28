param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$exePath = Join-Path $PSScriptRoot "artifacts" "cli-claude46-sonnet-medium-csharp-windows-amd64.exe"

if (-not (Test-Path $exePath)) {
    throw "Unable to locate cli-claude46-sonnet-medium-csharp-windows-amd64.exe at $exePath"
}

$hash = (Get-FileHash -Path $exePath -Algorithm SHA256).Hash.ToLower()

Write-Host "Hash: $hash"

$repo = "llm-supermarket/cli-claude46-sonnet-medium-csharp"
$url = "https://github.com/$repo/releases/download/v$Version/cli-claude46-sonnet-medium-csharp-windows-amd64.exe"

$manifestPath = Join-Path $PSScriptRoot "cli-claude46-sonnet-medium-csharp.json"

$manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json

$manifest.version = $Version
$manifest.architecture."64bit".url = $url
$manifest.architecture."64bit".hash = $hash

$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -NoNewline

Write-Host "Updated cli-claude46-sonnet-medium-csharp.json to v$Version"
