# LYBTZYZS 产品功能完整清单

> 审计日期: 2026-08-02 | 基于: 17 个 Server Controller + 24 个 Desktop 视图 + 审计报告

---

## 模块总览

| 模块 | Server API | Desktop 角色 | 功能完备度 |
|------|-----------|-------------|-----------|
| 认证授权 (Auth) | 5 个端点 | 登录/首次配置/服务器设置 | ✅ 基本完备 |
| 用户管理 (Users) | 14 个端点 | 管理员-用户管理 | ✅ 基本完备 |
| 患者管理 (Patients) | 10 个端点 | 临床-患者管理 | ✅ 基本完备 |
| 药材管理 (Herbs) | 13 个端点 | 临床-药材管理 | ✅ 基本完备 |
| 验方管理 (Formulas) | 12 个端点 | 临床-验方管理 | ✅ 基本完备 |
| 医案管理 (MedicalCase) | 18 个端点 | 临床-医案工作区 | ⚠️ 部分功能 |
| 挂号管理 (Registration) | 7 个端点 | 临床-前台/挂号列表 | ⚠️ 部分功能 |
| 统计报表 (Reports) | 3 个端点 | 医疗-报表首页 | ⚠️ 功能有限 |
| 系统配置 (Configuration) | 3 个端点 | 管理员-系统设置 | ⚠️ 功能有限 |
| 诊断调试 (Diagnostics) | 4 个端点 | 管理员-日志级别控制 | ✅ 基本完备 |
| 部署管理 (Deploy) | 2 个端点 | 管理员-部署视图 | ⚠️ 功能有限 |
| Shell 基础设施 | — | 账户设置/导航/登录状态 | ⚠️ 部分缺失 |

---

## 一、认证授权 (Auth)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| A-01 | 用户名密码登录 | `POST /auth/login` | LoginView | ✅ |
| A-02 | 登出 | `POST /auth/logout` | Shell 登出按钮 | ✅ |
| A-03 | Token 刷新 | `POST /auth/refresh` | 自动（SwitchingApiClient） | ✅ |
| A-04 | 自动登录（免密） | `POST /auth/auto-login` | — | ✅ API 层 |
| A-05 | Token 验证 | `GET /auth/validate` | 自动（启动时校验） | ✅ |
| A-06 | 服务器地址配置 | — | ServerConfigView | ✅ |
| A-07 | 首次运行向导 | — | FirstRunSetupView | ✅ 基础版 |
| A-08 | 登录限流 | `[EnableRateLimiting("Login")]` | — | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| A-M1 | 修改密码（个人） | API 有 `PUT /users/{id}/change-password`，Desktop 无独立 UI | 中 |
| A-M2 | 多设备登录控制 | 无踢出/会话管理机制 | 低 |
| A-M3 | 自动登录令牌管理 | 无生成/撤销自动登录令牌的 UI | 低 |

---

## 二、用户管理 (Users)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| U-01 | 用户列表（分页） | `GET /users` | UserManagementView | ✅ |
| U-02 | 用户详情 | `GET /users/{id}` | UserManagementView | ✅ |
| U-03 | 创建用户 | `POST /users` | UserManagementView | ✅ |
| U-04 | 更新用户 | `PUT /users/{id}` | UserManagementView | ✅ |
| U-05 | 删除用户（软删除） | `DELETE /users/{id}` | UserManagementView | ✅ |
| U-06 | 切换启用/禁用 | `POST /users/{id}/toggle-status` | UserManagementView | ✅ |
| U-07 | 恢复已删除用户 | `POST /users/{id}/restore` | UserManagementView | ✅ |
| U-08 | 批量删除 | `POST /users/batch-delete` | UserManagementView | ✅ |
| U-09 | 批量启用 | `POST /users/batch-enable` | UserManagementView | ✅ |
| U-10 | 批量禁用 | `POST /users/batch-disable` | UserManagementView | ✅ |
| U-11 | 重置密码 | `POST /users/{id}/reset-password` | UserManagementView | ✅ |
| U-12 | 获取当前用户 | `GET /users/current` | AccountSettingsView | ✅ |
| U-13 | 修改个人资料 | `PUT /users/{id}/profile` | AccountSettingsView | ✅ |
| U-14 | 修改密码 | `PUT /users/{id}/change-password` | AccountSettingsView | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| U-M1 | 用户头像 | 无头像上传/显示 | 低 |
| U-M2 | 用户操作日志 | 无用户级别的操作审计（医案有） | 低 |
| U-M3 | 角色分配 UI | 角色在创建时设定，无独立角色管理界面 | 中（当前角色由 SeedData 管理） |

