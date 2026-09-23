# Desktop 端架构设计标准

> **文档版本**: v1.1
> **最后更新**: 2026-09-13
> **适用范围**: LYBT.Desktop.* 所有业务模块
> **适用系统**: 凌隐宝堂中医诊所管理系统（桌面端）

## 📋 目录

- [1. 架构概述](#1-架构概述)
- [2. 三层架构设计](#2-三层架构设计)
- [3. Repository 设计规范](#3-repository-设计规范)
- [4. ViewModel 设计规范](#4-viewmodel-设计规范)
- [5. View 设计规范](#5-view-设计规范)
- [6. 模块设计规范](#6-模块设计规范)
  - [6.3 模块类型分类](#63-模块类型分类)
- [7. 服务分层标准](#7-服务分层标准)
- [8. 依赖注入规范](#8-依赖注入规范)
- [9. 命名规范](#9-命名规范)
- [10. 代码示例](#10-代码示例)
- [11. 架构测试](#11-架构测试)
- [12. 常见问题](#12-常见问题)
- [13. Shell 公共组件](#13-shell-公共组件2026-08-25-抽取ssot-desktop-layout-framework)
  - [13.4 对话框（Dialog）规范](#134-对话框dialog规范)
  - [13.5 数量口径与视图/VM 清单校验](#135-数量口径与视图vm-清单校验)
- [附录](#附录)

---

## 1. 架构概述

### 1.1 技术栈

- **UI 框架**: WPF (Windows Presentation Foundation)
- **架构模式**: MVVM (Model-View-ViewModel)
- **模块化框架**: Prism.DryIoc 8.x+
- **依赖注入**: Prism.DryIoc (DryIoc 容器)
- **对象映射**: Riok.Mapperly (编译期映射)
- **MVVM 工具**: CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`)
- **.NET 版本**: .NET 8.0

### 1.2 架构原则

1. **分层清晰**: View-ViewModel-Repository 三层架构，职责明确
2. **依赖方向**: View → ViewModel → Repository → API Client
3. **松耦合**: 面向接口编程，使用依赖注入
4. **模块化**: 按业务功能划分模块，模块间通过事件通信
5. **SSOT 原则**: 单一事实源，避免重复定义

### 1.3 架构层次

```
┌─────────────────────────────────────────────────────┐
│                    Desktop Client                    │
├─────────────────────────────────────────────────────┤
│  View 层 (XAML + Code-Behind)                       │
│  - 负责UI展示和用户交互                              │
│  - 通过 DataBinding 绑定 ViewModel                   │
├─────────────────────────────────────────────────────┤
│  ViewModel 层 (CoreViewModelBase / NavigableViewModelBase) │
│  - 负责业务逻辑和数据转换                            │
│  - 调用 Repository 获取数据                          │
│  - 使用 Mapperly 进行 DTO ↔ UI Model 转换           │
├─────────────────────────────────────────────────────┤
│  Repository 层 (IXxxRepository)                     │
│  - 负责数据访问和API调用                             │
│  - 返回裸类型 (不包装 ServiceResult)                 │
│  - 异常向上抛出由 ViewModel 处理                     │
├─────────────────────────────────────────────────────┤
│  API Client 层 (Refit Interface)                    │
│  - HTTP API 调用接口                                │
│  - 由 Shell 统一注册                                │
└─────────────────────────────────────────────────────┘
```

### 1.4 示例代码约定（与代码实际的边界）

本文档 §3 / §5 / §6 / §8 / §10 的代码示例为**教学骨架**，以 `Users` 模块为模板演示分层写法；其中的文件路径与类型名**不构成对代码实际（src/Client/Desktop）文件清单的断言**。口径与清单以 §13.5 为准。

- `UserManagementView` 实际位于 `Roles/LYBT.Desktop.Admin/Views/UserManagementView.xaml`（命名空间 `LYBT.Desktop.Admin.Views`），由 `Roles/LYBT.Desktop.Admin/AdminModule.cs` 注册导航；示例中写作 `LYBT.Desktop.Users.Views` 仅为便于连续阅读。
- `UserDetailView`、`UserEditorDialog`、`UserDetailViewModel`、`UserManagementViewModel` 在代码实际中**不存在** `[未建视图]`：用户模块的列表 + 详情由 `Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`（+ `UserMasterDetailViewModel`）承载，编辑/查看由同目录 `UserEditControl.xaml`（+ `UserEditorViewModel`）/`UserViewControl.xaml` 承载，均**不是**独立导航视图、也不是对话框。
- 示例中的 `{模块}/Interfaces/` 目录与「模块内注册 Repository」写法为教学骨架；代码实际的落点是——接口 `Core/LYBT.Desktop.Contracts/Repositories/`、实现 `Modules/{模块}/Repositories/`、注册集中在 Shell（`Shell/Extensions/DataSourceRegistrationExtensions.cs` 的 `RegisterRepositories`，`I{实体}Repository` → 模块实现，基于 `IApiClient{实体}`）。

---

## 2. 三层架构设计

### 2.1 View 层

**职责**:
- 负责 UI 展示和用户交互
- 通过 DataBinding 绑定 ViewModel 属性
- 不包含业务逻辑（仅限 UI 逻辑）

**约束**:
- ✅ 使用 `{Binding}` 绑定 ViewModel 属性
- ✅ 使用 `{x:Bind}` 优化性能（可选）
- ✅ 代码隐藏仅包含 UI 逻辑（如动画、焦点控制）
- ❌ 禁止在 Code-Behind 中调用 Repository
- ❌ 禁止在 Code-Behind 中包含业务逻辑

### 2.2 ViewModel 层

**职责**:
- 负责业务逻辑和数据转换
- 调用 Repository 获取数据
- 使用 Mapperly 进行 DTO ↔ UI Model 转换
- 处理异常并显示用户友好的错误信息

**约束**:
- ✅ 继承 `CoreViewModelBase`、`NavigableViewModelBase`、`MasterDetailViewModelBase<TListItem, TDetail>` 等标准基类
- ✅ 使用构造函数注入依赖
- ✅ 使用 Mapperly 进行对象映射
- ✅ 使用 `INotificationService` 显示消息
- ❌ 禁止直接调用 API（必须通过 Repository）
- ❌ 禁止在 ViewModel 中创建 UI 元素

### 2.3 Repository 层

**职责**:
- 负责数据访问和 API 调用
- 封装 API 调用细节
- 提供业务友好的数据访问接口

**约束**:
- ✅ 返回裸类型（如 `Task<List<UserDto>>`）
- ✅ 异常向上抛出由 ViewModel 处理
- ✅ 使用 Refit 接口调用 API
- ❌ 禁止返回 `ServiceResult<T>`（Server 端专用）
- ❌ 禁止在 Repository 中显示 UI 消息

---

## 3. Repository 设计规范

### 3.1 接口定义

**位置**: `{模块}/Interfaces/IXxxRepository.cs`

> **代码实际**：接口位于 `Core/LYBT.Desktop.Contracts/Repositories/I{实体}Repository.cs`（SYNC-D02；`tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs` 的 `DM05` 断言此约束），示例命名空间 `LYBT.Desktop.Users.Interfaces` 为教学骨架（见 §1.4）。

**命名规范**:
- 接口名称: `I{业务实体}Repository`
- 方法名称: 动词 + 名词（如 `GetUsersAsync`, `AddUserAsync`）

**示例**:
```csharp
namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户数据访问接口
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 获取所有用户
    /// </summary>
    Task<List<UserDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<UserDto?> GetByIdAsync(int id);

    /// <summary>
    /// 添加用户
    /// </summary>
    Task<UserDto> AddAsync(CreateUserDto dto);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task UpdateAsync(int id, UpdateUserDto dto);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task DeleteAsync(int id);
}
```

### 3.2 实现类

**位置**: `{模块}/Repositories/XxxRepository.cs`

**命名规范**:
- 类名: `{业务实体}Repository`
- 继承: 实现对应的 `IXxxRepository` 接口

**示例**:
```csharp
namespace LYBT.Desktop.Users.Repositories;

/// <summary>
/// 用户数据访问实现
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IUserApi _userApi;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        IUserApi userApi,
        ILogger<UserRepository> logger)
    {
        _userApi = userApi;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("正在获取所有用户...");
            return await _userApi.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有用户失败");
            throw; // 异常向上抛出
        }
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("正在获取用户 {UserId}...", id);
            return await _userApi.GetUserByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户 {UserId} 失败", id);
            throw;
        }
    }

    // 其他方法实现...
}
```

### 3.3 注册位置

**位置**: `{模块}/{模块名}Module.cs` 的 `RegisterTypes` 方法

**注册方式**: `RegisterSingleton<IXxxRepository, XxxRepository>()`

> **代码实际**：注册集中在 Shell——`Shell/Extensions/DataSourceRegistrationExtensions.cs` 的 `RegisterRepositories` 以 `containerRegistry.Register<I{实体}Repository>(…)` 把接口绑定到模块实现（实现基于 `IApiClient{实体}`），见 §12.2。

**示例**:
```csharp
public class UsersModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // Repository (数据访问层) 由各业务模块自行注册
        containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

        // 注册 ViewModel
        containerRegistry.Register<UserManagementViewModel>(); // [未建 VM] 代码实际为 UserMasterDetailViewModel
        containerRegistry.Register<UserDetailViewModel>();     // [未建 VM] 见 §1.4

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.UserManagementView>();
        // [未建视图] UserDetailView 在代码实际中不存在（见 §1.4）；用户详情由 Controls/UserViewControl.xaml 承载
        containerRegistry.RegisterForNavigation<Views.UserDetailView>();
    }
}
```

### 3.4 返回值约定

**✅ 正确示例**:
```csharp
// 返回裸类型
Task<List<UserDto>> GetAllAsync();
Task<UserDto?> GetByIdAsync(int id);
Task<UserDto> AddAsync(CreateUserDto dto);
Task UpdateAsync(int id, UpdateUserDto dto);
Task DeleteAsync(int id);
```

**❌ 错误示例**:
```csharp
// ❌ 不要返回 ServiceResult（Server 端专用）
Task<ServiceResult<List<UserDto>>> GetAllAsync();
Task<ServiceResult<UserDto>> GetByIdAsync(int id);
```

### 3.5 异常处理

**原则**: Repository 不处理异常，仅记录日志后向上抛出

**示例**:
```csharp
public async Task<List<UserDto>> GetAllAsync()
{
    try
    {
        _logger.LogInformation("正在获取所有用户...");
        return await _userApi.GetAllUsersAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取所有用户失败");
        throw; // 异常向上抛出，由 ViewModel 处理
    }
}
```

---

## 4. ViewModel 设计规范

### 4.1 基类选择

| 基类 | 说明 | 典型场景 |
|------|------|----------|
| `CoreViewModelBase` | 核心基类（ObservableObject + IViewModelServices 聚合） | 所有 VM 的根基类 |
| `NavigableViewModelBase` | 可导航 VM（INavigationAware + IRegionMemberLifetime） | 需要 Prism 导航的页面 |
| `MasterDetailViewModelBase<TListItem, TDetail>` | Master-Detail CRUD 模式 | 列表+详情管理页面 |
| `DialogViewModelBase` | 对话框 VM（IDialogAware） | 模态对话框 |
| `ChildViewModelBase` | 复合 VM 子组件 | 嵌入父 VM 的子区域 |

### 4.2 构造函数注入

**示例**:
```csharp
namespace LYBT.Desktop.Users.ViewModels;

public partial class UserManagementViewModel : NavigableViewModelBase
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly UserMapper _mapper;
    private readonly ILogger<UserManagementViewModel> _logger;

    public UserManagementViewModel(
        IUserRepository userRepository,
        INotificationService notificationService,
        IDialogService dialogService,
        UserMapper mapper,
        ILogger<UserManagementViewModel> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _mapper = mapper;
        _logger = logger;
    }

    // ViewModel 实现...
}
```

### 4.3 命令定义

**使用**: `[RelayCommand]` 特性（CommunityToolkit.Mvvm）

**示例**:
```csharp
public partial class UserManagementViewModel : NavigableViewModelBase
{
    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "正在加载用户列表...";

            var users = await _userRepository.GetAllAsync();
            Items = new ObservableCollection<UserDto>(users);

            _notificationService.ShowSuccess("用户列表加载成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户列表失败");
            _notificationService.ShowError($"加载用户列表失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddUserAsync() { /* ... */ }

    [RelayCommand]
    private async Task EditUserAsync(UserListDto user) { /* ... */ }

    [RelayCommand]
    private async Task DeleteUserAsync(UserListDto user) { /* ... */ }
}
```

### 4.4 异常处理模式

**标准模式**:
```csharp
private async Task ExecuteActionAsync()
{
    try
    {
        // 1. 设置忙状态
        IsBusy = true;
        BusyMessage = "正在执行操作...";

        // 2. 调用 Repository
        var result = await _repository.GetDataAsync();

        // 3. 更新 UI
        Items = new ObservableCollection<Item>(result);

        // 4. 显示成功消息
        _notificationService.ShowSuccess("操作成功");
    }
    catch (Exception ex)
    {
        // 5. 记录日志
        _logger.LogError(ex, "操作失败");

        // 6. 显示用户友好的错误消息
        _notificationService.ShowError($"操作失败: {ex.Message}");
    }
    finally
    {
        // 7. 清除忙状态
        IsBusy = false;
    }
}
```

### 4.5 Mapperly 映射

**配置**: 在 `{模块}/Mappings/` 中定义 Mapper 类（编译期生成，无运行时反射）

**示例**（以验方模块实际实现为例）:
```csharp
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Formula.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class FormulaDetailModelMapper
{
    // DTO → UI Model
    [MapperIgnoreSource(nameof(FormulaDetailDto.Herbs))]
    public partial FormulaDetailModel ToModel(FormulaDetailDto dto);

    // UI Model → Update DTO
    public partial FormulaDetailDto ToDto(FormulaDetailModel model);

    // UI Model → Create/Update Input DTO
    public partial FormulaInputDto ToInputDto(FormulaDetailModel model);
}
```

**使用**:
```csharp
// ViewModel 中使用（编译期生成，无运行时反射）
var model = _mapper.ToModel(dto);
var inputDto = _mapper.ToInputDto(model);
```

---

## 5. View 设计规范

### 5.1 XAML 设计原则

1. **数据绑定**: 使用 `{Binding}` 绑定 ViewModel 属性
2. **命令绑定**: 使用 `{Binding XxxCommand}` 绑定命令
3. **样式统一**: 使用 `MaterialDesignThemes` 统一样式
4. **响应式布局**: 使用 Grid/DockPanel 实现响应式布局

### 5.2 XAML 示例

> 示例命名空间 `LYBT.Desktop.Users.Views` 为教学骨架；`UserManagementView` 的代码实际位置是 `Roles/LYBT.Desktop.Admin/Views/UserManagementView.xaml`（见 §1.4）。

```xml
<UserControl x:Class="LYBT.Desktop.Users.Views.UserManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <!-- 工具栏 -->
        <DockPanel DockPanel.Dock="Top" Margin="16">
            <Button Content="添加用户"
                    Command="{Binding AddUserCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"/>
            <Button Content="刷新"
                    Command="{Binding LoadUsersCommand}"
                    Style="{StaticResource MaterialDesignFlatButton}"/>
        </DockPanel>

        <!-- 列表 -->
        <DataGrid ItemsSource="{Binding Items}"
                  SelectedItem="{Binding SelectedItem}"
                  AutoGenerateColumns="False">
            <DataGrid.Columns>
                <DataGridTextColumn Header="用户名" Binding="{Binding Username}"/>
                <DataGridTextColumn Header="姓名" Binding="{Binding FullName}"/>
                <DataGridTextColumn Header="角色" Binding="{Binding RoleName}"/>
                <DataGridTemplateColumn Header="操作">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="编辑"
                                        Command="{Binding DataContext.EditUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding Id}"/>
                                <Button Content="删除"
                                        Command="{Binding DataContext.DeleteUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding Id}"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 忙状态指示器 -->
        <md:Card Visibility="{Binding IsBusy, Converter={StaticResource BooleanToVisibilityConverter}}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar Style="{StaticResource MaterialDesignCircularProgressBar}" IsIndeterminate="True"/>
                <TextBlock Text="{Binding BusyMessage}" Margin="0,16,0,0"/>
            </StackPanel>
        </md:Card>
    </Grid>
</UserControl>
```

### 5.3 Code-Behind 约束

**✅ 允许的场景**:
```csharp
public partial class UserManagementView : UserControl
{
    public UserManagementView()
    {
        InitializeComponent();
    }

    // ✅ UI 逻辑：焦点控制
    private void SearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
    }

    // ✅ UI 逻辑：动画
    private void ListItem_MouseEnter(object sender, MouseEventArgs e)
    {
        // 播放动画
    }
}
```

**❌ 禁止的场景**:
```csharp
public partial class UserManagementView : UserControl
{
    // ❌ 禁止：直接调用 Repository
    private readonly IUserRepository _userRepository;

    // ❌ 禁止：包含业务逻辑
    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var users = await _userRepository.GetAllAsync();
        UserList.ItemsSource = users;
    }
}
```

---

## 6. 模块设计规范

### 6.1 模块目录结构

```
LYBT.Desktop.{模块名}/
├── Interfaces/               # 接口定义
│   └── IXxxRepository.cs
├── Repositories/             # Repository 实现
│   └── XxxRepository.cs
├── ViewModels/               # ViewModel
│   ├── XxxManagementViewModel.cs
│   └── XxxDetailViewModel.cs
├── Views/                    # View
│   ├── XxxManagementView.xaml
│   └── XxxDetailView.xaml
├── Models/                   # UI 模型
│   ├── XxxItem.cs
│   └── XxxInfo.cs
├── Mappings/                 # Mapperly 映射器
│   └── XxxMapper.cs
├── Events/                   # 模块事件
│   └── XxxChangedEvent.cs
└── {模块名}Module.cs          # 模块入口
```

### 6.2 模块类实现

**示例**:
```csharp
namespace LYBT.Desktop.Users;

[Module(ModuleName = nameof(UsersModule))]
[ModuleDependency("AuthenticationModule")] // 依赖认证模块
public class UsersModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑（可选）
        var logger = containerProvider.Resolve<ILogger<UsersModule>>();
        logger.LogInformation("用户管理模块已初始化");
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
        // - Repository (数据访问层) 由各业务模块自行注册
        containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

        // 注册 ViewModel
        containerRegistry.Register<UserManagementViewModel>();
        containerRegistry.Register<UserDetailViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.UserManagementView>();
        // [未建视图] UserDetailView 在代码实际中不存在（见 §1.4）
        containerRegistry.RegisterForNavigation<Views.UserDetailView>();

        // 注册对话框（可选）
        // [未建视图] UserEditorDialog / UserEditorDialogViewModel 在代码实际中不存在（见 §1.4）；
        // 用户编辑由 Controls/UserEditControl.xaml + UserEditorViewModel 承载（控件复用，非对话框）
        containerRegistry.RegisterDialog<Views.UserEditorDialog, ViewModels.UserEditorDialogViewModel>();
    }
}
```

### 6.3 模块依赖配置

**原则**:
- 使用 `[ModuleDependency]` 特性声明依赖关系
- 避免循环依赖
- 模块依赖应尽量少

**示例**:
```csharp
// 认证模块：无依赖
[Module(ModuleName = nameof(AuthenticationModule))]
public class AuthenticationModule : IModule { }

// 用户模块：依赖认证
[Module(ModuleName = nameof(UsersModule))]
[ModuleDependency("AuthenticationModule")]
public class UsersModule : IModule { }

// 患者模块：依赖认证和用户
[Module(ModuleName = nameof(PatientsModule))]
[ModuleDependency("AuthenticationModule")]
[ModuleDependency("UsersModule")]
public class PatientsModule : IModule { }
```

### 6.3 模块类型分类

根据业务实体与聚合根的关系，Desktop模块分为三种类型：

| 类型 | 数据访问层 | 特征 | 典型模块 |
|------|-----------|------|----------|
| **独立实体模块** | Repository | 独立管理的实体，有完整CRUD | Patients, Users, Herbs |
| **聚合根模块** | Repository + DataManager | 管理聚合及其子实体的生命周期 | MedicalCase, Formula |
| **从属实体模块** | CommandHandler | 子实体，通过父聚合的DataManager访问 | Consultation, Prescriptions |

> **代码实际（src/Client/Desktop）模块清单**：`Auth` / `Catalog`（药材 + 验方，即表中 `Herbs`、`Formula` 的合并落点）/ `MedicalCase` / `Patients` / `Registrations` / `Users`；表中 `Consultation`、`Prescriptions` 为 `MedicalCase` 聚合内的子实体，**无独立模块** `[未建模块]`。

#### 6.3.1 独立实体模块

独立实体模块直接使用Repository进行数据访问：

```csharp
// 目录结构（教学骨架；视图落点已按代码实际修正：Patients 模块用 Controls/ 而非 Views/）
LYBT.Desktop.Patients/
├── Interfaces/                      // 教学骨架：Repository 接口现位于 Core/LYBT.Desktop.Contracts/Repositories/IPatientRepository.cs
│   └── IPatientRepository.cs        // Repository接口
├── Repositories/
│   └── PatientRepository.cs         // Repository实现
├── Models/
│   ├── PatientDetailModel.cs        // 可编辑UI模型
│   ├── PatientViewState.cs          // 视图状态
│   └── Items/
│       └── PatientEditContext.cs    // 编辑上下文模型
├── ViewModels/
│   ├── PatientMasterDetailViewModel.cs
│   ├── PatientEditorViewModel.cs
│   └── PatientCardReaderViewModel.cs
└── Controls/                        // 代码实际：内嵌组件（由角色台 View 复用，非独立导航视图）
    ├── PatientMasterDetailControl.xaml
    ├── PatientEditControl.xaml
    ├── PatientViewControl.xaml
    └── PatientSelectionControl.xaml
                                       // [未建视图] Views/PatientMasterDetailView.xaml 不存在

// 模块注册
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();
        containerRegistry.Register<PatientMasterDetailViewModel>();
        containerRegistry.RegisterForNavigation<PatientMasterDetailView>(); // [未建视图] 代码实际无此视图
    }
}
```

#### 6.3.2 聚合根模块

聚合根模块使用Repository + DataManager管理聚合状态：

```csharp
// 目录结构
LYBT.Desktop.MedicalCase/
├── Interfaces/
│   ├── IMedicalCaseRepository.cs    // Repository接口
│   └── IMedicalCaseDataManager.cs   // DataManager接口
├── Repositories/
│   └── MedicalCaseRepository.cs
├── Services/
│   └── MedicalCaseDataManager.cs    // 管理聚合状态
├── Models/
│   ├── MedicalCaseDetailModel.cs
│   └── Items/
│       └── MedicalCaseItem.cs
└── ViewModels/
    └── MedicalCaseMasterDetailViewModel.cs

// DataManager职责：
// - 管理聚合根及其子实体(Consultation, Prescription)
// - 状态追踪和变更检测(HasChanges)
// - 统一保存(SaveAsync一次性提交整个聚合)

// 模块注册
public class MedicalCaseModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IMedicalCaseRepository, MedicalCaseRepository>();
        containerRegistry.RegisterScoped<IMedicalCaseDataManager, MedicalCaseDataManager>();
        containerRegistry.Register<MedicalCaseMasterDetailViewModel>();
    }
}
```

#### 6.3.3 从属实体模块

从属实体模块通过CommandHandler委托给父聚合的DataManager：

```csharp
// 目录结构（教学骨架：[未建模块] 代码实际无 LYBT.Desktop.Consultation 模块）
LYBT.Desktop.Consultation/
├── Interfaces/
│   └── IConsultationValidator.cs    // 验证器接口(可选)
├── Services/
│   ├── ConsultationCommandHandler.cs // 命令处理器
│   └── ConsultationValidator.cs
├── Models/
│   └── Items/
│       └── ConsultationItem.cs
└── ViewModels/
    └── ConsultationPanelViewModel.cs  // [未建 VM] 代码实际为 MedicalCase/ViewModels/Workspace/ConsultationEditorViewModel.cs

// CommandHandler职责：
// - 实现ICommandHandler接口
// - 依赖父聚合的IMedicalCaseDataManager
// - 通过DataManager执行保存/加载操作
// - 本地验证和业务规则

// 模块注册
public class ConsultationModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<ConsultationCommandHandler>();
        containerRegistry.Register<ConsultationValidator>();
        containerRegistry.Register<ConsultationPanelViewModel>();
    }
}
```

#### 6.3.4 数据流图

```
独立实体模块:
  ViewModel → Repository → API
      ↓
  DetailModel/Item

