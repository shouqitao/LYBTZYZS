# Desktop View 全景图 — 基于需求文档的 View 清单

> 版本: v0.1 | 日期: 2026-08-19 | 状态: 草案（待用户确认）
> 来源: 13 个需求文档 + 权限矩阵 + 现有代码 + Desktop 架构文档

---

## 设计原则

1. **每个 View 对应一个导航目标**（Prism Region 注册），不是每个弹窗
2. **按用户故事驱动**：从登录开始，跟随每个角色的工作时序排列
3. **角色决定入口**：`RoleRegistry` 按角色加载不同模块 → 不同角色看到不同 View
4. **细节可粗糙**：本文档先定 View 清单和导航关系，不深入控件细节
5. **主页卡片导航（方案 A，2026-08-17 确认）**：每个角色的 Home View 是导航中心，以功能卡片网格展示该角色所有入口；侧边栏仅保留全局操作（个人资料/主题切换/退出），不放功能导航

---

## 全局 View 清单（按层/模块）

### Shell 层（所有角色共享）

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| S-01 | **MainWindow** | Shell/Views/ | 主窗口，含 ContentRegion + 菜单栏 + 状态栏 | US-SHELL-005 |
| S-02 | **AccountSettingsView** | Shell/Views/ | **个人资料**（姓名/密码/头像/会话超时），所有角色可用 | US-SHELL-004 / US-USER-008 / US-USER-009 |

**Shell 对话框**（弹窗，非导航）：

| # | Dialog 名 | 位置 | 说明 | 需求依据 |
|---|-----------|------|------|----------|
| SD-01 | **ConfirmationDialog** | Shell/Dialogs/ | 通用确认弹窗（删除/操作确认） | 通用 |
| SD-02 | **InputDialog** | Shell/Dialogs/ | 通用输入弹窗 | 通用 |
| SD-03 | **MessageDialog** | Shell/Dialogs/ | 通用消息提示弹窗 | US-ERR-002 |

---

### Auth 模块（所有角色登录入口）

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| A-01 | **LoginView** | Auth/Views/ | 登录页（用户名+密码+模式选择） | US-AUTH-001 |
| A-02 | **ServerConfigView** | Auth/Views/ | 服务器地址配置（远程模式） | US-SHELL-007 |
| A-03 | **FirstRunSetupView** | Auth/Views/ | 首次运行向导（旧版，待迁移） | US-SHELL-011 |

**Auth 对话框**：

| # | Dialog 名 | 位置 | 说明 | 需求依据 |
|---|-----------|------|------|----------|
| AD-01 | **SessionTimeoutWarningDialog** | Auth/ | 会话超时警告（30秒倒计时） | US-AUTH-005 / ClientSessionOptions |

---

### ⭐ 首次初始化向导（sysadmin 专属，待新建）

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| W-01 | **InitializationWizardView** | Auth/ 或 Shell/ | 5 步强制向导：①改密码 → ②诊所信息 → ③选择模式 → ④创建 Admin → ⑤完成提示 | US-SHELL-011 |

> 这是一个**独立全屏向导**，登录成功后 sysadmin 首次进入时强制展示，不可跳过。完成后才进入 MainWindow。

---

### Sysadmin 角色（SuperAdmin 登录后）

sysadmin 登录 → 若首次走 W-01 向导 → 否则进入 **SysadminHomeView**（运维设置）。

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| SY-01 | **SysadminHomeView** | Admin/Sysadmin/Views/ | **运维设置**主页（7 组配置面板） | US-SHELL-018 |

**SysadminHomeView 内嵌面板（Tab 或分组）**：

| # | 面板 | 说明 | 需求依据 |
|---|------|------|----------|
| SY-01a | 诊所信息 | Name/Address/Phone/Department/License/Email | US-SHELL-018 |
| SY-01b | 会话设置 | InactivityTimeout/WarningBefore/ActivityCheck | US-SHELL-018 |
| SY-01c | 连接设置 | BaseUrl + 测试连通 + Timeout | US-SHELL-018 / US-SHELL-007 |
| SY-01d | 安全策略 | ForceChangeOnFirstLogin / NewUserPassword | US-SHELL-018 |
| SY-01e | 功能开关 | OverwriteConflicts / DuplicateHerbMergeStrategy（热更新） | US-SHELL-018 |
| SY-01f | 系统信息 | 版本/DB状态/连接状态（只读） | US-SHELL-018 |

