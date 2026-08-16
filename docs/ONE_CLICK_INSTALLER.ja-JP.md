# MindCanvas ワンクリックインストーラー

公式 GitHub Release はアーキテクチャ別 ZIP で配布します。展開後に `Install-MindCanvas.cmd` を実行してください。

ZIP には署名済み MSIX、`MindCanvas.cer`、CMD / PowerShell インストーラー、多言語説明、`SHA256SUMS.txt` を含める設計です。

インストーラーは SHA-256 を検証し、必要な場合は発行証明書を現在のユーザーの Trusted People ストアへ追加してから `Add-AppxPackage` でインストールします。通常のユーザー単位インストールでは管理者権限を必要としない設計です。

Microsoft Store 版は GitHub パッケージではなく Store の更新経路を使用します。