聚合根模块:
  ViewModel → DataManager → Repository → API
      ↓           ↓
  DetailModel   子实体DTO

从属实体模块:
  ViewModel → CommandHandler → 父DataManager → Repository → API
      ↓
  通过父聚合获取子实体数据
```

---

## 7. 服务分层标准

### 7.1 服务分类

根据 ADR-002 架构决策，Desktop 端服务分为三层：

| 层级 | 位置 | 注册位置 | 职责 |
|------|------|---------|------|
| **Foundation 层** | `LYBT.Desktop.Infrastructure` | Shell 统一注册 | 基础设施服务（导航、对话框、会话管理等） |
| **Infrastructure 层** | `LYBT.Desktop.Infrastructure` | Shell 统一注册 | 横切关注点（日志、缓存、配置等） |
| **Repository 层** | `LYBT.Desktop.{模块}.Repositories` | 模块自行注册 | 数据访问层（API 调用、数据缓存） |

### 7.2 Foundation 层服务

**定义**: 应用程序基础功能，所有模块都依赖

**示例**:
- `INavigationCoordinator`: 导航协调（统一入口，T5.5：原 `INavigationService` 已由 `INavigationCoordinator` 替代，遗留命名见本文历史版本）
- `IDialogService`: 对话框服务
- `ISessionManager`: 会话管理
- `IThemeService`: 主题服务
- `INotificationService`: 通知服务

**注册位置**: `LYBT.Desktop.Shell/App.xaml.cs` 或 `ShellModule.cs`

```csharp
// Shell 统一注册 Foundation 服务
containerRegistry.RegisterSingleton<INavigationCoordinator, NavigationCoordinator>();
containerRegistry.RegisterSingleton<IDialogService, PrismDialogService>();
containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
containerRegistry.RegisterSingleton<INotificationService, NotificationService>();
```

### 7.3 Infrastructure 层服务

**定义**: 横切关注点，非业务核心但通用的功能

**示例**:
- `ILogger<T>`: 日志服务（Serilog）
- `ICacheService`: 缓存服务
- `IConfigurationService`: 配置服务
- Mapperly 映射器（编译期生成，无需 DI 注册）

**注册位置**: `LYBT.Desktop.Shell/App.xaml.cs` 或 `InfrastructureModule.cs`

```csharp
// Shell 统一注册 Infrastructure 服务
containerRegistry.RegisterSingleton<ILogger<T>, Logger<T>>();
containerRegistry.RegisterSingleton<ICacheService, MemoryCacheService>();
containerRegistry.RegisterSingleton<IConfigurationService, ConfigurationService>();
// Mapperly 映射器由各模块自行注册（编译期生成，无需全局 IMapper）
```

### 7.4 Repository 层服务

**定义**: 数据访问层，封装 API 调用逻辑

**示例**:
- `IUserRepository`: 用户数据访问
- `IPatientRepository`: 患者数据访问
- `IPrescriptionRepository`: 处方数据访问

**注册位置**: 各业务模块的 `{模块名}Module.cs`

```csharp
// 模块自行注册 Repository
public class UsersModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();
        // ...
    }
}
```

---

## 8. 依赖注入规范

### 8.1 注入方式

**✅ 唯一推荐方式**: 构造函数注入

```csharp
public class UserManagementViewModel : NavigableViewModelBase
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserManagementViewModel> _logger;

    // ✅ 构造函数注入
    public UserManagementViewModel(
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<UserManagementViewModel> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }
}
```

**❌ 禁止方式**:
```csharp
// ❌ 禁止：属性注入
[Dependency]
public IUserRepository UserRepository { get; set; }

