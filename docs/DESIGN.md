# MindCanvas UI design handoff

## Source of truth

Only **V4** is used for implementation. Do not use V2 or V3 as a visual reference.

- MindCanvas V4: https://www.figma.com/design/v2ASRiL3MOtNY9YYWsdI2o/MindCanvas?node-id=24-2&t=Lxx6YXketx74v41G-1
- Windows UI Kit: https://www.figma.com/design/rYEiPqqUhm3nzBnUTtol36/Windows-UI-kit--Community-?node-id=165332-67172

## Implementation rules

- WinUI 3 / Windows App SDK is the target UI stack.
- Prefer native `TabView`, `NavigationView`, `CommandBar`, `ToggleSwitch`, `ComboBox`, `TextBox`, `Button`, `Grid`, and theme resources over custom-painted imitations.
- All pages keep the persistent document tab strip.
- Editor pages use Map / Outline / Split as current-document view switches.
- Non-editor pages retain the same continuous application shell but use page-specific actions.
- Mind map and outline views must consume the same SSOT document tree.
- Maintain Simplified Chinese / English / Japanese parity.
- Maintain `PerMonitorV2` DPI awareness and validate at 150% / 200% scaling.

## v0.1.5 handoff

The v0.1.5 UI pass should read the Figma file directly using the configured Figma access token and converge layout, spacing, typography hierarchy, Fluent colors, Mica/layering, and control sizing against V4. Do not store the Figma token in source, logs, commits, artifacts, or releases.