---

## 三、患者管理 (Patients)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| P-01 | 患者列表（分页） | `GET /patients` | PatientManagementView | ✅ |
| P-02 | 患者详情 | `GET /patients/{id}` | PatientManagementView | ✅ |
| P-03 | 创建患者 | `POST /patients` | PatientManagementView | ✅ |
| P-04 | 更新患者 | `PUT /patients/{id}` | PatientManagementView | ✅ |
| P-05 | 删除患者（软删除） | `DELETE /patients/{id}` | PatientManagementView | ✅ |
| P-06 | 切换启用/禁用 | `POST /patients/{id}/toggle-status` | PatientManagementView | ✅ |
| P-07 | 恢复已删除患者 | `POST /patients/{id}/restore` | PatientManagementView | ✅ |
| P-08 | 批量删除 | `POST /patients/batch-delete` | PatientManagementView | ✅ |
| P-09 | 检查患者引用关系 | `GET /patients/{id}/check-reference` | PatientManagementView | ✅ |
| P-10 | 批量检查引用 | `POST /patients/batch-check-reference` | PatientManagementView | ✅ |
| P-11 | 身份证号查询 | `GET /patients/by-id-number/{idNumber}` | PatientSelectionView | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| P-M1 | 患者导入/导出 | API 无 import-template/export 端点 | 中（T4 已规划） |
| P-M2 | 患者就诊历史汇总 | PatientSelectionView 有基础，但无独立的「患者 360」视图 | 低 |
| P-M3 | 患者分类/标签 | 无患者分类体系 | 低 |

---

## 四、药材管理 (Herbs)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| H-01 | 药材列表（分页） | `GET /herbs` | HerbManagementView | ✅ |
| H-02 | 药材详情 | `GET /herbs/{id}` | HerbManagementView | ✅ |
| H-03 | 创建药材 | `POST /herbs` | HerbManagementView | ✅ |
| H-04 | 更新药材 | `PUT /herbs/{id}` | HerbManagementView | ✅ |
| H-05 | 删除药材（软删除） | `DELETE /herbs/{id}` | HerbManagementView | ✅ |
| H-06 | 切换启用/禁用 | `POST /herbs/{id}/toggle-status` | HerbManagementView | ✅ |
| H-07 | 恢复已删除药材 | `POST /herbs/{id}/restore` | HerbManagementView | ✅ |
| H-08 | 批量删除 | `POST /herbs/batch-delete` | HerbManagementView | ✅ |
| H-09 | 检查引用关系 | `GET /herbs/{id}/check-reference` | HerbManagementView | ✅ |
| H-10 | 批量检查引用 | `POST /herbs/batch-check-reference` | HerbManagementView | ✅ |
| H-11 | 批量导入（JSON） | `POST /herbs/batch-import` | — | ✅ API 层 |
| H-12 | 批量启用 | `POST /herbs/batch-enable` | HerbManagementView | ✅ |
| H-13 | 批量禁用 | `POST /herbs/batch-disable` | HerbManagementView | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| H-M1 | Excel 导入模板下载 | API 无 `GET /herbs/import-template` | 中（T4 已规划） |
| H-M2 | Excel 导出 | API 无 `GET /herbs/export` | 中（T4 已规划） |
| H-M3 | 药材分类管理 | Herb 有 Category 字段但无独立分类管理 UI | 低 |
| H-M4 | 药材价格管理 | 无批量价格更新功能 | 低 |

---