**Sysadmin 子页面**（从运维设置导航进入）：

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| SY-02 | **BackupManagementView** | Admin/Sysadmin/Views/ | 备份文件列表 + 手动备份 + 恢复操作 | US-SHELL-013 |
| SY-03 | **DeploymentView** | Admin/Sysadmin/Views/ | 远程部署上传 + 服务重启 | US-SHELL-020 |
| SY-04 | **LogLevelControlView** | Admin/Sysadmin/Views/ | 运行时日志级别调整 | US-LOG-005 / US-SYS-006 |
| SY-05 | **CardReaderDiagnosticsView** | Sysadmin/Views/ | 读卡器诊断测试面板 | US-SHELL-019 |
| SY-06 | **SecurityAuditLogView** | Admin/Sysadmin/Views/ | 安全审计日志查看（2026-08-29 已实现；仅远程） | US-SHELL-014 / US-LOG-004 |
| SY-07 | **ConfigExportImportView** | Sysadmin/Views/ | 配置导出/导入 | US-SHELL-016 |
| SY-08 | **ServerConfigPanelView** | Sysadmin/Views/ | 服务端配置面板（仅远程模式） | US-SHELL-018 双模式面板 / ADR-0014 |

---

### Admin 角色（Admin 登录后）

Admin 登录 → 加载管理模块 → 进入 **AdminHomeView**。

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| ADM-01 | **AdminHomeView** | Admin/Views/ | 管理工作台主页（仪表盘/快捷入口） | US-SHELL-003 |

**Admin 可导航的业务 View**：

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| ADM-02 | **UserManagementView** | Admin/Views/ | 用户列表 + CRUD + 批量操作 | US-USER-001~012 |
| ADM-03 | **PatientManagementView** | Clinical/Views/ | 患者列表 + CRUD + 导入导出 | US-PAT-001~014 |
| ADM-04 | **HerbManagementView** | Clinical/Views/ | 药材列表 + CRUD + 批量导入导出 | US-HERB-001~014 |
| ADM-05 | **FormulaManagementView** | Clinical/Views/ | 验方列表 + CRUD + 导入导出 | US-FORM-001~014 |
| ADM-06 | **MedicalCaseManagementView** | Clinical/Views/ | 医案列表（只读查看全部） + 详情 | US-MC-005/015 |
| ADM-07 | **RegistrationListView** | Registrations/Views/ | 挂号列表（只读查看） | US-REG-004 |
| ADM-08 | **ReportsHomeView** | MedicalCase/Reports/Views/ | 报表页面（收入/就诊/药材排行/趋势） | US-REPORT-001~004 |

> **Admin 权限边界**：Admin 只读查看挂号/医案，不可创建/取消/接诊；药材/用户管理可 CRUD；验方可 CRUD（含自己的 + 全部）。

---

### Doctor 角色（医生登录后）

Doctor 登录 → 加载临床模块 → 进入 **ClinicalHomeView**。

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| DOC-01 | **ClinicalHomeView** | Clinical/Views/ | 临床工作台主页（快捷入口/今日统计） | US-SHELL-003 |

**Doctor 核心工作流 View**（按诊疗时序排列）：

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| DOC-02 | ~~PendingQueueView~~ | Clinical/Views/ | ~~独立待诊队列页~~ → **已吸收**：UI 内嵌 `PatientSelectionView`，`PendingQueueViewModel` 保留（XAML 2026-08-29 删除） | US-REG-004 / US-REG-008 |
| DOC-03 | **PatientSelectionView** | Clinical/Views/ | 患者选择/搜索（新建医案前选患者） | US-MC-001 / US-MC-006 |
| DOC-04 | **ClinicalWorkspaceView** | Clinical/Views/ | ⭐ 临床工作台（主编辑界面：诊断+处方+医案状态管理） | US-MC-001~004 / BR-000~003 |
| DOC-05 | **MedicalCaseWorkspaceView** | Clinical/Views/ | 医案详情工作台（含 EditModeStateMachine） | US-MC-002/004 |
| DOC-06 | **MedicalCaseManagementView** | Clinical/Views/ | 医案列表（仅自己的 + 分页搜索） | US-MC-005/006/007 |
| DOC-07 | **FormulaManagementView** | Clinical/Views/ | 验方管理（自己创建 + 共享可见 + 验证） | US-FORM-001~012 |
| DOC-08 | **HerbManagementView** | Clinical/Views/ | 药材查看（只读查询 + 缓存 US-HERB-014） | US-HERB-001/014 |
| DOC-09 | **PatientManagementView** | Clinical/Views/ | 患者管理（CRUD + 读卡登记） | US-PAT-001~014 |
| DOC-10 | **RegistrationListView** | Registrations/Views/ | 挂号列表（仅自己的） + QuickVisit 入口 | US-REG-002 / US-REG-004 |
| DOC-11 | **ReportsHomeView** | MedicalCase/Reports/Views/ | 报表（收入/就诊统计/药材排行） | US-REPORT-001~003 |
| DOC-12 | **AuditLogView** | MedicalCase/Views/ | 医案审计日志 | US-MC-017 |

