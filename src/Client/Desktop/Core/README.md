# LYBT.Desktop.Core v2.1 - 桌面客户端核心基础设施

## 🎯 项目概述

LYBT.Desktop.Core 为凌隐宝堂中医诊所系统的 WPF 客户端提供统一的技术底座。模块专注于“可复用的基础能力”，把通用的 MVVM 支撑、配置体系、主题与对话框服务、通用控件和转换器等集中在一个可维护的包中，供所有业务模块与工作台复用。

**核心价值**:
- 🏗️ **架构统一**: 提供 `CoreViewModel` 等基类，确保业务模块遵循一致的 MVVM 规范。
- ⚡ **开发提速**: 内置通知、错误处理、导航、配置等公共服务，避免重复封装。
- 🎨 **一致体验**: 主题、对话框与全局状态栏等 UI 组件保持视觉与交互一致。

## 📦 项目结构

```
LYBT.Desktop.Core/
├── Mvvm/                  # MVVM核心，提供ViewModel基类 (CoreViewModel)
├── ViewModels/            # 通用视图模型 (DialogViewModelBase)
├── Views/                 # 通用视图 (认证、状态栏等)
├── Controls/              # 可复用的WPF控件 (虚拟化列表等)
├── Converters/            # XAML绑定转换器
├── Services/              # 跨模块的公共服务接口与实现
├── Configuration/         # 客户端配置读取与管理
├── Events/                # Prism 跨模块通信事件定义
├── Validation/            # FluentValidation 验证器基类
└── Extensions/            # 框架与服务扩展方法
```

## 🛠 技术栈

- **.NET 8 & WPF**: 基础框架。
- **Prism.DryIoc 8.1**: 提供模块化、依赖注入和导航核心能力。
- **AutoMapper**: 用于对象-对象映射，尤其是在ViewModel和Model之间。
- **FluentValidation**: 提供强大、类型安全的验证规则。
- **Polly**: 提供网络请求的重试、熔断等弹性策略。
- **Microsoft.Extensions.Configuration**: 用于加载和绑定 `appsettings.json` 等配置文件。
- **System.Reactive**: 用于响应式编程，处理复杂的UI事件和异步数据流。

## 🚀 快速开始

此项目是一个类库，不包含可执行文件。可以通过解决方案或以下命令进行构建：

```bash
# 还原解决方案依赖
dotnet restore LYBT.All.sln

# 构建此项目
dotnet build src\Client\Desktop\Core\LYBT.Desktop.Core.csproj
```

## 🔌 API 接口

此项目为桌面端核心基础设施库，不直接对外提供任何API接口。它为上层业务模块提供可复用的组件和服务。

## 核心功能与推荐用法

### 继承基类 `CoreViewModel`

所有业务模块的 ViewModel 都应继承自 `CoreViewModel`，以自动获得以下功能：
- `IsBusy` 属性用于控制加载状态。
- `IEventAggregator` 用于发布和订阅跨模块事件。
- `IDialogService` 用于显示对话框。
- 统一的异步命令执行和异常处理。

```csharp
public class MyBusinessViewModel : CoreViewModel
{
    public DelegateCommand MyAsyncCommand { get; }

    public MyBusinessViewModel(IEventAggregator eventAggregator, IDialogService dialogService)
        : base(eventAggregator, dialogService)
    {
        MyAsyncCommand = new DelegateCommand(async () => await ExecuteAsync(DoSomething));
    }

    private async Task DoSomething()
    {
        // 你的异步业务逻辑
        await Task.Delay(1000);
    }
}
```

### 使用公共服务

通过依赖注入获取并使用 `Core` 模块提供的服务。

```csharp
// 在模块的 `RegisterTypes` 方法中注册
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // Core中已定义接口和部分实现
    // containerRegistry.RegisterSingleton<IConfigurationService, ConfigurationService>();
}

// 在ViewModel中使用
public class AnotherViewModel
{
    private readonly IConfigurationService _configService;

    public AnotherViewModel(IConfigurationService configService)
    {
        _configService = configService;
        var apiUrl = _configService.GetApiBaseUrl();
    }
}
```

## 🔒 维护建议
1. 修改 Core 代码前评估受影响模块，必要时同步联调。
2. 新增或调整公共接口需保持向后兼容，并更新文档。
3. 与外部依赖相关的调整需同步更新 `.csproj` 文件。
4. 跨模块行为变更必须附带单元/集成测试验证。