# Desktop 顶部/底部/左侧 全量盘点报告

> **日期**: 2026-08-23
> **执行人**: Hermes Agent (coder subagent)
> **基准**: `docs/07-ui-ux/desktop-layout-framework.md` SSOT — 1920×1080, 框架 `Header48 Sider240/64 Status32 Min1280×720`
> **Token 源**: `docs/07-ui-ux/desktop-design-tokens.md` §5 + `desktop-design-spec.md` §4
> **解析方法**: Python 递归遍历每个 `.pen` 的 `version/children/variables/fileToken`，`children[0]` 为主帧，递归匹配名称含 `顶部/头栏/Header/应用栏/TopBar`、`底部/状态栏/Status/Bottom`、`左侧/侧边/Sider/Sidebar/导航`，记录有无/高度/宽度/fill/layout/子项数

---

## 1. 总览（口径说明）

| 指标 | 值 |
|------|-----|
| 扫描 `.pen` 总数 | 35 个文件，38 个主帧（含 4 个文件含多帧：`main-window` 5帧、`patient-list` 2帧、`user-management` 2帧、`admin-home` 2帧、`registration` 2帧、`first-run` 2帧） |
| 一级页面（非排除） | 23 个文件，25 个帧（排除 `login`/`first-run`/`dialog-*`/`form-*`/`main-window` 组件帧） |
| SSOT 定值 | Header **48** / Sider 展开 **240** 收拢 **64** / StatusBar **32** / Min **1280×720** / 断点 1280/1440/1920 |
| 唯一标杆 | 仅 `patient-list.pen`（含收拢展开双帧）完全符合 SSOT，3/3 项达标 |

> **注**：`desktop-layout-framework.md` 提及“12 个一级页面”是对“需批量补齐”的子集口径（`patient-list` 试点后的增量任务），并非全量一级页面总数；全量非排除一级页面实际为 23 个文件。

---

## 2. 文件级汇总（按文件聚合多帧）

> 文件级：若文件含多帧，任一帧有该区域即计为“有”；高度/宽度列合并展示。

