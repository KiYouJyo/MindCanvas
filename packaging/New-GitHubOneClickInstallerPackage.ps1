param(
  [Parameter(Mandatory=$true)][string]$SignedBundlePath,
  [Parameter(Mandatory=$true)][string]$PublicCertificatePath,
  [Parameter(Mandatory=$true)][string]$OutputDirectory,
  [Parameter(Mandatory=$true)][string]$DisplayVersion,
  [Parameter(Mandatory=$true)][ValidateSet('x64','arm64')][string]$Architecture
)
$ErrorActionPreference='Stop'
$root = Join-Path $OutputDirectory "MindCanvas-v$DisplayVersion-$Architecture-one-click"
if(Test-Path $root){Remove-Item $root -Recurse -Force}
New-Item -ItemType Directory -Path $root -Force | Out-Null
$bundleName = "MindCanvas_$DisplayVersion.0_$Architecture.msixbundle"
Copy-Item $SignedBundlePath (Join-Path $root $bundleName) -Force
Copy-Item $PublicCertificatePath (Join-Path $root 'MindCanvas.cer') -Force
$installer = @'
param([ValidateSet('auto','en-US','zh-CN','ja-JP')][string]$Language='auto')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSCommandPath
if($Language -eq 'auto'){$Language=[Globalization.CultureInfo]::CurrentUICulture.Name;if($Language -notin @('zh-CN','ja-JP')){$Language='en-US'}}
$msg=@{
'en-US'=@{start='Installing MindCanvas...';hash='Verifying package integrity...';cert='Trusting the MindCanvas publishing certificate for the current user...';done='MindCanvas was installed successfully.';fail='Installation failed:'}
'zh-CN'=@{start='正在安装 MindCanvas...';hash='正在验证安装包完整性...';cert='正在为当前用户信任 MindCanvas 发布证书...';done='MindCanvas 安装成功。';fail='安装失败：'}
'ja-JP'=@{start='MindCanvas をインストールしています...';hash='パッケージの整合性を確認しています...';cert='現在のユーザーに MindCanvas 発行証明書を信頼させています...';done='MindCanvas のインストールが完了しました。';fail='インストールに失敗しました：'}
}[$Language]
try{
 Write-Host $msg.start
 $manifest=Get-Content (Join-Path $root 'SHA256SUMS.txt')
 Write-Host $msg.hash
 foreach($line in $manifest){if($line -match '^([a-fA-F0-9]{64})  (.+)$'){$expected=$Matches[1];$file=Join-Path $root $Matches[2];if(-not(Test-Path $file)){throw "Missing file: $($Matches[2])"};$actual=(Get-FileHash $file -Algorithm SHA256).Hash;if($actual -ne $expected){throw "SHA256 mismatch: $($Matches[2])"}}}
 $certPath=Join-Path $root 'MindCanvas.cer';$cert=New-Object Security.Cryptography.X509Certificates.X509Certificate2($certPath)
 if($cert.Subject -cne 'CN=AppPublisher'){throw "Unexpected certificate subject: $($cert.Subject)"}
 $existing=Get-ChildItem Cert:\CurrentUser\TrustedPeople | Where-Object Thumbprint -eq $cert.Thumbprint
 if(-not $existing){Write-Host $msg.cert;Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null}
 $bundle=Get-ChildItem $root -Filter 'MindCanvas_*.msixbundle' | Select-Object -First 1
 if(-not $bundle){throw 'Signed MSIX bundle not found.'}
 $signature=Get-AuthenticodeSignature $bundle.FullName
 if(-not $signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $cert.Thumbprint){throw 'Bundle signer does not match MindCanvas.cer.'}
 Add-AppxPackage -Path $bundle.FullName -ForceApplicationShutdown
 Write-Host $msg.done -ForegroundColor Green
 Start-Process 'mindcanvas:'
}catch{Write-Host "$($msg.fail) $($_.Exception.Message)" -ForegroundColor Red;Read-Host 'Press Enter to close';exit 1}
'@
Set-Content (Join-Path $root 'Install-MindCanvas.ps1') $installer -Encoding UTF8
$cmdAuto = @'
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-MindCanvas.ps1" -Language auto
'@
$cmdZh = @'
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-MindCanvas.ps1" -Language zh-CN
'@
$cmdJa = @'
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-MindCanvas.ps1" -Language ja-JP
'@
$cmdUninstall = @'
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-AppxPackage KiYouJyo.MindCanvas | Remove-AppxPackage"
'@
Set-Content (Join-Path $root 'Install-MindCanvas.cmd') $cmdAuto -Encoding ASCII
Set-Content (Join-Path $root 'Install-MindCanvas.zh-CN.cmd') $cmdZh -Encoding ASCII
Set-Content (Join-Path $root 'Install-MindCanvas.ja-JP.cmd') $cmdJa -Encoding ASCII
Set-Content (Join-Path $root 'Uninstall-MindCanvas.cmd') $cmdUninstall -Encoding ASCII
$hashFiles=@($bundleName,'MindCanvas.cer')
$hashLines=foreach($name in $hashFiles){$file=Join-Path $root $name;"{0}  {1}" -f (Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant(),$name}
Set-Content (Join-Path $root 'SHA256SUMS.txt') $hashLines -Encoding ASCII
Set-Content (Join-Path $root 'README.txt') "MindCanvas v$DisplayVersion ($Architecture)`r`nRun Install-MindCanvas.cmd for a current-user one-click installation.`r`nThe installer verifies SHA-256 and the package signer before installation." -Encoding UTF8
Set-Content (Join-Path $root 'README.zh-CN.txt') "MindCanvas v$DisplayVersion ($Architecture)`r`n运行 Install-MindCanvas.cmd 即可按当前用户一键安装。`r`n安装前会验证 SHA-256 与安装包签名。" -Encoding UTF8
Set-Content (Join-Path $root 'README.ja-JP.txt') "MindCanvas v$DisplayVersion ($Architecture)`r`nInstall-MindCanvas.cmd を実行すると現在のユーザーへワンクリックでインストールできます。`r`nインストール前に SHA-256 と署名を検証します。" -Encoding UTF8
return $root
