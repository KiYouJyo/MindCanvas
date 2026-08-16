# MindCanvas

[English](README.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja-JP.md)

> **プレビュー** — v0.1.0 は最初の公開エンジニアリングプレビューです。v0.1.5 では V4 UI の高精度実装に重点を置きます。

MindCanvas は **WinUI 3** で構築する Windows ネイティブのマインドマップ／アウトラインアプリです。マップ、アウトライン、分割表示は同じドキュメントツリー（SSOT）を共有し、構造・テーマ・コンテンツテンプレートは分離した仕組みとして設計します。

## v0.1.0 Preview

- WinUI 3 / Windows App SDK シェルと常時表示のドキュメントタブ
- ホーム / ドキュメント / テンプレート / 設定ナビゲーション
- 簡体字中国語、英語、日本語の UI リソース
- `.mcanvas` ドキュメントモデルと JSON 保存
- 新規 / 開く / 保存 / 名前を付けて保存、自動保存基盤、Undo / Redo 基盤
- 基本的な右向きロジックレイアウトとマップ / アウトライン表示
- Microsoft Store / サイドロードを識別するアプリ内更新基盤
- Per-Monitor V2 高 DPI 対応
- Windows CI、署名済み MSIXBundle、x64 / ARM64 ワンクリック Release

## UI デザイン基準

リポジトリでは承認済みの **V4** のみを UI 実装の基準とします。V2/V3 はリポジトリの実装基準にはしません。

- [MindCanvas V4 — Figma](https://www.figma.com/design/v2ASRiL3MOtNY9YYWsdI2o/MindCanvas?node-id=24-2&t=Lxx6YXketx74v41G-1)
- [Windows UI Kit reference](https://www.figma.com/design/rYEiPqqUhm3nzBnUTtol36/Windows-UI-kit--Community-?node-id=165332-67172)
- [Design handoff](docs/DESIGN.md)

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
docs/                   デザイン・リリース・インストール文書
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

GitHub Release では x64 / ARM64 のワンクリック ZIP を配布します。展開して `Install-MindCanvas.cmd` を実行してください。署名済み MSIXBundle、発行証明書、インストールスクリプト、SHA-256 チェックサム、多言語説明を含みます。

詳細は [ワンクリックインストール手順](docs/ONE_CLICK_INSTALLER.ja-JP.md) を参照してください。

## ライセンス

MindCanvas は [MIT License](LICENSE) で公開されます。