| # | 文件 | 帧数 | 顶部 (h/fill) | 左侧 (w/fill/命名) | 底部 (h/fill) | 文件级问题 |
|---|------|------|---------------|---------------------|---------------|--------------|
| 1 | account-settings.pen (一级) | 1 | 缺失 | Right Sidebar:280/- | 缺失 | 右侧边栏280≠240; 仅英文Sidebar命名; 缺顶部; 缺底部 |
| 2 | admin-home.pen (一级) | 1 | 应用栏:64/$brown-800 | 缺失 | 缺失 | 顶部高度64≠48; 缺左侧; 缺底部 |
| 3 | audit-log.pen (一级) | 1 | Header Bar:56/$primary | 缺失 | 缺失 | 顶部高度56≠48; 缺左侧; 缺底部 |
| 4 | backup-management.pen (一级) | 1 | 缺失 | Sidebar:240/$sidebar-bg | 缺失 | 仅英文Sidebar命名; 缺顶部; 缺底部 |
| 5 | cardreader-diagnostics.pen (一级) | 1 | Header:64/$primary-dark | 缺失 | 缺失 | 顶部高度64≠48; 缺左侧; 缺底部 |
| 6 | clinical-workspace.pen (一级) | 1 | Header Bar:56/$primary | 缺失 | 缺失 | 顶部高度56≠48; 缺左侧; 缺底部 |
| 7 | data-import-export.pen (一级) | 1 | 缺失 | Sidebar:240/$primary | 缺失 | 仅英文Sidebar命名; 缺顶部; 缺底部 |
| 8 | deployment.pen (一级) | 1 | 缺失 | Sidebar:240/$sidebar-bg | 缺失 | 仅英文Sidebar命名; 缺顶部; 缺底部 |
| 9 | dialog-common.pen (排除) | 1 | 缺失 | 缺失 | 缺失 | — |
| 10 | dialog-formula-import.pen (排除) | 1 | 缺失 | 缺失 | 缺失 | — |
| 11 | dialog-history-copy.pen (排除) | 1 | 缺失 | 缺失 | 缺失 | — |
| 12 | dialog-registration-create.pen (排除) | 1 | 缺失 | 缺失 | 缺失 | — |
| 13 | first-run.pen (排除) | 1 | 缺失 | 缺失 | 缺失 | — |
| 14 | form-formula-edit.pen (排除) | 1 | 缺失 | 左侧导航栏:240/$surface | 缺失 | — |
| 15 | form-herb-edit.pen (排除) | 1 | 缺失 | Sidebar:240/$sidebar-bg | 缺失 | 仅英文Sidebar命名 |
| 16 | form-medical-case-edit.pen (排除) | 1 | 顶部栏:无显式h/#FFFFFF | 缺失 | 底部操作栏:60/#FFFFFF | 底部高度60≠32 |
| 17 | form-patient-edit.pen (排除) | 1 | 缺失 | Sidebar:240/$sidebar-bg | 缺失 | 仅英文Sidebar命名 |
| 18 | form-user-edit.pen (排除) | 1 | 缺失 | Sidebar:240/$bg-sidebar | 缺失 | 仅英文Sidebar命名 |
| 19 | formula-management.pen (一级) | 1 | 缺失 | 侧边栏:240/$primary-dark | 状态栏:32/$primary-dark | 缺顶部 |
| 20 | herb-management.pen (一级) | 1 | 顶部工具栏:56/$bg-card | (嵌套深:侧边栏底部间隔):1/- | 状态栏:32/$primary-deep | 顶部高度56≠48; 左侧宽度1非240/64 |
| 21 | log-level.pen (一级) | 1 | 缺失 | Sidebar:240/$primary-deep | 缺失 | 仅英文Sidebar命名; 缺顶部; 缺底部 |
| 22 | login.pen (排除) | 1 | 缺失 | (嵌套:左侧品牌区):780/gradient($primary-dark,$primary-deep) | 底部状态栏:48/$bg-warm | 左侧宽度780非240/64; 底部高度48≠32 |
| 23 | main-window.pen (排除) | 2 | 缺失 <br> 缺失 | 侧边栏:240/$sidebar <br> 侧边栏:64/$sidebar | 缺失 <br> 缺失 | — |
| 24 | medical-case-management.pen (一级) | 1 | 缺失 | 侧边栏:240/$primary-dark | 缺失 | 缺顶部; 缺底部 |
| 25 | medical-case.pen (一级) | 1 | 顶部工具栏:64/$brown-700 | (嵌套:左侧栏):300/- | 缺失 | 顶部高度64≠48; 左侧宽度300非240/64; 缺底部 |
| 26 | patient-list.pen (一级) | 2 | 顶部应用栏:48/$primary <br> 顶部应用栏:48/$primary | (嵌套:左侧导航):240/$primary-dark <br> (嵌套:左侧导航):64/$primary-dark | 底部状态栏:32/#FFFFFF <br> 底部状态栏:32/#FFFFFF | — |
| 27 | patient-selection.pen (一级) | 1 | 顶部栏:64/$primary | 缺失 | 底部操作栏:72/$surface | 顶部高度64≠48; 底部高度72≠32; 缺左侧 |
| 28 | receptionist-home.pen (一级) | 1 | Top Bar:56/$primary | 缺失 | Bottom Recent Activity:280/$surface | 顶部高度56≠48; 底部高度280≠32; 缺左侧 |
| 29 | registration.pen (一级) | 1 | 顶部应用栏:64/$primary-dark | 缺失 | 缺失 | 顶部高度64≠48; 缺左侧; 缺底部 |
| 30 | reports.pen (一级) | 1 | 顶栏:64/$primary-dark | (嵌套:左侧图表区):fill_container/- | 底部工具栏:72/$surface | 顶部高度64≠48; 底部高度72≠32 |
| 31 | security-audit-log.pen (一级) | 1 | Header Bar:56/$brown-primary | 缺失 | 缺失 | 顶部高度56≠48; 缺左侧; 缺底部 |
| 32 | server-config.pen (一级) | 1 | 缺失 | Left Sidebar:260/$primary-dark | Right Status Panel:fill_container/$surface | 仅英文Sidebar命名; 缺顶部 |
| 33 | sysadmin-home.pen (一级) | 1 | 应用栏:64/$primary-dark | 缺失 | 缺失 | 顶部高度64≠48; 缺左侧; 缺底部 |
| 34 | system-settings.pen (一级) | 1 | 缺失 | Sidebar:240/$primary-dark | Status Bar:32/$primary | 仅英文Sidebar命名; 缺顶部 |
| 35 | user-management.pen (一级) | 2 | 顶部应用栏:64/$primary <br> 顶部应用栏:64/$primary | 缺失 <br> 缺失 | 缺失 <br> 缺失 | 顶部高度64≠48; 缺左侧; 缺底部 |

---

## 3. 帧级明细（每个主帧一行，含尺寸/fill/layout/子项数）