**Doctor 对话框（弹窗）**：

| # | Dialog 名 | 位置 | 说明 | 需求依据 |
|---|-----------|------|------|----------|
| DD-01 | **FormulaImportDialog** | MedicalCase/Dialogs | 从验方库导入药材到处方 | US-MC-016 / US-FORM-0013 |
| DD-02 | **HistoryCopyDialog** | MedicalCase/Dialogs | 从历史医案复制处方微调 | US-MC-019 |
| DD-03 | **UnsavedChangesDialog** | MedicalCase/Dialogs | 未保存修改确认（保存/放弃/取消） | BR-002 |
| DD-04 | **UnfinishedCaseDialog** | Infrastructure/ViewModels | 未完成医案处理（继续/新建/关闭） | US-MC-013 |
| DD-05 | **PrintPreviewDialog** | Printing/ | 处方打印预览 | US-PRINT-001/002 |

---

### Receptionist 角色（前台登录后）

Receptionist 登录 → 加载患者管理+挂号模块 → 进入 **ReceptionistHomeView**。

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| REC-01 | **ReceptionistHomeView** | Clinical/Receptionist/Views/ | 前台工作台主页 | US-SHELL-003 |

**Receptionist 业务 View**：

| # | View 名 | 位置 | 说明 | 需求依据 |
|---|---------|------|------|----------|
| REC-02 | **PatientManagementView** | Clinical/Views/ | 患者管理（CRUD + 读卡登记） | US-PAT-001~014 / US-CARD-001 |
| REC-03 | **RegistrationListView** | Registrations/Views/ | 挂号列表（创建+取消） | US-REG-001 / US-REG-006 |
| REC-04 | **RegistrationCreateView**（或弹窗） | Registrations/Views/ | 新建挂号（选患者→选医生→确认） | US-REG-001 |

> **Receptionist 权限边界**：仅患者 CRUD + 挂号创建/取消；不可查看药材/验方/医案/用户管理/打印/报表。

---

## 导航时序图（按角色）

### 全局启动流程

```
App 启动
  │
  ├─ 单实例互斥检查（US-SHELL-001）
  │   └─ 已有实例 → 拒绝启动
  │
  ├─ Splash Screen（Logo + 进度条 ≥1s）
  │   ├─ Step 1: ErrorHandling 初始化
  │   ├─ Step 2: CoreServices 初始化（API 客户端/缓存/映射）
  │   ├─ Step 3: ApiHealthCheck（后台，不阻塞）
  │   └─ Step 4: Warmup（预加载资源）
  │
  ├─ API 不可达？
  │   ├─ 是 → 提示"切换到本地模式"按钮 → ServerConfigView（A-02）
  │   └─ 否 → 进入登录
  │
  └─ LoginView（A-01）← ★ 所有角色的入口
```

### Sysadmin 导航时序

