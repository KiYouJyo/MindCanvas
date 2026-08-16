# MindCanvas 一键安装包

官方 GitHub Release 按架构提供 ZIP。解压后运行 `Install-MindCanvas.cmd`。

压缩包设计包含：

- 已签名的 MindCanvas MSIX
- 发布证书 `MindCanvas.cer`
- CMD / PowerShell 安装脚本
- 三语说明
- `SHA256SUMS.txt`

安装脚本会先验证 SHA-256，再在需要时把发布证书导入当前用户的“受信任人”证书存储，最后通过 `Add-AppxPackage` 安装。正常的当前用户安装不应要求管理员权限。

Microsoft Store 安装版本应始终使用商店更新通道，而不是 GitHub 安装包。
