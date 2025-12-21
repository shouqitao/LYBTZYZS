# Spec Delta: client-layer-architecture

## MODIFIED Requirements

### Requirement: CLI-002 Modules层职责

Modules层(8个项目) SHALL 实现业务UI功能。

**标准目录结构**:
```
LYBT.Desktop.{Domain}/
├── {Domain}Module.cs              # Prism模块注册
├── Views/                         # XAML视图
│   ├── {Feature}View.xaml
│   └── Dialogs/                   # 弹窗视图
├── ViewModels/                    # ViewModel
│   ├── {Feature}ViewModel.cs
│   ├── Components/                # ViewModel组件 (当ViewModel > 500行时必需)
│   │   ├── {Entity}CommandHandler.cs
│   │   ├── {Entity}DataManager.cs
│   │   └── {Entity}Validator.cs
│   └── Dialogs/                   # 弹窗ViewModel
└── Services/                      # 客户端服务(可选)
```

#### Scenario: 创建业务视图
- **WHEN** 需要新增功能界面
- **THEN** SHALL 创建{Feature}View.xaml和{Feature}ViewModel.cs
- **AND** View SHALL 只包含XAML声明
- **AND** ViewModel SHALL 继承UnifiedViewModelBase

#### Scenario: ViewModel需要Components
- **WHEN** ViewModel超过500行
- **THEN** SHALL 创建ViewModels/Components/目录
- **AND** SHALL 提取CommandHandler、DataManager、Validator组件

---

## ADDED Requirements

### Requirement: CLI-002-A Components注册规范

Components SHALL 在Module中注册为Transient。

**示例**:
```csharp
public class {Domain}Module : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Components (Transient)
        containerRegistry.Register<{Entity}CommandHandler>();
        containerRegistry.Register<{Entity}DataManager>();
        containerRegistry.Register<{Entity}Validator>();
        
        // ViewModel
        containerRegistry.Register<{Feature}ViewModel>();
        
        // View导航
        containerRegistry.RegisterForNavigation<{Feature}View>();
    }
}
```

#### Scenario: 注册Components
- **WHEN** 模块有Components拆分
- **THEN** SHALL 使用Register<T>()注册为Transient
- **AND** SHALL 在ViewModel注册之前注册Components
