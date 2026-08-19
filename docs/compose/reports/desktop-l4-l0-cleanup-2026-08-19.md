# Desktop L4-L0 死代码清理 + Review 问题修复报告（2026-08-19）

> 任务书：`.hermes-task-l4-l0-cleanup.md`（已删除）
> 范围：全 Desktop 项目 L4（HTTP Client）→ L0（ViewModel/View）
> 前置：desktop-refactoring-review-2026-08-19.md 的 4 个遗留问题（R1-R4）

---

## 一、Review 问题修复

### R1 ✅ 模块 README/AGENTS 旧名同步（11 处）

P1-2 重命名 `Remote*Service` → `*Service` 后，4 个文件残留旧名，本次全部同步：

| 文件 | 处数 | 内容 |
|------|------|------|
| `Modules/LYBT.Desktop.Users/README.md` | 4 | 目录结构/注册表/章节名/已知陷阱 |
| `Modules/LYBT.Desktop.Registrations/README.md` | 3 | 目录结构/注册表/章节名 |
| `Modules/LYBT.Desktop.Patients/README.md` | 2 | 目录结构/核心组件表 |
| `Modules/LYBT.Desktop.Registrations/AGENTS.md` | 2 | Key Files/Subdirectories |

**验证**：`grep Remote\w+Service src/Client/Desktop` 清零。`docs/compose/reports/` 历史报告保留（过程记录，按「文档是字典不是过程」规则不改）。

### R4 ✅ CardReaderPureTests.MaskIdNumber 存量失败修复

**根因**：`PrivacyHelper.MaskIdNumber` 与测试期望不符——
- 边界 `<= 10`：10 位号（`1234567890`）不脱敏，测试期望 `123456****7890`
- `?? string.Empty`：null 输入返回空串，测试期望 null

**修复**（`PrivacyHelper.cs`）：
- 边界 `<= 10` → `< 10`（10 位及以上脱敏，`[..6]+"****"+[^4..]` 对 10 位号恰好正确）
- null/空白/不足 10 位原样返回（不再吞 null）
- 返回类型 `string` → `string?`（3 个透传 VM 同步：CardReaderViewModel/PatientCardReaderViewModel/ReceptionistHomeViewModel）

**验证**：CardReaderPureTests 全绿（含 2 个此前失败的用例）。

### R3 ✅ AuditLogService/ReportService 单测补齐

| 测试文件 | 用例 | 覆盖 |
|----------|------|------|
| `AuditLogServiceTests.cs` | 3 | 成功返回数据 / 空分页 Succeeded（ADR-0020 读契约）/ 异常→Failed |
| `ReportServiceTests.cs` | 6 | 3 报表 ×（成功→Succeeded / 失败→Failed / 异常→Failed） |

P0-2 的行为变化（AuditLogService 失败时返回空分页而非 Failed）现在有测试锁定。

### R2 ✅ 13c-current-status 追加条目

`13c-current-status.md` 追加 #126 行（本批次全量记录，含 commit 追踪）。

---

## 二、L4-L0 各层扫描与清理

### L4（HTTP Client 层）

| 项 | 处置 |
|----|------|
| `HttpApiClientBase.WrapSuccess<T>` 泛型重载 | ✅ 删（零调用，非泛型重载在用） |
| `HttpApiClientBase.PostRawAsync<T>` | ✅ 删（零调用，无 Server 契约对应） |
| `RefitApiClient` 的 `using LYBT.Desktop.Contracts.Api` | 保留（`RestService.For<IAuthApi>` 确实使用） |
| Remote vs Local 适配器重复（10 对样板/URL 拼装/分页风格） | 登记（结构性重复，重构需专项，非死代码） |

### L3（API Client 契约层）

| 项 | 处置 |
|----|------|
| `IMedicalCaseApi.cs:169-172` 空文档注释（批量获取医案详情） | ✅ 删（无对应方法残留） |
| `IRegistrationApi.cs:54-57` + `IApiClientRegistrations.cs:68-70` 空文档注释（快速就诊） | ✅ 删（QuickVisit 已删除的注释残留） |
| `ValidateTokenAsync`（远程端点版，全链无消费） | 保留（Server `/auth/validate` 端点在线；客户端用本地 LocalTokenValidator，契约删除需 Server 协调） |
| `GetValueAsync`/`SetValueAsync`（Configuration 单值方法，客户端未用） | 保留（Server `GET/{key}`/`PUT/{key}` 端点在线） |
| `GetPendingValidationAsync`/`ValidateHerbAsync`、`GetPermissionsAsync` | 保留（Server 端点在线；关联 ViewModel 已删，属「客户端侧死契约」，登记后续与 Server 协调） |

### L2（Repository 层）

