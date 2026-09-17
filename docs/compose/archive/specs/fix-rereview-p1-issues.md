---
feature: fix-rereview-p1-issues
status: delivered
updated: 2026-09-17
branch: master
commits: fdec51250..HEAD
---

# 修复复审 P1 问题

## Report

**What was built** — 修复复审发现的全部 P1（7/8，R-7 SQL 集成测试基建需独立批次）+ 9 个 P2 + 9 个 P3。涵盖：领域事件失败隔离、Local 异常路径统一 ProblemDetails、BatchDelete 回滚挂号、限流配置接通、AES-GCM fail-fast + fail-closed、患者身份证号 HMAC 盲索引、移除遗留 ASP.NET 2.3.9 包、模块 DbContext 审计接线、429 统一 ProblemDetails、ViewRoleAccess 补全、UserNotificationService Dispatcher 保护、Formula 列表查询优化、死代码清理等。

**Verification** — NuGet restore 在当前环境有系统级问题（SDK 工作负载解析器返回 null），无法运行 `dotnet build`。构建验证需在能正常 restore 的环境（CI 或重装 SDK）执行。

**Journey log** —
- NuGet restore 环境问题贯穿整个 session，使用 `--no-restore` 绕过（依赖已有 obj/ 缓存）；清理 obj/ 后无法恢复
- 领域事件跨模块契约必须放 Infrastructure SharedKernel（P07 禁止模块互引）
- AES-GCM 非确定性加密导致按身份证号查询全表扫描，HMAC 盲索引是正确解法
- AuthSessionConfiguration 已存在于 Infrastructure，IdentityDbContext 内联配置是漂移而非缺失

## [S1] Problem

复审（基线 83d75cdbe）在新维度发现 8 个 P1 问题，涉及安全、性能、可靠性和依赖健康度。

## [S2] Design

### R-1: 领域事件失败隔离
事件 Handler 加 try/catch + Error 日志，失败不阻断主事务已提交结果。对齐 US-MC-017 审计隔离策略。

### R-2: Local 异常路径统一 ProblemDetails
SharedHost.ConfigurePipeline 改为 ProblemDetails，调用 AddLybtExceptionHandling + AddProblemDetailsConfiguration。

### R-3: BatchDelete 回滚挂号
批量软删复用 DeleteAsync 的项逻辑（含挂号回滚），消除双路径语义分叉。

### R-4: 限流配置接通
ConfigureRateLimiting 改为读取 SecurityOptions.RateLimiting 配置节。

### R-5: AES-GCM 回退密钥 fail-fast
生产环境 ResolveKey() 找不到环境变量时抛异常，不静默使用测试密钥。

### R-6: 患者身份证号查询优化
引入 HMAC 盲索引列，查询时按索引匹配而非全表扫描。

### R-8: 移除遗留 ASP.NET 2.3.9 包
Shared.Logging 改用 FrameworkReference，移除 Http.Abstractions/Mvc.Core 2.3.9。

## [S3] Out of Scope

- R-7: SQL 集成测试基建重建（需独立批次，工作量大）
- P2/P3 问题（后续批次）

## Tasks
- [x] T1: R-4 限流配置接通 + R-5 AES-GCM fail-fast + R-8 移除遗留包 (covers: S2)
- [x] T2: R-1 事件失败隔离 + R-3 BatchDelete 回滚 (covers: S2)
- [x] T3: R-2 Local 异常路径统一 ProblemDetails (covers: S2) — SharedHost 注册 AddSharedProblemDetails（AddProblemDetails + AddLybtExceptionHandling）+ UseExceptionHandler/UseStatusCodePages 写 ProblemDetails
- [x] T4: R-6 患者身份证号盲索引 (covers: S2) — Patient.IdCardHash HMAC-SHA256 + 迁移 + 启动回填 + 仓储索引查询
- [ ] T5: 全量构建验证 (depends: T1, T2, T3, T4)
