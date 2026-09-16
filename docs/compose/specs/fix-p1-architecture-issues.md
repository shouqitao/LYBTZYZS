---
feature: fix-p1-architecture-issues
status: delivered
updated: 2026-09-16
branch: master
commits: e42c5e477..HEAD
---

# 修复 P1 架构审查问题

## Report

**What was built** — 修复了架构审查发现的全部 10 个 P1 严重问题：(1) DoctorOnly 策略文档对齐（产品文档是 SSOT，代码正确，改安全架构文档）；(2) 删除表演性 LYBT.Tests.E2E 项目；(3) 补全 11 个 ErrorCode→HTTP 映射；(4) 修复 SensitiveDataLoggerProvider 死代码/双重输出/BeginScope null；(5) 解除 Entities→ExceptionHandling 反向依赖；(6) 删除 Server 空转缓存基建（保留 ICacheInvalidationService）；(7) 补 Local OnTokenValidated 禁用用户拦截；(8) 修复 ModuleLazyLoader 视图→模块映射（10 处修正）；(9) ClinicalModule 改 OnDemand 恢复懒加载；(10) 扩展双端策略一致性架构测试至全树。

**Verification** — `dotnet build --no-restore --no-incremental`：0 错误 0 警告；`dotnet test tests/LYBT.Tests.Architecture/`：100/100 通过（含新增全树策略比对测试）；`dotnet test tests/LYBT.Tests.Server/`：828/828 通过。

**Journey log** —
- NuGet restore 在当前环境有预存问题（"Value cannot be null. Parameter 'path1'"），使用 `--no-restore` 绕过；包已在本地缓存中
- 子代理 bash 权限受限（action=ask），目录删除和构建需父代理接手
- ModuleLazyLoader 映射修正发现 ClinicalModule 实际注册了全部 9 个导航视图（含薄包装管理页），Patients/Catalog/Users 模块不注册导航视图

## [S1] Problem

全项目架构审查发现 10 个 P1 严重问题，影响安全、正确性和架构一致性。

## [S2] Design

### P1-1 DoctorOnly 策略文档对齐
产品权限文档 `04-permissions.md:41,50` 是 SSOT：医案创建/处方打印 = Doctor **唯一**。代码 `RequireRole(Doctor)` 是正确的。需要修正 `09-security-architecture.md:121,140` 中错误的 `RequireRole("SuperAdmin", "Admin", "Doctor")`。

### P1-2 删除表演性 E2E 测试
`LYBT.Tests.E2E` 项目 8 个用例全是 `Task.Delay` + 恒真断言，提供虚假安全信号。Desktop E2E 基建已覆盖同等场景。直接删除项目。

### P1-3 Server 缓存空转
`AddOutputCache()` + `UseResponseCaching()` + `CacheInvalidationService` 已注册但全仓 0 处 `[OutputCache]`。二选一：加生产者或删基建。选择删除空转基建（避免假象）。

### P1-4 Local 无禁用用户令牌拦截
对齐 Remote，补 `OnTokenValidated` 查库拦截。

### P1-5 ErrorCode→HTTP 映射遗漏
补全 11 个缺失映射：业务规则违反→422、未删除→409、批量项不存在→404。

### P1-6 Entities 反向依赖 ExceptionHandling
Registration.Complete() 不抛异常，改由 Service 层校验（与 MedicalCase.Complete() 对齐）。

### P1-7 SensitiveDataLoggerProvider 修复
删除死代码循环；避免双重输出；BeginScope 委托真实 scope。

### P1-8 ModuleLazyLoader 映射修复
修正 ViewToModuleMap 使每个视图映射到实际注册它的模块。

### P1-9 双端策略一致性架构测试
扩展至全树：反射枚举 Remote/Local 同名控制器全部 `[Authorize]` 逐方法比对。

### P1-10 ClinicalModule 懒加载
将 ClinicalModule 改为 OnDemand（依赖 P1-8 先修复映射）。

## [S3] Out of Scope

- P2/P3 问题（后续批次）
- MedicalCases CQRS 化（中期批次）
- 领域事件框架（中期批次）

## Tasks
- [x] T1: P1-1 修正 09-security-architecture.md 中 DoctorOnly 策略描述 — acceptance: 文档与 04-permissions.md 一致，RequireRole 仅 Doctor (covers: S2)
- [x] T2: P1-2 删除 LYBT.Tests.E2E 项目 — acceptance: sln 中无此项目引用，solution 构建通过 (covers: S2)
- [x] T3: P1-5 补全 ErrorCode→HTTP 映射 — acceptance: 11 个遗漏码均有映射分支，非 500 (covers: S2)
- [x] T4: P1-7 修复 SensitiveDataLoggerProvider — acceptance: 无死代码、无双重输出、BeginScope 非 null (covers: S2)
- [x] T5: P1-6 解除 Entities→ExceptionHandling 依赖 — acceptance: LYBT.Entities.csproj 无 ExceptionHandling 引用，Registration.Complete 不抛 BusinessException (covers: S2)
- [x] T6: P1-3 删除 Server 空转缓存基建 — acceptance: 移除 AddOutputCache/UseResponseCaching 空转代码，保留 ICacheInvalidationService (covers: S2)
- [x] T7: P1-4 补 Local OnTokenValidated 禁用拦截 — acceptance: Local 模式禁用用户令牌被拦截 (covers: S2)
- [x] T8: P1-8 修复 ModuleLazyLoader 映射 — acceptance: 每个视图映射到实际注册它的模块 (covers: S2)
- [x] T9: P1-10 ClinicalModule 改 OnDemand — acceptance: ClinicalModule 为 OnDemand，启动时不再强制拉起 4 个依赖模块 (covers: S2; depends: T8)
- [x] T10: P1-9 扩展双端策略一致性架构测试 — acceptance: 反射比对全部控制器 [Authorize] 策略 (covers: S2)
- [x] T11: 全量构建验证 — acceptance: dotnet build --no-incremental 0 错误 0 警告 (depends: T1..T10)
