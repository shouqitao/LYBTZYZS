# Desktop UI/UX 详细设计文档
> 版本: v2.0 | 日期: 2026-08-22 | 基于: R01→R22 22 轮独立调研 + R23 综合
> 模型: DeepSeek V4 Flash | 状态: 定版 | 覆盖: 77 个 XAML + 30 个 ViewModel + 147 个 US

> **R23 说明**: 本文档为 R01-R22 的去重合并与优先级排序后的综合产出，代码 vs 需求 vs 设计已交叉验证，差异已标注于 §10 问题清单。

## 1. 设计系统概要

### 1.1 设计 Token（源 `TcmBrands.xaml`/`Spacing.xaml`/`Surfaces.xaml`）
- **主色**: 中医青 `#1A5C3A`（`TcmPrimaryBrush`，品牌）、辅金 `#C9A86A`、中性灰 `#F5F7FA` 背景、文本 `#1A1A1A`/`#6B7280`
- **字体**: 思源黑体 14px 正文、16px 标题、12px 辅助，行高 1.5
- **间距**: 8pt 基准 4/8/16/24/32（`Spacing.xaml:8`），禁止 10 等非 8 倍数
- **圆角**: 卡片 8、按钮 6、输入框 6
- **投影**: Elevation 0-5：Card `0 2 8 rgba(0,0,0,0.08)` 1 级、Dialog `0 8 24` 4 级、Toast `0 4 12` 5 级（高于 Dialog）
- **图标**: `Icons.xaml` 线性图标 20×20，主色或灰 600

### 1.2 布局模式定义
| 模式 | 用途 | 结构 | 示例页面 |
|------|------|------|----------|
| Master-Detail | 患者/药材/验方/用户/挂号 | 左 380 固定 + 右弹性详情/编辑 + 顶部 `DataGridToolbar` + 底部 `UnifiedPaginationBar` | `PatientMasterDetailControl` |
| Workspace | 医案工作台 | 顶患者卡 + 左右分栏（左辨证 45% 右处方 55%）+ 底部操作栏 | `MedicalCaseWorkspaceView` |
| Dashboard Grid | 各角色首页 | 2-4 列 `InfoCard` 网格 + 顶部统计 + 趋势区 | `ClinicalHomeView` |
| Form Dialog | 新增/编辑 | 居中 640×480，`BaseDetailContainer` 表单+底部 `Save/Cancel` | `PatientEditControl` |
| Split View | 挂号队列 | 左队列 `DataGrid` + 右 `PatientViewControl` | `PendingQueueView` |

### 1.3 共享控件清单（`LYBT.Desktop.Controls` 16 个）
| 控件 | 文件 | 复用页面 | 关键属性 |
|------|------|----------|----------|
| MasterDetailLayout | `Controls/MasterDetailLayout.xaml` | 患者/药材/验方/用户/挂号 | `MasterWidth=380` |
| DataGridToolbar | `Controls/DataGridToolbar.xaml` | 所有列表 | `SearchText, OnSearch, OnCreate` 统一顺序 `[搜索]|[新建][导入][导出]|[批量删除]` |
| UnifiedPaginationBar | `Controls/UnifiedPaginationBar.xaml` | 所有分页 | `TotalCount, PageSize, CurrentPage` 文案 `共 {n} 条` |
| SearchBox | `Controls/SearchBox.xaml` | 所有列表 | `Delay=300ms, Placeholder` |
| InfoCard | `Controls/InfoCard.xaml` | 首页 | `Title, Value, Icon, Trend` |
| StatusBadge | `Controls/StatusBadge.xaml` | 状态列 | `Status` → 颜色 |
| BreadcrumbBar | `Controls/BreadcrumbBar.xaml` | 主界面 | `Items: Title/ViewName/IsCurrent` 完整路径 |
| EmptyState | `Controls/EmptyState.xaml` | 空列表 | `Image, Title, Action` 统一 `暂无{实体}` |
| LoadingOverlay | `Controls/LoadingOverlay.xaml` | 加载中 | `IsLoading, Text` 必需文案 |
| ToastControl | `Controls/Toast/ToastControl.xaml` | 全局 | `Message, Type( Success/Error/Warning), Action` |
| PatientInfoCardControl | `Controls/PatientInfoCardControl.xaml` | 医案工作台 | `PatientDisplayModel` |
| HerbListControl | `Controls/HerbList/HerbListControl.xaml` | 处方 | `Items, OnAdd, OnRemove` 支持拖拽 |
| BreadcrumbBar | `Controls/BreadcrumbBar.xaml` | 导航 | `Level, IsLast` |
| DetailToolbar | `Controls/DetailToolbar.xaml` | 详情 | `OnEdit, OnDelete, OnPrint` |
| WorkflowStepIndicator | `MedicalCase/Controls/WorkflowStepIndicator.xaml` | 工作台 | `CurrentStep 1-5` |
| UnifiedPaginationBar | `Controls/UnifiedPaginationBar.xaml` | 分页 | `PageSize` |

## 2. 角色-页面-权限矩阵

