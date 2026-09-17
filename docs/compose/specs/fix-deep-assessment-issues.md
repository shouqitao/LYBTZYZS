---
feature: fix-deep-assessment-issues
status: in-progress
updated: 2026-09-17
branch: master
commits: a9d60a7b1..HEAD
---

# 修复深度评估问题（稳定项目）

## Report

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
- [ ] T1: P0 Bug + P1 安全/正确性修复 (covers: S2)
- [ ] T2: P2 代码质量修复 (covers: S2)
- [ ] T3: 全量构建验证 (depends: T1, T2)
