# ViewModel 重构后验证审计报告（导航/事件/命令/绑定）

> 版本: v1.0 | 日期: 2026-09-14 | 类型: 过程文档（审计 + 修复记录）
> 触发：ViewModel 层全量清理（INPC→`[ObservableProperty]`、`ICommand`→`[RelayCommand]`，见 `docs/07-ui-ux/viewmodel-layer-design.md` §八）后的正确性验证。
> 方法：9 个只读审计切片（导航 / 弹窗 / 事件 / 命令 / XAML 绑定 ×3 / 转换器 / 迁移等价性）+ 对每条「无效」结论的独立复核（`git log` 溯源 + XAML↔VM 对照 + 源生成器产物 `obj/**/*.g.cs`）。

## 一、结论计数（审计时点）

| 切片 | 范围 | 有效 | 无效 | 待修复 | 待确认 |
|---|---|---:|---:|---:|---:|
| 导航（Region/ViewName） | 100 处调用 / 26 文件 | 40 | 0 | 3 | 2 |
| 弹窗（RegisterDialog/ShowDialog） | 9 注册 / 8 命名调用 / 14 参数键 | 26 | 2 | 2 | 2 |
| 事件（EventAggregator） | 14 事件类型 / 17 发布 / 10 订阅 | 13 | 1 | 12 | 0 |
| 命令与 CanExecute | 180 处 `[RelayCommand]`（66 带 CanExecute） | 35 | 29 | 2 | 1 |
| 绑定 A（Shell/Auth/Patients/Controls） | 34 XAML ≈384 绑定 | 28 | 2 | 5 | 1 |
| 绑定 B（Catalog/MedicalCase） | 13 XAML | 59 | 1 | 1 | 1 |
| 绑定 C（Clinical/Admin/Users/Registrations） | 26 XAML ≈446 绑定 | 441 | 2 | 1 | 2 |
| 转换器/资源 | 82 XAML 全引用面 | 18 | 0 | 0 | 1 |
| 迁移等价性专项 | 10 文件 / 28 成员 | 28 | 0 | 2 | 1 |

**迁移专项结论：本次 ViewModel 迁移未引入任何导航/事件/命令/绑定失效**（28/28 成员等价；仅 2 处无语义影响的差异：`IsReadingCard` setter 可见性 `private`→`public`、`PrescriptionEditorViewModel.Prescription` 的通知顺序变化）。

## 二、已修复（11 组，全部验证通过）

