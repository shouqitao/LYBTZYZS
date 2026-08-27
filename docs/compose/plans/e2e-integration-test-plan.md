# E2E 集成测试计划

## 背景

VM 交互实现全部完成（P1-P5，53 个 ViewModel），但当前测试体系存在断层：

- **Unit tests**（212 个）：用 NSubstitute mock 测 VM 逻辑 → 验证的是"代码正确"
- **LocalApi tests**（24 个）：用 WebApplicationFactory 测 API 端点 → 验证的是"端点正确"
- **缺失**：VM↔API 真实调用的端到端链路 → 验证的是"用户场景走通"

## 目标

按 User Story 模式，验证 **ViewModel → IApiClient → API → DB** 全链路，覆盖 4 个角色的核心业务流程。

## 测试架构

```
tests/LYBT.Tests.Desktop/Integration/E2E/
├── Infrastructure/
│   ├── E2ETestBase.cs              — 基类（WebApplicationFactory + LocalDB + VM DI）
│   └── E2ECollectionFixture.cs     — 集合定义（禁用并行）
├── AuthFlow/
│   ├── LoginLogoutE2ETests.cs      — 登录→获取Token→注销 全链路
│   └── TokenRefreshE2ETests.cs     — Token过期→刷新 全链路
├── PatientFlow/
│   ├── PatientCrudE2ETests.cs      — 创建→查询→更新→删除 患者
│   └── PatientSearchE2ETests.cs    — 搜索→筛选→分页
├── RegistrationFlow/
│   ├── RegistrationE2ETests.cs     — 挂号→查询→状态流转
│   └── RegistrationPermissionE2ETests.cs — 权限边界
├── MedicalCaseFlow/
│   ├── MedicalCaseE2ETests.cs      — 创建医案→诊断→处方→完成
│   ├── PrescriptionE2ETests.cs     — 处方编辑→保存→打印
│   └── ConsultationE2ETests.cs     — 问诊记录→更新→历史
├── HerbFormulaFlow/
│   ├── HerbCrudE2ETests.cs         — 药材CRUD全链路
│   └── FormulaCrudE2ETests.cs      — 验方CRUD全链路
├── AdminFlow/
│   ├── UserManagementE2ETests.cs   — 用户管理CRUD
│   └── ConfigurationE2ETests.cs    — 系统配置读写
└── SysadminFlow/
    ├── BackupE2ETests.cs           — 备份操作
    └── DiagnosticsE2ETests.cs      — 系统诊断
```

## 基类设计

### E2ETestBase.cs

核心思路：**不 mock 任何东西**，用 WebApplicationFactory 启动真实 API，用真实 LocalDB，VM 层通过 DI 注入真实 IApiClient。

```csharp
public abstract class E2ETestBase : IAsyncLifetime
{
    // LocalDB 连接串（每次测试独立数据库）
    private readonly string _dbName = $"LYBTZYZS_E2E_{Guid.NewGuid():N}";
    
    // WebApplicationFactory 启动真实 API
    private WebApplication? _app;
    protected HttpClient Client { get; }
    
    // VM 实例（通过 DI 获取）
    protected IApiClientAuth AuthApi { get; }
    protected IApiClientPatients PatientApi { get; }
    protected IApiClientHerbs HerbApi { get; }
    // ... 其他 API Client
    
    // Token 管理
    protected string? CurrentToken { get; }
    
    public async Task InitializeAsync()
    {
        // 1. 启动 WebApplicationFactory + LocalDB
        // 2. 种子数据（角色 + 管理员）
        // 3. 通过 DI 获取真实 IApiClient 实例
        // 4. 登录获取 Token
    }
}
```

### 关键约束

1. **独立数据库**：每个测试类使用独立 LocalDB（`EnsureCreated` + `EnsureDeleted`）
2. **真实 API Client**：不 mock，用 WebApplicationFactory 内存管道 + Refit
3. **种子数据**：sysadmin/admin/doctor/receptionist 4 个角色用户
4. **数据隔离**：UniqueUsername/UniquePhone 保证不冲突
5. **权限验证**：每个角色测试其可见端点，403 作为断言

## 测试覆盖矩阵

| 用户故事 | 测试文件 | 覆盖内容 |
|----------|----------|----------|
| US-AUTH-001 登录 | LoginLogoutE2ETests | 登录→Token→Claims→注销 |
| US-AUTH-002 Token刷新 | TokenRefreshE2ETests | 过期→刷新→继续使用 |
| US-PAT-001 患者列表 | PatientCrudE2ETests | 创建→分页→详情→更新 |
| US-PAT-002 患者搜索 | PatientSearchE2ETests | 姓名/手机号/身份证搜索 |
| US-REG-001 挂号 | RegistrationE2ETests | 创建挂号→查询→状态 |
| US-REG-002 挂号权限 | RegistrationPermissionE2ETests | 前台可创建/医生只读 |
| US-MC-001 医案创建 | MedicalCaseE2ETests | 创建→诊断→处方→完成 |
| US-MC-007 处方保存 | PrescriptionE2ETests | 添加药材→保存→查询 |
| US-MC-008 问诊记录 | ConsultationE2ETests | 主诉→现病史→舌脉→辨证 |
| US-HERB-001 药材管理 | HerbCrudE2ETests | CRUD + 搜索 |
| US-FORM-001 验方管理 | FormulaCrudE2ETests | CRUD + 导入 |
| US-USER-001 用户管理 | UserManagementE2ETests | 4角色CRUD |
| US-SHELL-003 系统配置 | ConfigurationE2ETests | 读写配置 |
| US-SYS-001 系统诊断 | DiagnosticsE2ETests | DB信息+版本 |
| US-SYS-002 备份 | BackupE2ETests | 备份操作 |

## 实施步骤

### Phase 1：基础设施（1天）
- 创建 `E2ETestBase.cs`（复用 LocalWebApiTestBase 模式）
- 创建 `E2ECollectionFixture.cs`
- 验证基类能启动 + 种子数据 + 登录

### Phase 2：核心业务流程（3天）
- AuthFlow：登录/注销/Token刷新
- PatientFlow：患者CRUD全链路
- RegistrationFlow：挂号全链路
- MedicalCaseFlow：医案全链路（最大最复杂）

### Phase 3：目录+管理流程（2天）
- HerbFormulaFlow：药材/验方CRUD
- AdminFlow：用户管理+配置
- SysadminFlow：备份+诊断

### Phase 4：权限+边界（1天）
- 跨角色权限验证
- 数据隔离验证
- 并发安全验证

## 预期产出

- **测试数量**：~60-80 个 E2E 测试
- **运行时间**：全量 < 5 分钟（LocalDB 内存模式）
- **覆盖角色**：4 角色 × 核心业务流程
- **验收标准**：全部通过，0 失败

## 与现有测试的关系

```
Unit Tests (212)     → 验证 VM 逻辑正确（mock）
LocalApi Tests (24)  → 验证 API 端点正确（WebApplicationFactory）
E2E Tests (60-80)    → 验证用户场景走通（VM↔API 全链路）← 新增
```

三层测试互补，不重叠：
- Unit 测 VM 内部逻辑（if/else/状态机）
- LocalApi 测端点响应（HTTP 状态码+数据格式）
- E2E 测业务流程（用户故事端到端）
