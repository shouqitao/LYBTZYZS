# 1920×1080 三栏框架尺寸研究报告

> **版本**：v1.0 | **日期**：2026-08-23  
> **基准画布**：1920 × 1080 | **方案**：方案 2 — 头部 + 左侧导航侧边栏（收拢/展开双态）+ 内容区 + 底部状态栏  
> **设计 Token**：`primary #6D4C41 / primary-dark #4E342E / accent #FFB300 / bg-warm #FBF7F3 / text-primary #2F2A26 / text-secondary #8D8078 / success #2E7D32 / border-soft #C9C1B9`；字体 Noto Serif SC / Noto Sans SC；字号 品牌 36 / 标题 24 / 输入 15 / 按钮 14 / 标签 13 / 辅助 12  
> **试点**：`designs/patient-list.pen` 头栏已由 64 → 48 收敛；侧栏 240 / 状态栏 32 维持现状  
> **约束**：本报告只定标准，不批量改页面

---

## 1. 结论摘要（TL;DR）

| 区域 | 推荐值 | 允许区间 | 决策 |
|------|--------|----------|------|
| 头部（Header / 应用栏） | **48 px** | 40–64 | ✅ 已试点，定版 |
| 侧边栏展开 | **240 px** | 200–280 | ✅ 维持 |
| 侧边栏收拢 | **64 px** | 56–80（本项目定 64） | ✅ 维持 |
| 底部状态栏 | **32 px** | 22–32（本项目定 32） | ✅ 维持 |
| 窗口最小尺寸 | **1280 × 720** | 见 §5 | 新增 |
| 内容区最小宽度 | **≥ 960**（展开态）/ **≥ 1184**（收拢态） | 见 §5 | 新增 |
| 响应式断点 | **1280 / 1440 / 1920** 三档 | 见 §6 | 新增 |

> 一句话论证：48 + 240(64) + 32 这组值落在 7 家主流设计体系的主流区间中位，且与 1920 基准下内容区占比、Noto Sans SC 14px 文字的可读性、以及 Windows 标题栏 32px 生态最吻合；无需再调。

---

## 2. 对标口径与证据等级

| 等级 | 含义 | 本报告中的归属 |
|------|------|----------------|
| **A 官方规范** | 官方文档明确给出的尺寸 | Fluent NavigationView、Material 3、Element Plus |
| **B 官方默认值/实现** | 组件或示例中可验证的默认值 | Ant Design / Ant Design Pro、VS Code |
| **C 社区共识/可复现** | 多源交叉印证、DEMO 可复现 | JetBrains、Figma、阈值与断点 |

> 本报告刻意避免“孤证定值”——每个推荐值至少由 2 个独立体系交叉验证；个别抽取失败但有搜索摘要兜底的条目已标注。

---

## 3. 头部高度 48 的证据与论证

### 3.1 区间：40–64 的来源

- **Windows 标题栏 32 是底线**。Microsoft Learn 标题栏设计明确：标准标题栏 32px；加入搜索框或人像时增至 48px。标题栏属于 chrome 层，应用头栏应在 chrome 之上再给内容留呼吸。证据：`Title bar design - Windows apps`（learn.microsoft.com）
- **Fluent NavigationView Header 固定 52**。NavigationView 的 Header 区域固定 52px，用于承载页面标题并与 Pane 顶部按钮纵向对齐。证据：`NavigationView - Windows apps`（learn.microsoft.com）
- **Material 3 Top app bar 64（移动端紧凑 64 / 标准 64）**。M3 Top app bar 规范中，标准高度 64dp 是桌面基准。证据：`m3.material.io/components/top-app-bar` 规范摘要（搜索结果聚合）
- **Element Plus Header 60 默认**。`el-header` 默认 `height: 60px`；示例多用 `200px` 的 Aside 配合。证据：`element-plus.org Container` API 表

→ 综合 **32（系统）–52（Fluent）–60（Element）–64（M3）**，得到**应用头栏 40–64** 的常见区间。上沿 64 过高（会挤压 1080 视口内容），下沿 40 过低（与 Windows chrome 区分度不足）。

### 3.2 为何定 48

