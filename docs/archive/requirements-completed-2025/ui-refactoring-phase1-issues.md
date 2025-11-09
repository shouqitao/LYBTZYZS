# Desktop端UI重构 Phase 1 - GitHub Issues清单

**Epic**: Desktop端UI/UX重构
**Phase**: Phase 1 - 技术债务清理
**创建日期**: 2025-11-04
**总Issues数量**: 7个

---

## 📋 Epic Issue

### Epic: Desktop端UI重构 - Phase 1 技术债务清理

**标题**: `Epic: Desktop端UI重构 Phase 1 - 技术债务清理`

**描述**:
```markdown
## 背景
当前Desktop端存在39个XAML视图，其中多个界面功能重复、定位模糊，增加了维护成本和用户导航复杂度。Phase 1重点清理技术债务，删除冗余界面，简化UI结构。

## 目标
- UI文件数量: 39 → 34 (-13%)
- 代码维护成本: -30%
- 用户导航混乱度: -50%

## 范围
- 删除5个冗余XAML文件组（共14个文件）
- 新增1个统一用户表单对话框
- 更新导航路由配置
- 更新单元测试

## 子任务（Issues）
- #XXXX 合并用户创建和编辑界面
- #XXXX 删除医案管理冗余界面
- #XXXX 删除诊疗记录独立管理界面
- #XXXX 删除处方管理冗余主界面
- #XXXX 删除验方查看对话框
- #XXXX 更新导航路由和菜单配置
- #XXXX 更新单元测试

## 参考文档
- PRD: `docs/requirements/ui-refactoring-phase1-prd.md`
- 重构方案: `docs/reports/ui-ux-refactoring-plan-2025-11-04.md`
- ADR-009: `docs/explanation/architecture/decisions/ADR-009-desktop-component-pattern.md`

## 预计工期
1.5周 (7.5个工作日)

## 验收标准
- [ ] 所有子Issue已完成
- [ ] 单元测试覆盖率≥80%
- [ ] 回归测试通过率≥95%
- [ ] Code Review通过
- [ ] 文档已更新
```

**标签**: `epic`, `ui-refactoring`, `phase-1`, `desktop`, `P0`
**优先级**: P0 (Critical)
**预估**: 60小时

---

## 🎯 Issue #1: 合并用户创建和编辑界面

### 标题
`refactor(users): 合并UserCreate和UserEdit为统一UserFormDialog`

### 描述
```markdown
## 问题
UserCreateView和UserEditView两个界面95%代码重复，违反DRY原则，维护成本高。

## 解决方案
删除UserCreateView和UserEditView，新增UserFormDialog支持Create/Edit两种模式。

## 技术细节

### 删除文件
- `LYBT.Desktop.Users/Views/UserCreateView.xaml`
- `LYBT.Desktop.Users/Views/UserCreateView.xaml.cs`
- `LYBT.Desktop.Users/Views/UserEditView.xaml`
- `LYBT.Desktop.Users/Views/UserEditView.xaml.cs`
- `LYBT.Desktop.Users/ViewModels/UserCreateViewModel.cs`
- `LYBT.Desktop.Users/ViewModels/UserEditViewModel.cs`

### 新增文件
- `LYBT.Desktop.Users/Views/UserFormDialog.xaml`
- `LYBT.Desktop.Users/Views/UserFormDialog.xaml.cs`
- `LYBT.Desktop.Users/ViewModels/UserFormDialogViewModel.cs`
- `tests/UnitTests/Client/Desktop/Users/UserFormDialogViewModelTests.cs`

### 实现要点

#### UserFormDialogViewModel参数设计
```csharp
public class UserFormDialogViewModel : IDialogAware
{
    // 参数
    // mode: "create" | "edit"
    // userId?: Guid (编辑模式必传)