| 项 | 处置 |
|----|------|
| `ApiClientRepositoryBase.HandleException` 冗余 `args` 参数 + 恒等三元分支 | ✅ 删（两个分支完全相同，`args` 从未使用） |
| `ExecuteBatchDeleteAsync` 上游死亡（5 个 BatchDelete 调用方均零调用） | 保留（方法本身被接口链引用，删除需连带接口——登记） |
| Repository 层 ~15 处重复 try/catch 模板 | 登记（可提取 `ExecuteNullableAsync` 助手，重构专项） |
| Formula/Herb/Patient/User/Registration 的 BatchImport/Export 等公共方法零调用 | **不删**——对应 Server 端点（import-template/export/batch-import），是远程 API 契约的客户端映射，删除破坏契约完整性 |

### L1（Service 层）

| 项 | 处置 |
|----|------|
| 6 个 Service 同构 try/catch→CommandResult 模板（~15 处） | 登记（`CrudServiceBase.ExecuteAsync` 已存在，可收敛未继承基类的服务） |
| `MedicalCaseService` 门面 15+ 死成员 + `_sessionManager` 死字段 | **登记不删**——量大关联深（跨 Query/Command/Lifecycle 三接口），删除需专项任务逐个确认调用链 |
| `MedicalCaseRepository.CancelMedicalCaseAsync` 恒返回 null（注释自述 "Always returns null regardless of outcome"） | ⚠️ **疑似 bug** 登记——成功也返回 null，导致 LifecycleService 的 `data != null` 永远为假，取消永远报失败。需专项验证 |

### L0（ViewModel/View 层）

| 项 | 处置 |
|----|------|
| `MedicalCaseMasterDetailViewModel._loggerFactory` | ✅ 删（只赋值不读取；构造参数保留——仍传给子 VM） |
| `AdminHomeViewModel._dialogService` | ✅ 删（只赋值不读取） |
| `ClinicalHomeViewModel._dialogService` | ✅ 删（只赋值不读取） |
| `ConfirmationDialogViewModel.IsSoftDeleteSelected` | ✅ 删（零读取，Confirm 直接用 IsSoftDelete） |
| `MessageDialogViewModel.IconSource/IconColor` | ✅ 删（XAML 未绑定——仅 MessageType DataTrigger；顺带删 2 个 NotifyPropertyChangedFor） |
| `DataGridSelectionBehavior` 空 catch | ✅ 补注释（WPF SelectedItems 集合变更已知行为） |
| `PatientCardReaderViewModel.IsReadingCard` | 保留（命令内部状态，配套 CanReadCard 逻辑） |
| **XAML 坏绑定 5 处**（`QuickVisitCommand`/`ImportCommand`/`DownloadTemplateCommand`×2/`Consultation`/`Prescription`/`BoolToVis`） | **登记不删**——XAML 改动需人工确认（可能静默失效或绑定到资源），且删除会改变 UI 表面，超出本任务「代码清理」范围 |

### 空 catch 全量结论

任务书提及「28 个空 catch」——实际全仓扫描 **4 处**空 catch：
- `ThemeService.cs:94,111`（有注释：读取/保存失败静默）+ `MainWindow.xaml.cs:67`（有注释）+ `App.xaml.cs:176`（有注释）——**有意静默模式，保留**
- `DataGridSelectionBehavior.cs:271`（无注释）——已补注释

「28」数字与实测不符，可能源于早期估算；以实测为准。

---

## 三、验证

| 项 | 结果 |
|----|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | ✅ 0 错误 0 警告 |
| 新增单测（AuditLogService 3 + ReportService 6） | ✅ 9/9 |
| CardReaderPureTests（R4 修复） | ✅ 全绿（含原失败 2 例） |
| Repository 测试 | ✅ 通过 |
| 合计受影响测试 | ✅ 23/23 |

---

## 四、遗留项登记（后续 backlog）

1. **MedicalCaseService 门面死成员专项**（15+ 成员跨 Query/Command/Lifecycle 接口，量大关联深）
2. **MedicalCaseRepository.CancelMedicalCaseAsync 疑似恒 null bug**（取消永远报失败——需业务验证）
3. **XAML 坏绑定 5 处**（QuickVisitCommand/ImportCommand×2/DownloadTemplateCommand×2/Consultation/Prescription/DeploymentView BoolToVis——修复需人工确认 UI 意图）
4. **Repository/Service 层客户端侧死契约**（ValidateTokenAsync/GetValueAsync/SetValueAsync/GetPendingValidationAsync/ValidateHerbAsync/GetPermissionsAsync——Server 端点在线，删除需与 Server 协调）
5. **重复错误处理模板收敛**（L1/L2 ~30 处手写 try/catch → 共享助手）
6. **空 catch 策略**：有意静默处（ThemeService 等）若需可升级为 Debug 日志

---

## 五、结论

本批次完成：4 个 Review 问题（R1/R2/R3/R4）全部修复 + L4-L0 高置信死代码清理（3 个死方法 + 3 处空注释 + 3 个死字段 + 2 个死属性 + 1 处冗余参数 + 1 处空 catch 注释）。构建 0/0，测试 23/23。**可发布**。

删除遵循「每删前确认归属」：所有删除项均经全仓 grep 零调用 + 编译器验证；公共接口面（对应 Server 端点的契约方法）一律保留并登记。
