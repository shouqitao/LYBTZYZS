# 命名空间一致性独立实测验证（2026-08-07，Mimo Code 独立分析）

> 验证人：Mimo Code（独立分析者，与 `namespace-consistency-audit-2026-08-07.md` 技术总监报告交叉验证）
> 方法：只读分析 + 1 处临时试验（已 `git checkout` 还原）；**未修改任何生产代码，未提交**
> 基线：Q-02 后状态（工作树除他人未跟踪文档外干净）

---

## 一、任务 1：Formula 模块 CS0118 实测（审计 §三 关键存疑点）

### 1.1 试验步骤与命令

1. 临时修改 `src/Server/Modules/LYBT.Module.Formula/Infrastructure/FormulaRepository.cs`（命名空间 `LYBT.Module.Formulas.Infrastructure`，复数）：
   - using 区加 `using LYBT.Entities.Formulas;`
   - 第 23 行返回类型 `LYBT.Entities.Formulas.Formula?` → 裸 `Formula?`
2. 全量编译：`dotnet build LYBTZYZS.sln --no-incremental`
3. 还原：`git checkout -- src/Server/Modules/LYBT.Module.Formula/Infrastructure/FormulaRepository.cs` → `git status` 干净

### 1.2 实测输出（原文摘录）

```
EXITCODE=1
FormulaRepository.cs(24,23): error CS0118: “Formula”是 命名空间，但此处被当做 类型 来使用 [LYBT.Module.Formula.csproj]
FormulaRepository.cs(11,34): error CS0738: “FormulaRepository”不实现接口成员“IFormulaRepository.GetByIdAsync(Guid, CancellationToken)”……没有“Task<Formula?>”的匹配返回类型 [LYBT.Module.Formula.csproj]
```

CS0738 为 CS0118 的级联错误；错误仅出现在 FormulaRepository.cs 一处，无其他项目错误 → 基线干净，错误由试验改动引入。

### 1.3 结论：CS0118 **真实存在** —— 审计"可能是误判"的假设被实测否定

- 审计 §三 的理论推演（"末段 `Formulas` ≠ 类型名 `Formula`，理论上不应遮蔽"）**不成立**。
- **实测根因**（错误定位 + C# 名称解析规则交叉证实）：
  - 全模块 22 个文件中，**仅 1 个文件使用单数命名空间**：`Application/Validators/FormulaBatchImportCommandValidator.cs:4` → `namespace LYBT.Module.Formula.Application.Validators;`（其余 21 文件均 `LYBT.Module.Formulas.*`）。
  - 由此 `LYBT.Module` 命名空间下存在成员命名空间 **`Formula`**。C# 简单名称解析（规范 §12.8.4）在 `LYBT.Module.Formulas.Infrastructure` 内解析裸 `Formula` 时，从最近外层命名空间逐层向外查找，在 `LYBT.Module` 命中成员 `Formula`（命名空间）即绑定 → CS0118「命名空间用作类型」。using 导入的类型仅在成员查找失败后才参与解析，故 `using LYBT.Entities.Formulas;` 无法救回。
- **推论（建议修复阶段以 build 验证）**：把 `FormulaBatchImportCommandValidator.cs` 的命名空间修正为复数（移除 `LYBT.Module` 下的 `Formula` 成员）后，裸 `Formula` 将成功解析到实体类型 —— Formula 5 文件的全名回退即可还原（对齐 Herb/Patient 惯例）。
- **对审计报告的修正**：§四.1 将单数文件定性为"个别笔误，无功能危害"**不准确** —— 它恰是 Formula CS0118 的**根因**；§三 的待实测结论由"可能是 Mimo 误判"修正为"实测为真，根因 = 单数笔误文件"。Q-02 对 Formula 5 文件的全名回退**是正确的**。

### 1.4 还原确认

试验文件已 `git checkout` 还原，`git status` 仅剩他人未跟踪文档与本报告。

---

## 二、任务 2：Registration 模块引用完整性（63 处核对）

### 2.1 命令与总数

```
rg -n "LYBT\.Module\.Registration"  src -g "*.cs" -g "!**/obj/**"  → 63（含复数前缀的超集）
rg -n "LYBT\.Module\.Registrations" src -g "*.cs" -g "!**/obj/**"  →  0
精确单数 = 63 − 0 = 63 ✅
```

**结论：技术总监"63 处"数字准确，确认无误。** 唯一修正：其中 **3 处位于 `src/Client/Desktop/LocalWebAPI`**（Desktop 侧宿主 LocalWebAPI 引用 Server 模块，ADR-0010 特许），严格表述应为「src 全域 63 处 = Server 60 + Desktop LocalWebAPI 3」，而非纯 Server 63。

### 2.2 引用清单（按文件，src 全域 63 处）