```
LoginView（A-01）
  │
  ├─ 首次登录？
  │   └─ 是 → InitializationWizardView（W-01）← 5 步强制向导
  │           ├─ Step 1: 强制修改默认密码
  │           ├─ Step 2: 填写诊所信息（名称/科室/地址/电话）
  │           ├─ Step 3: 选择远程/本地模式
  │           ├─ Step 4: 创建首个 Admin 账号
  │           └─ Step 5: 完成 → 注销 sysadmin → 回到 LoginView
  │
  └─ 非首次 → SysadminHomeView（SY-01）← 运维设置主页
              │
              ├─ [Tab: 诊所信息]        ← SY-01a
              ├─ [Tab: 会话设置]        ← SY-01b
              ├─ [Tab: 连接设置]        ← SY-01c  ──→ 可导航到 ServerConfigView（远程模式）
              ├─ [Tab: 安全策略]        ← SY-01d
              ├─ [Tab: 功能开关]        ← SY-01e（热更新即时生效）
              ├─ [Tab: 系统信息]        ← SY-01f（只读）
              │
              ├─ [导航] → BackupManagementView（SY-02）← 备份恢复
              ├─ [导航] → DeploymentView（SY-03）← 远程部署
              ├─ [导航] → LogLevelControlView（SY-04）← 日志级别
              ├─ [导航] → CardReaderDiagnosticsView（SY-05）← 读卡器诊断
              ├─ [导航] → SecurityAuditLogView（SY-06）← 安全审计
              ├─ [导航] → ConfigExportImportView（SY-07）← 配置导出/导入
              └─ [导航] → ServerConfigPanelView（SY-08）← 服务端配置（仅远程）
```

### Admin 导航时序

```
LoginView（A-01）
  │
  └─ AdminHomeView（ADM-01）← 管理工作台主页
      │
      ├─ [菜单: 用户管理]     → UserManagementView（ADM-02）
      │                           ├─ 用户列表（分页 + 搜索）
      │                           ├─ 创建用户（弹窗/子页面）
      │                           ├─ 编辑用户
      │                           ├─ 重置密码
      │                           └─ 批量操作（启用/禁用/删除）
      │
      ├─ [菜单: 患者管理]     → PatientManagementView（ADM-03）
      │                           ├─ 患者列表（分页 + 拼音码搜索）
      │                           ├─ 创建/编辑患者
      │                           ├─ 导入/导出
      │                           └─ 恢复软删除
      │
      ├─ [菜单: 药材管理]     → HerbManagementView（ADM-04）
      │                           ├─ 药材列表（分页 + 搜索）
      │                           ├─ 创建/编辑药材
      │                           ├─ 批量导入（Excel）
      │                           ├─ 导出全部药材
      │                           └─ 引用检查
      │
      ├─ [菜单: 验方管理]     → FormulaManagementView（ADM-05）
      │                           ├─ 验方列表（自己的 + 共享的）
      │                           ├─ 创建/编辑验方
      │                           └─ 导入/导出
      │
      ├─ [菜单: 医案查看]     → MedicalCaseManagementView（ADM-06）← 只读
      │                           └─ 医案详情
      │
      ├─ [菜单: 挂号查看]     → RegistrationListView（ADM-07）← 只读
      │
      ├─ [菜单: 报表]         → ReportsHomeView（ADM-08）
      │                           ├─ 收入报表（按时间范围）
      │                           ├─ 就诊统计
      │                           ├─ 药材使用排行
      │                           └─ 趋势与绩效（仅远程）
      │
      └─ [菜单: 个人资料]     → AccountSettingsView（S-02）
```

### Doctor 导航时序（核心诊疗流程）

