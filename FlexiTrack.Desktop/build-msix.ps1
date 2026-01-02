#Requires -Version 5.1
<#
.SYNOPSIS
    Builds an MSIX package for FlexiTrack Desktop
.DESCRIPTION
    This script publishes the WPF application and creates an MSIX package
    using Windows SDK tools (MakeAppx.exe and SignTool.exe)
.PARAMETER Version
    Package version (default: 1.0.0.0)
.PARAMETER Configuration
    Build configuration (default: Release)
.PARAMETER SkipSign
    Skip code signing (creates unsigned package)
.EXAMPLE
    .\build-msix.ps1
    .\build-msix.ps1 -Version "1.2.0.0"
    .\build-msix.ps1 -SkipSign
#>
param(
    [string]$Version = "1.0.0.0",
    [string]$Configuration = "Release",
    [switch]$SkipSign
)

$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$OutputDir = Join-Path $ProjectDir "AppPackages"
$PublishDir = Join-Path $ProjectDir "bin\$Configuration\net10.0-windows\win-x64\publish"
$MsixDir = Join-Path $OutputDir "msix-content"
$PackageName = "FlexiTrack.Desktop_$Version.msix"
$PackagePath = Join-Path $OutputDir $PackageName
$CertPath = Join-Path $OutputDir "FlexiTrack.pfx"

# Find Windows SDK tools
function Find-WindowsSDKPath {
    # First, check NuGet packages (from Microsoft.Windows.SDK.BuildTools)
    $nugetPaths = @(
        "$env:USERPROFILE\.nuget\packages\microsoft.windows.sdk.buildtools"
        "$env:NUGET_PACKAGES\microsoft.windows.sdk.buildtools"
    )
    foreach ($basePath in $nugetPaths) {
        if (Test-Path $basePath) {
            $versions = Get-ChildItem $basePath -Directory | Sort-Object Name -Descending
            foreach ($ver in $versions) {
                $toolPath = Join-Path $ver.FullName "bin\10.0.26100.0\x64"
                if (Test-Path (Join-Path $toolPath "makeappx.exe")) {
                    return $toolPath
                }
                # Try other version patterns
                $binPath = Join-Path $ver.FullName "bin"
                if (Test-Path $binPath) {
                    $subVersions = Get-ChildItem $binPath -Directory | Sort-Object Name -Descending
                    foreach ($subVer in $subVersions) {
                        $toolPath = Join-Path $subVer.FullName "x64"
                        if (Test-Path (Join-Path $toolPath "makeappx.exe")) {
                            return $toolPath
                        }
                    }
                }
            }
        }
    }

    # Fall back to installed Windows SDK
    $sdkPaths = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.26100.0\x64"
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64"
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64"
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64"
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.18362.0\x64"
    )
    foreach ($path in $sdkPaths) {
        if (Test-Path (Join-Path $path "makeappx.exe")) {
            return $path
        }
    }
    # Try to find any version
    $basePath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path $basePath) {
        $versions = Get-ChildItem $basePath -Directory | Where-Object { $_.Name -match "^10\." } | Sort-Object Name -Descending
        foreach ($ver in $versions) {
            $toolPath = Join-Path $ver.FullName "x64"
            if (Test-Path (Join-Path $toolPath "makeappx.exe")) {
                return $toolPath
            }
        }
    }
    return $null
}

$SDKPath = Find-WindowsSDKPath
if (-not $SDKPath) {
    Write-Error "Windows SDK not found. Please install Windows SDK with App Certification Kit."
    exit 1
}

$MakeAppx = Join-Path $SDKPath "makeappx.exe"
$SignTool = Join-Path $SDKPath "signtool.exe"
$MakeCert = Join-Path $SDKPath "makecert.exe"
$Pvk2Pfx = Join-Path $SDKPath "pvk2pfx.exe"

Write-Host "Using Windows SDK from: $SDKPath" -ForegroundColor Cyan

# Clean output directories
Write-Host "`nCleaning output directories..." -ForegroundColor Yellow
if (Test-Path $MsixDir) { Remove-Item $MsixDir -Recurse -Force }
if (Test-Path $PackagePath) { Remove-Item $PackagePath -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $MsixDir -Force | Out-Null

# Publish the application
Write-Host "`nPublishing application..." -ForegroundColor Yellow
Push-Location $ProjectDir
try {
    dotnet publish -c $Configuration -r win-x64 --self-contained true -p:PublishReadyToRun=true
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed"
    }
} finally {
    Pop-Location
}

