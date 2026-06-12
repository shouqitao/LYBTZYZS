# 设计审查报告 - 2026 Q2

**状态**: 待处理
**日期**: 2026-05-04
**版本**: v1.1.0
**审查范围**: 已有功能的设计不足、缺失或矛盾

## 摘要

本报告识别了 v1.1.0 版本中 8 类设计问题，包括 ADR 违规、API 文档不一致、模块结构不一致等。按优先级分为高/中/低三档。

---

## 🔴 高优先级问题

### 问题 1：ADR-0004 违规 - 用户上下文传播不一致

**违反规则**: ADR-0004 明确禁止 Service 层注入 `IHttpContextAccessor`

**违规代码**:

| 文件 | 行号 | 违规点 |
|------|------|--------|
| `UserService.cs` | L33, L46 | 直接注入 IHttpContextAccessor，实现 GetCurrentUserRole()/GetCurrentUserId() |
| `UserStatusService.cs` | L21, L29 | 直接注入 IHttpContextAccessor |
| `UserBatchOperationService.cs` | L22, L28 | 直接注入 IHttpContextAccessor |
| `SecurityAuditService.cs` | L17, L22-26 | 直接注入 IHttpContextAccessor |

**影响**:
- 违反依赖倒置原则
- 无法在 LocalWebAPI 模式下复用 Service 层
- 单元测试困难（需要 mock HttpContext）

**修复方案**:
```csharp
// 当前（违规）
public class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public UserService(IHttpContextAccessor httpContextAccessor) { ... }
    private Guid GetCurrentUserId() => Guid.Parse(_httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);
}

// 修复后
public class UserService : IUserService
{
    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId)
    {
        // userId 由 Controller 层通过 GetOperator() 提取并传入
    }
}
```

**修复优先级**: 🔴 高
**预计影响**: 4 个 Service 文件，~20 个方法签名

---

### 问题 2：ADR-0003 违规 - 测试策略不一致

**违反规则**: ADR-0003 规定 Server 测试使用真实 SQL Server + Respawn

**违规代码**:

| 文件 | 行号 | 违规点 |
|------|------|--------|
| `HerbRepositoryTests.cs` | L23 | 使用 UseInMemoryDatabase |
| `DatabaseInitializationServiceTests.cs` | L46 | 使用 UseInMemoryDatabase |
| `TokenRevocationServiceTests.cs` | L27 | 使用 UseInMemoryDatabase |
| `SecurityAuditCleanupServiceTests.cs` | L34-35 | 使用 UseInMemoryDatabase |

**影响**:
- InMemory 数据库不支持 SQL 特性（如 RowVersion 并发控制）
- 测试结果不可靠（InMemory 与 SQL Server 行为差异）
- 违反"零 mock"原则

**修复方案**:
```csharp
// 当前（违规）
options.UseInMemoryDatabase("TestDb");

// 修复后
options.UseSqlServer(TestDatabaseConfig.GetConnectionString());
// 配合 Respawn 每测试重置
```

**修复优先级**: 🔴 高
**预计影响**: 4 个测试文件

---

### 问题 3：API 文档与实现不一致

**不一致列表**:

| 端点 | 文档 | 代码实际 | 修复建议 |
|------|------|----------|----------|
| Patients check-reference | POST | **GET** | 更新文档为 GET |
| Patients status toggle | PUT /patients/{id}/status | **POST /patients/{id}/toggle-status** | 更新文档为 POST /patients/{id}/toggle-status |
| Herbs import | POST /herbs/import (Excel) | **batch-import (JSON)** | 文档区分 Excel import（LocalWebAPI）和 JSON batch-import（Server） |

**修复优先级**: 🔴 高
**预计影响**: 3 个 API 文档文件

---

## 🟡 中优先级问题

### 问题 4：API 文档缺失

**缺失端点**:

