#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs the FlexiTrack Desktop MSIX package
.DESCRIPTION
    This script installs the self-signed certificate and the MSIX package.
    Must be run as Administrator.
#>

$ErrorActionPreference = "Stop"
$AppPackagesDir = "$PSScriptRoot\AppPackages"
$CerPath = "$AppPackagesDir\FlexiTrack.cer"
$MsixPath = "$AppPackagesDir\FlexiTrack.Desktop_1.0.0.0.msix"

Write-Host "FlexiTrack Desktop Installer" -ForegroundColor Cyan
Write-Host "============================`n" -ForegroundColor Cyan

# Check if running as admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "This script must be run as Administrator. Right-click and select 'Run as Administrator'."
    exit 1
}

# Check if files exist
if (-not (Test-Path $MsixPath)) {
    Write-Error "MSIX package not found at $MsixPath. Run build-msix.ps1 first."
    exit 1
}

if (-not (Test-Path $CerPath)) {
    # Export certificate if not already exported
    $cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq 'CN=FlexiTrack' } | Select-Object -First 1
    if ($cert) {
        Export-Certificate -Cert $cert -FilePath $CerPath -Force | Out-Null
        Write-Host "Exported certificate to $CerPath" -ForegroundColor Green
    } else {
        Write-Error "FlexiTrack certificate not found. Run build-msix.ps1 first."
        exit 1
    }
}

# Install certificate to Root and TrustedPeople stores (required for self-signed MSIX)
Write-Host "Installing certificate..." -ForegroundColor Yellow

# Install to Root store (needed for self-signed certificate chain)
$rootCert = certutil -store Root | Select-String "CN=FlexiTrack"
if (-not $rootCert) {
    certutil -addstore Root $CerPath | Out-Null
    Write-Host "  Certificate installed to Root store" -ForegroundColor Green
} else {
    Write-Host "  Certificate already in Root store" -ForegroundColor Green
}

# Install to TrustedPeople store
$trustedCert = certutil -store TrustedPeople | Select-String "CN=FlexiTrack"
if (-not $trustedCert) {
    certutil -addstore TrustedPeople $CerPath | Out-Null
    Write-Host "  Certificate installed to TrustedPeople store" -ForegroundColor Green
} else {
    Write-Host "  Certificate already in TrustedPeople store" -ForegroundColor Green
}

# Remove old version if installed
Write-Host "Checking for existing installation..." -ForegroundColor Yellow
$existingApp = Get-AppxPackage -Name "FlexiTrack.Desktop" -ErrorAction SilentlyContinue
if ($existingApp) {
    Write-Host "  Removing existing version..." -ForegroundColor Yellow
    Remove-AppxPackage -Package $existingApp.PackageFullName
    Write-Host "  Removed $($existingApp.Version)" -ForegroundColor Green
}

# Install the MSIX package
Write-Host "Installing FlexiTrack Desktop..." -ForegroundColor Yellow
Add-AppxPackage -Path $MsixPath
Write-Host "  Installation complete!" -ForegroundColor Green

# Verify installation
$installed = Get-AppxPackage -Name "FlexiTrack.Desktop"
if ($installed) {
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "FlexiTrack Desktop installed successfully!" -ForegroundColor Green
    Write-Host "Version: $($installed.Version)" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "`nYou can find FlexiTrack Desktop in your Start Menu." -ForegroundColor Cyan
} else {
    Write-Error "Installation verification failed"
}
