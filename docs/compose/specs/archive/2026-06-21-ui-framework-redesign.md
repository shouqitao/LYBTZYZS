# UI/UX 基础框架重设计 Design

## [S1] 问题

Desktop UI 有 5 个核心问题：
1. 侧边栏没有功能导航（只有 3 个工具项）
2. 信息重复（时钟/用户名/API状态各出现 3-4 次）
3. 设计令牌碎片化（两套间距/圆角系统并存）
4. 卡片点击模式不一致 + emoji 图标
5. 冗余 BreadcrumbBar 控件

## [S2] 目标

完全重设计基础框架：侧边栏成为角色导航主导航、信息去重、令牌统一、卡片样式统一。

## [S3] 布局

保持全屏无边框。布局不变：左侧边栏(200px) + 面包屑 + ContentRegion + 状态栏。

## [S4] 侧边栏重设计

### 结构（从上到下）

```
┌──────────────────┐
│ ☰ 凌隐宝堂       │ ← Logo + 可折叠
├──────────────────┤
│ 👤 张医生 医生    │ ← 用户信息（唯一位置）
├──────────────────┤
│ 导航              │
│ 🏠 主页           │ ← 返回角色首页
│ 👥 患者管理       │ ← 角色权限过滤
│ 🌿 药材管理       │
│ 📋 验方管理       │
│ 📁 医案管理       │
│ 👤 用户管理       │
│ 📊 统计报表       │
├──────────────────┤
│ ⚙ 设置           │ ← 底部工具区
│ 🕐 14:30         │ ← 时钟（唯一位置）
│ ● 远程已连接      │ ← API状态（唯一位置）
├──────────────────┤
│ 🚪 退出登录       │
└──────────────────┘
```

### 导航项角色过滤

| 导航项 | Admin | Doctor | Receptionist | Sysadmin |
|--------|-------|--------|-------------|----------|
| 主页 | ✅ | ✅ | ✅ | ✅ |
| 患者管理 | ✅ | ✅ | ✅ | ❌ |
| 药材管理 | ✅ | ✅ | ❌ | ❌ |
| 验方管理 | ✅ | ✅ | ❌ | ❌ |
| 医案管理 | ✅ | ✅ | ❌ | ❌ |
| 用户管理 | ✅ | ❌ | ❌ | ✅(仅admin) |
| 统计报表 | ✅ | ✅ | ❌ | ❌ |

导航项可见性由 `RoleDefinition.GetAllModules()` + `ViewNames` 映射驱动。

### 图标

用矢量 Path 替换 emoji。每个导航项定义一个 24x24 Path data。

## [S5] 信息去重

| 信息 | 原位置 | 新位置（唯一） |
|------|--------|-------------|
| 时钟 | 侧边栏(展开) + 侧边栏(折叠) + 状态栏 | **侧边栏底部** |
| 用户名 | 侧边栏 + 状态栏 | **侧边栏顶部** |
| API 状态 | 侧边栏点 + 侧边栏文字 + 状态栏 + 连接徽章 | **侧边栏底部** |
| 连接模式 | 状态栏徽章 | **侧边栏底部**（与 API 状态合并） |

## [S6] 状态栏精简

从 7 列精简为 2 列：
- 左：加载指示器 + 加载消息
- 右：应用版本号

## [S7] 设计令牌统一

合并 `DesignSystem.xaml` + `DesignTokens/Spacing.xaml` 为单一文件。

决策：
- 间距令牌：`SpacingXS=4 / SpacingSM=8 / SpacingMD=12 / SpacingLG=16 / SpacingXL=24 / SpacingXXL=32`
- 圆角令牌：`RadiusSM=4 / RadiusMD=8 / RadiusLG=12`
- 删除 `Spacing.xaml` 中的重复令牌
- 全部改为 `DynamicResource`（支持未来主题切换）
- 删除 App.xaml 中的字体覆盖（让 DesignSystem 的字体栈生效）

## [S8] 卡片样式统一

- 统一为 `Button` + `TransparentButtonStyle`（可键盘聚焦、有 IsEnabled）
- 图标用矢量 Path（从 `HomePageStyles.xaml` 的 emoji 改为 Path data）
- 三个角色首页（Admin/Clinical/Receptionist）使用相同的 `FunctionCardStyle`
- 删除 Clinical 和 Receptionist 中本地定义的 `TransparentButtonStyle` 重复

## [S9] 清理

- 删除 `BreadcrumbBar.xaml`（死代码，仅 BreadcrumbControl 在用）
- 删除 `GlobalStatusBar` 中的时钟、用户名、API 状态（迁移到侧边栏）
- 删除 MainWindow 中的内联连接模式徽章（迁移到侧边栏）

## [S10] 范围

v1 只改基础框架（Sidebar/Breadcrumb/StatusBar/DesignSystem/卡片样式）。
角色首页内容（卡片网格布局）保持现有逻辑，只换图标和样式。
