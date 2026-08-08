# Desktop 层方法级深审报告（Mimo 独立分析）

> 任务：A-30-S3｜派发：Mimo Code（只读审查）｜版本：v1.0
> 产出日期：2026-08-09｜基线 commit：`7ea542cd6`（代码树同 S0/S1/S2 基线，无代码变更）
> 任务书：`docs/compose/specs/task-a30-s3-desktop-audit-2026-08-08.md`｜依据：S0 基线（`method-audit-baseline/duplicates/dead-candidates-2026-08-08.md`）+ S1 Shared 结论（`method-audit-shared-2026-08-08.md`）+ S2 Server 结论（`method-audit-server-2026-08-08.md`）
> 性质：**只读**，未修改任何 src/tests 代码，未 commit（报告由技术总监统一提交）

---

## 0. 方法说明（工具/基线/时间点）

| 项 | 说明 |
|----|------|
| 范围 | Desktop 层 **16 项目**：Core 6（Contracts/Controls/Foundation/Infrastructure/Printing/LocalWebAPI）+ Modules 7（Auth/Users/Patients/Herbs/Formula/MedicalCase/Registrations）+ Roles 2（Admin/Clinical）+ Shell 1；方法总数 **2987**（S0 基线口径） |
| 复核方法 | 6 个并行符号级子审（VM 重复/映射/双轨/Repo/死方法×2/合并候选）+ 主审交叉验证；每发现带 `文件:行号` 证据；XAML 绑定/命令绑定/DP 回调/事件订阅/`[RelayCommand]` 源生成逐项核实 |
| 判定标准 | A=依据充分（设计依据+真实调用链）；B=支撑性（private/内部 helper）；C=重复/分散（同职责 2+ 处实现）；D=死方法（0 调用）；E=可集中（机制应集中到 Shared 或收敛点，沿用 S1/S2 判定） |
| 时间点 | S0 生成 2026-08-09 02:10；本报告符号级复核 2026-08-09（同基线代码树） |
| 已知盲区 | ① XAML 绑定/`[RelayCommand]` 源生成/DP `PropertyMetadata` 回调 grep 不可见——已对 VM/控件方法**强制查 .xaml + 特性 + 订阅点**；② `nameof`/反射字符串已逐项排除；③ Mapperly 生成代码在 `obj/` 不在扫描面，已人工核实 |

---

## 1. 16 项目方法分级总表（A/B/C/D/E 计数）

> 分级口径：C 计数单位为「组/处」（一组=2+ 方法重复），D 计数为符号（接口+实现成对计 2），E 为「应迁移机制点」；A/B 为代表性评估（子审对 C/D/E 全量符号级，A/B 抽代表性类确认）。D 级已排除全部框架反射面（`[RelayCommand]`/XAML/DP 回调/事件订阅/`IsNavigationTarget`/`ConvertBack`/Prism `RegisterTypes`/路由 action）。

| # | 项目 | 类型数 | 方法数 | A | B | C 组/处 | D 符号 | E 机制点 | 关键结论 |
|---|------|-------|-------|----|----|---------|--------|---------|---------|
| 1 | LYBT.Desktop.Contracts | 110 | 487 | ~440 | — | 3 处 | 4 | — | 接口契约层健康；双轨接口镜像（Refit IApi vs IApiClient）是设计性镜像 |
| 2 | LYBT.Desktop.Controls | 46 | 149 | ~130 | — | 2 组 | 2+2 复核 | — | 控件 DP/事件模式正确；拼音搜索逻辑与 Infrastructure 重复 |
| 3 | LYBT.Desktop.Foundation | 108 | 511 | ~470 | — | 4 组 | 7 | 3（迁移 Shared） | Service 适配器骨架 4 份同构；死方法最集中 |
| 4 | LYBT.Desktop.Infrastructure | 127 | 596 | ~555 | — | 5 组 | 7 | 2（迁移 Shared） | 基类 protected API 残留 5 项；MD 命令组双实现 |
| 5 | LYBT.Desktop.Printing | 17 | 61 | 60 | — | 0 | 1 | 0 | 自包含、无重复；仅 BatchPrintAsync 断链 |
| 6 | LYBT.LocalWebAPI | 24 | 109 | ~70 | — | 4 处内联复刻 | 0 | 1（策略补齐） | Reports 内联复刻 Service 逻辑；策略缺口（P0） |
| 7 | LYBT.Desktop.Auth | 10 | 57 | ~45 | — | 2 组 | 0 | — | TestConnection 状态机双份 ~95% 同构 |
| 8 | LYBT.Desktop.Users | 15 | 77 | ~60 | — | 3 组 | 3 | — | Editor VM 手写映射；UserItem 死类 |
| 9 | LYBT.Desktop.Patients | 19 | 73 | ~58 | — | 3 组 | 2 | — | Editor VM 手写映射；PatientItem 死类 |
| 10 | LYBT.Desktop.Herbs | 13 | 59 | ~45 | — | 4 组 | 0 | — | 与 Formula 85% 同构（合并候选核心） |
| 11 | LYBT.Desktop.Formula | 14 | 77 | ~58 | — | 4 组 | 1+3 复核 | — | 同上；OnSelfPropertyChanged 空体死代码 |
| 12 | LYBT.Desktop.MedicalCase | 55 | 240 | ~200 | — | 5 组 | 5+2 | — | Clone 双机制；ToInputDto 双链路；Save 映射未 Mapperly |
| 13 | LYBT.Desktop.Registrations | 10 | 58 | ~48 | — | 2 组 | 0 | — | 裸事件订阅泄漏；SignalR 回调正常 |
| 14 | LYBT.Desktop.Admin | 19 | 61 | ~55 | — | 2 组 | 0 | — | 导航命令重复（3 Home VM） |
| 15 | LYBT.Desktop.Clinical | 23 | 140 | ~110 | — | 6 组 | 0 | — | 读卡流程 4 处 ~85% 同构（最大单点） |
| 16 | LYBT.Desktop.Shell | 55 | 232 | ~195 | — | 4 组 | 6 | — | 3 个 GetDiagnostics 接口成员 0 消费 |
| | **合计** | **665** | **2987** | — | — | **~53 组/处** | **78 符号 + 2 死类** | **6** | 见 §6/§7/§8 |

> 注：① D 级 78 符号 + 2 死类中「可安全删」14（纯机械删除）、「需人工复核」64（接口契约 39+14 / 文档化基类 API 7 / DI 扩展 3 / 死 Model 4 / extern SDK 1 组）；D 计数为**实现位置归属**的符号数（§7.4 口径），项目行内数字含该模块内实现的死方法（如 Foundation 7 含 ClientErrorMessageMapper 3 等），接口成员 0 消费按「接口在 Contracts/实现在各处」分别计入归属项目；② E 级 6 机制点 = 日志 3（LoggingHttpHandler/DesktopSerilogConfiguration/LoggingRegistration）+ 异常 2（ClientErrorMessageMapper/DesktopExceptionHandler 委托）+ 策略 1（DoctorOrAdminOrReceptionist）；③ 全部候选经 XAML/命令绑定核实，无「仅凭 C# grep 判死」项。

---

## 2. VM 方法/命令重复清单

> 复核范围：44 个 ViewModel（Modules 27 + Roles 12 + Shell 5）。基类能力基准：`NavigableViewModelBase`（OnNavigatedTo/From+Core 钩子+InitializeAsync，`Navigation.cs:26-67`；`ExecuteWithErrorHandlingAsync` `Async.cs:22-93`；`Dispose/OnDisposing` `Editable.cs:139-163`）、`MasterDetailViewModelBase`（全套命令 Refresh/Search/CreateNew/Edit/Save/Cancel/Delete，`:131-145`）、`MasterDetailCommandGroup`。

### 2.1 同名命令目标方法跨模块重复（13 组）

| # | 命令 | 位置（file:line） | 同构度 | 备注 |
|---|------|------------------|--------|------|
| V1 | RefreshAsync | 基类 `MasterDetailViewModelBase.cs:131`；`RegistrationListViewModel.cs:171-175`；`ClinicalWorkspaceViewModel.cs:162-163`；`PatientSelectionViewModel.cs:179-180`；`PendingQueueViewModel.cs:65-66` | 4 处非基类重写 | 5 个非 MD 列表型 VM 绕开体系各自重写——**最大收敛机会** |
| V2 | SearchAsync | 基类 `:132`；`ClinicalWorkspaceViewModel.cs:166-167`；`PatientSelectionViewModel.cs:183-184` | 3 处 | 同上 |
| V3 | SaveAsync | 基类 `:140`；`SystemSettingsViewModel.cs:202-203`；`MedicalCaseCommandsViewModel.cs:115`(ExecuteSaveAsync) | 骨架同构 | — |
| V4 | TestConnectionAsync | `ServerConfigViewModel.cs:87-120`；`FirstRunSetupViewModel.cs:75-112` | **~95%** | 状态机逐行一致；CanTestConnection/OnTestStatusChanged/IsNotTesting 成对重复 |
| V5 | 读卡流程 | `PatientCardReaderViewModel.cs:47-84`；`PatientMasterDetailViewModel.cs:269-299`；`CardReaderViewModel.cs:131-166`；`ReceptionistHomeViewModel.cs:96-148` | 4 处 ~85% | 读卡→找/建患者→导航全流程 |
| V6 | ToggleStatus(Async) | `HerbMasterDetailViewModel.cs:220-231`；`FormulaMasterDetailViewModel.cs:227-236`；`UserMasterDetailViewModel.cs:358-367` | 3 处 | — |
| V7 | SearchByCategoryAsync | `HerbMasterDetailViewModel.cs:266-274`；`FormulaMasterDetailViewModel.cs:313-321` | **~95%** | — |
| V8 | NavigateToPatientManagement | `AdminHomeViewModel.cs:80-81`；`ClinicalHomeViewModel.cs:96-101`；`ReceptionistHomeViewModel.cs:87-89` | 3 处一行式 | Home VM 菜单命令 |
| V9 | NavigateToRegistrationQueue | `ClinicalHomeViewModel.cs:137-142`；`ReceptionistHomeViewModel.cs:91-93` | 2 处 | — |
| V10 | NewPatient/CreateNewPatient | `ClinicalWorkspaceViewModel.cs:146-159`；`PatientSelectionViewModel.cs:170-176`；`ReceptionistHomeViewModel.cs:150-154` | 3 处 | — |
| V11 | GoBack | `AuditLogViewModel.cs:93-94`；`DeploymentViewModel.cs:108-109`；`AccountSettingsViewModel.cs:217-218` | 3 处逐字相同 | 一行式 `NavigateBack()` |
| V12 | CopyHerb/CopyFormulaAsync | `HerbMasterDetailViewModel.cs:234-248`；`FormulaMasterDetailViewModel.cs:241-276` | 同义异实现 | — |
| V13 | LoadPatientsAsync | `ClinicalWorkspaceViewModel.cs:174-219`；`PatientSelectionViewModel.cs:250-285`；`PatientMasterDetailViewModel.cs:100-127` | ~80% | 三方同构 |

### 2.2 VM 生命周期样板重复（7 类）

