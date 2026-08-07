# 命名空间一致性审计（2026-08-07，技术总监独立分析）

> 审计人：技术总监（独立分析）｜触发：Q-02 `using` 别名清理后发现根因——模块命名空间与实体命名空间词根撞名，迫使别名/全名
> 关联：`webapi-using-alias-audit-2026-08-07.md`、`13-project-master-plan.md` §九 Q-02 行
> 工作流：本报告为第①份（技术总监独立报告），随后派发 Mimo Code 独立分析交叉验证，合并后出方案。

---

## 一、命名空间现状全景

### 1.1 Server 模块命名空间（末段单/复数）

| 模块（文件夹） | 命名空间末段 | 一致性 | 实体命名空间 | 撞名危害 |
|------|------|------|------|------|
| Auth | `Auth`（单） | 单 | `Entities.Auth`（单） | 无（末段≠类型名）|
| Formula | **`Formula`(单) + `Formulas`(复) 混用** | ❌ 不一致 | `Entities.Formulas`（复） | 待实测（见 §三）|
| Herbs | `Herbs`（复） | 复 | `Entities.Herbs`（复） | 无 |
| MedicalCase | `MedicalCases`（复） | 复 | `Entities.MedicalCases`（复） | 无（类型名 MedicalCase ≠ 末段 MedicalCases）|
| Patients | `Patients`（复） | 复 | `Entities.Patients`（复） | 无 |
| **Registration** | **`Registration`（单）** | 单 | `Entities.Registrations`（复） | **🔴 CS0118：模块末段 `Registration` == 实体类型名 `Registration`** |
| Reports | `Reports`（复） | 复 | — | 无 |
| Users | `Users`（复） | 复 | `Entities.Users`（复） | 无 |

### 1.2 实体命名空间（全部复数，一致）
`Entities.Auth/Common/Consultations/Formulas/Herbs/MedicalCases/Patients/Prescriptions/Registrations/Users` —— 全部复数，内部一致。

### 1.3 Contracts 命名空间（全部单数）
`Contracts.Auth/Common/Consultation/Formula/Health/Herbs/MedicalCase/Patients/Prescriptions/Registration/Reports/Users` —— 全部单数，与实体（复数）不一致，但**不与任何类型名撞名，无 CS0118 风险**（仅风格问题，全仓引用约 2956 处，改动成本极高，不推荐动）。

### 1.4 Desktop 模块命名空间
`LYBT.Desktop.Registration`（单数）等 7 个命名空间，与 Server 端 Registration 模块同样单数。若 Server 端 Registration 改复数，Desktop 端应同步以保持一致。

---

## 二、真正的危害点（CS0118）

C# 名称解析规则：当在命名空间 `A.B.Registration`（末段 `Registration`）内写裸类型 `Registration` 时，编译器优先将 `Registration` 解析为**外层命名空间末段 `Registration`**，而非导入的 `LYBT.Entities.Registrations.Registration` 类型 → **CS0118「命名空间不能用作类型」**。

**实测证据**（Q-02 回退后现状）：
- `RegistrationRepository.cs` 第 29/36/74/84/110/119 行全部被迫用全名 `LYBT.Entities.Registrations.Registration`。
- `IRegistrationRepository.cs` / `RegistrationMapper.cs` / `CreateRegistrationCommandHandler.cs` / `QuickVisitCommandHandler.cs` 同理（共 8 文件 Q-02 回退全名）。
- 这正是 Q-02 里 8 个文件"因 CS0118 回退全名"的成因。

**结论**：只有 **Registration 模块**（单数命名空间末段 == 实体类型名）产生真实 CS0118 危害。其他模块（Herbs/Patients/Users/MedicalCases）模块末段与实体类型名不同（如 `Herbs`≠`Herb`、`Patients`≠`Patient` 类型名是 `Patient` 但实体命名空间末段 `Patients`，裸 `Patient` 能解析到 `Entities.Patients.Patient`，不撞），故无此问题——这也解释了为何 `HerbRepository`/`PatientRepository` 能直接用裸 `Herb`/`Patient` 而无需别名。

---

## 三、Formula 模块的 CS0118 待实测（关键存疑点）

