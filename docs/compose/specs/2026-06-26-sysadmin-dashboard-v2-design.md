# Sysadmin Dashboard 设计完善 Spec

> 版本: v2.0 | 日期: 2026-06-26

---

## [S1] 问题陈述

Sysadmin Dashboard 当前存在以下设计问题：

1. **信息冗余** — API 状态和连接模式已在底部状态栏显示，首页重复展示
2. **导航入口分散** — 快捷操作按钮放在首页内容区，应统一到左侧工具栏
3. **卡片样式平淡** — 缺少阴影、悬停效果、视觉层次
4. **无状态指示器** — 缺少颜色编码的状态标识

---

## [S2] 设计约束

- 工具栏设计已确定，不改动
- 状态栏设计已确定，不改动
- 只修改主页内容区域（ContentRegion）
- 使用现有棕色主色调（PrimaryColor = #5D4037）
- 支持 Dark/Light 主题切换

---

## [S3] Dashboard 重新设计

### 3.1 信息架构

**移除冗余信息：**
- ❌ API 状态卡片 — 状态栏已有 `ApiStatusIcon` + `ApiStatusColor`
- ❌ 连接模式卡片 — 状态栏已有 `ConnectionModeDisplay`
- ❌ 快捷操作按钮 — 应统一到工具栏导航

**保留核心信息：**
- ✅ 数据库状态 — 状态栏无此信息，sysadmin 需要监控
- ✅ 系统信息 — 版本号 + 诊所名称，便于问题追踪

**新增系统概览：**
- 📊 用户统计 — 总用户数、各角色分布
- ⏱️ 系统运行 — 启动时间、运行时长

### 3.2 布局设计

```
┌─────────────────────────────────────────────────────────────┐
│                    运维控制台                                 │
│              凌隐宝堂中医诊所 · 系统运维                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  系统状态                                            │   │
│  │  ┌──────────────┐  ┌──────────────┐                 │   │
│  │  │ 🗄️ 数据库     │  │ ℹ️ 系统信息   │                 │   │
│  │  │   连接正常    │  │   v1.0.0     │                 │   │
│  │  │   ● 正常      │  │   诊所名称    │                 │   │
│  │  └──────────────┘  └──────────────┘                 │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  系统概览                                            │   │
│  │  ┌──────────────┐  ┌──────────────┐                 │   │
│  │  │ 👥 用户统计   │  │ ⏱️ 运行状态   │                 │   │
│  │  │   5 个用户    │  │   运行 2 天   │                 │   │
│  │  │   Admin: 1    │  │   启动: 08:00 │                 │   │
│  │  └──────────────┘  └──────────────┘                 │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 卡片样式

使用 `FunctionCardStyle` 风格（与 Admin Dashboard 一致）：
- 背景：`MaterialDesignBody`
- 圆角：12px
- 阴影：DropShadowEffect (BlurRadius=8, Opacity=0.1)
- 悬停：边框变棕色，阴影加深

### 3.4 状态指示器

- 🟢 绿色圆点：数据库连接正常
- 🔴 红色圆点：数据库连接异常
- 使用 `OpsSuccessBrush` / `OpsDangerBrush`

### 3.5 数据源

| 卡片 | 数据来源 | 说明 |
|------|---------|------|
| 数据库 | `IAuthApi.HealthCheckAsync()` | API 在线 = DB 可用 |
| 系统信息 | `SystemConstants.ApplicationVersion` + `IClinicSettingsService.ClinicName` | 版本 + 诊所名 |
| 用户统计 | `IUserApi.GetUsersAsync()` | 分页查询统计 |
| 运行状态 | `Environment.TickCount64` | 计算运行时长 |

---

## [S4] ViewModel 变更

### 4.1 SysadminHomeViewModel

**移除：**
- `NavigateToAdminUsersCommand` — 移到工具栏
- `NavigateToClinicSettingsCommand` — 移到工具栏
- `NavigateToLogLevelCommand` — 移到工具栏

**新增属性：**
- `UserCount` — 总用户数
- `AdminCount` — Admin 用户数
- `DoctorCount` — Doctor 用户数
- `ReceptionistCount` — Receptionist 用户数
- `UptimeDisplay` — 运行时长显示

**新增方法：**
- `LoadUserStatisticsAsync()` — 加载用户统计
- `CalculateUptime()` — 计算运行时长

---

## [S5] XAML 变更

### 5.1 SysadminHomeView.xaml

**结构变更：**
- 移除导航按钮区域（`UniformGrid Columns="2"`）
- 保留系统状态卡片（2 张）
- 新增系统概览卡片（2 张）
- 使用 `FunctionCardStyle` 替代纯色背景

**关键代码：**

```xml
<!-- 系统状态区域 -->
<UniformGrid Columns="2" Margin="0,0,0,24">
    <!-- 数据库卡片 -->
    <Border Style="{StaticResource FunctionCardStyle}" Margin="12">
        <StackPanel>
            <Path Style="{StaticResource CardIconStyle}" Data="{DynamicResource IconDatabase}" />
            <TextBlock Style="{StaticResource CardTitleStyle}" Text="数据库" />
            <StackPanel Orientation="Horizontal">
                <Ellipse Width="10" Height="10" Fill="{StaticResource OpsSuccessBrush}" />
                <TextBlock Text="连接正常" Margin="8,0,0,0" />
            </StackPanel>
        </StackPanel>
    </Border>
    
    <!-- 系统信息卡片 -->
    <Border Style="{StaticResource FunctionCardStyle}" Margin="12">
        <StackPanel>
            <Path Style="{StaticResource CardIconStyle}" Data="{DynamicResource IconInfo}" />
            <TextBlock Style="{StaticResource CardTitleStyle}" Text="系统信息" />
            <TextBlock Text="{Binding Dashboard.SystemInfo.Value}" />
            <TextBlock Text="{Binding Dashboard.SystemInfo.Status}" />
        </StackPanel>
    </Border>
