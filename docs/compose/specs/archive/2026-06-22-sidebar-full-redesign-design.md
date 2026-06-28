# 侧边栏全面重设计 Design

> **日期**: 2026-06-22
> **状态**: Approved (brainstorm phase)
> **范围**: SidebarControl 全面重设计 — 双面板布局 + 信息架构重建 + bug 修复
> **前置**: 替代 `2026-06-21-sidebar-collapse-fix.md`（该 spec 仅修复表层一致性，未触及结构问题）

---

## [S1] 问题诊断

用户反馈 3 个核心痛点：

1. **交互不一致** — 用户信息编辑入口（Avatar 点击 → 代码后置 ContextMenu）与主页/用户管理的命令式交互割裂
2. **展开/折叠显示 bug** — 切换时布局错位、信息丢失、宽度瞬间跳变
3. **信息不一致** — AccountSettings 编辑后侧边栏无确认反馈

### 根因分析（代码层）

| 编号 | 症状 | 代码位置 | 根因 |
|------|------|---------|------|
| R1 | Avatar 点击走代码后置 | `SidebarControl.xaml.cs:15-32` `OnUserAvatarClick` | 即用即弃 `ContextMenu`，未走 `Command` 绑定 |
| R2 | `EditProfileCommand` 暗绑 | `SidebarControl.xaml.cs:120-130` | DP 暴露但仅在代码后置路径调用 |
| R3 | `NavigateToSystemSettingsCommand` 死代码 | `SidebarControl.xaml.cs:134-145` | DP 暴露但 `MainWindow.xaml` 从未绑定 |
| R4 | `IsExpanded` 默认 `false` | `SidebarControl.xaml.cs:42-44` | 启动即 56px 收缩态，用户初次看到的是"残缺版" |
| R5 | 宽度瞬间跳变 | `SidebarControl.xaml:93` `BoolToDoubleConverter` | 无动画，`Double` 值直接切换 |
| R6 | 折叠态丢失模式/时钟 | `SidebarControl.xaml:229-244` | 折叠态仅保留 API 圆点，其他状态信息完全消失 |
| R7 | `NavigateToHomeCommand` 暴露未用 | `SidebarControl.xaml.cs:106-117` | DP 暴露但 XAML 中无任何元素绑定 |
| R8 | 编辑后无反馈 | `AccountSettingsControl.xaml` | 保存成功后无 Toast，侧边栏不显示新字段 |

---

## [S2] 设计目标与约束

### 目标

- 修复全部 R1-R8 根因
- 重建侧边栏信息架构（双面板布局）
- 提升视觉重设计感（不大动主题令牌）
- 支持平滑展开/折叠动画

### 约束（用户确认）

| 约束 | 内容 |
|------|------|
| **范围** | C. 全面重设计 |
| **主题** | 不大动 — 复用现有 DynamicResource 令牌 |
| **角色差异** | 仅 NavigationItems 注入差异，外壳完全一致 |
| **折叠** | 保留并修复（动画 + 信息保留） |

---

## [S3] 整体布局 — 双面板 + 状态条

### 分区结构

```
┌──────────────────────────┐
│  上半卡片 (品牌+导航)     │  ← SidebarBrush (现有深色)
│                          │
│  [品牌行: ☰ 凌隐宝堂]    │  56px
│                          │
│  [导航区 - 可滚动]        │
│    主页组(无标题)          │
│    业务组                  │
│    管理组                  │
│                          │
└──────────────────────────┘
       ↕ 8px 留白
┌──────────────────────────┐
│  下半卡片 (用户+状态)     │  ← SidebarHoverBrush (稍亮)
│                          │
│  [用户卡 - 可点]          │  64px (展开) / 48px (折叠)
│                          │
│  [状态条 - 始终可见]      │  56px (展开) / 28px (折叠)
└──────────────────────────┘
```

### 尺寸规范

| 元素 | 展开态 | 折叠态 | 动画 |
|------|--------|--------|------|
| 侧边栏总宽 | 220px | 56px | 200ms `CubicEase` |
| 上半卡片高 | `*` (填充剩余) | `*` | — |
| 下半卡片高 | `Auto` (内容驱动) | `Auto` | — |
| 品牌行 | 56px | 56px | — |
| 用户卡 | 64px | 48px | 150ms |
| 状态条 | 56px (双行) | 28px (单行图标) | 150ms |
| 导航项 | 40px，padding `12,0` | 40px，居中 | — |
| 分组标题 | 11px 字号，`SidebarSecondaryTextBrush` | 隐藏 | — |

