# A-27 技术栈减法执行报告

> 执行：Mimo Code｜日期：2026-08-08｜基线：`47f3d7058`（A-26 报告）
> 任务书：`docs/compose/specs/task-a27-stack-subtraction-2026-08-08.md`
> 用户决策：方案 A（2026-08-08 拍板）——先删死重量（零风险）

---

## 1. 执行摘要

按任务书删除/评估 4 个死重量依赖候选，并将 `PasswordHelper` 收敛为纯工具类（哈希验证唯一走 Identity UserManager / PBKDF2）：

| 依赖 | 任务书判断 | 实际处置 | 结果 |
|------|-----------|---------|------|
| BCrypt.Net-Next | 删除 | ✅ **删除**（3 处包条目 + 5 个方法 + 2 个类型 + 4 个常量） | PasswordHelper 609→367 行 |
| Swagger（Swashbuckle） | 核实后选择，倾向保留 | ➡️ **评估保留**（完整接线，见 §3） | 未改代码 |
| Velopack | 移除（若存在） | ✅ **未引入**（全仓 0 引用，无包条目） | 无需动作 |
| Sqlite | 移除 | ✅ **删除**（Directory.Packages.props 2 条目） | 无 csproj 引用 |

---

## 2. 任务 1：统一密码处理方案（设计收敛）

**设计 SSOT（收敛后）**：密码哈希/验证唯一入口 = Identity `UserManager`（`CheckPasswordAsync`/`ResetPasswordAsync`，PBKDF2）；`PasswordHelper` 瘦身为纯工具类（生成/策略，与哈希无关）。

### 2.1 动作明细

| # | 动作 | 文件 | 状态 |
|---|------|------|------|
| 1 | 移除 `BCrypt.Net-Next` PackageReference | `src/Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj` | ✅ |
| 2 | 移除 `BCrypt.Net-Next` PackageReference | `src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj` | ✅ |
| 3 | 移除 `BCrypt.Net-Next` 版本条目 | `Directory.Packages.props` | ✅ |
| 4 | 删除方法 `HashPassword` / `VerifyPassword` / `VerifyAndRehashIfNeeded` / `UpdateWorkFactor` / `GetConfiguration` | `PasswordHelper.cs` | ✅ |
| 5 | 删除属性 `WorkFactor` + 常量 `DefaultWorkFactor` / `MinWorkFactor` / `MaxWorkFactor` | `PasswordHelper.cs` | ✅ |
| 6 | 删除类型 `PasswordVerificationResult` / `PasswordHelperConfiguration`（仅被删方法用） | `PasswordHelper.cs` | ✅ |
| 7 | 保留 `GenerateSecurePassword`（4 重载）/ `ValidatePassword` / `CheckPasswordStrength` / `IsCommonPassword` / `GenerateTemporaryPassword` / `GenerateSalt` / `SecureEquals` / `PasswordStrength` 枚举 / `PasswordValidationResult` | `PasswordHelper.cs` | ✅ |
| 8 | 删除空 region「配置管理」+ 未使用的 `using Microsoft.Extensions.Logging` | `PasswordHelper.cs` | ✅ |
| 9 | 类头注释改为「纯密码工具类（生成/策略）——哈希与验证统一走 Identity UserManager（PBKDF2），本类不提供哈希方法」 | `PasswordHelper.cs` | ✅ |
| 10 | `PasswordPolicyValidator` 不动（独立类，实际在用） | — | ✅ |
| 11 | 历史注释保留：`DatabaseInitializationService.cs:191` / `IUserCrossModuleService.cs:34` / Tools `Program.cs:126-127`（任务书 action 6） | — | ✅ 未动 |

### 2.2 范围偏差说明（强制必要）

任务书硬性约束「0 错误 0 警告」要求 `dotnet build LYBTZYZS.sln --no-incremental`，而 3 个测试项目在 sln 内。测试项目引用了被删除的 BCrypt 代码/包，不处理则构建失败，故对测试做**最小必要**同步（非扩scope）：

