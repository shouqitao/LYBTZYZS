# API 单一职能原则审查报告

| 项目 | 内容 |
|------|------|
| **审查日期** | 2026-08-14 |
| **审查范围** | Remote WebAPI（11 Controller）+ LocalWebAPI（12 Controller）全部端点 |
| **审查标准** | API 单一职能原则（5 条规则） |
| **审查依据** | P07（模块隔离）、P08（跨模块接口）、Controller 层职责定义 |

## 审查概要

| 指标 | 数值 |
|------|------|
| 总端点数（Remote + Local，含继承） | ~150 |
| 发现问题总数 | 19 |
| P0（跨模块编排/严重违反） | 1 |
| P1（Controller 含业务逻辑/隐式联动） | 13 |
| P2（DTO 过大/命名不清） | 5 |

## 正例（符合单一职能的典型实现）

| Controller | 端点 | 评价 |
|------------|------|------|
| RegistrationsController (Remote) | POST /registrations | ✅ 纯 Command dispatch，零业务逻辑 |
| RegistrationsController (Remote) | PUT /{id}/start-visit | ✅ 纯 Command dispatch |
| ReportsController (Remote) | 全部 8 个端点 | ✅ 纯 Service call → 返回结果，日期默认值是参数级非业务逻辑 |
| HealthController (Remote) | GET / + /ping + /details | ✅ 纯查询 + 返回，轻微状态映射可接受 |
| BaseCrudController 模板方法 | ExecuteBatchDeleteAsync / ExecuteBatchCheckReferenceAsync | ✅ 复用模板消除复制粘贴，职责清晰 |
| CatalogController (Remote) | GET herbs/import-template + formulas/import-template | ✅ 纯静态模板返回，无业务逻辑 |

## 问题清单

### P0 — 跨模块编排 / 严重违反

| # | Controller | 端点 | 问题描述 | 建议修复 |
|---|-----------|------|---------|---------|
| P0-1 | Local ReportsController | GET /daily/income, /daily/consultations, /daily/herbs | **直接注入 IReportRepository 并在 Controller 内组装 DTO**（L16-21, L32-42）。Remote 端使用 IReportService，Local 端跳过 Service 层直接调 Repository，且在 Controller 内做了多步查询聚合（如 daily/income 调两次 Repository 再拼加法）。违反三层架构和单一职能。 | 1. Local ReportsController 改为注入 `IReportService`（与 Remote 一致）；2. DTO 组装逻辑移入 Service 层 |

### P1 — Controller 含业务逻辑 / 隐式联动

