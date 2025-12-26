<#
.SYNOPSIS
    Installs Modulus module templates to Visual Studio.

.DESCRIPTION
    This script packages and installs the Modulus module project templates
    to Visual Studio's user templates directory.

.PARAMETER VSVersion
    Visual Studio version (2019, 2022). Defaults to 2022.

.EXAMPLE
    .\Install-Templates.ps1
    .\Install-Templates.ps1 -VSVersion 2019
#>

param(
    [ValidateSet("2019", "2022")]
    [string]$VSVersion = "2022"
)

$ErrorActionPreference = "Stop"

# Determine VS templates directory
$documentsPath = [Environment]::GetFolderPath("MyDocuments")
$vsTemplatesPath = Join-Path $documentsPath "Visual Studio $VSVersion\Templates\ProjectTemplates\Modulus"

Write-Host "Installing Modulus templates to: $vsTemplatesPath" -ForegroundColor Cyan

# Create directory if it doesn't exist
if (-not (Test-Path $vsTemplatesPath)) {
    New-Item -ItemType Directory -Path $vsTemplatesPath -Force | Out-Null
    Write-Host "Created directory: $vsTemplatesPath" -ForegroundColor Green
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$vsTemplatesSourceDir = Join-Path $scriptDir "VisualStudio"

# Package and install each template
$templates = @(
    "ModulusModule.Avalonia",
    "ModulusModule.Blazor",
    "ModulusHostApp.Avalonia",
    "ModulusHostApp.Blazor"
)

foreach ($template in $templates) {
    $templateSourceDir = Join-Path $vsTemplatesSourceDir $template
    $zipPath = Join-Path $vsTemplatesPath "$template.zip"

    if (-not (Test-Path $templateSourceDir)) {
        Write-Warning "Template source not found: $templateSourceDir"
        continue
    }

    Write-Host "Packaging $template..." -ForegroundColor Yellow

    # Remove existing zip if present
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    # Create zip archive
    Compress-Archive -Path "$templateSourceDir\*" -DestinationPath $zipPath -Force

    Write-Host "  Installed: $zipPath" -ForegroundColor Green
}

Write-Host ""
Write-Host "Installation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To use the templates:" -ForegroundColor Cyan
Write-Host "  1. Restart Visual Studio if it's running"
Write-Host "  2. File -> New -> Project"
Write-Host "  3. Search for 'Modulus Module'"
Write-Host ""

