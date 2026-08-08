# 任务 A-20：DbContext 全独立（ADR-0017 落地，设计最终方案）

> 依据：产品负责人 2026-08-08 决策「从设计角度要最终方案，不考虑是否费时」+ ADR-0017（每模块独立 DbContext）
> 状态：方案设计（勘察完成，待确认后执行）
> 设计原则：模块自治是最终形态，工程量大但正确

---

## 一、目标态（ADR-0017 完全落地）

每个业务模块拥有**自己的 DbContext + 自己的迁移链**，模块间不共享 DbContext，跨模块查询走 `ICrossModuleService` 接口。

| 模块 | 目标 DbContext | 表（实体） |
|------|---------------|-----------|
| Auth | AuthDbContext（已有） | AuthSession + **SecurityAuditLog + SystemLog（新增）** |
| Users | UsersDbContext（已有） | Users(Identity) |
| Herbs | HerbsDbContext（已有）+ HerbReference | Herb |
| Formula | FormulaDbContext（已有） | Formula |
| Patients | **PatientsDbContext（新建）** | Patient |
| MedicalCase | **MedicalCaseDbContext（新建）** | MedicalCase/Consultation/Prescription/PrescriptionItem/PrintLog/AuditLog（6 表） |
| Registration | **RegistrationDbContext（新建）** | Registration |
| Reports | **保持 AppDbContext 或独立** | 只读聚合（见 §四决策点 2）|

**AppDbContext 最终**：仅保留启动/迁移基础设施用途或移除（见 §四决策点 3）。

---

## 二、勘察结论（已确认）

### 2.1 跨模块依赖是 ID 级，无跨 DbContext Join

| 依赖 | 形式 | 拆独立后影响 |
|------|------|-------------|
| MedicalCase→Patient | `m.PatientId == patientId` 单表 Where | ✅ 无碍（不 Join） |
| Registration→Patient/MedicalCase | `r.PatientId` / `r.MedicalCaseId` 单表 Where | ✅ 无碍 |
| Auth SecurityAudit | 独立，无跨模块 | ✅ |
| HerbReference | 只读 Herb，同模块 | ✅ |

**结论**：不需要跨 DbContext Join，拆独立是**纯 DbContext 切换 + 迁移拆分**，业务查询代码基本不动。

### 2.2 迁移链现状

- 唯一迁移链：`AppDbContext`（Infrastructure/Migrations，11 个迁移 + Snapshot）
- 5 个待拆模块的表都在这条链里
- 5 个已有独立 DbContext 的模块**无迁移**（表由 AppDbContext 链建）

### 2.3 现有 5 个独立 DbContext 实为「部分配置」

Auth/Users/Herbs/Formula 模块已有 DbContext 但**无迁移、无独立建表**——它们靠 AppDbContext 链建表。所以当前是「半个独立」状态。

---

## 三、执行方案（分 4 步）

### 步骤 1：新建 3 个 DbContext（Patients/MedicalCase/Registration）

每个新建 `{Module}DbContext : DbContext`：
- 只注册本模块的 DbSet（Patients: Patient；MedicalCase: 6 表；Registration: Registration）
- 复用实体配置（`OnModelCreating` 应用本模块的 `IEntityTypeConfiguration`）
- **不建新迁移**，继续共享 AppDbContext 的物理表（**同库不同 DbContext**——EF 支持，只是逻辑隔离）

> 关键认识：**「独立 DbContext」≠「独立数据库/独立表」**。当前所有模块共用同一个 SQL Server 库（LYBTDB_Dev），目标态是**同一个库、每个模块自己的 DbContext 类型**。物理表不变，只是访问对象分离。

### 步骤 2：Repository 注入切换

5 个 Repository 从注入 `AppDbContext` 改为注入各自模块 DbContext：
- PatientRepository → PatientsDbContext
- MedicalCaseRepository（含 partial 扩展）→ MedicalCaseDbContext
- RegistrationRepository → RegistrationDbContext
- SecurityAuditRepository → AuthDbContext
- HerbReferenceRepository → HerbsDbContext

### 步骤 3：迁移链处理（关键决策）

由于是**同库**，EF 迁移可以保持 AppDbContext 为「唯一迁移所有者」：
- **方案 A（推荐）**：3 个新 DbContext + 已有 5 个都不建迁移，**继续由 AppDbContext 迁移链管理物理表**（现状延续）。DbContext 独立只是逻辑层分离，物理 schema 仍单一迁移链——**简单、零迁移风险**，符合「模块自治」的代码层目标
- 方案 B（彻底）：每个模块独立迁移链（`dotnet ef migrations add` 到各自项目）——同库多迁移链会有表归属冲突（每个 DbContext 只认自己的表，会尝试 Drop 别人的表），**风险极高，不推荐同库场景**

**推荐方案 A**：DbContext 独立（代码层自治）+ AppDbContext 单迁移链（schema 层统一）。这是同库模块化单体的标准做法。

### 步骤 4：注册与验证

- 各模块 Module.cs 注册 `AddDbContext<{Module}DbContext>`（连接串与 AppDbContext 一致）
- 架构测试更新：新增断言「每个模块 Repository 注入自己的 DbContext，不注入 AppDbContext」
- 验证：build 0 错误 0 警告 + 架构测试 + 既有单测（EF InMemory 需换 DbContext 类型）

---

## 四、决策点（技术总监建议）

| # | 决策 | 建议 |
|---|------|------|
| 1 | 迁移链方案 A（AppDbContext 单一）vs B（全独立迁移） | **A**：同库多迁移链是灾难（表归属冲突），A 达成本质目标（代码层自治）|
| 2 | Reports 模块：保持 AppDbContext 或独立 | 保持 AppDbContext（只读聚合，无自有表）——若 AppDbContext 最终移除则用 PatientsDbContext 或建 ReportsDbContext |
| 3 | AppDbContext 最终去留 | 若只服务 Reports，可保留为「基础设施 DbContext」；若全拆完只剩它，移除或改名 |
| 4 | 跨模块查询（如 MedicalCase 需 Patient 名） | 走 `IMedicalCaseCrossModuleService`（已有先例）——需核对现有跨模块服务是否覆盖 |

---

## 五、工作量预估

| 项 | 预估 |
|----|------|
| 3 个 DbContext + 实体配置复用 | 0.5-1d |
| 5 个 Repository 注入切换 | 0.5d |
| 跨模块服务核对（决策点 4）| 0.5d |
| 架构测试 + 单测更新 | 0.5d |
| **合计** | **2-3d** |

---

## 六、验证

1. `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
2. `dotnet test tests/LYBT.Tests.Architecture/`：84/84 + 新断言
3. 相关模块单测（EF InMemory 换 DbContext 后全过）
4. 本地冒烟：启动 WebAPI → 患者/医案/挂号 CRUD 正常
5. 迁移链验证：`has-pending-model-changes` 无差异（方案 A 下 schema 不变）

## 七、不做

- ❌ 不拆数据库/分表（同库设计保留）
- ❌ 不做每个模块独立迁移链（方案 A 否决 B）
- ❌ 不重写业务查询（ID 级依赖不动）
