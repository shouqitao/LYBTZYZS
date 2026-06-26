# Sysadmin 功能完善设计文档

> 版本: v1.0 | 日期: 2026-06-24 | 状态: 待审批

---

## [S1] 问题陈述

### 1.1 现状

Sysadmin 模块（`LYBT.Desktop.Sysadmin`）已实现基础框架：
- **Dashboard**: 4 张状态卡片（2x2 网格）+ 2 个导航按钮 + 30 秒轮询
- **用户管理**: 嵌入 `UserMasterDetailControl`，无角色过滤
- **日志控制**: 运行时日志级别调整（已完善）

### 1.2 问题

| 组件 | 问题 | 严重度 |
|------|------|--------|
| 数据库状态卡片 | 硬编码 "连接正常"，未实际检测 | **高** |
| 今日登录卡片 | 显示 "--"，功能未实现 | **中** |
| 系统信息卡片 | 只显示版本号，信息不足 | **低** |
| 用户管理 | 显示所有用户，未按角色过滤 | **中** |
| sysadmin 账号 | 无专属保护逻辑 | **低** |

---

## [S2] 设计目标

### 2.1 Dashboard 目标

**核心原则**: 为 sysadmin 提供真实、有价值的系统状态信息。

| 目标 | 描述 | 验收标准 |
|------|------|---------|
| 真实数据 | 消除所有占位符和硬编码 | 每张卡片显示实时真实数据 |
| 连接感知 | 显示当前连接模式和 URL | sysadmin 一眼可知本地/远程模式 |
| 诊所配置 | 导航按钮跳转到现有 SystemSettingsView | 一键进入诊所配置页面 |
| 最小改动 | 保持 2x2 布局 + 诊所信息面板 | 不改变现有视觉结构 |

### 2.2 用户管理目标

| 目标 | 描述 | 验收标准 |
|------|------|---------|
| 角色过滤 | 默认只显示 Admin 角色用户 | 首次加载时 RoleFilter = Admin |
| sysadmin 保护 | sysadmin 账号不可编辑/删除 | 操作按钮对 sysadmin 禁用 |
| 密码重置 | sysadmin 可重置任何 Admin 密码 | 重置密码按钮始终可用 |

---

## [S3] Dashboard 详细设计

### 3.0 主题一致性

**现有主题体系：**
- 主应用：Material Design In Xaml (MDIX) 主题（Brown 主色，Amber 强调色）
- 主题切换：`ThemeService` 支持 Light/Dark 模式切换（通过 MDIX `PaletteHelper`）

**设计原则：**
1. Sysadmin 模块使用 MDIX Dark 主题（通过 `ThemeService.ApplyTheme(true)` 切换）
2. `SysadminDarkTheme.xaml` 仅保留 Ops* 语义别名，映射到 MDIX 资源键
3. 卡片使用 `DynamicResource MaterialDesignBody` / `MaterialDesignPaper` 等 MDIX 资源
4. 按钮使用 MDIX 内置样式（`MaterialDesignFlatMidBgButton` / `MaterialDesignFlatButton`）
5. 状态颜色使用 `OpsSuccessBrush` / `OpsDangerBrush` / `OpsWarningBrush`（映射到 MDIX）

**MDIX 资源映射：**
| Ops* 别名 | MDIX 资源键 |
|-----------|------------|
| OpsBackgroundBrush | `MaterialDesignPaper` |
| OpsCardBackgroundBrush | `MaterialDesignBody` |
| OpsAccentBrush | `MaterialDesignFlatButton.ClickThrough` |
| OpsTextPrimaryBrush | `MaterialDesignBody` |
| OpsTextSecondaryBrush | `MaterialDesignBodyLight` |
| OpsSuccessBrush | `MaterialDesignBrush.Success` |
| OpsDangerBrush | `MaterialDesignBrush.Error` |
| OpsWarningBrush | `MaterialDesignBrush.Warning` |

### 3.1 卡片重新定义

| 位置 | 当前 | 改为 | 数据来源 |
|------|------|------|---------|
| 左上 | API 状态 | **API 状态** | `IAuthApi.HealthCheckAsync()` |
| 右上 | 数据库 | **连接模式** | `IConnectionModeService.CurrentModeDisplay` |
| 左下 | 今日登录 | **数据库状态** | API 健康检查（间接判断） |
| 右下 | 系统信息 | **系统信息** | 版本 + 诊所名称 |

