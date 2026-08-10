# LYBTZYZS 当前状态

> 由 [13-project-master-plan.md §四/§五](13-project-master-plan.md) 拆出（2026-08-04 规则体系优化 E-03），内容原样迁移：Desktop 视图实现现状 + 已知问题清单。**本文件是 Build/测试/已知问题的唯一权威。**

## 三、当前状态速览

| 项 | 值 |
|----|-----|
| Build | 0 错误 **0 警告** |
| 架构测试 | 88/88 pass（P07/P08/P10 约束不可违反） |
| Desktop 测试 | 240 pass **104 fail**（测试主机进程崩溃） |
| 最新迁移 | `RecreateDroppedAuditTables` |

## 四、Desktop 视图（代码实际定义）

### 4.1 Shell

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| LoginView | Modules/LYBT.Desktop.Auth/Views/ | 登录界面 | — |
| FirstRunSetupView | Modules/LYBT.Desktop.Auth/Views/ | 首次运行向导 | 功能有限 |
| ServerConfigView | Modules/LYBT.Desktop.Auth/Views/ | 服务器地址配置 | — |
| AccountSettingsView | Shell/Views/ | 账户设置（个人资料/密码） | — |

### 4.2 管理员角色

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| AdminHomeView | Roles/LYBT.Desktop.Admin/Views/ | 管理员首页 | — |
| UserManagementView | Roles/LYBT.Desktop.Admin/Views/ | 用户管理 | — |
| SystemSettingsView | Roles/LYBT.Desktop.Admin/Views/ | 系统设置 | 仅读取，无编辑 |
| SysadminHomeView | Roles/LYBT.Desktop.Admin/Sysadmin/Views/ | 运维首页 | — |
| LogLevelControlView | Roles/LYBT.Desktop.Admin/Sysadmin/Views/ | 日志级别控制 | — |
| DeploymentView | Roles/LYBT.Desktop.Admin/Sysadmin/Views/ | 部署视图 | 仅上传+重启 |

### 4.3 临床角色

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| ClinicalHomeView | Roles/LYBT.Desktop.Clinical/Views/ | 临床首页 | TODO: 今日统计 |
| ClinicalWorkspaceView | Roles/LYBT.Desktop.Clinical/Views/ | 临床工作台 | — |
| PatientManagementView | Roles/LYBT.Desktop.Clinical/Views/ | 患者管理 | — |
| PatientSelectionView | Roles/LYBT.Desktop.Clinical/Views/ | 患者选择（身份证读卡） | — |
| HerbManagementView | Roles/LYBT.Desktop.Clinical/Views/ | 药材管理 | — |
| FormulaManagementView | Roles/LYBT.Desktop.Clinical/Views/ | 验方管理 | — |
| MedicalCaseManagementView | Roles/LYBT.Desktop.Clinical/Views/ | 医案管理 | — |
| MedicalCaseWorkspaceView | Roles/LYBT.Desktop.Clinical/Views/ | 医案工作台（核心） | 超大类型 558 行 |
| PendingQueueView | Roles/LYBT.Desktop.Clinical/Views/ | 待诊队列 | — |
| ReceptionistHomeView | Roles/LYBT.Desktop.Clinical/Receptionist/Views/ | 前台首页 | — |

### 4.4 医疗模块

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| MedicalCaseMasterDetailView | Modules/LYBT.Desktop.MedicalCase/Views/ | 医案主从详情 | — |
| AuditLogView | Modules/LYBT.Desktop.MedicalCase/Views/ | 审计日志 | — |
| ReportsHomeView | Modules/LYBT.Desktop.MedicalCase/Reports/Views/ | 报表首页 | TODO: 待完善 |

### 4.5 其他

| 视图 | 文件 | 功能 | 问题 |
|------|------|------|------|
| RegistrationListView | Modules/LYBT.Desktop.Registration/Views/ | 挂号列表 | — |

---

## 五、已知问题（代码实际状态）

### 🔴 P0 — 必须修复

| ID | 问题 | 位置 | 影响 |
|----|------|------|------|
| P0-01 | Shell 登出状态机错误 | Shell/LoginCoordinator | ✅ 已修复 (2026-08-04, B-01) |
| P0-02 | 并发登录竞态 | Shell/LoginCoordinator | ✅ 已修复 (2026-08-04, B-01) |
| P0-03 | 异常时事件未发布 | Shell/ShellEventCoordinator | ✅ 已修复 (2026-08-04, B-01) |
| P0-04 | Sync-over-Async 死锁 | Desktop 多处 `.GetAwaiter().GetResult()` | ✅ 已核实无残留 (2026-08-04, B-01) |
| P0-05 | 明文密码泄露 | appsettings.json (SSH/SA/JWT SecretKey) | ✅ 已修复 (2026-08-04, B-01) |
| P0-06 | 104 个 Desktop 测试失败 | tests/LYBT.Tests.Desktop | 测试主机崩溃，质量保障失效 |
| P0-07 | 配置无法 API 修改 | ConfigurationController 无 PUT | 运维只能手动改文件 |

### 🟡 P1 — 应修复

| ID | 问题 | 位置 | 影响 |
|----|------|------|------|
| P1-01 | 9 个 Build 警告 | 多处 (CA1001/CS8603/CS0168/CS4014) | ✅ 已修复 (2026-08-10, B1/B2/B3 后 build 0 警告门禁) |
| P1-02 | 8 个 TODO 残留 | MedicalCase/Shell/Reports | 技术债务 |
| P1-03 | 5 个超大类型 (>600行) | MedicalCaseCommandService/Repository/HttpClientApiClient/NavigableViewModelBase/PrescriptionPrintService | ✅ 已解决 (2026-08-10: B2 重写后 5 个文件均 <600 行) |
| P1-04 | 22 个 MediatR trivial Handler | MedicalCase Application/ | 过度设计 |
| P1-05 | 实体双模型 | Domain/ vs Shared/ (6 对双胞胎) | 维护成本翻倍 |
| P1-06 | Excel 导入/导出缺失 | Herbs/Formula/Patients | 无法批量操作 |
| P1-07 | 报表功能严重不足 | ReportsController (仅 3 个日统计) | 数据分析能力弱 |
| P1-08 | 6 个 NotSupportedException 桩 | Desktop Foundation Http/Clients | Desktop 功能不完整 |
| P1-09 |处方价格刷新未实现 | MedicalCasePrescriptionService TODO | 价格不自动更新 |

### 🔵 P2 — 可后续完善

| ID | 问题 | 位置 | 影响 |
|----|------|------|------|
| P2-01 | 自动更新 | Shell (Velopack) | 运维依赖 |
| P2-02 | 数据备份/恢复 | — | 运维依赖 |
| P2-03 | SignalR 实时通知 | — | 体验增强 |
| P2-04 | 离线同步 v2.0 | 旧分支已放弃 | 大功能 |
| P2-05 | Swagger/OpenAPI | — | 开发体验 |
| P2-06 | 排班管理 | Registration | 业务增强 |