| # | Controller | 端点 | 问题描述 | 建议修复 |
|---|-----------|------|---------|---------|
| P1-1 | Remote ConfigurationController | POST /restart | **Controller 内实现重启限频逻辑**（`TryAcquireRestartSlot` 滑动窗口，L193-203）+ `Task.Run` 延迟重启（L144-148）。Rate limit 是基础设施关注点，不应在 Controller。 | 限频逻辑移入 ISystemConfigurationService 或独立的 IRestartService |
| P1-2 | Local ConfigurationController | PUT /{key}, GET /sections/{section}, PUT /sections/{section}, POST /restart | **Controller 内做角色检查**（`User.FindFirst(ClaimTypes.Role)?.Value`，L69-71 等 4 处）。类级 `[Authorize(Policy=SysAdminOnly)]` 已声明权限，Controller 内重复检查是冗余业务逻辑。 | 删除 Controller 内 role check，依赖 `[Authorize]` 属性 |
| P1-3 | Local ConfigurationController | PUT /sections/{section} | **Controller 内做白名单校验 + 逐键持久化循环**（L120-137）。白名单校验（`ConfigurationWritePolicy.IsAllowed`）和热更新判定（FeatureToggles/ClinicSettings）是业务规则。 | 白名单校验和热更新判定移入 IConfigurationStore 或 ConfigurationService |
| P1-4 | Remote DeployController | POST /upload | **Controller 内实现完整文件上传逻辑**：扩展名校验（L36-37）、目录创建（L40）、文件名生成（L42）、文件流保存（L45-46）。这是服务层职责。 | 业务逻辑移入 IDeployService |
| P1-5 | Remote DiagnosticsController | POST /logging/debug/enable | **Controller 内做业务规则校验**：durationMinutes 上限 120（L71）、level 字符串到枚举转换（L62-68）。 | 校验移入 LoggingLevelManager 或 Service |
| P1-6 | Remote DownloadController | GET / | **Controller 内实现文件系统扫描 + HTML 生成**（L41-108）。`ExtractVersion`、`BuildHtml`、`FormatSize` 三个私有方法都是业务/展示逻辑。 | 文件扫描 + HTML 生成移入 IDownloadService，Controller 仅调用并返回 Content |
| P1-7 | Remote CatalogController (Herbs) | PUT /{id}, DELETE /{id}, POST /{id}/toggle-status | **Controller 内做存在性检查 + 所有权验证**（先 GetById → ValidateOwnership → 再 Send Command）。`Update`（L235-240）、`Delete`（L267-271）、`ToggleStatus`（L294-299）三处重复。 | 存在性+所有权检查移入 CommandHandler，Controller 只负责 Send+映射结果 |
| P1-8 | Remote CatalogController (Formulas) | PUT /formulas/{id}, DELETE /formulas/{id}, POST /formulas/{id}/toggle-status | **同 P1-7，验方端点同样在 Controller 内做 Get+ValidateOwnership 再 Send Command**（L664-668, L693-697, L721-725）。 | 同上 |
| P1-9 | Remote PatientsController | PUT /{id}, DELETE /{id}, POST /{id}/toggle-status | **同 P1-7/8，患者端点也做 Get+ValidateOwnership**。`CheckOwnershipAsync` 私有方法（L427-440）封装了此逻辑。 | 所有权校验移入 CommandHandler |
| P1-10 | Remote MedicalCasesController | PUT /{id}/status | **Controller 内做状态分支路由**（L279-286）：`if (request.Status == Completed)` → CompleteAsync，否则 → UpdateStatusAsync。这是业务编排逻辑。 | 统一由 StateService.UpdateStatus 内部处理 Completed 分支 |
| P1-11 | Remote MedicalCasesController | PUT /{id}/close | **Controller 内做权限判断**（L310-311）：`if (!isAdmin) return Forbid(...)`。Class-level 策略为 DoctorOrAdmin，但 close 要求 Admin-only。这是业务授权规则。 | 改为方法级 `[Authorize(Policy=AdminOrSuperAdmin)]` |
| P1-12 | Local DiagnosticsController | GET /logs/recent | **Controller 直接注入 ISystemLogRepository**（L22, L82）。绕过 Service 层直接查 Repository，且在 Controller 内做 LINQ projection（L84-93）。 | 注入 ISystemLogService 或将查询+映射移入 Service |
| P1-13 | Local HealthController | GET /details | **Controller 直接注入 UserManager<ApplicationUser>**（L20, L67）。`_userManager.Users.Count()` 是数据访问，应通过 Service 层。 | 通过 IHealthCheckService 暴露 UserCount，移除 UserManager 直接注入 |

### P2 — DTO 过大 / 命名不清 / 重复校验

| # | Controller | 端点 | 问题描述 | 建议修复 |
|---|-----------|------|---------|---------|
| P2-1 | Remote CatalogController (Herbs) | POST /batch-enable, POST /batch-disable | **DTO 命名不匹配**：BatchEnable/BatchDisable 使用 `BatchDeleteInputDto`，语义不一致（Delete vs Enable/Disable）。 | 创建 `BatchOperationInputDto` 或复用更通用的名称 |
| P2-2 | Remote CatalogController (Formulas) | POST /formulas/batch-enable, POST /formulas/batch-disable | **同 P2-1**，验方批量启用/禁用也复用 `BatchDeleteInputDto`。 | 同上 |
| P2-3 | Remote ReportsController | GET /daily/income, /daily/consultations, /daily/herbs, /doctor-performance, /herbs/ranking | **日期参数验证逻辑重复**（5 处）：`start > end → BadRequest`。重复代码，非单一职能问题，但增加维护负担。 | 提取为 `[ApiController]` ModelValidation 或 ActionFilter |
| P2-4 | Remote MedicalCasesController | 全部端点 | **Controller 职责过重**（23 个端点含继承）：同时负责 CRUD + 状态流转 + 打印记录 + 权限查询 + 审计日志 + 患者历史。虽然每个端点是单一职责，但 Controller 类本身承载了过多领域关注点。 | 考虑拆分为 MedicalCasesController（CRUD）+ MedicalCaseProcessingController（状态流转/打印/关闭）——这是原始设计的回归建议 |
| P2-5 | Remote IdentityController | 类级 | **Auth + Users 合并 Controller**（A-31-C3a 决策）。IdentityController 路由 `/api/v1/users`，通过继承 BaseUsersController 获得 Users 端点，自身实现 Auth 端点（路由 `/api/v1/auth`）。虽然每个端点单一，但 Controller 同时服务两个不同领域（认证 vs 用户管理）。 | 保持现状（A-31-C3a 有意决策），但文档化此设计取舍 |