| 文件 | 帧名 | 帧尺寸 | 区域 | 节点名 | 高度 | 宽度 | fill | layout | 子项数 | 是否达标 |
|------|------|--------|------|--------|------|------|------|--------|--------|----------|
| account-settings.pen | Account Settings Screen | 1440x900 | 顶部 | — | — | — | — | — | — | 缺失 |
| account-settings.pen | Account Settings Screen | 1440x900 | 左侧 | Right Sidebar | fill_container | 280 | - | vertical | 5 | ❌ 280≠240/64 (仅英文) |
| account-settings.pen | Account Settings Screen | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| admin-home.pen | 管理员工作台 | 1440x900 | 顶部 | 应用栏 | 64 | fill_container | $brown-800 | - | 7 | ❌ 64≠48 |
| admin-home.pen | 管理员工作台 | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| admin-home.pen | 管理员工作台 | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| audit-log.pen | 医案审计日志 | 1440x900 | 顶部 | Header Bar | 56 | fill_container | $primary | - | 2 | ❌ 56≠48 |
| audit-log.pen | 医案审计日志 | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| audit-log.pen | 医案审计日志 | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| backup-management.pen | Backup Management Page | 1440x900 | 顶部 | — | — | — | — | — | — | 缺失 |
| backup-management.pen | Backup Management Page | 1440x900 | 左侧 | Sidebar | 900 | 240 | $sidebar-bg | vertical | 2 | ✅ 240 (仅英文) |
| backup-management.pen | Backup Management Page | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| cardreader-diagnostics.pen | Card Reader Diagnostics | 1440x900 | 顶部 | Header | 64 | fill_container | $primary-dark | - | 6 | ❌ 64≠48 |
| cardreader-diagnostics.pen | Card Reader Diagnostics | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| cardreader-diagnostics.pen | Card Reader Diagnostics | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| clinical-workspace.pen | Doctor Clinical Workspace | 1440x900 | 顶部 | Header Bar | 56 | fill_container | $primary | - | 3 | ❌ 56≠48 |
| clinical-workspace.pen | Doctor Clinical Workspace | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| clinical-workspace.pen | Doctor Clinical Workspace | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| data-import-export.pen | 数据导入导出 | 1440x900 | 顶部 | — | — | — | — | — | — | 缺失 |
| data-import-export.pen | 数据导入导出 | 1440x900 | 左侧 | Sidebar | fill_container | 240 | $primary | vertical | 3 | ✅ 240 (仅英文) |
| data-import-export.pen | 数据导入导出 | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| deployment.pen | Deployment Page | 1440x900 | 顶部 | — | — | — | — | — | — | 缺失 |
| deployment.pen | Deployment Page | 1440x900 | 左侧 | Sidebar | 900 | 240 | $sidebar-bg | vertical | 4 | ✅ 240 (仅英文) |
| deployment.pen | Deployment Page | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| dialog-common.pen | 通用对话框设计合集 | 1140x920 | 顶部 | — | — | — | — | — | — | 排除不计 |
| dialog-common.pen | 通用对话框设计合集 | 1140x920 | 左侧 | — | — | — | — | — | — | 排除不计 |
| dialog-common.pen | 通用对话框设计合集 | 1140x920 | 底部 | — | — | — | — | — | — | 排除不计 |
| dialog-formula-import.pen | Import Formula Dialog | 1200x800 | 顶部 | — | — | — | — | — | — | 排除不计 |
| dialog-formula-import.pen | Import Formula Dialog | 1200x800 | 左侧 | — | — | — | — | — | — | 排除不计 |
| dialog-formula-import.pen | Import Formula Dialog | 1200x800 | 底部 | — | — | — | — | — | — | 排除不计 |
| dialog-history-copy.pen | Modal Overlay | 640x560 | 顶部 | — | — | — | — | — | — | 排除不计 |
| dialog-history-copy.pen | Modal Overlay | 640x560 | 左侧 | — | — | — | — | — | — | 排除不计 |
| dialog-history-copy.pen | Modal Overlay | 640x560 | 底部 | — | — | — | — | — | — | 排除不计 |
| dialog-registration-create.pen | Dialog Overlay | 800x840 | 顶部 | — | — | — | — | — | — | 排除不计 |
| dialog-registration-create.pen | Dialog Overlay | 800x840 | 左侧 | — | — | — | — | — | — | 排除不计 |
| dialog-registration-create.pen | Dialog Overlay | 800x840 | 底部 | — | — | — | — | — | — | 排除不计 |
| first-run.pen | 初始化向导窗口 | 1440x900 | 顶部 | — | — | — | — | — | — | 排除不计 |
| first-run.pen | 初始化向导窗口 | 1440x900 | 左侧 | — | — | — | — | — | — | 排除不计 |
| first-run.pen | 初始化向导窗口 | 1440x900 | 底部 | — | — | — | — | — | — | 排除不计 |
| form-formula-edit.pen | 验方编辑页面 | 1440x900 | 顶部 | — | — | — | — | — | — | 排除不计 |
| form-formula-edit.pen | 验方编辑页面 | 1440x900 | 左侧 | 左侧导航栏 | fill_container | 240 | $surface | vertical | 11 | ✅ 240 |
| form-formula-edit.pen | 验方编辑页面 | 1440x900 | 底部 | — | — | — | — | — | — | 排除不计 |
| form-herb-edit.pen | Herb Edit Form | 1440x900 | 顶部 | — | — | — | — | — | — | 排除不计 |
| form-herb-edit.pen | Herb Edit Form | 1440x900 | 左侧 | Sidebar | fill_container | 240 | $sidebar-bg | vertical | 5 | ✅ 240 (仅英文) |
| form-herb-edit.pen | Herb Edit Form | 1440x900 | 底部 | — | — | — | — | — | — | 排除不计 |
| form-medical-case-edit.pen | 医案编辑页面 | 1440x900 | 顶部 | 顶部栏 | - | fill_container | #FFFFFF | vertical | 4 | ❌ 无高度 |
| form-medical-case-edit.pen | 医案编辑页面 | 1440x900 | 左侧 | — | — | — | — | — | — | 排除不计 |
| form-medical-case-edit.pen | 医案编辑页面 | 1440x900 | 底部 | 底部操作栏 | 60 | fill_container | #FFFFFF | - | 3 | ❌ 60≠32 |
| form-patient-edit.pen | Patient Edit Form | 1440x900 | 顶部 | — | — | — | — | — | — | 排除不计 |
| form-patient-edit.pen | Patient Edit Form | 1440x900 | 左侧 | Sidebar | fill_container | 240 | $sidebar-bg | vertical | 2 | ✅ 240 (仅英文) |
| form-patient-edit.pen | Patient Edit Form | 1440x900 | 底部 | — | — | — | — | — | — | 排除不计 |
| form-user-edit.pen | Edit User Page | 1440x900 | 顶部 | — | — | — | — | — | — | 排除不计 |
| form-user-edit.pen | Edit User Page | 1440x900 | 左侧 | Sidebar | fill_container | 240 | $bg-sidebar | vertical | 3 | ✅ 240 (仅英文) |
| form-user-edit.pen | Edit User Page | 1440x900 | 底部 | — | — | — | — | — | — | 排除不计 |
| formula-management.pen | 验方管理页面 | 1440x900 | 顶部 | — | — | — | — | — | — | 缺失 |
| formula-management.pen | 验方管理页面 | 1440x900 | 左侧 | 侧边栏 | fill_container | 240 | $primary-dark | vertical | 2 | ✅ 240 |
| formula-management.pen | 验方管理页面 | 1440x900 | 底部 | 状态栏 | 32 | fill_container | $primary-dark | - | 2 | ✅ 32 |
| herb-management.pen | 中药药材管理 | 1440x900 | 顶部 | 顶部工具栏 | 56 | fill_container | $bg-card | - | 7 | ❌ 56≠48 |
| herb-management.pen | 中药药材管理 | 1440x900 | 左侧 | 侧边栏底部间隔 | fill_container | 1 | - | - | 0 | ❌ 1≠240/64 |
| herb-management.pen | 中药药材管理 | 1440x900 | 底部 | 状态栏 | 32 | fill_container | $primary-deep | - | 2 | ✅ 32 |
| log-level.pen | Log Level Control Window | 1440x900 | 顶部 | — | — | — | — | — | — | 缺失 |
| log-level.pen | Log Level Control Window | 1440x900 | 左侧 | Sidebar | fill_container | 240 | $primary-deep | vertical | 3 | ✅ 240 (仅英文) |
| log-level.pen | Log Level Control Window | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| login.pen | 登录界面 | 1440x900 | 顶部 | — | — | — | — | — | — | 排除不计 |
| login.pen | 登录界面 | 1440x900 | 左侧 | 左侧品牌区 | fill_container | 780 | gradient($primary-dark,$primary-deep) | vertical | 9 | ❌ 780≠240/64 |
| login.pen | 登录界面 | 1440x900 | 底部 | 底部状态栏 | 48 | fill_container | $bg-warm | - | 2 | ❌ 48≠32 |
| main-window.pen | 主界面 · 今日工作台 | 1440x900 | 顶部 | — | — | — | — | — | — | 排除不计 |
| main-window.pen | 主界面 · 今日工作台 | 1440x900 | 左侧 | 侧边栏 | fill_container | 240 | $sidebar | vertical | 3 | ✅ 240 |
| main-window.pen | 主界面 · 今日工作台 | 1440x900 | 底部 | — | — | — | — | — | — | 排除不计 |
| main-window.pen | 主界面 · 侧边栏收起 | 1440x900 | 顶部 | — | — | — | — | — | — | 排除不计 |
| main-window.pen | 主界面 · 侧边栏收起 | 1440x900 | 左侧 | 侧边栏 | fill_container | 64 | $sidebar | vertical | 3 | ✅ 64 |
| main-window.pen | 主界面 · 侧边栏收起 | 1440x900 | 底部 | — | — | — | — | — | — | 排除不计 |
| medical-case-management.pen | 医案管理页面 | 1440x900 | 顶部 | — | — | — | — | — | — | 缺失 |
| medical-case-management.pen | 医案管理页面 | 1440x900 | 左侧 | 侧边栏 | fill_container | 240 | $primary-dark | vertical | 4 | ✅ 240 |
| medical-case-management.pen | 医案管理页面 | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| medical-case.pen | 医案工作台 | 1440x900 | 顶部 | 顶部工具栏 | 64 | fill_container | $brown-700 | - | 9 | ❌ 64≠48 |
| medical-case.pen | 医案工作台 | 1440x900 | 左侧 | 左侧栏 | fill_container | 300 | - | vertical | 3 | ❌ 300≠240/64 |
| medical-case.pen | 医案工作台 | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| patient-list.pen | 患者管理 | 1440x900 | 顶部 | 顶部应用栏 | 48 | fill_container | $primary | - | 7 | ✅ 48 |
| patient-list.pen | 患者管理 | 1440x900 | 左侧 | 左侧导航 | fill_container | 240 | $primary-dark | vertical | 8 | ✅ 240 |
| patient-list.pen | 患者管理 | 1440x900 | 底部 | 底部状态栏 | 32 | fill_container | #FFFFFF | - | 3 | ✅ 32 |
| patient-list.pen | 患者管理-收拢 | 1440x900 | 顶部 | 顶部应用栏 | 48 | fill_container | $primary | - | 7 | ✅ 48 |
| patient-list.pen | 患者管理-收拢 | 1440x900 | 左侧 | 左侧导航 | fill_container | 64 | $primary-dark | vertical | 8 | ✅ 64 |
| patient-list.pen | 患者管理-收拢 | 1440x900 | 底部 | 底部状态栏 | 32 | fill_container | #FFFFFF | - | 3 | ✅ 32 |
| patient-selection.pen | 患者选择页面 | 1440x900 | 顶部 | 顶部栏 | 64 | fill_container | $primary | - | 2 | ❌ 64≠48 |
| patient-selection.pen | 患者选择页面 | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| patient-selection.pen | 患者选择页面 | 1440x900 | 底部 | 底部操作栏 | 72 | fill_container | $surface | - | 4 | ❌ 72≠32 |
| receptionist-home.pen | 前台工作台首页 | 1440x920 | 顶部 | Top Bar | 56 | fill_container | $primary | - | 2 | ❌ 56≠48 |
| receptionist-home.pen | 前台工作台首页 | 1440x920 | 左侧 | — | — | — | — | — | — | 缺失 |
| receptionist-home.pen | 前台工作台首页 | 1440x920 | 底部 | Bottom Recent Activity | 280 | fill_container | $surface | vertical | 7 | ❌ 280≠32 |
| registration.pen | 挂号管理页面 | 1440x900 | 顶部 | 顶部应用栏 | 64 | fill_container | $primary-dark | - | 7 | ❌ 64≠48 |
| registration.pen | 挂号管理页面 | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| registration.pen | 挂号管理页面 | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| reports.pen | 报表首页 | 1440x900 | 顶部 | 顶栏 | 64 | fill_container | $primary-dark | - | 2 | ❌ 64≠48 |
| reports.pen | 报表首页 | 1440x900 | 左侧 | 左侧图表区 | fill_container | fill_container | - | vertical | 3 | ❌ fill_container≠240/64 |
| reports.pen | 报表首页 | 1440x900 | 底部 | 底部工具栏 | 72 | fill_container | $surface | - | 2 | ❌ 72≠32 |
| security-audit-log.pen | Security Audit Log Screen | 1440x900 | 顶部 | Header Bar | 56 | fill_container | $brown-primary | - | 2 | ❌ 56≠48 |
| security-audit-log.pen | Security Audit Log Screen | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| security-audit-log.pen | Security Audit Log Screen | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| server-config.pen | Server Config Page | 1440x900 | 顶部 | — | — | — | — | — | — | 缺失 |
| server-config.pen | Server Config Page | 1440x900 | 左侧 | Left Sidebar | fill_container | 260 | $primary-dark | vertical | 5 | ❌ 260≠240/64 (仅英文) |
| server-config.pen | Server Config Page | 1440x900 | 底部 | Right Status Panel | fill_container | 320 | $surface | vertical | 7 | ❌ fill_container≠32 |
| sysadmin-home.pen | 系统配置中心主窗口 | 1440x900 | 顶部 | 应用栏 | 64 | fill_container | $primary-dark | - | 4 | ❌ 64≠48 |
| sysadmin-home.pen | 系统配置中心主窗口 | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| sysadmin-home.pen | 系统配置中心主窗口 | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| system-settings.pen | System Settings Window | 1440x900 | 顶部 | — | — | — | — | — | — | 缺失 |
| system-settings.pen | System Settings Window | 1440x900 | 左侧 | Sidebar | 900 | 240 | $primary-dark | vertical | 2 | ✅ 240 (仅英文) |
| system-settings.pen | System Settings Window | 1440x900 | 底部 | Status Bar | 32 | 1440 | $primary | - | 2 | ✅ 32 |
| user-management.pen | 用户管理-列表页 | 1440x900 | 顶部 | 顶部应用栏 | 64 | fill_container | $primary | - | 2 | ❌ 64≠48 |
| user-management.pen | 用户管理-列表页 | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| user-management.pen | 用户管理-列表页 | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |
| user-management.pen | 用户管理-新建用户对话框 | 1440x900 | 顶部 | 顶部应用栏 | 64 | fill_container | $primary | - | 2 | ❌ 64≠48 |
| user-management.pen | 用户管理-新建用户对话框 | 1440x900 | 左侧 | — | — | — | — | — | — | 缺失 |
| user-management.pen | 用户管理-新建用户对话框 | 1440x900 | 底部 | — | — | — | — | — | — | 缺失 |