1. `tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj`：移除 `BCrypt.Net-Next` PackageReference（否则 NU1010 还原错误）
2. `tests/LYBT.Tests.Server/Unit/Utilities/Utilities/PasswordHelperTests.cs`：删除 27 个 BCrypt 专用测试（测试已删除方法），保留 30 个纯工具测试（SecureEquals/ValidatePassword/CheckPasswordStrength/IsCommonPassword/GenerateSecurePassword/GenerateTemporaryPassword/GenerateSalt）；同时删除随之无用的 `_logger` 字段与 Logging using
3. `tests/LYBT.Tests.Server/Unit/Infrastructure/DatabaseInitializationServiceTests.cs`：5 处 `BCrypt.Net.BCrypt.HashPassword(...)` 替换为 `new PasswordHasher<ApplicationUser>().HashPassword(...)`（**与生产一致的 Identity PBKDF2**），新增私有辅助方法 `HashPassword`

其余测试/文档未动（`tests/LYBT.Tests.Desktop/_Infrastructure/TestDataFactory.cs:68` 的 `$2a$11$...` 占位字符串为纯字面量，不影响构建，按「排除 tests」范围保留）。

### 2.3 任务 1 验证

- ✅ `dotnet build LYBTZYZS.sln --no-incremental`：**0 错误 0 警告**
- ✅ `rg "PasswordHelper\.(HashPassword|VerifyPassword)" src`：**0 命中**
- ✅ `rg "BCrypt" src -g "*.cs" -g "*.csproj" -g "!Tools/**"`：仅 3 处**任务书要求保留的历史注释**（DatabaseInitializationService.cs:191、IUserCrossModuleService.cs:34、Tools Program.cs:126-127）——无任何代码/包残留
- ✅ 定向测试：PasswordHelperTests + DatabaseInitializationServiceTests **65/65 通过**

---

## 3. 任务 2：Swagger 评估（✅ 保留）

**核实结论：Swagger 已完整接线，非半成品**，按任务书 action 1 保留，未改任何代码：

| 接线点 | 位置 | 状态 |
|--------|------|------|
| `AddEndpointsApiExplorer()` + `AddSwaggerGen(...)`（含 SwaggerOptions 强类型绑定 + JWT Bearer security definition） | `ApiServiceCollectionExtensions.cs:76-114` | ✅ |
| `ConfigureSwaggerMiddleware()` 被调用 | `UnifiedMiddlewareConfiguration.cs:130` | ✅ |
| `UseSwagger()` + `UseSwaggerUI()`（`!app.Environment.IsProduction()` 门控——标准做法） | `UnifiedMiddlewareConfiguration.cs:185-200` | ✅ |
| `SwaggerOptions` 配置注册 | `ServerConfigurationExtensions.cs:76-77` + `SwaggerOptions.cs` | ✅ |
| Swagger 配置写保护 | `ConfigurationWritePolicy.cs:25` | ✅ |

**判断**：注册 + 中间件 + 配置三件套齐全，保留成本≈0，B-16（API 文档）未来可直接使用。蓝图 §0.5.5 标注为「评估保留」。

---

## 4. 任务 3：Velopack（✅ 未引入，无需动作）

- `rg "Velopack" src -g "*.csproj"`：**0 命中**；`Directory.Packages.props`：**0 命中**
- B-09（自动更新）未做，全仓仅文档提及（13-project-master-plan.md / 需求文档等）
- 按任务书「若存在」——不存在条目，无删除动作；蓝图 §0.5.5 记录为「未引入」

---

## 5. 任务 4：Sqlite（✅ 已移除）