// ❌ 禁止：方法注入
public void SetRepository(IUserRepository repository) { }

// ❌ 禁止：服务定位器模式
var repository = Container.Resolve<IUserRepository>();
```

### 8.2 生命周期管理

| 方法 | 生命周期 | 适用场景 |
|------|---------|---------|
| `RegisterSingleton<TInterface, TImplementation>()` | 单例 | Repository, Foundation 服务, Infrastructure 服务 |
| `Register<TViewModel>()` | 瞬时 | ViewModel |
| `RegisterForNavigation<TView>()` | 瞬时 | View（导航） |
| `RegisterDialog<TView, TViewModel>()` | 瞬时 | Dialog（对话框） |

**示例**:
```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 单例：Repository（每个模块只需要一个实例）
    containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

    // 瞬时：ViewModel（每次导航创建新实例）
    containerRegistry.Register<UserManagementViewModel>();
    containerRegistry.Register<UserDetailViewModel>();

    // 导航：View
    containerRegistry.RegisterForNavigation<Views.UserManagementView>();

    // 对话框：View + ViewModel
    // [未建视图] UserEditorDialog 在代码实际中不存在（见 §1.4）；对话框清单见 §13.4
    containerRegistry.RegisterDialog<Views.UserEditorDialog, ViewModels.UserEditorDialogViewModel>();
}
```

---

## 9. 命名规范

### 9.1 命名空间

| 类型 | 命名空间 | 示例 |
|------|---------|------|
| 模块根 | `LYBT.Desktop.{模块名}` | `LYBT.Desktop.Users` |
| Repository 接口 | `LYBT.Desktop.{模块名}.Interfaces` | `LYBT.Desktop.Users.Interfaces` |
| Repository 实现 | `LYBT.Desktop.{模块名}.Repositories` | `LYBT.Desktop.Users.Repositories` |
| ViewModel | `LYBT.Desktop.{模块名}.ViewModels` | `LYBT.Desktop.Users.ViewModels` |
| View | `LYBT.Desktop.{模块名}.Views` | `LYBT.Desktop.Users.Views` |
| UI Model | `LYBT.Desktop.{模块名}.Models` | `LYBT.Desktop.Users.Models` |
| Mapperly 映射器 | `LYBT.Desktop.{模块名}.Mappings` | `LYBT.Desktop.Users.Mappings` |
| 事件 | `LYBT.Desktop.{模块名}.Events` | `LYBT.Desktop.Users.Events` |

### 9.2 类命名

| 类型 | 命名规范 | 示例 |
|------|---------|------|
| Repository 接口 | `I{实体}Repository` | `IUserRepository` |
| Repository 实现 | `{实体}Repository` | `UserRepository` |
| ViewModel | `{功能}ViewModel` | `UserMasterDetailViewModel`, `RegistrationListViewModel` |
| View | `{功能}View` | `UserManagementView`, `RegistrationListView` |
| Dialog | `{功能}Dialog` | `RegistrationCreateDialog`, `FormulaImportDialog` |
| UI Model | `{实体}Item`, `{实体}Info` | `HerbItem`, `FormulaItem` |
| Mapperly Mapper | `{模块}Mapper` | `FormulaDetailModelMapper` |
| Event | `{实体}{动作}Event` | `UserCreatedEvent`, `UserUpdatedEvent` |

### 9.3 成员命名

| 类型 | 命名规范 | 示例 |
|------|---------|------|
| 私有字段 | `_camelCase` | `_userRepository`, `_logger` |
| 属性 | `PascalCase` | `IsEnabled`, `UserName` |
| 方法 | `PascalCase` | `GetAllAsync`, `AddUserAsync` |
| 命令 | `{动作}Command` | `LoadUsersCommand`, `AddUserCommand` |
| 异步方法 | `{动作}Async` | `LoadUsersAsync`, `SaveDataAsync` |

---

## 10. 代码示例

### 10.1 完整模块示例

以 `Users` 模块为例，展示完整的三层架构实现。

> **示例骨架说明**：本节的文件路径与类型名为教学骨架，与代码实际（src/Client/Desktop）存在差异——`UserDetailView`、`UserDetailViewModel`、`UserManagementViewModel`、`UserEditorDialog` 均为 `[未建视图]`/`[未建 VM]`，`UserManagementView` 实际位于 `Roles/LYBT.Desktop.Admin/Views/`；差异清单与真实落点见 §1.4，口径见 §13.5。

#### 10.1.1 Repository 接口

**文件**: `LYBT.Desktop.Users/Interfaces/IUserRepository.cs`

```csharp
using LYBT.Shared.Dtos.User;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户数据访问接口
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 获取所有用户
    /// </summary>
    Task<List<UserDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<UserDto?> GetByIdAsync(int id);

    /// <summary>
    /// 添加用户
    /// </summary>
    Task<UserDto> AddAsync(CreateUserDto dto);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task UpdateAsync(int id, UpdateUserDto dto);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 搜索用户
    /// </summary>
    Task<List<UserDto>> SearchAsync(string keyword);
}
```

#### 10.1.2 Repository 实现

**文件**: `LYBT.Desktop.Users/Repositories/UserRepository.cs`

```csharp
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.ApiInterfaces;
using LYBT.Shared.Dtos.User;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Repositories;