## 五、验方管理 (Formulas)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| F-01 | 验方列表（分页） | `GET /formulas` | FormulaManagementView | ✅ |
| F-02 | 验方详情 | `GET /formulas/{id}` | FormulaManagementView | ✅ |
| F-03 | 创建验方 | `POST /formulas` | FormulaManagementView | ✅ |
| F-04 | 更新验方 | `PUT /formulas/{id}` | FormulaManagementView | ✅ |
| F-05 | 删除验方（软删除） | `DELETE /formulas/{id}` | FormulaManagementView | ✅ |
| F-06 | 切换启用/禁用 | `POST /formulas/{id}/toggle-status` | FormulaManagementView | ✅ |
| F-07 | 恢复已删除验方 | `POST /formulas/{id}/restore` | FormulaManagementView | ✅ |
| F-08 | 批量删除 | `POST /formulas/batch-delete` | FormulaManagementView | ✅ |
| F-09 | 批量导入（JSON） | `POST /formulas/batch-import` | — | ✅ API 层 |
| F-10 | 批量启用 | `POST /formulas/batch-enable` | FormulaManagementView | ✅ |
| F-11 | 批量禁用 | `POST /formulas/batch-disable` | FormulaManagementView | ✅ |
| F-12 | 待校验验方列表 | `GET /formulas/pending-validation` | — | ✅ API 层 |
| F-13 | 校验验方药材匹配 | `POST /formulas/{id}/herbs/{itemId}/validate` | — | ✅ API 层 |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| FM-M1 | Excel 导入模板下载 | API 无 `GET /formulas/import-template` | 中（T4 已规划） |
| FM-M2 | Excel 导出 | API 无 `GET /formulas/export` | 中（T4 已规划） |
| FM-M3 | 验方校验 UI | API 有待校验列表+校验端点，Desktop 无对应 UI | 低 |
| FM-M4 | 验方共享控制 | `IsShared` 字段存在但 UI 无共享设置入口 | 低 |

---

## 六、医案管理 (MedicalCase) — 核心业务模块

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| M-01 | 医案列表（分页） | `GET /medicalcases` | MedicalCaseManagementView | ✅ |
| M-02 | 医案详情 | `GET /medicalcases/{id}` | MedicalCaseMasterDetailView | ✅ |
| M-03 | 创建医案 | `POST /medicalcases` | MedicalCaseWorkspaceView | ✅ |
| M-04 | 更新医案（保存） | `PUT /medicalcases/{id}` | MedicalCaseWorkspaceView | ✅ |
| M-05 | 删除医案 | `DELETE /medicalcases/{id}` | MedicalCaseManagementView | ✅ |
| M-06 | 批量删除 | `POST /medicalcases/batch-delete` | MedicalCaseManagementView | ✅ |
| M-07 | 完成医案 | `PUT /medicalcases/{id}/complete` | MedicalCaseWorkspaceView | ✅ |
| M-08 | 挂起医案 | `PUT /medicalcases/{id}/suspend` | MedicalCaseWorkspaceView | ✅ |
| M-09 | 取消医案 | `PUT /medicalcases/{id}/cancel` | MedicalCaseWorkspaceView | ✅ |
| M-10 | 更新状态 | `PUT /medicalcases/{id}/status` | MedicalCaseWorkspaceView | ✅ |
| M-11 | 标记处方需求 | `PUT /medicalcases/{id}/prescription-flag` | MedicalCaseWorkspaceView | ✅ |
| M-12 | 记录打印完成 | `PUT /medicalcases/{id}/print-completed` | 处方打印流程 | ✅ |
| M-13 | 辨证记录列表 | `GET /medicalcases/{id}/consultations` | MedicalCaseWorkspaceView | ✅ |
| M-14 | 处方列表 | `GET /medicalcases/{id}/prescriptions` | MedicalCaseWorkspaceView | ✅ |
| M-15 | 患者辨证历史 | `GET /medicalcases/patient/{id}/consultations` | MedicalCaseWorkspaceView | ✅ |
| M-16 | 患者处方历史 | `GET /medicalcases/patient/{id}/prescriptions` | MedicalCaseWorkspaceView | ✅ |
| M-17 | 批量查询详情 | `POST /medicalcases/batch-details` | — | ✅ API 层 |
| M-18 | 跨医案搜索 | `GET /medicalcases/search` | MedicalCaseManagementView | ✅ |
| M-19 | 统一查询端点 | `GET /medicalcases/query` | MedicalCaseWorkspaceView | ✅ |
| M-20 | 操作权限查询 | `GET /medicalcases/{id}/permissions` | MedicalCaseWorkspaceView | ✅ |
| M-21 | 审计日志 | `GET /medicalcases/{id}/audit-logs` | AuditLogView | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| MC-M1 | 处方打印 | Desktop 有 `PrescriptionPrintService`，Server 无打印端点 | 低（Desktop 本地打印） |
| MC-M2 | 处方导出 PDF | Desktop 有 QuestPDF 导出 | 低（Desktop 本地导出） |
| MC-M3 | 医案导出/打印统计 | Server 无导出端点 | 低 |
| MC-M4 | 医案状态流转可视化 | 无状态流转图/时间线 UI | 低 |

