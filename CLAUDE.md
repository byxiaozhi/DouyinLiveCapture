# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在此代码库中工作时提供指导。

## 项目概述

DouyinLiveCapture 是一个基于 .NET 9.0 构建的 WinUI 3 桌面应用程序，用于捕获抖音直播流。项目使用 Windows App SDK，目标平台为 Windows 10 19041 版本及更高版本。

## 解决方案结构

- `src/DouyinLiveCapture/` - 主要的 WinUI 应用程序项目
- `src/DouyinLiveCapture.Tests/` - 基于 MSTest 的单元测试项目
- `DouyinLiveCapture.slnx` - Visual Studio 解决方案文件

## 构建命令

### 构建整个解决方案
```bash
dotnet build DouyinLiveCapture.slnx
```

### 构建特定配置
```bash
dotnet build DouyinLiveCapture.slnx -c Release
dotnet build DouyinLiveCapture.slnx -c Debug
```

### 为特定平台构建
```bash
dotnet build DouyinLiveCapture.slnx -c Release -r win-x64
dotnet build DouyinLiveCapture.slnx -c Release -r win-x86
dotnet build DouyinLiveCapture.slnx -c Release -r win-arm64
```

## 测试命令

### 运行测试
```bash
dotnet test --project src/DouyinLiveCapture.Tests/
```

### 运行测试并显示详细输出
```bash
dotnet test --project src/DouyinLiveCapture.Tests/ --verbosity normal
```

**注意**: 由于项目配置了多个平台架构，必须使用 `--project` 参数指定测试项目。这是运行测试的最简单方式。

## 项目配置

### 关键属性
- 目标框架：`net9.0-windows10.0.19041.0`
- 输出类型：Windows 可执行文件 (`WinExe`)
- UI 框架：WinUI 3 (`UseWinUI=true`)
- 语言版本：`preview`
- 可空引用类型：已启用
- 隐式 using：已启用

### 构建配置
- **Debug**：标准开发构建，包含完整调试信息
- **Release**：启用 AOT 编译的优化构建
  - `SelfContained=true`
  - `PublishSingleFile=true`
  - `PublishTrimmed=true`
  - `PublishAot=true`

### 平台支持
- 支持 x86、x64、ARM64 架构
- Windows 运行时标识符：`win-x86`、`win-x64`、`win-arm64`

## 架构说明

应用程序遵循标准的 WinUI 3 应用程序模式：
- `App.xaml.cs` - 应用程序入口点和生命周期管理
- `MainWindow.xaml.cs` - 主应用程序窗口
- `Class1.cs` - 示例工具类

项目使用 MSTest 进行单元测试，采用 MSTest.Sdk 进行现代化测试项目配置。

## 开发说明

- 这是一个仅限 Windows 的应用程序，需要 Windows 10 19041+
- 项目使用预览语言功能
- MSIX 打包已配置但当前禁用 (`WindowsPackageType=None`)
- 项目支持所有目标平台的独立部署