# 布局框架方案决策 — 基于功能需求

> 日期: 2026-08-22 | 前置: `ui-layout-framework-research.md`（3 方案调研）
> 决策依据: 功能需求（US/角色/页面清单/交互证据）对照 3 方案，本文件为**选择结论 + 理由**

## 决策结论

**选择方案 2（三栏端规则：固定头栏 48px + 可折叠侧栏 240/64 + 状态栏 32px）**

---

## 1. 功能需求证据（从代码/需求收集）

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

## 2. 方案 × 需求对照矩阵

| 功能需求 | 方案 1 整体直排 | 方案 2 三栏端规则 | 方案 3 抽屉全屏 |
|----------|----------------|------------------|------------------|
| F1 主从高频 + 拖拽 | ⚠️ 无 GridSplitter | ✅ GridSplitter 12px + Master 固定 | ❌ 无拖拽空间（内容全宽但 Master 硬 380） |
| F2 工作台长期停留 | ✅ 页内滚动 | ✅ Detail 滚动，Master 不干扰 | ✅ 内容最大化 |
| F3 高频跨页跳转 | ✅ 持久侧栏 | ✅ 持久侧栏 | ❌ 导航隐藏，每跳转 2 次点击 |
| F4 角色分组菜单 | ✅ 补分组 | ✅ 补分组 | ✅ 抽屉内分组 |
| F5 面包屑 | ⚠️ 头栏 48 新增 | ✅ 头栏 48 新增 | ✅ 头栏 56 新增 |
| F6 状态栏保留 | ✅ | ✅ | ⚠️ 精简 24px 移用户（信息减） |
| F7 列表状态保持 | ✅ | ✅ Master 常驻 | ✅ |
| F8 1024 窄屏 | ✅ 头栏+侧栏 320 | ⚠️ 头栏+侧栏+Master 620/1024 内容偏窄 | ✅ 但以隐藏导航为代价 |
| F9 MDIX 原生 | ✅ 现有 ListBox | ✅ NavigationPrimaryListBox (已用) | ✅ DrawerHost 原生 |
| F10 医疗持久导航 | ✅ | ✅ | ❌ |
| F11 快捷键 | ✅ | ✅ | ⚠️ 抽屉关闭态汉堡需再开 |
| F12 设计稿一致 | ⚠️ 部分 | ✅ 最高 | ❌ 无固定侧栏 |
| **满足数** | 8/12 | **11/12** | 8/12 |

## 3. 满足度评分

| 方案 | ✅ | ⚠️ | ❌ | 得分（✅=2, ⚠️=1, ❌=0） | 关键短板 |
|------|-----|-----|-----|------|----------|
| 方案 1 | 8 | 4 | 0 | 20 | 无主从拖拽、面包屑仍需补但头栏非固定 |
| **方案 2** | **11** | **1** | **0** | **23** | F8 窄屏 1024 下内容区 ~400px（可接受，MinWidth 已定） |
| 方案 3 | 8 | 3 | 1 | 19 | F3/F10 导航隐藏违医疗规范，F6 状态栏精简 |

## 4. 选择理由（按功能需求权重）

1. **F3 高频导航 > F2 内容最大化**（权重最高）：前台/医生/管理员均高频跨页（挂号→工作台、临床→患者），方案 3 的隐藏导航会在每个跳转增加 1 次点击；持久侧栏（方案 1/2）更符合 F10 医疗规范。方案 2 与方案 1 在此平分，但——>
2. **F1 主从拖拽使方案 2 胜出**：5 个 `MasterDetailLayout` 页面是系统主干（患者/药材/验方/用户/挂号），方案 2 的 GridSplitter 允许 Master 宽度可调（380→可拖），直接改善 5 页体验；方案 1 无此能力，方案 3 让渡给内容全宽。
3. **F5/F12 面包屑与设计稿**：方案 2 固定头栏是面包屑的天然载体（`BreadcrumbBar` 常驻），与 `main-window.png` 设计稿（顶部搜索+头像）最接近，差距最小（仅补面包屑+分组）。
4. **F6 状态栏零改动**：方案 2 保留现有 32px 状态栏（API/模式/用户/时间），方案 3 需精简迁移动用户到头栏（改动面+信息减）。
5. **F8 1024 窄屏**：方案 2 的唯一 ⚠️，但 `MinWidth=1024` 已由产品定，侧栏可折叠至 64（`Ctrl+M`）+ Master 可拖窄，Detail 仍 ≥400px，可接受。

## 5. 方案 2 实施范围（后续任务）

| 步骤 | 内容 | 改动文件 |
|------|------|----------|
| 1 | 头栏 48px：汉堡 + BreadcrumbBar（`Items` 绑定 `NavigationPath`）+ 全局 `SearchBox` + 通知 + 头像下拉 | `MainWindow.xaml` + 新 `Shell/Controls/AppHeaderControl.xaml` |
| 2 | 侧栏分组折叠：`NavigationItems` 改两级（组头：临床/目录/管理），`MenuManager` 按角色过滤组 | `MainWindow.xaml` ListBox ItemTemplate + `NavigationItem` model |
| 3 | `MasterDetailLayout` 内嵌 `GridSplitter` 12px（规格 §4.2 悬停 Primary 0.3/拖动 0.6） | `Controls/MasterDetailLayout.xaml` |
| 4 | 31 页补 `NavigationPath`（`Level1›2›3`）供面包屑 | 各 `*ViewModel.cs` `NavigationPath` 属性 |
| 5 | 列表页 `KeepAlive=true`（F7）| `MasterDetailViewModelBase.cs:118` 改 true + `OnNavigatedTo` 刷新 |

## 6. 验证

- `build 0/0` + `arch 91/91`
- 手工：4 角色登录 → 侧栏分组折叠 vs 角色、面包屑回溯、GridSplitter 拖拽、`Ctrl+M` 折叠
- 留存：`ui-layout-framework-research.md`（3 方案）+ 本文档（决策）供用户确认后转任务书

> 本文档为**选择结论**。若用户对方案 2 有异议（如倾向抽屉全屏或整体直排），可回退对照矩阵调整；实施范围将转为独立任务书。