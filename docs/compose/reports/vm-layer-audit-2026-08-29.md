# ViewModel 层全面审计报告（6 维）

> 范围: `src/Client/Desktop/` 全部 ViewModel（53 业务 VM + 基类/共享控件 VM）| 方法: 5 路并行只读审计（Auth/Shell、MedicalCase、Catalog/Patients/Users/Registrations、Roles、Infrastructure 基类）
> 维度: D1 异常处理 / D2 资源泄漏 / D3 状态一致性 / D4 UI 线程 / D5 业务逻辑 / D6 代码质量
> 日期: 2026-08-29 | 前次 2026-08-26 P0×5 已修；本报告为新增维度审计

---

## 一、逐 VM 审计表

### P1（逻辑缺陷/资源泄漏/崩溃风险）

| VM | 维度 | 位置 | 问题 | 修复建议 |
|----|------|------|------|----------|
| ConfigurationCenterViewModel | D1 | SaveAsync | `SaveAsync` 无 try-catch → 保存失败抛异常崩溃 | 顶层 try-catch + StatusMessage |
| ConfigurationCenterViewModel | D5 | ForceChangeOnFirstLogin | 硬编码 `= true` 而非从 Options 加载 | `LoadFromOptions` 中读取 `DefaultPasswordOptions` |
| FormulaMasterDetailViewModel | D5 | SaveDetailAsync | 仅验证 FormulaEditContext，未验证每行药材 `Dosage` [Required]/[Range(1,500)] → 0/600g 药材可保存 | 保存前遍历 `EditHerbItems` 校验 |
| PatientMasterDetailViewModel | D5 | LoadListAsync | 只判 `Data != null`，请求失败（Success=false）被吞 | 增加 `pagedResult.Success` 判断 + 错误提示 |
| UserMasterDetailViewModel | D5 | ApplyFilters | 在已分页结果上过滤 → TotalCount 未过滤，分页计数错乱 | 筛选下推服务端（API 仅 keyword）或拉全量过滤；当前标注 |
| 各 MasterDetail VM（User/Herb/Formula/Patient） | D5 | 批量操作 | 批量删除/启停用部分失败（SuccessCount<count）仅日志不提示 | 失败时 `ShowErrorAsync`/`SetError` 提示失败数 |
| MedicalCaseMasterDetailViewModel | D2 | OnDisposing | 子 VM（Consultation/PrescriptionEditor）从不 Dispose → PrescriptionEditor `Items.CollectionChanged` 订阅泄漏 | 重写 OnDisposing 释放两子 VM |
| PrescriptionItemViewModel | D3 | Discount | `Discount` setter 不通知 `TotalPrice`；item 级 Dosage/UnitPrice 变化不重通知价格 | setter 补 `RaisePropertyChanged(TotalPrice)`；item 变化统一重通知 |
| ReportsHomeViewModel | D1/D4 | OnNavigatedTo/WhenAll | `async void OnNavigatedTo`；`Task.WhenAll` 一个报表抛异常则丢失另两个成功结果；日期快速切换 stale 覆盖 | 改普通 void fire-and-forget；逐个 await try-catch；版本号 guard |
| RegistrationListViewModel | D3/D2/D6 | CanExecute/Timer | busy 状态不刷新 StartVisit/Cancel CanExecute；`_registrationService` 缺 null-guard；30s PeriodicTimer 魔法数字且不 Dispose | 重写 `OnIsBusyChangedCore` 刷新命令；补 guard；`RefreshTimer` 常量 + Dispose |
| MasterDetailViewModelBase | D3 | 公共命令 CanExecute | 公共 RelayCommand 包装命令从不 NotifyCanExecuteChanged（仅内部 CommandGroup 刷新）→ XAML 绑定启用状态陈旧 | 基类保存公共命令引用并在状态变化时刷新 |
| MasterDetailViewModelBase | D5 | 批量虚方法 | `EnableBatch/DisableBatch/SetItemEnabledAsync` 默认静默 no-op，但 Batch 命令常启用 → 无端点实体点击无反馈 | 默认实现给出"不支持"提示或命令 CanExecute 依能力禁用 |
| ServiceEventBridge | D4 | 事件转发 | PropertyChanged/SafeFireAndForget 延续在非 UI 线程 | 经 IUiThreadDispatcher 转发 |
| NavigableViewModelBase | D2 | InitializeAsync | fire-and-forget 无 disposal/cancellation guard | 结合 CTS 与 Dispose 取消 |

### P2（质量/一致性）