| 引用数 | 文件 |
|---|---|
| 8 | `src/Server/Modules/LYBT.Module.Registration/RegistrationModule.cs`（7 条 using + 1 命名空间声明；`AddScoped`/`AddDbContext`/MediatR 注册的类型均经这些 using 解析）|
| 4 | `src/Server/Modules/LYBT.Module.Registration/Application/Commands/CreateRegistrationCommandHandler.cs` |
| 3 | `.../Application/Commands/CancelRegistrationCommandHandler.cs` |
| 3 | `.../Services/NotificationService.cs` |
| 3 | `.../Controllers/BaseRegistrationsController.cs` |
| 3 | `.../Application/Queries/GetWaitingQueueQueryHandler.cs` |
| 3 | `.../Application/Queries/GetRegistrationQueryHandler.cs` |
| 3 | `.../Application/Queries/GetRegistrationsQueryHandler.cs` |
| 2 | `src/Client/Desktop/LocalWebAPI/Controllers/RegistrationsController.cs`（using `Module.Registration.Application.Commands` + `.Controllers`）|
| 2 | `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs`（同结构）|
| 2 | `.../Services/RegistrationCrossModuleService.cs` |
| 2 | `.../Infrastructure/RegistrationRepository.cs` |
| 2 | `.../Application/Validators/QuickVisitCommandValidator.cs` |
| 2 | `.../Application/Validators/CreateRegistrationValidator.cs` |
| 2 | `.../Application/Commands/QuickVisitCommandHandler.cs` |
| 2 | `.../Application/Commands/StartVisitCommandHandler.cs` |
| 1 | `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs`（using）|
| 1 | `src/Server/Modules/LYBT.Module.Registration/Application/Commands/CancelRegistrationCommand.cs` |
| 1 | `.../Application/Commands/CreateRegistrationCommand.cs` |
| 1 | `.../Application/Commands/QuickVisitCommand.cs` |
| 1 | `.../Mappers/RegistrationMapper.cs` |
| 1 | `.../Interfaces/IRegistrationRepository.cs` |
| 1 | `.../Interfaces/INotificationService.cs` |
| 1 | `.../Application/Queries/GetWaitingQueueQuery.cs` |
| 1 | `.../Hubs/RegistrationHub.cs` |
| 1 | `.../Hubs/RegistrationConnectionManager.cs` |
| 1 | `.../Domain/Events/RegistrationCreatedEvent.cs` |
| 1 | `.../Domain/Events/RegistrationCancelledEvent.cs` |
| 1 | `.../Application/Commands/StartVisitCommand.cs` |
| 1 | `src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs`（using）|
| 1 | `.../Application/Queries/GetRegistrationQuery.cs` |
| 1 | `.../Application/Queries/GetRegistrationsQuery.cs` |
| 1 | `src/Server/Services/LYBT.WebAPI/Program.cs`（`:255 app.MapHub<LYBT.Module.Registration.Hubs.RegistrationHub>` 全限定）|

重点核对的引用类型均已覆盖：DI 注册（RegistrationModule.cs，8 处）、Controller 命名空间（`LYBT.Module.Registration.Controllers`，WebAPI + Desktop LocalWebAPI 各 1 文件）、跨模块引用（WebAPI `Program.cs` 全限定 Hub 映射、`ServiceCollectionExtensions.cs`、Desktop `LocalWebApiProgram.cs`）。

### 2.3 技术总监遗漏的引用点（63 之外，方案甲受影响面）

- **测试代码（.cs 引用命名空间/程序集）**：`tests/LYBT.Tests.Server/Unit/Registration/NotificationServiceTests.cs`、`RegistrationConnectionManagerTests.cs`（using `LYBT.Module.Registration.Hubs/Services`）；`tests/LYBT.Tests.Desktop/Unit/Registration/RegistrationMasterDetailViewModelTests.cs`（using `LYBT.Desktop.Registration.*`）；架构测试 5 文件见 §三。
- **项目文件**：6 个 csproj 的 `<ProjectReference>`（WebAPI、LocalWebAPI、Desktop.Shell、Desktop.Clinical、Tests.Server、Tests.Desktop）+ `LYBTZYZS.sln` 2 个条目 + `LYBT.Desktop.Registration.csproj` 的 `AssemblyName/RootNamespace/PackageId` —— **仅在项目/程序集名一并重命名时受影响**（见 §三.2 方案 B）。
- **文档**：`docs/03-architecture/03-server.md`、`01-system-overview.md`、`13-project-master-plan.md`、`decisions/0010-localwebapi-unified-service-layer.md`、模块 README/AGENTS、`src/Client/Desktop/README.md` 等。

---

## 三、任务 3：方案甲对测试的影响评估

### 3.1 关键发现：架构测试全部按「程序集名」加载，无按命名空间字符串的断言

所有 `LYBT.Module.Registration` / `LYBT.Desktop.Registration` 命中均为 **`Assembly.Load("<程序集名>")`** 或**引用程序集名比较**，程序集名 = csproj 项目名，与命名空间无关：