| # | 样板 | 证据 | 收敛方向 |
|---|------|------|---------|
| L1 | **Editor VM 模板 4 份拷贝**（InitializeFromDto/InitializeForNewCase/GetXData/Validate/Reset/IsDirty/OnXPropertyChanged） | `UserEditorViewModel.cs` / `PatientEditorViewModel.cs` / `HerbEditorViewModel.cs` / `FormulaEditorViewModel.cs`，Patient/Herb/Formula ~90% 逐行同构（User 略异）；ConsultationEditor/PrescriptionEditor 同骨架 | 引入 `EditorViewModelBase<TContext,TDto,TInput>` |
| L2 | 空转 INavigationAware override 6 处 + API 分裂 | AdminHome:161-177、ClinicalHome:207-223、ClinicalWorkspace:327-338、PatientSelection:450-469、ReportsHome:46-57、Login:235-238；**`AccountSettingsViewModel.cs:254` 为 async void 且不调 base**（吞掉基类 IsActive/InitializeAsync 逻辑） | 统一用 Core 钩子，删空转 override，修 async void |
| L3 | Dispose(bool) 重写绕过 OnDisposing 钩子 2 处 | `UserMasterDetailViewModel.cs:396-403`、`FormulaMasterDetailViewModel.cs:359-366` | 改用 OnDisposing |
| L4 | LoadCurrentUser 4 处 | AdminHome:133-155、ClinicalHome:172-191（逐行同构）、ReceptionistHome:79-83、AccountSettings:224-241 | 基类模板方法 |
| L5 | 30 秒轮询 2 处 | RegistrationList:134-157（PeriodicTimer）、SysadminHome:47-96（CTS） | `AutoRefreshController` |
| L6 | 遮蔽基类属性（需人工复核） | `ReportsHomeViewModel.cs:24-28`、`AuditLogViewModel.cs:23` 的 `[ObservableProperty] _isLoading/_errorMessage` 遮蔽基类 IsLoading/ErrorMessage | 复核后删除遮蔽 |
| L7 | MD LoadListAsync 骨架 5 份重复 | 5 个 MD VM 同模板（Log 分页→ExecuteWithLoadingAsync→Items.Clear+Add→catch HandleException） | 下沉为基类模板方法 |

### 2.3 事件订阅/取消订阅重复（5 处）

| # | 问题 | 证据 |
|---|------|------|
| E1 | ctor 裸订阅从不 Unsubscribe（泄漏） | `RegistrationListViewModel.cs:95` `eventAggregator.GetEvent<RegistrationRefreshedEvent>().Subscribe(...)`，未用基类 Events 管理器 |
| E2 | 双重机制重叠 | `MedicalCaseWorkspaceViewModel.cs:309-310` 用 `Events.Subscribe`（自动清理），:583-584 又手工 `Unsubscribe` |
| E3 | 每次 Initialize 重新 `+=`，仅 Reset 内 `-=`（多 Initialize/弃用不清，泄漏风险） | Patient/Herb/Formula 3 个 Editor VM（:52,62,91 / :53,63,98 / :76,89,148） |
| E4 | KeepAlive=true 下每次导航 `+=` 可能重复订阅（需人工复核） | `MedicalCaseWorkspaceViewModel.cs:460-461` 子 VM PropertyChanged |
| E5 | 良好范例对照（无需改） | ConnectionStatus:46-50/257-261、Login:173-177/310-311、Clinical CardReader:90-92/435-437、MainWindow:111-115/265-268 |

---

## 3. DTO↔Model 映射方法分析

### 3.1 Desktop 映射方法总表（机制分布）

**Mapperly（5 个 Mapper 类 / 13 个映射方法）**——全部在 MedicalCase 与 Formula 模块：

| Mapper | 方法（file:line） | 源→目标 |
|--------|------------------|---------|
| `ConsultationMapper`（MedicalCase） | `ToItemCore/ToItem` `:43/50`、`ToDtoCore/ToDto` `:85/92`、`ToInputDto` `:124` | ConsultationDetailDto↔ConsultationItem；Item→ConsultationInputDto |
| `PrescriptionMapper`（MedicalCase） | `ToItemCore/ToItem` `:49/56`、`ToDtoCore/ToDto` `:96/103`、`ToInputDtoCore/ToInputDto` `:147/154`（Items 手写 162-171） | PrescriptionDetailDto↔PrescriptionItemViewModel |
| `MedicalCaseDetailModelMapper` | `ToItemCore/ToItem` `:74/81`（嵌套 Consultation/Prescription 手写 85-106）、`ToInputDtoCore/ToInputDto` `:150/157` | MedicalCaseDetailDto↔MedicalCaseDetailModel |
| `FormulaDetailModelMapper` | `ToItemCore/ToItem` `:49/56`（Herbs 手写 60-73）、`ToDtoCore/ToDto` `:98/105`、`ToInputDtoCore/ToInputDto` `:138/145`（Herbs 手写 153-160） | FormulaDetailDto↔FormulaDetailModel |
| `MedicalCaseCloneMapper` | `Clone`×3 `:28/35/42` | DTO 深克隆（MC/Consultation/Prescription） |

**手写映射（13 处）**——集中 5 个文件（全在 Editor VM / MasterDetail VM / Shared 扩展）：

| 位置 | 方法 | 说明 |
|------|------|------|
| `Shared/.../Extensions/DtoConversionExtensions.cs:20,40,59` | `ToInputDto`×2 / `ToPrescriptionInputDto` | **唯一活跃调用点 `MedicalCaseCommandService.cs:46`**（workspace 保存链路）——S1 C3 判定的两套机制并存实锤 |
| `HerbEditorViewModel.cs:31,69` | `InitializeFromDto` / `GetHerbData` | HerbDetailDto↔HerbEditContext ~14 字段 |
| `HerbMasterDetailViewModel.cs:114-130,132-148` | LoadDetail 内联 ×2 | HerbDetailDto→HerbDetailModel 与 →Editor 双段纯复制 |
| `FormulaEditorViewModel.cs:40,58-70,108` | `InitializeFromDto` / `GetHerbInputDtos` | 含 Herbs 手写 |
| `FormulaHerbItemViewModel.cs:41` | `ToDto` | →FormulaHerbItemInputDto |
| `PatientEditorViewModel.cs:36,68` | `InitializeFromDto` / `GetPatientData` | 全手写，零 Mapperly |
| `UserEditorViewModel.cs:35,68` | `InitializeFromDto` / `GetUserInput` | 全手写，零 Mapperly |
| `Controls/HerbItem/HerbItemControlViewModel.cs:166,183`、`HerbListControlViewModel.cs:128,145` | `LoadFromDto`/`ToDto` | PrescriptionItemDto↔控件 VM |
| `MedicalCase/Extensions/PrescriptionImportExtensions.cs:16,43` | `ToPrescriptionItemDtos`×2 | FormulaHerbItemDto→PrescriptionItemDto；历史项刷价 |
| `Patients/Models/Items/PatientItem.cs:163`、`Users/Models/Items/UserItem.cs:240` | `UpdateFromDto` | **无调用者（死代码）** |
| `MedicalCaseDetailModel.cs:222` | `Clone` | Model 手写克隆（与 Mapperly CloneMapper 双机制） |

### 3.2 各模块 Model 层现状（P2-11 现状判定）

| 模块 | 独立 Model | 映射机制 | 列表 | 详情/编辑 |
|------|-----------|---------|------|----------|
| Herbs | HerbDetailModel + **HerbEditContext 双份** | **纯手写**（零 Mapperly） | 直绑 HerbListDto（`HerbMasterDetailViewModel.cs:92`） | 编辑绑 EditContext、展示绑 DetailModel |
| Formula | FormulaDetailModel + FormulaEditContext | **Mapperly** + Editor 手写 | 直绑 FormulaListDto（`:106`） | Model 内 `Herbs` 为 `ObservableCollection<FormulaHerbItemDto>`（DTO 集合 `FormulaDetailModel.cs:30`） |
| Patients | PatientDetailModel + PatientEditContext + **PatientItem 死** | 全手写 | 直绑 PatientListDto（`:117`） | 编辑绑 EditContext |
| Users | UserDetailModel + UserEditContext + **UserItem 死** | 全手写 | 直绑 UserListDto（`:153`） | 编辑绑 EditContext |
| MedicalCase | MedicalCaseDetailModel + ConsultationItem + PrescriptionItemViewModel | **Mapperly 4 个** + Model 内嵌静态 s_mapper | 直绑 MedicalCaseListDto（`MasterDetailViewModel.cs:99-102`） | 详情绑 Model（`_mapper.ToItem` :125）；Model 内 `PrescriptionItems` 直持 `PrescriptionItemDto`（`MedicalCaseDetailModel.cs:170`） |

**P2-11 立场**：现状已是事实上的「**列表直绑 DTO + 详情/编辑绑 Model**」混合模式。建议固化为统一方针：**列表 DTO 直绑（只读展示零映射成本）+ 详情一律 Model + 保存一律 Mapperly 生成 InputDto**。

### 3.3 映射重复清单（同实体多处映射，7 项）

| # | 重复 | 证据 |
|---|------|------|
| M1 | PrescriptionItemDto→InputDto 双份手写（都带 Subtotal 计算） | `DtoConversionExtensions.cs:72-84` vs `PrescriptionMapper.cs:162-171` |
| M2 | Consultation→InputDto 双机制 | 手写 `DtoConversionExtensions.cs:40-49` vs Mapperly `ConsultationMapper.cs:124` |
| M3 | MedicalCaseDetailDto→InputDto 双链路 | 手写 `DtoConversionExtensions.cs:20-31`（**workspace 保存活链路** `MedicalCaseCommandService.cs:46`）vs Mapperly `MedicalCaseDetailModelMapper.cs:157`（医案管理保存）——同一聚合两条保存路径 |
| M4 | HerbDetailDto 手写双份 | `HerbMasterDetailViewModel.cs:114-130` vs `:132-148` 字段一一对应纯复制 |
| M5 | PrescriptionItemDto 两套载体 | 控件 `HerbItemControlViewModel.cs:166/183` vs `PrescriptionItemViewModel.Items` 直存 DTO 透传 |
| M6 | Clone 双机制 | Model 手写 `MedicalCaseDetailModel.Clone():222` vs Mapperly `MedicalCaseCloneMapper`（仅 DTO 层） |
| M7 | 死代码映射 | `PatientItem.UpdateFromDto:163`、`UserItem.UpdateFromDto:240` 零调用 |

### 3.4 与 S1 C3 衔接确认

- **Shared 手写 `DtoConversionExtensions` 唯一活跃调用点**：`MedicalCaseCommandService.cs:46`（`_context.CurrentDetail.ToInputDto()`，CurrentDetail 为 `MedicalCaseDetailDto`）。**该扩展不是纯残留**——仍在 workspace 保存链路活跃使用，与 Mapperly 并存成立。
- 模块内手写 `ToInputDto` 4 处均走 Mapperly（`ConsultationItem.cs:239`、`PrescriptionItemViewModel.cs:383`、`PrescriptionMapper.cs:154`、`MedicalCaseDetailModelMapper.cs:157`）。
- **收敛路径**：`MedicalCaseCommandService.SaveAsync` 改用 Editor VM 已生成的 `GetConsultationData()/GetPrescriptionData()`（`MedicalCaseWorkspaceViewModel.cs:477-498` 已有）组装聚合 → 删除 Shared 三个手写扩展 → 直接回应 S1 C3/A-26 P2-11。
- ⚠️ **文档漂移**：`docs/03-architecture/15-mapperly.md:19,33,143-158` 仍描述已废弃的 `LYBT.Desktop.LocalData`（6 个 Mapper，Both 策略）——项目已删（`Test-Path src/Client/Desktop/Core/LYBT.Desktop.LocalData` = False，A-21 C1 废弃），文档需同步。

