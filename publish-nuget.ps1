# NuGet Publishing Script for AvaloniaImageCropper
# Usage: .\publish-nuget.ps1 [-Version "1.0.2"]

param(
    [string]$Version = "",
    [string]$ApiKey = ""
)

$ErrorActionPreference = "Stop"
$NuGetApiKey = if ($ApiKey) { $ApiKey } elseif ($env:NUGET_API_KEY) { $env:NUGET_API_KEY } else { throw "No API key provided. Use -ApiKey parameter or set NUGET_API_KEY environment variable." }
$ProjectPath = "$PSScriptRoot\src\Avalonia.ImageCropper\Avalonia.ImageCropper.csproj"
$OutputDir = "$PSScriptRoot\nupkg"

# Clean output directory
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir | Out-Null

# Build version argument
$versionArg = if ($Version) { "/p:Version=$Version" } else { "" }

Write-Host "Building and packing..." -ForegroundColor Cyan
dotnet pack $ProjectPath -c Release -o $OutputDir $versionArg

if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed!" -ForegroundColor Red
    exit 1
}

# Find the generated package
$package = Get-ChildItem -Path $OutputDir -Filter "*.nupkg" | Select-Object -First 1

if (-not $package) {
    Write-Host "No package found!" -ForegroundColor Red
    exit 1
}

Write-Host "Pushing $($package.Name) to NuGet..." -ForegroundColor Cyan
dotnet nuget push $package.FullName --api-key $NuGetApiKey --source https://api.nuget.org/v3/index.json

if ($LASTEXITCODE -eq 0) {
    Write-Host "Published successfully!" -ForegroundColor Green
} else {
    Write-Host "Push failed!" -ForegroundColor Red
    exit 1
}
