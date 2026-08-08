# 任务 A-31-C2：异常统一设计（Shared.ExceptionHandling 完整职责）

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-08
> 依据：`docs/03-architecture/15-solution-integration-plan.md` §3.2（异常专项 B）+ `docs/compose/reports/method-audit-shared-2026-08-08.md` §3（S1 异常专项）
> 用户方针：先收敛再完善｜已授权执行 C 批次

## 任务

将 `LYBT.Shared.ExceptionHandling` 升级为异常**完整职责**项目（层次 + 处理器 + 注册扩展 + 错误码映射），消除三处独立实现分叉；删除 4 整类死类。

## 范围

- ✅ `src/Shared/LYBT.Shared.ExceptionHandling/`（7 文件现有 + 新增）
- ✅ 迁移源：`Server/Core/LYBT.Infrastructure/ExceptionHandling/SystemExceptionHandler.cs` + `BusinessExceptionHandler.cs`
- ✅ 调用方改造：`ApiServiceCollectionExtensions.cs:73-74`（AddExceptionHandler 注册点）
- ✅ 删除：`Shared.ExceptionHandling/Exceptions/Business/ConflictException.cs` + `ApiException.cs` + `Exceptions/Security/UnauthorizedException.cs` + `Exceptions/Business/ValidationException.cs`（4 整类死类，37 方法，S1 符号级确认生产零构造）
- ✅ `ErrorCodeExtensions`（Shared.Models/Primitives/ErrorCodes/）映射 SSOT 化
- ❌ 排除：Desktop 异常处理（ClientErrorMessageMapper/DesktopExceptionHandler——评估但不强制迁移，见下）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线（C-1 完成后）

## 现状证据（S1 §3）

1. **异常→HTTP 映射三处独立实现分叉**：
   - `ErrorCodeExtensions.ToHttpStatusCode`（Shared.Models，28+9+12+11+3+22 枚举覆盖）
   - 子类 `GetHttpStatusCode` 硬编码常量（绕过 ErrorCode 枚举映射，422/429 被吞）
   - Desktop `ClientErrorMessageMapper`（Foundation）
2. **处理器分裂**：Server `SystemExceptionHandler`（192 行）+ `BusinessExceptionHandler`（82 行）在 **Infrastructure**，不在 ExceptionHandling 项目
3. **4 整类死类**：ConflictException/ApiException/UnauthorizedException/ValidationException 生产零构造（仅定义+继承测试），`ValidationBehavior.cs:35` 实为 FluentValidation 异常非 ValidationException

## 动作

### 1. 处理器收敛（E1-E3）
- E1：`SystemExceptionHandler` → `Shared.ExceptionHandling/Handlers/`（保留 IExceptionHandler 实现；注意其引用 `ErrorCodeExtensions`/`ILogger`/`CorrelationId`——评估依赖，HttpContext 相关保持 ASP.NET Core 依赖）
- E2：`BusinessExceptionHandler` → 同上
- E3：`ApiServiceCollectionExtensions.cs:73-74` 改调 `AddLybtExceptionHandling(IServiceCollection)` 扩展（由 ExceptionHandling 提供）

### 2. 映射 SSOT 化
- `ErrorCodeExtensions.ToHttpStatusCode` 为**唯一**异常→HTTP 映射源
- 删除子类 `GetHttpStatusCode` 硬编码分支（统一走 ErrorCodeExtensions）
- 保留 422/429 语义（S1 发现被吞——ErrorCodeExtensions 若有对应枚举则恢复，无则补）

### 3. 死类删除（4 类 37 方法）
- 删除 `ConflictException.cs`/`ApiException.cs`/`UnauthorizedException.cs`/`ValidationException.cs`
- 全仓 grep 确认零引用（含测试；若测试引用则同步删测试）
- **若用户决策为"保留"**：跳过本步，报告说明（决策点 §六-1 默认删除）

### 4. Desktop 侧评估（不强迁）
- `ClientErrorMessageMapper`/`DesktopExceptionHandler` 评估与 Server 侧统一映射策略的可行性
- **若统一成本低**（共用 ErrorCode→消息映射）则实施；否则**蓝图注明差异**（Desktop=UI 文案层，Server=HTTP 状态层，职责不同合理保留）

## 前置（技术引入治理）

Shared.ExceptionHandling 需补 ASP.NET Core 依赖（IExceptionHandler/HttpContext）：
- 蓝图 §0.5 技术栈表标注 ExceptionHandling 职责扩展
- 架构测试 P05b 豁免清单同步（ExceptionHandling 例外）
- **先改文档再改代码**（T2 流程）

## 硬性约束

1. **Surgical Changes**：只改异常相关文件
2. **文档先行**：蓝图 §0.5 + P05b 豁免清单先更新
3. **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental`
4. **架构测试**：`dotnet test tests/LYBT.Tests.Architecture/` 全绿
5. 单 commit + push：`refactor(exceptions): A-31-C2 异常统一——处理器收敛 ExceptionHandling + 映射 SSOT + 死类删除`
6. 产出报告：`docs/compose/reports/a31-c2-exception-unification.md`（每项动作/验证/残留检查）

## 明确不做（防发散）

- ❌ 不碰日志（C-1 批次）
- ❌ 不执行项目合并（C-3/C-4）
- ❌ 不重写业务模块异常抛法（业务代码的 throw AppException 等不动，只统一映射层）
- ❌ 不新增异常类型（除非映射恢复 422/429 需要）