    public void OnDialogOpened(IDialogParameters parameters)
    {
        var mode = parameters.GetValue<string>("mode");
        if (mode == "create")
        {
            Title = "创建用户";
            SubmitButtonText = "创建";
            // 初始化空表单
        }
        else if (mode == "edit")
        {
            Title = "编辑用户";
            SubmitButtonText = "保存";
            var userId = parameters.GetValue<Guid>("userId");
            // 加载用户数据
            await LoadUserAsync(userId);
        }
    }
}
```

#### UserManagementViewModel调用更新
```csharp
// 创建用户
private void OnCreateUser()
{
    var parameters = new DialogParameters { { "mode", "create" } };
    _dialogService.ShowDialog("UserFormDialog", parameters, OnDialogClosed);
}

// 编辑用户
private void OnEditUser(User user)
{
    var parameters = new DialogParameters
    {
        { "mode", "edit" },
        { "userId", user.Id }
    };
    _dialogService.ShowDialog("UserFormDialog", parameters, OnDialogClosed);
}

private void OnDialogClosed(IDialogResult result)
{
    if (result.Result == ButtonResult.OK)
    {
        LoadUsers(); // 刷新列表
    }
}
```

#### Module注册更新
```csharp
// LYBT.Desktop.Users/UsersModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 删除
    // containerRegistry.RegisterForNavigation<UserCreateView>();
    // containerRegistry.RegisterForNavigation<UserEditView>();

    // 新增
    containerRegistry.RegisterDialog<UserFormDialog, UserFormDialogViewModel>();
}
```

## 验收标准
- [ ] UserCreateView.xaml和UserEditView.xaml已删除
- [ ] UserFormDialog.xaml实现，支持Create/Edit两种模式
- [ ] UserManagementView调用更新为DialogService
- [ ] 单元测试通过（Create/Edit场景覆盖）
- [ ] 回归测试：用户创建和编辑功能正常
- [ ] Code Review通过

## 测试用例
- [ ] 创建模式：空表单，标题"创建用户"，按钮"创建"
- [ ] 编辑模式：加载用户数据，标题"编辑用户"，按钮"保存"
- [ ] 验证：必填字段验证，用户名重复检查
- [ ] 提交：Create调用CreateAsync，Edit调用UpdateAsync
- [ ] 取消：关闭对话框，不刷新列表
```

**标签**: `refactor`, `desktop`, `users`, `ui-cleanup`, `P0`
**优先级**: P0
**预估**: 6小时
**Epic**: #XXXX (Epic Issue编号)

---

## 🎯 Issue #2: 删除医案管理冗余界面

### 标题
`refactor(medicalcase): 删除MedicalCaseList和OtherCasesQuery冗余界面`

### 描述
```markdown
## 问题
MedicalCaseManagementView、MedicalCaseListView、OtherCasesQueryView三个界面功能重叠，用户不清楚应该使用哪个。

## 解决方案
保留MedicalCaseManagementView作为唯一入口，删除List和OtherCases界面。

## 技术细节

### 删除文件
- `LYBT.Desktop.MedicalCase/Views/MedicalCaseListView.xaml`
- `LYBT.Desktop.MedicalCase/Views/MedicalCaseListView.xaml.cs`
- `LYBT.Desktop.MedicalCase/Views/OtherCasesQueryView.xaml`
- `LYBT.Desktop.MedicalCase/Views/OtherCasesQueryView.xaml.cs`
- `LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseListViewModel.cs`
- `LYBT.Desktop.MedicalCase/ViewModels/OtherCasesQueryViewModel.cs`

### 保留并增强
- `LYBT.Desktop.MedicalCase/Views/MedicalCaseManagementView.xaml`
  - 功能: 查询、筛选、分页、创建、查看、删除

### Module注册更新
```csharp
// LYBT.Desktop.MedicalCase/MedicalCaseModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 删除
    // containerRegistry.RegisterForNavigation<MedicalCaseListView>();
    // containerRegistry.RegisterForNavigation<OtherCasesQueryView>();

    // 保留
    containerRegistry.RegisterForNavigation<MedicalCaseManagementView>();
}
```

### 主菜单更新
```xml
<!-- MainWindow.xaml或MenuView.xaml -->
<!-- 删除 -->
<!-- <MenuItem Header="其他病案查询" Command="{Binding NavigateCommand}" CommandParameter="OtherCasesQueryView" /> -->