---

## 4. 双轨差异方法清单（LocalWebAPI vs WebAPI）

> 双树各 12 Controller。Users/Herbs/Patients 三对仅微量差异；Reports 为**内联复刻**；Auth 为**本地 CQRS 复刻**；Deploy 为**桩**。

### 4.1 12v12 Controller 方法差异表

| Controller | 差异类型 | 方法/证据 |
|---|---|---|
| **Auth** | 同名实现差异（内联复刻 CQRS handler） | Local 发送**本地私有 Commands**（`LocalLoginCommand`/`LocalRefreshTokenCommand`/`LocalAutoLoginCommand`/`LocalValidateTokenQuery`，`LocalWebAPI/Commands/`+`Handlers/`）——把 Remote `LYBT.Module.Auth.Application` 的 Login/Refresh/AutoLogin/Validate 逻辑**内联复刻**进 LocalWebAPI 程序集（`AuthController.cs:32,50,60,70`）。Local `Logout`（`:38-44`）为 **no-op 桩**（只记日志，不 send `LogoutCommand`）；Remote `LogoutAsync`（`:66-80`）真实失效 token。ValidateToken 路径不同（Local 从 Claims 取 userId `:24-25,66-72` vs Remote 从 Bearer header 解析 `Remote:122-162`）；RateLimiter 不同（Local `"LocalLogin"` / Remote `"Login"`）；Local 缺 `ValidateModel()`/ErrorCode→HTTP 映射/ProducesResponseType。Remote 独有 `Get()` 桩 `:164-168` |
| **Configuration** | 同名实现差异 | Local 注入 `IConfigurationStore`（JsonFile 落盘 `Local:21-27`）vs Remote `ISystemConfigurationService`（白名单+热更新 `Remote:20-28`）；Local `Set` 手写 role claim 双重检查（`:59-61`）；`Validate`（Local `:78-100`）vs `ValidateProduction`（Remote `:91-99`）语义不同；Remote 独有 `UpdateConfiguration` PUT `:75-86` |
| **Deploy** | Local 为桩 | Local `Upload`/`Restart` 均 `BusinessFail` 桩（`DeployController.cs:17-26`）；Remote 真实上传 zip + `StopApplication()`（`Remote:31-62`，Restart 要求 `confirm="RESTART"`） |
| **Diagnostics** | Local 独有 3 端点 | `GetDbInfo`（`:34-54`）、`GetVersion`（`:56-74`）、`GetRecentLogs`（`:76-96`）Remote 无；4 个 logging 方法逻辑一致但 Local 加 `IsAdminOrHigher()` claim 双重检查（`:118,147,162`） |
| **Formulas** | 同名实现差异（**契约分歧**） | `POST batch-import` 请求形状不同：Local `[FromBody] List<FormulaImportItemDto>`+`fileName:null`（`:213-221`）vs Remote `FormulaBatchImportInputDto`+`FileName`（`Remote:217-236`）。Local 缺 ValidateGuid/Restore LogOperation/rate limit/version；**Local 独有 `Clone`（`:174-207`）内联复刻**（手拼 FormulaInputDto→复用 CreateFormulaCommand，应收敛为 CloneFormulaCommand） |
| **Health** | 同名实现差异 | Local 类级 `[AllowAnonymous]`+真实 DB 检查（`:35-37`）；Remote 类级 `[Authorize]`+静态 "Healthy"（`Remote:41`）；`GetDetails` Local 注入 `UserManager<ApplicationUser>` 统计用户（`:67,76`）+恒 200，Remote 无统计+非 Healthy 返回 503（`Remote:97-98`） |
| **Herbs** | 同名实现差异（微量） | 14 方法一一对应；Local 缺 Create rate limit+version、3 处 LogOperation、CheckReference ValidateGuid（`Local:192,233,250,207` vs `Remote:228,280,300,239`） |
| **MedicalCases** | 同名实现差异 | 双树同继承 Module 基类 `BaseMedicalCasesController`（12 方法共享非复制）。`Query` override 行为分歧（`Local:76-84` 不设 `IncludeAllDoctors` vs 基类 `:64-83`）；`UpdateStatus` 返回体分歧（Local 纯消息 `:256-276` vs Remote Mapper 映射 DTO `Remote:249-273`）；失败返回 Local `BusinessFail` vs Remote `NotFound`（`Local:214,231,248` vs `Remote:289,313,336`）；`Create` Local 200 vs Remote 201 `CreatedAtAction`（`Remote:108-110`）。Local 独有 `GetByStatus`（`:89-97`）/`GetPending`（`:102-117`） |
| **Patients** | 同名实现差异（微量） | Remote 抽私有 `CheckOwnershipAsync`（`Remote:282-292`）；Local 仅 Update 内联所有权检查（`:95-99`），Delete/ToggleStatus 无。Local `Restore` 缺 LogOperation+「未被删除」映射（`Local:185-195` vs `Remote:185-191`）；`ToggleStatus` 文案不同+缺 LogOperation（`Local:177` vs `Remote:168`）。类级策略双树相同 |
| **Registrations** | 同名实现差异 | Local `QuickVisit`/`StartVisit`/`Cancel` 委托基类 `BaseRegistrationsController`（`Local:27-33,48-49`），仅 `Create` 内联且缺 LogOperation/CreatedAtAction（`Local:37-44`）；Remote 4 方法**全部内联复写**+日志+限流（`Remote:36-105`）。策略集双树一致 |
| **Reports** | **内联复刻（S2 确认）** | Local 注入 `IReportRepository` 而非 `IReportService`（`Local:16-21`），`:32-42`/`:54-63`/`:75-79` **逐行复刻** `ReportService.GetDailyIncomeAsync`（`ReportService.cs:20-31`）/`GetDailyConsultationsAsync`（`:33-43`）/`GetDailyHerbUsageAsync`（`:45-50`）；`AddReportsModule` 已注册 IReportService（`ReportsModule.cs:21`；`LocalWebApiProgram.cs:76`）——复刻可完全消除。Remote 独有 5 端点（trend/income、trend/consultations、doctor-performance、herbs/ranking、patient-flow `Remote:89-178`）Local 全缺，且 `IApiClientReports` 仅 3 个 daily 方法（`IApiClientReports.cs:8-12`）桌面端零消费 → **Remote 死亡表面**。**策略分歧（新发现）**：Local `DoctorOrAdminOrReceptionist`（`Local:13`）vs Remote `DoctorOrAdmin`（`Remote:19`）——同端点授权面不同 |
| **Users** | 无（完全收敛） | 两侧均为空子类继承 `BaseUsersController`（`Local/UsersController.cs:9-20` vs `Remote/UsersController.cs:12-24`），仅路由模板不同；基类 14 方法共享——**双树对齐模板** |

**内联复刻清单汇总**：① Reports 3 方法（对 ReportService 逐行复刻，Service 已注册）；② Auth 4 个本地 CQRS handler（对 Module.Auth 逻辑复刻）；③ Formulas.Clone（本地拼接逻辑）；④ Registrations.Create（简化内联缺日志）。

### 4.2 SwitchingApiClient 分支方法

| 层次 | 分支方式 | 证据 |
|------|---------|------|
| 统一切换点 | **实例级单一切换**：`SwitchingApiClient.Current`（`SwitchingApiClient.cs:56-83`）按 `_connectionSettings.IsLocal` 创建 `HttpClientApiClient`（本地）/`RefitApiClient`（远程），URL 未变走无锁快路径；11 个领域属性全部转发 `Current.X`（`:86-116`）。**无 per-method if/else** ✅ | `SwitchingApiClient.cs:75-77` |
| URL 判定 | `ConnectionSettingsService.IsLocal` = URL 含 127.0.0.1/localhost（`ConnectionSettingsService.cs:71`）；`ConnectionModeService` 双路径健康探测（`TestRemoteConnectionAsync` `:97-120` vs `TestLocalConnectionAsync` `:123-137`） | `ConnectionModeService.cs` |
| 双实现契约 stub | 本地-only 方法在远程适配器抛 `NotSupportedException`，反之亦然：`UserApiClient.GetCurrentUserAsync`（`:87` 本地-only，远程模式消费方 AccountSettings:228/ClinicalHome:176/AdminHome:137 **抛异常**）；`FormulaApiClient.GetCategoriesAsync`（`:101`）/`HerbApiClient.GetCategoriesAsync`（`:88`）；`RegistrationApiClient.GetRegistrationsAsync/QuickVisitAsync/DeleteRegistrationAsync`（`:65/:70/:75` 本地-only）——**QuickVisit 快速看诊远程不可用，但 Remote 服务器有该端点**（`Remote/RegistrationsController.cs:33-50`）；`ConfigurationHttpApiClient` 全 5 方法 NSE（`:20-32` 服务器配置 remote-only） | `UnifiedApiClientExtensions.cs` |
| handler 链 | 远程 HttpClient 走 TokenRefresh→Authorization→Logging 三层 handler（`UnifiedApiClientExtensions.cs:64-94`）；本地为裸 HttpClient 无 handler 链（`:97-101,117-139`）；注册 `SwitchingApiClient` 单例（`:103-111`） | 同上 |

### 4.3 双树策略缺口复核（交叉确认 S2 C3）

- **Local 注册 5 策略**：`LocalJwtConfig.cs:70-90`（AdminBusinessOnly/DoctorOrAdmin/DoctorOnly/AdminOrSuperAdmin/DoctorOrReceptionist）。
- **Remote 注册 6 策略**：`AuthenticationServiceCollectionExtensions.cs:112-134`（多 **DoctorOrAdminOrReceptionist** `:132-134`）。
- **缺口确认 ✅**：`DoctorOrAdminOrReceptionist` 未在 Local 注册，但被 Local 引用：`PatientsController.cs:20`、`RegistrationsController.cs:17`、`ReportsController.cs:13`（均类级）。后果：本地模式这三组端点授权求值时策略不存在 → `InvalidOperationException` → **本地模式患者/挂号/报表模块整体不可用**（P0 真实 bug，S1 已标，S3 交叉确认引用点）。
- **新增分歧**：Reports 双树策略值不同（Local=DoctorOrAdminOrReceptionist `:13` vs Remote=DoctorOrAdmin `:19`）——独立于注册缺口的第二处授权漂移。

---

## 5. Repository/Service 方法复核（A-28 后）

> 复核范围：Desktop 7 个业务 Repository + 2 个基类 + 5 个业务 Service 接口/实现（LocalWebAPI 控制器注入的是 Server 侧 `LYBT.Module.*` 服务——`HerbsController.cs:25` `using LYBT.Module.Herbs.Interfaces`——与 Desktop Service 无共享，不计入）。

### 5.1 Repository 继承全景（结论：无裸实现，分两族）

| Repository | 继承类型 | 文件:行 |
|---|---|---|
| HerbRepository | EntityApiClient | `Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs:13` |
| FormulaRepository | EntityApiClient | `Modules/LYBT.Desktop.Formula/Repositories/FormulaRepository.cs:13` |
| UserRepository | EntityApiClient | `Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs:17` |
| PatientRepository | EntityApiClient | `Modules/LYBT.Desktop.Patients/Repositories/PatientRepository.cs:14-16` |
| MedicalCaseRepository | ApiClient（**手写 CRUD**） | `Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs:14` |
| RegistrationRepository | ApiClient（**手写 CRUD**） | `Modules/LYBT.Desktop.Registrations/Repositories/RegistrationRepository.cs:13` |

