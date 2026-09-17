param(
    [string]$Configuration = "Release",
    [string]$PublishAot = "false",
    [string]$OutputDir = "artifacts"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Packaging Baudr for Windows (win-x64) ===" -ForegroundColor Cyan

$distDir = "dist/win-x64"
if (Test-Path $distDir) { Remove-Item -Recurse -Force $distDir }
if (!(Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir }

# Publish
$publishArgs = @(
    "publish", "src/Baudr.App/Baudr.App.csproj",
    "-c", $Configuration,
    "-r", "win-x64",
    "--self-contained",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-o", $distDir
)

if ($PublishAot -eq "true") {
    $publishArgs += "-p:PublishAot=true"
}

Write-Host "Running: dotnet $($publishArgs -join ' ')"
dotnet @publishArgs

$exePath = Join-Path $distDir "Baudr.exe"
if (!(Test-Path $exePath)) {
    throw "Build failed: Baudr.exe not found at $exePath"
}

# Run non-interactive verification
Write-Host "Running package sanity check..." -ForegroundColor Cyan
$verifyReport = Join-Path $distDir "verification.json"
Start-Process -FilePath $exePath -ArgumentList "--verify-package `"$verifyReport`"" -Wait

if (!(Test-Path $verifyReport)) {
    throw "Package verification failed: verification.json was not generated."
}
$reportContent = Get-Content $verifyReport -Raw
Write-Host "Verification Report: $reportContent" -ForegroundColor Green

# Prepare clean release directory (strip PDBs and test artifacts)
$releaseStaging = "dist/staging-win-x64"
if (Test-Path $releaseStaging) { Remove-Item -Recurse -Force $releaseStaging }
New-Item -ItemType Directory -Path $releaseStaging | Out-Null

Copy-Item $exePath -Destination $releaseStaging

if (Test-Path "README.md") { Copy-Item "README.md" -Destination $releaseStaging }
if (Test-Path "LICENSE") { Copy-Item "LICENSE" -Destination $releaseStaging }
if (Test-Path "THIRD-PARTY-NOTICES.md") { Copy-Item "THIRD-PARTY-NOTICES.md" -Destination $releaseStaging }

# Create Zip
$zipPath = Join-Path $OutputDir "Baudr-win-x64.zip"
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

Write-Host "Creating archive $zipPath..." -ForegroundColor Cyan
Compress-Archive -Path "$releaseStaging\*" -DestinationPath $zipPath -CompressionLevel Optimal

$zipItem = Get-Item $zipPath
Write-Host "Successfully packaged Baudr-win-x64.zip ($([math]::Round($zipItem.Length / 1MB, 2)) MB)" -ForegroundColor Green