<!-- 保留 -->
<MenuItem Header="医案管理" Command="{Binding NavigateCommand}" CommandParameter="MedicalCaseManagementView" />
```

## 架构合规性检查
根据AR-001聚合根约束：
- ✅ MedicalCase是聚合根
- ✅ 通过Patient聚合根查询医案（PatientId筛选）
- ❌ OtherCasesQueryView违反约束（直接查询非本患者医案）

## 验收标准
- [ ] MedicalCaseListView.xaml已删除
- [ ] OtherCasesQueryView.xaml已删除
- [ ] MedicalCaseManagementView功能完整
- [ ] 主菜单无死链接
- [ ] 单元测试通过
- [ ] 回归测试：医案查询、创建、查看功能正常
```

**标签**: `refactor`, `desktop`, `medicalcase`, `ui-cleanup`, `architecture`, `P0`
**优先级**: P0
**预估**: 4小时
**Epic**: #XXXX

---

## 🎯 Issue #3: 删除诊疗记录独立管理界面

### 标题
`refactor(consultation): 删除独立管理界面，强制聚合根约束`

### 描述
```markdown
## 问题
ConsultationManagementView违反AR-001聚合根约束，诊疗记录应该只能通过MedicalCase聚合根访问。

## 架构约束 (AR-001)
```
约束内容:
  - Consultation是MedicalCase的聚合子实体
  - 写操作必须通过MedicalCase聚合根
  - 禁止直接访问Consultation进行独立管理

违规后果:
  - 数据不一致（Consultation与MedicalCase状态脱节）
  - 破坏聚合根边界
```

## 解决方案
删除ConsultationManagementView，只保留ConsultationFormView在MedicalCase上下文中使用。

## 技术细节

### 删除文件
- `LYBT.Desktop.Consultation/Views/ConsultationManagementView.xaml`
- `LYBT.Desktop.Consultation/Views/ConsultationManagementView.xaml.cs`
- `LYBT.Desktop.Consultation/ViewModels/ConsultationManagementViewModel.cs`

### 保留文件
- `LYBT.Desktop.Consultation/Views/ConsultationFormView.xaml` (仅在MedicalCaseFlowView中使用)

### Module注册更新
```csharp
// LYBT.Desktop.Consultation/ConsultationModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 删除
    // containerRegistry.RegisterForNavigation<ConsultationManagementView>();

    // 保留（仅在MedicalCase上下文中使用）
    containerRegistry.RegisterForNavigation<ConsultationFormView>();
}
```

### 导航限制
- ConsultationFormView只能从MedicalCaseFlowView导航
- 主菜单和其他模块禁止直接访问

### 主菜单更新
```xml
<!-- 删除 -->
<!-- <MenuItem Header="诊疗记录管理" Command="{Binding NavigateCommand}" CommandParameter="ConsultationManagementView" /> -->
```

## 验收标准
- [ ] ConsultationManagementView.xaml已删除
- [ ] ConsultationFormView保留，只在MedicalCaseFlowView中使用
- [ ] 主菜单和所有导航路由中无ConsultationManagement入口
- [ ] 单元测试通过
- [ ] 回归测试：诊疗记录创建和查看功能正常（通过医案）
- [ ] 架构合规性：符合AR-001约束
```

**标签**: `refactor`, `desktop`, `consultation`, `architecture`, `aggregate-root`, `P0`
**优先级**: P0
**预估**: 3小时
**Epic**: #XXXX
**相关ADR**: ADR-005, ADR-006

---

## 🎯 Issue #4: 删除处方管理冗余主界面

### 标题
`refactor(prescriptions): 删除PrescriptionsMainView冗余界面`

