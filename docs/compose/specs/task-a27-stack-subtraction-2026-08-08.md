# 任务 A-27：技术栈减法——删除死重量依赖 + 蓝图补技术栈评估

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-08
> 用户决策：方案 A（2026-08-08 拍板）——先删死重量（零风险），项目合并后续再评估

## 任务

执行技术栈减法，删除 4 个死重量依赖/半成品：**BCrypt.Net / Swashbuckle(Swagger) / Velopack / Sqlite**，并在 `docs/03-architecture/14-structure-design-blueprint.md` 新增「技术栈合理性评估」章节。

## 范围

- ✅ `src/Shared` + `src/Server` + `src/Client`（代码 + csproj）
- ✅ `Directory.Packages.props`（包版本统一清单）
- ✅ `docs/03-architecture/14-structure-design-blueprint.md`（蓝图）
- ❌ 排除：`tests/`、`docs/`（除蓝图外）、`src/Tools/PasswordHashGenerator/`（运维工具，保留 BCrypt 注释说明）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线 `47f3d7058`（A-26 报告）

## 任务 1：移除 BCrypt.Net 依赖（⚠️ 部分移除，精细操作）

**背景**：`PasswordHelper`（Shared.Models/Utilities/Security/PasswordHelper.cs）混用 BCrypt 与 Identity PBKDF2，已确认 **BCrypt 哈希/验证方法零调用**（全仓 grep 无 `PasswordHelper.HashPassword/VerifyPassword` 调用者，PasswordHashGenerator 用 Identity `hasher.HashPassword`）。

**动作**：
1. 从 `LYBT.Shared.Models.csproj` + `LYBT.Module.Users.csproj` 移除 `BCrypt.Net-Next` PackageReference
2. 从 `Directory.Packages.props` 移除 `BCrypt.Net-Next` 版本条目
3. **删除** PasswordHelper 中 BCrypt 专用方法：`HashPassword`（:69）/ `VerifyPassword`（:103）/ `VerifyAndRehashIfNeeded`（:165）/ `UpdateWorkFactor`（:232）/ `GetConfiguration`（:246）/ `WorkFactor`（:44）/ `DefaultWorkFactor` / BCrypt 相关 using 和常量
4. **保留**（被真实调用）：`GenerateSecurePassword`（:383-480，ResetPasswordCommandHandler.cs:32 用）/ `ValidatePassword`（:273，PasswordPolicyValidator 相关）/ `CheckPasswordStrength`（:334）/ `IsCommonPassword`（:373）/ `GenerateTemporaryPassword`（:184）/ `GenerateSalt`（:215）/ `SecureEquals`（:493）
5. `PasswordVerificationResult` 若仅被已删方法用则一并删；被保留方法用则保留
6. 注释同步：DatabaseInitializationService.cs:191 / IUserCrossModuleService.cs:34 / PasswordHashGenerator Program.cs:126-127 的「BCrypt/PBKDF2 冲突」注释是**历史说明**，保留不动（Tools 目录除外，见范围）

