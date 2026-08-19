# Desktop 架构收敛报告（2026-08-19）

> 任务书：`.hermes-task-desktop-convergence.md`（已删除）
> 范围：C-5 机制收敛 + C-6/C-7 死代码清理 + Review 遗留问题（R1/R2）+ 文档同步
> 前置：desktop-l4-l0-cleanup-2026-08-19.md（L4-L0 清理批次）+ desktop-refactoring-review-2026-08-19.md（Review 报告）

---

## 一、机制收敛（C-5 模式）

### 1. CancelMedicalCaseAsync 恒 null bug 修复（ADR-0020 契约对齐）

**上批遗留 bug #2 根治**：`MedicalCaseRepository.CancelMedicalCaseAsync` 原实现恒返回 `null`（注释自述 "Always returns null regardless of outcome"）——即使 API 成功也返回 null，导致 `LifecycleService.CancelMedicalCaseAsync` 的 `data != null` 判断永远为假，**取消操作永远报失败**。

**根因**：方法签名 `Task<MedicalCaseDetailDto?>` 错误——`IApiClientMedicalCases.CancelMedicalCaseAsync` 返回非泛型 `ApiResponse`（仅 Success/Message，无 Data），Repository 层无法返回数据。取消是纯写操作，按 ADR-0020 契约应返回成功/失败。

**修复**（3 文件）：
| 文件 | 变更 |
|------|------|
| `IMedicalCaseRepository.cs` | 签名 `Task<MedicalCaseDetailDto?>` → `Task<bool>`（写操作契约） |
| `MedicalCaseRepository.cs` | 返回 `response.Success`（成功→true / 失败→false），保留日志，异常 rethrow |
| `MedicalCaseLifecycleService.cs` | `data != null` → `cancelled` 直接判断 |

**回归测试**（`MedicalCaseRepositoryTests` +2）：成功→true / 失败→false。

### 2. 错误处理模式核查

Service 层全量核查（~30 个 Service）——错误处理三态已收敛：
- **写操作**：`try/catch → CommandResult.Failed`（Lifecycle/Command/Report/AuditLog/CrudServiceBase）✅ 符合 ADR-0020
- **读操作**：`catch → return null/空集合`（QueryService）✅ 符合
- **基础设施**：`catch → LogError + rethrow`（Print/Repository 层）✅ 允许
- **空 catch**：全仓 4 处均为有意静默（ThemeService×2/MainWindow/App 有注释说明），保留

### 3. 命名统一核查

- `Remote*Service` 类名残留：**0**（src/Client/Desktop grep 实证——上批 R1 已清零，StringResources.resx 的 RemoteService 为 UI 文案资源非类名）
- Service 命名：5 个 CRUD Service（UserService/HerbService/FormulaService/PatientService/RegistrationService）+ MedicalCase 三服务 + AuditLog/Report 全部无前缀规范命名 ✅

---

## 二、死代码清理（C-6/C-7 模式）

### 1. MedicalCaseService 门面死成员删除（上批登记遗留项）

`MedicalCaseService.cs` 删 4 个**非接口成员**死方法（全仓零调用，含 ViewModel/测试）：
- `SetPrescriptionFlagAsync`（Repository 有对应方法，门面无消费者）
- `UpdateStatusAsync`（同上）
- `SuspendViaApiAsync`（同上）
- `DeleteMedicalCaseAsync`（同上）

同步清理 `using LYBT.Shared.Models.Enums`（删除后无引用）。

> 注意：`CloseCaseAsync` 是 `IMedicalCaseLifecycleService` 接口成员（保留），其余 4 个为门面独有非接口方法（可安全删）。

### 2. XAML 死绑定清理（上批登记 5 处坏绑定 → 本批修复 4 类 6 处）

| 位置 | 坏绑定 | 处置 |
|------|--------|------|
| `DeploymentView.xaml:38` | `StaticResource BoolToVis`（资源不存在——运行时报错/静默失效） | → `x:Static converters:Cvt.BoolToVis` + 补 converters 命名空间 |
| `RegistrationListView.xaml:46` | `QuickVisitCommand`（命令已随 QuickVisit 两步改造删除——按钮残留） | 删整个「快速就诊」按钮 |
| `UserMasterDetailControl.xaml:73-74` | `ImportCommand`/`DownloadTemplateCommand`（UserImportExportHandler 已删，命令不存在） | 删 2 个死按钮 |
| `PatientMasterDetailControl.xaml:97-114` | `ImportCommand`/`ExportCommand`/`DownloadTemplateCommand`（零命令对应） | 删 3 个死菜单项 |

