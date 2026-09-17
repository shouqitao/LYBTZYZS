# 布局框架方案决策 — 基于功能需求（已归档）

> **归档说明**: 本报告已于 2026-09-17 归档。原始文件路径: `docs/compose/reports/ui-layout-framework-decision.md`
> **归档原因**: 决策已定并落地为 SSOT `docs/07-ui-ux/desktop-layout-framework.md`
> **原始日期**: 2026-08-22 | **前置**: `ui-layout-framework-research.md`（3 方案调研）

## 归档内容摘要

### 决策结论

**选择方案 2（三栏端规则：固定头栏 48px + 可折叠侧栏 240/64 + 状态栏 32px）**

### 功能需求证据（从代码/需求收集）

| # | 功能需求 | 证据 | 对布局的要求 |
|---|----------|------|--------------|
| F1 | **5 个 Master-Detail 高频页**（患者/药材/验方/用户/挂号） | `PatientMasterDetailControl.xaml:26` 等 5 处 `MasterDetailLayout` | Master 固定 + Detail 可滚 + GridSplitter 拖拽可调 |
| F2 | **医案工作台长时间停留**（核心编辑） | `MedicalCaseWorkspaceViewModel.cs:64 KeepAlive=true`（558 行编辑态） | 内容区尽可能宽，但需快速返回（Alt+Left 已有） |
| F3 | **高频跨页跳转**（前台/医生） | `RegistrationListViewModel.cs:271`、`ClinicalWorkspaceViewModel.cs:135/153` 多处 `NavigateTo` | 持久可见导航（减少 1 次点击/跳转） |
| F4 | **4 角色 × 22 页**（权限矩阵 §2） | `04-permissions.md` SSOT | 侧栏分组折叠（临床/目录/管理），角色菜单动态 |
| F5 | **面包屑完整路径可回溯** | 调研 R15 "面包屑仅一级"问题清单 #33 | 头栏 BreadcrumbBar `Level1›2›3` |
| F6 | **状态栏信息常驻**（API/模式/用户/时间） | `MainWindow.xaml` 已实现 32px 右对齐 | 保留现状，勿删 |
| F7 | **列表页搜索/分页状态保持** | 调研 R09/R11 问题 #18 KeepAlive 未启用 | Detail 区滚动不影响 Master 状态 |
| F8 | **MinWidth=1024 窗口** | `MainWindow.xaml` MinWidth=1024 | 侧栏 240 + Master 380 + Detail 可接受（剩 ~400） |
| F9 | **MDIX 内置样式优先**（AGENTS 红线） | `desktop-design-spec.md §1.2` 不自定义 ControlTemplate | 方案须用 `MaterialDesignNavigationPrimaryListBox` / 现有样式 |
| F10 | **医疗软件持久导航规范** | web_search Health Gorilla "sidebar command center" | 导航不可隐藏（方案 3 缺陷） |
| F11 | **键盘快捷键**（`Ctrl+M` 侧栏 / `Alt+Left/Right/Home`） | `MainWindow.xaml InputBindings` | 折叠侧栏保留图标轨，快捷键继续有效 |
| F12 | **设计稿一致**（main-window.png：顶部搜索+侧栏+内容） | `design-spec-gap-analysis.md` 第 2 页 | 方案 2 与设计稿最接近，仅补面包屑/分组 |

## 后续落地文档

- **布局框架 SSOT**: [`docs/07-ui-ux/desktop-layout-framework.md`](../../07-ui-ux/desktop-layout-framework.md)（定版 48/240/64/32/220ms/1280-1440-1920）
- **对标自检**: [`desktop-frame-selfcheck-2026-08-23.md`](../reports/desktop-frame-selfcheck-2026-08-23.md)