### 描述
```markdown
## 问题
PrescriptionsMainView和PrescriptionManagementView功能重叠，用户不清楚应该使用哪个。

## 解决方案
删除PrescriptionsMainView，保留PrescriptionManagementView作为唯一入口。

## 技术细节

### 删除文件
- `LYBT.Desktop.Prescriptions/Views/PrescriptionsMainView.xaml`
- `LYBT.Desktop.Prescriptions/Views/PrescriptionsMainView.xaml.cs`
- `LYBT.Desktop.Prescriptions/ViewModels/PrescriptionsMainViewModel.cs`

### 保留并增强
- `LYBT.Desktop.Prescriptions/Views/PrescriptionManagementView.xaml`
  - 功能: 查询、筛选、分页、创建、编辑、打印
- `LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml`
  - 功能: 只读详情，打印预览

### Module注册更新
```csharp
// LYBT.Desktop.Prescriptions/PrescriptionsModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 删除
    // containerRegistry.RegisterForNavigation<PrescriptionsMainView>();

    // 保留
    containerRegistry.RegisterForNavigation<PrescriptionManagementView>();
    containerRegistry.RegisterForNavigation<PrescriptionView>();
}
```

### 主菜单更新
```xml
<!-- 统一入口 -->
<MenuItem Header="处方管理" Command="{Binding NavigateCommand}" CommandParameter="PrescriptionManagementView" />
```

## 验收标准
- [ ] PrescriptionsMainView.xaml已删除
- [ ] PrescriptionManagementView功能完整（包含原Main功能）
- [ ] PrescriptionView保留，只读功能正常
- [ ] 主菜单导航更新为PrescriptionManagementView
- [ ] 单元测试通过
- [ ] 回归测试：处方管理、查看、打印功能正常
```

**标签**: `refactor`, `desktop`, `prescriptions`, `ui-cleanup`, `P0`
**优先级**: P0
**预估**: 3小时
**Epic**: #XXXX

---

## 🎯 Issue #5: 删除验方查看对话框

### 标题
`refactor(formula): 删除ViewFormulaDialog，FormulaDetailView支持双模式`

### 描述
```markdown
## 问题
FormulaDetailView和ViewFormulaDialog都是只读查看功能，完全重复。

## 解决方案
删除ViewFormulaDialog，扩展FormulaDetailView支持view/edit两种模式。

## 技术细节

### 删除文件
- `LYBT.Desktop.Formula/Views/ViewFormulaDialog.xaml`
- `LYBT.Desktop.Formula/Views/ViewFormulaDialog.xaml.cs`
- `LYBT.Desktop.Formula/ViewModels/ViewFormulaDialogViewModel.cs`

### 增强文件
- `LYBT.Desktop.Formula/Views/FormulaDetailView.xaml`
- `LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs`

### 实现要点

#### FormulaDetailViewModel参数设计
```csharp
public class FormulaDetailViewModel : BindableBase, INavigationAware
{
    // 参数
    // mode: "view" | "edit"
    // formulaId: Guid

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        var mode = navigationContext.Parameters.GetValue<string>("mode");
        var formulaId = navigationContext.Parameters.GetValue<Guid>("formulaId");

        IsReadOnly = (mode == "view");

        if (IsReadOnly)
        {
            // 隐藏保存按钮
            SaveButtonVisibility = Visibility.Collapsed;
        }
        else
        {
            // 显示保存按钮
            SaveButtonVisibility = Visibility.Visible;
        }

        await LoadFormulaAsync(formulaId);
    }
}
```

#### 调用方更新
```csharp
// 查看模式
var parameters = new NavigationParameters
{
    { "mode", "view" },
    { "formulaId", formulaId }
};
_regionManager.RequestNavigate("ContentRegion", "FormulaDetailView", parameters);

// 编辑模式
var parameters = new NavigationParameters
{
    { "mode", "edit" },
    { "formulaId", formulaId }
};
_regionManager.RequestNavigate("ContentRegion", "FormulaDetailView", parameters);
```

### Module注册更新
```csharp
// LYBT.Desktop.Formula/FormulaModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 删除
    // containerRegistry.RegisterForNavigation<ViewFormulaDialog>();

    // 保留（支持双模式）
    containerRegistry.RegisterForNavigation<FormulaDetailView>();
}
```

## 验收标准
- [ ] ViewFormulaDialog.xaml已删除
- [ ] FormulaDetailView支持view/edit两种模式
- [ ] view模式：所有字段只读，无保存按钮
- [ ] edit模式：字段可编辑，显示保存按钮
- [ ] 所有调用ViewFormulaDialog的地方改为FormulaDetailView
- [ ] 单元测试通过
- [ ] 回归测试：验方查看和编辑功能正常
```

