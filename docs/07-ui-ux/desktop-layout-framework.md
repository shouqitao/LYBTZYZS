# Desktop 布局框架 — 端规则（SSOT）

> 版本: v1.0 | 日期: 2026-09-24（头登记日）

> 状态：**已固化（2026-08-23）** | 样板：`designs/patient-list.pen`（展开/收拢双帧）
> 本页为三栏框架 + 左侧导航 + 中间母版的唯一权威来源，设计与代码实现均以本页为准。
> 每个元素的属性精确到像素/色值/字号/间距，批量生成或修改时逐项比对。

## 变量表（全页共用）

| Token | 类型 | 值 | 用途 |
|-------|------|----|------|
| `$bg` | color | `#FBF7F3` | 全局背景 |
| `$surface` | color | `#FFFFFF` | 卡片/面板背景 |
| `$primary` | color | `#6D4C41` | 主色（顶栏/选中态/主按钮） |
| `$primary-dark` | color | `#4E342E` | 左侧栏背景 |
| `$primary-soft` | color | `#F1E9E4` | 浅底色（标签/徽标） |
| `$accent` | color | `#FFB300` | 品牌色（品牌块/顶部装饰条） |
| `$accent-dark` | color | `#8A5A00` | 深品牌色（徽标文字/图标） |
| `$text-primary` | color | `#3E2723` | 主文字 |
| `$text-secondary` | color | `#8D6E63` | 次文字 |
| `$border` | color | `#E9DFD7` | 描边/分割线 |
| `$on-primary` | color | `#FFFFFF` | 主色上文字 |
| `$bg-warm` | color | `#FBF7F3` | 暖灰背景（底部状态栏/主内容区） |
| `$success` | color | `#2E7D32` | 成功态（API 已连接） |
| `$font-ui` | string | `Noto Sans SC` | UI 字体 |
| `$font-body` | string | `Noto Sans SC` | 正文字体 |

## 根帧

| 属性 | 值 |
|------|-----|
| layout | `vertical`（必须！否则子元素错位） |
| fill | `$bg` |
| width | `1440`（基准） |
| height | `900`（基准） |
| clip | `true` |
| children | 顶部应用栏 + 下部主体 |

## 顶部应用栏（程序级信息层）

| 属性 | 值 |
|------|-----|
| name | `顶部应用栏` |
| width | `fill_container` |
| height | **48** |
| fill | `$primary` |
| layout | `horizontal` |
| gap | `12` |
| padding | `[0, 24]` |
| alignItems | `center` |

### 子元素（从左到右，7 个）

| # | name | 类型 | 属性 |
|---|------|------|------|
| 1 | 品牌标识 | frame | w=36 h=36 fill=$accent cornerRadius=10 layout=horizontal alignItems=center justifyContent=center |
| 1.1 | 标识字 | text | content=`医` fontFamily=$font-ui fontSize=20 fontWeight=700 fill=$primary-dark |
| 2 | 诊所名 | text | content=`中医诊所管理系统` fontFamily=$font-ui fontSize=17 fontWeight=600 fill=$on-primary |
| 3 | 弹性占位 | frame | width=fill_container height=fill_container |
| 4 | 用户分隔线 | frame | width=1 height=20 fill=#FFFFFF opacity=0.3 |
| 5 | (无name) | icon | icon=account_circle library=Material\ Symbols\ Rounded width=26 height=26 fill=#F0E4DE |
| 6 | 用户姓名 | text | content=`陈医生` fontFamily=$font-body fontSize=13 fontWeight=600 fill=#FFFFFF |
| 7 | 用户角色 | text | content=`医生` fontFamily=$font-body fontSize=12 fill=#D9C7C1 |

## 下部主体

| 属性 | 值 |
|------|-----|
| name | `下部主体` |
| width | `fill_container` |
| height | `fill_container` |
| layout | `horizontal` |
| gap | `0` |

children = [左侧导航, 右列]

## 左侧导航（展开帧 240 / 收拢帧 64）