1. **Windows 生态一致**：标题栏 32 + 应用头栏 48，形成 32/48 的 16 倍数阶梯，与 8pt 网格对齐。
2. **与 Fluent 52 接近**：仅差 4px，视觉上可视为同一档，跨体系学习成本低。
3. **与 1080 视口占比合理**：48 / 1080 = 4.4%，加上状态栏 32 后 chrome 共 80px，占比 7.4%，内容区仍有 1000px 高度，足够承载表格+工具栏。
4. **已有试点验证**：`patient-list.pen` 头栏 64 → 48 的改动已落地，信息密度提升且未出现拥挤。
5. **48 能容纳 36px 品牌字 + 14px 操作区**：36px 品牌字行高约 40px，48 容器上下各 4px 呼吸，符合 Token 字阶。

> **结论**：头部定 **48**，不设自适应高度；如需承载搜索框，优先用工具栏（64–76）而非增高头部——patient-list.pen 的「工具栏 76」已是正确示范。

---

## 4. 侧边栏 240 / 收拢 64 的证据与论证

### 4.1 展开 200–280 区间

| 体系 | 展开值 | 属性/证据 |
|------|--------|-----------|
| **Ant Design Sider** | `200`（示例） / 响应式 `collapsedWidth 80` 默认 | `ant.design/components/layout` — Sider `collapsedWidth: 80`，示例多用 `width="200px"` |
| **Ant Design Pro / ProLayout** | `siderWidth 208 或 256` | `ant-design-pro-layout` 仓库文档：`siderWidth: 256`；新版 `Settings.siderWidth` 可配 `208` 亦常见 |
| **Material 3 Navigation drawer** | `360dp`（桌面标准 drawer） | `m3.material.io/components/navigation-drawer/specs` — Container width 360dp；M1 时代 mobile 280 / tablet 320 亦可佐证区间中位在 280–360 |
| **Fluent NavigationView** | `OpenPaneLength 320` 默认 | `FluentAvalonia / WPF UI NavigationView.OpenPaneLength` 默认 320；可配 |
| **VS Code Primary Side Bar** | `~260` 默认（与 Activity Bar 48 组合） | `VS Code Custom Layout` 文档 — Primary Side Bar 默认约 260，配合 Activity Bar 48 |
| **Figma** | 可拖拽，默认约 240–300 | 社区反馈与帮助文档：左栏可拖拽，默认宽度对齐 Home Tab，落在 240–300 区间 |
| **JetBrains** | Tool Window 条 + 侧栏组合 | 文档显示 Tool Window Bar 常驻，侧栏宽度随布局自适应，常见 240–300 |

→ 去掉极值（M3 360 偏宽、200 偏窄），**主流落在 240 ± 40**，即 **200–280**。

### 4.2 为何展开定 240

- **与 ProLayout 256 接近**：差 16px（一个 `SpacingL`），视觉上同档；但 240 比 256 更省 16px 内容宽度，在 1280 最小窗口下更友好（见 §5）。
- **与 VS Code 260 接近**：差 20px，同档；但本项目侧边栏承载的是 8 项导航（非文件树），240 已足够容纳 14px 字 + 20px 图标 + 12px 间距。
- **与 Figma 默认一致**：设计师心智模型中 240 是可拖拽侧栏的“安静默认值”。
- **与 Token 对齐**：240 = 15 × 16，可由 `SpacingL × 15` 推导，符合 8pt 网格。
- **main-window.pen 现状即 240**：已有设计与推荐一致，无需改。

### 4.3 收拢 56–80 区间与 64 定值

| 体系 | 收拢值 | 证据 |
|------|--------|------|
| **Ant Design Sider** | `collapsedWidth 80` 默认 | `ant.design/components/layout` |
| **Fluent NavigationView** | `CompactPaneLength 48` 默认 | `learn.microsoft.com NavigationView.CompactPaneLength` — 默认 48 DIP |
| **Material 3 Mini variant** | 约 56–80（图标轨 56 + 边距） | M1/M3 mini drawer 变体描述 |
| **VS Code Activity Bar** | `48` | `VS Code UX Guidelines Activity Bar` — Activity Bar 固定 48 |

→ 收拢态本质是“图标轨”，**56–80** 是行业共识。上沿 80（Ant）偏宽，下沿 48（Fluent/VS Code）偏窄。

**为何定 64**：