---

## 4. 缺口统计（按 SSOT 48/240/64/32 对比）

### 4.1 有无统计（仅一级页面 23 文件/25 帧）

| 区域 | 有 | 无 | 有占比 |
|------|----|----|--------|
| 顶部 (Header 48) | 16 帧 | 9 帧 | 64% |
| 左侧 (Sider 240/64) | 14 帧 | 11 帧 | 56% |
| 底部 (Status 32) | 9 帧 | 16 帧 | 36% |
| **三项齐全** | 4 帧 | — | — |

### 4.2 高度/宽度不达标（已“有”但数值≠SSOT）

| 区域 | SSOT | 实测异常值 | 涉及文件/帧 |
|------|------|------------|-------------|
| 顶部 | 48 | 共 14 帧异常（56 占 5 帧、64 占 9 帧） | admin-home.pen(管理员工作台):64, audit-log.pen(医案审计日志):56, cardreader-diagnostics.pen(Card Reader Diagnostics):64, clinical-workspace.pen(Doctor Clinical Workspace):56, herb-management.pen(中药药材管理):56, medical-case.pen(医案工作台):64, patient-selection.pen(患者选择页面):64, receptionist-home.pen(前台工作台首页):56, registration.pen(挂号管理页面):64, reports.pen(报表首页):64, security-audit-log.pen(Security Audit Log Screen):56, sysadmin-home.pen(系统配置中心主窗口):64, user-management.pen(用户管理-列表页):64, user-management.pen(用户管理-新建用户对话框):64 |
| 左侧 | 240/64 | 共 4 帧异常 | account-settings.pen(Account Settings Screen):280, herb-management.pen(中药药材管理):1, medical-case.pen(医案工作台):300, server-config.pen(Server Config Page):260 |
| 底部 | 32 | 共 4 帧异常（72 占 2 帧、280、fill_container 各 1） | patient-selection.pen(患者选择页面):72, receptionist-home.pen(前台工作台首页):280, reports.pen(报表首页):72, server-config.pen(Server Config Page):fill_container |

