# Generate placeholder icons for MSIX packaging
# This script creates simple colored PNG files for development purposes
# Replace these with actual branded icons before production release

Add-Type -AssemblyName System.Drawing

$assetsPath = "$PSScriptRoot\Assets"
if (-not (Test-Path $assetsPath)) {
    New-Item -ItemType Directory -Path $assetsPath -Force | Out-Null
}

# Icon sizes required for MSIX
$icons = @(
    @{ Name = "StoreLogo.png"; Width = 50; Height = 50 }
    @{ Name = "Square44x44Logo.png"; Width = 44; Height = 44 }
    @{ Name = "Square44x44Logo.targetsize-44_altform-unplated.png"; Width = 44; Height = 44 }
    @{ Name = "Square150x150Logo.png"; Width = 150; Height = 150 }
    @{ Name = "Wide310x150Logo.png"; Width = 310; Height = 150 }
    @{ Name = "SmallTile.png"; Width = 71; Height = 71 }
    @{ Name = "LargeTile.png"; Width = 310; Height = 310 }
    @{ Name = "SplashScreen.png"; Width = 620; Height = 300 }
)

# FlexiTrack blue color
$bgColor = [System.Drawing.Color]::FromArgb(255, 59, 130, 246)
$fgColor = [System.Drawing.Color]::White

foreach ($icon in $icons) {
    $bitmap = New-Object System.Drawing.Bitmap($icon.Width, $icon.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear($bgColor)

    # Draw "FT" text in center
    $fontSize = [Math]::Min($icon.Width, $icon.Height) / 3
    $font = New-Object System.Drawing.Font("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold)
    $brush = New-Object System.Drawing.SolidBrush($fgColor)
    $text = "FT"
    $textSize = $graphics.MeasureString($text, $font)
    $x = ($icon.Width - $textSize.Width) / 2
    $y = ($icon.Height - $textSize.Height) / 2
    $graphics.DrawString($text, $font, $brush, $x, $y)

    $graphics.Dispose()
    $font.Dispose()
    $brush.Dispose()

    $outputPath = Join-Path $assetsPath $icon.Name
    $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()

    Write-Host "Created: $($icon.Name)"
}

Write-Host "`nPlaceholder icons generated in $assetsPath"
Write-Host "Replace these with actual branded icons before production release."