**标签**: `refactor`, `desktop`, `formula`, `ui-cleanup`, `P0`
**优先级**: P0
**预估**: 2小时
**Epic**: #XXXX

---

## 🎯 Issue #6: 更新导航路由和菜单配置

### 标题
`refactor(navigation): 更新导航路由和主菜单配置`

### 描述
```markdown
## 目标
更新所有模块的导航路由配置和主菜单，删除已废弃界面的入口，确保无死链接。

## 技术细节

### Module.cs文件更新（5个模块）

#### LYBT.Desktop.Users/UsersModule.cs
```csharp
// 删除
// containerRegistry.RegisterForNavigation<UserCreateView>();
// containerRegistry.RegisterForNavigation<UserEditView>();

// 新增
containerRegistry.RegisterDialog<UserFormDialog, UserFormDialogViewModel>();
```

#### LYBT.Desktop.MedicalCase/MedicalCaseModule.cs
```csharp
// 删除
// containerRegistry.RegisterForNavigation<MedicalCaseListView>();
// containerRegistry.RegisterForNavigation<OtherCasesQueryView>();
```

#### LYBT.Desktop.Consultation/ConsultationModule.cs
```csharp
// 删除
// containerRegistry.RegisterForNavigation<ConsultationManagementView>();
```

#### LYBT.Desktop.Prescriptions/PrescriptionsModule.cs
```csharp
// 删除
// containerRegistry.RegisterForNavigation<PrescriptionsMainView>();
```

#### LYBT.Desktop.Formula/FormulaModule.cs
```csharp
// 删除
// containerRegistry.RegisterForNavigation<ViewFormulaDialog>();
```

### 主菜单更新

**文件**: `LYBT.Desktop.Shell/Views/MainWindow.xaml` (或MenuView.xaml)

```xml
<!-- 删除以下菜单项 -->
<!--
<MenuItem Header="医案列表" Command="{Binding NavigateCommand}" CommandParameter="MedicalCaseListView" />
<MenuItem Header="其他病案查询" Command="{Binding NavigateCommand}" CommandParameter="OtherCasesQueryView" />
<MenuItem Header="诊疗记录管理" Command="{Binding NavigateCommand}" CommandParameter="ConsultationManagementView" />
<MenuItem Header="处方主界面" Command="{Binding NavigateCommand}" CommandParameter="PrescriptionsMainView" />
-->

<!-- 确保保留的菜单项正确 -->
<MenuItem Header="用户管理" Command="{Binding NavigateCommand}" CommandParameter="UserManagementView" />
<MenuItem Header="医案管理" Command="{Binding NavigateCommand}" CommandParameter="MedicalCaseManagementView" />
<MenuItem Header="处方管理" Command="{Binding NavigateCommand}" CommandParameter="PrescriptionManagementView" />
<MenuItem Header="验方管理" Command="{Binding NavigateCommand}" CommandParameter="FormulaManagementView" />
```

### 快捷导航更新

**文件**: `LYBT.Desktop.Shell/ViewModels/ShellViewModel.cs` (如有快捷按钮)

```csharp
// 删除已废弃View的快捷导航
// private void NavigateToOtherCases() { ... }

// 确保保留的导航正确
private void NavigateToMedicalCaseManagement()
{
    _regionManager.RequestNavigate("MainRegion", "MedicalCaseManagementView");
}
```

## 验收标准
- [ ] 5个Module.cs文件已更新，删除废弃View注册
- [ ] 主菜单已更新，删除废弃菜单项
- [ ] 所有快捷导航已更新
- [ ] 编译通过，无未注册的View引用错误
- [ ] 手动测试：所有菜单项可点击，无死链接
- [ ] 手动测试：导航到正确的View
```

**标签**: `refactor`, `desktop`, `navigation`, `configuration`, `P0`
**优先级**: P0
**预估**: 4小时
**Epic**: #XXXX

---

## 🎯 Issue #7: 更新单元测试

### 标题
`test(desktop): 更新单元测试，覆盖重构变更`

