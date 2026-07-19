# Shell 层最佳实践

## 1. 导航规范

### 1.1 使用 ViewNames 常量
所有导航必须使用 `ViewNames` 常量，禁止硬编码字符串。

### 1.2 使用强类型导航参数
优先使用 `NavigateTo<TParams>` 泛型重载，而非 `Dictionary<string, object>`。

### 1.3 导航失败处理
`NavigationCoordinator.NavigateTo` 内部已处理异常，调用方无需额外 try-catch。

## 2. 模块加载规范

### 2.1 模块分类
- **核心模块**：`WhenAvailable`（AuthenticationModule, ClinicalModule, AdminModule, SysadminModule）
- **业务模块**：`OnDemand`（PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule, RegistrationModule）

### 2.2 模块依赖
使用 `[ModuleDependency]` 声明模块依赖，Prism 自动处理加载顺序。

### 2.3 模块预加载
登录成功后自动预加载高频模块（由 `ShellEventCoordinator` 触发）。

## 3. DI 注册规范

### 3.1 生命周期选择
- **Singleton**：无状态服务（导航、事件协调、角色注册表）
- **Transient**：有状态服务（ViewModel、Repository）
- **Scoped**：按会话隔离的服务

### 3.2 避免 Singleton → Transient
Singleton 依赖 Transient 会导致捕获过期实例。审计依赖链。

## 4. 事件通信规范

### 4.1 Shell 内部通信
使用 C# event（如 `LoginStateChanged`、`LogoutRequested`）。

### 4.2 跨模块通信
使用 Prism `EventAggregator`（如 `AuthEvents.PasswordChangedEvent`）。

### 4.3 事件定义位置
事件定义在 `LYBT.Desktop.Infrastructure.Events` 共享程序集中。

## 5. MVVM 规范

### 5.1 ViewModel 基类
所有 VM 继承 `NavigableViewModelBase`（已实现 `IEditable`、`INavigationAware`）。

### 5.2 属性和命令
使用 `CommunityToolkit.Mvvm`：
- `[ObservableProperty]` 替代手动 `OnPropertyChanged`
- `[RelayCommand]` 替代 `DelegateCommand`

### 5.3 ViewModel 映射
在模块的 `RegisterTypes` 中使用 `ViewModelLocationProvider.Register` 注册映射。

## 6. 代码风格

### 6.1 命名
- 公共成员：PascalCase
- 私有字段：_camelCase
- 接口：I 前缀

### 6.2 注释
- 中文业务文档/注释
- 英文标识符/commit
- 无 Emoji（除非要求）