---

## 七、挂号管理 (Registration)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| R-01 | 挂号列表（分页+筛选） | `GET /registrations` | RegistrationListView | ✅ |
| R-02 | 挂号详情 | `GET /registrations/{id}` | — | ✅ API 层 |
| R-03 | 创建挂号 | `POST /registrations` | RegistrationListView | ✅ |
| R-04 | 接诊（开始看诊） | `PUT /registrations/{id}/start-visit` | PendingQueueView | ✅ |
| R-05 | 取消挂号 | `PUT /registrations/{id}/cancel` | RegistrationListView | ✅ |
| R-06 | 等待队列 | `GET /registrations/queue` | PendingQueueView | ✅ |
| R-07 | 医生快速看诊 | `POST /registrations/quick-visit` | ReceptionistHomeView | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| RG-M1 | 挂号完诊 | 无显式「完诊」端点（通过医案完成隐式处理） | 低（当前设计合理） |
| RG-M2 | 挂号费管理 | 无挂号费用字段/支付状态 | 低（中医诊所场景可能不需要） |
| RG-M3 | 排班管理 | 无医生排班/号源管理 | 中（如需预约挂号则必需） |
| RG-M4 | 挂号统计 | 无挂号量/排队时长统计 | 低 |

---

## 八、统计报表 (Reports)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| RP-01 | 日收入统计 | `GET /reports/daily/income` | ReportsHomeView | ✅ |
| RP-02 | 日问诊统计 | `GET /reports/daily/consultations` | ReportsHomeView | ✅ |
| RP-03 | 日药材使用统计 | `GET /reports/daily/herbs` | ReportsHomeView | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| RP-M1 | 时间范围选择 | API 支持 startDate/endDate，UI 是否支持待确认 | 中 |
| RP-M2 | 图表可视化 | ReportsHomeView 是否有图表组件待确认 | 中 |
| RP-M3 | 患者统计 | 无新增患者数、复诊率等统计 | 中 |
| RP-M4 | 医生工作量统计 | 无按医生维度的问诊/处方统计 | 中 |
| RP-M5 | 报表导出 | 无 Excel/PDF 导出 | 低 |
| RP-M6 | 自定义报表 | 无报表筛选/组合功能 | 低 |

---

## 九、系统配置 (Configuration)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| C-01 | 获取全部配置 | `GET /configuration` | SystemSettingsView | ✅ |
| C-02 | 获取单个配置 | `GET /configuration/{key}` | SystemSettingsView | ✅ |
| C-03 | 生产环境验证 | `POST /configuration/validate` | SystemSettingsView | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| CF-M1 | 修改配置 | API 无 `PUT/POST` 端点，配置只能手动编辑 appsettings | 高（T7 已规划） |
| CF-M2 | 配置分组展示 | 当前 `Dictionary<string,string>` 无分组/分类 | 中 |
| CF-M3 | 配置变更审计 | 无配置修改日志 | 低 |
| CF-M4 | 敏感配置加密显示 | 密码/密钥等应脱敏 | 中 |

---

## 十、诊断调试 (Diagnostics)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| D-01 | 获取日志级别 | `GET /diagnostics/logging/status` | LogLevelControlView | ✅ |
| D-02 | 启用调试模式 | `POST /diagnostics/logging/debug/enable` | LogLevelControlView | ✅ |
| D-03 | 禁用调试模式 | `POST /diagnostics/logging/debug/disable` | LogLevelControlView | ✅ |
| D-04 | 设置日志级别 | `POST /diagnostics/logging/level` | LogLevelControlView | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| DG-M1 | 实时日志流 | 无 SignalR/WebSocket 推送日志 | 低（依赖 F2 SignalR） |
| DG-M2 | 数据库健康检查 | 无 DB 连接/延迟检测端点 | 低 |

---

## 十一、部署管理 (Deploy)

### 已实现功能

| ID | 功能 | API 端点 | Desktop UI | 状态 |
|----|------|---------|-----------|------|
| DP-01 | 上传更新包 | `POST /deploy/upload` | DeploymentView | ✅ |
| DP-02 | 重启服务 | `POST /deploy/restart` | DeploymentView | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| DP-M1 | 版本检查 | 无 `GET /deploy/version` 端点 | 中（T7 自动更新依赖） |
| DP-M2 | 更新历史 | 无更新记录查询 | 低 |
| DP-M3 | 回滚机制 | 无版本回滚功能 | 低 |

