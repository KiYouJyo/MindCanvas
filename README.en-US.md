# MindCanvas

[简体中文](README.md) · [English](README.en-US.md) · [日本語](README.ja-JP.md)

> **Preview** — v0.1.0 was the first public engineering preview. v0.1.5 implements the high-fidelity Figma V4 UI.

MindCanvas is a Windows-native mind mapping and outline application built with **WinUI 3**. Mind map, outline, and split views share one document tree (SSOT), while layouts, themes, and content templates are designed as separate systems.

## v0.1.5 Preview

- WinUI 3 / Windows App SDK application shell with persistent document tabs
- Home / Documents / Templates / Settings navigation plus Map / Outline / Split editing views
- Figma V4-aligned global shell, page layout, dimensions, spacing, corner radii, typography, and control states
- Seven distinct Settings categories: General, Language & region, Appearance, Editing, Files, Export, About
- Simplified Chinese, English, and Japanese UI resources
- `.mcanvas` document model and JSON persistence
- New / open / save / save as, autosave groundwork, undo / redo foundation
- Basic right-logic layout and initial map / outline rendering
- Microsoft Store / sideload-aware in-app update infrastructure
- Per-Monitor V2 DPI awareness
- Windows CI, signed MSIXBundle packaging, and architecture-specific one-click ZIP releases

## UI design source of truth

Only the approved **V4** design is used as the UI source of truth for implementation. Earlier V2/V3 drafts are not repository references.

- [MindCanvas V4 — Figma](https://www.figma.com/design/v2ASRiL3MOtNY9YYWsdI2o/MindCanvas?node-id=24-2&t=Lxx6YXketx74v41G-1)
- [Windows UI Kit reference](https://www.figma.com/design/rYEiPqqUhm3nzBnUTtol36/Windows-UI-kit--Community-?node-id=165332-67172)
- [Design handoff notes](docs/DESIGN.md)

## Repository layout

```text
src/
  MindCanvas.App/       WinUI 3 desktop application
  MindCanvas.Core/      document model and editing commands
  MindCanvas.Layout/    layout strategies and geometry snapshots
  MindCanvas.Storage/   native document persistence
  MindCanvas.Update/    update channel detection and update services
tests/                  unit tests
packaging/              one-click installer tooling
docs/                   design/release/installer documentation
.github/workflows/      CI and release automation
```

## Development

Requirements: Windows 11 recommended, Visual Studio 2026 / .NET SDK 10, Windows SDK 10.0.26100.

```powershell
dotnet restore MindCanvas.slnx
dotnet build MindCanvas.slnx -c Debug
dotnet test MindCanvas.slnx -c Debug
```

## One-click release package

GitHub Releases provide x64 and ARM64 one-click ZIP packages. Extract the ZIP and run `Install-MindCanvas.cmd`. Each package contains the signed MSIXBundle, publishing certificate, installer scripts, checksums, and localized instructions.

See [One-click installer documentation](docs/ONE_CLICK_INSTALLER.md).

## Documentation

- [Changelog](CHANGELOG.md)
- [Design handoff](docs/DESIGN.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
- [One-click installer](docs/ONE_CLICK_INSTALLER.md)

## License

MindCanvas is released under the [MIT License](LICENSE).