- 基类：`ApiClientRepositoryBase<TList,TDetail>`（abstract，`Foundation/Repositories/ApiClientRepositoryBase.cs:11`）+ `EntityApiClientRepositoryBase<TList,TDetail,TInput>`（继承上者，`:17`，增加 GetPagedAsync:42/GetByIdAsync:69/CreateAsync:81/UpdateAsync:97/DeleteAsync:117 五个 virtual CRUD）。
- **无裸实现** ✅（A-28 后健康）；但继承分两族——4 个走泛型 CRUD 基类，2 个（MedicalCase/Registration）因 `IMedicalCaseApi`/`IRegistrationApi` 无泛型段而手写 CRUD。

### 5.2 基类方法 vs 继承者重复（最严重单点）

| # | 重复 | 证据 |
|---|------|------|
| R1 | **MedicalCaseRepository 手写 CRUD = 基类逐行复刻** | `GetPagedAsync`（`:30-50`）≈ 基类 `:42-64`；`GetByIdAsync`（`:52-61`）≈ `:69-76`；`CreateAsync`（`:63-79`）≈ `:81-92`（含 `response.Success==false→throw InvalidOperationException` 逐行）；`UpdateAsync`（`:81-99`）≈ `:97-112`（含 Id 校验）；`DeleteAsync`（`:101-114`）≈ `:117-130` |
| R2 | RegistrationRepository 手写 CRUD | `CreateAsync`（`:28-44`）/`GetByIdAsync`（`:47-56`）/`GetPagedAsync`（`:59-73`）与基类同构（GetPaged 少 PageSize 透传、返回 `response.Data` 原样——轻微漂移） |
| R3 | SearchAsync 4 份同构未上收 | Herb:29-41、Formula:29-41、User:39-51、Patient:38-47——完全相同形状（`GetXxxAsync(1,100,keyword)`，Data==null→`[]`，`.Items.ToList()`） |
| R4 | ToggleStatusAsync 3 份 | Herb:128-142、Formula:68-82、User:189-204 |
| R5 | RestoreAsync 2 份 | Formula:84-98、User:206-220 |
| R6 | BatchDeleteAsync 5 份 | Herb:144-151、Formula:100-107、User:222-229、Patient:131-138、MedicalCase:360-367——全部 3 行 `ExecuteBatchDeleteAsync(()=>api.BatchDeleteAsync(...))` |
| R7 | BatchImportAsync 3 份 | Herb:47-72、Formula:113-136、Patient:84-97——相同手动 try/catch、失败返回 null（注释逐字相同） |
| R8 | ExportTemplateAsync 3 份 | Herb:74-97、Formula:163-186、Patient:99-111 |
| R9 | ExportXxxAsync 3 份 | Herb.ExportHerbsAsync:99-122、Formula.ExportFormulasAsync:138-161、Patient.ExportPatientsAsync:113-125 |

### 5.3 镜像方法清单（跨仓库同构）

| 方法 | 出现次数 | 形状 |
|---|---|---|
| GetPagedAsync | 7/7 | 4 继承/转发（User:34-35、Patient:33-34 转发；Herb/Formula 直接继承）+ 2 手写（MedicalCase:30、Registration:59）→ **2 种实现形状** |
| SearchAsync | 4 | Herb:29 / Formula:29 / User:39 / Patient:38 |
| ToggleStatusAsync | 3 | Herb:128 / Formula:68 / User:189 |
| RestoreAsync | 2 | Formula:84 / User:206 |
| BatchDeleteAsync | 5 | Herb:144 / Formula:100 / User:222 / Patient:131 / MedicalCase:360 |
| BatchImportAsync | 3 | Herb:47 / Formula:113 / Patient:84 |
| ExportTemplateAsync | 3 | Herb:74 / Formula:163 / Patient:99 |
| ExportHerbs/Formulas/PatientsAsync | 3 | Herb:99 / Formula:138 / Patient:113 |

> 任务书点名的 `GetByIdIncludingDeletedAsync`/`ExistsByNameAsync`/`GetAllActiveHerbsAsync` **Desktop 不存在**（Server-only）；`GetCategoriesAsync` 仅存在于 HttpApiClient 层且 local-only stub（`HerbApiClient.cs:87` 抛 NotSupported），Repository/Service 未暴露。独有方法（合理特化）：User.GetByUsernameAsync:57/GetDoctorsAsync:81/ChangeProfileAsync:110/ChangePasswordAsync:130/ResetPasswordAsync:155；Patient.GetByIdNumberAsync:53；Formula.CloneFormulaAsync:47；MedicalCase 生命周期族（CloseCase/Cancel/Suspend/UpdateStatus/Save/SetPrescriptionFlag）。

### 5.4 Service 接口 0 消费成员（22 个）+ 死实现方法（6 个）

> Service 层性质：5 个业务 Service（Herb/Patient/Formula/User/Registration）均为「CommandResult 适配器」——每方法 = `try{log; await repo.Xxx(); return Succeeded} catch{return Failed}`，**零业务逻辑**（`RemoteHerbService.cs:34-49` 为例），与 Repository 方法 1:1 透传签名镜像。这是「重复」的主要形态。

| Service | 0 消费接口成员 | 证据 |
|---------|---------------|------|
| IHerbService | `BatchDeleteAsync:33`、`GetAllAsync:53`、`BatchImportAsync:76`、`ExportTemplateAsync:81`、`ExportHerbsAsync:86`（5） | DI 注册 `HerbsModule.cs:32`；已消费 7 个（Create/Update/Delete/GetById/GetPaged/Search/ToggleStatus） |
| IPatientService | `BatchDeletePatientsAsync:36`、`BatchImportAsync:64`、`ExportTemplateAsync:69`、`ExportPatientsAsync:74`（4） | 注册 `PatientsModule.cs:44` |
| IFormulaService | `BatchDeleteAsync:91`、`BatchImportAsync:100`、`ExportFormulasAsync:105`、`ExportTemplateAsync:110`、**`ToggleStatusAsync:82`（被绕过）**（5） | **FormulaStatusHandler.cs:16 直接注入 `IFormulaRepository`**，:37 调 `_formulaRepository.ToggleStatusAsync`，VM（FormulaMasterDetailViewModel.cs:231）走 `_statusHandler`——Service 的 ToggleStatusAsync 无消费者 |
| IUserService | `BatchDeleteAsync:33`、`GetAllAsync:53`、`GetByUsernameAsync:58`、`SearchAsync:63`（4） | 注册 `UsersModule.cs:38` |
| IRegistrationService | `GetByIdAsync:23`（1） | 注册 `RegistrationModule.cs:31` |
| IMedicalCaseService | 接口成员：`HasChanges:66`、`SaveAsync:68`（VM 改用 AggregateSaveAsync）、`DeleteAsync:71`、`InitializeAsync:85`、`ReloadAsync:88`、`CompleteMedicalCaseAsync:97`（仅被 SaveAndCompleteAsync:201 自调）、`CloseCaseAsync:103`、`GetPendingCasesAsync:58`（被 PatientSelectionViewModel.cs:198 经 `IMedicalCaseQueryService` 直连，不走 facade）、`ClearCache:141`（9）；**非接口死方法**：`GetByIdSimpleAsync:240`、`SetPrescriptionFlagAsync:253`、`DeleteMedicalCaseAsync:275`、`UpdateStatusAsync:288`、`SuspendViaApiAsync:309`、`CancelMedicalCaseViaApiAsync:330`（6） | 注册 `MedicalCaseModule.cs:44`；测试仅走 HTTP 客户端（MedicalCaseTests.cs:250/318），不经 service |

**合计 0 消费：接口成员 22 + 非接口死实现 6 = 28 符号。**

**附带发现**：
- `Modules/LYBT.Desktop.Users/AGENTS.md` 反模式条目「Desktop IUserService not registered, not referenced」与「UserImportExportHandler 存在」**均已过时**——IUserService 实际注册（UsersModule.cs:38）并被 5 处消费；`*ImportExportHandler*` 文件全库不存在（glob 0 命中）。
- 状态切换路径三模不一致：Formula 走 Repository（FormulaStatusHandler.cs:16）、Herb/User 走 Service（HerbStatusHandler.cs:38、UserStatusHandler.cs:45）——统一为 Service，避免 Service 成员继续旁路死亡。

---

## 6. C 级重复清单 + 收敛方向

> 汇总 §2-§5 的 C 级发现（去重），按维度分组；「收敛方向」为建议，执行需另立任务书并经审批。

### 6.1 VM/命令（C-V）

| # | 重复内容 | 证据 | 收敛方向 |
|---|---------|------|---------|
| C-V1 | 5 个非 MD 列表 VM 绕开体系重写 Refresh/Search/Load | RegistrationList:171、ClinicalWorkspace:162、PatientSelection:179、PendingQueue:65、SystemSettings:202（基类能力 `MasterDetailViewModelBase.cs:131-145` 未用） | 下沉为基类模板方法（最大收敛机会） |
| C-V2 | TestConnection 状态机双份 ~95% | `ServerConfigViewModel.cs:87-120` vs `FirstRunSetupViewModel.cs:75-112` | Auth 内合并 `ConnectionTestViewModelBase` |
| C-V3 | 读卡流程 4 处 ~85% | PatientCardReader:47 / PatientMasterDetail:269 / CardReader:131 / ReceptionistHome:96 | 单一 `ICardReadFlowCoordinator` |
| C-V4 | Editor VM 模板 4 份 ~90% | User/Patient/Herb/FormulaEditorViewModel | `EditorViewModelBase<TContext,TDto,TInput>` |
| C-V5 | Home 菜单 NavigateToXxx 命令 3 Home VM | AdminHome:69-115、ClinicalHome:96-142、ReceptionistHome:87-93 | 基类导航 API 下沉 |
| C-V6 | LoadCurrentUser 4 处 | AdminHome:133、ClinicalHome:172（逐行同构）、ReceptionistHome:79、AccountSettings:224 | 基类模板方法 |
| C-V7 | ToggleStatus/SearchByCategory/GoBack/LoadPatientsAsync 跨模块 | §2.1 V6/V7/V11/V13 | 随基类模板一并收敛 |
| C-V8 | 事件订阅清理分裂 | §2.3 E1-E4 | 统一基类 Events 管理器 |

### 6.2 映射（C-M）

| # | 重复内容 | 证据 | 收敛方向 |
|---|---------|------|---------|
| C-M1 | MedicalCase 聚合保存双链路（手写 Shared 扩展 vs Mapperly） | `DtoConversionExtensions.cs:20`（`MedicalCaseCommandService.cs:46` 唯一调用）vs `MedicalCaseDetailModelMapper.cs:157` | 统一 Mapperly；删 Shared 3 个手写扩展（S1 C3 落点） |
| C-M2 | Herbs/Patients/Users 三模块零 Mapperly 纯手写 | HerbEditor:31,69 / PatientEditor:36,68 / UserEditor:35,68 | 补 Mapperly（对齐 Formula/MedicalCase 模式） |
| C-M3 | PrescriptionItemDto→InputDto 双份手写 | `DtoConversionExtensions.cs:72-84` vs `PrescriptionMapper.cs:162-171` | 归并 Mapperly |
| C-M4 | HerbDetailDto 手写双段纯复制 | `HerbMasterDetailViewModel.cs:114-130` vs `:132-148` | 合并为单一 Mapper |
| C-M5 | Clone 双机制 | `MedicalCaseDetailModel.Clone():222` vs `MedicalCaseCloneMapper` | 统一 Mapperly 深克隆 |