**验证**：`dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告；`grep -rn "BCrypt" src --include="*.cs" --include="*.csproj"` 残留 0（Tools 目录除外）。

## 任务 2：移除 Swashbuckle(Swagger)（⚠️ 需判断，见下）

**背景**：Swagger 已在 `ApiServiceCollectionExtensions.cs:76-84` 注册 `AddSwaggerGen`，但 **B-16（Swagger API 文档）从未完成**——Swagger UI 从未对外可用，属于半成品。

**动作（二选一，按以下判断执行）**：
1. 若 `AddSwaggerGen/UseSwagger/UseSwaggerUI` 已接线完整（注册+中间件都在），**保留**（成本≈0，未来 B-16 可用）——**只在任务报告里说明现状**，不改代码
2. 若只是部分接线（如只注册未启用 UI）或存在编译警告——**移除** Swashbuckle 包 + SwaggerOptions 配置 + 注册代码
3. 实际情况请核实后选择，**倾向保留**（Swagger 对 API 开发有真实价值，注册已就绪）

**涉及文件（核实用）**：`ApiServiceCollectionExtensions.cs` / `SwaggerOptions.cs`（Shared.Configuration）/ `ConfigurationWritePolicy.cs`（含 Swagger 配置写保护）/ `UnifiedMiddlewareConfiguration.cs` / `AuthenticationServiceCollectionExtensions.cs` / `EnvironmentAwareHosting.cs` / `ServiceCollectionExtensions.cs`

**验证**：若保留——`dotnet build` 0 错误 0 警告即可；若移除——同上 + Swagger 引用 grep 残留 0。

## 任务 3：移除 Velopack 引用

**背景**：B-09（自动更新）未做，Velopack 0 引用（grep 无任何 cs 引用）。

**动作**：从全部 csproj + Directory.Packages.props 移除 Velopack 相关条目（若存在）。

**验证**：`grep -rn "Velopack" src --include="*.csproj"` 残留 0。

## 任务 4：移除 Sqlite 相关包

**背景**：项目已定 LocalDB（SQL Server）为本地模式，Sqlite 0 引用。`Microsoft.EntityFrameworkCore.Sqlite` / `Microsoft.Data.Sqlite` 出现在 Directory.Packages.props。

**动作**：从 Directory.Packages.props + 全部 csproj 移除 `Microsoft.EntityFrameworkCore.Sqlite` / `Microsoft.Data.Sqlite` 条目。

**验证**：`grep -rn "Sqlite" src --include="*.csproj"` + Directory.Packages.props 残留 0。

## 任务 5：蓝图新增「技术栈合理性评估」章节

在 `docs/03-architecture/14-structure-design-blueprint.md` §0.5（或合适位置）新增「技术栈合理性」小节，内容：

1. **技术栈全景表**：层 / 技术 / 职责 / 使用量（.NET 8 / EF Core / WPF+Prism / ASP.NET Core / MediatR / Mapperly / FluentValidation / Refit / JWT+Identity / Serilog / SignalR / CommunityToolkit.Mvvm / QuestPDF / pinyin4net / MaterialDesignThemes）
2. **评估框架 4 标准**：必要性（不可替代职责）/ 活跃度（引用数）/ 替代成本 / 复杂度预算
3. **核心必选 10 项**（无争议，标注为 SSOT）
4. **有成本但合理的 3 项**：MediatR+Service 双轨（命令走管道/查询走 Service，CQRS 经典形态）/ Prism 模块化 16 项目 / Dual-Mode 双轨
5. **已移除死重量 4 项**：BCrypt（Identity PBKDF2 取代）/ Swagger（状态按任务 2 结果写）/ Velopack（B-09 未做）/ Sqlite（LocalDB 取代）——标注删除原因
6. **已配置未启用**：Asp.Versioning（v1 生效 v2 预留）
7. **评估日期 + 维护者**：2026-08-08 / 技术总监

## 硬性约束

1. **Surgical Changes**：只改与 4 项依赖删除相关的代码，不「顺手」改其他
2. 每处删除都要可追溯（删除原因在任务报告说明）
3. **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental`
4. 架构测试：`dotnet test tests/LYBT.Tests.Architecture/` 全绿
5. 产出报告：`docs/compose/reports/a27-stack-subtraction.md`（含每项动作/验证结果/残留检查）
6. **单 commit + push**：`refactor(stack): remove dead-weight deps (BCrypt/Swagger/Velopeack/Sqlite) + blueprint tech-stack assessment`

## 明确不做（防发散）

- ❌ 不合并任何项目（Shared 5→3 等后续再评估）
- ❌ 不删 PasswordHashGenerator 工具（运维用）
- ❌ 不修 BCrypt 相关既有 bug（只删死代码）
- ❌ 不改 DatabaseInitializationService 的 Identity 逻辑