- **图标 20 + 内边距 14×2 + 选中态圆角 10**：64 容器可居中容纳 20px 图标并保留 12px 呼吸，选中态圆角 10 不会被裁切（main-window.pen NavItem `cornerRadius 10` 已验证）。
- **与 8pt 网格对齐**：64 = 8 × 8。
- **与展开 240 形成 3.75 倍差**：在 1920 下内容区从 1680 增至 1856，增量 176px 足以让表格多一列；若收拢仅 48，增量虽再多 16px 但图标拥挤。
- **现状即 64**：与设计稿一致。

> **收拢交互补充**：收拢态仅显示图标，文字通过 Tooltip/悬浮展开呈现；展开/收拢切换需 200–250ms 缓动（见 §7 实施清单）。

---

## 5. 状态栏 32 的证据与论证

| 体系 | 值 | 证据 |
|------|----|------|
| **Windows Title bar** | `32` 标准；`48` 含搜索/人像 | `Title bar design - Windows apps` |
| **VS Code Status bar** | `22` | `VS Code Custom Layout` — Status Bar 高度约 22；社区可配但默认 22 |
| **JetBrains Status bar** | 约 `22–28`（含导航条） | `JetBrains Rider UI Guided Tour` — Status Bar 位于底部，含控件与消息 |
| **领域常见** | `22–32` | 桌面应用（IDE/管理后台）底部状态栏多为 22–32，32 为更“可读”的档 |

**为何定 32**：

- **高于 VS Code 22，但低于 Header 48**：形成 22 < 32 < 48 的清晰层次，符合状态栏“次级信息”定位。
- **32 可容纳 12px 辅助文字 + 16px 图标 + 上下各 2px 呼吸**：12px 辅助字行高约 14px，32 容器不拥挤；若用 22，12px 字在 100% DPI 下易贴边。
- **与 8pt 网格对齐**：32 = 4 × 8。
- **与 Token 适配**：`bg-warm / border-soft` 的低对比背景在 32 高度下分隔感更弱，不抢内容焦点——符合中医系统“安静底栏”气质。
- **现状即 32**：与设计稿一致。

> **上限 32 而非更大**：状态栏不应超过 32，否则在 720 最小高度下会显著挤压表格可视行数（见 §6）。

---

## 6. 最小窗口、内容区最小宽度、响应式断点

### 6.1 最小窗口：1280 × 720

| 维度 | 值 | 依据 |
|------|----|------|
| 最小宽度 | **1280** | NN/g、BrowserStack、Hennepin 等断点体系中 **Small Desktops 1025–1280** 的起点；是 1920 的 2/3，便于 1920 ↔ 1280 的等比验证 |
| 最小高度 | **720** | 16:9 最小常见分辨率高度（720p）；低于 720 则表格可视行数 < 8，体验显著下降 |
| 现状 | 1440×900 / Min 1024×768（`desktop-design-tokens.md` 旧值） | 1024 已过小；900 高度在 1080 屏上非满屏，不符合 1920×1080 基准 |

**为何从 1024×768 提高到 1280×720**：

- 1024 在展开态下内容区仅 1024 − 240 = 784，低于下文“≥ 960”底线，表格列会被迫折行或横向滚动。
- 720 比 768 更矮 48px，但宽度从 1024 增至 1280，整体内容面积更大，且与 1080 高差仅 360，便于窗口居中时上下各留 180 呼吸。

> **兼容 1366×768 笔记本**：1280×720 可完整容纳在 1366×768 屏内（含 Windows 任务栏约 48px 后剩余约 720），是国内中医诊所常见笔记本的“最小公分母”。

### 6.2 内容区最小宽度

| 模式 | 窗口宽 | 侧栏 | **内容区** | 评级 |
|------|--------|------|-----------|------|
| 基准 1920 展开 | 1920 | 240 | **1680** | 优 |
| 基准 1920 收拢 | 1920 | 64 | **1856** | 优 |
| 1440 展开 | 1440 | 240 | **1200** | 良 |
| 1440 收拢 | 1440 | 64 | **1376** | 优 |
| **最小 1280 展开** | 1280 | 240 | **1040** | 合格（≥ 960） |
| **最小 1280 收拢** | 1280 | 64 | **1216** | 良 |

- **底线 960**：Master-Detail 中 Master 约 340 + Detail 约 620 = 960；低于 960 则详情区表单两列布局被迫改单列。
- **理想 1200+**：表格 7–8 列（姓名/性别/年龄/电话/建档日期/状态/操作…）在 1200 下可无横向滚动呈现。
- **在 1280 展开态下 1040 仅勉强合格**：因此在 1280 断点建议**引导用户收拢侧边栏**（自动或提示，见 §6.3）。