| VM | 维度 | 位置 | 问题 | 修复建议 |
|----|------|------|------|----------|
| LoginViewModel | D3 | ExecuteLoginAsync | 登录成功路径不重置 `IsLoading`（残留 true） | 成功分支置 false |
| LoginViewModel | D6 | ctor | 11 参数构造 | 聚合子 VM/服务为参数对象 |
| FirstRunSetupViewModel / ServerConfigViewModel | D5 | SetModeAsync | fire-and-forget 关闭对话框前未完成模式切换、吞失败 | await 或切换后回调 |
| BackupManagementViewModel | D1 | OnNavigatedTo | `async void` 重写 | 改 fire-and-forget 内部 try-catch |
| CardReaderDiagnosticsViewModel | D1 | SaveSettingsAsync | `int.Parse` 未保护（非法输入崩溃） | TryParse + 校验提示 |
| MedicalCaseWorkspaceViewModel | D1 | ResumeSuspendedIfNeededAsync | 未保护，异常时跳过 `IActiveConsultationService.Register` | try-catch 包住 |
| AuditLogViewModel | D3 | _isLoading | 遮蔽基类 `IsLoading`（双状态） | 移除遮蔽复用基类 |
| AuditLogViewModel | D3 | Prev/Next CanExecute | 翻页后 CanExecute 不刷新（末页仍可 Next） | `[NotifyCanExecuteChangedFor]` 或加载后刷新 |
| ReportsHomeViewModel | D4 | 日期竞态 | 快速切换日期 stale 覆盖 | 版本号 guard |
| HerbListControlViewModel | D2 | 订阅 | 非 IDisposable，item/ListChanged 订阅无 teardown | 实现 IDisposable |
| NavigableViewModelBase | D2 | _disposables | CompositeDisposable 从未填充（空壳） | 统一入桶或移除 |
| ConfigurationCenterViewModel | D6 | ctor | 9 参数 | 聚合 Options 包装 |
| RegistrationCreateDialogViewModel | D6 | ctor | 3 服务注入缺 null-guard | 补 guard |
| PatientMasterDetailViewModel | D1 | ReadCardAsync | 命令缺 try-catch（内部有，确认） | 复核 |

### P3（小项，报告建议）

- LoginViewModel.ExecuteCloseApplicationAsync / SideNavViewModel.LogoutAsync / MainWindowViewModel.RetryHealthCheckAsync 无 try-catch（P3）
- AccountSettingsViewModel 密码最小长度魔法数字 8（P3）
- ChildViewModelBase 空 Dispose no-op（P3）
- HerbItemViewModelBase/HerbItemControlViewModel 魔法数字 500 重复（P3）
- HistoryCopyDialogViewModel.LoadCasesInternalAsync 未设 IsLoading（P3）
- MedicalCaseCommandsViewModel ShowDialog 回调 async void lambda（P3）
- FormulaImportDialogViewModel.OnDialogOpened fire-and-forget（P3，SafeFireAndForget 兜底）
- LogLevelControlViewModel JSON 序列化+Contains 检测级别脆弱（P3）
- ReceptionistHomeViewModel.LoadStatisticsAsync 无日期过滤（P3）
- SystemSettingsViewModel 部分保存中途失败无回滚提示（P3）

### 无问题 VM（审计通过 ✅）

AdminHome / SystemSettings(主体) / Deployment / LogLevelControl(主体) / ServerConfigSection / SysadminHome(轮询已修) / ClinicalHome / ClinicalWorkspace / PatientSelection / CardReader(Workspace) / PendingQueue / ReceptionistHome(异常处理) / FormulaEditor / FormulaHerbItem / HerbEditor / PatientCardReader / PatientEditor / UserEditor / ConsultationEditor / UnsavedChangesDialog / MessageDialog / ConfirmationDialog / InputDialog / Footer / Header / ConnectionStatus / ConnectionTestViewModelBase / LoginCredentials / HerbItemViewModelBase / ValidatableModelBase / MasterDetailCommandGroup(无订阅) / ViewModelServices

---

## 二、汇总统计

- 审计 VM 文件：**65**（53 业务 VM + 12 基类/共享控件 VM）
- **P0：0** ｜ **P1：14** ｜ **P2：12** ｜ **P3：12**
- 已修复（前次批次）：P0×5、P1#6/#7 等

## 三、修复计划（按优先级）

| 优先级 | 项 | 预估 |
|--------|-----|------|
| P1-A | ConfigurationCenter SaveAsync catch + ForceChangeOnFirstLogin 从 options | 0.5h |
| P1-B | FormulaMasterDetail 保存前校验药材行 | 0.5h |
| P1-C | PatientMasterDetail LoadListAsync 失败判断 + 批量部分失败提示（4 VM） | 1h |
| P1-D | MedicalCaseMasterDetail 子 VM Dispose + PrescriptionItem 价格通知 | 1h |
| P1-E | ReportsHome async void/WhenAll 隔离/日期竞态 | 1h |
| P1-F | RegistrationList busy CanExecute + null-guard + timer Dispose | 0.5h |
| P2-A | BackupManagement/CardReaderDiagnostics/MedicalCaseWorkspace 异常保护 + AuditLog IsLoading/CanExecute + RegistrationCreateDialog guards | 1h |
| P2-B | MasterDetail 公共命令 CanExecute 刷新（基类，风险中） | 1h |

> 修复执行见下文；P2-B（基类命令刷新）与 ServiceEventBridge 线程化（架构性）建议独立批次评估，本次先修 P1-A~F 与 P2-A。
