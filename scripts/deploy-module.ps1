param(
    [string]$ModuleId = "SimpleNotes",
    [string]$Configuration = "Debug"
)

$RootDir = Resolve-Path "$PSScriptRoot/.."
$ModulesOutDir = "$RootDir/_output/modules/$ModuleId"

Write-Host "Deploying $ModuleId to $ModulesOutDir..."

# Clean
if (Test-Path $ModulesOutDir) {
    Remove-Item $ModulesOutDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ModulesOutDir -Force | Out-Null

# Copy Manifest
Copy-Item "$RootDir/src/Modules/$ModuleId/manifest.json" "$ModulesOutDir/"

# Build and Copy Core
$CoreProject = "$RootDir/src/Modules/$ModuleId/$ModuleId.Core/$ModuleId.Core.csproj"
dotnet publish $CoreProject -c $Configuration -o "$ModulesOutDir" --no-self-contained

# Build and Copy UI.Blazor
$BlazorProject = "$RootDir/src/Modules/$ModuleId/$ModuleId.UI.Blazor/$ModuleId.UI.Blazor.csproj"
dotnet publish $BlazorProject -c $Configuration -o "$ModulesOutDir" --no-self-contained

# Build and Copy UI.Avalonia
$AvaloniaProject = "$RootDir/src/Modules/$ModuleId/$ModuleId.UI.Avalonia/$ModuleId.UI.Avalonia.csproj"
dotnet publish $AvaloniaProject -c $Configuration -o "$ModulesOutDir" --no-self-contained

# Cleanup unwanted files (pdb, etc if needed, or duplicates)
# dotnet publish will copy dependencies. We might have duplicates or shared libs.
# For now, simplistic flat folder.
# NOTE: In real scenario, we might want subfolders or ALC handling of shared deps.
# Modulus Core expects flat structure currently? 
# ModuleLoadContext looks in `_basePath`.
# ModuleLoader: `foreach (var assemblyRelativePath in manifest.CoreAssemblies) ... alc.LoadFromAssemblyPath(Path.Combine(packagePath, assemblyRelativePath));`
# So flat is fine if manifest says "SimpleNotes.Core.dll".

Write-Host "Deployment Complete."

