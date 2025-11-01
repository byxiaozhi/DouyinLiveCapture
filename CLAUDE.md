# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在此代码库中工作时提供指导。

## 项目概述

DouyinLiveCapture 是一个基于 .NET 9.0 构建的 WinUI 3 桌面应用程序，专门用于抖音直播间的自动录制。

### 核心功能

- **🎥 开播自动录制**: 监控指定抖音直播间，开播时自动开始录制
- **🛑 下播自动停止**: 直播结束时自动停止录制，无需手动干预
- **📁 自动保存**: 将直播流自动保存到用户指定的目录
- **🔄 视频转码**: 支持将录制的视频片段转码为通用 MP4 格式
- **⚙️ 自定义设置**: 支持自定义输出分辨率和码率，满足不同质量需求

### 应用场景

- 内容创作者的直播内容备份
- 重要直播节目的自动存档
- 多直播间同时监控和录制
- 直播内容的二次创作素材收集

项目使用 Windows App SDK，目标平台为 Windows 10 19041 版本及更高版本。

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

## MVVM 框架

此项目使用 **CommunityToolkit.Mvvm** 作为 MVVM 框架（版本 8.4.0），这是 Microsoft 官方推荐的现代 MVVM 库。

### 核心 Features

- **Source Generators**: 使用 C# 源生成器在编译时生成代码，零运行时开销
- **ObservableProperty**: 自动生成可观察属性，无需手动实现 INotifyPropertyChanged
- **RelayCommand**: 自动生成命令，支持同步和异步操作
- **ObservableObject**: 提供可观察对象基类

### 设计原则

**遵循标准 C# MVVM 设计范式：**

- **Model (模型)**: 业务逻辑和数据实体，不包含任何 UI 相关代码
- **View (视图)**: 纯 UI 层，XAML 文件和对应的代码隐藏文件，只处理 UI 交互和视觉呈现
- **ViewModel (视图模型)**: 连接 Model 和 View 的桥梁，处理 UI 逻辑、状态管理和数据转换

**关键设计要点：**

1. **View 和 ViewModel 分离**: View 只负责展示，ViewModel 只负责逻辑
2. **数据流向**: View → ViewModel → Model，单向数据流
3. **命令驱动**: 用户操作通过 Command 触发，避免事件处理
4. **可观察性**: 使用 `ObservableProperty` 实现属性变更通知
5. **无直接引用**: View 不直接引用 Model，通过 ViewModel 间接访问
6. **可测试性**: ViewModel 不依赖 UI 框架，便于单元测试

### 快速开始

#### ViewModel 示例

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = "应用程序标题";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "就绪";

    [ObservableProperty]
    public partial string UserName { get; set; } = "";

    [ObservableProperty]
    public partial int Count { get; set; }

    [ObservableProperty]
    public partial string SelectedOption { get; set; } = "选项1";

    [RelayCommand]
    private void StartProcess()
    {
        IsProcessing = true;
        StatusMessage = "正在处理...";
    }

    [RelayCommand]
    private void StopProcess()
    {
        IsProcessing = false;
        StatusMessage = "处理已停止";
    }

    [RelayCommand]
    private void SelectOption(string option)
    {
        SelectedOption = option;
        StatusMessage = $"已选择: {option}";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "加载中...";

            // 模拟异步操作
            await Task.Delay(1000);

            Count = 42;
            StatusMessage = "加载完成";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

#### XAML 绑定

```xml
<Page xmlns:viewModels="using:DouyinLiveCapture.ViewModels">
    <Page.DataContext>
        <viewModels:MainViewModel x:Name="ViewModel"/>
    </Page.DataContext>

    <StackPanel Spacing="12" Padding="20">
        <TextBlock Text="{x:Bind ViewModel.Title}"/>
        <TextBlock Text="{x:Bind ViewModel.StatusMessage}"/>
        <ProgressRing IsActive="{x:Bind ViewModel.IsLoading}"/>

        <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Content="开始捕获"
                    Command="{x:Bind ViewModel.StartCaptureCommand}"/>
            <Button Content="加载数据"
                    Command="{x:Bind ViewModel.LoadDataCommand}"/>
        </StackPanel>
    </StackPanel>
</Page>
```

#### 高级特性

```csharp
public partial class AdvancedViewModel : ObservableObject
{
    // 依赖属性通知
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    public partial string InputText { get; set; } = "";

    // 命令状态通知
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial bool IsValidInput { get; set; }

    // 计算属性
    public string DisplayText => $"输入: {InputText}, 有效: {IsValidInput}";

    // 属性变更回调
    partial void OnInputTextChanged(string? value)
    {
        Console.WriteLine($"输入已变更为: {value}");
        IsValidInput = !string.IsNullOrEmpty(value);
    }
}
```

### 性能优化建议

1. **使用 Source Generators**: 编译时生成代码，零运行时开销
2. **避免过度通知**: 只在必要时使用 `[NotifyPropertyChangedFor]`
3. **合理使用异步**: 对于长时间操作使用 `IAsyncRelayCommand`
4. **属性验证**: 使用 `[NotifyDataErrorInfo]` 进行数据验证
5. **内存管理**: 对于大型集合使用 `ObservableCollection<T>` 并适当实现分页

### 常见陷阱

1. **忘记 partial 类**: 使用 Source Generators 时必须将 ViewModel 声明为 `partial`
2. **命名约定**: 属性使用 PascalCase，源生成器会自动处理字段转换
3. **命令命名**: `[RelayCommand]` 会自动生成以 "Command" 结尾的属性
4. **MVVM 违反**: 避免在 ViewModel 中直接操作 UI 控件或引用 View
5. **业务逻辑混用**: Model 层不应包含 UI 相关逻辑，ViewModel 层不应包含持久化逻辑
6. **生命周期管理**: 正确实现异步操作的取消和异常处理

## 开发说明

- 这是一个仅限 Windows 的应用程序，需要 Windows 10 19041+
- 项目使用预览语言功能
- MSIX 打包已配置但当前禁用 (`WindowsPackageType=None`)
- 项目支持所有目标平台的独立部署