### 描述
```markdown
## 目标
- 删除已废弃ViewModel的单元测试
- 新增UserFormDialogViewModel单元测试
- 更新调用方ViewModel的单元测试（导航调用变更）
- 确保测试覆盖率≥80%

## 技术细节

### 删除测试文件
- `tests/UnitTests/Client/Desktop/Users/UserCreateViewModelTests.cs`
- `tests/UnitTests/Client/Desktop/Users/UserEditViewModelTests.cs`
- `tests/UnitTests/Client/Desktop/MedicalCase/MedicalCaseListViewModelTests.cs`
- `tests/UnitTests/Client/Desktop/MedicalCase/OtherCasesQueryViewModelTests.cs`
- `tests/UnitTests/Client/Desktop/Consultation/ConsultationManagementViewModelTests.cs`
- `tests/UnitTests/Client/Desktop/Prescriptions/PrescriptionsMainViewModelTests.cs`
- `tests/UnitTests/Client/Desktop/Formula/ViewFormulaDialogViewModelTests.cs`

### 新增测试文件
- `tests/UnitTests/Client/Desktop/Users/UserFormDialogViewModelTests.cs`

### UserFormDialogViewModelTests实现

```csharp
public class UserFormDialogViewModelTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IDialogService> _dialogService;
    private readonly UserFormDialogViewModel _viewModel;

    public UserFormDialogViewModelTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _dialogService = new Mock<IDialogService>();
        _viewModel = new UserFormDialogViewModel(_userRepository.Object, _dialogService.Object);
    }

    [Fact]
    public void OnDialogOpened_CreateMode_ShouldInitializeEmptyForm()
    {
        // Arrange
        var parameters = new DialogParameters { { "mode", "create" } };

        // Act
        _viewModel.OnDialogOpened(parameters);

        // Assert
        Assert.Equal("创建用户", _viewModel.Title);
        Assert.Equal("创建", _viewModel.SubmitButtonText);
        Assert.Null(_viewModel.UserName);
        Assert.Null(_viewModel.RealName);
    }

    [Fact]
    public void OnDialogOpened_EditMode_ShouldLoadExistingUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User { Id = userId, UserName = "test", RealName = "测试" };
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);

        var parameters = new DialogParameters
        {
            { "mode", "edit" },
            { "userId", userId }
        };

        // Act
        _viewModel.OnDialogOpened(parameters);

        // Assert
        Assert.Equal("编辑用户", _viewModel.Title);
        Assert.Equal("保存", _viewModel.SubmitButtonText);
        Assert.Equal("test", _viewModel.UserName);
        Assert.Equal("测试", _viewModel.RealName);
    }

    [Fact]
    public async Task SaveCommand_CreateMode_ShouldCreateNewUser()
    {
        // Arrange
        var parameters = new DialogParameters { { "mode", "create" } };
        _viewModel.OnDialogOpened(parameters);

        _viewModel.UserName = "newuser";
        _viewModel.RealName = "新用户";
        _viewModel.Password = "password123";

        // Act
        await _viewModel.SaveCommand.Execute();

        // Assert
        _userRepository.Verify(r => r.CreateAsync(It.Is<User>(u =>
            u.UserName == "newuser" &&
            u.RealName == "新用户"
        )), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_EditMode_ShouldUpdateExistingUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User { Id = userId, UserName = "test", RealName = "测试" };
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);

        var parameters = new DialogParameters
        {
            { "mode", "edit" },
            { "userId", userId }
        };
        _viewModel.OnDialogOpened(parameters);

        _viewModel.RealName = "更新后的名字";

        // Act
        await _viewModel.SaveCommand.Execute();

        // Assert
        _userRepository.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.Id == userId &&
            u.RealName == "更新后的名字"
        )), Times.Once);
    }

    [Fact]
    public void CancelCommand_ShouldCloseDialogWithCancelResult()
    {
        // Act
        _viewModel.CancelCommand.Execute();

        // Assert
        Assert.Equal(ButtonResult.Cancel, _viewModel.DialogResult);
    }
}
```

### 更新调用方测试

#### UserManagementViewModelTests更新
```csharp
[Fact]
public void OnCreateUser_ShouldShowUserFormDialog()
{
    // Arrange
    var dialogService = new Mock<IDialogService>();

    // Act
    _viewModel.CreateUserCommand.Execute();

    // Assert
    dialogService.Verify(d => d.ShowDialog(
        "UserFormDialog",
        It.Is<IDialogParameters>(p => p.GetValue<string>("mode") == "create"),
        It.IsAny<Action<IDialogResult>>()
    ), Times.Once);
}