**评估为有效绑定（不删）**：`Consultation`/`Prescription` 绑定（`ConsultationEditor.Consultation`/`PrescriptionEditor.Prescription` 属性存在——上批误登记为坏绑定）。

### 3. 架构守卫存量修复（A02）

`ReportRepository`（P0-2 引入）未继承 `ApiClientRepositoryBase`——A02 架构守卫违规（上批报告声称架构 87/87 实为 86/87）。修复：继承 `ApiClientRepositoryBase<object, object>` + 构造补 `ILogger<ReportRepository>`（DI 开放泛型已注册，`RegisterSingleton` 类型注册自动适配）。架构测试恢复 **87/87**。

### 4. TODO 清理（任务书「4 个 TODO」→ 实测 6 处）

| 位置 | TODO | 处置 |
|------|------|------|
| `MedicalCaseWorkspaceViewModel.cs:35` | 超大类型建议拆分（引用文档已归档） | **删**——579 行 <600 限制；薄壳+子 VM 拆分已落地；引用报告 code-review-duplicates.md 已不存在 |
| `ClinicalHomeViewModel.cs:215` | US-SHELL-005 今日统计待实现 | **保留**——功能待实现 |
| `PrescriptionItemViewModel.cs:24/33` | P1-3 BindableBase 约束 | **保留**——Mapperly 依赖已定案 |
| `ReportsModule.cs:14` | 报表待完善 | **保留**——功能待实现 |
| `Users/AGENTS.md:43` | ChangePasswordAsync placeholder | **删**——代码已完整实现（文档过时） |

### 5. 文档过时修正（Users AGENTS.md）

上批遗漏的活文档漂移——修正 5 处：
- 目录结构 `ViewModels/Components/`（UserService 实为 `Services/`）
- `IUserImportExportHandler` 引用（类已删）
- ANTI-PATTERNS 段 `IUserService dead code`/`IUserCommandHandler dead code`/`ChangePasswordAsync placeholder` 全过时（IUserService 已注册并被 VM/Handlers 引用，ChangePasswordAsync 完整实现）——删除
- `IUserDataSource` 约定（已不存在——Repository 改注入 IApiClient 子接口）
- Import/Export "Excel operations" → "JSON import/export"

---

## 三、Review 问题状态

| 问题 | 状态 |
|------|------|
| **R1**：4 模块 README/AGENTS 的 `Remote*Service` 旧名引用 | ✅ 上批已完成（本批 grep 验证零残留） |
| **R2**：13c-current-status 追加本批次条目 | ✅ 本批追加 #127 |
| 蓝图 §3.5 | ✅ 已有 ADR-0020 错误契约表（上批 Batch 1 完成），本批无新增需同步 |

---

## 四、验证

| 项 | 结果 |
|----|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | ✅ 0 错误 0 警告 |
| 架构测试 | ✅ 87/87（修复 A02 存量失败 86→87） |
| MedicalCaseRepositoryTests | ✅ 6/6（+2 新增 Cancel 回归） |
| MedicalCase 相关测试 | 59 通过 / 34 环境性失败（localhost:5000 未起——13c 存量基线一致，非本次引入） |

---

## 五、评估保留项（继续登记）

1. **空 catch 4 处**（ThemeService×2/MainWindow/App）：有意静默模式，可后续升级 Debug 日志
2. **Converter 13 个**：全部在用（x:Static Cvt 模式），无废弃
3. **Core Controls**：全部被模块引用，无废弃控件
4. **客户端侧死契约**（ValidateTokenAsync/GetValueAsync/SetValueAsync/GetPendingValidationAsync/ValidateHerbAsync/GetPermissionsAsync）：Server 端点在线，删除需与 Server 协调
5. **Repository/Service 重复错误处理模板收敛**（L1/L2 ~30 处手写 try/catch → 共享助手）：重构专项，非死代码
6. **RemoteService/RemoteServiceHelp UI 资源**：UI 文案（远程服务提示），保留
7. **TODO 5 处**（US-SHELL-005/报表待完善/P1-3 约束×2/ReportsModule）：功能待实现标记，非死代码

---

## 六、结论

本批次完成：1 个真实 bug 修复（CancelMedicalCaseAsync 恒 null——取消永远失败）+ 4 个门面死方法删除 + 6 处 XAML 死绑定清理 + 1 个架构守卫存量失败修复（86→87）+ 文档过时修正 2 文件 + TODO 过时清理 2 处。构建 0/0，架构 87/87，相关单测 6/6。**可发布**。

删除遵循「每删前确认归属」：所有删除项均经全仓 grep 零调用 + 编译器验证；公共接口面（对应 Server 端点的契约方法）一律保留并登记。
