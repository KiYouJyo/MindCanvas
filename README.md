# MindCanvas

[简体中文](README.md) · [English](README.en-US.md) · [日本語](README.ja-JP.md)

> **预览版** — v0.1.0 为首个公开工程预览版；v0.1.5 已按 Figma V4 完成高精度 WinUI 3 界面实现。

MindCanvas 是一款基于 **WinUI 3** 的 Windows 原生思维导图与大纲软件。导图、大纲与分屏共享同一棵文档树（SSOT），结构、主题与内容模板采用分离式架构。

## v0.1.5 Preview

- WinUI 3 / Windows App SDK 应用框架与全局文档标签
- 首页 / 文档库 / 模板 / 设置导航，以及导图 / 大纲 / 分屏编辑视图
- 按 Figma V4 对齐的全局 Shell、页面布局、尺寸、间距、圆角、字体与控件状态
- 七个互不重复的设置分类：常规、语言与区域、外观、编辑、文件、导出、关于
- 简体中文、English、日本語三语资源
- `.mcanvas` 文档模型与 JSON 持久化
- 新建 / 打开 / 保存 / 另存为、自动保存基础、撤销 / 重做基础
- 右向逻辑图布局，以及导图 / 大纲初始渲染
- 区分 Microsoft Store 与侧载来源的应用内更新基础设施
- Per-Monitor V2 高 DPI 支持
- Windows CI、签名 MSIXBundle 与 x64 / ARM64 一键安装 Release

## UI 设计基准

仓库只以已经确认的 **V4** 为 UI 实现基准；V2/V3 不作为仓库设计参考。

- [MindCanvas V4 — Figma](https://www.figma.com/design/v2ASRiL3MOtNY9YYWsdI2o/MindCanvas?node-id=24-2&t=Lxx6YXketx74v41G-1)
- [Windows UI Kit 参考](https://www.figma.com/design/rYEiPqqUhm3nzBnUTtol36/Windows-UI-kit--Community-?node-id=165332-67172)
- [设计交接说明](docs/DESIGN.md)

## 仓库结构

```text
src/
  MindCanvas.App/       WinUI 3 桌面应用
  MindCanvas.Core/      文档模型与编辑命令
  MindCanvas.Layout/    布局策略与几何快照
  MindCanvas.Storage/   原生文档持久化
  MindCanvas.Update/    更新来源识别与更新服务
tests/                  单元测试
packaging/              一键安装包脚本
docs/                   设计、发布与安装文档
.github/workflows/      CI 与发布自动化
```

## 开始开发

建议环境：Windows 11、Visual Studio 2026 / .NET SDK 10、Windows SDK 10.0.26100。

```powershell
dotnet restore MindCanvas.slnx
dotnet build MindCanvas.slnx -c Debug
dotnet test MindCanvas.slnx -c Debug
```

## 一键安装 Release

GitHub Release 提供 x64 与 ARM64 一键安装 ZIP。解压后运行 `Install-MindCanvas.cmd`；压缩包内包含签名 MSIXBundle、发布证书、安装脚本、SHA-256 校验和与三语说明。

详见 [一键安装说明](docs/ONE_CLICK_INSTALLER.zh-CN.md)。

## 文档

- [更新日志](CHANGELOG.md)
- [设计交接说明](docs/DESIGN.md)
- [参与贡献](CONTRIBUTING.md)
- [安全策略](SECURITY.md)
- [一键安装说明](docs/ONE_CLICK_INSTALLER.zh-CN.md)

## 许可证

MindCanvas 使用 [MIT License](LICENSE)。
