---
feature: fix-deep-assessment-issues
status: delivered
updated: 2026-09-17
branch: master
commits: a9d60a7b1..HEAD
---

# 修复深度评估问题（稳定项目）

## Report

**What was built** — 修复资深架构师深度评估发现的 P0+P1+P2 全部 19 项。P0：NRE Bug 修复。P1：ReportsController 信封统一、OperatorAccessor 安全加固、TokenHashHelper 去重、EnsureCanOperate 合并、LoginCommandHandler 拆分、CommandResult ErrorCode。P2：并发异常类型化、中间件顺序、Options 标准链、IFileDialogService 解耦、Mapperly 映射、搜索前缀优化、HealthController 修复、路由统一、Obsolete 别名删除、ConcurrentDictionary、导出去伪分页。.NET 保持 8.0 LTS。

**Verification** — NuGet restore 环境问题持续，无法运行 `dotnet build`。需在能正常 restore 的环境执行构建验证。

**Journey log** —
- ComputeTokenHash 实际有 4 处重复（审查报告说 3 处），全部提取
- ReportsController 实际有 5 处裸 BadRequest（审查报告说 2 处），全部修复
- 搜索从子串匹配改前缀匹配是语义变更，Phone 搜索保留 Contains
- UserCredentialDto 和 UserDetailDto 是不同类型，LoginCommandHandler 需保持 credential 类型贯穿验证路径

## [S1] Problem

资深架构师深度评估发现 1 个 NRE Bug、8 个 P1、16 个 P2。用户要求按稳定项目目标修复，.NET 保持 8.0 LTS 不升级。

## [S2] Design

### P0 — Bug 修复
- NRE: GetAuditLogsAsync null 检查顺序

### P1 — 安全/正确性
- ReportsController 裸 BadRequest → ValidationFail
- OperatorAccessor.ParseUserRole 失败改抛异常
- ComputeTokenHash 三处重复 → 提取 Helper
- EnsureCanEdit/Delete 近重复 → 合并
- LoginCommandHandler 187 行 → 拆分
- Desktop CommandResult 增加 ErrorCode

### P2 — 代码质量
- SaveChangesAsync 并发异常类型化
- 中间件顺序 HSTS 先于 HTTPS Redirect
- Options 手动 Bind 改标准链
- ExportPdfAsync 解耦 UI
- OperationType 魔法数字
- QueryRecentAsync 走 Mapperly
- 搜索 ToLower().Contains 优化
- 导出端点去伪分页
- HealthController 修复
- FormulasController 相对路由
- Result Obsolete 别名删除
- SensitiveDataJsonConverterFactory ConcurrentDictionary
- 构造函数空检统一 ThrowIfNull

## [S3] Out of Scope

- .NET 版本升级（保持 8.0 LTS）
- IMedicalCaseRepository 拆分（需独立设计）
- MedicalCaseCommandService 依赖收敛（需独立设计）
- Idempotency-Key（需独立设计）
- CatalogEntityCommandHandlerBase 收敛（需独立设计）

## Tasks
- [x] T1: P0 Bug + P1 安全/正确性修复 (covers: S2)
- [x] T2: P2 代码质量修复 (covers: S2)
- [ ] T3: 全量构建验证 — NuGet 环境问题阻塞 (depends: T1, T2)