### 6.3 双轨（C-D）

| # | 重复内容 | 证据 | 收敛方向 |
|---|---------|------|---------|
| C-D1 | Reports 内联复刻 Service 逻辑 | `LocalWebAPI/ReportsController.cs:32-42,54-63,75-79` == `ReportService.cs:20-31,33-43,45-50` | Local 改注入 IReportService（已注册）；Remote 5 死亡端点先决策 |
| C-D2 | Auth 本地 CQRS handler 复刻 | `LocalWebAPI/Commands+Handlers/`（LocalLoginCommand 等 4 个） | 复用 Module.Auth 逻辑或文档化例外 |
| C-D3 | Formulas.Clone 内联复刻 | `LocalWebAPI/FormulasController.cs:174-207` | 收敛为 `CloneFormulaCommand` |
| C-D4 | 双树策略缺口 + 分歧 | §4.3（P0） | LocalJwtConfig 补 DoctorOrAdminOrReceptionist + Reports 策略取一 |
| C-D5 | BatchImport 契约分歧 | Local `List<FormulaImportItemDto>` vs Remote `FormulaBatchImportInputDto` | 统一 DTO 形状 |
| C-D6 | MedicalCases Query/UpdateStatus/Create 行为分歧 | §4.1 | 对齐（Local 补 ValidateGuid；返回体统一） |

### 6.4 Repository/Service（C-R）

| # | 重复内容 | 证据 | 收敛方向 |
|---|---------|------|---------|
| C-R1 | MedicalCase/Registration 手写 CRUD = 基类复刻 | §5.2 R1/R2 | 接入 `IEntityApiSegment` 删手写 CRUD（~85 行/仓） |
| C-R2 | 8 组镜像方法（Search/ToggleStatus/Restore/BatchDelete/BatchImport/Export×2） | §5.2 R3-R9 | 基类受保护模板方法，继承者传 API 委托（净删 ~150 行） |
| C-R3 | Service 适配器骨架 4 份逐字同构 | RemoteHerbService.cs:34-49 等 | `ServiceBase<T>` 或装饰器收敛 |
| C-R4 | 0 消费成员 28 符号（含整条未接线导入/导出/批量删除链） | §5.4 | 二选一：补 UI（先文档后代码）或删除（技术总监裁定） |
| C-R5 | 状态切换路径三模不一致 | §5.4 附带 | 统一走 Service |

### 6.5 文档-代码漂移（D 级文档问题，需同步修复）

| # | 漂移 | 证据 |
|---|------|------|
| W1 | `15-mapperly.md:19,33,143-158` 仍描述已废弃 LocalData（6 Mapper，Both 策略） | `Test-Path src/Client/Desktop/Core/LYBT.Desktop.LocalData` = False（A-21 C1 废弃） |
| W2 | `Core/AGENTS.md` 依赖序声称 `Contracts ← Foundation ← Infrastructure ← Controls`（Controls 依赖 Infrastructure） | 实际 Infrastructure.csproj 依赖 Controls（`Infrastructure.csproj` ProjectReference） |
| W3 | `Infrastructure/AGENTS.md` 列出的 `Controls/Converters/Themes` 目录不存在 | glob 0 命中（在 LYBT.Desktop.Controls） |
| W4 | `Roles/AGENTS.md` 声称「Admin/Clinical MUST NOT reference business modules directly」 | Admin.csproj/Clinical.csproj 实际直引 5 个模块项目 |
| W5 | `Users/AGENTS.md` 反模式条目过时（IUserService 未注册/ImportExportHandler 存在） | 见 §5.4 附带 |
| W6 | `DESKTOP_ARCHITECTURE_STANDARD.md:1179-1186` 仍描述 UserItem Mapperly 方案 | UserItem 无实例化死代码 |

---

## 7. D 级死方法清单（含 XAML 绑定核实）

> 复核方法：全仓 grep（src/ + tests/，含 .xaml/nameof/`+=`/method-group）+ serena LSP `find_referencing_symbols` 双重 0 引用。S0 Desktop 候选 **770** 项（§2 高置信 243 + §3 714 及 LocalWebAPI 56）中约 **96% 翻案为活**——主因是 `[RelayCommand]`/`[ObservableProperty] partial void` 源生成、XAML 事件字符串、DP `PropertyMetadata` 回调、事件订阅、接口分发五类 grep 不可见面。
> ⚠️ 全部经符号级复核；删除决策由技术总监审批（含对应文档/测试联动）。

### 7.1 确认死方法（可安全删——0 引用，删除零编译影响，14 项）

> 本表 = general-5 全量复核（D1-D11）+ general-7 Core 6 项目细复核补充（D40-D42）；两子审对 ClientErrorMessageMapper 3 方法判定冲突（general-7 误标活，经主审 grep 验证 general-5 正确：`GetFullTrackingCode` 仅测试引用 `ErrorTraceCodeTests.cs:43`，其余 2 个连测试也无）。

| # | 文件:行 | 类.方法 | 判定依据 |
|---|---------|---------|---------|
| D1 | `Core/LYBT.Desktop.Foundation/Http/HttpApiClientBase.cs:117` | `BuildQueryString` (protected static) | 12 个子类无任何调用；grep+serena 双 0 |
| D2 | `Core/LYBT.Desktop.Foundation/ExceptionHandling/ClientErrorMessageMapper.cs:365` | `GetSafeMessageWithTrackingCode` | 全仓 0 调用（连测试也无） |
| D3 | 同文件:381 | `GetMessageWithTrackingCode` | 0 调用（连测试也无） |
| D4 | 同文件:404 | `GetFullTrackingCode` | 0 调用（**仅测试引用** `ErrorTraceCodeTests.cs:43`） |
| D5 | `Core/LYBT.Desktop.Controls/Helpers/ResponsiveLayoutHelper.cs:43` | `GetOptimalColumnCount` (public static) | 0 调用；同类 GetScreenCategory/GetRecommendedMasterWidth 被 MasterDetailLayout.xaml.cs:56-58 使用，独此方法 0 引用 |
| D6 | `Core/LYBT.Desktop.Controls/Models/DuplicateDosageStrategy.cs:62` | `GetDisplayName` (public static ext) | 0 调用（含 tests） |
| D7 | `Core/LYBT.Desktop.Contracts/Models/MedicalCaseNavigationParameters.cs:61` | `ForManagementView` | 0 调用（ForClinical 在用） |
| D8 | 同文件:78 | `ForManagementEdit` | 0 调用 |
| D9 | `Core/LYBT.Desktop.Contracts/Models/PerformanceReport.cs:53` | `GetFormattedReport` | 0 调用 |
| D10 | 同文件:101 | `GetJsonReport` | 0 调用 |
| D11 | `Core/LYBT.Desktop.Foundation/Security/AuthenticationStateMachine.cs:206` | `ForceState` (internal) | 生产+tests 双 0；README 声称「恢复场景」但无调用 |
| D40 | `Core/LYBT.Desktop.Foundation/Http/HttpApiClientBase.cs:53` | `DeserializeAsync<T>` (protected static) | 0 引用；同文件其他 helper（DeserializeEnvelopeAsync 等）被子类调用，独此方法 0 |
| D41 | `Core/LYBT.Desktop.Infrastructure/Services/WpfUiThreadDispatcher.cs:17` | `WpfUiThreadDispatcher(Dispatcher)` (internal ctor) | 0 引用；public ctor(:11) 由 DI 注册（ViewModelServicesExtensions.cs:28）=活，internal 版本无使用 |
| D42 | `Core/LYBT.Desktop.Foundation/Http/RetryPolicyExtensions.cs:86` | `CreateCompositePolicy` | 业务 0 调用（仅 RetryPolicyIntegrationTests.cs:235,263 测试用） |

### 7.2 确认死方法（需人工复核——接口契约/文档化基类 API/DI 注册面/死 Model，64 项）

**A. 基类 protected API 残留（7 项，已被 `Services.UiThreadDispatcher`/`CloseDialog(parameters)` 取代；删除前先改 README/ADR-0007——文档是 SSOT）：**

| # | 文件:行 | 类.方法 | 复核点 |
|---|---------|---------|--------|
| D12 | `Infrastructure/ViewModels/Base/NavigableViewModelBase.Async.cs:22` | `ExecuteWithErrorHandlingAsync` (protected) | README+ADR-0007 文档化为基类 API |
| D13 | 同文件:62 | `ExecuteWithErrorHandlingAsync<T>` (protected) | 同上 |
| D14 | 同文件:102 | `RunOnUIThread` (protected) | 0 调用（VM 实际用 `Services.UiThreadDispatcher`） |
| D15 | 同文件:110 | `RunOnUIThreadAsync` (protected) | 0 调用 |
| D16 | `NavigableViewModelBase.cs:284` | `AddDisposable` (protected) | 0 调用 |
| D17 | `Infrastructure/ViewModels/Base/DialogViewModelBase.cs:125` | `CloseDialogWithResult<T>` (protected) | 子类全用 `CloseDialog(parameters, result)` |
| D18 | 同文件:174 | `TryGetDialogParameter<T>` (protected) | 子类全用 `GetDialogParameter<T>` 双载 |

**B. DI 注册扩展 0 调用（3 项，Shell 已改走 SwitchingApiClient 路径，需人工确认是否遗留）：**

| # | 文件:行 | 类.方法 |
|---|---------|---------|
| D19 | `Foundation/Http/HttpClientApiClientExtensions.cs:43` | `AddHttpClientApiClient(this IContainerRegistry, int port)` |
| D20 | 同文件:86 | `AddHttpClientApiClient(this IContainerRegistry)` |
| D21 | `Foundation/Http/RefitApiClientExtensions.cs:34` | `AddRefitApiClient` |

**C. 接口成员 0 消费（14 项，接口契约变更属设计决策）：**