Mimo 在 Q-02 报告中称 Formula 模块 5 文件也因 CS0118 回退全名。但据命名空间规则分析：
- Formula 模块命名空间末段是 **`Formulas`（复数）**（22/23 文件），实体类型名是 **`Formula`（单数）**。
- 写裸 `Formula` 时，外层命名空间末段 `Formulas` ≠ `Formula`，**理论上不应遮蔽**类型 `Formula`。
- 即 Formula 的"全名回退"可能是 **Mimo 误判**（把 Registration 的 CS0118 套用），或另有原因（如某处 `using LYBT.Module.Formulas;` 与 `using LYBT.Entities.Formulas;` 导致末段 `Formulas` 歧义但类型名 `Formula` 仍唯一）。

**需独立实测确认**：在 `FormulaRepository.cs` 临时加 `using LYBT.Entities.Formulas;` 并将一处返回类型改为裸 `Formula` 跑 build，看是否 CS0118。若**不报错** → Formula 的全名回退是过度保守，可还原为裸 `Formula`（按 Herb/Patient 惯例）；若**报错** → 存在未知遮蔽路径，需深挖。

---

## 四、次要问题（非危害，风格/笔误）

1. **Formula 模块命名空间单/复笔误**：22 文件用 `LYBT.Module.Formulas`（复数），仅 1 文件 `FormulaBatchImportCommandValidator.cs` 用 `LYBT.Module.Formula`（单数）。属个别笔误，与 Q-02 别名无关但同属命名混乱，建议一并修正为复数（对齐 22 文件的多数）。
2. **Contracts 单数命名**：系统性不一致（2956 处引用），无功能危害，ROI 极低，**不纳入本次**。

---

## 五、影响面评估（决定 ROI）

| 改动项 | 源码引用数 | 危害 | 推荐 |
|------|------|------|------|
| Registration 模块 → 复数 `LYBT.Module.Registrations`（Server+Desktop） | 63（Server）+ Desktop 7 命名空间 | 🔴 CS0118 真实危害 | **纳入**（治本）|
| Formula 模块 1 文件笔误单数 → 复数 | 1 | 无 | 顺带修正 |
| Formula 模块全名回退还原裸 `Formula`（若实测无 CS0118） | 5 文件 | 无（仅风格） | 实测后定 |
| Contracts 2956 处单数 → 复数 | 2956 | 无 | **不纳入**（ROI 低）|

---

## 六、方案选项（待用户拍板）

### 方案甲（推荐，治本）：统一 Registration 模块命名为复数
- Server：`LYBT.Module.Registration` → `LYBT.Module.Registrations`（63 处引用，含 DI 注册/using/完全限定）；同步 Desktop `LYBT.Desktop.Registration` → `LYBT.Desktop.Registrations`（7 命名空间）。
- 效果：模块末段 `Registrations` ≠ 实体类型名 `Registration`，CS0118 消除 → `RegistrationRepository` 等 8 文件可还原裸 `Registration`（对齐 Herb/Patient），彻底告别别名/全名噪音。
- 代价：全仓级重命名（mechanical，serena rename_symbol 或精确 sed），需 build 0 警告 + 架构测试验证。
- 顺带：Formula 模块笔误 1 文件修正为复数。

### 方案乙（最小改动）：仅修 Formula 笔误 + 还原 Formula 全名（若实测可行）
- 不动 Registration（维持 Q-02 全名回退）。
- 仅 1 文件笔误修正 + Formula 5 文件还原裸 `Formula`（需实测无 CS0118）。
- 代价极小，但 **Registration 的 CS0118 噪音保留**，未治本。

> 注：Registration 的 CS0118 是真实功能危害（迫使全名），Formula 的"全名回退"待实测。若用户选方案乙，Registration 的全名噪音将长期存在。

## 七、待交叉验证（Mimo Code 独立分析任务）
1. 实测 Formula CS0118 真伪（§三 临时改 + build）。
2. 复核 Registration 63 处引用清单完整性（含 DI 注册 `RegistrationModule`）。
3. 评估方案甲重命名对架构测试（83 条）的影响（命名空间变更可能触发 P07/P08 模块边界测试）。
4. 输出独立报告，与本报告交叉合并后出最终执行方案。
