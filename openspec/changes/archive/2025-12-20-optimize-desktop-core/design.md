# Design: optimize-desktop-core

## 架构决策

### 1. 异常处理统一方案

**决策**: 删除Desktop层异常处理实现，统一使用Shared.ExceptionHandling

**当前状态**:
```
Desktop.Infrastructure/Services/ErrorHandling/
├── ErrorHandlingService.cs (280行)
└── IErrorHandlingService.cs

Desktop.Presentation/Notifications/
├── UnifiedErrorHandlingService.cs (240行)
└── IUnifiedErrorHandlingService.cs
```

**目标状态**:
```
Shared.ExceptionHandling/
├── Exceptions/           (已有)
├── ProblemDetails/       (已有)
└── Services/             (新增)
    ├── IExceptionHandlingService.cs
    └── DesktopExceptionHandler.cs
```

**变更细节**:
- Desktop层添加对Shared.ExceptionHandling的引用
- 创建DesktopExceptionHandler适配WPF环境
- 删除两个重复实现

---

### 2. Token管理统一方案

**决策**: 删除Infrastructure.ITokenManager，保留Foundation层完整Token体系

**当前状态**:
```
Foundation/Security/
├── ITokenStorage.cs
├── ITokenStorageService.cs
├── ITokenValidator.cs
├── ITokenLifecycleService.cs  ← 保留此接口
├── SecureTokenStorage.cs
├── TokenStorageService.cs
└── TokenLifecycleService.cs

Infrastructure/Interfaces/
├── ITokenManager.cs           ← 删除(与上面重复)
└── ISessionManager.cs         ← 简化(移除Token职责)
```

**目标状态**:
```
Foundation/Security/
├── ITokenLifecycleService.cs  (主接口)
│   ├── bool IsTokenValid { get; }
│   ├── DateTime? TokenExpiration { get; }
│   ├── Task<string?> RefreshTokenAsync()
│   └── void SetToken(string token)
├── ITokenStorage.cs           (存储抽象)
└── ITokenValidator.cs         (验证抽象)

Infrastructure/Interfaces/
└── ISessionManager.cs         (仅会话状态)
    ├── UserDetailDto? CurrentUser { get; }
    ├── bool IsAuthenticated { get; }
    └── void SetCurrentUser(UserDetailDto user)
    // 删除: CurrentToken, AccessToken, RefreshToken
```

---

### 3. 映射器统一方案

**决策**: 保留SimpleMapper，删除MappingService，AutoMapper作为可选

**当前状态**:
```
Models/Mappers/SimpleMapper.cs     (JSON序列化方式)
Models/Mapping/MappingService.cs   (反射方式)
Presentation引用AutoMapper         (依赖注入方式)
```

**目标状态**:
```
Models/Mappers/SimpleMapper.cs     (保留-简单场景)
[删除] MappingService.cs
Presentation/AutoMapper            (保留-复杂场景)
```

**使用规范**:
- 简单DTO转换 → SimpleMapper
- 复杂映射规则 → AutoMapper Profile

---

### 4. 会话管理职责划分

**决策**: 明确三层职责，减少接口重叠

```
Foundation.IAuthenticationService (API调用)
├── LoginAsync(credentials) → LoginResponse
├── LogoutAsync()
├── RefreshTokenAsync() → RefreshTokenResponse
└── ChangePasswordAsync(...)

Foundation.ITokenLifecycleService (Token管理)
├── IsTokenValid
├── TokenExpiration
├── RefreshTokenAsync()
└── SetToken(token)

Infrastructure.ISessionManager (内存状态)
├── CurrentUser
├── IsAuthenticated
├── SetCurrentUser(user)
├── ClearSession()
└── HasPermission(role)
// 删除Token相关属性，委托给ITokenLifecycleService
```

---

### 5. ViewModel基类简化

**决策**: 4层→2层继承，抽离HTTP处理

