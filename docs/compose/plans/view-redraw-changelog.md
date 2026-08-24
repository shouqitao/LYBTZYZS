# View 视觉重绘变更台账 v1（连续执行）

> 按 docs/compose/plans/.hermes-task-view-redraw-v1.md（P0→P1→P2）逐页重画，保留 VM 绑定，遵循居中/铺满/输入描边规则

## P0 核心链路

| View | 笔稿 | 变更摘要 | Commit |
|------|------|----------|--------|
| LoginView | login.pen | 右侧暖底420表单48h8圆角带图标，左侧780渐变品牌托盘116含叶图标与琥珀分隔线，保持VM绑定校验 | 90e8884 feat(auth) |
| PatientList (PatientMasterDetailControl) | patient-list.pen | MasterDetail bg-warm, 工具栏56h白12r, 搜索40h Margin16, DataGrid铺满Header居中 | 58e4c86 |
| ClinicalWorkspaceView | clinical-workspace.pen | bg-warm, 主内容gap16, 工具栏48, 内容区260/fill三栏, 文字居中 | f058103 |
| MedicalCaseManagement (MedicalCaseMasterDetailControl) | medical-case-management.pen | 同患者管理：56h工具栏, 40h搜索铺满, Header居中 | c77793c |
| MedicalCaseWorkspaceView | medical-case.pen | bg-warm铺满, 标题含患者信息, 底部按钮右置, 输入描边可见 | 07e3ad1 |
| RegistrationListView | registration.pen | bg-warm gap16, 工具栏56h白12r, 卡片12r铺满, Header居中 | 06af61c |

## P1 管理/设置

| View | 笔稿 | Commit |
|------|------|--------|
| AdminHome / SysadminHome / ReceptionistHome / ClinicalHome (P-D导航网格) | admin-home/sysadmin-home/receptionist-home.pen | d6c9649 + c5ee0c6 |
| UserManagement (UserMasterDetailControl) | user-management.pen | 2a5f456 toolbar56+search40+铺满 |
| AccountSettings / SystemSettings / ServerConfig (P-B) | account-settings/system-settings/server-config.pen | ac5dbd0 bg-warm居中 |
| PatientSelection / Herb/Formula Catalog | patient-selection/herb-management/formula-management.pen | c5ee0c6 + 91746e8 |

## P2 运维/审计/报表

| View | 笔稿 | Commit |
|------|------|--------|
| BackupManagement / Deployment / LogLevel | backup-management/deployment/log-level.pen | c5ee0c6 bg-warm铺满 |
| AuditLog / SecurityAuditLog | audit-log.pen | c5ee0c6 |
| ReportsHome | reports.pen | c5ee0c6 |
| Herb/Formula 更多、AccountSettingsControl | herb/formula/account-settings | 91746e8 |

## 验收

- `dotnet build LYBTZYZS.sln --no-incremental` → 0 错误 8 警告（存量 CS0618，与本次无关）
- 每页：文字默认居中（DataGridColumnHeader HorizontalContentAlignment Center）、输入框描边可见（40-48h, Border #E9DFD7/#C9C1B9）、主内容区 fill_container 铺满不留大空白、间隙按 framework gap16/padding16
- VM 绑定保持（未改 ViewModel，仅 XAML 布局/样式）

## 后续

- P2 中 CardReaderDiagnostics / Deployment 步骤式交互未扩展（仅视觉基线）
- DataImportExport 复用各 MasterDetail 的导入导出按钮，无独立 View