| 页面 | Sysadmin | Admin | Doctor | Receptionist | 权限源 |
|------|----------|-------|--------|--------------|--------|
| 登录 | ✅ | ✅ | ✅ | ✅ | AllowAnonymous |
| 首次运行向导 | ✅ | ❌ | ❌ | ❌ | 仅首次 `InitialSetupToken` |
| 服务器配置 | ✅ | ✅ | ✅ | ✅ | 登录前 |
| Sysadmin首页 | ✅ | ❌ | ❌ | ❌ | `SysAdminOnly` |
| Admin首页 | ❌ | ✅ | ❌ | ❌ | `AdminOrSuperAdmin` |
| 临床首页 | ❌ | ❌ | ✅ | ❌ | `Doctor` |
| 前台首页 | ❌ | ❌ | ❌ | ✅ | `Receptionist` |
| 用户管理 | ✅(仅Admin) | ✅(仅Doctor/Receptionist) | ❌ | ❌ | 分级 `Sysadmin→Admin→Doctor/Receptionist` |
| 患者管理 | ❌ | ✅ | ✅ | ✅ | 三角色 |
| 患者选择 | ❌ | ❌ | ✅ | ✅ | 挂号/就诊 |
| 药材管理 | ❌ | ✅ | 仅查看 | ❌ | `AdminOrSuperAdmin` 写 |
| 验方管理 | ❌ | ✅ | ✅(仅自己) | ❌ | `DoctorOrAdmin` |
| 医案管理 | ❌ | ✅(全部) | ✅(仅自己) | ❌ | 行级 `doctorIdFilter` |
| 医案工作台 | ❌ | ❌ | ✅ | ❌ | `DoctorOnly` + `IsLocked` |
| 待诊队列 | ❌ | ❌ | ✅ | ✅ | 共享，`doctor-{id}` 分组 |
| 挂号列表 | ❌ | ✅只读 | ✅仅自己 | ✅全部 | `ReceptionistOnly` 取消 |
| 报表首页 | ❌ | ✅ | ✅ | ❌ | `DoctorOrAdmin` + 行级 |
| 审计日志 | ❌ | ✅ | ✅仅自己 | ❌ | 医案审计 |
| 系统设置 | ❌ | ✅ | ❌ | ❌ | `AdminOrSuperAdmin` |
| 备份恢复 | ✅ | ❌ | ❌ | ❌ | `SysAdminOnly` 高危 |
| 日志级别 | ✅ | ❌ | ❌ | ❌ | `SysAdminOnly` |
| 部署 | ✅ | ❌ | ❌ | ❌ | `SysAdminOnly` |

> SSOT `04-permissions.md`，`12-permissions-matrix.md` 为视图；前端 `CanExecute` 与后端 `Policy` 同源 `IPermissionService`（R17）。

## 3. 导航流程图

### 3.1 Sysadmin
```
Login → SysadminHomeView [Tab: 配置|备份|日志|部署]
  ├─ 配置 → ServerConfigSectionViewModel (6 节 API)
  ├─ 备份 → BackupManagementView → 恢复 (二次确认+倒计时)
  ├─ 日志 → LogLevelControlView (Debug 警告+30min回退)
  └─ 部署 → DeploymentView (上传 nupkg + RELEASES 预览)
```

### 3.2 Admin
```
Login → AdminHomeView (卡片: 用户/患者/系统)
  ├─ 用户管理 → UserManagementView (Master-Detail, 批量二次确认)
  ├─ 患者管理 → PatientManagementView
  ├─ 挂号列表(只读) → RegistrationListView
  ├─ 报表 → ReportsHomeView
  └─ 系统设置 → SystemSettingsView (乐观锁)
```

### 3.3 Doctor
```
Login → ClinicalHomeView (待诊大卡置顶 + 红点)
  ├─ 临床工作台 → ClinicalWorkspaceView
  ├─ 待诊队列 → PendingQueueView (超时分级黄/红) → 选中 → MedicalCaseWorkspaceView
  ├─ 患者管理 → PatientManagementView → 选患者 → MedicalCaseWorkspaceView
  ├─ 医案管理 → MedicalCaseMasterDetailView → 选医案 → MedicalCaseWorkspaceView
  ├─ 药材(只读) → HerbManagementView
  ├─ 验方 → FormulaManagementView
  └─ 报表 → ReportsHomeView
```

### 3.4 Receptionist
```
Login → ReceptionistHomeView (叫号横幅+挂号/患者快捷)
  ├─ 挂号列表 → RegistrationListView (新建/取消/ReceptionistOnly)
  ├─ 患者管理 → PatientManagementView (新建 + 读身份证主按钮)
  ├─ 患者选择 → PatientSelectionView (SearchBox + 读卡)
  └─ 待诊队列 → PendingQueueView
```

> 导航经 `ViewNames` 强类型 + `INavigationCoordinator.NavigateToMedicalCaseWorkspace(params)` + `RegionNames.MainContent`（R09），`KeepAlive` 列表页保持搜索/分页。

## 4. 页面详细规格（17 页）

