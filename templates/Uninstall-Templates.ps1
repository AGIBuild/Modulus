<#
.SYNOPSIS
    Uninstalls Modulus module templates from Visual Studio.

.PARAMETER VSVersion
    Visual Studio version (2019, 2022). Defaults to 2022.

.EXAMPLE
    .\Uninstall-Templates.ps1
#>

param(
    [ValidateSet("2019", "2022")]
    [string]$VSVersion = "2022"
)

$documentsPath = [Environment]::GetFolderPath("MyDocuments")
$vsTemplatesPath = Join-Path $documentsPath "Visual Studio $VSVersion\Templates\ProjectTemplates\Modulus"

if (Test-Path $vsTemplatesPath) {
    Write-Host "Removing Modulus templates from: $vsTemplatesPath" -ForegroundColor Yellow
    Remove-Item $vsTemplatesPath -Recurse -Force
    Write-Host "Templates uninstalled successfully." -ForegroundColor Green
} else {
    Write-Host "No Modulus templates found to uninstall." -ForegroundColor Yellow
}