| # | 文件:行 | 类.方法 |
|---|---------|---------|
| D22 | `Contracts/Services/IDesktopCacheManager.cs:36` + `Foundation/Caching/DesktopCacheManager.cs:94` | `InvalidateAll`（接口+实现；缓存失效实际走 CacheEvents.InvalidatedEvent） |
| D23 | `Foundation/Security/IAuthenticationService.cs:18` + `AuthenticationService.cs:47` | `IsLoggedInAsync`（登录走 ILoginCoordinator） |
| D24 | `IAuthenticationService.cs:59` + `AuthenticationService.cs:221` | `CheckConnectionAsync`（健康检查走 IApplicationStateService） |
| D25 | `Contracts/Services/IStartupPipeline.cs:128` | `StartupStepResult.SkippedResult()`（startup 管道预留 API） |
| D26 | `MedicalCase/Models/WorkspaceState.cs:88`（及 `:85 EnterEditMode`，S0 未列） | `EnterReadOnlyMode`（FSM 重构后遗留，状态变更现全走 WorkspaceStateManager） |
| D27 | `MedicalCase/Services/MedicalCaseService.cs:240` | `GetByIdSimpleAsync`（public virtual 无 override、接口未声明） |
| D28 | 同文件:330 | `CancelMedicalCaseViaApiAsync`（同上） |
| D29 | `MedicalCase/ViewModels/Components/EditModeStateMachine.cs:93,170` | `CanFire`/`GetPermittedEvents`（接口 IEditModeStateMachine 成员；Fire:103 内联守卫未走 CanFire） |
| D30 | `Patients/Services/PatientService.cs:98` | `BatchDeletePatientsAsync`（仅 IPatientService:36 声明） |
| D31 | `Patients/Services/PatientCardReaderIntegration.cs:148` | `MatchPatientAsync`（仅测试调用） |
| D32 | `Users/ViewModels/Handlers/UserStatusHandler.cs:67` | `CanRestore`（RestoreAsync 走基类 ExecuteRestoreAsync 不经过它） |
| D33 | `Shell/Services/HealthCheck/ApiHealthMonitor.cs:75,103` | `StopMonitoringAsync`/`ResetCircuitBreaker`（StatusBarManager 只用 Start/ForceCheck/Dispose） |
| D34 | `Shell/Services/Login/LoginCoordinator.cs:183,271` | `HandleLoginSuccessAsync`/`GetDiagnostics`（登录走 LoginAsync） |
| D35 | `Shell/Services/Startup/StartupPipeline.cs:219`、`Shell/Services/Session/SessionLifecycleManager.cs:237` | `GetDiagnostics`×2（3 个 GetDiagnostics 接口成员全 0 消费） |

**D. 死代码 Model/映射（4 项）：**

| # | 文件:行 | 类.方法 |
|---|---------|---------|
| D36 | `Patients/Models/Items/PatientItem.cs:163` + 整类 | `UpdateFromDto`（整类无实例化——死类） |
| D37 | `Users/Models/Items/UserItem.cs:240` + 整类 | `UpdateFromDto`（同上） |
| D38 | `Controls/HerbList/HerbListControlViewModel.cs:249,296` | `MoveItem`/`GetNextEmptySlotIndex`（同文件 RequestNewSlot 被 code-behind:319 调用证明类活，仅此 2 方法断链） |
| D39 | `Formula/ViewModels/FormulaMasterDetailViewModel.cs:354` | `OnSelfPropertyChanged`（空体逻辑死；ctor:78 订阅 + Dispose:363 退订仍在） |

**E. 接口契约成员 0 业务调用（Core 侧补充，general-7 细复核确认——删除需同步接口+实现类，属设计决策）**

> general-7 对 Core 6 项目逐符号复核，确认 **39 个接口契约成员**业务 0 调用（general-5 概览未覆盖的部分）；其中「仅测试引用」3 项、「重构后遗留 API」集群最集中。与 §5.4 Service 0 消费成员（22 接口+6 死实现）不重叠（§5.4 是模块 Service 接口，此处是 Core 基础设施接口）。

| # | 接口（文件:行） | 成员 | 备注 |
|---|----------------|------|------|
| D43 | `Security/IAuthenticationStateMachine.cs:46` + `AuthenticationStateMachine.cs:180` | `FireAsync` | 消费方全用同步 `Fire`（LoginCoordinator.cs:106 等 7 处）——纯异步包装残留 |
| D44 | `ApiClient/IApiClientHerbs.cs:114` + HerbsHttpApiClient.cs:68 + HerbApiClient.cs:87 | `GetCategoriesAsync` | 远程实现直接 `throw NotSupportedException`（HerbApiClient.cs:88）——整链预留 |
| D45 | `ApiClient/IApiClientFormulas.cs:136` + FormulasHttpApiClient.cs:78 + FormulaApiClient.cs:100 | `GetCategoriesAsync` | 同上（FormulaApiClient.cs:101 throw） |
| D46 | `ApiClient/IApiClientRegistrations.cs:82,88,94` + 2 实现 | `GetRegistrationsAsync`/`QuickVisitAsync`/`DeleteRegistrationAsync` | 接口+双实现，0 调用 |
| D47 | `Performance/IPerformanceMonitor.cs:30,35,42,47,53` + PerformanceMonitor.cs:108-154 | `RecordMemoryBaseline`/`GetMemorySnapshots`/`GetMetric`/`GetAllMetrics`/`GenerateReport` | PerformanceMonitor 仅 StartTiming/StopTiming 被消费（StartupPipeline.cs:260/270） |
| D48 | `Roles/IRoleRegistry.cs:27,33` + RoleRegistry.cs:57,63 | `GetAllDefinitions`/`IsRegistered` | 消费方用 GetDefinition/GetModulesForRole |
| D49 | `Services/IApiHealthMonitor.cs:60,66` + ApiHealthMonitor.cs:75,103 | `StopMonitoringAsync`/`ResetCircuitBreaker` | 同 D33（两子审独立确认） |
| D50 | `Services/ICommonDialogService.cs:30,70,78,87` + CommonDialogService.cs:23-102 | `ShowInfoAsync`/`ShowInputAsync`/`ShowOpenFileDialogAsync`/`ShowSaveFileDialogAsync` | 业务用 ShowConfirm/ShowWarning/ShowError |
| D51 | `Services/IConnectionModeService.cs:78` + ConnectionModeService.cs:123 | `TestLocalConnectionAsync` | 业务用 TestRemoteConnectionAsync |
| D52 | `Services/IDesktopCacheManager.cs:36` + DesktopCacheManager.cs:94 | `InvalidateAll` | 同 D22（两子审独立确认） |
| D53 | `Services/ILoginCoordinator.cs:59,70` + LoginCoordinator.cs:183,271 | `HandleLoginSuccessAsync`/`GetDiagnostics` | 同 D34 |
| D54 | `Services/INavigationCoordinator.cs:121,126` + NavigationCoordinator.cs:294,297 | `SubscribeToRegionCollection`/`UnsubscribeFromRegionCollection` | 0 调用 |
| D55 | `Services/IPatientService.cs:36` + PatientService.cs:98 | `BatchDeletePatientsAsync` | 同 D30 |
| D56 | `Services/ISessionManager.cs:48,70,75,80` + SessionManager.cs:43,73-75 | `SetSession`/`HasRole`/`IsAdmin`/`GetCurrentUserRoleDisplay` | 业务用 HasPermission/ClearSession |
| D57 | `Services/IStartupPipeline.cs:68` + StartupPipeline.cs:219 | `GetDiagnostics` | 同 D35 |
| D58 | `Services/IUserNotificationService.cs:34` + UserNotificationService.cs:68 | `ShowInfoAsync` | 业务用 ShowError/ShowWarning |
| D59 | `ExceptionHandling/IDesktopExceptionHandler.cs:24,34,47,66,71` | `LogException`/`CanRetry`/`UnregisterGlobalExceptionHandlers`/`SafeExecuteAsync`×2 | 仅 RegisterGlobalExceptionHandlers 被 ErrorHandlingStartupStep.cs:42 调用 |
| D60 | `Services/Interfaces/IAsyncExecutor.cs:16-76` + AsyncExecutor.cs:22-139 | `ExecuteSafelyAsync`×2/`ExecuteWithRetryAsync`×2/`ExecuteOnUIThread`/`ExecuteOnUIThreadAsync`/`ExecuteWithTimeoutAsync`（7） | **重构后遗留 API 集群**——被 UnifiedApiClientExtensions/ServiceEventBridge 取代 |
| D61 | `Services/Interfaces/ISelectionService.cs:44,50` | `SelectMultiple`/`ToggleSelection` | 业务用 Select/SelectedItems |
| D62 | `Services/Interfaces/IDialogManager.cs:36,53,62` + DialogManager.cs:43,93,120 | `ShowInfoAsync`/`ShowInputAsync`/`ShowDialogAsync` | 业务用 ShowConfirm/ShowError/ShowWarning |
| D63 | `Services/Notifications/INotificationService.cs:37,57,62` + NotificationService.cs:75,122,143 | `ShowInfoAsync`/`ShowLoading`/`HideLoading` | 同上 |
| D64 | `CardReader/Abstractions/ICardReaderFactory.cs:14` + CardReaderFactory.cs:55 | `GetSupportedReaders` | 0 调用 |
| D65 | `Security/ICredentialVault.cs:78,84,91` | `VerifyIntegrityAsync`/`MigrateOldFormatAsync`/`HasValidTokenAsync` | 仅测试（CredentialVaultTests.cs:227-314）；业务用 Save/Get/ClearPassword |
| D66 | `Security/IPhotoStorageService.cs:22,29,34` | `LoadPhotoAsync`/`DeletePhotoAsync`/`PhotoExists` | 仅测试；业务用 SavePhotoAsync |
| D67 | `Security/ILogoutService.cs:37` | `ProcessPendingServerLogoutsAsync` | 仅测试（LogoutServiceTests.cs） |
| D68 | `Security/ITokenValidator.cs:28` + LocalTokenValidator.cs:177 | `ValidateAndGetUserInfoAsync` | 业务用 ValidateTokenAsync（另一方法） |
| D69 | `Security/ITokenManager.cs:39,44,50,57` | `SetTokens`/`ClearTokens`/`IsTokenValid`/`IsTokenExpiringSoon` | 仅 AccessToken 属性被 SignalRClient.cs:60 使用 |
| D70 | `Modules/IModuleLoadingService.cs:22,39` | `GetLoadedModules`/`LoadModulesAsync` | ModuleLazyLoader 用 IsModuleLoaded/LoadModuleAsync |
| D71 | `Printing/Services/PrescriptionPrintService.cs:173` + IPrintService.cs:40 | `BatchPrintAsync` | PrescriptionPrintHandler 只调 PreviewAsync(:97)/ExportAsync(:148) |
| D72 | `CardReader/Native/HuaDaNativeMethods.cs:48,87-157` | `HD_ReadCard`/`GetCertNo`/`GetSex`/`GetNation`/`GetBirth`/`GetAddress`/`GetDepartemt`/`GetEffectDate`/`GetExpireDate`/`GetBmpFileData`/`GetBmpFile`/`PtrToString`/`IsDllAvailable`（13 extern） | ⚠️ **extern P/Invoke 硬件 SDK 预留**，adapter 只用 HD_InitComm 等 5 个；删除前须确认无外部 DLL 依赖 |

> 已排除的 S0 候选（复核为活）：`CommandResult` 隐式 bool 运算符（`if (result)` ReceptionistHomeViewModel.cs:176/313/334 隐式调用）、`ClientErrorMessageMapper` 3 方法（S1 已确认，general-5 判死为**唯一准确口径**——见 §7.1 注）、`WpfUiThreadDispatcher` public ctor（DI `ViewModelServicesExtensions.cs:28`）、`HttpApiClientBase` 10 个 protected helper（11 子类 adapter 调用）、全部 Converters `ConvertBack`（IValueConverter 框架）、全部 `IsNavigationTarget`/`ConfirmNavigationRequest`（INavigationAware 框架）、LocalWebAPI 全 56 路由 action（HTTP 反射）、`TokenRefreshResult` 私有 ctor（工厂 Succeeded/Failed 调用）、`PrintResult` 私有 ctor（工厂）、`PrescriptionPrintService` 族构造器（DI 注册 `PrintingModule.cs:24-27`）。

### 7.3 翻案为活的重要候选（S0 标死、实际活——方法级核实）

> 以下为 S0 候选中最典型的翻案样例，证明「VM/控件方法必须查 XAML/特性/订阅点才能判死」：

