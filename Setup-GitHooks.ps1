#!/usr/bin/env pwsh
# Script to set up the git hooks for Modulus

Write-Host "Setting up git hooks for Modulus..." -ForegroundColor Cyan

# Ensure the .githooks directory exists and has executable scripts
if (Test-Path ".githooks") {
    # Make sure pre-commit is executable (for Unix systems)
    if ($IsLinux -or $IsMacOS) {
        chmod +x .githooks/pre-commit
    }
    
    # Configure git to use our hooks directory
    git config core.hooksPath .githooks
    
    Write-Host "Git hooks installed successfully!" -ForegroundColor Green
    Write-Host "The following hooks are active:" -ForegroundColor Green
    Get-ChildItem .githooks | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Green }
} else {
    Write-Host "Error: .githooks directory not found!" -ForegroundColor Red
    Write-Host "Please make sure you're running this script from the repository root." -ForegroundColor Red
    exit 1
}

# Remind about the AI context manifest
Write-Host ""
Write-Host "Remember to use 'nuke StartAI' before starting development to bootstrap AI context." -ForegroundColor Yellow
Write-Host "For more information, see CONTRIBUTING.md" -ForegroundColor Yellow

exit 0
