# Desktop UI 需求文档
> 版本: v1.1 | 日期: 2026-09-13（审计修正）

> **基于**: desktop-ui-inventory-2026-08-15.md + desktop-design-spec.md + 10 个设计稿

---

## 设计稿对应关系

| # | 设计稿文件 | 对应页面 | 角色 | 优先级 |
|---|-----------|---------|------|--------|
| 1 | login.pen/png | 登录界面 | All | P0 |
| 2 | main-window.pen/png | 主界面（侧边栏+工作台） | All | P0 |
| 3 | patient-list.pen/png | 患者管理列表 | A/D/R | P0 |
| 4 | medical-case.pen/png | 医案工作台 | D | P0 |
| 5 | registration.pen/png | 挂号管理列表 | R/D | P0 |
| 6 | admin-home.pen/png | Admin 首页（仪表盘） | A | P0 |
| 7 | first-run.pen/png | 首次初始化向导（5步） | S | P0 |
| 8 | reports.pen/png | 报表首页 | A/D | P1 |
| 9 | user-management.pen/png | 用户管理 | A | P1 |
| 10 | sysadmin-home.pen/png | Sysadmin 运维设置 | S | P1 |

---

## 一、通用页面（All 角色）

### 1.1 登录界面
- **设计稿**: login.pen/png ✅
- **代码**: LoginView.xaml ✅ 已实现
- **功能**: 用户名/密码登录，记住密码，远程/本地模式切换，API 连接状态
- **交互**: 输入框实时验证，登录按钮 Loading 状态，错误 Toast 提示
- **US**: US-AUTH-001/009
- **优先级**: P0

### 1.2 主界面
- **设计稿**: main-window.pen/png ✅
- **代码**: MainWindow.xaml ✅ 已实现
- **功能**: 左侧可折叠侧边栏（8 个菜单项），顶部状态栏（连接模式/用户信息），右侧工作台
- **交互**: 侧边栏折叠/展开，菜单项图标+文字，角色感知菜单可见性
- **US**: US-SHELL-001/003/005
- **优先级**: P0

### 1.3 首次初始化向导
- **设计稿**: first-run.pen/png ✅
- **代码**: FirstRunSetupView.xaml ⚠️ 部分实现（仅基础框架）
- **功能**: 5 步向导：改密→诊所信息→连接模式→创建 Admin→完成
- **交互**: 步骤指示器（1-2-3-4-5），上一步/下一步按钮，表单验证
- **US**: US-SHELL-011
- **优先级**: P0（需完善 5 步 UI）

---

## 二、Admin 角色页面

### 2.1 Admin 首页（仪表盘）
- **设计稿**: admin-home.pen/png ✅
- **代码**: AdminHomeView.xaml ✅ 已实现
- **功能**: 统计卡片（今日挂号/收入/医生/待办），快捷操作，系统状态
- **交互**: 卡片点击跳转，数据实时刷新
- **US**: US-SHELL-003
- **优先级**: P0

### 2.2 用户管理
- **设计稿**: user-management.pen/png ✅
- **代码**: UserManagementView.xaml ✅ 已实现
- **功能**: 用户列表（搜索/筛选/分页），创建/编辑用户对话框，角色分配
- **交互**: 主从详情布局，批量操作，表单验证
- **US**: US-USER-001~012
- **优先级**: P1

### 2.3 诊所设置
- **设计稿**: 无独立设计稿（内嵌在 Admin 首页）
- **代码**: SystemSettingsView.xaml ✅ 已实现
- **功能**: 诊所配置（名称/地址/电话/科室/执照），功能开关，API 配置
- **交互**: 表单编辑，保存确认
- **US**: US-CFG-001~006
- **优先级**: P1

### 2.4 报表首页
- **设计稿**: reports.pen/png ✅
- **代码**: ReportsHomeView.xaml ✅ 已实现
- **功能**: 收入/就诊/药材报表，趋势图表，日期范围筛选，导出
- **交互**: 标签页切换，图表交互，导出按钮
- **US**: US-REPORT-001~004
- **优先级**: P1

---

## 三、Sysadmin 角色页面

### 3.1 运维设置
- **设计稿**: sysadmin-home.pen/png ✅
- **代码**: SysadminHomeView.xaml ✅ 已实现
- **功能**: 诊所信息/会话设置/连接设置/安全策略/功能开关/系统信息 + 备份恢复/远程部署/日志管理/安全审计/配置导入导出/服务端配置
- **交互**: 卡片网格布局，每个卡片有图标+操作按钮
- **US**: US-SHELL-018
- **优先级**: P1

### 3.2 数据库备份恢复
- **设计稿**: 无独立设计稿
- **代码**: BackupManagementView.xaml ✅ 已实现
- **功能**: LocalDB 备份/恢复/状态查看
- **US**: US-SHELL-013
- **优先级**: P1