```
LoginView（A-01）
  │
  └─ ClinicalHomeView（DOC-01）← 临床工作台主页
      │
      │  ═══════════════════════════════════════════════
      │  ★ 核心诊疗流程（时序从上到下）
      │  ═══════════════════════════════════════════════
      │
      ├─ [菜单: 待诊队列]     → PendingQueueView（DOC-02）
      │     │  患者到达 → 从前台挂号列表选中 → 点击"开始看诊"
      │     │
      │     ├─ 方式 1: 从待诊队列选中 → StartVisit（Waiting→InProgress + 创建医案）
      │     └─ 方式 2: QuickVisit（US-REG-002 两步：建 Waiting 挂号 → StartVisit）
      │
      │  ──── StartVisit 完成后 ────
      │
      ├─ → ClinicalWorkspaceView（DOC-04）← ⭐ 核心！诊疗主界面
      │     │
      │     ├─ Tab/区域: 患者基本信息（只读）
      │     ├─ Tab/区域: 中医诊断（Consultation 编辑）
      │     │     ├─ 主诉 / 现病史 / 既往史
      │     │     ├─ 望诊 / 闻诊 / 问诊 / 切诊
      │     │     └─ 辨证结论（TcmDiagnosis，BR-003 必填）
      │     ├─ Tab/区域: 处方编辑（Prescription 编辑）
      │     │     ├─ HerbListControl（药材列表控件，复用）
      │     │     ├─ [按钮] 验方导入 → FormulaImportDialog（DD-01）
      │     │     ├─ [按钮] 复制历史处方 → HistoryCopyDialog（DD-02）
      │     │     └─ 药材数量 + 总金额（实时计算）
      │     ├─ [按钮] 保存（聚合保存 US-MC-002）
      │     ├─ [按钮] 挂起（US-MC-013）
      │     ├─ [按钮] 完成（US-MC-011，需通过 BR-003 校验）
      │     ├─ [按钮] 取消/关闭（US-MC-014）
      │     └─ [按钮] 打印处方（仅 Completed，US-PRINT-001）
      │           └─ → PrintPreviewDialog（DD-05）→ 打印机
      │
      │  ═══════════════════════════════════════════════
      │  ★ 医案管理（非诊疗流程，日常管理）
      │  ═══════════════════════════════════════════════
      │
      ├─ [菜单: 医案列表]     → MedicalCaseManagementView（DOC-06）
      │                           ├─ 分页查询（仅自己的医案）
      │                           ├─ 按状态筛选（Active/Suspended/Completed）
      │                           ├─ 跨模块搜索（患者+诊断+日期 US-MC-007）
      │                           └─ 选中 → 详情 → ClinicalWorkspaceView
      │
      ├─ [菜单: 验方管理]     → FormulaManagementView（DOC-07）
      │                           ├─ 验方列表（自己的 + 共享的）
      │                           ├─ 创建/编辑验方
      │                           ├─ 待验证验方（US-FORM-007）
      │                           └─ 验证药材绑定（US-FORM-008）
      │
      ├─ [菜单: 药材查看]     → HerbManagementView（DOC-08）← 只读
      │
      ├─ [菜单: 患者管理]     → PatientManagementView（DOC-09）
      │
      ├─ [菜单: 挂号]         → RegistrationListView（DOC-10）
      │                           ├─ 我的待诊列表
      │                           └─ [按钮] 快速看诊（QuickVisit 两步）
      │
      ├─ [菜单: 报表]         → ReportsHomeView（DOC-11）
      │
      ├─ [菜单: 审计日志]     → AuditLogView（DOC-12）
      │
      └─ [菜单: 个人资料]     → AccountSettingsView（S-02）
```

### Receptionist 导航时序

```
LoginView（A-01）
  │
  └─ ReceptionistHomeView（REC-01）← 前台工作台主页
      │
      ├─ [菜单: 患者管理]     → PatientManagementView（REC-02）
      │                           ├─ 新患者登记（读卡/手动）
      │                           ├─ 查找已有患者（拼音码/身份证号）
      │                           ├─ 编辑患者信息
      │                           └─ 读卡登记（US-CARD-001）
      │
      ├─ [菜单: 挂号]         → RegistrationListView（REC-03）
      │                           ├─ 创建挂号（选患者 → 选医生 → 确认 → Waiting）
      │                           ├─ 取消挂号（仅当天 Waiting + 无医案）
      │                           └─ 查看排队状态
      │
      └─ [菜单: 个人资料]     → AccountSettingsView（S-02）

  ⚠️ Receptionist 不可访问：药材/验方/医案/用户管理/打印/报表
```

---

## View 与现有代码对照