| # | 问题 | 修复 | 文件 | 验证 |
|---|------|------|------|------|
| P0-1 (I-2) | 「手动读卡」按钮绑定 `CardReader.ReadCardCommand`，VM 只生成 `ManualReadCardCommand` → 按钮无反应 | 绑定改为 `CardReader.ManualReadCardCommand` | `Roles/…/Clinical/Views/PatientSelectionView.xaml:80` | build 0/0 |
| P0-2 (I-3) | 部署上传命令 CanExecute 依赖三态但零通知 → 选文件后按钮永不启用（上传不可达） | `_isUploading`/`_isRestarting`/`_selectedFileName` 加 `[NotifyCanExecuteChangedFor(UploadCommand/RestartCommand)]` | `Roles/…/Admin/Sysadmin/ViewModels/DeploymentViewModel.cs` | build 0/0 |
| P0-3 (I-4) | 验方导入弹窗回传 `SelectedFormula` 为 `FormulaListDto`，调用方按 `FormulaDetailDto` 读取 → 导入失败 | 回传 `SelectedFormulaDetail`（`FormulaDetailDto`）；`CanConfirm` 要求详情已加载 | `Modules/…/MedicalCase/Dialogs/FormulaImportDialogViewModel.cs` | build 0/0 |
| P0-4 (I-9) | 配置节编辑绑 `Dictionary` 迭代项（只读 `KeyValuePair.Value`）→ 保存不生效 | 新增可编辑条目模型 `ServerConfigEntry`（+`ServerSectionItem.Entries`），XAML 绑 `Entries`，保存从 `Entries` 组装（脱敏项 `IsReadOnly` 跳过） | `Roles/…/Admin/Sysadmin/ViewModels/ServerConfigSectionViewModel.cs`、`Views/SysadminHomeView.xaml:284` | build 0/0 |
| P0-5 (I-5) | 确认框传参键 `message`/`title`（小写）vs 读取 `Message`/`Title` → 所有确认框显示默认文案；另 5 处实参顺序颠倒 | `DialogManager.ShowConfirmAsync` 键改 PascalCase；`MasterDetailCommandGroup` 5 处实参顺序按契约 `(message, title)` 修正 | `Core/…/Services/DialogManager.cs`、`Core/…/Composition/MasterDetailCommandGroup.cs`、`tests/…/FormulaMasterDetailViewModelTests.cs:284`（断言同步修正） | build 0/0；MasterDetail|Herb|Formula 136/136 |
| P1-6 (I-1) | XAML 绑定的是基类**代理命令**（12 个），而通知只发给 `MasterDetailCommandGroup` 内部同名命令 → 5 个主从页工具栏启用状态陈旧 | 基类新增 `NotifyOwnCrudCommands`/`NotifyOwnPaginationCommands`，在 4 个既有通知点同步转发到本类命令；新增子类扩展点 `OnCrudCommandStateChanged()`（SearchText 变化亦回调，用于筛选类命令） | `Core/…/ViewModels/MasterDetailViewModelBase.cs` | build 0/0；136/136 |
| P1-7 | 模块自定义命令零通知（共 15 个命令 / 6 个文件） | 各模块重写 `OnCrudCommandStateChanged()` 并通知自有命令：Herb(2)、Formula(4)、User(3，另筛选属性变化通知 `ClearFiltersCommand`)、Patient(3，并订阅子 VM `IsReadingCard` 变化重广播+通知)、`PatientCardReaderViewModel._isReadingCard` 加 `NotifyCanExecuteChangedFor`、`HerbListControlViewModel` 3 处 `SortByRoleCommand` 通知 | Catalog/Users/Patients/Controls 各 VM | build 0/0；136/136 |
| P2-8a (I-6) | `HerbItems` TwoWay 绑到只读 `EditHerbItems`（WPF 写回非法） | 宿主绑定改 `Mode=OneWay`（内层 `HerbListControl` 仍可就地修改集合项；集合替换本就不支持） | `Modules/…/Catalog/Controls/FormulaMasterDetailControl.xaml:430`、`Modules/…/MedicalCase/Controls/MedicalCaseEditControl.xaml:472` | build 0/0 |
| P2-8b (I-7) | 患者卡只设 `DataContext`，控件内部绑的是自身 `Patient` DP（`PatientDisplayModel`）→ 卡片恒空；另 `Patient.VisitCount` 路径不存在 | 新增 `PatientSelectionViewModel.SelectedPatientDisplayModel` 投影（含性别文案），XAML 改 `Patient="{Binding SelectedPatientDisplayModel}"`；删除无数据源的「就诊次数」行与随之失效的 `ShowVisitCount` DP | `Roles/…/Clinical/ViewModels/PatientSelectionViewModel.cs`、`Views/PatientSelectionView.xaml:148`、`Core/…/Controls/PatientInfoCardControl.xaml(+.cs)` | build 0/0；Clinical 相关 183/183 |
| P2-8c (I-8) | `FormulaViewControl` 绑 `Formula.Source`（DTO/Model 均无该字段） | 删除该「来源」行（无数据源；`Source` 仅存在于导入 DTO） | `Core/…/Controls/FormulaView/FormulaViewControl.xaml:338` | build 0/0 |
| P2-9 (I-11) | 改密成功后不发布 `PasswordChangedEvent` → 改密后不强制重新登录（Issue #1906 静默失效） | `AccountSettingsViewModel.ChangePasswordAsync` 成功分支补 `Publish(new PasswordChangedPayload { UserName })` | `Shell/ViewModels/AccountSettingsViewModel.cs:205` | 新增回归测试 `AccountSettingsPasswordChangedEventTests` 2/2（成功发布 / 失败不发布） |
| P2-10 | 死事件与死方法 | 删除 `PatientEvents`（零发布零订阅）、`CaseEvents`（空壳）、`AuthEvents.PendingLogoutsClearedEvent`（+载荷、接口注释同步）、`EventSubscriptionManager.Publish<TEvent,TPayload>/Publish<TEvent>`（零调用） | Infrastructure/Events、Foundation/Security | build 0/0；183/183 |
| P3-11 | 4 处文档漂移（实际 5 文件） | `Auth/AGENTS.md`（手写命令→`[RelayCommand]`）、`Auth/README.md`（`ExecuteLoginAsync`→`LoginAsync`）、`MedicalCase/README.md`（9 命令源生成）、`docs/03-architecture/02-desktop.md`（事件清单按代码重写 + 记录删除项）、`Core/…/Controls/README.md`（转换器清单按 `Cvt` 13 成员重写） | 见左 | 文档同步 |
| 附加（同族，审计导航切片待修复③） | `AppShell` 面包屑绑 `SelectedNavItem.Title` 但宿主未订阅管理器变更 → 停在首次绑定值 | `MainWindowViewModel` 订阅 `INavigationManager.PropertyChanged`，`SelectedNavItem` 变化时重广播（与侧栏模式一致，OnDisposing 退订） | `Shell/ViewModels/MainWindowViewModel.cs` | build 0/0；Shell 相关测试 |