/// <summary>
/// 用户数据访问实现
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IUserApi _userApi;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        IUserApi userApi,
        ILogger<UserRepository> logger)
    {
        _userApi = userApi;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("正在获取所有用户...");
            return await _userApi.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有用户失败");
            throw;
        }
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("正在获取用户 {UserId}...", id);
            return await _userApi.GetUserByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户 {UserId} 失败", id);
            throw;
        }
    }

    public async Task<UserDto> AddAsync(CreateUserDto dto)
    {
        try
        {
            _logger.LogInformation("正在添加用户 {Username}...", dto.Username);
            return await _userApi.CreateUserAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加用户失败");
            throw;
        }
    }

    public async Task UpdateAsync(int id, UpdateUserDto dto)
    {
        try
        {
            _logger.LogInformation("正在更新用户 {UserId}...", id);
            await _userApi.UpdateUserAsync(id, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户 {UserId} 失败", id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogInformation("正在删除用户 {UserId}...", id);
            await _userApi.DeleteUserAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户 {UserId} 失败", id);
            throw;
        }
    }

    public async Task<List<UserDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogInformation("正在搜索用户，关键字: {Keyword}...", keyword);
            return await _userApi.SearchUsersAsync(keyword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索用户失败");
            throw;
        }
    }
}
```

#### 10.1.6 View

**文件**: `Roles/LYBT.Desktop.Admin/Views/UserManagementView.xaml`（代码实际；示例骨架原写作 `LYBT.Desktop.Users/Views/UserManagementView.xaml`，见 §1.4）

```xml
<UserControl x:Class="LYBT.Desktop.Users.Views.UserManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/">

    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 工具栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
            <Button Content="添加用户"
                    Command="{Binding AddUserCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"
                    Margin="0,0,8,0"/>
            <Button Content="刷新"
                    Command="{Binding LoadDataCommand}"
                    Style="{StaticResource MaterialDesignFlatButton}"/>
        </StackPanel>

        <!-- 列表 -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Items}"
                  SelectedItem="{Binding SelectedItem}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True">
            <DataGrid.Columns>
                <DataGridTextColumn Header="用户名" Binding="{Binding Username}" Width="150"/>
                <DataGridTextColumn Header="姓名" Binding="{Binding FullName}" Width="200"/>
                <DataGridTextColumn Header="角色" Binding="{Binding RoleName}" Width="150"/>
                <DataGridCheckBoxColumn Header="启用" Binding="{Binding IsActive}" Width="80"/>
                <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt, StringFormat='yyyy-MM-dd'}" Width="120"/>
                <DataGridTemplateColumn Header="操作" Width="150">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="编辑"
                                        Command="{Binding DataContext.EditUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource MaterialDesignFlatButton}"
                                        Margin="0,0,4,0"/>
                                <Button Content="删除"
                                        Command="{Binding DataContext.DeleteUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource MaterialDesignFlatButton}"
                                        Foreground="Red"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 忙状态遮罩 -->
        <Border Grid.Row="1"
                Background="#80000000"
                Visibility="{Binding IsBusy, Converter={StaticResource BooleanToVisibilityConverter}}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar Style="{StaticResource MaterialDesignCircularProgressBar}"
                             IsIndeterminate="True"
                             Width="64"
                             Height="64"/>
                <TextBlock Text="{Binding BusyMessage}"
                           Foreground="White"
                           Margin="0,16,0,0"
                           FontSize="14"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

#### 10.1.7 Module 类

**文件**: `LYBT.Desktop.Users/UsersModule.cs`

