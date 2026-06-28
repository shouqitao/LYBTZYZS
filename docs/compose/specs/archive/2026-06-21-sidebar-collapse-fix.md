# 侧边栏展开/折叠一致性修复 Design

## [S1] 问题
展开(220px)和折叠(56px)两种状态下，各区域显示不一致、对齐混乱。

## [S2] 展开状态 (220px) — 完整显示

```
┌────────────────────┐
│ ☰  凌隐宝堂        │ Row 0: 汉堡 + 文字
├────────────────────┤
│ ⬤ 张医生           │ Row 1: 头像(32px) + 姓名 + 角色
│   医生角色          │       (可点击 → 弹出菜单)
├────────────────────┤
│ ────────────────── │ Row 2: 分隔线
├────────────────────┤
│ 🏠 主页            │ Row 3: 导航项（图标 + 文字）
│ 👥 患者管理        │       左对齐，Padding 16,0
│ 🌿 药材管理        │
│ ...                │
├────────────────────┤
│ ● 远程 14:30:25    │ Row 4: 状态（API圆点+文字 + 时钟）
│   2026-06-21       │
├────────────────────┤
│ ────────────────── │ Row 5: 分隔线
├────────────────────┤
│ 🚪 退出登录        │ Row 6: 退出（图标 + 文字）
└────────────────────┘
```

## [S3] 折叠状态 (56px) — 仅图标

```
┌──────┐
│  ☰   │ Row 0: 仅汉堡（居中）
├──────┤
│  ⬤   │ Row 1: 仅头像(28px, 居中, 可点击)
├──────┤
│  ──  │ Row 2: 分隔线（Margin 12,0）
├──────┤
│  🏠  │ Row 3: 仅图标（居中，20x20）
│  👥  │       每项 Height=44
│  🌿  │
│  ... │
├──────┤
│  ●   │ Row 4: 仅API状态圆点（居中）
│      │       时钟隐藏
├──────┤
│  ──  │ Row 5: 分隔线
├──────┤
│  🚪  │ Row 6: 仅退出图标（居中）
└──────┘
```

## [S4] 统一规则

1. **所有文字元素**：`Visibility="{Binding IsExpanded, ElementName=Root, Converter=Cvt.BoolToVis}"`
2. **所有图标元素**：始终可见，折叠时 `HorizontalAlignment=Center`
3. **头像**：展开 32px / 折叠 28px（缩小适应窄宽度）
4. **导航项 Padding**：展开 `16,0` / 折叠 `0,0`（去掉左右 padding 让图标居中）
5. **导航项 StackPanel**：展开 `Orientation=Horizontal` / 折叠 不变（文字隐藏后只剩图标）

## [S5] 实现

修改文件：
- `SidebarControl.xaml` — 统一所有区域的折叠/展开 DataTrigger
- `SidebarControl.xaml.cs` — 无需改动

关键改动：
- NavMenuItemStyle：加 DataTrigger `IsExpanded=False` → `HorizontalContentAlignment=Center` + `Padding=0,0`
- Row 1 用户信息 Button：加 DataTrigger → `HorizontalContentAlignment=Center`
- Row 4 状态区域：折叠时隐藏文字，只显示 API 圆点
- Row 6 退出按钮：加 DataTrigger → `HorizontalContentAlignment=Center`
- Row 1 头像尺寸：DataTrigger → `Width=28 Height=28`
