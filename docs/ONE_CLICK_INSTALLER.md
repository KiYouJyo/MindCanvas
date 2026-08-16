# MindCanvas one-click installer

[简体中文](ONE_CLICK_INSTALLER.zh-CN.md) · [日本語](ONE_CLICK_INSTALLER.ja-JP.md)

Official GitHub releases are packaged as architecture-specific ZIP archives. Extract the archive and run `Install-MindCanvas.cmd`.

The package is designed to contain:

- signed MindCanvas MSIX
- MindCanvas publishing certificate (`MindCanvas.cer`)
- `Install-MindCanvas.cmd` and PowerShell installer
- localized launchers / instructions
- `SHA256SUMS.txt`

The installer validates checksums, imports the publishing certificate into the current user's Trusted People store when required, then installs the signed package with `Add-AppxPackage`. Administrative rights should not normally be required for the current-user flow.

Microsoft Store installations should use Store-managed updates instead of GitHub packages.