---

## 十二、Shell 基础设施

### 已实现功能

| ID | 功能 | Desktop UI | 状态 |
|----|------|-----------|------|
| S-01 | 登录状态管理 | Shell 状态栏 | ✅ |
| S-02 | 角色导航路由 | 根据角色显示不同菜单 | ✅ |
| S-03 | 账户设置 | AccountSettingsView | ✅ |
| S-04 | 服务器模式切换 | ServerConfigView（Remote/Local） | ✅ |
| S-05 | 多窗口支持 | Shell 支持多文档 | ✅ |

### 缺失/待完善

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| SF-M1 | 自动更新（Velopack） | T7 已规划，当前无 | 高（T7） |
| SF-M2 | 初始化向导 | T7 已规划，FirstRunSetupView 为基础版 | 中（T7） |
| SF-M3 | 数据备份/恢复 | T7 已规划，当前无 | 中（T7） |
| SF-M4 | 配置中心 UI | T7 已规划，当前 SystemSettingsView 为基础版 | 中（T7） |
| SF-M5 | 登出竞态修复 | Shell 有 3 个已知 Bug（审计报告 §5.3 #5） | 高（安全） |
| SF-M6 | 并发登录竞态 | 同上 | 高（安全） |
| SF-M7 | 异常时事件未发布 | 同上 | 高（安全） |
| SF-M8 | Sync-over-Async 风险 | `.GetAwaiter().GetResult()` 在 WPF UI 线程（审计报告 §5.3 #6） | 高（死锁风险） |

---

## 十三、跨模块/全局功能

### 已实现

| ID | 功能 | 说明 |
|----|------|------|
| G-01 | 双模式切换 | SwitchingApiClient 自动切换 Remote/Local |
| G-02 | OutputCache | Patients/Herbs/Formulas 列表缓存 |
| G-03 | Rate Limiting | 登录/写操作限流 |
| G-04 | API 版本控制 | v1 路由 |
| G-05 | 结构化日志 | ILogger + 模块前缀标签 |
| G-06 | 架构测试守卫 | P07/P08/P10 约束 |

### 缺失

| ID | 功能 | 说明 | 优先级建议 |
|----|------|------|-----------|
| GM-M1 | Excel 导出/导入 | T4 已规划，Herbs/Formulas/Patients 均需 | 中（T4） |
| GM-M2 | SignalR 实时通知 | T6 已规划 | 中（T6） |
| GM-M3 | 离线同步 v2.0 | T9 已规划，当前分支放弃 | 低（v2.0） |
| GM-M4 | Swagger/OpenAPI | 无 API 文档端点 | 低 |
| GM-M5 | 健康检查 | HealthController 存在但功能待确认 | 低 |

---

## 附：已知安全问题（审计报告 §5.3）

| ID | 问题 | 严重度 |
|----|------|--------|
| SEC-01 | 明文密码泄露（SSH/SA/JWT SecretKey） | 🔴 P0 |
| SEC-02 | Shell 登出状态机错误 | 🔴 P0 |
| SEC-03 | 并发登录竞态 | 🔴 P0 |
| SEC-04 | 异常时事件未发布 | 🟡 P1 |
| SEC-05 | Sync-over-Async 死锁风险 | 🟡 P1 |

---

## 附：功能优先级汇总（待确认）

### 高优先级（必须先做）

1. **SEC-01~05** — 安全问题修复
2. **CF-M1** — 配置修改能力（当前只能手动改文件）
3. **SF-M5~M7** — Shell 3 个已知 Bug
4. **SF-M8** — Sync-over-Async 死锁修复

### 中优先级（功能完善）

5. **T4（H-M1/M2, FM-M1/M2, GM-M1）** — Excel 导出/导入
6. **T7（SF-M1~M4）** — 自动更新/初始化向导/备份/配置中心
7. **RP-M1~M4** — 报表功能增强
8. **T6（GM-M2）** — SignalR 实时通知

### 低优先级（锦上添花）

9. **A-M1~M3, U-M1~M3, P-M1~M3** — 各模块小功能补充
10. **GM-M3~M5** — 离线同步/Swagger/健康检查
11. **DP-M1~M3** — 部署功能增强