### 关键视觉变化

1. **双卡片分层** — 下半卡片用 `SidebarHoverBrush` 制造"漂浮"感，8px 透明间隙分隔
2. **导航分组小标题** — 11px 灰色字符串，提升信息扫描效率
3. **用户卡 `⋯` 副按钮** — 右侧独立可点的"更多"按钮，弹 Popup
4. **状态条圆点放大** — API 圆点 10px → 12px，加 `DropShadowEffect` 模拟内发光

---

## [S4] 用户卡 + 账户菜单交互

### 用户卡布局

**展开态**:

```
┌─────────────────────────────┐
│ ┌──┐                        │
│ │张│  张三           [⋯]    │  ← 主体可点 + ⋯按钮独立可点
│ └──┘  主治医生              │
└─────────────────────────────┘
```

**折叠态**:

```
┌────┐
│ 张 │  ← 仅 avatar (28px)
└────┘  hover tooltip: "张三 / 主治医生"
```

### 点击行为矩阵

**展开态**:

| 触发位置 | 行为 | 绑定命令 |
|---------|------|---------|
| 用户卡主体（avatar + 文字区域） | 跳转到 `AccountSettingsView` | `EditProfileCommand` |
| 右侧 `⋯` 更多按钮 | 弹出 `Popup` 菜单 | — |
| Popup → 「个人资料」 | 跳账户设置 | `EditProfileCommand` |
| Popup → 「退出登录」 | 退出登录 | `LogoutCommand` |

**折叠态**（`⋯` 按钮隐藏）:

| 触发位置 | 行为 | 绑定命令 |
|---------|------|---------|
| Avatar 圆圈 | **直接弹出 Popup 菜单**（不跳账户页） | — |

> **折叠态设计理由**：折叠态下用户卡空间不足以容纳文字提示，直接跳账户页会让用户困惑"为什么点了头像就跳页面"。改为弹 Popup 给用户明确的选择入口。展开态空间充足，主体点击跳账户页是更高效的快捷路径。

> **注**：原计划含「切换连接模式」项，经评估 `ToggleConnectionCommand` 不在 `MainWindowViewModel` 范围内，删除以避免范围蔓延。

### "主体可点" 边界澄清

用户卡是一个 `Grid`，内部分为两个 `Button`：
- **主体 Button**：占据左侧 ~85% 宽度（avatar + name + role 区域）
- **⋯ Button**：固定 32x32，右对齐

两个 Button 互不重叠，`Grid.ColumnDefinitions` 明确分配宽度。`Hit Test` 不会冲突。

### 关键修复

| 编号 | 修复 |
|------|------|
| **R1** | 删除 `OnUserAvatarClick` 整段代码后置 |
| **R2** | 用户卡主体 `Button Command="{Binding EditProfileCommand, ElementName=Root}"` — `EditProfileCommand` 获得明确绑定路径 |
| **R3** | 删除 `NavigateToSystemSettingsCommand` DP + `MainWindow.xaml` 中对应绑定 |
| **R4** | `IsExpanded` DP 默认值改为 `true`（`PropertyMetadata(true)`） |
| **R7** | `NavigateToHomeCommand` 绑定到品牌行（☰ 凌隐宝堂 logo）点击 |

### Popup 实现选择

使用 WPF `Popup` 控件（不是 `ContextMenu`）：
- 自定义内容、样式继承自主题
- `StaysOpen=False` 点击外部自动关闭
- `Placement=Bottom` + `PlacementTarget=⋯按钮`
- `AllowsTransparency=True` + 弹出动画

### 信息同步机制（R8 修复）

```
AccountSettings 编辑 RealName/手机/邮箱
  → 保存成功
  → Toast 提示 "已更新" （新增反馈）
  → UserDetailDto 是共享引用（INotifyPropertyChanged）
  → 侧边栏 RealName 自动刷新
  → 手机/邮箱不在侧边栏常驻显示（避免拥挤）
  → hover 用户卡 tooltip 显示完整信息
```

**原则**：侧边栏保持**最小信息显示**（avatar + 姓名 + 角色），完整信息在账户页查看。

---

## [S5] 导航分组