> ⚠️ **版本号统一**: 当前版本号定义分散在多个位置，需要统一为单一来源。

**当前混乱状态：**
| 位置 | 当前值 | 问题 |
|------|--------|------|
| `SystemConstants.ApplicationVersion` | "2.0.0" | Desktop 客户端硬编码 |
| `Directory.Build.props` | "2.1.0" | MSBuild 构建版本 |
| `LocalWebAPI/HealthController` | "1.0.0-local" | 代码中硬编码 |

**统一方案：**
1. `Directory.Build.props` 作为单一来源（VersionPrefix = "1.0.0"）
2. `SystemConstants.ApplicationVersion` 改为运行时从程序集读取
3. 移除所有硬编码版本号

### 3.2 数据源映射

#### 卡片 1: API 状态（保持不变）

```
数据源: IAuthApi.HealthCheckAsync()
显示逻辑:
  - 成功 → Value="在线", IsHealthy=true, Status="正常"
  - 失败 → Value="离线", IsHealthy=false, Status="异常"
  - 异常 → Value="不可达", IsHealthy=false
轮询间隔: 30 秒
```

#### 卡片 2: 连接模式（新增）

```
数据源: IConnectionModeService.CurrentModeDisplay
显示逻辑:
  - Remote → Value="远程模式"
  - Local → Value="本地模式"
  - IsHealthy = true（始终健康，只是模式不同）
额外信息: 可在 Status 字段显示连接 URL（可选）
```

#### 卡片 3: 数据库状态（替换今日登录）

```
数据源: IAuthApi.HealthCheckAsync() 的间接判断
显示逻辑:
  - API 在线 → Value="连接正常", IsHealthy=true
  - API 离线 → Value="连接异常", IsHealthy=false
理由: API 依赖数据库，API 可用 = 数据库可用
```

#### 卡片 4: 系统信息（增强）

```
数据源: 
  - SystemConstants.ApplicationVersion → 版本号
  - IConnectionSettingsService.CurrentUrl → 连接 URL
  - IClinicSettingsService.ClinicName → 诊所名称
显示逻辑:
  - Value = "v1.0.0"（版本号，修复后）
  - Status = "凌隐宝堂中医诊所"（诊所名称）
  - 可选: 鼠标悬停显示完整信息（地址、电话等）
```

### 3.3 代码变更

#### DashboardStatus.cs

```csharp
// 变更: 重命名 LoginCount → ConnectionMode
// 变更: 修改默认 Title
// 变更: 使用 DesignSystem.xaml 中的间距令牌

[ObservableProperty]
private StatusCard _connectionMode = new() { Title = "连接模式", Value = "检测中..." };

[ObservableProperty]
private StatusCard _dbStatus = new() { Title = "数据库", Value = "检测中..." };

[ObservableProperty]
private StatusCard _systemInfo = new() { Title = "系统信息", Value = "加载中..." };
```

#### SysadminHomeViewModel.cs

```csharp
// 新增依赖
private readonly IConnectionModeService _connectionModeService;
private readonly IConnectionSettingsService _connectionSettings;
private readonly IClinicSettingsService _clinicSettings;

// 构造函数新增参数
public SysadminHomeViewModel(
    IViewModelServices services,
    IAuthApi authApi,
    INavigationCoordinator navigationCoordinator,
    IConnectionModeService connectionModeService,
    IConnectionSettingsService connectionSettings,
    IClinicSettingsService clinicSettings)

// PollDashboardAsync 修改
private async Task PollDashboardAsync(CancellationToken ct)
{
    // ... 现有代码 ...
    
    // 卡片 1: API 状态（保持不变）
    // 卡片 2: 连接模式
    Dashboard.ConnectionMode.Value = _connectionModeService.CurrentModeDisplay;
    Dashboard.ConnectionMode.IsHealthy = true;
    Dashboard.ConnectionMode.Status = _connectionSettings.CurrentUrl;
    
    // 卡片 3: 数据库状态（通过 API 间接判断）
    if (healthResp.Success)
    {
        Dashboard.DbStatus.Value = "连接正常";
        Dashboard.DbStatus.IsHealthy = true;
    }
    else
    {
        Dashboard.DbStatus.Value = "连接异常";
        Dashboard.DbStatus.IsHealthy = false;
    }
    
    // 卡片 4: 系统信息
    Dashboard.SystemInfo.Value = $"v{SystemConstants.ApplicationVersion}";
    Dashboard.SystemInfo.Status = _clinicSettings.ClinicName;
}
```