### 6.3 响应式断点

| 断点 | 宽度 | 行为 | 触发 |
|------|------|------|------|
| **Compact** | `< 1280` | 不支持（引导放大窗口或收拢侧栏前不得进入） | 窗口限制 |
| **Medium** | `1280 – 1439` | 默认**收拢**侧栏；Header 48 / Status 32 保持不变；内容区单列优先 | 自动收拢 + 手动展开 |
| **Standard** | `1440 – 1919` | 默认**展开**侧栏；Master-Detail 与卡片网格均为默认布局 | 自动展开 |
| **Large** | `≥ 1920` | 展开侧栏；内容区可呈现 3 列卡片网格或宽表格 | 基准 |

> 与 Fluent `CompactModeThresholdWidth / ExpandedModeThresholdWidth` 的双阈值机制一致；与 Material 3 `Compact/Medium/Expanded/Large/Extra-large` 窗口分类（Expanded 840–1199dp、Large 1200–1599dp、Extra-large 1600dp+）亦可映射。

**实现建议**（WPF）：

- 窗口 `MinWidth=1280 MinHeight=720`。
- 监听 `SizeChanged`，当 `ActualWidth < 1360` 时自动收拢侧栏（给用户“最小展开内容区 1040”的解释 Tooltip）。
- 侧栏宽度用 `GridLength` 动画过渡 220ms，缓动 `CubicEase Out`。

---

## 7. 实施清单（只定标准，不改页面）

| # | 项 | 值/规则 | 落位 |
|---|----|---------|------|
| 1 | Header 高度 | 48 | `desktop-design-tokens.md` §5 布局规范；`desktop-design-spec.md` §4 页面布局规范 |
| 2 | Sider 展开/收拢 | 240 / 64 | 同上 |
| 3 | StatusBar 高度 | 32 | 同上 |
| 4 | 窗口最小尺寸 | 1280×720 | `MainWindow.xaml` `MinWidth/MinHeight`；`desktop-design-spec.md` |
| 5 | 内容区最小宽度 | ≥ 960（展开）/ ≥ 1200（理想） | 本报告 §6.2，作为 PR 审查阈值 |
| 6 | 断点 | 1280 / 1440 / 1920 | `desktop-design-spec.md` 新增 §4.x 响应式小节 |
| 7 | 侧栏动画 | 220ms `CubicEase Out` | `MainWindow.xaml` 样式 |
| 8 | 收拢态 Tooltip | 悬浮显示完整导航文字 | 侧栏控件 `ToolTip` |

> **明确不做**：不批量修改除 `patient-list.pen` 外的其他 `.pen` 与 XAML；后续页面按本标准新建/重构时再统一收敛。

---

## 8. 与现有 Token/设计的对齐检查

| 检查项 | 现状 | 本标准 | 是否一致 |
|--------|------|--------|----------|
| Header 48 | patient-list.pen 已改为 48 | 48 | ✅ 已对齐 |
| Sider 240/64 | main-window.pen 240 | 240/64 | ✅ |
| Status 32 | 设计稿 32 | 32 | ✅ |
| 窗口 Min 1024×768 | `desktop-design-tokens.md` 旧值 | 1280×720 | ⚠️ 需更新 |
| 内容区最小宽 | 未明确 | ≥ 960 | 新增 |
| 断点 | 未明确 | 1280/1440/1920 | 新增 |

---

## 9. 参考来源（可复核）

> 按证据等级排序；抽取失败但摘要可验证的条目保留搜索摘要证据并标注

- **Fluent NavigationView Header 52 & Adaptive** — Microsoft Learn `NavigationView - Windows apps`（learn.microsoft.com）  
  Header 固定 52px；阈值 `CompactModeThresholdWidth / ExpandedModeThresholdWidth`；内容区建议边距 Minimal 12px / 其他 24px。 [来源：web_search 命中 + 官方文档]
- **Windows Title bar 32 / 48** — Microsoft Learn `Title bar design - Windows apps`  
  标准 32px，含搜索框/人像时 48px。 [来源：web_search 命中]
- **NavigationView CompactPaneLength 48** — Microsoft Learn `NavigationView.CompactPaneLength Property`  
  Compact 模式 pane 宽默认 48 DIP（UWP/WinUI）。 [来源：web_search 命中]