### 4.1 登录 (`LoginView.xaml`)
```
┌─────────────────────────────────────────────┐
│  ┌─────────────┐  ┌─────────────────────┐   │
│  │  左侧品牌    │  │  右侧表单           │   │
│  │  🌿 LOGO    │  │  用户名 [____]      │   │
│  │  诊所名(可绑)│  │  密码   [____]      │   │
│  │  中医诊所    │  │  ☑记住密码   ○自动登录│   │
│  │  管理系统   │  │  [      登录      ] │   │
│  └─────────────┘  │   ●在线/离线 [远程|本地] │   │
│  底部: 连接模式/API配置 | ⚙API配置(开 ServerConfigView)
└─────────────────────────────────────────────┘
```
| # | 控件 | 类型 | 位置 | 大小 | 说明 |
|---|------|------|------|------|------|
|1|Logo|Image|左侧居中|120|中医图标|
|2|系统名|TextBlock|Logo下|Auto|16pt 粗体 大标题 `ClinicName`（绑定配置，空回退默认）|
|3|副标题|TextBlock|系统名下|Auto|`中医诊所管理系统`|
|4|用户名|TextBox|右侧|320×40|必填 3-32，`TabIndex0`|
|5|密码|PasswordBox|用户名下|320×40|6-128，`TabIndex1`|
|6|记住密码|CheckBox|密码下左|Auto|`IsRememberMe`|
|7|自动登录|CheckBox|记住右|Auto|`IsAutoLogin`（两端对齐，Grid 两列）|
|8|登录|Button|表单底|320×48|主色 `IsDefault`，`Loading`禁用|
|9|连接切换|ToggleButton|状态栏左|Auto|`Remote/Local`|
|10|API状态|StatusBadge|状态栏|Auto|绿/红/黄|
|11|API配置|Button|状态栏右|Auto|⚙+API配置 文字，点击开 `ServerConfigView`（复用 `OpenSettingsCommand`）|
|12|状态栏|Border|底部|高度 48|统一 48，含切换/状态/API配置|
| 按钮 | 前置 | 逻辑 | 后置 |
|------|------|------|------|
|登录|用户名非空+密码非空|1.Loading 2.`POST /api/v1/auth/login` 3.成功→`ITokenStorage.Save`→导航主界面 4.失败→Toast|成功关登录开MainWindow；失败恢复按钮|
|记住|—|勾选→`ICredentialVault.Save`|无|
|切换|—|`SetModeAsync` + `CheckRemoteAvailableAsync` + `NotifyCanExecuteChanged`|更新文案|
| 状态流转 | 初始→输入中→提交中→成功(导航)/失败(红 Snackbar “用户名或密码错误”/`423 锁定`) |
| 异常 | 网络红 Snackbar“网络失败”；API 不可用 StatusBadge 红；锁定等 15min |

### 4.2 首次运行向导 (`FirstRunSetupView.xaml`)
5 步 `StepIndicator`：欢迎→系统检查→创建 SuperAdmin(`POST /auth/setup` + `InitialSetupToken`)→诊所信息→完成，`Next/Back` + 进度条，`TabIndex` 0-4，焦点首输入。

### 4.3 服务器配置 (`ServerConfigView.xaml`)
`API地址` 输入 + `TestConnection` + `Save` + `ConnectionStatusViewModel` 卡（`RemoteUrl` 自动探测 `UrlChanged→CheckRemoteAvailableAsync`）；`Save` 成功 `InfoCard` “✓ 已保存 12:34”。

### 4.4 主界面 (`MainWindow.xaml`)
```
┌──────────────────────────────────────────────────┐
│ 顶部: Logo | 面包屑(完整路径) | 搜索 | 🔔 | 头像▼ │
├────────┬─────────────────────────────────────────┤
│ 侧边栏 │  内容区 Region MainContent               │
│ 分组折 │  卡片网格/列表 + 分页                     │
│ 临床   │                                         │
│ 目录   │                                         │
│ 管理   │                                         │
└────────┴─────────────────────────────────────────┘
```
- 侧边栏分组折叠（临床/目录/管理），当前角色组默认展开
- 面包屑完整路径 `患者管理 > 张三 > 医案工作台` 可点击回溯
- `InputBindings` 全局 `Ctrl+N 新建患者, Ctrl+S 保存, Ctrl+P 打印, Esc 返回`

### 4.5 患者管理 (`PatientManagementView` + `PatientMasterDetailControl`)
- **布局**: `MasterDetailLayout` 左 380 列表 + 右 `PatientView/Edit` + 顶部 `DataGridToolbar` 统一顺序 `[搜索]|[新建][导入][导出]|[批量删除]` + 底部 `UnifiedPaginationBar` `共 {n} 条`
- **列表列**: 姓名/性别/年龄/电话/`StatusBadge` + `RowStyle` 虚拟化 `EnableRowVirtualization`
- **按钮**: 新建 `POST /patients` 201，编辑 `PUT /patients/{id}`，删除 `DELETE` 软删 `AdminOrSuperAdmin` 二次确认，恢复 `POST /restore`，批量删除 `POST /batch-delete` Max100，导出 `GET /export` JSON + Excel
- **校验**: 姓名 2-50 必填，电话 11 位 `ExistsByPhoneAsync` 防抖 500ms，身份证 18 位唯一过滤索引 `IsDeleted=0`，`Validation.ErrorTemplate` 仅红框+Tooltip
- **空/错**: `EmptyState` “暂无患者，去创建” + 按钮；`Error` 红图标“加载失败 重试”

### 4.6 患者选择 (`PatientSelectionView`)
`SearchBox` + `PatientSelectionControl` 列表 + 右 `读身份证` 主按钮常驻（图标+文字），`CardReader` 状态灯；读卡 `ICardReader.ReadCard → FindPatientByIdNumberAsync` 未找到 `QuickCreatePatientAsync` 自动填充。

### 4.7 药材管理 (`HerbManagementView`)
`MasterDetail` `HerbView/Edit`，字段 名称/拼音/分类/性味/产地/规格/单位/单价/成本/功效/用法，`IsShared`；按钮 新建/编辑/删除/启用/禁用/批量启用/禁用/批量删除/导入/导出（写 `AdminOrSuperAdmin`，读 `DoctorOrAdmin`），`CheckReference` 引用检查。