| 激活途径 | 代表（文件:行） |
|---------|----------------|
| `[RelayCommand]` 源生成（方法名=命令目标，无需查 XAML 即活） | Admin 6×NavigateToXxx、SystemSettings 4 方法、Auth 全 19、Clinical 27（ReceptionistHomeView.xaml:119-259 等）、Formula/Herbs CanToggleStatus/CanCopy、MedicalCase ReportsHome.GoToToday、Patients CanReadCard（PatientMasterDetailControl.xaml:117）、Registrations CreateRegistration、Shell AccountSettings 3 方法、MainWindowViewModel 2 方法 |
| 手工 `new AsyncRelayCommand/RelayCommand/DelegateCommand(方法名)` | LoginViewModel 3（:165-168，AGENTS.md 明确 manual）、MenuManager 7×Execute*（ctor:104-114）、MedicalCaseCommandsViewModel 9×Execute*（ctor:78-86）、SearchBox.ExecuteClear、RegistrationList.CreateRegistration |
| XAML 事件处理器（XAML 反射调用） | `PatientSelectionControl_PatientDoubleClicked`×2（PatientSelectionView.xaml:132/ClinicalWorkspaceView.xaml:41）、`PatientDataGrid_MouseDoubleClick`（PatientSelectionControl.xaml:59）、HerbItemControl 8 事件、HerbListControl 3 事件 |
| DP `PropertyMetadata` 回调 | `OnNavigationPathChanged`、`OnAllHerbsChanged`×2、`OnStatusChanged`/`OnBadgeTypeChanged`、`OnCurrentStepChanged`、`OnShowCheckBoxColumnChanged`/`OnSelectedItemsChanged`、`OnBoundPasswordChanged`（DependencyProperty.Register 回调参数） |
| 附加属性访问器（XAML `behaviors:PasswordBoxHelper.BoundPassword=` 等 6+ 处） | `GetBoundPassword`/`GetShowCheckBoxColumn`/`SetShowCheckBoxColumn`/`SetSelectedItems` |
| 事件订阅（`+=`/EventAggregator/Timer/SignalR） | `OnApiStatusChanged`/`OnConnectionModeChanged`×2、MasterDetailLayout 3、LoadingOverlay.OnDelayTimerTick、`OnTick`×2、ShellEventCoordinator 4、SignalRClient 2、`AutoReadCallback`（Timer）、`OnMonitorTick`（Timer）、`OnPreProcessInput`、`OnReconnectedAsync`/`OnClosedAsync`、`OnRegistrationRefreshed`、`OnEditStateChangedFsm`（FSM StateChanged :272） |
| `[ObservableProperty] partial void On*Changed` 源生成回调 | OnTestStatusChanged×2、OnSelectedPatientChanged×3、OnRememberUsernameChanged 等 20+ 个 |
| CanExecute 经 `nameof()` 引用 | CanTestConnection×2、CanStartConsultation、CanToggleStatus×2、CanCopyHerb/Formula、CanViewMedicalRecords 等 20+ 个 |
| 框架/接口分发 | CreateInstanceCore（Freezable）、CreateShell/InitializeShell（PrismApplication）、各 RegisterTypes（Prism IModule）、各 IsNavigationTarget（INavigationAware）、各 ConvertBack（IValueConverter）、LocalWebAPI 全 56 路由 action、`Handle`（MediatR）、StringResources ctor（ResGen）、AuthenticationStateMachine internal 测试 ctor、PrintResult 私有 ctor（工厂）、HttpApiClientBase 全部 protected 成员（12 子类）、CommandResult bool 运算符（`if (result && result.Data != null)` 隐式） |

### 7.4 D 级统计

| 项 | 数值 |
|----|------|
| S0 Desktop 候选总数 | **770**（§2 高置信 243 + §3 714 含 LocalWebAPI 56） |
| 符号级复核 | 全部 770 项过检：§2 高置信 243/243 全覆盖（Core 侧 general-7 436 项全量 + 模块/Roles/Shell 侧 general-8 137 项）+ §3 public 候选逐一/批量归类 |
| **确认死 N** | **78 符号 + 2 死类**（可安全删 14：D1-D11/D40-D42；需人工复核 64：基类 API 7 + DI 扩展 3 + 死 Model/空体 4 + 接口契约成员 39（D43-D71 去重）+ 模块侧 public 0 调用 18 + extern SDK 1 组） |
| 翻案为活 M | ≈ **692**（~90%+）——S1/S2 教训在本仓 100% 复验 |
| 未判（需人工复核） | 7.2 全部 64 项按「先文档后代码」流程处理；接口契约成员删除必须同步接口+全部实现类（部分涉及 Refit/HttpClient 双实现），建议分组评审禁止单点删除 |

> 注：两子审数字口径差异说明——general-5（全量概览）确认 28 项、general-7（Core 6 细复核）确认 61 项（18 可安全删 + 4 public + 39 接口）、general-8（模块/Roles/Shell）确认 1 逻辑死 + 18 public 0 调用；本表按「两子审独立确认即采信、冲突以主审 grep 验证为准」去重合并。冲突案例：ClientErrorMessageMapper 3 方法（general-5 判死 ✅ 经主审验证；general-7 误标活）；ApiHealthMonitor 2 方法/InvalidateAll/GetDiagnostics 族（两子审独立确认一致）。

---

## 8. 合并候选分析（方法级证据，供 S4 整合）

### 8.1 Core 项目合并评估

**依赖图（csproj 硬证据）**：
```
Contracts → Shared.Models
Foundation → Contracts + Shared{Models,ExceptionHandling,Configuration,Logging}（0 处 using System.Windows ✅ 无头）
Controls → Contracts + Foundation + Shared.Models（不引用 Infrastructure）
Infrastructure → Controls + Foundation + Contracts + Shared×4
Printing → Infrastructure（单向）
LocalWebAPI → Contracts + Shared×2 + Entities + Server/Infrastructure + 8 Server Modules（跨层例外 ADR-0010）
Shell → 全部
```

| 候选 | 评估 | 方法级证据 | 结论 |
|------|------|-----------|------|
| **Controls+Printing → Infrastructure** | 依赖上可行（无环）；Controls 是纯表现层（22 XAML 控件+5 主题字典+14 Converters），Infrastructure 是 WPF 服务/导航层，职责正交 | 拼音搜索逻辑两处重复：Controls `HerbItemControlViewModel.cs:235`（FilterHerbs）vs Infra `HerbItemViewModelBase.cs:128`（FilterHerbs），同构约 30%；其余无继承重复 | **不推荐整体合并**——合并仅省 1 项目但使最大项目更臃肿；真正收益在「去重拼音搜索逻辑」，无需合并项目即可完成 |
| **Printing → Infrastructure** | 仅依赖 Infrastructure；61 方法自包含、QuestPDF 内聚 | `Printing.csproj` 仅 1 个 ProjectReference | 可合并但收益小（少 1 项目，QuestPDF/SixLabors 依赖上移）——建议保持 |
| **Foundation/Infrastructure 边界** | 边界清晰：Foundation 无头（0 WPF 引用）、单向依赖 | 异常/日志关注点跨边界分裂（mapper 在 F、handler 在 I）——S1 E4/E5/M1/M4 收敛后自清 | **维持边界** |
| **Contracts 去向** | 依赖图叶子，被全部项目 + LocalWebAPI 引用 | `LocalWebAPI.csproj` 直引 Contracts（API 接口 IApi/IApiClient/IRepository 复用） | **保持独立**——并入 Foundation 使 LocalWebAPI 被迫引用带实现的 Foundation |

### 8.2 模块合并候选（Herbs+Formula 可行，Auth 不合并）

**HerbsMasterDetailViewModel vs FormulaMasterDetailViewModel（11/13 同名 override，同构 ≈ 85%）**：

| 方法 | HerbMasterDetailViewModel.cs | FormulaMasterDetailViewModel.cs |
|------|------------------------------|--------------------------------|
| GetDetailDisplayName | :37 | :47 |
| LoadListAsync | :71 | :84 |
| LoadDetailAsync | :104 | :118 |
| CreateNewDetail | :153 | :141 |
| SaveDetailAsync | :162 | :149 |
| DeleteItemAsync | :202 | :208 |
| ToggleStatusAsync/CanToggleStatus | :221/:231 | :228/:238 |
| Copy{Herb\|Formula}Async | :235 | :242 |
| InvalidateCachesAsync | :253 | :281 |
| RestoreItemAsync | :260 | :288 |
| SearchByCategoryAsync | :267 | :314 |

- 11 个同名同签名 override 均来自 `MasterDetailViewModelBase.cs:238-242,248-269` 同一组抽象钩子；差异仅 Formula 多 AddHerb(:295)/DeleteHerb(:304)/LoadAllHerbsAsync(:334)/OnSelfPropertyChanged(:354)。
- **EditorViewModel 三件套 6/6 骨架同构**：InitializeFromDto/InitializeForNewCase/GetXData/Validate/Reset/OnXPropertyChanged——HerbEditor:31,59,69,90,96,103；FormulaEditor:40,83,108,125,146,156；UserEditor:35,59,68,87,95。
- **Service 层同构 >90%**：RemoteHerbService.cs:47,67,87,111… / FormulaService.cs:44,62,110… / RemoteUserService.cs:48,68,88… 全为同一 `CommandResult<T>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("操作", ex))` CRUD 模板。
- **Module.cs RegisterTypes 结构 100% 一致**：HerbsModule.cs:27-43、FormulaModule.cs:26-48、UsersModule.cs:28-45、PatientsModule.cs:33-53（ViewModelLocationProvider.Register + AddMasterDetailServices + Register<MasterDetailVM> + Register<EditorVM> + Register<Handler>）。

**结论**：
- **Herbs+Formula 合并可行且推荐**——Desktop 侧同构 ~85%（远高于 Server 侧 43%，因统一 MasterDetailViewModelBase 模板 + 复制-粘贴式 Module.RegisterTypes）；需处理 `FormulaModule.cs:18 [ModuleDependency("HerbsModule")]` 与 MedicalCase 对两者的引用（`MedicalCaseModule.cs:26-27`）。
- **Auth 不合并**——`LoginViewModel.cs:37-308` 是 NavigableViewModelBase 登录流（BackgroundInit/ExecuteLogin/ExecuteOpenSettings…），与 MasterDetail 模板完全不同构；Users 与 Auth 的合并证据在 Server 侧（S2 §8.2），Desktop 侧 Users 是标准 MD 模板模块，不并入 Auth。

### 8.3 Roles 职责评估（边界清晰）

- **Admin**（`AdminModule.cs:18-29`）：AdminHomeViewModel（7 个 NavigateTo* 菜单命令 `:69-115`）+ SystemSettingsViewModel（Initialize/LoadClinicSettings/Save/Reset/BrowseBackupPath/LoadServerConfig/SaveServerConfig/ValidateConfig `:159-405`）= **导航入口 + 系统配置**。
- **Clinical**（`ClinicalModule.cs:22-44`）：ClinicalHome + PatientSelection + MedicalCaseWorkspace + Receptionist 子域（StartMedicalCase/HandleSuspendedCase/CreateAndNavigateToNewMedicalCase、CardReader 5 命令、PendingQueue 4 方法、MedicalCaseWorkspace FSM 编辑状态机）= **临床工作流**。
- **边界清晰**：两角色方法集零重叠（Admin 无临床工作流方法、Clinical 无系统设置方法）；角色项目是「导航/布局组合层」，业务逻辑全在 Modules/Infrastructure 的 MasterDetailControl 薄包装里（如 `Admin/Views/UserManagementView.xaml.cs`）。
- ⚠️ 文档-代码冲突：`Roles/AGENTS.md` 声称「MUST NOT reference business modules directly」，但 Admin.csproj/Clinical.csproj 实际直引 5 个模块项目（W4）。

