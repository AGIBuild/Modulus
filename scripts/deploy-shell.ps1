param(
    [string]$ModuleId = "Modulus.Shell",
    [string]$Configuration = "Debug"
)

$RootDir = Resolve-Path "$PSScriptRoot/.."
$ModulesOutDir = "$RootDir/_output/modules/$ModuleId"

Write-Host "Deploying $ModuleId to $ModulesOutDir..."

if (Test-Path $ModulesOutDir) {
    Remove-Item $ModulesOutDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ModulesOutDir -Force | Out-Null

Copy-Item "$RootDir/src/Modules/Shell/manifest.json" "$ModulesOutDir/"

# Core
# Project name mismatch: folder is Shell.Core but csproj is Shell.Core.csproj?
# I created it as Shell.Core/Shell.Core.csproj
$CoreProject = "$RootDir/src/Modules/Shell/Shell.Core/Shell.Core.csproj"
dotnet publish $CoreProject -c $Configuration -o "$ModulesOutDir" --no-self-contained

# Blazor
$BlazorProject = "$RootDir/src/Modules/Shell/Shell.UI.Blazor/Shell.UI.Blazor.csproj"
dotnet publish $BlazorProject -c $Configuration -o "$ModulesOutDir" --no-self-contained

# Avalonia
$AvaloniaProject = "$RootDir/src/Modules/Shell/Shell.UI.Avalonia/Shell.UI.Avalonia.csproj"
dotnet publish $AvaloniaProject -c $Configuration -o "$ModulesOutDir" --no-self-contained

Write-Host "Deployment Complete."

