# MindCanvas

[English](README.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja-JP.md)

> **プレビュー** — MindCanvas は開発中です。最初の公開マイルストーンは v0.1.0 です。

MindCanvas は **WinUI 3** で構築する、モダンな Windows ネイティブのマインドマップ／アウトラインアプリです。マップ、アウトライン、分割表示は同じドキュメントツリーを共有し、構造テンプレートとビジュアルテーマを主要機能として扱います。

## v0.1.0 の目標

- 承認済み V4 デザインに基づく WinUI 3 アプリシェル
- 常時表示のドキュメントタブと ホーム / ドキュメント / テンプレート / 設定 ナビゲーション
- 簡体字中国語、英語、日本語の UI リソース
- マインドマップ文書モデルと JSON ベースの `.mcanvas` 形式
- 新規 / 開く / 保存 / 名前を付けて保存、自動保存基盤、Undo / Redo 基盤
- 基本的な右向きロジックレイアウトと編集可能なマップ画面
- Microsoft Store とサイドロードを識別するアプリ内更新基盤
- GitHub Actions によるビルド、テスト、署名、ワンクリック配布

## リポジトリ構成

```text
src/
  MindCanvas.App/       WinUI 3 デスクトップアプリ
  MindCanvas.Core/      ドキュメントモデルと編集コマンド
  MindCanvas.Layout/    レイアウト戦略とジオメトリ
  MindCanvas.Storage/   ネイティブ文書保存
  MindCanvas.Update/    更新チャネルと更新サービス
tests/                  単体テスト
packaging/              ワンクリックインストーラー
.github/workflows/      CI とリリース自動化
```

## 開発

Windows 11、Visual Studio 2026 / .NET SDK 10、Windows SDK 10.0.26100 を推奨します。

```powershell
dotnet restore MindCanvas.slnx
dotnet build MindCanvas.slnx -c Debug
dotnet test MindCanvas.slnx -c Debug
```

## ワンクリック Release

GitHub Release ではアーキテクチャ別の ZIP を配布する設計です。展開して `Install-MindCanvas.cmd` を実行してください。署名済み MSIX、発行証明書、インストールスクリプト、チェックサム、多言語説明を含みます。

詳細は [ワンクリックインストール手順](docs/ONE_CLICK_INSTALLER.ja-JP.md) を参照してください。

## ライセンス

MindCanvas は [MIT License](LICENSE) で公開されます。
