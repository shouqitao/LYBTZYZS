# LYBT.Desktop.Core

## 概述

LYBT.Desktop.Core是凌隐宝堂桌面客户端的核心框架库，提供WPF应用程序的基础架构、通用组件和服务。基于Prism.DryIoc 8.1.97框架构建，采用MVVM架构模式，为整个桌面应用提供统一的技术底座。

## 核心功能

### 🏗️ 架构基础
- **MVVM框架**: 基于Prism.DryIoc的完整MVVM架构实现
- **依赖注入**: DryIoc容器管理服务生命周期
- **事件聚合**: 模块间松耦合通信机制
- **导航服务**: 统一的视图导航和参数传递
- **对话框服务**: 标准化的弹窗和确认对话框

### ⚙️ 配置管理
- **AppConfiguration**: 支持多环境的配置管理系统
- **热重载**: 配置文件变更实时生效
- **环境特化**: Development/Staging/Production环境配置
- **运行时配置**: 动态修改和持久化配置项

### 🔧 核心服务
- **SessionManager**: 用户会话和状态管理
- **NotificationService**: 统一的消息通知服务  
- **ErrorHandlingService**: 全局异常处理和错误分类
- **ThemeService**: 主题切换和UI样式管理
- **CacheWarmupService**: 智能缓存预热机制

### 🎨 UI组件库
- **自定义控件**: 30+业务定制化控件
- **数据转换器**: 25+数据绑定转换器
- **虚拟化控件**: VirtualizedDataGrid、VirtualizedListView
- **智能加载**: SmartLoadingIndicator加载指示器
- **专业控件**: 用户、患者、处方、验方等业务控件

### 📱 MVVM组件
- **基础ViewModel**: CoreViewModel、DialogViewModel等基类
- **命令系统**: AsyncRelayCommand、RelayCommand命令实现
- **数据绑定**: ObservableObject响应式对象基类
- **列表管理**: BaseListViewModel分页列表基类

## 项目结构

```
src/Client/Desktop/Core/
├── Configuration/          # 配置管理
│   ├── AppConfiguration.cs     # 应用程序配置
│   ├── ApiConfiguration.cs     # API配置
│   └── RoleNavigationConfig.cs # 角色导航配置
├── Services/               # 核心服务
│   ├── SessionManager.cs       # 会话管理
│   ├── NotificationService.cs  # 通知服务
│   ├── ErrorHandlingService.cs # 错误处理
│   ├── ThemeService.cs         # 主题服务
│   └── Performance/            # 性能优化服务
├── ViewModels/            # 视图模型
│   ├── Base/                   # 基础ViewModel
│   ├── Dialogs/               # 对话框ViewModel
│   └── [Module]/              # 各业务模块ViewModel
├── Controls/              # 自定义控件
│   ├── Auth/                   # 认证控件
│   ├── Users/                 # 用户控件
│   ├── Patients/              # 患者控件
│   └── Prescriptions/         # 处方控件
├── Converters/            # 数据转换器
├── Events/                # 事件系统
├── Models/                # 数据模型
├── Interfaces/            # 服务接口
└── Extensions/            # 扩展方法
```

## 技术栈

### 核心依赖
- **.NET 8.0**: 目标框架
- **WPF**: Windows Presentation Foundation
- **Prism.DryIoc 8.1.97**: MVVM框架和依赖注入
- **AutoMapper 15.0.1**: 对象映射
- **FluentValidation 12.0.0**: 数据验证

### 支持库  
- **Polly 8.5.1**: 弹性和瞬态故障处理
- **Newtonsoft.Json 13.0.3**: JSON序列化
- **Microsoft.Extensions.Configuration 9.0.7**: 配置管理
- **System.Reactive 6.0.0**: 响应式编程
- **NPOI 2.7.4**: Excel文件处理

## 核心特性

### 🚀 性能优化
- **启动优化**: StartupOptimizationService减少冷启动时间
- **模块懒加载**: ModuleLoadingCoordinator按需加载业务模块  
- **UI虚拟化**: 大数据集合的虚拟化显示
- **智能缓存**: 多层级缓存策略和预热机制
- **对象池**: ObjectPoolService减少对象分配开销

### 🎯 用户体验
- **智能加载**: 统一的Loading状态管理
- **错误友好**: 用户友好的错误消息和恢复建议
- **主题切换**: 亮色/暗色主题动态切换
- **快捷键**: 键盘导航和快捷键支持
- **用户偏好**: 个性化设置持久化

### 🔒 稳定可靠
- **全局异常**: 统一异常捕获和分类处理
- **重试机制**: HTTP请求和服务调用重试策略
- **内存管理**: WeakEventManager避免内存泄漏
- **配置验证**: 启动时配置有效性检查

## 使用指南

### 基本配置

```json
{
  "ApiBaseUrl": "https://localhost:7001",
  "ConnectionTimeout": 30,
  "IsDebugMode": true,
  "Cache": {
    "DefaultExpirationMinutes": 30,
    "MaxSize": 1000
  },
  "Performance": {
    "EnableVirtualization": true,
    "MaxConcurrentRequests": 10
  }
}
```

### ViewModel继承

```csharp
public class YourViewModel : CoreViewModel
{
    public YourViewModel(IMapper mapper, ILogger<YourViewModel> logger) 
        : base(mapper, logger)
    {
        // 自动获得配置访问、错误处理、通知服务
    }
}
```

### 服务注册

```csharp
// 在模块初始化中
containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
containerRegistry.Register<INotificationService, NotificationService>();
```

## 开发规范

### MVVM模式
- 所有ViewModel必须继承CoreViewModel或其子类
- 使用AsyncRelayCommand处理异步命令
- 通过IMapper进行对象映射，避免手动转换
- 使用事件聚合器进行模块间通信

### 错误处理
- 捕获异常后调用ErrorHandlingService.HandleError()
- 为用户操作提供友好的错误消息
- 记录详细的错误日志供开发调试使用

### 性能最佳实践
- 大列表使用VirtualizedListView
- 异步操作显示SmartLoadingIndicator
- 使用对象池管理频繁创建的对象
- 及时释放事件订阅避免内存泄漏

## 相关模块

- **LYBT.Desktop.Infrastructure**: 基础设施和数据访问
- **LYBT.Desktop.Services**: 业务服务层
- **LYBT.Desktop.Auth**: 认证和权限模块
- **LYBT.Shared.Models**: 共享数据模型
- **LYBT.Shared.Interfaces**: 共享服务接口

## 维护说明

该项目是桌面客户端的核心基础库，任何修改都会影响到所有业务模块。请在修改前：

1. **充分测试**: 确保不影响现有功能
2. **向后兼容**: 保持接口和行为的向后兼容性
3. **文档更新**: 及时更新接口文档和使用示例
4. **性能验证**: 关键组件修改需进行性能测试

---

*该文档反映当前代码实现状态，与实际功能保持100%同步 - UltraThink文档驱动开发标准*