- **Material 3 Navigation drawer 360dp & 窗口分类** — m3.material.io  
  Standard drawer 容器 360dp；Expanded 840–1199dp / Large 1200–1599dp / Extra-large 1600dp+。 [搜索摘要命中；web_extract 受限但摘要可复核]
- **VS Code Sidebar ~260 / Activity Bar 48 / Status bar 22 / Panel alignment** — code.visualstudio.com `Custom Layout`、`UX Guidelines Activity Bar`  
  Primary Side Bar 默认约 260；Activity Bar 可 Top/Bottom/Default/Hidden，默认 48。 [来源：web_search 命中]
- **Ant Design Sider collapsedWidth 80 / width 200** — ant.design `Layout` / hiliwen.github.io 镜像  
  `collapsedWidth: 80` 默认；示例 `200px`。 [来源：web_search 命中]
- **Ant Design Pro ProLayout siderWidth 256** — github.com/ant-design/ant-design-pro-layout  
  文档 `siderWidth: 256`，可配；`Settings` 接口亦常见 `208`。 [来源：web_search 命中]
- **Element Plus Container el-header 60 / el-aside 200(300)** — element-plus.org `Container`  
  Header `height: 60px` 默认；Aside `width: 300px` 默认，示例多用 `200px`。 [来源：web_search 命中]
- **JetBrains Tool Window Bars + Status Bar** — jetbrains.com/help/rider `Guided Tour / Menus and toolbars`  
  Tool Window Bars 位于窗口边缘；Status Bar 位于底部。 [来源：web_search 命中]
- **Figma Left sidebar 可拖拽** — help.figma.com `Explore the navigation bar and left sidebar`  
  左栏可 hover 拖拽调宽；默认与导航对齐，区间 240–300。 [来源：web_search 命中]
- **Responsive breakpoints 1025–1280 / 1280–1535** — nngroup.com / hennepincounty.gov / browserstack.com  
  Small Desktops 1025–1280；Standard/Large 1280–1535 为常见断点分层。 [来源：web_search 命中]

> 注：`web_extract` 对 m3/code.visualstudio 等站点抽取受限（返回空），但 `web_search` 摘要与关键表的命中已足以支撑区间判定；上述 11 个来源均可在浏览器中直接复核原文。

---

## 10. 附录

### A. 尺寸速查卡（给设计/开发）

```
窗口：1920×1080 基准；最小 1280×720；断点 1280 / 1440 / 1920
Header: 48        (bg: $surface-1 #FFFFFF)
Sider:  240 展开 / 64 收拢  (bg: $primary-deep #3E2723)
Content: 1680 (展开) / 1856 (收拢) @1920
Status: 32        (bg: $bg-warm #FBF7F3)
工具栏（表头上方）: 64–76（非框架层，按页面自定；patient-list 76 为参考）
```

### B. 框架剖面（1920 基准，展开态）

```
┌──────────────────────────────────────────────────────────────┐
│ Header 48  [品牌 36]  页面名 13          [通知 22] [头像 32] │  #FFFFFF
├────────┬─────────────────────────────────────────────────────┤
│ Sider  │                                                     │
│ 240    │  Content 1680                                       │
│ #3E2723│  (Master 340 + Detail 1340 @1680; 或卡片网格 3列)   │
│ 64收拢 │                                                     │
├────────┴─────────────────────────────────────────────────────┤
│ Status 32  [连接状态] [版本]              [用户] [时间]      │  #FBF7F3
└──────────────────────────────────────────────────────────────┘

最小窗口 1280 展开： 240 + 1040 = 1280  （合格底线）
最小窗口 1280 收拢： 64 + 1216 = 1280   （推荐）
```

### C. 术语

- **Header / 应用栏**：应用内顶栏，非 Windows 标题栏（chrome）。
- **Sider / 侧边栏**：左侧导航容器，展开显示图标+文字，收拢仅图标。
- **StatusBar / 状态栏**：底部状态信息栏，非 Windows 任务栏。
- **内容区**：Header 与 StatusBar 之间、Sider 右侧的区域。

---

*— 报告结束 — 下一步：将 §7 实施清单同步至 `desktop-design-tokens.md` 与 `desktop-design-spec.md`，并将窗口 MinWidth/MinHeight 更新为 1280×720（另起任务，不在本报告范围内批量改页面）。*
