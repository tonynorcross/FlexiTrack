# Install the FlexiTrack certificate to Trusted People store
# This must be run as Administrator

$certPath = "$PSScriptRoot\AppPackages\FlexiTrack.pfx"
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq 'CN=FlexiTrack' } | Select-Object -First 1

if ($cert) {
    Write-Host "Found certificate: $($cert.Thumbprint)" -ForegroundColor Green

    # Export to CER (public key only)
    $cerPath = "$PSScriptRoot\AppPackages\FlexiTrack.cer"
    Export-Certificate -Cert $cert -FilePath $cerPath -Force | Out-Null

    # Import to Trusted People (requires admin)
    try {
        Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\LocalMachine\TrustedPeople
        Write-Host "Certificate installed to Trusted People store!" -ForegroundColor Green
    } catch {
        Write-Host "Failed to install certificate. Please run as Administrator." -ForegroundColor Red
        Write-Host "Or manually install by:" -ForegroundColor Yellow
        Write-Host "  1. Double-click $cerPath" -ForegroundColor White
        Write-Host "  2. Click 'Install Certificate'" -ForegroundColor White
        Write-Host "  3. Select 'Local Machine' -> 'Trusted People'" -ForegroundColor White
    }
} else {
    Write-Host "FlexiTrack certificate not found in CurrentUser\My store" -ForegroundColor Red
    Write-Host "Run build-msix.ps1 first to create the certificate" -ForegroundColor Yellow
}