```csharp
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Repositories;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Users;

/// <summary>
/// 用户管理模块
/// </summary>
[Module(ModuleName = nameof(UsersModule))]
[ModuleDependency("AuthenticationModule")] // 用户模块依赖认证模块
public class UsersModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑（可选）
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
        // - Repository (数据访问层) 由各业务模块自行注册
        containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

        // 注册 ViewModel
        containerRegistry.Register<ViewModels.UserManagementViewModel>(); // [未建 VM] 代码实际为 UserMasterDetailViewModel
        containerRegistry.Register<ViewModels.UserDetailViewModel>();     // [未建 VM] 见 §1.4

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.UserManagementView>();
        // [未建视图] UserDetailView 在代码实际中不存在（见 §1.4）
        containerRegistry.RegisterForNavigation<Views.UserDetailView>();

        // 注册对话框
        // [未建视图] UserEditorDialog 在代码实际中不存在（见 §1.4）；对话框清单见 §13.4
        containerRegistry.RegisterDialog<Views.UserEditorDialog, ViewModels.UserEditorDialogViewModel>();
    }
}
```

---

## 11. 架构测试

### 11.1 架构测试目的

使用 NetArchTest.Rules 编写架构测试，确保代码遵循架构约束：

- Desktop 层不依赖 Server 层
- Desktop 层不包含 DTO 类
- Desktop 层不直接使用 Entity 类
- ViewModel 必须继承标准基类（`DialogViewModelBase` 在允许基类清单内，见 §13.4）
- Repository 接口必须位于 `Core/LYBT.Desktop.Contracts/Repositories/`，且必须有远程实现（对应实测 `DM05` / `DM01`；ADR-002 时期「在模块内注册」的口径已随 SYNC-D02 调整，见 §12.2）

> **代码实际（src/Client/Desktop / tests）**：架构测试位于 `tests/LYBT.Tests.Architecture/`——`DesktopLayerArchTests.cs`（DP01–DP09、DM01–DM08）、`ShellViewViewModelBindingTests.cs`（见 §13.5）、`ShellExtractionArchTests.cs`、`CustomControlArchTests.cs` 等。以下 §11.2 为教学骨架，其程序集清单与方法名与代码实际不同，仅示意写法。

### 11.2 架构测试示例

**文件**: `tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs`（示例骨架；实际由 DP/DM 编号系列测试组成）

```csharp
using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace LYBT.ArchTests;

/// <summary>
/// Desktop层架构约束测试
/// </summary>
public class DesktopLayerArchTests
{
    private static readonly Assembly[] DesktopAssemblies =
    [
        Assembly.Load("LYBT.Desktop.Infrastructure"),
        Assembly.Load("LYBT.Desktop.Models"),
        Assembly.Load("LYBT.Desktop.Shell"),
        Assembly.Load("LYBT.Desktop.Auth"),
        Assembly.Load("LYBT.Desktop.Users"),
        Assembly.Load("LYBT.Desktop.Patients"),
        Assembly.Load("LYBT.Desktop.MedicalCase"),
        Assembly.Load("LYBT.Desktop.Consultation"),
        Assembly.Load("LYBT.Desktop.Prescriptions"),
        Assembly.Load("LYBT.Desktop.Herbs"),
        Assembly.Load("LYBT.Desktop.Formula"),
        Assembly.Load("LYBT.Desktop.AdminWorkstation"),
        Assembly.Load("LYBT.Desktop.ClinicalWorkstation")
    ];

    /// <summary>
    /// Desktop层不得依赖Server层
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Depend_On_Server_Layers()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .Should()
            .NotHaveDependencyOnAll("LYBT.Infrastructure", "LYBT.Entities")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Desktop层违规依赖Server层: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// Desktop层不得包含DTO类
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Contain_DTO_Classes()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("Desktop")
            .Should()
            .NotHaveNameEndingWith("Dto")
            .And()
            .NotHaveNameEndingWith("DTO")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Desktop层包含DTO类（应使用Item/ViewState/Info）: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");
    }

    /// <summary>
    /// Desktop层不应直接使用Entity类
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Use_Entity_Classes()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("Desktop")
            .Should()
            .NotHaveDependencyOn("LYBT.Entities")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Desktop层直接使用了Entity类: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// Desktop层ViewModels必须继承自正确基类
    /// </summary>
    [Fact]
    public void Desktop_ViewModels_Should_Inherit_From_Base_Classes()
    {
        var allowedBaseClasses = new[]
        {
            "CoreViewModelBase",
            "NavigableViewModelBase",
            "MasterDetailViewModelBase`2",
            "DialogViewModelBase",
            "ChildViewModelBase"
        };

        var viewModelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespace("ViewModels")
            .And()
            .HaveNameEndingWith("ViewModel")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .GetTypes()
            .Where(t => !t.Name.Contains("Design") && !t.Name.Contains("Mock"))
            .ToList();

        foreach (var vmType in viewModelTypes)
        {
            var currentType = vmType.BaseType;
            var hasValidBase = false;

            while (currentType != null && currentType != typeof(object))
            {
                var baseName = currentType.IsGenericType
                    ? currentType.GetGenericTypeDefinition().Name
                    : currentType.Name;

                if (allowedBaseClasses.Contains(baseName))
                {
                    hasValidBase = true;
                    break;
                }
                currentType = currentType.BaseType;
            }

            Assert.True(
                hasValidBase,
                $"ViewModel {vmType.FullName} 未继承自标准基类");
        }
    }

    /// <summary>
    /// 验证所有 Repository 都在对应模块中注册
    /// </summary>
    [Fact]
    public void All_Repositories_Should_Be_Registered_In_Modules()
    {
        var moduleAssemblies = new[]
        {
            Assembly.Load("LYBT.Desktop.Auth"),
            Assembly.Load("LYBT.Desktop.Users"),
            Assembly.Load("LYBT.Desktop.Patients"),
            Assembly.Load("LYBT.Desktop.MedicalCase"),
            Assembly.Load("LYBT.Desktop.Consultation"),
            Assembly.Load("LYBT.Desktop.Prescriptions"),
            Assembly.Load("LYBT.Desktop.Herbs"),
            Assembly.Load("LYBT.Desktop.Formula")
        };

        var repositoriesWithoutRegistration = new List<string>();

        foreach (var assembly in moduleAssemblies)
        {
            var repositoryInterfaces = assembly.GetTypes()
                .Where(t => t.IsInterface && t.Name.EndsWith("Repository"))
                .ToList();

            if (!repositoryInterfaces.Any())
                continue;

            var moduleType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name.EndsWith("Module") &&
                                   t.GetInterfaces().Any(i => i.Name == "IModule"));

            if (moduleType == null)
            {
                repositoriesWithoutRegistration.Add($"{assembly.GetName().Name}: 未找到 Module 类");
                continue;
            }

            var registerMethod = moduleType.GetMethod("RegisterTypes");
            if (registerMethod == null)
            {
                repositoriesWithoutRegistration.Add($"{assembly.GetName().Name}: Module 类未实现 RegisterTypes 方法");
                continue;
            }
        }

        Assert.Empty(repositoriesWithoutRegistration);
    }
}
```

---

## 12. 常见问题

### 12.1 为什么 Repository 不返回 ServiceResult？

**问题**: Server 端使用 `ServiceResult<T>` 包装返回值，为什么 Desktop 端不使用？

**答案**:
- **Server 端**: 需要返回统一的 HTTP 响应格式（包含状态码、错误消息等），所以使用 `ServiceResult<T>`
- **Desktop 端**: Repository 仅负责数据访问，不需要关心 HTTP 响应格式，异常由 ViewModel 处理并显示给用户

**示例对比**:
```csharp
// ✅ Desktop Repository - 返回裸类型
Task<List<UserDto>> GetAllAsync();

// ✅ Server Service - 返回 ServiceResult
Task<ServiceResult<List<UserDto>>> GetAllUsersAsync();
```

### 12.2 为什么 Repository 在模块中注册而不是 Shell？

**问题**: 为什么 Repository 要在业务模块的 `*Module.cs` 中注册，而不是统一在 Shell 中注册？

**答案**:
- **职责分离**: Repository 是业务模块的数据访问层，属于业务逻辑的一部分，应由模块自行管理
- **模块化**: 每个模块负责自己的依赖注入，降低模块间耦合
- **扩展性**: 新增模块时，只需在模块内注册 Repository，无需修改 Shell

**参考**: ADR-002 架构决策记录

> **代码实际**：ADR-002 之后注册位置已迁移到 Shell——`Shell/Extensions/DataSourceRegistrationExtensions.cs` 的 `RegisterRepositories` 统一绑定 `I{实体}Repository`（`Core/LYBT.Desktop.Contracts/Repositories/`）到各模块 `Modules/{模块}/Repositories/` 的实现，实现基于 `IApiClient{实体}`；`Modules/LYBT.Desktop.Users/UsersModule.cs`、`Modules/LYBT.Desktop.Patients/PatientsModule.cs` 中亦注释「由 Shell DI 注册 (Refit API)」。本问答保留 ADR-002 的决策语境，注册位置以代码实际为准。

### 12.3 为什么 ViewModel 不直接调用 API？

**问题**: 为什么 ViewModel 不能直接注入 `IUserApi` 调用 API，而必须通过 Repository？

**答案**:
- **关注点分离**: ViewModel 关注业务逻辑和 UI 交互，Repository 关注数据访问细节
- **可测试性**: 通过 Repository 接口，ViewModel 可以轻松 Mock 数据进行单元测试
- **可维护性**: API 调用逻辑集中在 Repository，便于统一处理缓存、重试、异常等

**错误示例**:
```csharp
// ❌ 错误：ViewModel 直接调用 API
public class UserManagementViewModel : NavigableViewModelBase
{
    private readonly IUserApi _userApi; // ❌ 不应直接注入 API