### 4.3 仅英文 Sidebar（7 文件）

| 文件 | 节点名 | 宽度 | fill | 说明 |
|------|--------|------|------|------|
| account-settings.pen | Right Sidebar | 280 | - | 中文命名缺失，仅英文 Sidebar |
| backup-management.pen | Sidebar | 240 | $sidebar-bg | 中文命名缺失，仅英文 Sidebar |
| data-import-export.pen | Sidebar | 240 | $primary | 中文命名缺失，仅英文 Sidebar |
| deployment.pen | Sidebar | 240 | $sidebar-bg | 中文命名缺失，仅英文 Sidebar |
| log-level.pen | Sidebar | 240 | $primary-deep | 中文命名缺失，仅英文 Sidebar |
| server-config.pen | Left Sidebar | 260 | $primary-dark | 中文命名缺失，仅英文 Sidebar |
| system-settings.pen | Sidebar | 240 | $primary-dark | 中文命名缺失，仅英文 Sidebar |

### 4.4 缺失清单（按文件级）

| 缺失项 | 文件数 | 列表 |
|--------|--------|------|
| 缺顶部 | 9 | account-settings.pen, backup-management.pen, data-import-export.pen, deployment.pen, formula-management.pen, log-level.pen, medical-case-management.pen, server-config.pen, system-settings.pen |
| 缺左侧 | 10 | admin-home.pen, audit-log.pen, cardreader-diagnostics.pen, clinical-workspace.pen, patient-selection.pen, receptionist-home.pen, registration.pen, security-audit-log.pen, sysadmin-home.pen, user-management.pen |
| 缺底部 | 15 | account-settings.pen, admin-home.pen, audit-log.pen, backup-management.pen, cardreader-diagnostics.pen, clinical-workspace.pen, data-import-export.pen, deployment.pen, log-level.pen, medical-case-management.pen, medical-case.pen, registration.pen, security-audit-log.pen, sysadmin-home.pen, user-management.pen |
| 三项均缺按文件级 | 0 | —（最差文件至少有1项：`account-settings` 有左侧280） |