#### SysadminHomeView.xaml

```xml
<!-- 变更: 绑定路径更新 -->
<!-- 使用 MDIX DynamicResource 确保主题切换时自动更新 -->
<!-- 添加 materialDesign:DialogHost 包装，支持 MDIX 主题 -->
<!-- 按钮使用 MDIX 内置样式 -->

<materialDesign:DialogHost>
    <ScrollViewer VerticalScrollBarVisibility="Auto" Background="{DynamicResource MaterialDesignPaper}">
        <StackPanel Margin="24">
            <!-- 卡片 2: 连接模式 -->
            <Border Background="{DynamicResource MaterialDesignBody}"
                    CornerRadius="{StaticResource RadiusMD}" Padding="{StaticResource SpacingXL}" Margin="{StaticResource SpacingSM}">
                <StackPanel>
                    <TextBlock Text="{Binding Dashboard.ConnectionMode.Title}" 
                               Foreground="{DynamicResource MaterialDesignBodyLight}" FontSize="13" />
                    <TextBlock Text="{Binding Dashboard.ConnectionMode.Value}" 
                               Foreground="{DynamicResource MaterialDesignBody}" FontSize="28" FontWeight="Bold" 
                               Margin="0,8,0,0" />
                    <TextBlock Text="{Binding Dashboard.ConnectionMode.Status}" 
                               Foreground="{DynamicResource MaterialDesignBodyLight}" FontSize="11" 
                               Margin="0,4,0,0" TextTrimming="CharacterEllipsis" />
                </StackPanel>
            </Border>

            <!-- 卡片 3: 数据库 -->
            <Border Background="{DynamicResource MaterialDesignBody}"
                    CornerRadius="{StaticResource RadiusMD}" Padding="{StaticResource SpacingXL}" Margin="{StaticResource SpacingSM}">
                <StackPanel>
                    <TextBlock Text="{Binding Dashboard.DbStatus.Title}" 
                               Foreground="{DynamicResource MaterialDesignBodyLight}" FontSize="13" />
                    <TextBlock Text="{Binding Dashboard.DbStatus.Value}" 
                               Foreground="{DynamicResource MaterialDesignBody}" FontSize="28" FontWeight="Bold" 
                               Margin="0,8,0,0" />
                    <Ellipse Width="10" Height="10" Margin="0,8,0,0"
                             Fill="{StaticResource OpsSuccessBrush}" 
                             Visibility="{Binding Dashboard.DbStatus.IsHealthy, Converter={x:Static converters:Cvt.BoolToVis}}" />
                </StackPanel>
            </Border>

            <!-- 导航按钮 - 使用 MDIX 样式 -->
            <UniformGrid Columns="2" Margin="0,24,0,0">
                <Button Content="管理员账号管理" Command="{Binding NavigateToAdminUsersCommand}"
                        Style="{StaticResource MaterialDesignFlatMidBgButton}"
                        Margin="{StaticResource SpacingSM}" Padding="{StaticResource SpacingMD}" />
                <Button Content="诊所信息配置" Command="{Binding NavigateToClinicSettingsCommand}"
                        Style="{StaticResource MaterialDesignFlatMidBgButton}"
                        Margin="{StaticResource SpacingSM}" Padding="{StaticResource SpacingMD}" />
                <Button Content="日志级别控制" Command="{Binding NavigateToLogLevelCommand}"
                        Style="{StaticResource MaterialDesignFlatMidBgButton}"
                        Margin="{StaticResource SpacingSM}" Padding="{StaticResource SpacingMD}" />
            </UniformGrid>
        </StackPanel>
    </ScrollViewer>
</materialDesign:DialogHost>
```

---

### 3.3 诊所信息配置（复用现有）

#### 现有实现

Admin 角色已有完整的诊所信息配置：

| 文件 | 内容 |
|------|------|
| `SystemSettingsView.xaml` | 诊所信息配置 UI（诊所名称、科室、地址、电话、邮箱、许可证号） |
| `SystemSettingsViewModel.cs` | 配置逻辑（LoadClinicSettings、SaveAsync、Reset） |
| `IClinicSettingsService` | 数据服务（GetSettings、SaveSettingsAsync） |

#### Sysadmin 复用方案

Sysadmin Dashboard 导航按钮直接跳转到 `SystemSettingsView`：

```csharp
[RelayCommand]
private void NavigateToClinicSettings() => _navigationCoordinator.NavigateTo("SystemSettingsView");
```