# Copy published files to MSIX staging directory
Write-Host "`nStaging files for MSIX package..." -ForegroundColor Yellow
Copy-Item -Path "$PublishDir\*" -Destination $MsixDir -Recurse -Force

# Copy assets
$AssetsSource = Join-Path $ProjectDir "Assets"
$AssetsTarget = Join-Path $MsixDir "Assets"
if (Test-Path $AssetsSource) {
    if (-not (Test-Path $AssetsTarget)) {
        New-Item -ItemType Directory -Path $AssetsTarget -Force | Out-Null
    }
    Copy-Item -Path "$AssetsSource\*" -Destination $AssetsTarget -Force
}

# Create AppxManifest.xml
Write-Host "`nCreating AppxManifest.xml..." -ForegroundColor Yellow
$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity
    Name="FlexiTrack.Desktop"
    Publisher="CN=FlexiTrack"
    Version="$Version"
    ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>FlexiTrack Desktop</DisplayName>
    <PublisherDisplayName>FlexiTrack</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
  </Resources>

  <Applications>
    <Application Id="App"
      Executable="FlexiTrack.Desktop.exe"
      EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="FlexiTrack Desktop"
        Description="Time tracking and task management desktop application"
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" Square71x71Logo="Assets\SmallTile.png" Square310x310Logo="Assets\LargeTile.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
    <Capability Name="internetClient" />
  </Capabilities>
</Package>
"@

$manifestPath = Join-Path $MsixDir "AppxManifest.xml"
$manifest | Out-File -FilePath $manifestPath -Encoding utf8

# Create the MSIX package
Write-Host "`nCreating MSIX package..." -ForegroundColor Yellow
& $MakeAppx pack /d $MsixDir /p $PackagePath /o
if ($LASTEXITCODE -ne 0) {
    throw "MakeAppx failed"
}

# Sign the package
if (-not $SkipSign) {
    Write-Host "`nSigning MSIX package..." -ForegroundColor Yellow

    # Create self-signed certificate if it doesn't exist
    if (-not (Test-Path $CertPath)) {
        Write-Host "Creating self-signed certificate..." -ForegroundColor Yellow

        # Use PowerShell to create certificate
        $cert = New-SelfSignedCertificate `
            -Type Custom `
            -Subject "CN=FlexiTrack" `
            -KeyUsage DigitalSignature `
            -FriendlyName "FlexiTrack Development Certificate" `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

        # Export to PFX
        $password = ConvertTo-SecureString -String "FlexiTrack123!" -Force -AsPlainText
        Export-PfxCertificate -Cert $cert -FilePath $CertPath -Password $password

        Write-Host "Certificate created and exported to: $CertPath" -ForegroundColor Green
        Write-Host "Certificate thumbprint: $($cert.Thumbprint)" -ForegroundColor Green
        Write-Host ""
        Write-Host "IMPORTANT: To install the MSIX package, you must first trust the certificate:" -ForegroundColor Yellow
        Write-Host "  1. Double-click $CertPath" -ForegroundColor White
        Write-Host "  2. Click 'Install Certificate'" -ForegroundColor White
        Write-Host "  3. Select 'Local Machine' -> 'Place all certificates in the following store'" -ForegroundColor White
        Write-Host "  4. Browse and select 'Trusted People'" -ForegroundColor White
        Write-Host ""
    }

    # Sign the package
    & $SignTool sign /fd SHA256 /a /f $CertPath /p "FlexiTrack123!" $PackagePath
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Signing failed. The package was created but is unsigned."
        Write-Host "You can sign it manually later with:" -ForegroundColor Yellow
        Write-Host "  signtool sign /fd SHA256 /a /f <certificate.pfx> /p <password> `"$PackagePath`"" -ForegroundColor White
    } else {
        Write-Host "Package signed successfully!" -ForegroundColor Green
    }
}

# Clean up staging directory
Remove-Item $MsixDir -Recurse -Force

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "MSIX package created successfully!" -ForegroundColor Green
Write-Host "Package: $PackagePath" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Green

# Show file size
$fileInfo = Get-Item $PackagePath
Write-Host "Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Cyan