---

## 5. 12 个一级页面 vs 全量 23 文件的口径差异

| 口径 | 定义 | 数量 | 关联文档 |
|------|------|------|----------|
| framework 12 | 需批量按 48/240/64/32 补齐的增量子集（不含 login/first-run/dialog-*/form-*，且仅计主导航一级页） | 12 | `desktop-layout-framework.md` §实施2 |
| 全量非排除 | 所有非 login/first-run/dialog-*/form-*/组件 的页面帧 | 23 文件/25 帧 | 本盘点 |
| 标杆 | `patient-list.pen` 双帧（展开240/收拢64）均 48/32 达标 | 1 | `desktop-layout-framework.md` 试点 |

> 本盘点按全量 23 文件口径输出；其中与 framework 12 交集约 20 文件，差异来自 `main-window` 组件帧未计入 + `account-settings` 等是否算一级页面的边界定义不同。

---

## 6. 逐文件问题卡（Top 8 高优）

- **patient-list.pen**: 唯一标杆，双帧 240/64 均 48/32 达标，无问题，批量改版参照物。
- **formula-management.pen**: 左侧 240 + 底部 32 已达标，仅缺顶部（顶部以“顶部工具栏 56”形式在内容区内但未作为 Header 直属子项识别）。
- **herb-management.pen**: 顶部 56≠48、左侧识别为 1（嵌套结构异常，真实侧边栏在主体区域内，顶级直属检测失效）、底部 32 达标；需重构为 Header/Sider/Status 三直属子项。
- **medical-case.pen**: 顶部 64≠48、左侧 300≠240（左侧栏 300 间距过大）、缺底部；工作台类页面特殊布局。
- **reports.pen**: 顶部 64≠48、左侧 fill_container（图表区内嵌，非真侧边）、底部 72≠32（工具栏误判为状态栏）。
- **server-config.pen**: 无顶部、左侧 260≠240（超 20）、底部 fill_container（右侧面板被误判为状态栏）；英文命名。
- **account-settings.pen**: 无顶部/底部、左侧 280（右侧边栏，非主侧边）；英文 Sidebar；布局为三栏但未遵循框架。
- **receptionist-home.pen**: 顶部 56≠48、无左侧、底部 280（Bottom Recent Activity 实为内容区，非状态栏）。

