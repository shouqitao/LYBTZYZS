# Sysadmin Dashboard v3 设计 Spec

> 日期: 2026-06-27 | 状态: 待审批

---

## [S1] 问题陈述

当前 Sysadmin Dashboard 存在三类问题：

### 1.1 运行时绑定错误

`DashboardStatus` 模型已删除 `ApiStatus` 和 `ConnectionMode` 属性，但 `SysadminHomeView.xaml` 仍绑定这两个属性 → 运行时静默失败，卡片显示空白。

### 1.2 导航入口分散

Dashboard 内容区有 3 个导航按钮（管理员账号管理、诊所信息配置、日志级别控制）。用户要求统一到侧边栏。

### 1.3 侧边栏导航不完整

`BuildNavigationItems` 只给 SuperAdmin 加了"主页"+"用户管理"。缺少：
- 管理员账号管理（AdminUserManagementView）
- 诊所信息配置（SystemSettingsView）
- 日志级别控制（LogLevelControlView）

### 1.4 冗余信息

API 状态和连接模式已在底部状态栏显示，首页不应重复。

---

## [S2] 设计原则

**MDIX 原生优先，不依赖旧自定义样式。**

Sysadmin 是 MDIX 重构的起点。Admin/Clinical 的 `FunctionCardStyle`、`CardIconStyle` 等是旧代码遗留，不作为参考。本设计全部使用 MDIX 内置组件和样式。

### MDIX 原生组件选择

| 用途 | MDIX 组件/样式 | 说明 |
|------|---------------|------|
| 卡片容器 | `materialDesign:Card` | 自带阴影、圆角、padding |
| 阴影深度 | `ElevationAssist.Elevation="Dp4"` | MDIX 阴影分级 |
| 圆角 | `UniformCornerRadius="16"` | MDIX Card 原生属性 |
| 卡片标题 | `MaterialDesignHeadline6TextBlock` | MDIX 内置文字样式 |
| 卡片正文 | `MaterialDesignBody2TextBlock` | MDIX 内置文字样式 |
| 图标 | `materialDesign:PackIcon` | MDIX 矢量图标库 |
| 按钮 | `MaterialDesignFlatButton` | MDIX 扁平按钮 |
| 页面背景 | `MaterialDesign.Brush.Background` | MDIX 主题感知背景 |
| 前景文字 | `MaterialDesign.Brush.Foreground` | MDIX 主题感知文字 |

### 其他约束

- 不修改工具栏和状态栏的现有设计
- 颜色用 `DynamicResource`，确保 Dark 主题适配
- 不引入 `FunctionCardStyle`、`HomePageStyles.xaml` 等旧自定义样式

---

## [S3] 设计方案

### 3.1 Dashboard 内容区重新设计

**移除：**
- API 状态卡片（状态栏已有）
- 连接模式卡片（状态栏已有）
- 3 个导航按钮（移到侧边栏）

**保留并升级：**
- 数据库状态卡片 — 使用 `FunctionCardStyle` + MDIX PackIcon
- 系统信息卡片 — 使用 `FunctionCardStyle` + MDIX PackIcon

**完整 XAML（MDIX 原生组件）：**

