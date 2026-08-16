# MindCanvas

[English](README.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja-JP.md)

> **预览版** — MindCanvas 正在持续开发，首个公开里程碑为 v0.1.0。

MindCanvas 是一款基于 **WinUI 3** 的现代 Windows 原生思维导图与大纲软件。导图、大纲与分屏共享同一棵文档树，并把结构模板、视觉主题与双视图编辑作为核心能力。

## v0.1.0 目标

- 按已确认的 V4 设计实现 WinUI 3 应用框架
- 全局文档标签，以及 首页 / 文档库 / 模板 / 设置 导航
- 简体中文、English、日本語三语资源
- 思维导图文档模型与 JSON `.mcanvas` 原生格式
- 新建 / 打开 / 保存 / 另存为、自动保存基础、撤销 / 重做基础
- 右向逻辑图基础布局与可编辑导图画布
- 区分 Microsoft Store 与侧载来源的应用内更新基础设施
- GitHub Actions 构建、测试、签名打包与一键安装 Release 工作流

## 仓库结构

```text
src/
  MindCanvas.App/       WinUI 3 桌面应用
  MindCanvas.Core/      文档模型与编辑命令
  MindCanvas.Layout/    自动布局策略与几何快照
  MindCanvas.Storage/   原生文档持久化
  MindCanvas.Update/    更新来源识别与更新服务
tests/                  单元测试
packaging/              一键安装包脚本
.github/workflows/      CI 与发布自动化
```

## 开始开发

环境建议：Windows 11、Visual Studio 2026 / .NET SDK 10、Windows SDK 10.0.26100。

```powershell
dotnet restore MindCanvas.slnx
dotnet build MindCanvas.slnx -c Debug
dotnet test MindCanvas.slnx -c Debug
```

## 一键安装 Release

GitHub Release 按架构提供一键安装 ZIP。解压后运行 `Install-MindCanvas.cmd`；压缩包内包含签名 MSIX、发布证书、安装脚本、校验和与三语说明。

详见 [一键安装说明](docs/ONE_CLICK_INSTALLER.zh-CN.md)。

## 文档

- [更新日志](CHANGELOG.md)
- [参与贡献](CONTRIBUTING.md)
- [安全策略](SECURITY.md)
- [一键安装说明](docs/ONE_CLICK_INSTALLER.zh-CN.md)

## 许可证

MindCanvas 使用 [MIT License](LICENSE)。