## 各 Controller 端点统计

### Remote WebAPI

| Controller | 端点数 | 问题数 | P0 | P1 | P2 |
|-----------|--------|--------|----|----|-----|
| CatalogController (Herbs) | 15 | 2 | 0 | 1 (P1-7) | 1 (P2-1) |
| CatalogController (Formulas) | 15 | 2 | 0 | 1 (P1-8) | 1 (P2-2) |
| ConfigurationController | 8 | 1 | 0 | 1 (P1-1) | 0 |
| DeployController | 2 | 1 | 0 | 1 (P1-4) | 0 |
| DiagnosticsController | 4 | 1 | 0 | 1 (P1-5) | 0 |
| DownloadController | 1 | 1 | 0 | 1 (P1-6) | 0 |
| HealthController | 3 | 0 | 0 | 0 | 0 |
| IdentityController | 5+10 | 0 | 0 | 0 | 1 (P2-5) |
| MedicalCasesController | 18 | 2 | 0 | 2 (P1-10, P1-11) | 1 (P2-4) |
| PatientsController | 14 | 1 | 0 | 1 (P1-9) | 0 |
| RegistrationsController | 6 | 0 | 0 | 0 | 0 |
| ReportsController | 8 | 0 | 0 | 0 | 1 (P2-3) |

### LocalWebAPI

| Controller | 端点数 | 问题数 | P0 | P1 | P2 |
|-----------|--------|--------|----|----|-----|
| AuthController | 5 | 0 | 0 | 0 | 0 |
| UsersController | 10 | 0 | 0 | 0 | 0 |
| PatientsController | 14 | 0 | 0 | 0 | 0 |
| CatalogController | 30 | 0 | 0 | 0 | 0 |
| MedicalCasesController | 15 | 0 | 0 | 0 | 0 |
| RegistrationsController | 6 | 0 | 0 | 0 | 0 |
| ConfigurationController | 8 | 2 | 0 | 2 (P1-2, P1-3) | 0 |
| HealthController | 3 | 1 | 0 | 1 (P1-13) | 0 |
| DiagnosticsController | 8 | 1 | 0 | 1 (P1-12) | 0 |
| ReportsController | 3 | 1 | 1 (P0-1) | 0 | 0 |
| DeployController | 2 | 0 | 0 | 0 | 0 |
| DownloadController | 1 | 0 | 0 | 0 | 0 |

## 建议修复优先级

### 立即修复（P0）

1. **Local ReportsController**（P0-1）：改为注入 `IReportService`，DTO 组装移入 Service。预计 1-2h 工作量。

### 高优先级（P1，建议近期修复）

2. **ConfigurationController 重启限频**（P1-1）：限频逻辑移入 Service
3. **DeployController Upload**（P1-4）：文件上传逻辑移入 Service
4. **MedicalCasesController UpdateStatus 分支路由**（P1-10）：统一到 StateService
5. **CatalogController/PatientsController Get+Ownership 模式**（P1-7/8/9）：所有权检查移入 Handler

### 低优先级（P2，择机优化）

6. BatchDeleteInputDto 复用问题（P2-1/2）：创建通用 BatchOperationInputDto
7. ReportsController 日期校验重复（P2-3）：提取为 ActionFilter
8. MedicalCasesController 端点过多（P2-4）：评估拆分必要性

## 设计决策确认

以下是有意的设计决策，不视为违反单一职能：

| 决策 | 说明 |
|------|------|
| A-31-C3b CatalogController 合并 Herbs + Formulas | 共享 ICatalogQueryService，操作模式一致，CRUD 模板复用 |
| A-31-C3a IdentityController 合并 Auth + Users | 减少 Controller 数量，每个端点仍保持单一职责 |
| BaseMedicalCasesController 共享基类 | Remote + Local 共享状态流转/权限/审计端点实现 |
| RegistrationsController QuickVisit 两步拆分 | POST /registrations + PUT /start-visit = 显式两步，符合单一职能正例 |

---

> **结论**：项目整体 API 设计质量良好，大多数端点遵循「单一职责」原则。主要问题集中在 Controller 层承担了本应属于 Service 层的业务逻辑（存在性检查、所有权验证、文件操作、限频），以及 Local ReportsController 跳过 Service 层直接调用 Repository（唯一 P0）。修复工作量可控，建议优先处理 P0，P1 按迭代逐步清理。