### 4.8 验方管理 (`FormulaManagementView`)
`MasterDetail` `FormulaView` 含药材组成表格 `HerbName/Dosage/Unit`；工具栏 新建/编辑/复制/模板/导入/导出 + **`待校验`**（✅ 2026-09-09 B-13：切换 Draft 待办列表——`GET pending-validation` 分页 + 列表「校验」列 待校验/已验证）；待校验模式详情为**校验面板**——每行未绑定药材 ComboBox 选系统药材 + 校验绑定（`POST /formulas/{id}/herbs/{itemId}/validate`，全部绑定自动晋升 Validated）；导入走 JSON 模板 + `POST batch-import`（模板下载 2026-08-13 起为 JSON）。

### 4.9 医案管理 (`MedicalCaseMasterDetailView`)
`MasterDetail` `MedicalCaseViewControl` 只读 + `StatusBadge` Draft/Active/Completed/Locked/Suspended；列表 患者/医生/状态/创建时间/是否打印；按钮 新建 `DoctorOnly` /查看/编辑/完成/关闭/挂起/取消/打印/审计。

### 4.10 医案工作台 (`MedicalCaseWorkspaceView` 187 + ViewModel 558 — 核心)
```
┌─────────────────────────────────────────────┐
│ 患者信息卡 PatientInfoCardControl            │
├──────────────┬──────────────────────────────┤
│ 左 45% 辨证  │ 右 55% 处方 (常驻, 非 Tab)    │
│ 主诉/现病史  │ HerbListControl + 总价/折扣   │
│ 舌诊/脉诊/诊 │ 医嘱 + 拖拽排序               │
│ 断            │                              │
├──────────────┴──────────────────────────────┤
│ 底部栏: [保存] [完成] [打印] [取消] + Step 1-5│
│ EditReason 折叠区 (Completeness.RequiresEditReason 时展开红*)│
└─────────────────────────────────────────────┘
```
- **ViewModel**: 薄壳 `ConsultationEditor/PrescriptionEditor/Commands` + `EditModeStateMachine` + `WorkspaceStateManager` + `WorkspaceNavigationHandler`
- **处方**: `PrescriptionItemViewModel` 集合，`HerbSearchProvider` 填充单价，`AllHerbs` + 库存 `StatusBadge`（预留）
- **保存**: `MedicalCaseInputDto → POST/PUT /medicalcases` 经 `MedicalCaseStateGuard.EnsureCanEdit`（`IsLocked/EditReason/IsPrinted/异人`）`
- **完成**: `CompleteAsync` → `Completed` + 挂号联动 `Completed` + `CanComplete` 校验
- **打印**: `IPrintService.PreviewAsync → PrescriptionPrintTemplate` FixedDocument
- **交互**: 行单击选中/双击编辑统一，`Ctrl+S` 保存，拖拽排序 `GongSolutions.WPF.DragDrop`，`EditReason` 条件展开

### 4.11 待诊队列 (`PendingQueueView`)
左队列 `DataGrid` + 右 `PatientViewControl`；列 患者/队列号/状态/`等待时间 StatusBadge`（>30 黄 >60 红）；`SignalR RegistrationHub doctor-{id}` 分组实时推送；在诊/超时筛选 Toggle；选中 → `MedicalCaseWorkspaceView`。

### 4.12 挂号列表 (`RegistrationListView`)
`MasterDetail` + `RegistrationCreateDialog` 新建；字段 患者/医生/队列号/状态/费用/来源；按钮 新建 `Receptionist/Doctor`、取消 `ReceptionistOnly` 二次确认、开始就诊 `DoctorOnly start-visit` 原子建档 `MedicalCaseId`；单患者单 `Waiting/InProgress` 过滤唯一索引 `UX_Registrations_PatientId_Pending`；状态机 `Waiting→InProgress→Completed/Cancelled`。

### 4.13 临床首页/工作台/前台首页
- `ClinicalHomeView`: 待诊大卡置顶红点 + 今日接诊/患者总数小卡 + 趋势区懒加载
- `ClinicalWorkspaceView`: 快捷入口 `新建患者/挂号/待诊`
- `ReceptionistHomeView`: 叫号横幅“当前叫号 张三” + 挂号/患者快捷 + 今日挂号统计

### 4.14 报表首页 (`ReportsHomeView`)
`DateRangePicker` + `Granularity` 日/周/月 + 4 趋势图（收入/接诊/药材排行/绩效）+ `ReportTimeBuckets.Build` 分桶；`LoadingOverlay Skeleton` + `EmptyState` “暂无数据 选其他时间” +  `ExportExcel`；`doctorIdFilter` 行级二次校验。

### 4.15 审计日志 (`AuditLogView`)
表格 操作人/时间/操作类型/字段差异 `ChangedFields/OldValues/NewValues` + `ReportRepository` 聚合；`GET /medicalcases/{id}/audit-logs|history`。

### 4.16 管理员首页/系统设置/用户管理
- `AdminHomeView`: 仪表盘卡片
- `SystemSettingsView`: 诊所名称/地址/电话（`ClinicSettingsService` 热更新 + `RowVersion` 乐观锁“已被他人修改”）
- `UserManagementView`: Master-Detail，用户 CRUD 分级 `Sysadmin→Admin→Doctor/Receptionist` + 禁用/重置密码 `SysAdminOnly` + 批量二次确认 + 密码确认

### 4.17 运维首页/备份/部署/日志
- `SysadminHomeView`: Tab 配置/备份/日志/部署
- `BackupManagementView`: 上次备份/文件数/总大小 + 手动备份 `ProgressBar` + 文件列表 `DataGrid` + 恢复 `POST /backup/restore` 红警告+输入“确认恢复”+5s倒计时
- `DeploymentView`: 上传 `nupkg` `ProgressBar` + 类型校验 + `RELEASES` 预览 + 重启 `POST /deploy/restart`
- `LogLevelControlView`: `LoggingLevelManager` 运行时 `ComboBox`，切 `Debug` 警告“30分钟后回退” + 定时器

## 5. 跨页数据流
| 数据 | 来源 | 目标 | 方式 | 备注 |
|------|------|------|------|------|
| PatientId | 患者列表 | 医案工作台 | `NavigationParameters["PatientId"]` 强类型 `MedicalCaseNavigationParameters` | `INavigationCoordinator` |
| RegistrationId | 挂号列表 | 医案工作台 | 同上 | `start-visit` 返回 `MedicalCaseId` |
| MedicalCaseId | 医案列表 | 工作台/审计 | 同上 + `WorkspaceState` 持久化 `LastPatientId` | 异常恢复回填 |
| HerbId | 药材列表 | 验方/处方 | `IHerbSearchProvider` 委托 `ICatalogQueryService` | `AllHerbs` 55% 常驻 |
| FormulaId | 验方列表 | 医案 FormulaImportDialog | `Dialog` 参数 | 克隆/导入 |
| UserId | 登录 | 全局 | `ICurrentUserProvider` + `ClaimsPrincipal` | `NameIdentifier` |

## 6. 状态机定义
| 实体 | 状态 | 转换 | 守卫 |
|------|------|------|------|
| Registration | Waiting→InProgress→Completed | `StartVisit` 仅 Waiting→InProgress 原子；`Complete` 仅 InProgress→Completed | `RegistrationStateGuard.EnsureCanCancel` + 唯一索引 `Status IN (0,1)` |
| Registration | Waiting→Cancelled | `Cancel` 需 `MedicalCaseId==null` 且 `CaseStatus!=Completed` | 同上，前端 `ReceptionistOnly` |
| MedicalCase | Draft→Active→Completed→Locked | `Save` Draft→Active；`Complete` Active→Completed；隔天 `IsLocked` | `MedicalCaseStateGuard.EnsureCanEdit/EnsureNotLocked` + `IMedicalCaseTimeService.IsLocked` |
| MedicalCase | Completed→Completed(编辑) | 需 `EditReason` | 同上，`RequiresEditReason` 展开 |
| Patient | Enabled↔Disabled | `ToggleStatus/Restore` | `AdminOrSuperAdmin` |
| Herb/Formula | Enabled↔Disabled | `ToggleStatus/BatchEnable/Disable` | `AdminOrSuperAdmin` |
| User | Enabled↔Disabled | `ToggleStatus` + 撤销 Token 族 | `AdminOrSuperAdmin` + `RevokeAllUserTokens` |

## 7. 验证规则汇总
| 字段 | 必填 | 格式 | 范围 | 唯一性 | 备注 |
|------|------|------|------|--------|------|
| PatientName | ✅ | 中文/英文 | 2-50 | ❌ | 失焦 + 提交双校验文案统一 `ValidationMessages` |
| Gender | ✅ | 枚举 | Unknown/Male/Female | — | — |
| PhoneNumber | ❌ | 11 位数字 | 7-20 | 查重 `ExistsByPhoneAsync` 防抖 500ms | 加密 `AesGcm` 列 200 |
| IdNumber | ❌ | 18 位 | 15-50 | 唯一过滤索引 `IsDeleted=0` 内存查询 | 加密，列 200 |
| BirthDate | ❌ | Date | <Today | — | 计算 Age |
| HerbName | ✅ | 文本 | 2-50 | 唯一 | — |
| Price | ❌ | decimal | 0-9999.99 | — | `decimal(10,2)` |
| FormulaName | ✅ | 文本 | 2-100 | 同名+组成查重 | — |
| Dosage | ✅ | decimal | >0 | — | `FormulaHerbItem` |
| Registration PatientName | ✅ | 文本 | 2-100 | — | `RegistrationInputDtoValidator` |
| RegistrationFee | ❌ | decimal | ≥0 | — | `UserBasicDto.RegistrationFee` 带出 |
| UserName | ✅创建 | 3-32 正则 | — | 唯一 | `UserInputDtoValidator` |
| Password | ✅创建 | 6-128 | — | — | `ConfirmPassword` 一致 |

## 8. API 端点映射
| 页面操作 | API | 方法 | 请求体 | 响应 | 权限 |
|---------|-----|------|--------|------|------|
| 登录 | /api/v1/auth/login | POST | LoginRequest | LoginResponse+Token | AllowAnonymous |
| 刷新 | /api/v1/auth/refresh | POST | RefreshTokenRequest | LoginResponse | AllowAnonymous |
| 患者列表 | /api/v1/patients | GET | ?page&pageSize&keyword&role&status | PagedResult | DoctorOrAdminOrReceptionist |
| 创建患者 | /api/v1/patients | POST | PatientInputDto | PatientDetailDto 201 | 同上 |
| 更新患者 | /api/v1/patients/{id} | PUT | PatientInputDto | PatientDetailDto | 同上 |
| 删除患者 | /api/v1/patients/{id} | DELETE | — | 204 | AdminOrSuperAdmin |
| 恢复患者 | /api/v1/patients/{id}/restore | POST | — | PatientDetailDto | AdminOrSuperAdmin |
| 批量删除 | /api/v1/patients/batch-delete | POST | BatchDeleteInputDto | BatchResult | AdminOrSuperAdmin |
| 按身份证查询 | /api/v1/patients/by-id-number/{id} | GET | — | PatientDetailDto | 同上 |
| 药材列表 | /api/v1/herbs | GET | ?page&keyword | PagedResult | DoctorOrAdmin |
| 创建药材 | /api/v1/herbs | POST | HerbInputDto | HerbDetailDto 201 | AdminOrSuperAdmin |
| 更新药材 | /api/v1/herbs/{id} | PUT | HerbInputDto | HerbDetailDto | AdminOrSuperAdmin |
| 删除药材 | /api/v1/herbs/{id} | DELETE | — | 204 | AdminOrSuperAdmin |
| 切换药材状态 | /api/v1/herbs/{id}/toggle-status | POST | — | HerbDetailDto | AdminOrSuperAdmin |
| 批量导入药材 | /api/v1/herbs/batch-import | POST | HerbBatchImportInputDto | ImportResult | AdminOrSuperAdmin |
| 验方列表 | /api/v1/formulas | GET | ?page&keyword | PagedResult | DoctorOrAdmin |
| 创建验方 | /api/v1/formulas | POST | FormulaInputDto | FormulaDetailDto 201 | DoctorOrAdmin |
| 更新验方 | /api/v1/formulas/{id} | PUT | FormulaInputDto | FormulaDetailDto | DoctorOrAdmin |
| 删除验方 | /api/v1/formulas/{id} | DELETE | — | 204 | DoctorOrAdmin |
| 批量导入验方 | /api/v1/formulas/batch-import | POST | List<FormulaImportItemDto> | ImportResult | AdminOrSuperAdmin |
| 克隆验方 | /api/v1/formulas/{id}/clone | POST | — | FormulaDetailDto | DoctorOrAdmin |
| 待校验验方 | /api/v1/formulas/pending-validation | GET | ?page | PagedResult | DoctorOrAdmin |
| 创建医案 | /api/v1/medicalcases | POST | MedicalCaseInputDto | MedicalCaseDetailDto | DoctorOnly |
| 更新医案 | /api/v1/medicalcases/{id} | PUT | MedicalCaseInputDto | MedicalCaseDetailDto | DoctorOrAdmin |
| 获取医案列表 | /api/v1/medicalcases | GET | ?page&keyword | PagedResult | DoctorOrAdmin |
| 完成医案 | /api/v1/medicalcases/{id}/complete | POST | — | MedicalCaseDetailDto | DoctorOrAdmin |
| 打印医案 | /api/v1/medicalcases/{id}/print | POST | PrintRequest | PrintResult | DoctorOnly |
| 审计日志 | /api/v1/medicalcases/{id}/audit-logs | GET | — | List<AuditLog> | DoctorOrAdmin |
| 创建挂号 | /api/v1/registrations | POST | RegistrationInputDto | RegistrationDto 201 | DoctorOrReceptionist |
| 取消挂号 | /api/v1/registrations/{id}/cancel | POST | — | 204 | ReceptionistOnly |
| 开始就诊 | /api/v1/registrations/{id}/start-visit | POST | — | MedicalCaseId | DoctorOnly |
| 待诊队列 | /api/v1/registrations/pending | GET | ?doctorId | List<Registration> | DoctorOrReceptionist |
| 报表-收入 | /api/v1/reports/income | GET | ?start&end&granularity | IncomeTrendDto | DoctorOrAdmin |
| 报表-接诊 | /api/v1/reports/consultation | GET | ?start&end | ConsultationTrendDto | DoctorOrAdmin |
| 报表-绩效 | /api/v1/reports/doctor-performance | GET | ?start&end | List<DoctorPerformanceDto> | DoctorOrAdmin |
| 报表-排行 | /api/v1/reports/herb-ranking | GET | ?top | List<HerbUsageItemDto> | DoctorOrAdmin |
| 用户列表 | /api/v1/users | GET | ?page&role | PagedResult | AdminOrSuperAdmin |
| 创建用户 | /api/v1/users | POST | UserInputDto | UserDetailDto 201 | AdminOrSuperAdmin |
| 禁用用户 | /api/v1/users/{id}/toggle-status | POST | — | UserDetailDto | AdminOrSuperAdmin |
| 配置 | /api/v1/configuration | GET/PUT | — | ConfigDto | SysAdminOnly |
| 健康检查 | /health | GET | — | Healthy | AllowAnonymous |
| 备份 | /api/v1/backup | POST/GET | — | BackupDto | SysAdminOnly |

## 9. 共享控件复用矩阵
| 控件 | 使用页面 | 说明 | 关键属性 |
|------|---------|------|----------|
| MasterDetailLayout | 患者/药材/验方/用户/挂号 | 统一主从 | `MasterWidth=380` |
| DataGridToolbar | 所有列表 | 搜索+新建+批量 | 统一顺序 |
| UnifiedPaginationBar | 所有分页 | 页码 | `共 {n} 条` |
| SearchBox | 所有列表 | 实时搜索 | `Delay=300ms` |
| InfoCard | 首页 | 统计 | `Title, Value, Icon, Trend` |
| StatusBadge | 状态列 | 启用/禁用/完成 | `Status` |
| BreadcrumbBar | 主界面 | 面包屑 | 完整路径可回溯 |
| EmptyState | 空列表 | 占位 | `暂无{实体} 去创建` |
| LoadingOverlay | 加载中 | 遮罩 | 强制文案 |
| ToastControl | 全局 | 提示 | `Success/Error/Warning + Action` |
| DetailToolbar | 详情 | 编辑/删除/打印 | `OnEdit, OnDelete, OnPrint` |
| PatientInfoCardControl | 医案工作台 | 患者信息 | `PatientDisplayModel` |
| HerbListControl | 处方 | 药材列表 | 拖拽排序 |
| WorkflowStepIndicator | 工作台 | 步骤 | `CurrentStep 1-5` |

## 10. 跨 R 去重后问题清单（P0/P1/P2）

| # | 问题 | 来源 | 级别 | 状态 |
|---|------|------|------|------|
| 1 | 药材/验方查看 Receptionist 误有权限（类级 `DoctorOrReceptionist`） | R02,R17,R22 | P1 | 待修：类级改为 `DoctorOrAdmin` |
| 2 | 打印无 `DoctorOnly` 细分 | R02,R17 | P1 | 待修 |
| 3 | 挂号取消无 `ReceptionistOnly` 细分 | R02,R17 | P1 | 待修 |
| 4 | 报表行级仅 Controller 下推，Service 无二次校验 | R07,R17 | P1 | 待修：`ReportService` 加 `ICurrentUserProvider` |
| 5 | 待诊队列无超时分级 | R01,R15 | P2 | 待办：>30黄 >60红 |
| 6 | 报表空数据无 `EmptyState` | R02,R16 | P2 | 待办 |
| 7 | 系统设置保存无持久确认 | R02,R12 | P2 | 待办：`✓ 已保存` |
| 8 | 挂号医生下拉无可用性 | R03,R15 | P2 | 待办：`StatusBadge` |
| 9 | 备份恢复文案风险不足 | R04,R22 | P1 | 待修：红警告+倒计时 |
| 10 | 日志级别无影响提示 | R04,R12 | P2 | 待办 |
| 11 | 部署上传无进度 | R04,R16 | P2 | 待办 |
| 12 | 跨页上下文丢失 | R05,R11 | P2 | 待办：`LastPatientId` 持久化 |
| 13 | StartVisit 无防重遮罩 | R05,R12 | P1 | 待修：`LoadingOverlay` 全屏 |
| 14 | Catalog 列表重复 | R06,R21 | P2 | 待办：泛型模板 |
| 15 | 验方导入无行级预览 | R06,R12 | P2 | 待办 |
| 16 | ViewNames 与 Region 不一致 | R09,R15 | P2 | 待办 |
| 17 | 导航参数明文无类型 | R09 | P2 | 待办：强类型 record |
| 18 | 列表 `KeepAlive` 未启用 | R09,R11 | P2 | 待办 |
| 19 | ComboBox 单向绑定 | R10 | P1 | 待修：`Mode=TwoWay` |
| 20 | CanExecute 未与校验联动 | R10,R18 | P1 | 待修 |
| 21 | 集合 `List` 非 Observable | R10 | P2 | 待办 |
| 22 | Session 切换残留 | R11 | P1 | 待修：登出重置 |
| 23 | 配置路径双轨 | R08,R11 | P2 | 待办：统一 `%LOCALAPPDATA%` |
| 24 | 保存失败无重试 | R12 | P2 | 待办：`Snackbar` 重试 Action |
| 25 | 422/500 未分级 | R12 | P2 | 待办：黄/红区分 |
| 26 | 行双击不一致 | R13 | P2 | 待办：单击选中双击编辑 |
| 27 | 快捷键未统一 | R13 | P2 | 待办：`Ctrl+S/P/Esc` |
| 28 | 处方拖拽未实现 | R13 | P2 | 待办 |
| 29 | 主色混用紫 | R14 | P1 | 待修：`TcmPrimaryBrush` |
| 30 | 间距非 8pt | R14 | P2 | 待办 |
| 31 | 投影层级错 | R14 | P2 | 待办 |
| 32 | 侧边栏无折叠 | R15 | P2 | 待办 |
| 33 | 面包屑仅一级 | R15 | P2 | 待办：完整路径 |
| 34 | 首页卡片权重失衡 | R15 | P2 | 待办：待诊大卡 |
| 35 | Loading 文案缺失 | R16 | P2 | 待办：强制文案 |
| 36 | 成功无常驻 | R16 | P2 | 待办 |
| 37 | 空/错视觉混用 | R16 | P2 | 待办 |
| 38 | 文案不一致 | R18 | P2 | 待办：统一 `ValidationMessages` |
| 39 | 查重无防抖 | R18 | P2 | 待办：500ms |
| 40 | ErrorTemplate 遮挡 | R18 | P2 | 待办：仅红框+Tooltip |
| 41 | 虚拟化未显式 | R19 | P2 | 待办 |
| 42 | 分页预加载未用 | R19 | P2 | 待办 |
| 43 | 报表懒加载未做 | R19 | P2 | 待办：首屏仅 income |
| 44 | TabIndex 未显式 | R20 | P2 | 待办 |
| 45 | 焦点未自动 | R20 | P2 | 待办 |
| 46 | 读屏 Name 未绑 | R20 | P2 | 待办 |
| 47 | 工具栏顺序不一致 | R21 | P2 | 待办 |
| 48 | 空文案不一致 | R21 | P2 | 待办 |
| 49 | 分页文案不一致 | R21 | P2 | 待办 |
| 50 | 设计稿与代码不一致 | R22 | P1 | 待修：补 4 图 |
| 51 | 推送无设计 | R22 | P2 | 待办 |
| 52 | 备份文案与需求不符 | R22 | P1 | 待修 |

> 共 52 项去重后：P1 12 项，P2 40 项，无 P0；R01-R22 已交叉引用，去重合并完成。

## 11. 实施建议
1. **P1 优先**（12 项，2 周）：权限细分 4 项 + 表单/绑定 3 项 + 保存/会话 3 项 + 设计对齐 2 项
2. **P2 分批**（40 项，4 周）：按页面聚合（患者/药材/医案/报表/运维）每 Sprint 10 项
3. **验证**：`build 0/0 + arch 91/91` 为门禁，每 P1 闭合后 `grep -r` 权限/校验一致性检查

> 本文档为 R01→R22 的综合（R23），可直接作为 `desktop-ui-detailed-design.md` 定版，`R01-R22` 22 份独立报告已归档于 `docs/compose/reports/ui-research-R*.md`。

---


---

## 附录 A：登录后统一框架（已确认）

> 决策依据：`docs/compose/reports/ui-layout-framework-decision.md`（12项功能需求对照，方案2得...[truncated]
分11/12，方案1=8/12，方案3=8/12）

### A.1 框架结构

```
┌──────────────────────────────────────────────────────────┐
│  头栏 48px (固定)                                         │
│  [☰汉堡] [面包屑 Level1›2›3] [全局搜索] [🔔通知] [👤头像] │
├──────────┬───────────────────────────────────────────────┤
│          │                                               │
│  侧边栏  │  ContentRegion (可内含 SplitView)              │
│  240/64px│  ┌──────────┬───────────────┐                │
│  可折叠   │  │ Master   │ Detail(可滚)  │                │
│  分组折叠 │  └──────────┴───────────────┘                │
│  临床/目录│  ← GridSplitter 12px 拖拽                    │
│  管理     │                                               │
├──────────┴───────────────────────────────────────────────┤
│  状态栏 32px (固定): API | 模式 | 用户 | 时间              │
└──────────────────────────────────────────────────────────┘
```

### A.2 固定元素规格

| 元素 | 位置 | 大小 | 内容 | 交互 |
|------|------|------|------|------|
| **头栏** | 窗口顶部 | 高 48px，全宽 | 汉堡按钮 + 面包屑 + 全局搜索 + 通知 + 头像下拉 | 汉堡折叠侧栏 Ctrl+M |
| **侧边栏** | 窗口左侧 | 宽 240px（折叠 64px 图标轨） | Logo + 分组导航（临床/目录/管理） + 账户/主题/退出 | 分组折叠，角色菜单动态过滤 |
| **状态栏** | 窗口底部 | 高 32px，全宽 | API状态灯 + 连接模式 + 用户角色 + 用户名 + 时间 | 只读 |
| **面包屑** | 头栏内 | 弹性 | 首页 > 当前模块 > 当前页面 | 点击可返回上级 |
| **GridSplitter** | Master/Detail 之间 | 宽 12px | 拖拽分隔线 | 悬停高亮，拖拽调整比例 |

### A.3 侧边栏菜单（按角色过滤）

| 角色 | 可见菜单项 |
|------|-----------|
| **Sysadmin** | 运维首页、备份恢复、日志级别、部署管理 |
| **Admin** | 首页、用户管理、患者管理、药材管理、验方管理、报表、系统设置 |
| **Doctor** | 首页、患者管理、医案管理、待诊队列、药材查看、验方管理、报表 |
| **Receptionist** | 首页、患者管理、挂号列表、待诊队列 |

### A.4 内容区域布局模式

| 模式 | 适用页面 | 结构 |
|------|---------|------|
| **Master-Detail** | 患者/药材/验方/用户/挂号管理 | 左 380px 固定 Master + 右弹性 Detail + GridSplitter |
| **Workspace** | 医案工作台 | 顶部患者卡 + 中部分栏（诊断/处方） |
| **Dashboard Grid** | 各角色首页 | 2-4 列 InfoCard 网格 |
| **Form Dialog** | 新增/编辑弹窗 | 居中 640×480 表单 |
| **Split View** | 待诊队列 | 左队列 + 右患者详情 |

### A.5 生成设计稿 Prompt 模板

```
设计要求：
1. 窗口尺寸 1440×900，Material Design 风格
2. 顶部 48px 头栏：汉堡按钮 + 面包屑导航 + 全局搜索 + 通知图标 + 用户头像
3. 左侧 240px 可折叠侧边栏（折叠后 64px 图标轨）：
   - 顶部 Logo + 系统名称
   - 中间分组导航菜单（临床/目录/管理分组）
   - 底部账户设置/主题切换/退出
4. 内容区域：页面特定内容 + GridSplitter（列表页）
5. 底部 32px 状态栏：API状态灯 + 连接模式 + 用户角色 + 用户名 + 时间
6. 统一设计 Token：主色 #1A5C3A，辅金 #C9A86A，背景 #F5F7FA
7. 侧边栏背景：MaterialDesign.Brush.Primary (Brown)
8. 所有文字使用思源黑体/微软雅黑
9. 间距基准 8pt (4/8/16/24/32)
10. 品牌名统一显示"中医诊所管理系统"
```