**当前状态**:
```
BindableBase (Prism)
└── ViewModelBase (407行, 40+成员)
    └── UnifiedViewModelBase
        └── UnifiedListViewModelBase
```

**目标状态**:
```
BindableBase (Prism)
└── ViewModelBase (简化版, ~150行)
    ├── IsLoading, IsBusy, HasError
    ├── ErrorMessage, SuccessMessage
    ├── ExecuteSafelyAsync<T>()  (不含HTTP处理)
    └── Dispose()
    │
    ├── ListViewModelBase<T>
    │   ├── Items: ObservableCollection<T>
    │   ├── CurrentPage, TotalPages
    │   └── LoadCommand, RefreshCommand
    │
    └── DetailViewModelBase
        ├── IsEditMode
        ├── SaveCommand, CancelCommand
        └── ValidateAsync()
```

**HTTP状态码处理迁移**:
```csharp
// 从ViewModelBase移除，改为扩展方法或服务
public static class ApiExceptionHandler
{
    public static async Task HandleApiExceptionAsync(
        ApiException ex,
        IExceptionHandlingService handler,
        string operationName)
    {
        // HTTP状态码处理逻辑
    }
}
```

---

### 6. 接口位置调整

| 接口 | 当前位置 | 目标位置 | 原因 |
|------|---------|---------|------|
| ITokenManager | Infrastructure | 删除 | 与Foundation重复 |
| IUserSessionManager | Infrastructure | 删除 | 与ISessionManager合并 |
| IUserNotificationService | Infrastructure | Presentation | 通知是UI概念 |
| ILoginCoordinator | Infrastructure | Foundation | 登录是业务基础 |

---

### 7. 控件分离方案

**当前**: Infrastructure/Controls/有30+控件

**目标**:
```
Infrastructure/Controls/Common/     (通用控件)
├── LoadingOverlay.xaml
├── SearchBox.xaml
├── EmptyState.xaml
├── StatusBadge.xaml
└── VirtualizedDataGrid.xaml

各业务模块/Controls/                (业务控件)
├── MedicalCase/Controls/
│   └── ConsultationPanel.xaml
├── Prescriptions/Controls/
│   └── PrescriptionEditor.xaml
└── ...
```

**迁移清单**:
- LoginControl → Auth模块
- FormulaTemplateListItemControl → Formula模块
- 其他业务特定控件 → 对应模块

---

### 8. Item模型命名规范

**规范**:
- `{Entity}Item` = UI列表项模型，包含显示属性
- `{Entity}Dto` = API传输对象(在Shared层)
- `{Entity}ViewModel` = 带行为的视图模型

**示例**:
```csharp
// Shared层 - 数据传输
public class PatientDto { ... }

// Desktop.Models层 - 列表项(无行为)
public class PatientItem
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string DisplayAge { get; }  // 计算属性
}

// Desktop.Modules层 - 带行为
public class PatientItemViewModel : ViewModelBase
{
    public DelegateCommand EditCommand { get; }
}
```

---

## 依赖关系变更

### 变更前
```
Contracts ← Foundation ← Infrastructure ← Models ← Presentation
                              ↑
                         (重复功能)
```

### 变更后
```
Shared.ExceptionHandling
           ↓
Contracts ← Foundation ← Infrastructure ← Models ← Presentation
                              ↓
                         (职责清晰)
```

---

## 风险评估

### 高风险操作
1. 删除ITokenManager后更新所有引用
2. ViewModel基类简化后验证所有子类

### 缓解措施
- 每个Phase独立完成并验证编译
- 保持接口方法签名兼容
- 利用IDE重构工具批量更新引用

---

## 测试策略

1. **编译验证**: 每个Phase后确保全解决方案编译通过
2. **单元测试**: 运行现有测试套件
3. **冒烟测试**: 手动验证登录/主要功能流程

---

**Created**: 2025-12-20
**Author**: Claude Code