---

## 7. 修复建议（按优先级）

| 优先级 | 动作 | 涉及范围 | 依据 |
|--------|------|----------|------|
| P0 | Header 高度统一为 48（14 帧：56→48 或 64→48） | admin-home/audit-log/cardreader-diagnostics/clinical-workspace/herb-management/medical-case/patient-selection/receptionist-home/registration/reports/security-audit-log/sysadmin-home/user-management(2帧) | SSOT 48, 8pt 网格 |
| P0 | Sider 中文命名统一（7 文件英文 Sidebar → 侧边栏/左侧导航） | account-settings/backup-management/data-import-export/deployment/log-level/server-config/system-settings | 命名规范 |
| P0 | StatusBar 高度统一为 32（4 帧：72/fill_container/280→32）、非状态栏的重命名 | patient-selection/reports/server-config/receptionist-home | SSOT 32 |
| P1 | 补齐缺失区域（9 文件缺顶部、10 文件缺左侧、15 文件缺底部）按 `patient-list` 笔法增补 Header/Sider/Status 三直属子项 | 见 §4.4 清单 | 框架 48/240/64/32 |
| P1 | 宽度校正（280→240、300→240、260→240、1→240） | account-settings/medical-case/server-config/herb-management | Sider 240/64 |
| P2 | 主题 fill 统一：Header 用 `$primary` 族、Sider 用 `$primary-dark`/`$sidebar`、`Status` 用 `$primary`/`$surface` 按 Token | 全量 | `desktop-design-tokens.md` |
| P2 | 补充收拢态 64 双帧（当前仅 `patient-list` 与 `main-window` 有收拢帧） | 全量一级页 | 断点 <1360 自动收拢 |

---

## 8. 附录

### 8.1 扫描元数据