    public async Task LoadUsersAsync()
    {
        var users = await _userApi.GetAllUsersAsync(); // ❌ 不应直接调用 API
    }
}
```

**正确示例**:
```csharp
// ✅ 正确：ViewModel 通过 Repository 调用
public class UserManagementViewModel : NavigableViewModelBase
{
    private readonly IUserRepository _userRepository; // ✅ 注入 Repository

    public async Task LoadUsersAsync()
    {
        var users = await _userRepository.GetAllAsync(); // ✅ 通过 Repository
    }
}
```

### 12.4 如何处理跨模块通信？

**问题**: 模块 A 需要通知模块 B 数据发生变化，如何实现？

**答案**: 使用 Prism EventAggregator（发布-订阅模式）

**示例**:

1. 定义事件（`LYBT.Desktop.Users/Events/UserChangedEvent.cs`）:
```csharp
public class UserChangedEvent : PubSubEvent<int> { }
```

2. 发布事件（模块 A）:
```csharp
public class UserManagementViewModel : NavigableViewModelBase
{
    private readonly IEventAggregator _eventAggregator;

    public async Task UpdateUserAsync(int userId)
    {
        await _userRepository.UpdateAsync(userId, updateDto);
        _eventAggregator.GetEvent<UserChangedEvent>().Publish(userId); // 发布事件
    }
}
```

3. 订阅事件（模块 B）:
```csharp
public class PatientManagementViewModel : NavigableViewModelBase
{
    private readonly IEventAggregator _eventAggregator;

    public PatientManagementViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        _eventAggregator.GetEvent<UserChangedEvent>().Subscribe(OnUserChanged); // 订阅事件
    }

    private void OnUserChanged(int userId)
    {
        // 用户数据变化，刷新患者列表
        RefreshAsync().ConfigureAwait(false);
    }
}
```

### 12.5 如何进行 Repository 单元测试？

**问题**: 如何对 Repository 进行单元测试？

**答案**: 使用 Moq 框架 Mock `IXxxApi` 接口

**示例**:
```csharp
using Moq;
using Xunit;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnUserList()
    {
        // Arrange
        var mockApi = new Mock<IUserApi>();
        var mockLogger = new Mock<ILogger<UserRepository>>();

        var expectedUsers = new List<UserDto>
        {
            new UserDto { Id = 1, Username = "admin", FullName = "管理员" },
            new UserDto { Id = 2, Username = "user1", FullName = "用户1" }
        };

        mockApi.Setup(api => api.GetAllUsersAsync())
               .ReturnsAsync(expectedUsers);

        var repository = new UserRepository(mockApi.Object, mockLogger.Object);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("admin", result[0].Username);
        mockApi.Verify(api => api.GetAllUsersAsync(), Times.Once);
    }
}
```

### 12.6 如何处理长时间运行的操作？

**问题**: 用户点击按钮后，需要执行一个耗时操作（如导入数据），如何避免 UI 卡顿？

**答案**: 使用 `async/await` + `IsBusy` 状态 + `BusyMessage`

**示例**:
```csharp
[RelayCommand]
private async Task ImportDataAsync()
{
    try
    {
        // 1. 设置忙状态
        IsBusy = true;
        BusyMessage = "正在导入数据，请稍候...";

        // 2. 执行耗时操作（在后台线程）
        await Task.Run(async () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                await _repository.ImportItemAsync(items[i]);

                // 更新进度
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    BusyMessage = $"正在导入数据 ({i + 1}/1000)...";
                });
            }
        });

        // 3. 显示成功消息
        _notificationService.ShowSuccess("数据导入成功");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导入数据失败");
        _notificationService.ShowError($"导入数据失败: {ex.Message}");
    }
    finally
    {
        // 4. 清除忙状态
        IsBusy = false;
    }
}
```

---

## 13. Shell 公共组件（2026-08-25 抽取，SSOT desktop-layout-framework）

> 依据 `docs/07-ui-ux/desktop-layout-framework.md`（固化）与 shell-component-extraction-plan（已清理，git 历史可查）30 轮方案 A。业务 View 零改动，Shell 层仅通过纯 UserControl 组合实现三栏公共壳。

### 13.1 组件清单

| 组件 | 文件 | 尺寸/样式 | 职责 | 宽度常量 |
|------|------|-----------|------|----------|
| HeaderControl / HeaderViewModel | `Shell/Views/HeaderControl.xaml` `Shell/ViewModels/HeaderViewModel.cs` | h48 fill `$primary` padding `[0,24]` 7 子元素 | 品牌块36×36 `$accent` r10「医」20/700 + 标题17/600白 + 弹性 + 分隔1×20白30% + 用户icon 26 + 姓名13/600白 + 角色12 `#D9C7C1`；用户区可点击 `EditProfileCommand` | — |
| SideNavControl / SideNavViewModel | `Shell/Views/SideNavControl.xaml` `Shell/ViewModels/SideNavViewModel.cs` | 展开240 收拢64 fill `$primary-dark` | 汉堡h40居中 + 分组标题11/600 `#C9B8A6`（仅展开） + 菜单h38 r10 选中`$primary` gap12 + 底部固定区（分割线+深色模式+退出）；C+矩阵 4角色×3项 | `ShellConstants.SidebarCollapsedWidth=64` `SidebarExpandedWidth=240` |
| FooterControl / FooterViewModel | `Shell/Views/FooterControl.xaml` `Shell/ViewModels/FooterViewModel.cs` | h32 暖灰 顶部描边 `#E9DFD7`1 padding `[0,24]` | 左组 API 状态（`ApiStatusIcon`/`ApiStatusColor`/`ApiStatusText` 三者同绑定——文本随真实健康状态变化，禁止硬编码「已连接」）+ 连接模式12 `#8D6E63` gap16，右时间12；Tick 订阅已从 MainWindow 迁移 | — |
| AppShell | `Shell/Views/AppShell.xaml` | 无 VM，纯组合 | Header(48) + Grid( SideNav(240/64，列宽绑**宿主 VM 的 SidebarWidth 代理**，来自 `ISidebarStateManager`) + 右列[ContentRegion唯一 + Footer32])；对话框统一走 Prism（2026-09-23 移除无消费者的 MDIX `DialogHost Identifier=RootDialog` 包裹层） | 引用 ShellConstants |
| ShellConstants | `Shell/ShellConstants.cs` | — | 侧栏宽度 SSOT | 64 / 240 |
| SidebarStateManager | `Shell/Services/SidebarStateManager.cs` | — | 侧栏展开态/宽度 SSOT（`ISidebarStateManager` 单例）——宿主与侧栏 VM 均为代理，消除 P1 双份状态不同步 | 64 / 240 推导 |
| ShellLogoutService | `Shell/Services/ShellLogoutService.cs` | — | 登出唯一入口：活跃医案 `RequestLeaveAsync` 守卫 + 二次确认 → `PerformLogoutAsync`；宿主与侧栏退出按钮共用（修复既有审查 #34） | `LogoutOutcome` |
| ShellViewMappings | `Shell/ShellViewMappings.cs` | — | 视图→VM 显式映射表（约定名不匹配时唯一登记点）；`App.ConfigureViewModelLocator` 调用，守卫测试同源校验 | 5 条映射 |

### 13.2 命名与职责

- 命名与绑定：`HeaderControl/SideNavControl/FooterControl/AppShell` 位于 `Shell.Views`，对应 VM 位于 `Shell.ViewModels`（`...ViewModel` 后缀，继承 `ObservableObject`，构造注入 `IShellServices`）。**三控件的 Prism 约定名（`HeaderControlViewModel`/`SideNavControlViewModel`/`FooterControlViewModel`）不存在**，因此一律经 `ShellViewMappings.Mappings` 显式映射（`App.ConfigureViewModelLocator` → `ShellViewMappings.Register()`）；新增 AW 控件必须在该表登记，守卫测试 `ShellViewViewModelBindingTests`（架构测试项目）会拦截漏登记——历史上漏登记导致 AW 静默跳过赋值、控件继承宿主 DataContext，绑定失效且无任何异常/日志。
- 状态来源（SSOT）：侧栏展开态/宽度 = `IShellServices.Sidebar`（`SidebarStateManager`）；深色模式 = `IShellServices.Theme`（`ThemeService` 经 MaterialDesign `PaletteHelper` 应用并持久化）；登录态 = `ILoginStateManager`；API 状态/时间 = `IStatusBarManager`；登出 = `IShellServices.Logout`（含活跃医案守卫）。VM 只做代理，不复制状态。
- 职责：Header 只读展示+个人资料入口；SideNav 负责导航列表与折叠/主题；Footer 负责健康+时间；AppShell 负责布局组合，不新增 Region。
- 常量：所有 64/240 引用必须来自 `ShellConstants`，禁止内联。

### 13.3 与框架文档的对齐

