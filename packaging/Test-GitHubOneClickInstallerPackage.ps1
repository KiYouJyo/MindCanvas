param([Parameter(Mandatory=$true)][string]$ReleaseDirectory)
$ErrorActionPreference='Stop'
$required=@('Install-MindCanvas.cmd','Install-MindCanvas.ps1','Install-MindCanvas.zh-CN.cmd','Install-MindCanvas.ja-JP.cmd','Uninstall-MindCanvas.cmd','MindCanvas.cer','SHA256SUMS.txt','README.txt','README.zh-CN.txt','README.ja-JP.txt')
foreach($name in $required){if(-not(Test-Path (Join-Path $ReleaseDirectory $name))){throw "Missing one-click package file: $name"}}
$bundle=@(Get-ChildItem $ReleaseDirectory -Filter 'MindCanvas_*.msixbundle');if($bundle.Count -ne 1){throw "Expected exactly one MSIX bundle; found $($bundle.Count)."}
foreach($cmd in @('Install-MindCanvas.cmd','Install-MindCanvas.zh-CN.cmd','Install-MindCanvas.ja-JP.cmd','Uninstall-MindCanvas.cmd')){$body=Get-Content (Join-Path $ReleaseDirectory $cmd) -Raw;if($body.Contains('\r\n')){throw "$cmd contains literal backslash newline escapes."}}
$manifest=Get-Content (Join-Path $ReleaseDirectory 'SHA256SUMS.txt')
foreach($line in $manifest){if($line -notmatch '^([a-f0-9]{64})  (.+)$'){throw "Invalid checksum line: $line"};$path=Join-Path $ReleaseDirectory $Matches[2];if(-not(Test-Path $path)){throw "Checksum target missing: $($Matches[2])"};$actual=(Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant();if($actual -ne $Matches[1]){throw "Checksum mismatch: $($Matches[2])"}}
Write-Host "One-click package validation passed: $ReleaseDirectory"
