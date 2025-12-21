# Spec Deltas: optimize-desktop-core

## viewmodel-conventions

### 新增: VM-002-A 标准Components列表

在VM-002 Components模式后添加:

```markdown
#### Requirement: VM-002-A 标准Components类型

当ViewModel需要拆分时，SHALL 使用以下标准Component类型:

| Component类型 | 命名模式 | 职责 | 必需性 |
|---------------|----------|------|--------|
| CommandHandler | `{Entity}CommandHandler` | 处理用户命令(CRUD/批量操作) | 推荐 |
| DataManager | `{Entity}DataManager` | 数据加载、保存、导入导出 | 推荐 |
| Validator | `{Entity}Validator` | 业务验证逻辑 | 推荐 |
| Calculator | `{Entity}Calculator` | 计算逻辑 | 可选 |
| Coordinator | `{Entity}Coordinator` | 跨组件协调 | 可选 |
| StateMachine | `{Entity}StateMachine` | 状态管理 | 可选 |

#### Scenario: 选择Component类型
- **WHEN** ViewModel超过500行需要拆分
- **THEN** SHALL 优先提取CommandHandler、DataManager、Validator
- **AND** 根据业务需要选择性添加Calculator、Coordinator、StateMachine
```

---

## client-layer-architecture

### 更新: CLI-002 Modules层职责

更新**标准目录结构**部分:

```markdown
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
```

### 新增: CLI-002-A Components注册规范

在CLI-006 模块注册规范后添加:

```markdown
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
```