- 顶部 48 七子元素、左侧 240/64、菜单 38 r10、底部 32 等均逐项对齐 `desktop-layout-framework.md`。
- 角色×菜单矩阵已在 `NavigationManager.BuildNavigationItems` 落地 C+（Doctor 3 等）。
- `MainWindow` 仅保留 LoginRegion + AppShell + Snackbar（2026-09-23 移除无消费者的 DialogHost 包裹层），`ContentRegion` 保持唯一，业务 View 零改动。

### 13.4 对话框（Dialog）规范

**口径**（与 §13.5 同源）：Dialog = 经 `containerRegistry.RegisterDialog<TView, TViewModel>()` 注册的模态对话框（Shell 3 个位于 `Shell/Dialogs/Views/`，模块 4 个位于 `{模块}/Dialogs/`，另有 2 个物理位于 `Modules/LYBT.Desktop.Auth/Views/` 但按对话框注册）。代码实际共 **9 处注册**。

| # | 对话框 | 注册点 | View / ViewModel | 用途 | 需求依据 |
|---|--------|--------|------------------|------|----------|
| 1 | ConfirmationDialog | `Shell/App.xaml.cs` | `Shell/Dialogs/Views/ConfirmationDialog.xaml` + `Shell/Dialogs/ViewModels/ConfirmationDialogViewModel.cs` | 通用确认：标题/消息/图标/按钮文案可配；`ShowDeleteOptions=true` 时附软删除（默认）/物理删除二选一；确认 → `ButtonResult.OK`（删除模式经参数回传），取消 → `ButtonResult.Cancel` | 破坏性操作需确认（`docs/07-ui-ux/desktop-design-spec.md` 容错原则）；统一入口 `DialogManager.ShowConfirmAsync` |
| 2 | MessageDialog | `Shell/App.xaml.cs` | `Shell/Dialogs/Views/MessageDialog.xaml` + `Shell/Dialogs/ViewModels/MessageDialogViewModel.cs` | Success/Error/Warning/Info 四类统一消息（`MessageType`）；关闭 → `ButtonResult.OK` | 统一消息出口；`DialogManager` 常量 `MessageDialogName = "MessageDialog"` |
| 3 | InputDialog | `Shell/App.xaml.cs` | `Shell/Dialogs/Views/InputDialog.xaml` + `Shell/Dialogs/ViewModels/InputDialogViewModel.cs` | 单值输入：入参 `message`/`title`/`defaultValue`/`placeholder`/`isRequired`，确认 → `ButtonResult.OK` + 输入值参数 | 通用输入基建；代码实际暂无 `ShowDialog("InputDialog")` 调用点（预留） |
| 4 | FormulaImportDialog | `Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs` | `Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialog.xaml` + `.../FormulaImportDialogViewModel.cs` | 从经验方库检索选择验方，批量导入药材到当前处方（只读 DTO 展示，B3 决策 DP-M1 豁免）；确认 → `ButtonResult.OK` + 导入结果参数 | 医案「验方导入到处方」（`docs/02-requirements/07-medical-cases.md`，原 US-MC-016）+ MC-D08（仅 `Validated` 且 `Enabled`）/MC-D09（已禁用药材跳过）/MC-D12（导入为复制）/MC-D17（重复剂量合并策略） |
| 5 | HistoryCopyDialog | `MedicalCaseModule.cs` | `Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml` + `.../HistoryCopyDialogViewModel.cs` | 左历史 / 右当前双栏复制处方；入参 `PatientId`/`PatientName`，默认最近 5 条已完成记录、可展开本患者全部、可切「全部患者」查询；确认 → `ButtonResult.OK` + 复制结果 | US-MC-019（复用 US-MC-009 处方历史），`docs/02-requirements/07-medical-cases.md` |
| 6 | UnsavedChangesDialog | `MedicalCaseModule.cs` | `Modules/LYBT.Desktop.MedicalCase/Dialogs/UnsavedChangesDialog.xaml` + `.../UnsavedChangesDialogViewModel.cs` | 未保存修改三选项：保存 → `ButtonResult.Yes`、放弃 → `ButtonResult.No`、取消 → `ButtonResult.Cancel`（默认分支） | 未保存退出守卫（`IConfirmNavigationRequest`，见 `docs/07-ui-ux/desktop-design-spec.md`）+ MC-D18（不实现自动保存） |
| 7 | RegistrationCreateDialog | `Modules/LYBT.Desktop.Registrations/RegistrationModule.cs` | `Modules/LYBT.Desktop.Registrations/Dialogs/RegistrationCreateDialog.xaml` + `.../RegistrationCreateDialogViewModel.cs` | 前台新建挂号（患者 + 医生 + 挂号类型 + 费用）；成功 → `ButtonResult.OK`，调用方 `RegistrationListViewModel` 据此刷新队列 | US-REG-001（前台创建挂号 Waiting 排队） |
| 8 | ServerConfigView | `Modules/LYBT.Desktop.Auth/AuthenticationModule.cs` | `Modules/LYBT.Desktop.Auth/Views/ServerConfigView.xaml` + `.../ViewModels/ServerConfigViewModel.cs` | 服务器地址配置 + 连接测试（状态机收敛于 `ConnectionTestViewModelBase`） | US-SHELL-007（双模式连接切换）；`docs/03-architecture/05-dual-mode.md` |
| 9 | FirstRunSetupView | `AuthenticationModule.cs` | `Modules/LYBT.Desktop.Auth/Views/FirstRunSetupView.xaml` + `.../ViewModels/FirstRunSetupViewModel.cs` | 首次运行配置向导（当前 Steps 1-2 完整），由登录页 `LoginViewModel` 触发 | US-SHELL-011（5 步强制向导；B4 决策 I-4 推迟到 v2.0）；`docs/07-ui-ux/desktop-ui-requirements.md` §1.3 |

**基类与生命周期契约**（`Core/LYBT.Desktop.Infrastructure/ViewModels/Base/DialogViewModelBase.cs`，声明为 `DialogViewModelBase : NavigableViewModelBase, IDialogAware`）：

| 成员 | 契约 |
|------|------|
| `Title` | `[ObservableProperty]` 对话框标题；子类在构造函数或 `OnDialogOpenedCore` 中赋值 |
| `OnDialogOpened(IDialogParameters)` | Prism 回调入口；基类记日志后转调 `OnDialogOpenedCore(parameters)` |
| `OnDialogOpenedCore(IDialogParameters?)` | 子类重写点：读入参并初始化状态（默认空实现） |
| `CanCloseDialog()` | 默认 `true`；重写可拦截关闭 |
| `OnDialogClosed()` | Prism 回调入口；基类记日志后转调 `OnDialogClosedCore()` |
| `OnDialogClosedCore()` | 子类重写点：释放资源/通知宿主（默认空实现） |
| `RequestClose` | `event Action<IDialogResult>?`——**唯一**关闭通道，子类不直接操作窗口 |
| `CloseDialog(ButtonResult)` / `CloseDialog(IDialogParameters, ButtonResult = ButtonResult.OK)` | 关闭并回传结果（无参重载默认 `ButtonResult.None`）；多值回传一律走带参重载 |
| `CancelCommand` / `ConfirmCommand` | 基类内置：`Cancel()` → `ButtonResult.Cancel`；`Confirm()` → `ButtonResult.OK`，可执行条件 `CanConfirm()` 默认 `!IsBusy && !IsLoading`，且 `IsLoading`/`IsBusy` 变更时自动 `NotifyCanExecuteChanged` |
| `GetDialogParameter<T>(parameters, key)` / `(…, defaultValue)` | 入参提取；无默认值重载在缺失或为 `null` 时抛 `ArgumentException` |

约定：

- 新业务对话框统一继承 `DialogViewModelBase`；现网唯一中间基类是 Auth 的 `ConnectionTestViewModelBase : DialogViewModelBase`（收敛 `ServerConfigViewModel`/`FirstRunSetupViewModel` 约 95% 同构的连接测试状态机，A-31-C5-4）。
- 目录：模块对话框放 `Modules/{模块}/Dialogs/`（XAML 与 `...DialogViewModel.cs` 同目录、均**不**在 `Views/`）；Shell 通用对话框放 `Shell/Dialogs/Views/` + `Shell/Dialogs/ViewModels/`。
- 结果语义：`ButtonResult.OK` = 已确认/已提交，宿主据此执行业务动作（如 `RegistrationListViewModel` 刷新队列、`MedicalCaseCommandsViewModel` 执行导入/复制）；`Cancel`/`None` = 放弃。

**DataContext 装配（无需在 XAML 写 AutoWire）**：

- `RegisterDialog<TView, TViewModel>()` 装配容器时同时登记 View→VM 映射；`IDialogService.ShowDialog(name, parameters, callback)` 由 Prism `DialogService` 创建窗口内容后经 `ConfigureDialogWindowContent → MvvmHelpers.AutowireViewModel(dialogContent)`（Prism.Wpf 8.1.97）**自动**把 DataContext 设为已注册的 VM，并断言 `dialogContent.DataContext is IDialogAware`（不满足即抛异常，不会静默失效）。
- 因此对话框 XAML **不需要**（也不应依赖）`prism:ViewModelLocator.AutoWireViewModel="True"`；代码实际中仅 `InputDialog.xaml`/`MessageDialog.xaml` 保留了该属性（冗余、无害），`ConfirmationDialog.xaml` 与 4 个模块对话框均未声明。
- 与 §13.2 的 Shell 控件映射问题（`ShellViewMappings`）不同：对话框走「注册即映射」路径，不存在约定名不匹配导致 AW 静默跳过赋值的风险。