> 命令通知缺口 29 处审计项已全覆盖（12 基类代理 + 15 模块自定义 + 2 部署/配置节 VM）；配置节 VM 另补 `IsLoading/IsSaving` → `SaveSectionCommand`/`RestartServerCommand` 通知。

## 三、验证汇总

| 套件 | 结果 |
|------|------|
| `dotnet build LYBTZYZS.sln --no-incremental`（每次优先级修复后 + 收尾） | ✅ 0 错误 0 警告 |
| P0 相关（FormulaImportDialog/Sysadmin/Dialog/Clinical/FormulaMasterDetail） | ✅ 91/91 |
| P1 相关（MasterDetail/Herb/Formula/HerbList） | ✅ 136/136 |
| P2 相关（Clinical/AccountSettings/Shell/Formula/MedicalCase/Herb） | ✅ 183/183 |
| I-11 回归测试（新增） | ✅ 2/2 |
| 全量 Desktop / Server / Architecture | 见 `13c-current-status.md` #142 |

## 四、有意保留（含原因）与后续建议

| 项 | 原因 | 建议 |
|---|---|---|
| 7 个「只发布无订阅」事件（`LoginStarted`/`LogoutStarted`/`LogoutCompleted`/`ServerLogoutFailed`/`TokenRefreshSucceeded`/`TokenRefreshFailed`/`SessionExtended`）+ `LogoutService` 服务（`ILogoutService` 无注入点） | 均由测试断言（`AuthEventPublishingTests`/`LogoutServiceTests`/`TokenRefreshHandlerTests`），删除=改测试契约；属**产品决策**（保留为扩展点 vs 删除） | 立项决策：删事件+服务，或补 UI 订阅（失败提示接 Toast、登出完成驱动 UI 重置） |
| `SessionLifecycleManager.SessionExpired/StateChanged`、`SessionManager.SessionExpired`（.NET 事件无订阅者） | 公共事件面，删除属接口变更 | 随会话层重构一并评估 |
| `HerbListControl` 回写 `List<PrescriptionItemDto>` 且投影丢 `DecocteMethod/Role` | 回写目标本就不接受该类型（已改 OneWay 止损）；要做真回写需先补齐投影字段 | 专项：`ToPrescriptionItemDto` 补字段后，用映射/`ListChanged` 事件替代 DP 回写 |
| `SearchBox.xaml:123` 清除按钮可见性用 `Cvt.NullToVis`（`SearchText` 为 `string.Empty` 恒非 null → 恒可见） | 不影响功能（视觉细节）；改 `Cvt.StringToVis` 需确认设计稿意图 | 1 行修复：换 `StringToVis` |
| `HerbListControl.xaml:58 ItemIndex=(ItemsControl.AlternationIndex)` 未设 `AlternationCount` → 恒 0 | 视觉细节（序号列） | 1 行修复：`ItemsControl.AlternationCount="1000"` |
| `Cvt.NullToVis` 命名与语义相反（非 null→Visible，与 `NotNullToVis` 同实现） | 牵动多处 XAML 引用 | 专项重命名（`NullToVis`→`NotNullToVis` 语义），需全量 XAML 回归 |
| 导航参数 `Action="AddNew"` 无消费者、`DefaultRoleFilter` 无生产者 | 属功能未接线，需产品确认是补实现还是删除 | 立项决策 |
| `MedicalCaseEditControl`/`PatientSelectionControl` 等 TwoWay 写回负载类型不符 | 已按 OneWay 止损；真正修复需设计「控件→VM 集合」同步契约 | 结合 HerbList 回写专项一起做 |
