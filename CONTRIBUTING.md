# Contributing to MindCanvas

Thanks for helping improve MindCanvas.

## Development workflow

1. Create a focused branch from `main`.
2. Keep UI strings localizable; do not hard-code user-facing text when a resource key is appropriate.
3. Add or update tests for Core, Layout, and Storage behavior.
4. Run `dotnet build MindCanvas.slnx` and `dotnet test MindCanvas.slnx` before opening a pull request.
5. Keep commits and pull requests scoped and explain user-visible changes.

## Architecture rules

- `MindCanvas.Core` must not depend on WinUI.
- The document tree is the single source of truth for map and outline views.
- Layout algorithms consume document data and produce layout snapshots; UI code must not embed layout policy.
- File-format changes require an explicit schema-version migration path.
- Store installations must never be redirected to GitHub packages for updates.

## Languages

User-facing UI is maintained in Simplified Chinese, English, and Japanese. New resource keys should be added to all three `.resw` files in the same change.