- `Directory.Packages.props:14-15`：移除 `Microsoft.EntityFrameworkCore.Sqlite` / `Microsoft.Data.Sqlite` 2 个版本条目
- 全仓无 csproj PackageReference（0 引用）；`rg "Sqlite" Directory.Packages.props src -g "*.csproj"`：**0 命中**
- 本地模式定 LocalDB（SQL Server），Sqlite 为纯残留

---

## 6. 任务 5：蓝图新增「技术栈合理性评估」章节

`docs/03-architecture/14-structure-design-blueprint.md` 新增 **§0.5**（版本 v1.1→v1.3，变更记录追加 v1.3 行）：

1. **§0.5.1 技术栈全景表**：18 项（层/技术/职责/使用量）——覆盖任务书列出的 15 项 + SQL Server/Swagger
2. **§0.5.2 评估框架 4 标准**：必要性 / 活跃度 / 替代成本 / 复杂度预算
3. **§0.5.3 核心必选 10 项（SSOT）**：.NET 8 / EF Core / WPF+Prism / ASP.NET Core / Identity(PBKDF2)+JWT / Mapperly / FluentValidation / Refit / Serilog / SignalR
4. **§0.5.4 有成本但合理的 3 项**：MediatR+Service 双轨（CQRS 经典形态）/ Prism 模块化 16 项目 / Dual-Mode 双轨
5. **§0.5.5 已移除死重量 4 项**：BCrypt（✅ 移除）/ Swagger（➡️ 评估保留）/ Velopack（✅ 未引入）/ Sqlite（✅ 移除）——均标注处置原因
6. **§0.5.6 已配置未启用**：Asp.Versioning.Mvc（v1 生效，v2 预留）
7. 评估日期 + 维护者：2026-08-08 / 技术总监

---

## 7. 硬性约束验证

| 约束 | 结果 |
|------|------|
| Surgical Changes（只改相关代码） | ✅ 4 项依赖相关 + 强制的测试编译同步（§2.2） |
| 每处删除可追溯 | ✅ 本报告 §2/§5 逐项列明 |
| `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告 | ✅ **0 错误 / 0 警告** |
| 架构测试全绿 | ✅ `tests/LYBT.Tests.Architecture` **86/86 通过** |
| 产出报告 | ✅ 本文件 |
| 单 commit + push | ✅ 见 §9 |
| 明确不做 | ✅ 未合并项目 / 未删 PasswordHashGenerator / 未修既有 bug / 未改 DatabaseInitializationService 逻辑 |

---

## 8. 变更文件清单

| 文件 | 变更 |
|------|------|
| `Directory.Packages.props` | 删 BCrypt.Net-Next(4.1.0) + EFCore.Sqlite + Data.Sqlite 条目 |
| `src/Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj` | 删 BCrypt.Net-Next 引用 |
| `src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj` | 删 BCrypt.Net-Next 引用 |
| `src/Shared/LYBT.Shared.Models/Utilities/Security/PasswordHelper.cs` | 瘦身：609→367 行，删全部 BCrypt 方法/类型/常量 |
| `tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj` | 删 BCrypt.Net-Next 引用 |
| `tests/LYBT.Tests.Server/Unit/Utilities/Utilities/PasswordHelperTests.cs` | 删 27 个 BCrypt 测试，保留 30 个纯工具测试 |
| `tests/LYBT.Tests.Server/Unit/Infrastructure/DatabaseInitializationServiceTests.cs` | 5 处 BCrypt 哈希 → Identity PasswordHasher |
| `docs/03-architecture/14-structure-design-blueprint.md` | 新增 §0.5 技术栈合理性评估 + v1.3 变更记录 |
| `docs/03-architecture/13-project-master-plan.md` | A-27 状态 🟡→✅ |
| `docs/compose/reports/a27-stack-subtraction.md` | 新增（本报告） |

---

## 9. 提交信息

```bash
git commit -m "refactor(stack): remove dead-weight deps (BCrypt/Swagger/Velopeack/Sqlite) + blueprint tech-stack assessment"
```