**优势：**
- 零代码重复
- 统一的配置管理入口
- Admin 和 Sysadmin 共享同一套实现

---

## [S4] 用户管理详细设计

### 4.1 角色过滤

**方案**: 在 `AdminUserManagementView` 加载时，通过导航参数传递默认过滤值。

#### 实现方式

1. **SysadminHomeViewModel** 导航时传递参数：

```csharp
[RelayCommand]
private void NavigateToAdminUsers()
{
    var parameters = new NavigationParameters
    {
        { "DefaultRoleFilter", UserRole.Admin }
    };
    _navigationCoordinator.NavigateTo("AdminUserManagementView", parameters);
}
```

2. **AdminUserManagementView** 接收参数并设置过滤：

```csharp
// AdminUserManagementView.xaml.cs
public void OnNavigatedTo(NavigationContext navigationContext)
{
    if (navigationContext.Parameters.ContainsKey("DefaultRoleFilter"))
    {
        var role = (UserRole)navigationContext.Parameters["DefaultRoleFilter"];
        // 设置 UserMasterDetailControl 的默认过滤
        // 需要在 UserMasterDetailControl 上暴露 SetDefaultFilter 方法
    }
}
```

3. **UserMasterDetailControl** 新增方法：

```csharp
// UserMasterDetailControl.xaml.cs
public void SetDefaultRoleFilter(UserRole role)
{
    if (DataContext is UserMasterDetailViewModel vm)
    {
        vm.SelectedRoleFilter = role;
    }
}
```

### 4.2 sysadmin 账号保护

**方案**: 在 `UserMasterDetailViewModel` 中增加 sysadmin 检查逻辑。

#### 检查点

| 操作 | 检查逻辑 | 行为 |
|------|---------|------|
| 编辑 | `item.UserName == "sysadmin"` | 禁用编辑按钮 |
| 删除 | `item.UserName == "sysadmin"` | 禁用删除按钮 |
| 重置密码 | 无限制 | 始终可用 |
| 切换状态 | `item.UserName == "sysadmin"` | 禁用状态切换 |

#### 实现位置

在 `UserMasterDetailViewModel` 的 `CanExecute` 方法中增加检查：

```csharp
private bool CanEdit() => SelectedItem != null && 
    SelectedItem.UserName != "sysadmin" && !IsBusy;

private bool CanDelete() => SelectedItem != null && 
    SelectedItem.UserName != "sysadmin" && !IsBusy;

private bool CanToggleUserStatus() => 
    _statusHandler.CanToggleUserStatus(SelectedItem, IsBusy) &&
    SelectedItem?.UserName != "sysadmin";
```

### 4.3 视觉提示

在 `UserMasterDetailControl` 的列表中，对 sysadmin 行显示特殊标记：

```xml
<!-- 在用户列表项模板中 -->
<StackPanel Orientation="Horizontal">
    <TextBlock Text="{Binding UserName}" />
    <TextBlock Text=" (系统管理员)" 
               Foreground="Gray" 
               Visibility="{Binding UserName, Converter={StaticResource SysadminVisibilityConverter}}" />
</StackPanel>
```

---

## [S5] DI 注册变更

### 5.1 SysadminModule.cs

```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 现有注册保持不变
    containerRegistry.Register<ViewModels.SysadminHomeViewModel>();
    containerRegistry.Register<ViewModels.LogLevelControlViewModel>();
    
    // 以下服务已在 Shell DI 扩展中注册，无需重复：
    // - IConnectionModeService (DataSourceRegistrationExtensions.cs)
    // - IConnectionSettingsService (ServiceCollectionExtensions.cs)
    // - IClinicSettingsService (ServiceCollectionExtensions.cs)
}
```

### 5.2 DI 注册顺序

`SysadminHomeViewModel` 构造函数参数：
1. `IViewModelServices` - 基类必需
2. `IAuthApi` - 健康检查
3. `INavigationCoordinator` - 导航
4. `IConnectionModeService` - 连接模式（已在 Shell DI 扩展注册）
5. `IConnectionSettingsService` - 连接设置（已在 Shell DI 扩展注册）
6. `IClinicSettingsService` - 诊所信息（已在 Shell DI 扩展注册）

