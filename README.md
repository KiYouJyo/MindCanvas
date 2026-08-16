# MindCanvas

[English](README.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja-JP.md)

> **Preview** — MindCanvas is under active development. The first public milestone is v0.1.0.

MindCanvas is a modern Windows-native mind mapping and outline application built with **WinUI 3**. It treats mind maps, outlines, and split view as equal views over one document tree, with template-driven structure and visual themes as first-class capabilities.

## v0.1.0 goals

- WinUI 3 application shell based on the approved V4 design
- Persistent document tabs and Home / Documents / Templates / Settings navigation
- Simplified Chinese, English, and Japanese UI resources
- Mind-map document model and JSON-based `.mcanvas` storage
- New / open / save / save as, autosave groundwork, and undo / redo foundation
- Basic right-logic layout and editable map surface
- Distribution-aware in-app update infrastructure for Microsoft Store and sideload builds
- GitHub Actions build, test, signed packaging, and one-click installer release workflow

## Repository layout

```text
src/
  MindCanvas.App/       WinUI 3 desktop application
  MindCanvas.Core/      document model and editing commands
  MindCanvas.Layout/    layout strategies and geometry snapshots
  MindCanvas.Storage/   native document persistence
  MindCanvas.Update/    update channel detection and update services
tests/
  MindCanvas.Core.Tests/
  MindCanvas.Layout.Tests/
  MindCanvas.Storage.Tests/
packaging/              one-click installer tooling
.github/workflows/      CI and release automation
```

## Quick start

### Development

Requirements:

- Windows 11 recommended
- Visual Studio 2026 with Windows App SDK / WinUI workload, or .NET SDK 10
- Windows SDK 10.0.26100

```powershell
dotnet restore MindCanvas.slnx
dotnet build MindCanvas.slnx -c Debug
dotnet test MindCanvas.slnx -c Debug
```

### One-click release package

GitHub releases are designed to provide architecture-specific one-click ZIP packages. Extract the ZIP and run `Install-MindCanvas.cmd`. The package contains the signed MSIX, publishing certificate, installer scripts, checksums, and localized instructions.

See [One-click installer documentation](docs/ONE_CLICK_INSTALLER.md).

## Documentation

- [Changelog](CHANGELOG.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
- [One-click installer](docs/ONE_CLICK_INSTALLER.md)

## License

MindCanvas is released under the [MIT License](LICENSE).
