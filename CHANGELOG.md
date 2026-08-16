# Changelog

All notable changes to MindCanvas are documented here. Product releases follow Semantic Versioning.

## [Unreleased]

### Added
- Started v0.2.0 functional editor integration on top of the Figma V4 UI baseline.
- Selected-node synchronization between map, narrow outline, and full outline views.
- Functional root-topic, subtopic, sibling-topic, delete, collapse, expand, rename, undo, and redo actions.
- Clipboard subtree operations with cross-tab copy, cut, paste, and duplicate support while generating fresh node IDs for pasted copies.
- Keyboard tree navigation with Up / Down, parent / child traversal with Left / Right, and hierarchy editing with Alt + Arrow keys.
- Keyboard editing shortcuts: Enter for sibling topics, Tab/Insert for subtopics, Delete for deletion, F2 for rename, Ctrl+C/X/V/D for subtree clipboard operations, and Ctrl+Z/Y for undo/redo.
- Undoable delete, move, collapse, and subtree-insert commands in the document core.
- Document-independent subtree templates that preserve titles, notes, hyperlinks, collapsed state, child order, and nested content.
- Real map zoom controls with 25%–400% range, live zoom percentage, and fit-to-view.
- Visible-tree traversal for collapsed branches and expanded tests covering editor mutation and subtree copy/paste primitives.
- Signed x64 acceptance packaging in CI with manifest-payload validation, signature verification, one-click installer validation, and SHA-256 checksum output.

### Changed
- Map / Outline / Split now behave as distinct functional view modes rather than presentation-only states.
- Editor toolbar enablement now follows the selected node and undo/redo history.
- Per-document selected-node state is retained when switching tabs or editor views.
- Development package version advanced to 0.2.0.0.
- Acceptance package metadata and filenames are now derived from the actual package manifest instead of the old v0.1.0 bootstrap value.

## [0.1.5] - 2026-08-16

### Added
- High-fidelity WinUI 3 implementation of the approved Figma V4 UI.
- Unified V4 top shell with persistent document tabs, context/category header, page actions, and editor command bar.
- Home, Documents, Templates, Settings, Map, Outline, and Split views aligned to V4 dimensions, spacing, colors, typography, and control states.
- Seven distinct Settings categories (General, Language & region, Appearance, Editing, Files, Export, About).
- Simplified Chinese, English, and Japanese strings for the V4 shell and page content.

### Changed
- Bumped package version to 0.1.5.0.
- Updated in-app update metadata to v0.1.5.

## [0.1.0] - 2026-08-16

### Added
- First public WinUI 3 engineering preview.
- Persistent document tabs and Home / Documents / Templates / Settings application shell.
- Simplified Chinese, English, and Japanese resources.
- SSOT `MindMapDocument` / `MindNode` tree and JSON-based `.mcanvas` storage.
- New, open, save, save-as, autosave groundwork, and undo / redo foundation.
- Basic right-logic layout, map rendering, and outline rendering.
- Microsoft Store / sideload-aware in-app update infrastructure.
- Per-Monitor V2 DPI awareness.
- Signed x64 / ARM64 MSIXBundle build and one-click ZIP release pipeline.
- Repository governance, contribution, security, release, and installer documentation.

### Notes
- Figma V4 is the only UI design source of truth in the repository.
- v0.1.0 establishes the runnable architecture; pixel-level V4 UI convergence continues in v0.1.5.