### 8.4 DI/ViewModelLocator 注册方法重复模式

全 Desktop 38 处注册调用，两个固定模板：

| 模板 | 形态 | 证据 |
|------|------|------|
| **MasterDetail 模板（5/5 模块一字不差）** | `ViewModelLocationProvider.Register(typeof({X}MasterDetailControl).ToString(), typeof({X}MasterDetailViewModel))` + `AddMasterDetailServices<TList,TDetail>`（`ViewModelServicesExtensions.cs:77-86` 注册 4 个泛型服务）+ Register<MasterDetailVM> + Register<EditorVM> + Register<Handler> | Herbs:29 / Formula:28 / Patients:35 / Users:33 / MedicalCase:37 |
| **导航/对话框模板** | RegisterForNavigation + RegisterDialog | Auth:36,39,42；Registration:40,43；MedicalCase:50-52,56,59；Clinical:30-43；Admin:25-28；Sysadmin:29-31 |

- Shell 聚合：`App.xaml.cs:90-113` RegisterTypes（RegisterAllServices 聚合 5 个 Extensions）+ `ConfigureViewModelLocator:116-121`。
- 结论：注册代码已模板化但仍是复制-粘贴（5 处几乎相同的 RegisterTypes 体）；可提取 IModule 基类但**非合并项目的前置条件**。

---

## 9. 与 S1 日志/异常专项衔接（Desktop 侧方法迁移落点）

> 本报告核实 Desktop 侧 4 个待迁移文件的方法清单与调用点，为 S1 迁移计划 M1/M3/M4/E4/E5 提供 Desktop 落点。目标位置（Shared.Logging/Http、Shared.Logging/Bootstrap、Shared.ExceptionHandling/Mapping）当前均不存在，需新建。

### 9.1 日志迁移落点（S1 §2 M1/M3/M4）

**① LoggingHttpHandler** — `Core/LYBT.Desktop.Foundation/Http/LoggingHttpHandler.cs`（95 行）
- 方法：ctor(:17)、`SendAsync`(:22，traceparent :31、请求日志 :41、响应日志 :54、错误 Body 脱敏 :66、异常日志 :86)。
- 调用点：**唯一实例化** `Shell/Extensions/UnifiedApiClientExtensions.cs:85-87`（手写链 HttpClientHandler→TokenRefreshHandler→AuthorizationMessageHandler→LoggingHttpHandler→HttpClient，仅 Remote 工厂 :64-94）；`RefitApiClient.cs:29,55`、`RefitApiClientExtensions.cs:21` 仅为文档注释；Local 模式链（LocalWebApiHttpClientFactory :97-101）**无** LoggingHttpHandler。
- **M4 落点**：Shared.Logging/Http/（新建），DI 解析点保留在 UnifiedApiClientExtensions:86；顺带决策「Local 链是否也挂日志 handler」。

**② DesktopSerilogConfiguration** — `Core/LYBT.Desktop.Infrastructure/Logging/DesktopSerilogConfiguration.cs`（108 行）
- 成员：LogBasePath(:19)/LogFilePath(:27)/CorrelationIdProvider(:39)/`CreateLoggerConfiguration`(:46)/`Initialize`(:76)/`CloseAndFlush`(:85)/EnsureLogDirectoryExists(:93)。
- 调用点：`Shell/App.xaml.cs:48`（OnStartup→Initialize）、`:75`（OnExit→CloseAndFlush）。
- 已用共享件：UseSharedLogging(:57)/SensitiveDataDestructuringPolicy(:60)/ActivityCorrelationIdProvider(:34)——迁移残留仅剩「bootstrap 本身」。
- **M1 落点**：Shared.Logging/LoggingBootstrap（新建），App.xaml.cs:48/75 改调 LoggingBootstrap.Initialize/CloseAndFlush。

**③ 附：Shell LoggingRegistrationExtensions** — `Shell/Extensions/LoggingRegistrationExtensions.cs`
- `RegisterLogging`（ILoggerFactory 单例 + ILogger<>）被 `ServiceCollectionExtensions.cs:49` 调用。
- **M3 落点**：由 `AddLybtLogging` 的 DI 注册接管，源文件删除。

### 9.2 异常统一落点（S1 §3 E4/E5）

**④ DesktopExceptionHandler** — `Core/LYBT.Desktop.Infrastructure/ExceptionHandling/DesktopExceptionHandler.cs`（242 行）
- 方法：HandleException(:23)/HandleExceptionAsync(:29)/LogException(:36)/GetUserFriendlyMessage(:51)/CanRetry(:57)/`RegisterGlobalExceptionHandlers`(:74)/UnregisterGlobalExceptionHandlers(:95)/OnUnhandledException(:113)/OnUnobservedTaskException(:134)/LogExceptionInternal(:155)/DetermineLogLevel(:174)/HandleException<T>(:192)/HandleExceptionWithResult(:204)/SafeExecuteAsync×2(:216,:229)；接口 `IDesktopExceptionHandler.cs:14-71`（14 成员）。
- 挂接点：DI 注册 `Shell/Extensions/ServiceCollectionExtensions.cs:111`；启动挂接 `Shell/Services/Startup/Steps/ErrorHandlingStartupStep.cs:42`（Order=10、IsRequired=true）；事件挂接 AppDomain.UnhandledException(:82) + TaskScheduler.UnobservedTaskException(:83)。
- **E5 落点**：挂接部分保留 Desktop；`GetUserFriendlyMessage(:51)` 改委托共享 `IExceptionMessageMapper`。

**⑤ ClientErrorMessageMapper** — `Core/LYBT.Desktop.Foundation/ExceptionHandling/ClientErrorMessageMapper.cs`（410 行）
- 公开方法：GetUserMessageFromStatusCode(HttpStatusCode)(:44)/GetUserMessageFromStatusCode(int)(:54)/GetUserMessageFromErrorCode(string)(:84)/GetUserMessageFromErrorCode(int)(:106)/GetUserFriendlyMessage(:132)/GetSafeOperationFailureMessage×2(:333,:348)/GetSafeMessageWithTrackingCode(:365)/GetMessageWithTrackingCode(:381)/GetShortTrackingCode(:395)/GetFullTrackingCode(:404)；静态属性 TraceIdProvider(:360)。
- 引用面：**138 处调用、约 35 个文件**，横跨 Core(Foundation×2、Infrastructure×5)/Modules(6)/Roles(3)/Shell(10)——**Desktop 最高 fan-in 工具类**；已委托 ErrorMessages 单一数据源(:111)。
- **E4 落点**：Shared.ExceptionHandling/Mapping/IExceptionMessageMapper（新建）；**138 处调用面建议保留静态壳委托**（避免全量改调用）；注意 §7.1 D2-D4 三个断链方法（GetSafeMessageWithTrackingCode/GetMessageWithTrackingCode/GetFullTrackingCode）随迁移一并删。
- ⚠️ **迁移联动**：日志/异常收敛需 Shared.Logging/Shared.ExceptionHandling 引入 ASP.NET Core 依赖（`IExceptionHandler`/中间件），S1 已标注需走技术引入治理 + 更新 `P05b` 架构测试。

### 9.3 策略缺口（Desktop 侧优先修复项，与 S2 C3 一致）

- `LocalJwtConfig.cs:70-90` 补注册 `DoctorOrAdminOrReceptionist`（照抄 Remote `AuthenticationServiceCollectionExtensions.cs:132-134` 定义）；裁决 Reports 双树策略取一统一（Local `ReportsController.cs:13` vs Remote `ReportsController.cs:19`）。可加架构测试「Local 引用策略 ⊆ Local 注册策略」。

---

## 10. 统计汇总

| 维度 | 数值 |
|------|------|
| 审查项目/方法 | **16 项目 / 2987 方法**（Contracts 487 / Controls 149 / Foundation 511 / Infrastructure 596 / Printing 61 / LocalWebAPI 109 / Auth 57 / Users 77 / Patients 73 / Herbs 59 / Formula 77 / MedicalCase 240 / Registrations 58 / Admin 61 / Clinical 140 / Shell 232） |
| C 级重复 | **~53 组/处**：VM/命令 8 类（44 VM 审出 30 处，13 组跨模块同名命令）+ 映射 5 + 双轨 6 + Repo/Service 5 + 文档漂移 6 |
| D 级死方法 | **确认死 78 符号 + 2 死类**：可安全删 14（D1-D11/D40-D42）+ 需人工复核 64（基类 API 7 / DI 扩展 3 / 死 Model 4 / 接口契约成员 39+14 跨两子审去重 / Printing 1 / extern 13 项 1 组）；S0 Desktop 候选 **770** 项中 ~96% 翻案为活（RelayCommand/XAML/DP 回调/事件订阅/接口分发）——S0 机器初筛对 `[RelayCommand]`/`[ObservableProperty]` 方法系统性误报，**凡 VM/控件方法必须查特性/XAML/订阅点才能判死** |
| E 级可集中 | 日志 3（LoggingHttpHandler/DesktopSerilogConfiguration/LoggingRegistration）+ 异常 2（ClientErrorMessageMapper/DesktopExceptionHandler 委托）+ 策略 1；**与 S1 衔接落点见 §9（M1/M3/M4/E4/E5）** |
| 双轨 | 12v12 中 Users/Herbs/Patients 近收敛；**Reports 内联复刻（3 方法逐行）**、**Auth 本地 CQRS 复刻（4 handler）**、Formulas.Clone 内联、Deploy 桩；**P0 策略缺口**（DoctorOrAdminOrReceptionist 未注册致本地 3 模块不可用）+ Reports 策略分歧 |
| Repo/Service | 7 仓储全继承基类（无裸实现 ✅）；MedicalCase/Registration 手写 CRUD = 基类逐行复刻；8 组镜像方法；**Service 28 符号 0 消费**（接口 22 + 死实现 6，含整条未接线导入/导出/批量删除链） |
| 映射 | 5 Mapperly + 13 手写并存；Shared DtoConversionExtensions 唯一活跃调用点 MedicalCaseCommandService.cs:46；P2-11 立场建议「列表直绑 DTO + 详情 Model + 保存 Mapperly」 |
| 合并候选 | **Herbs+Formula 合并可行且推荐**（Desktop 侧 ~85% 同构，远高于 Server 43%）；Controls+Printing 不合并；Contracts 保持独立；Foundation/Infrastructure 边界维持；Roles 边界清晰 |
| 遗留风险 | ① **P0：Local 缺 DoctorOrAdminOrReceptionist 策略**（本地 3 模块不可用，建议最先修）；② MedicalCase 保存链路手写映射与 Mapperly 并存（C-M1）；③ 28 个 0 消费 Service 成员需「补 UI 或删」裁定；④ 6 处文档-代码漂移需同步（W1-W6）；⑤ 迁移日志/异常需先过技术引入治理；⑥ Remote Reports 5 死亡端点待产品确认 |

**未 commit**：本报告由技术总监统一提交；后续执行批次（策略修复/日志异常收敛/VM 模板收敛/Herbs+Formula 合并/死方法清理）需另立任务书并经审批。