</UniformGrid>

<!-- 系统概览区域 -->
<UniformGrid Columns="2">
    <!-- 用户统计卡片 -->
    <Border Style="{StaticResource FunctionCardStyle}" Margin="12">
        <StackPanel>
            <Path Style="{StaticResource CardIconStyle}" Data="{DynamicResource IconUsers}" />
            <TextBlock Style="{StaticResource CardTitleStyle}" Text="用户统计" />
            <TextBlock Text="{Binding UserCount, StringFormat='总计 {0} 个用户'}" />
            <TextBlock Text="{Binding AdminCount, StringFormat='Admin: {0}'}" />
        </StackPanel>
    </Border>
    
    <!-- 运行状态卡片 -->
    <Border Style="{StaticResource FunctionCardStyle}" Margin="12">
        <StackPanel>
            <Path Style="{StaticResource CardIconStyle}" Data="{DynamicResource IconClock}" />
            <TextBlock Style="{StaticResource CardTitleStyle}" Text="运行状态" />
            <TextBlock Text="{Binding UptimeDisplay}" />
        </StackPanel>
    </Border>
</UniformGrid>
```

---

## [S6] 工具栏导航变更

### 6.1 导航菜单更新

在工具栏导航菜单中添加 Sysadmin 专属入口：
- 主页（已有）
- 用户管理（已有）
- 日志级别控制（新增）

### 6.2 导航参数

用户管理导航传递 `DefaultRoleFilter = UserRole.Admin` 参数。

---

## [S7] 实施计划

| Task | 内容 | 文件 |
|------|------|------|
| 1 | 更新 DashboardStatus 模型 | `DashboardStatus.cs` |
| 2 | 更新 ViewModel | `SysadminHomeViewModel.cs` |
| 3 | 更新 XAML | `SysadminHomeView.xaml` |
| 4 | 更新工具栏导航 | 工具栏相关文件 |
| 5 | 验证编译 | - |

---

## [S8] 变更日志

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-26 | v2.0 | 重新设计，移除冗余信息，统一导航到工具栏 |