| 现有 View | 所属层 | 状态 | 差距 |
|-----------|--------|------|------|
| LoginView | Auth | ✅ 已有 | — |
| ServerConfigView | Auth | ✅ 已有 | — |
| FirstRunSetupView | Auth | ✅ 已有 | 待迁移到 InitializationWizard |
| MainWindow | Shell | ✅ 已有 | — |
| AccountSettingsView | Shell | ✅ 已有 | — |
| SysadminHomeView | Admin/Sysadmin | ✅ 已有 | — |
| BackupManagementView | Admin/Sysadmin | ✅ 已有 | — |
| DeploymentView | Admin/Sysadmin | ✅ 已有 | — |
| LogLevelControlView | Admin/Sysadmin | ✅ 已有 | — |
| AdminHomeView | Admin | ✅ 已有 | — |
| UserManagementView | Admin | ✅ 已有 | — |
| SystemSettingsView | Admin | ✅ 已有 | **诊所设置**（诊所名称/地址/电话等业务信息） |
| ClinicalHomeView | Clinical | ✅ 已有 | — |
| ClinicalWorkspaceView | Clinical | ✅ 已有 | — |
| MedicalCaseWorkspaceView | Clinical | ✅ 已有 | — |
| MedicalCaseManagementView | Clinical | ✅ 已有 | — |
| FormulaManagementView | Clinical | ✅ 已有 | — |
| HerbManagementView | Clinical | ✅ 已有 | — |
| PatientManagementView | Clinical | ✅ 已有 | — |
| PatientSelectionView | Clinical | ✅ 已有 | — |
| PendingQueueView | Clinical | ~~✅~~ XAML 已删（2026-08-29） | 功能嵌入 PatientSelectionView，PendingQueueViewModel 保留为子组件 |
| ReceptionistHomeView | Clinical/Receptionist | ✅ 已有 | — |
| RegistrationListView | Registrations | ✅ 已有 | — |
| AuditLogView | MedicalCase | ✅ 已有 | — |
| ReportsHomeView | MedicalCase/Reports | ✅ 已有 | — |
| ConfirmationDialog | Shell/Dialogs | ✅ 已有 | — |
| InputDialog | Shell/Dialogs | ✅ 已有 | — |
| MessageDialog | Shell/Dialogs | ✅ 已有 | — |
| **InitializationWizardView** | Auth/Shell | 🔴 **待新建** | US-SHELL-011 5 步向导 |
| **CardReaderDiagnosticsView** | Sysadmin | 🔴 **待新建** | US-SHELL-019 |
| **SecurityAuditLogView** | Sysadmin | ✅ 2026-08-29 | US-SHELL-014 |
| **ConfigExportImportView** | Sysadmin | 🔴 **待新建** | US-SHELL-016 |
| **ServerConfigPanelView** | Sysadmin | 🔴 **待新建** | US-SHELL-018 双模式面板 |

---

## View 数量汇总

| 类别 | 已有 | 待新建 | 合计 |
|------|:----:|:------:|:----:|
| Shell 全局 | 2 | 0 | 2 |
| Shell 对话框 | 3 | 0 | 3 |
| Auth 模块 | 3 | 0 | 3 |
| 初始化向导 | 0 | 1 | 1 |
| Sysadmin 面板+子页面 | 4 | 4 | 8 |
| Admin 角色 | 3 | 0 | 3 |
| Doctor 角色（含复用） | 7 | 0 | 7 |
| Receptionist 角色（含复用） | 2 | 0 | 2 |
| 业务对话框 | 5 | 0 | 5 |
| **合计** | **29** | **5** | **34** |

> 注：很多 View 是多角色复用的（如 PatientManagementView、HerbManagementView），实际独立 XAML 文件数 < View 逻辑数。

---

## 关键待确认问题

1. ~~**SystemSettingsView 的定位**~~ ✅ **已确认**：`SystemSettingsView` = **诊所设置**（诊所名称/地址/电话等业务信息），归 Admin 角色；`SysadminHomeView` = **运维设置**（系统运行配置），归 Sysadmin 角色。两者职责不同，不重叠。
2. ~~**导航架构**~~ ✅ **已确认（2026-08-17）**：采用 **方案 A「主页卡片导航」**——每个角色的 Home View 是导航中心，以功能卡片网格展示该角色所有入口；侧边栏仅保留全局操作（个人资料/主题切换/退出），不放功能导航。与 pen.dev 设计稿一致。
3. **注册/挂号创建是弹窗还是独立页面**？US-REG-001 前台创建挂号 → 当前 `RegistrationListView` 内嵌创建，还是独立 `RegistrationCreateView`？
4. **报表在 Sysadmin 下可见吗**？权限矩阵 SuperAdmin 有报表查看权限，但模块加载表 SuperAdmin 加载"全部模块"——sysadmin 登录后是否也加载 Clinical 模块的 ReportsHomeView？
5. **Receptionist 的 PatientSelectionView**：前台是否需要患者选择视图（从挂号流程选患者），还是只在 PatientManagementView 内完成？