### 13.5 数量口径与视图/VM 清单校验

**为什么固定口径**：本文件历史版本只谈分层写法，未定义「View/Control/Dialog」的计数边界，导致同一仓库在不同文档出现互相矛盾的数字。以下口径与数字是 **2026-09-13** 对代码实际（`src/Client/Desktop`，排除 `bin/`、`obj/`）机器扫描的结果；任何引用都必须连同口径一起声明。

| 类别 | 判定规则 | 计数 |
|------|----------|------|
| View（页面/导航级） | `*/Views/*.xaml`：`Shell/Views/`、`Modules/*/Views/`、`Roles/*/Views/`、`Roles/*/*/Views/`、`Modules/*/Reports/Views/`——**排除** `*/Dialogs/Views/*`（那 3 个归 Dialog） | **30** |
| Control（内嵌组件） | `*/Controls/*.xaml`：`Core/LYBT.Desktop.Controls/Controls/`（共享设计系统控件）与各模块 `Controls/`（含 `Shell/Controls/AccountSettingsControl`） | **33** |
| Dialog（模态对话框） | `*/Dialogs/**/*.xaml`（经 `RegisterDialog` 注册） | **7** |
| Root | `Shell/App.xaml`（应用级资源，非视图） | **1** |
| 资源/模板 XAML | `Core/LYBT.Desktop.Controls/Themes/*`、`Core/LYBT.Desktop.Controls/Converters/*`、`Core/LYBT.Desktop.Printing/Templates/*` | **11** |
| ViewModel | 文件名以 `ViewModel.cs` 结尾的 C# 文件，**每文件 1 个 VM 类型**——含模块 `*/Dialogs/*ViewModel.cs`（4 个）、Shell `Shell/Dialogs/ViewModels/*.cs`（3 个）与 `Core/LYBT.Desktop.Controls/Controls/**` 内的控件 VM（2 个）；`ViewModels/Handlers/` 下的 Handler 类与 `obj/` 生成物不计入 | **55** |

**合计校验**：XAML 合计 **82** = 视图 **71**（View 30 + Control 33 + Dialog 7 + Root 1）+ 资源/模板 **11**。

**边界（口径必须显式声明的两条）**：

- `Modules/LYBT.Desktop.Auth/Views/ServerConfigView.xaml` 与 `FirstRunSetupView.xaml` 物理在 `Views/`（按路径计为 View），但经 `RegisterDialog` 按**对话框**注册——§13.4 的 9 条注册清单中这 2 条与 View 计数**重叠，不重复计数**。
- `Shell/Views/HeaderControl.xaml`、`SideNavControl.xaml`、`FooterControl.xaml` 按路径归 View（Shell 区域），与 §13.1 组件清单中的 VM 一一对应。

**校验方法**（可复现，无需构建）：以 `src/Client/Desktop` 为根递归枚举 `*.xaml`，排除 `bin/`、`obj/`，按路径归类计数：

```powershell
Get-ChildItem src/Client/Desktop -Recurse -Filter *.xaml |
  Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
  Group-Object { if     ($_.FullName -match '\\Dialogs\\')   { 'Dialog' }
                 elseif ($_.FullName -match '\\Controls\\')  { 'Control' }
                 elseif ($_.FullName -match '\\Themes\\|\\Converters\\|\\Templates\\') { 'Resource' }
                 elseif ($_.Name -eq 'App.xaml')             { 'Root' }
                 else                                        { 'View' } } |
  Sort-Object Name | Format-Table Name, Count
```

ViewModel 同理按「文件名以 `ViewModel.cs` 结尾、排除 `bin/`/`obj/`、每文件 1 个类型」口径枚举计数（结果为 55）。

**守护测试**（`tests/LYBT.Tests.Architecture/`）：

- `ShellViewViewModelBindingTests.Shell_AutoWireViews_Must_Resolve_ViewModel_By_Convention_Or_Explicit_Mapping`：扫描 Shell XAML 中开启 `AutoWireViewModel="True"` 的视图，断言其 VM 可经 Prism 约定名或 `ShellViewMappings.Mappings` 解析——新增 AW 控件漏登记即失败。
- `ShellViewViewModelBindingTests.Shell_Mappings_Must_Be_Concrete_ViewModels_In_Shell_Assembly`：校验映射表目标必须是 Shell 程序集内的具体 VM 类型。
- 同项目 `DesktopLayerArchTests`（ViewModel 必须继承标准基类，`DialogViewModelBase` 在允许清单内）与 `ShellExtractionArchTests` 提供相邻约束。

**本轮修正的文档-代码落差**（同一事实源）：

| 落差 | 文档旧表述 | 代码实际（src/Client/Desktop） |
|------|-----------|------------------------------|
| 侧栏定位 | 「侧边栏仅保留全局操作（个人资料/主题/退出），不放功能导航」 | `SideNavControl` 绑定 `GroupedNavigationItems` = `NavigationManager.BuildNavigationItems` **按角色生成的角色导航矩阵**（每个角色 **3 项** = 标题「主页」+ 2 个业务入口；`NavigationItem.Group` 取值为「临床」/「目录」/「管理」——模型默认值「业务」未被 `NavigationManager` 使用）+ 底部全局操作（深色模式/退出）；**个人资料入口在顶栏 Header**（`HeaderViewModel.EditProfileCommand`）。对应 US-SHELL-005（菜单导航）/ US-SHELL-004（个人资料） |
| Doctor 首页 | `ClinicalHomeView` | `RoleRegistry` 注册 **Doctor 首页 = `ClinicalWorkspaceView`**；`ClinicalHomeView` 仍存在但**不是**首页 |
| Receptionist 新建挂号 | `RegistrationCreateView`（或可弹窗） | `RegistrationCreateDialog`（对话框，见 §13.4 #7，US-REG-001） |
| 幽灵视图 | `InitializationWizardView`(W-01)、`CardReaderDiagnosticsView`(SY-05)、`ConfigExportImportView`(SY-07)、`ServerConfigPanelView`(SY-08)、`SessionTimeoutWarningDialog`(AD-01)、`UnfinishedCaseDialog`(DD-04)、`PrintPreviewDialog`(DD-05)、`PendingQueueView`(DOC-02) | 前七者全部 `[未建视图]`：SY-05/SY-07/SY-08 的 VM（`CardReaderDiagnosticsViewModel`/`ConfigurationCenterViewModel`/`ServerConfigSectionViewModel`）**存在**，但是 `SysadminHomeView`（US-SHELL-018）的内嵌子 VM，非独立视图；`PendingQueueView` 的 XAML 已于 2026-08-29 删除，其功能内嵌 `PatientSelectionView`，`PendingQueueViewModel` 保留为子组件 |
| 计数 | 外部文档曾引「25 View + 24 Control + 7 Dialog + 51+ ViewModel」 | 以本节口径为准：**View 30 / Control 33 / Dialog 7 / ViewModel 55** |
| 品牌名 | 混用「LYBT 诊疗管理系统」等 | 系统名统一 **「凌隐宝堂中医诊所管理系统」**（WebAPI Swagger 标题「凌隐宝堂中医诊所 API」） |

---

## 附录

### A. 参考文档

- `docs/03-architecture/16-desktop-architecture-spec.md` - Desktop 端架构规范（视图/VM/对话框/绑定）
- `docs/03-architecture/02-desktop.md` - Desktop 端架构设计（视图/对话框/交互流程）
- `docs/07-ui-ux/desktop-layout-framework.md` - Shell 布局框架（§13 依据，固化）
- `docs/07-ui-ux/desktop-design-spec.md` - Desktop 设计规范（容错/对话框/未保存退出）
- `docs/05-development/02-code-standards.md` - 编码规范（C#/XAML 强制约定）
- `docs/05-development/standards/README.md` - 技术标准索引（STD-01 CQRS 边界 … STD-06 JWT 安全）

### B. 相关 ADR

- ADR-001: 拒绝过度工程化
- ADR-002: Desktop.Services 层移除决策（Repository 注册位置）
- ADR-004: 服务接口统一设计标准

### C. 联系方式

如有疑问或建议，请在 GitHub 上提交 Issue。

---

**文档版本历史**:
- v1.0 (2025-10-12): 初始版本，涵盖完整的 Desktop 三层架构设计标准
- v1.1 (2026-09-13): 本次文档复盘——新增 §1.4「示例代码约定」、§13.4「对话框（Dialog）规范」（9 处 `RegisterDialog` 注册清单 + `DialogViewModelBase` 生命周期契约 + Prism 对话框 DataContext 自动装配）与 §13.5「数量口径与视图/VM 清单校验」（View 30 / Control 33 / Dialog 7 / Root 1 / ViewModel 55，XAML 82 = 71 + 11；目录扫描校验方法 + 守护测试 `ShellViewViewModelBindingTests`）；修正幽灵视图引用（`UserDetailView`/`UserDetailViewModel`/`UserManagementViewModel`/`UserEditorDialog`/`PatientMasterDetailView`/`ConsultationPanelViewModel` 等标 `[未建视图]`/`[未建 VM]`）、侧栏定位表述、Doctor 首页与品牌名口径。