| 属性 | 展开 | 收拢 |
|------|------|------|
| name | `左侧导航` | `左侧导航` |
| width | **240** | **64** |
| height | `fill_container`（贯通到底） | `fill_container` |
| fill | `$primary-dark` | `$primary-dark` |
| layout | `vertical` | `vertical` |
| gap | `4` | `4` |
| padding | `[12, 12, 12, 12]` | `[8, 12, 8, 12]` |
| clip | `true` | `true` |

### 汉堡按钮

| 属性 | 值 |
|------|-----|
| name | `汉堡按钮` |
| width | `fill_container` |
| height | `40` |
| layout | `horizontal` |
| alignItems | `center` |
| justifyContent | `center`（展开）/ `center`（收拢） |
| padding | `[0, 8]` |
| cornerRadius | `8` |

子元素：icon menu 20×20 fill=#F0E4DE library=Material\ Symbols\ Rounded

### 导航分组标题（仅展开帧）

| 属性 | 值 |
|------|-----|
| name | `导航分组标题` |
| content | `导航` |
| fontFamily | `$font-ui` |
| fontSize | **11** |
| fontWeight | `600` |
| fill | `#C9B8A6` |
| letterSpacing | `0.8` |
| opacity | `0.9` |
| padding | `[8, 0, 4, 12]` |
| **收拢帧** | **不显示（不存在）** |

### 菜单项（展开帧 = 图标 + 文字，收拢帧 = 仅图标）

展开帧菜单项：

| 属性 | 值 |
|------|-----|
| width | `fill_container` |
| height | **38** |
| layout | `horizontal` |
| alignItems | `center` |
| justifyContent | `flex-start` |
| gap | `12` |
| padding | `[0, 12]` |
| cornerRadius | **10** |
| 选中 fill | `$primary` |
| 未选中 fill | 无（透明） |

子元素：
- icon: width=18 height=18 library=Material\ Symbols\ Rounded，选中 fill=#FFFFFF，未选中 fill=#E8DDD4
- 文字: fontFamily=$font-body fontSize=13 fontWeight=400（选中500），选中 fill=#FFFFFF，未选中 fill=#E8DDD4

收拢帧菜单项：

| 属性 | 值 |
|------|-----|
| width | `fill_container` |
| height | **38** |
| layout | `horizontal` |
| alignItems | `center` |
| justifyContent | **`center`**（图标居中） |
| padding | **`[0, 0]`** |
| cornerRadius | **10** |
| 选中/未选中 | 同展开 |

子元素：**仅 icon**（18×18），**不包含文字节点**（已删除，不是隐藏）

### 角色 × 菜单矩阵

> **表头声明**：「设计稿目标态」列为 UI 设计稿像素级目标（`designs/*.pen`），**不是**当前代码行为。代码现状以 `Shell/Services/NavigationManager.cs` 为准（每角色 3 项：主页 + 2 业务入口）。两列并存供产品决策后二选一对齐。

| 角色 | 设计稿目标态（菜单项 / 图标） | 代码现状（NavigationManager.cs，2026-09-27 复核） |
|------|---------------|-------------------------|
| Doctor | 首页 / 患者管理 / 医案管理 / 药材(只读) / 验方 / 挂号 / 报表 / 个人资料 — home / people / medical_services / herbalism / recipe / assignment / bar_chart / person | 主页（ClinicalWorkspace）/ 患者选择 / 挂号队列 — 各 3 项 |
| Receptionist | 首页 / 患者管理 / 挂号 / 个人资料 — home / people / assignment / person | 主页（ReceptionistHome）/ 新建挂号 / 患者管理 — 各 3 项 |
| Admin | 首页 / 用户管理 / 患者管理 / 药材/验方 / 医案查看 / 报表 / 个人资料 — home / manage_accounts / people / herbalism / medical_services / bar_chart / person | 主页（AdminHome）/ 用户管理 / 药材/验方 — 各 3 项 |
| Sysadmin | 首页 / 备份管理 / 部署管理 / 日志级别 / 安全审计 / 个人资料 — home / backup / deploy / tune / security / person | 主页（SysadminHome）/ 备份管理 / 部署管理 — 各 3 项 |
| 共用底部 | 深色模式 / 退出 | 深色模式 / 退出（底栏，非侧栏菜单项） |

