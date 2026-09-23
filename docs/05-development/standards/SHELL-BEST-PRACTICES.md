# Shell 层最佳实践
> 版本: v1.0 | 日期: 2026-08-20

## 1. 导航规范

### 1.1 使用 ViewNames 常量
所有导航必须使用 `ViewNames` 常量，禁止硬编码字符串。

### 1.2 使用强类型导航参数（*Nav 工厂）
导航参数契约 SSOT 为 `*Nav` 工厂返回的字典：`MedicalCaseNav`（`ForExistingCase`/`ForNewCase`）、
`PatientManagementNav`、`RegistrationListNav`——键名为 `public const string` 契约键，消费端按契约键读取
（架构守卫 `NavParams_ContractKeys_ConsumedByTargetViewModel` 守护）。

```csharp
var navParams = MedicalCaseNav.ForExistingCase(medicalCaseId, patient, returnView: ViewNames.RegistrationList);
_ = _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, navParams);
```

> **禁止**再引入 `NavigateTo<TParams>` 泛型重载（2026-09-23 已删除）：C# 重载解析会把
> `Dictionary<string, object>` 实参优先绑定到泛型重载（恒等转换优于接口转换），其「按属性反射展开」
> 的实现会把 Dictionary 自身的 `Comparer/Count/Keys/Values` 当作导航参数 → 目标 VM 取不到契约键
> （接诊跳转医案工作台等链路参数全失）。导航参数一律走字典重载 `NavigateTo(string, IDictionary<string, object>?)`。

### 1.3 导航失败处理
`NavigationCoordinator.NavigateTo` 内部已处理异常，调用方无需额外 try-catch。

## 2. 模块加载规范

### 2.1 模块分类
- **核心模块**：`WhenAvailable`（AuthenticationModule, AdminModule, SysadminModule）
- **业务模块**：`OnDemand`（ClinicalModule, PatientsModule, CatalogModule, MedicalCaseModule, RegistrationModule, UsersModule 等）

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