| 模块 | 端点 | 数量 |
|------|------|------|
| Registrations | GET /registrations, GET /registrations/{id}, POST /registrations, PUT /registrations/{id}, DELETE /registrations/{id}, GET /registrations/queue, PUT /registrations/{id}/status | 7 |
| MedicalCase Print | PUT /medical-cases/{id}/print-completed, POST /medical-cases/{id}/print-logs | 2 |
| Configuration | GET /configuration, GET /configuration/{key}, POST /configuration/validate | 3 |

**修复方案**: 在 `docs/04-api-reference/` 创建 `registrations.md`，更新 `medical-cases.md` 和 `configuration.md`

**修复优先级**: 🟡 中
**预计影响**: 3 个文档文件

---

### 问题 5：模块结构不一致

**Server 模块**:

| 模块 | 问题 | 标准结构 |
|------|------|----------|
| Auth | 使用 `Models/` 而非 `Mapping/` | 应改为 `Mapping/` |
| Sync | 缺少 `Mapping/` | 补充 `Mapping/` 文件夹 |
| Registration | 缺少 `README.md` | 补充 `README.md` |

**Desktop 模块**:

| 模块 | 问题 |
|------|------|
| Auth | 仅有 `ViewModels/`/`Views/`，缺少 `Repositories/`/`Interfaces/` 等 |
| Registration | 缺少 `README.md`、`Interfaces/`、`Mappers/`、`Models/` |

**修复方案**: 逐步对齐标准结构，Auth 模块特殊性需记录在 ADR 中

**修复优先级**: 🟡 中
**预计影响**: 5 个模块目录

---

### 问题 6：本地扩展端点未文档化

**仅在 LocalWebAPI 中存在**:

| 端点 | 用途 |
|------|------|
| POST /formulas/{id}/clone | 方剂克隆 |
| GET /formulas/categories | 方剂分类列表 |
| GET /diagnostics/db-info | 数据库信息 |
| GET /diagnostics/version | 版本信息 |
| GET /diagnostics/logs/recent | 最近日志 |
| GET /patients/by-id-number | 按身份证号查询 |
| GET /medical-cases/pending | 待处理医案 |
| GET /medical-cases/by-status | 按状态查询 |

**修复方案**: 更新 `dual-mode.md`，明确"核心对等 + 本地可选扩展"架构

**修复优先级**: 🟡 中
**预计影响**: 1 个架构文档

---

## 🟢 低优先级问题

### 问题 7：双模式架构文档未反映实际

**当前文档**: `dual-mode.md` 声称 Remote 和 Local 应保持端点对等

**实际情况**: LocalWebAPI 有额外扩展端点（诊断、克隆等）

**修复方案**: 更新文档为"核心对等 + 本地可选扩展"模式，说明扩展端点的设计意图

**修复优先级**: 🟢 低
**预计影响**: 1 个架构文档

---

### 问题 8：WPF 设计器遗留文件

**问题**: `src/Client/Desktop/Modules/` 下存在 `_wpftmp.csproj` 文件

**修复方案**: 
1. 删除现有 `_wpftmp.csproj` 文件
2. 更新 `.gitignore` 添加 `*_wpftmp.csproj`

**修复优先级**: 🟢 低
**预计影响**: .gitignore + 删除临时文件

---

## 修复计划

### 阶段 1：高优先级（ADR 违规 + API 文档）
1. 修复 ADR-0004 违规（4 个 Service 文件）
2. 修复 ADR-0003 违规（4 个测试文件）
3. 修复 API 文档不一致（3 处）

### 阶段 2：中优先级（文档补充 + 结构对齐）
4. 补充缺失 API 文档（12 端点）
5. 对齐模块结构（5 模块）
6. 文档化本地扩展端点（8 端点）

### 阶段 3：低优先级（文档更新 + 清理）
7. 更新双模式架构文档
8. 清理 WPF 设计器遗留文件

---

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-05-04 | 初始设计审查报告 |
