param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
function New-Asset([string]$Name,[int]$Width,[int]$Height) {
  $bitmap = New-Object System.Drawing.Bitmap($Width,$Height)
  $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
  $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $graphics.Clear([System.Drawing.Color]::FromArgb(0,0,0,0))
  $margin = [Math]::Max(2,[Math]::Floor([Math]::Min($Width,$Height)*0.12))
  $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,0,120,212))
  $graphics.FillRectangle($brush,$margin,$margin,$Width-($margin*2),$Height-($margin*2))
  $path = Join-Path $OutputDirectory $Name
  $bitmap.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
  $brush.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
New-Asset 'StoreLogo.png' 50 50
New-Asset 'Square44x44Logo.png' 44 44
New-Asset 'Square150x150Logo.png' 150 150
New-Asset 'Wide310x150Logo.png' 310 150
New-Asset 'SplashScreen.png' 620 300