> ⚠️ 产品决策未决：侧栏是否扩展到设计稿 4~8 项。当前代码维持 C+ 矩阵（每角色 3 项）；扩展入口走主页卡片/快捷键，不改侧栏密度。

### 底部固定区（左侧栏底部）

| 顺序 | name | 属性 |
|------|------|------|
| 1 | 弹性空间 | frame w=fill_container h=fill_container |
| 2 | 分割线 | frame w=fill_container h=1 fill=#FFFFFF opacity=0.14 margin=[12,0,12,0] |
| 3 | 菜单:深色模式 | 同菜单项规格，icon=dark_mode |
| 4 | 菜单:退出 | 同菜单项规格，icon=logout |

## 右列

| 属性 | 值 |
|------|-----|
| name | `右列` |
| width | `fill_container` |
| height | `fill_container` |
| layout | `vertical` |
| gap | `0` |

children = [主内容区, 底部状态栏]

## 底部状态栏（程序级信息层）

| 属性 | 值 |
|------|-----|
| name | `底部状态栏` |
| width | `fill_container` |
| height | **32** |
| fill | `$bg-warm` |
| layout | `horizontal` |
| gap | `8` |
| padding | `[0, 24]` |
| alignItems | `center` |
| justifyContent | `space_between` |
| stroke | `#E9DFD7` |
| strokeWidth | `1`（顶部描边） |

### 子元素（左组 + 右时间）

| # | name | 类型 | 属性 |
|---|------|------|------|
| 1 | 状态左组 | frame | layout=horizontal gap=16 alignItems=center |
| 1.1 | API状态 | text | content=`● API 已连接` fontFamily=$font-body fontSize=12 fill=$success |
| 1.2 | 连接模式 | text | content=`远程模式` fontFamily=$font-body fontSize=12 fill=#8D6E63 |
| 2 | 状态时间 | text | content=`2026-08-23 10:00` fontFamily=$font-body fontSize=12 fill=#8D6E63 |

## 中间母版（内容级容器）

| 层级 | 元素 | 属性 |
|------|------|------|
| 外容器 | `主内容区` | w=fill_container h=fill_container fill=$bg-warm layout=vertical gap=**16** padding=**[16,16,16,16]** clip=true |
| 工具条 | `工具栏` | w=fill_container h=**56** fill=$surface gap=8 padding=[0,16] alignItems=center cornerRadius=**12** stroke=#E9DFD7 strokeWidth=1 |
| 内容横排 | `内容区` | w=fill_container h=fill_container layout=horizontal gap=**16** |
| 卡片 | `XXX卡片/面板` | fill=$surface cornerRadius=**12** stroke=#E9DFD7 strokeWidth=1 |
| 分割 | `GridSplitter` | w=1 h=fill_container fill=#E9DFD7 |

## 双帧规则

| 规则 | 说明 |
|------|------|
| 每页两帧 | 展开帧 x=0 + 收拢帧 x=1480（不可重叠） |
| 收拢帧复用 | 展开帧 clone → 改侧栏 64 → 移除导航标题 → 菜单删文字 → justify=center |
| 帧名 | `{页面名}` + `{页面名}-收拢` |
| ID 唯一 | 两帧内所有节点 ID 不可重复（pen.dev 报 duplicate identifiers） |
| layout | 根帧必须 `layout=vertical`（否则子元素错位） |
| 组件帧 | 额外的组件/对话框帧不套三栏框架，保留原样 |

## 排除页（不使用母版）

- `login.pen`（登录界面，独立布局）
- `first-run.pen`（初始化向导，独立布局）
- `dialog-*.pen`（弹窗 overlay，浮于主框架之上）
- `form-*.pen`（表单页，独立布局）
- `main-window.pen`（组件库，非页面帧）