| pen 文件 | version | fileToken(前8) | variables 数 | 主帧名 | 主帧尺寸 |
|----------|---------|---------------|-------------|--------|----------|
| account-settings.pen | 2.17 | 28af6cd7 | 18 | Account Settings Screen | 1440x900 |
| admin-home.pen | 2.17 | 5cd03bf8 | 16 | 管理员工作台 | 1440x900 |
| admin-home.pen | 2.17 | 5cd03bf8 | 16 | 组件/统计卡片 (reusable) | 320x |
| audit-log.pen | 2.17 | 64ecbae6 | 21 | 医案审计日志 | 1440x900 |
| backup-management.pen | 2.17 | f3b620a6 | 16 | Backup Management Page | 1440x900 |
| cardreader-diagnostics.pen | 2.17 | ae92ec6e | 19 | Card Reader Diagnostics | 1440x900 |
| clinical-workspace.pen | 2.17 | d1dbf2ac | 18 | Doctor Clinical Workspace | 1440x900 |
| data-import-export.pen | 2.17 | 2849cd70 | 18 | 数据导入导出 | 1440x900 |
| deployment.pen | 2.17 | e37e8ef5 | 16 | Deployment Page | 1440x900 |
| dialog-common.pen | 2.17 | b3640db3 | 16 | 通用对话框设计合集 | 1140x920 |
| dialog-formula-import.pen | 2.17 | 65cc8c16 | 11 | Import Formula Dialog | 1200x800 |
| dialog-history-copy.pen | 2.17 | 48e2973e | 14 | Modal Overlay | 640x560 |
| dialog-registration-create.pen | 2.17 | 749fc303 | 0 | Dialog Overlay | 800x840 |
| first-run.pen | 2.17 | 9df6d4e0 | 12 | 初始化向导窗口 | 1440x900 |
| first-run.pen | 2.17 | 9df6d4e0 | 12 | TextField 组件 (reusable) | 560x |
| form-formula-edit.pen | 2.17 | a98fc3ac | 14 | 验方编辑页面 | 1440x900 |
| form-herb-edit.pen | 2.17 | cca51648 | 15 | Herb Edit Form | 1440x900 |
| form-medical-case-edit.pen | 2.17 | 89e0890a | 18 | 医案编辑页面 | 1440x900 |
| form-patient-edit.pen | 2.17 | caa27714 | 16 | Patient Edit Form | 1440x900 |
| form-user-edit.pen | 2.17 | 833ddc96 | 18 | Edit User Page | 1440x900 |
| formula-management.pen | 2.17 | 67baee6c | 18 | 验方管理页面 | 1440x900 |
| herb-management.pen | 2.17 | 42a18288 | 16 | 中药药材管理 | 1440x900 |
| log-level.pen | 2.17 | 729d281f | 9 | Log Level Control Window | 1440x900 |
| login.pen | 2.17 | ad878b68 | 11 | 登录界面 | 1440x900 |
| main-window.pen | 2.17 | 94fbacc1 | 20 | NavItem 组件 (reusable) | fill_container(216)x |
| main-window.pen | 2.17 | 94fbacc1 | 20 | 统计卡片组件 (reusable) | fill_container(260)x |
| main-window.pen | 2.17 | 94fbacc1 | 20 | 快捷操作组件 (reusable) | fill_container(170)x |
| main-window.pen | 2.17 | 94fbacc1 | 20 | 主界面 · 今日工作台 | 1440x900 |
| main-window.pen | 2.17 | 94fbacc1 | 20 | 主界面 · 侧边栏收起 | 1440x900 |
| medical-case-management.pen | 2.17 | 18cdb157 | 13 | 医案管理页面 | 1440x900 |
| medical-case.pen | 2.17 | 1e074cdb | 18 | 医案工作台 | 1440x900 |
| patient-list.pen | 2.17 | e2bcb57e | 12 | 患者管理 | 1440x900 |
| patient-list.pen | 2.17 | e2bcb57e | 12 | 患者管理-收拢 | 1440x900 |
| patient-selection.pen | 2.17 | 4963c2e6 | 15 | 患者选择页面 | 1440x900 |
| receptionist-home.pen | 2.17 | 778d9a9a | 20 | 前台工作台首页 | 1440x920 |
| registration.pen | 2.17 | 18f2d39f | 13 | 挂号管理页面 | 1440x900 |
| registration.pen | 2.17 | 18f2d39f | 13 | 挂号列表行 (reusable) | 992x64 |
| reports.pen | 2.17 | 72f1a127 | 14 | 报表首页 | 1440x900 |
| security-audit-log.pen | 2.17 | 6aa25a14 | 17 | Security Audit Log Screen | 1440x900 |
| server-config.pen | 2.17 | a9c7a945 | 18 | Server Config Page | 1440x900 |
| sysadmin-home.pen | 2.17 | c9a3399a | 14 | 系统配置中心主窗口 | 1440x900 |
| system-settings.pen | 2.17 | 98ed2241 | 15 | System Settings Window | 1440x900 |
| user-management.pen | 2.17 | 0e3efd54 | 20 | 用户管理-列表页 | 1440x900 |
| user-management.pen | 2.17 | 0e3efd54 | 20 | 用户管理-新建用户对话框 | 1440x900 |

### 8.2 检测规则

- **顶部判定**：节点 name 含 `顶部/头栏/Header/应用栏/AppBar/TopBar/顶栏`，优先匹配直属子项 `children[0].children[]`，缺失时搜索全树并标记 `嵌套`。
- **底部判定**：name 含 `底部/状态栏/Status/StatusBar/BottomBar/底栏/Footer`，直属优先；`Status Badge/Text` 等 h=20 的噪音因非直属且高度不符 32，已排除（仅直属计入）。
- **左侧判定**：name 含 `左侧/侧边/Sider/Sidebar/导航/Nav` 且 w∈[180,320] 时计入；`Right Sidebar` 宽度 280 单独标记；`Nav:xxx` 菜单项 h=48 忽略。
- **英文判定**：name 含 `Sidebar` 但不含 `侧边/左侧` 即为仅英文。
- **达标判定**：顶部 h==48、底部 h==32、左侧 w∈{240,64} 为 ✅，其余为 ❌。

### 8.3 关联基线

- `docs/07-ui-ux/desktop-layout-framework.md` — SSOT：Header48 / Sider 240/64 / Status32 / Min1280×720
- `docs/07-ui-ux/desktop-design-tokens.md` §5 — 布局规范三栏图
- `docs/07-ui-ux/desktop-design-spec.md` §4 — 页面布局规范
- `designs/patient-list.pen` — 试点标杆（唯一 48/240/64/32 全达标）