# Client/Desktop 架构优化

> 日期: 2026-06-29
> 状态: 草稿
> 范围: Shell + Modules + Core

## [O1] 当前架构问题

### 高优先级（HIGH）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| H1 | Desktop.Registration 引用其他模块 | LYBT.Desktop.Registration.csproj | 违反模块隔离 |
| H2 | 8 个项目未纳入架构测试 | DesktopLayerArchTests.cs | 无保护 |
| H3 | LoginCoordinator 硬编码旁路 | LoginCoordinator.cs | 已确认为 bug |

### 中优先级（MEDIUM）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| M1 | 部分 ViewModel 超过 500 行 | 各 ViewModel | 可维护性下降 |
| M2 | SignalR 未实现 | Registration 模块 | 待诊队列靠轮询刷新 |

## [O2] 模块依赖图

```
Shell
├── Roles
│   ├── Admin → Herbs, Formula, Patients, MedicalCase, Users
│   ├── Clinical → Herbs, Formula, Patients, MedicalCase
│   ├── Receptionist → Registration, Patients
│   └── Sysadmin → Users
├── Core
│   ├── Infrastructure → Foundation, Contracts
│   ├── Foundation → Contracts
│   ├── Contracts (无依赖)
│   ├── Controls → Foundation, Contracts
│   ├── Navigation → Infrastructure, Foundation, Contracts
│   ├── Printing → Infrastructure
│   ├── CardReader → Infrastructure
│   └── LocalData → LYBT.Entities (例外)
├── Modules
│   ├── Auth → Core.*
│   ├── Users → Core.*
│   ├── Patients → Core.*
│   ├── Herbs → Core.*
│   ├── Formula → Core.*, HerbsModule
│   ├── MedicalCase → Core.*, FormulaModule
│   ├── Registration → Core.*, MedicalCase, Patients, Users ⚠️
│   ├── Reports → Core.*
│   └── Sysadmin → Core.*
└── LocalWebAPI → 所有 Server 模块 (例外)
```

## [O3] 优化方案

### 方案 1: Desktop.Registration 模块隔离（H1）

```markdown
当前问题: Desktop.Registration 引用 MedicalCase, Patients, Users

解决方案: 通过接口通信

1. 在 LYBT.Desktop.Contracts 中定义接口
public interface IPatientSearchService
{
    Task<List<PatientListDto>> SearchPatientsAsync(string keyword);
}

public interface IDoctorSearchService
{
    Task<List<UserListDto>> GetDoctorsAsync();
}

2. 各模块实现接口
// Patients 模块
public class PatientSearchService : IPatientSearchService { }

// Users 模块
public class DoctorSearchService : IDoctorSearchService { }

3. Registration 模块注入接口
public class RegistrationCreateDialogViewModel
{
    private readonly IPatientSearchService _patientService;
    private readonly IDoctorSearchService _doctorService;
    
    // 不再直接引用 Patients/Users 模块
}
```

### 方案 2: 架构测试补全（H2）

```csharp
// DesktopLayerArchTests.cs 补充 8 个项目
private static readonly string[] DesktopAssemblies = new[]
{
    "LYBT.Desktop.LocalData",
    "LYBT.Desktop.Navigation",
    "LYBT.Desktop.Printing",
    "LYBT.Desktop.CardReader",
    "LYBT.Desktop.Reports",
    "LYBT.Desktop.Registration",
    "LYBT.Desktop.Sysadmin",
    "LYBT.Desktop.Receptionist",
    "LYBT.Desktop.Auth",
    "LYBT.Desktop.Users",
    "LYBT.Desktop.Patients",
    "LYBT.Desktop.Herbs",
    "LYBT.Desktop.Formula",
    "LYBT.Desktop.MedicalCase",
};

// 新增模块间引用检查
[Fact]
public void DesktopModules_Should_Not_Reference_Other_DesktopModules()
{
    // 检查模块间无直接引用
    // 例外: Registration 可以引用其他模块（协调者角色）
}
```

### 方案 3: 修复 LoginCoordinator（H3）

```csharp
// 当前: LoginCoordinator 硬编码旁路
// 修复: 移除硬编码，使用标准模块加载流程

public class LoginCoordinator : ILoginCoordinator
{
    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        // 标准登录流程
        var loginResult = await _authService.LoginAsync(username, password);
        if (!loginResult.Success)
            return LoginResult.Failed(loginResult.Message);
        
        // 标准模块加载（非硬编码）
        await _moduleLoadingService.LoadModulesForRoleAsync(loginResult.UserRole);
        
        return LoginResult.Succeeded(loginResult.User);
    }
}
```

### 方案 4: ViewModel 拆分（M1）

```markdown
当前问题: 部分 ViewModel 超过 500 行

解决方案: 使用 ADR-0006 组件化分解模式

超过 500 行的 ViewModel 必须拆分:
- Coordinator: 绑定+导航
- Components:
  - DataManager: 数据加载、缓存
  - CommandHandler: CRUD 命令
  - Validator: 业务验证

示例:
MedicalCaseMasterDetailViewModel (480行)
├── MedicalCaseDataManager (数据加载)
├── MedicalCaseCommandHandler (CRUD)
└── MedicalCaseValidator (验证)
```

## [O4] 实施优先级

| 批次 | 任务 | 工作量 | 风险 |
|------|------|--------|------|
| 1 | H3: 修复 LoginCoordinator | 低 | 低 |
| 2 | H2: 架构测试补全 | 低 | 低 |
| 3 | H1: Desktop.Registration 模块隔离 | 中 | 中 |
| 4 | M1: ViewModel 拆分 | 中 | 低 |

## [O5] 成功标准

1. **模块隔离**: Desktop 模块间无直接引用（Registration 例外）
2. **架构测试**: 100% 覆盖 Desktop 项目
3. **LoginCoordinator**: 无硬编码旁路
4. **ViewModel**: 所有超过 500 行的 ViewModel 已拆分