| 文件:行 | 内容 | 性质 |
|---|---|---|
| `tests/LYBT.Tests.Architecture/TestAssemblies.cs:26` | `Assembly.Load("LYBT.Module.Registration")` | 程序集名 |
| `tests/LYBT.Tests.Architecture/TestAssemblies.cs:46` | `Assembly.Load("LYBT.Desktop.Registration")` | 程序集名 |
| `tests/LYBT.Tests.Architecture/ArchTests.cs:630`（P07 模块隔离）| `Assembly.Load("LYBT.Module.Registration")` | 程序集名 |
| `tests/LYBT.Tests.Architecture/LocalWebApiPatternTests.cs:77`（P21 ADR-0010）| 预期列表 `"LYBT.Module.Registration"`，比较 `GetReferencedAssemblies().Name` | 引用程序集名 |
| `tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs:403`（DM01）、`:593`（DM05）| `Assembly.Load("LYBT.Desktop.Registration")` | 程序集名 |

NetArchTest 断言均为**相对规则**，无绝对命名空间字符串：`ResideInNamespaceContaining("Repositories"/"Desktop"/"Infrastructure")`、`ResideInNamespaceMatching(@"^LYBT\.Module\..*")`、`DoNotHaveName("BaseRegistrationsController")`（类型名，不受命名空间变更影响）等。`ServerArchTests.cs` 中的 "Registration" 命中仅为测试方法名（`A03_Modules_Must_Have_DI_Registration`）与类型名规则，不受影响。

### 3.2 影响判定（取决于实施方案）

| 实施方案 | 架构测试 | 其他测试 | 备注 |
|---|---|---|---|
| **A. 纯命名空间重命名**（推荐：csproj/程序集名/文件夹名不变，仅改 `namespace`/`using`/全限定）| **零改动** —— `Assembly.Load("LYBT.Module.Registration")` 按程序集名解析，dll 名不变，P07/P21/DM01/DM05 继续通过 | 需同步改 using 的 3 个文件：<br>• `tests/LYBT.Tests.Server/Unit/Registration/NotificationServiceTests.cs`（2 条 using）<br>• `tests/LYBT.Tests.Server/Unit/Registration/RegistrationConnectionManagerTests.cs`（2 条 using）<br>• `tests/LYBT.Tests.Desktop/Unit/Registration/RegistrationMasterDetailViewModelTests.cs`（3 条 using）| 改动面最小 |
| B. 项目/程序集一并重命名（文件夹 + csproj 改名）| **6 处全部断链**：TestAssemblies 2 + ArchTests 1 + DesktopLayerArchTests 2 + P21 预期列表 1，必须同步改 | 另需改 6 个 csproj ProjectReference + sln + Desktop csproj 内 AssemblyName/RootNamespace + 文档 | 风险显著增大，**不推荐** |

### 3.3 Desktop 端命名空间改动面

`LYBT.Desktop.Registration` 在 `src/Client` 的 .cs 中共 **16 处 / 11 文件**（7 个命名空间声明：`Registration` 根 + Dialogs/Events/Repositories/Services/ViewModels/Views；其余为 using/全限定；审计"Desktop 7 命名空间"即指 7 个声明）。

### 3.4 结论

方案甲若按**纯命名空间重命名**执行：**架构测试 83 条无需任何修改**；需同步改的测试仅 3 个文件的 using 指令（2 Server 单元 + 1 Desktop 单元，共 7 条 using）。方案 B（项目改名）会破坏 6 处架构测试断链点，成本与风险成倍上升，应避免。

---

## 四、独立结论：是否同意方案甲（Registration 改复数治本）

**同意方案甲方向，附 3 条修正建议。**

1. **方案甲治本成立**：Registration 的 CS0118 是实测确认的真实功能危害（迫使 8 文件全名），改复数后模块末段 `Registrations` ≠ 实体类型名 `Registration`，遮蔽消除，8 文件可还原裸 `Registration`（对齐 Herb/Patient）。纯命名空间重命名版的改动面清晰可控：src 63 处 + 测试 3 文件 using；架构测试零改动；ROI 高。
2. **修正建议一（Formula 根因必须一并处理）**：审计 §四.1 的单数笔误文件 `FormulaBatchImportCommandValidator.cs` 是 **Formula CS0118 的根因**，不是"无危害笔误"。方案甲应同时将其命名空间修正为复数 —— 之后 Formula 5 文件即可还原裸 `Formula`（方案乙的还原动作也只有在此修正后才能安全执行）。
3. **修正建议二（严格限定改名边界）**：明确为**命名空间级**重命名，不碰 csproj/程序集名/文件夹名，否则触发 §三.2 的 6 处架构测试断链 + 6 csproj + sln，风险倍增。
4. **风险提示**：P21 测试依赖 ADR-0010 引用清单与 LocalWebAPI 实际引用一致；纯命名空间重命名不改变引用关系，安全。实施后验证链：`dotnet build --no-incremental`（0 错误 0 警告）+ 架构测试全绿 + Server/Desktop 相关测试通过。