---

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">

    <!-- 运维控制台暗色主题 - 使用 MDIX Dark 主题资源 -->
    <!-- 全局主题已切换为 Dark（通过 ThemeService），此处定义 Ops* 语义别名 -->

    <!-- Ops* 语义别名 → MDIX 暗色主题资源 -->
    <SolidColorBrush x:Key="OpsBackgroundBrush" Color="{DynamicResource MaterialDesignPaper}" />
    <SolidColorBrush x:Key="OpsCardBackgroundBrush" Color="{DynamicResource MaterialDesignBody}" />
    <SolidColorBrush x:Key="OpsAccentBrush" Color="{DynamicResource MaterialDesignFlatButton.ClickThrough}" />
    <SolidColorBrush x:Key="OpsTextPrimaryBrush" Color="{DynamicResource MaterialDesignBody}" />
    <SolidColorBrush x:Key="OpsTextSecondaryBrush" Color="{DynamicResource MaterialDesignBodyLight}" />
    <SolidColorBrush x:Key="OpsSuccessBrush" Color="{DynamicResource MaterialDesignBrush.Success}" />
    <SolidColorBrush x:Key="OpsDangerBrush" Color="{DynamicResource MaterialDesignBrush.Error}" />
    <SolidColorBrush x:Key="OpsWarningBrush" Color="{DynamicResource MaterialDesignBrush.Warning}" />

</ResourceDictionary>
```



---

## [S6] 测试策略

### 6.1 单元测试

| 测试类 | 测试用例 |
|--------|---------|
| `SysadminHomeViewModelTests` | `PollDashboardAsync_UpdatesConnectionModeCard` |
| `SysadminHomeViewModelTests` | `PollDashboardAsync_UpdatesDbStatusFromHealthCheck` |
| `SysadminHomeViewModelTests` | `PollDashboardAsync_ShowsConnectionUrlInSystemInfo` |

### 6.2 集成测试

| 测试场景 | 验证点 |
|---------|--------|
| Dashboard 加载 | 4 张卡片全部显示真实数据 |
| 连接模式切换 | 卡片实时更新 |
| API 离线 | 数据库卡片显示异常 |

---

## [S7] 实施计划

### Phase 0: 版本号统一（预计 1 小时）

1. 修改 `Directory.Build.props` - 设置 `VersionPrefix = "1.0.0"`
2. 修改 `SystemConstants.cs` - 改为运行时读取程序集版本
3. 修改 `LocalWebAPI/HealthController.cs` - 移除硬编码版本号
4. 修改 `SystemConfigurationService.cs` - 从程序集读取版本
5. 验证所有位置显示一致的版本号

### Phase 1: Dashboard 修复（预计 2.5 小时）

1. 修改 `DashboardStatus.cs` - 重命名 LoginCount → ConnectionMode
2. 修改 `SysadminHomeViewModel.cs` - 注入新服务，更新轮询逻辑，添加诊所配置导航
3. 修改 `SysadminHomeView.xaml` - 更新绑定路径，添加诊所配置导航按钮
4. 修改 `SysadminModule.cs` - 确认 DI 注册

### Phase 1.5: 诊所信息配置（复用现有，预计 5 分钟）

1. 修改 `SysadminHomeViewModel.cs` - 导航按钮指向 `SystemSettingsView`
2. 无需新增文件 - 复用 Admin 角色的现有实现

### Phase 2: 用户管理（预计 1.5 小时）

1. 修改 `AdminUserManagementView.xaml.cs` - 接收导航参数
2. 修改 `UserMasterDetailControl.xaml.cs` - 新增 SetDefaultRoleFilter
3. 修改 `UserMasterDetailViewModel.cs` - 增加 sysadmin 保护逻辑
4. 添加 sysadmin 视觉标记

### Phase 3: 测试（预计 1 小时）

1. 验证版本号显示为 1.0.0
2. 编写单元测试
3. 手动测试 Dashboard
4. 手动测试用户管理

---

## [S8] 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| IConnectionModeService 未注册 | DI 解析失败 | 检查 Infrastructure 模块注册 |
| IClinicSettingsService 未注册 | DI 解析失败 | 检查 Infrastructure 模块注册 |
| 版本号不一致 | 显示错误版本 | 同步修复 SystemConstants 和 Directory.Build.props |
| 导航参数传递失败 | 过滤不生效 | 添加参数存在性检查 |
| sysadmin 检查遗漏 | 可编辑 sysadmin 账号 | 全面检查所有操作入口 |

---

## [S9] 变更日志

| 日期 | 版本 | 变更 | 作者 |
|------|------|------|------|
| 2026-06-24 | v1.0 | 初始设计 | MiMoCode |