### 模型变更

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Controls/Models/NavigationItem.cs
public class NavigationItem
{
    public string Title { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;
    public Geometry? IconData { get; set; }
    public bool IsVisible { get; set; } = true;
    public ICommand? Command { get; set; }

    // 新增字段
    public string Group { get; set; } = "业务";  // 可选: "主页" / "业务" / "管理"
}
```

### 分组渲染规则

```
┌─主页组(无标题)─────┐
│ 🏠 主页             │
└────────────────────┘
   ↕ 12px
┌─业务组─────────────┐
│ 业务                │   ← 11px SidebarSecondaryTextBrush
│ 👤 患者管理         │
│ 📋 医案管理         │
│ 🌿 经验方管理       │   ← 仅医生角色
│ 💊 药材管理         │
└────────────────────┘
   ↕ 12px
┌─管理组─────────────┐
│ 管理                │
│ 👥 用户管理         │   ← 仅管理员
│ 📊 数据同步         │
└────────────────────┘
```

**规则**：如果某组没有项（角色无权限），整组（含标题）不渲染 — `Visibility=Collapsed when Items.Count==0`。

### 渲染实现

3 个独立 `ItemsControl`，各自绑定到 `MainWindowViewModel` 暴露的 `HomeNavItems` / `BusinessNavItems` / `AdminNavItems` 集合（在 VM 中按 `Group` 字段筛选）。

> **替代方案**：使用 `CollectionViewSource` + `GroupStyle`，但需要更多 XAML 配置。3 个 `ItemsControl` 更直观、易维护，故选此。

---

## [S6] 状态条 — 始终可见

### 展开态（56px 双行）

```
┌─────────────────────┐
│ ● 在线 · 远程模式    │  ← API 圆点(12px) + 状态文字 + 模式徽章
│ 🕐 14:23 · 06-22    │  ← 时钟图标 + 时间 + 日期
└─────────────────────┘
```

### 折叠态（28px 单行）

```
┌─────────────────────┐
│ ●在线  🌐远程  🕐    │  ← 3 个图标水平排列，无文字
└─────────────────────┘
       ↑ 每个 icon 单独 tooltip 含完整说明
```

### 修复点（R6）

| 状态项 | 当前折叠态 | 新折叠态 |
|--------|-----------|---------|
| API | 圆点（保留） | 圆点（保留）+ tooltip |
| 模式 | **完全丢失** | 🌐 图标 + tooltip "远程模式" |
| 时钟 | **完全丢失** | 🕐 图标 + tooltip "14:23 06-22" |

---

## [S7] 折叠态信息保留矩阵

| 元素 | 展开态 | 折叠态 | 实现 |
|------|--------|--------|------|
| 品牌 logo | "凌隐宝堂" | 仅 ☰ 图标 | `BoolToVis` 文字 |
| 用户卡 | avatar+name+role | 仅 avatar | `BoolToVis` 文字区 |
| ⋯ 更多按钮 | 可见 | 隐藏 | `BoolToVis` |
| 导航项 | 图标+文字 | 仅图标居中 | `BoolToVis` 文字 |
| 分组标题 | "业务"/"管理" | 隐藏 | `BoolToVis` |
| 状态: API | 圆点+文字 | 仅圆点+tooltip | `BoolToVis` 文字 |
| 状态: 模式 | 徽章+文字 | 🌐+tooltip | `BoolToVis` 文字 |
| 状态: 时钟 | 时间+日期 | 🕐+tooltip | `BoolToVis` 文字 |

---

## [S8] 宽度动画规格

### 当前（R5）

```xaml
<!-- 瞬间跳变 -->
<Border Width="{Binding IsExpanded, Converter={StaticResource BoolToSidebarWidthConverter}}" />
```

### 新设计

```xaml
<!-- 双向动画 200ms CubicEase -->
<Border x:Name="RootBorder">
  <Border.Style>
    <Style TargetType="Border">
      <Setter Property="Width" Value="56" />
      <Style.Triggers>
        <DataTrigger Binding="{Binding IsExpanded, ElementName=Root}" Value="True">
          <DataTrigger.EnterActions>
            <BeginStoryboard>
              <Storyboard>
                <DoubleAnimation
                    Storyboard.TargetProperty="Width"
                    To="220"
                    Duration="0:0:0.2"
                    EasingFunction="{StaticResource CubicEase}" />
              </Storyboard>
            </BeginStoryboard>
          </DataTrigger.EnterActions>
          <DataTrigger.ExitActions>
            <BeginStoryboard>
              <Storyboard>
                <DoubleAnimation
                    Storyboard.TargetProperty="Width"
                    To="56"
                    Duration="0:0:0.2"
                    EasingFunction="{StaticResource CubicEase}" />
              </Storyboard>
            </BeginStoryboard>
          </DataTrigger.ExitActions>
        </DataTrigger>
      </Style.Triggers>
    </Style>
  </Border.Style>
  <!-- 内容 -->
</Border>
```

`CubicEase` 作为资源在 `App.xaml` 或 `SidebarControl.Resources` 中声明：

```xaml
<CubicEase x:Key="CubicEase" />
```

---

## [S9] 文件改动清单

| 文件 | 改动类型 | 复杂度 | 涉及 Root Cause |
|------|---------|--------|----------------|
| `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml` | 重写 ~80% | 高 | R1, R2, R4, R5, R6, R7 |
| `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs` | 删除 `OnUserAvatarClick` + `NavigateToSystemSettingsCommand` DP + `IsExpanded` 默认值改 true | 低 | R1, R3, R4 |
| `src/Client/Desktop/Core/LYBT.Desktop.Controls/Models/NavigationItem.cs` | 新增 `Group` 字段（默认"业务"） | 极低 | — |
| `src/Client/Desktop/Shell/Views/MainWindow.xaml` | 删除 `NavigateToSystemSettingsCommand` 绑定 | 极低 | R3 |
| `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` | `IsDrawerOpen` 默认 `true` + 拆分 NavigationItems 为 3 组 | 中 | R4 |
| `src/Client/Desktop/Shell/Controls/AccountSettingsControl.xaml.cs` | 保存成功后触发 Toast | 低 | R8 |

### 不改动

- 主题资源字典 `Themes/*.xaml`（约束：主题不大动）
- `AccountSettingsControl.xaml` 整体结构（已是合理左右分栏）
- 现有 `Converters`（全部复用）
- `MainWindow.xaml` 整体布局（仅改 SidebarControl 绑定）

---

## [S10] Bug 修复映射表

| 用户痛点 | Root Cause | 修复手段 | 验证方式 |
|---------|-----------|---------|---------|
| **交互不一致** | R1, R2, R3, R7 | 删除代码后置；用户卡主体绑 `EditProfileCommand`；删除死 DP；`NavigateToHomeCommand` 绑品牌行 | 点击用户卡直接跳账户设置；点击品牌 logo 回主页 |
| **展开/折叠 bug** | R4, R5, R6 | `IsExpanded` 默认 true；200ms CubicEase 动画；折叠态保留 3 个状态图标 + tooltip | 启动即展开；切换平滑无闪烁；折叠后 hover 有 tooltip |
| **信息不一致** | R8 | 保存成功 → Toast；共享 UserDetailDto 引用自动同步 RealName | 改 RealName → 侧边栏即时刷新；改手机 → Toast 反馈 |

---

## [S11] 测试覆盖

| 测试类型 | 覆盖点 |
|---------|--------|
| 单元测试 | `NavigationItem.Group` 默认值 = "业务"；`MainWindowViewModel.IsDrawerOpen` 初始 true；3 组 NavItems 正确分类 |
| UI 手工测试 | 4 角色登录分组正确；启动即展开；切换折叠动画顺滑；用户卡点击跳转；Popup 菜单工作；编辑后 RealName 同步；折叠态 hover tooltip |
| 回归测试 | `dotnet build LYBTZYZS.sln` 通过；现有 ~760 个 Desktop 测试不破坏 |

---

## [S12] 实现顺序（compose:plan 将细化）

```
Step 1. NavigationItem.cs 加 Group 字段（向后兼容，默认"业务"）
Step 2. MainWindowViewModel: IsDrawerOpen 默认 true + NavItems 按 Group 拆分
Step 3. SidebarControl.xaml 全面重写（双卡片 + 分组 + Popup + 动画）
Step 4. SidebarControl.xaml.cs: 删除 OnUserAvatarClick + 死 DP + IsExpanded 默认值
Step 5. MainWindow.xaml: 删除 NavigateToSystemSettingsCommand 绑定
Step 6. AccountSettingsControl.xaml.cs: 保存成功后触发 Toast
Step 7. dotnet build + 手工 UI 验证
Step 8. compose:review 子代理审查 diff
```

---

## 变更记录

| 日期 | 变更 | 作者 |
|------|------|------|
| 2026-06-22 | 初始版本（brainstorm 完成） | MiMoCode Compose |