[Fact]
public void OnEditUser_ShouldShowUserFormDialogWithUserId()
{
    // Arrange
    var user = new User { Id = Guid.NewGuid(), UserName = "test" };

    // Act
    _viewModel.EditUserCommand.Execute(user);

    // Assert
    _dialogService.Verify(d => d.ShowDialog(
        "UserFormDialog",
        It.Is<IDialogParameters>(p =>
            p.GetValue<string>("mode") == "edit" &&
            p.GetValue<Guid>("userId") == user.Id
        ),
        It.IsAny<Action<IDialogResult>>()
    ), Times.Once);
}
```

## 测试覆盖率目标
- UserFormDialogViewModel: 100%
- UserManagementViewModel（导航部分）: 100%
- 整体覆盖率: ≥80%

## 验收标准
- [ ] 所有废弃ViewModel测试文件已删除
- [ ] UserFormDialogViewModelTests实现，覆盖所有场景
- [ ] UserManagementViewModelTests更新，覆盖DialogService调用
- [ ] 所有单元测试通过
- [ ] 测试覆盖率≥80%
- [ ] 测试报告生成，无失败用例
```

**标签**: `test`, `desktop`, `unit-test`, `refactoring`, `P0`
**优先级**: P0
**预估**: 8小时
**Epic**: #XXXX

---

## 📊 Issues优先级和依赖关系

### 依赖关系图
```
Epic Issue (Phase 1)
├── Issue #1: 合并用户界面 (独立，无依赖)
├── Issue #2: 删除医案冗余界面 (独立，无依赖)
├── Issue #3: 删除诊疗独立界面 (独立，无依赖)
├── Issue #4: 删除处方冗余界面 (独立，无依赖)
├── Issue #5: 删除验方对话框 (独立，无依赖)
├── Issue #6: 更新导航路由 (依赖: Issue #1-5完成后)
└── Issue #7: 更新单元测试 (依赖: Issue #1-6完成后)
```

### 并行执行建议
**Week 1 (并行执行)**:
- Day 1-2: Issue #1, #2, #3 (可并行，不同模块)
- Day 3-4: Issue #4, #5 (可并行，不同模块)

**Week 2 (顺序执行)**:
- Day 1: Issue #6 (依赖#1-5完成)
- Day 2-3: Issue #7 (依赖#1-6完成)
- Day 4: 集成测试和回归测试

---

## 📝 Issue创建步骤

### 步骤1: 创建Epic Issue
```bash
gh issue create \
  --title "Epic: Desktop端UI重构 Phase 1 - 技术债务清理" \
  --body-file docs/requirements/ui-refactoring-phase1-issues.md \
  --label "epic,ui-refactoring,phase-1,desktop,P0"
```

### 步骤2: 创建子Issues（批量）
使用GitHub CLI批量创建（需要先记录Epic Issue编号）:
```bash
EPIC_NUMBER=<Epic Issue编号>

# Issue #1
gh issue create \
  --title "refactor(users): 合并UserCreate和UserEdit为统一UserFormDialog" \
  --body "$(cat issue1-body.md)" \
  --label "refactor,desktop,users,ui-cleanup,P0" \
  --assignee <username>

# Issue #2-7 (重复以上命令，修改title和body)
```

### 步骤3: 关联子Issues到Epic
在每个子Issue描述中添加:
```markdown
**Epic**: #<Epic编号>
```

---

## 📋 后续工作

Phase 1完成后，继续Phase 2的规划：

### Phase 2 PRD和Issues（预览）
- **PRD**: `docs/requirements/ui-refactoring-phase2-prd.md`
- **Issues**: 4个主要Issues（MedicalCaseFlow, PrescriptionEditor, HerbManagement, FormulaManagement）
- **工期**: 3-4周

---

**文档状态**: ✅ 待创建Issues
**创建方式**: GitHub CLI批量创建
**Epic Owner**: 待分配