```xml
<UserControl x:Class="LYBT.Desktop.Sysadmin.Views.SysadminHomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             prism:ViewModelLocator.AutoWireViewModel="True"
             xmlns:prism="http://prismlibrary.com/">

    <materialDesign:DialogHost>
        <ScrollViewer VerticalScrollBarVisibility="Auto"
                      Background="{DynamicResource MaterialDesign.Brush.Background}">
            <StackPanel Margin="32">

                <!-- 标题区 -->
                <TextBlock Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                           Text="运维控制台" Margin="0,0,0,4" />
                <TextBlock Style="{StaticResource MaterialDesignBody2TextBlock}"
                           Text="凌隐宝堂中医诊所 · 系统运维"
                           Opacity="0.6" Margin="0,0,0,32" />

                <!-- 状态卡片 -->
                <UniformGrid Columns="2">

                    <!-- 数据库状态卡片 -->
                    <materialDesign:Card Margin="8" Padding="24"
                                         UniformCornerRadius="16"
                                         materialDesign:ElevationAssist.Elevation="Dp2">
                        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                            <materialDesign:PackIcon Kind="Database"
                                                      Width="48" Height="48"
                                                      HorizontalAlignment="Center"
                                                      Margin="0,0,0,16"
                                                      Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                            <TextBlock Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                                       Text="数据库"
                                       HorizontalAlignment="Center" />
                            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,8,0,0">
                                <materialDesign:PackIcon Kind="CheckCircle"
                                                          Width="16" Height="16"
                                                          Margin="0,0,4,0"
                                                          Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                                <TextBlock Style="{StaticResource MaterialDesignBody2TextBlock}"
                                           Text="{Binding Dashboard.DbStatus.Value}" />
                            </StackPanel>
                        </StackPanel>
                    </materialDesign:Card>

                    <!-- 系统信息卡片 -->
                    <materialDesign:Card Margin="8" Padding="24"
                                         UniformCornerRadius="16"
                                         materialDesign:ElevationAssist.Elevation="Dp2">
                        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                            <materialDesign:PackIcon Kind="InformationOutline"
                                                      Width="48" Height="48"
                                                      HorizontalAlignment="Center"
                                                      Margin="0,0,0,16"
                                                      Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                            <TextBlock Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                                       Text="系统信息"
                                       HorizontalAlignment="Center" />
                            <TextBlock Style="{StaticResource MaterialDesignBody2TextBlock}"
                                       Text="{Binding Dashboard.SystemInfo.Value}"
                                       HorizontalAlignment="Center" Margin="0,8,0,0" />
                            <TextBlock Style="{StaticResource MaterialDesignBody2TextBlock}"
                                       Text="{Binding Dashboard.SystemInfo.Status}"
                                       HorizontalAlignment="Center" Margin="0,4,0,0"
                                       Opacity="0.6" TextTrimming="CharacterEllipsis" />
                        </StackPanel>
                    </materialDesign:Card>

                </UniformGrid>
            </StackPanel>
        </ScrollViewer>
    </materialDesign:DialogHost>
</UserControl>
```

**与旧设计的核心区别：**

| 维度 | 旧设计（Admin/Clinical 参考） | 新设计（MDIX 原生） |
|------|---------------------------|-------------------|
| 卡片容器 | `Border` + `FunctionCardStyle` | `materialDesign:Card` |
| 阴影 | `DropShadowEffect` 硬编码 | `ElevationAssist.Elevation="Dp2"` |
| 圆角 | `CornerRadius="12"` | `UniformCornerRadius="16"` |
| 图标 | `Path` + `CardIconStyle` | `materialDesign:PackIcon` |
| 标题 | `CardTitleStyle` (自定义) | `MaterialDesignHeadline6TextBlock` |
| 正文 | 硬编码 FontSize | `MaterialDesignBody2TextBlock` |
| 背景 | `MaterialDesignBody` | `MaterialDesign.Brush.Background` |
| 文字 | `MaterialDesignBody` | `MaterialDesign.Brush.Foreground` |
| 资源依赖 | 需要 HomePageStyles.xaml | **零额外依赖** |

### 3.2 侧边栏导航项更新

在 `BuildNavigationItems` 中为 SuperAdmin 角色添加：

| 导航项 | ViewName | 图标 | 分组 |
|--------|----------|------|------|
| 主页 | SysadminHome | Home | 主页 |
| 管理员账号 | AdminUserManagementView | AccountTie | 管理 |
| 诊所信息 | SystemSettingsView | Domain | 管理 |
| 日志控制 | LogLevelControlView | Tune | 管理 |

### 3.3 ViewModel 清理

**移除：**
- `NavigateToAdminUsersCommand`
- `NavigateToClinicSettingsCommand`
- `NavigateToLogLevelCommand`
- `IConnectionModeService` 依赖（不再需要）
- `IConnectionSettingsService` 依赖（不再需要）
- `INavigationCoordinator` 依赖（不再需要）

**保留：**
- `IAuthApi` — 数据库健康检查
- `IClinicSettingsService` — 诊所名称显示
- `DashboardStatus` — DbStatus + SystemInfo

---

## [S4] 文件变更

| 操作 | 文件 | 变更 |
|------|------|------|
| Modify | `SysadminHomeView.xaml` | 重写：2 张 FunctionCard，移除导航按钮 |
| Modify | `SysadminHomeViewModel.cs` | 移除导航命令和多余依赖 |
| Modify | `MainWindowViewModel.cs` BuildNavigationItems | 添加 SuperAdmin 导航项 |
| Modify | `SysadminModule.cs` | 确认 SystemSettingsView 和 LogLevelControlView 已注册 |

---

## [S5] 验收标准

- [ ] XAML 无运行时绑定错误
- [ ] Dashboard 只显示数据库状态和系统信息
- [ ] 侧边栏有 4 个导航项（主页、管理员账号、诊所信息、日志控制）
- [ ] 导航功能正常（点击侧边栏可跳转）
- [ ] `dotnet build` 通过
- [ ] Dark/Light 主题切换正常

---

## [S6] 变更日志

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-27 | v3.0 | 基于代码现状重新设计，修复绑定错误，导航统一到侧边栏 |