### 3.3 远程部署
- **设计稿**: 无独立设计稿
- **代码**: DeploymentView.xaml ✅ 已实现
- **功能**: 远程服务器部署状态，版本信息
- **US**: US-SHELL-020
- **优先级**: P1

---

## 四、Doctor/临床角色页面

### 4.1 临床首页
- **设计稿**: 无独立设计稿（复用 main-window 设计）
- **代码**: ClinicalHomeView.xaml ✅ 已实现
- **功能**: 工作台概览，待诊患者数，今日收入，快捷操作
- **US**: US-SHELL-003
- **优先级**: P0

### 4.2 医案工作台
- **设计稿**: medical-case.pen/png ✅
- **代码**: MedicalCaseWorkspaceView.xaml ✅ 已实现
- **功能**: 患者信息卡片，诊断区域，处方编辑，操作面板，工作流步骤
- **交互**: 主从详情，步骤指示器，保存/挂起/完成/打印
- **US**: US-MC-001~020
- **优先级**: P0

### 4.3 患者管理
- **设计稿**: patient-list.pen/png ✅
- **代码**: PatientManagementView.xaml ✅ 已实现
- **功能**: 患者列表（搜索/筛选/分页），患者详情，创建/编辑患者
- **交互**: 主从详情布局，右侧详情面板
- **US**: US-PAT-001~014
- **优先级**: P0

### 4.4 药材管理
- **设计稿**: 无独立设计稿
- **代码**: HerbManagementView.xaml ✅ 已实现
- **功能**: 药材列表，药材详情，创建/编辑药材
- **US**: US-HERB-001~014
- **优先级**: P1

### 4.5 验方管理
- **设计稿**: 无独立设计稿
- **代码**: FormulaManagementView.xaml ✅ 已实现
- **功能**: 验方列表，验方详情，创建/编辑验方
- **US**: US-FORM-001~014
- **优先级**: P1

---

## 五、Receptionist/前台角色页面

### 5.1 挂号管理
- **设计稿**: registration.pen/png ✅
- **代码**: RegistrationListView.xaml ✅ 已实现
- **功能**: 挂号列表（搜索/筛选/分页），创建挂号对话框，挂号详情
- **交互**: 主从详情，创建对话框，患者选择
- **US**: US-REG-001~008
- **优先级**: P0

### 5.2 前台首页
- **设计稿**: 无独立设计稿（复用 main-window 设计）
- **代码**: ReceptionistHomeView.xaml ✅ 已实现
- **功能**: 前台工作台，挂号入口，排队查看
- **US**: US-SHELL-003
- **优先级**: P0

---

## 六、共享控件

> **控件清单权威来源**: [desktop-design-spec.md §8.1 共享控件规范](./desktop-design-spec.md)。
> 下表仅列实现状态。

| 控件 | 状态 |
|------|------|
| MasterDetailLayout | ✅ |
| BaseDetailContainer | ✅ |
| DataGridToolbar | ✅ |
| DetailToolbar | ✅ |
| SearchBox | ✅ |
| StatusBadge | ✅ |
| InfoCard | ✅ |
| EmptyState | ✅ |
| LoadingOverlay | ✅ |
| BreadcrumbBar | ✅ |
| UnifiedPaginationBar | ✅ |
| PatientInfoCardControl | ✅ |
| ToastControl | ✅ |
| WorkflowStepIndicator | ✅ |
| HerbListControl | ✅ |
| HerbItemControl | ✅ |
| FormulaViewControl | ✅ |
| PatientSelectionControl | ✅ |
| ConfirmationDialog | ✅ |
| InputDialog | ✅ |
| MessageDialog | ✅ |

---

## 七、待完善/缺失

| # | 页面 | 状态 | 需要操作 |
|---|------|------|---------|
| 1 | FirstRunSetupView | ⚠️ 部分实现 | 需完善 5 步向导 UI |
| 2 | ReportsHomeView | ⚠️ 仅 3/8 端点 | 需扩展趋势/绩效报表 |
| 3 | 安全审计日志 | ✅ 已实现 | SecurityAuditLogView（2026-08-29） |
| 4 | 数据导入导出 | ⚠️ 部分实现 | JSON 导入导出已实现；独立配置导入导出页待新建 |

---

## 八、设计稿中需修正的问题

| # | 问题 | 涉及文件 | 操作 |
|---|------|---------|------|
| 1 | 编造品牌名"良医堂"/"凌隐宝堂" | 所有 10 个 .pen 文件 | 用 MCP 修正为"中医诊所管理系统" |
| 2 | 系统名称应为可配置占位符 | 所有设计稿 | 统一显示"中医诊所管理系统" |
