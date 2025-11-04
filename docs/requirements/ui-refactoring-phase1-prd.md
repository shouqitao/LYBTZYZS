# Desktop端UI重构 Phase 1 PRD - 技术债务清理

**文档类型**: 产品需求文档 (Product Requirements Document)
**创建日期**: 2025-11-04
**版本**: v1.0
**优先级**: 🔴 P0 (Critical)
**预计工期**: 1-2周
**Epic**: Desktop端UI/UX重构
**Phase**: Phase 1 - 技术债务清理

---

## 📋 执行摘要

### 背景
当前Desktop端存在39个XAML视图，其中多个界面功能重复、定位模糊，增加了维护成本和用户导航复杂度。根据《Desktop端UI/UX重构方案》，Phase 1重点清理技术债务，删除冗余界面，简化UI结构。

### 目标
- **定量目标**: UI文件数量从39个减少到34个（-13%）
- **质量目标**: 代码维护成本降低30%，用户导航混乱度降低50%
- **合规目标**: 符合AR-001聚合根约束、ADR-009组件化规范、DRY原则

### 范围
- **包含**: 5个冗余XAML文件删除、1个新对话框创建、导航路由更新、单元测试更新
- **不包含**: ViewModel组件化重构（Phase 2）、UX流程优化（Phase 3）、UI现代化设计（Phase 4）

---

## 1. 需求详情

### 1.1 需求1: 合并用户创建和编辑界面 (🔴 P0)

#### 当前状况
- **文件**: `LYBT.Desktop.Users/Views/UserCreateView.xaml`
- **文件**: `LYBT.Desktop.Users/Views/UserEditView.xaml`
- **ViewModel**: `UserCreateViewModel.cs`, `UserEditViewModel.cs`
- **问题**: 两个界面95%代码重复，违反DRY原则

#### 需求描述
**作为** 用户管理员
**我希望** 使用统一的用户表单对话框进行创建和编辑操作
**以便于** 减少维护成本，保持界面一致性

#### 解决方案
```
删除:
  - UserCreateView.xaml
  - UserEditView.xaml
  - UserCreateViewModel.cs
  - UserEditViewModel.cs

新增:
  - UserFormDialog.xaml (对话框，支持Create/Edit模式)
  - UserFormDialogViewModel.cs

参数设计:
  - mode: "create" | "edit"
  - userId?: Guid (编辑模式必传)

功能:
  - Create模式: 标题"创建用户"，所有字段空白，提交按钮"创建"
  - Edit模式: 标题"编辑用户"，字段预填充，提交按钮"保存"
```

#### 验收标准
- [ ] UserCreateView.xaml和UserEditView.xaml已删除
- [ ] UserFormDialog.xaml实现，支持Create/Edit两种模式
- [ ] UserManagementView导航调用更新为DialogService.ShowDialog
- [ ] 单元测试通过，覆盖Create/Edit场景
- [ ] 回归测试：用户创建和编辑功能正常

---

### 1.2 需求2: 删除医案管理冗余界面 (🔴 P0)

#### 当前状况
- **文件**: `LYBT.Desktop.MedicalCase/Views/MedicalCaseManagementView.xaml` (管理界面)
- **文件**: `LYBT.Desktop.MedicalCase/Views/MedicalCaseListView.xaml` (列表界面)
- **文件**: `LYBT.Desktop.MedicalCase/Views/OtherCasesQueryView.xaml` (其他病案查询)
- **问题**: 三个界面功能重叠，定位不清晰

#### 需求描述
**作为** 医生
**我希望** 使用单一的医案管理界面
**以便于** 快速找到需要的功能，避免界面跳转混乱

#### 解决方案
```
保留:
  - MedicalCaseManagementView.xaml (主管理界面)
    - 功能: 查询、筛选、分页、创建、查看、删除

删除:
  - MedicalCaseListView.xaml (功能与Management重复)
  - OtherCasesQueryView.xaml (不符合AR-001聚合根约束)

理由:
  - MedicalCaseListView与ManagementView功能完全重复
  - OtherCasesQueryView违反AR-001约束（医案应该通过患者聚合根访问）
```

#### 验收标准
- [ ] MedicalCaseListView.xaml已删除
- [ ] OtherCasesQueryView.xaml已删除
- [ ] MedicalCaseManagementView功能完整（查询、筛选、分页、操作）
- [ ] 所有导航入口更新为MedicalCaseManagementView
- [ ] 单元测试通过
- [ ] 回归测试：医案管理功能正常

---

### 1.3 需求3: 删除诊疗记录独立管理界面 (🔴 P0)

#### 当前状况
- **文件**: `LYBT.Desktop.Consultation/Views/ConsultationManagementView.xaml` (独立管理界面)
- **文件**: `LYBT.Desktop.Consultation/Views/ConsultationFormView.xaml` (表单界面)
- **问题**: 违反AR-001聚合根约束

#### 需求描述
**作为** 架构师
**我希望** 强制诊疗记录只能通过医案聚合根访问
**以便于** 保证数据一致性，符合DDD架构原则

#### 业务规则参考
**AR-001**: MedicalCase聚合根约束
```
约束内容:
  - Consultation是MedicalCase的聚合子实体
  - 写操作必须通过MedicalCase聚合根
  - 禁止直接访问Consultation进行独立管理

违规后果:
  - 数据不一致（Consultation与MedicalCase状态脱节）
  - 破坏聚合根边界
```

#### 解决方案
```
删除:
  - ConsultationManagementView.xaml (独立管理界面)
  - ConsultationManagementViewModel.cs

保留:
  - ConsultationFormView.xaml (仅在MedicalCaseFlowView上下文中使用)

导航限制:
  - ConsultationFormView只能通过MedicalCaseFlowView导航
  - 禁止从主菜单或其他模块直接访问
```

#### 验收标准
- [ ] ConsultationManagementView.xaml已删除
- [ ] ConsultationFormView保留，且只在MedicalCaseFlowView中使用
- [ ] 主菜单和所有导航路由中无ConsultationManagement入口
- [ ] 单元测试通过
- [ ] 回归测试：诊疗记录创建和查看功能正常（通过医案上下文）

---

### 1.4 需求4: 删除处方管理冗余主界面 (🔴 P0)

#### 当前状况
- **文件**: `LYBT.Desktop.Prescriptions/Views/PrescriptionsMainView.xaml` (主界面)
- **文件**: `LYBT.Desktop.Prescriptions/Views/PrescriptionManagementView.xaml` (管理界面)
- **文件**: `LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml` (详情界面)
- **问题**: Main和Management界面功能重叠，用户不清楚应该使用哪个

#### 需求描述
**作为** 医生
**我希望** 使用统一的处方管理界面
**以便于** 快速查找、创建、编辑和打印处方

#### 解决方案
```
删除:
  - PrescriptionsMainView.xaml (功能与Management重复)

保留:
  - PrescriptionManagementView.xaml (合并Main和Management功能)
    - 功能: 查询、筛选、分页、创建、编辑、打印
  - PrescriptionView.xaml (只读详情，用于打印预览和查看)

优化:
  - PrescriptionManagementView作为唯一入口
  - PrescriptionView仅用于只读查看和打印
```

#### 验收标准
- [ ] PrescriptionsMainView.xaml已删除
- [ ] PrescriptionManagementView功能完整（包含原Main功能）
- [ ] PrescriptionView保留，只读功能正常
- [ ] 主菜单导航更新为PrescriptionManagementView
- [ ] 单元测试通过
- [ ] 回归测试：处方管理、查看、打印功能正常

---

### 1.5 需求5: 删除验方查看对话框 (🔴 P0)

#### 当前状况
- **文件**: `LYBT.Desktop.Formula/Views/FormulaDetailView.xaml` (详情页面)
- **文件**: `LYBT.Desktop.Formula/Views/ViewFormulaDialog.xaml` (查看对话框)
- **问题**: 两个界面都是只读查看功能，完全重复

#### 需求描述
**作为** 医生
**我希望** 使用统一的验方详情界面
**以便于** 减少界面维护成本

#### 解决方案
```
删除:
  - ViewFormulaDialog.xaml

保留:
  - FormulaDetailView.xaml (支持只读和编辑两种模式)

功能扩展:
  - FormulaDetailView增加mode参数: "view" | "edit"
  - view模式: 所有字段只读，隐藏保存按钮
  - edit模式: 字段可编辑，显示保存按钮
```

#### 验收标准
- [ ] ViewFormulaDialog.xaml已删除
- [ ] FormulaDetailView支持view/edit两种模式
- [ ] 所有调用ViewFormulaDialog的地方改为FormulaDetailView
- [ ] 单元测试通过
- [ ] 回归测试：验方查看和编辑功能正常

---

## 2. 技术实施方案

### 2.1 导航路由更新

#### Prism RegionManager配置

**文件**: `LYBT.Desktop.*/Module.cs` (各模块的Module注册类)

**修改内容**:
```csharp
// 1. Users模块 (LYBT.Desktop.Users/UsersModule.cs)
// 删除
// containerRegistry.RegisterForNavigation<UserCreateView>();
// containerRegistry.RegisterForNavigation<UserEditView>();

// 新增
containerRegistry.RegisterDialog<UserFormDialog, UserFormDialogViewModel>();

// 2. MedicalCase模块 (LYBT.Desktop.MedicalCase/MedicalCaseModule.cs)
// 删除
// containerRegistry.RegisterForNavigation<MedicalCaseListView>();
// containerRegistry.RegisterForNavigation<OtherCasesQueryView>();

// 3. Consultation模块 (LYBT.Desktop.Consultation/ConsultationModule.cs)
// 删除
// containerRegistry.RegisterForNavigation<ConsultationManagementView>();

// 4. Prescriptions模块 (LYBT.Desktop.Prescriptions/PrescriptionsModule.cs)
// 删除
// containerRegistry.RegisterForNavigation<PrescriptionsMainView>();

// 5. Formula模块 (LYBT.Desktop.Formula/FormulaModule.cs)
// 删除
// containerRegistry.RegisterForNavigation<ViewFormulaDialog>();
```

### 2.2 主菜单导航更新

**文件**: `src/Client/Desktop/LYBT.Desktop.Shell/Views/MainWindow.xaml` (或MenuView.xaml)

**修改内容**:
```xml
<!-- 删除无效菜单项 -->
<!--
<MenuItem Header="其他病案查询" Command="{Binding NavigateCommand}" CommandParameter="OtherCasesQueryView" />
<MenuItem Header="诊疗记录管理" Command="{Binding NavigateCommand}" CommandParameter="ConsultationManagementView" />
-->

<!-- 确保保留的菜单项指向正确的View -->
<MenuItem Header="处方管理" Command="{Binding NavigateCommand}" CommandParameter="PrescriptionManagementView" />
```

### 2.3 ViewModel导航调用更新

#### UserManagementViewModel调用更新
```csharp
// Before
private async void OnCreateUser()
{
    _regionManager.RequestNavigate("MainRegion", "UserCreateView");
}

private async void OnEditUser(User user)
{
    var parameters = new NavigationParameters { { "userId", user.Id } };
    _regionManager.RequestNavigate("MainRegion", "UserEditView", parameters);
}

// After
private async void OnCreateUser()
{
    var parameters = new DialogParameters { { "mode", "create" } };
    _dialogService.ShowDialog("UserFormDialog", parameters, result => {
        if (result.Result == ButtonResult.OK)
        {
            LoadUsers(); // 刷新列表
        }
    });
}

private async void OnEditUser(User user)
{
    var parameters = new DialogParameters
    {
        { "mode", "edit" },
        { "userId", user.Id }
    };
    _dialogService.ShowDialog("UserFormDialog", parameters, result => {
        if (result.Result == ButtonResult.OK)
        {
            LoadUsers(); // 刷新列表
        }
    });
}
```

### 2.4 单元测试更新

**新增测试文件**:
- `tests/UnitTests/Client/Desktop/Users/UserFormDialogViewModelTests.cs`

**删除测试文件**:
- `UserCreateViewModelTests.cs`
- `UserEditViewModelTests.cs`

**测试用例设计** (UserFormDialogViewModel):
```csharp
[Fact]
public void Constructor_CreateMode_ShouldInitializeEmptyForm()
{
    // Arrange & Act
    var parameters = new DialogParameters { { "mode", "create" } };
    var vm = new UserFormDialogViewModel(_userRepository.Object, _dialogService.Object);
    vm.OnDialogOpened(parameters);

    // Assert
    Assert.Equal("创建用户", vm.Title);
    Assert.Equal("创建", vm.SubmitButtonText);
    Assert.Null(vm.UserName);
    Assert.Null(vm.RealName);
}

[Fact]
public void Constructor_EditMode_ShouldLoadExistingUser()
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
    var vm = new UserFormDialogViewModel(_userRepository.Object, _dialogService.Object);
    vm.OnDialogOpened(parameters);

    // Assert
    Assert.Equal("编辑用户", vm.Title);
    Assert.Equal("保存", vm.SubmitButtonText);
    Assert.Equal("test", vm.UserName);
    Assert.Equal("测试", vm.RealName);
}

[Fact]
public async Task SaveCommand_CreateMode_ShouldCreateNewUser()
{
    // Arrange
    var parameters = new DialogParameters { { "mode", "create" } };
    var vm = new UserFormDialogViewModel(_userRepository.Object, _dialogService.Object);
    vm.OnDialogOpened(parameters);

    vm.UserName = "newuser";
    vm.RealName = "新用户";
    vm.Password = "password123";

    // Act
    await vm.SaveCommand.Execute();

    // Assert
    _userRepository.Verify(r => r.CreateAsync(It.Is<User>(u =>
        u.UserName == "newuser" &&
        u.RealName == "新用户"
    )), Times.Once);
}
```

---

## 3. 测试计划

### 3.1 单元测试

**测试范围**:
- 所有新增的ViewModel (UserFormDialogViewModel等)
- 所有修改的ViewModel (导航调用变更的部分)

**覆盖率目标**: ≥80%

**测试工具**: xUnit + NSubstitute

### 3.2 集成测试

**测试场景**:
1. 用户管理流程: 创建用户 → 编辑用户 → 查看用户 → 删除用户
2. 医案管理流程: 查询医案 → 创建医案 → 查看医案详情
3. 诊疗记录流程: 通过医案创建诊疗记录 → 查看诊疗记录
4. 处方管理流程: 查询处方 → 创建处方 → 查看处方 → 打印处方
5. 验方管理流程: 查询验方 → 查看验方 → 编辑验方

### 3.3 回归测试

**测试清单**:
- [ ] 所有8个模块主要功能可用
- [ ] 主菜单导航正确
- [ ] 无死链接（点击后无响应或报错）
- [ ] 数据CRUD操作正常
- [ ] 权限验证正常（普通用户vs超级管理员）

**测试环境**:
- 开发环境: Windows 11, .NET 8.0, SQL Server 2022
- 测试数据: 使用测试数据库（LYBTZYZS_Test）

### 3.4 性能测试

**基准测试**:
- 界面加载时间: <500ms
- 对话框弹出时间: <200ms
- 数据查询响应: <1s (100条记录)

---

## 4. 风险评估与缓解

### 4.1 风险矩阵

| 风险 | 概率 | 影响 | 缓解措施 | 责任人 |
|------|------|------|----------|--------|
| 删除界面导致功能缺失 | 中 | 高 | 完整回归测试，保留Git历史 | QA团队 |
| 导航路由配置错误 | 中 | 中 | 手动测试所有导航入口 | 开发团队 |
| 单元测试遗漏 | 低 | 中 | Code Review，覆盖率报告 | 开发团队 |
| 用户习惯变更抵触 | 低 | 低 | 用户培训，提供文档 | 产品团队 |

### 4.2 回滚计划

**Git分支策略**:
```bash
# 创建功能分支
git checkout -b refactor/ui-cleanup-phase1

# 完成开发和测试后，合并到master
git checkout master
git merge refactor/ui-cleanup-phase1

# 如需回滚
git revert <merge-commit-hash>
```

**回滚触发条件**:
- 回归测试失败率>10%
- 关键功能不可用
- 性能明显下降（响应时间>2x基准）

---

## 5. 实施时间表

### Week 1: 开发和单元测试
| 任务 | 工作量 | 负责人 | 状态 |
|------|--------|--------|------|
| 需求1: 合并用户创建编辑界面 | 6h | 待分配 | 🔲 待开始 |
| 需求2: 删除医案冗余界面 | 4h | 待分配 | 🔲 待开始 |
| 需求3: 删除诊疗独立管理界面 | 3h | 待分配 | 🔲 待开始 |
| 需求4: 删除处方主界面 | 3h | 待分配 | 🔲 待开始 |
| 需求5: 删除验方查看对话框 | 2h | 待分配 | 🔲 待开始 |
| 导航路由和菜单更新 | 4h | 待分配 | 🔲 待开始 |
| 单元测试编写 | 8h | 待分配 | 🔲 待开始 |

**Week 1总计**: 30小时 (~4个工作日)

### Week 2: 集成测试和部署
| 任务 | 工作量 | 负责人 | 状态 |
|------|--------|--------|------|
| 集成测试执行 | 8h | 待分配 | 🔲 待开始 |
| 回归测试执行 | 8h | 待分配 | 🔲 待开始 |
| Bug修复 | 6h | 待分配 | 🔲 待开始 |
| Code Review | 2h | 待分配 | 🔲 待开始 |
| 文档更新 | 2h | 待分配 | 🔲 待开始 |
| 部署到测试环境 | 2h | 待分配 | 🔲 待开始 |

**Week 2总计**: 28小时 (~3.5个工作日)

**Phase 1总工期**: 7.5个工作日 (1.5周)

---

## 6. 验收标准

### 6.1 功能验收

- [ ] **需求1**: UserFormDialog实现，支持Create/Edit模式，功能正常
- [ ] **需求2**: MedicalCaseListView和OtherCasesQueryView已删除，功能合并到ManagementView
- [ ] **需求3**: ConsultationManagementView已删除，诊疗记录只能通过医案访问
- [ ] **需求4**: PrescriptionsMainView已删除，功能合并到ManagementView
- [ ] **需求5**: ViewFormulaDialog已删除，FormulaDetailView支持view/edit模式

### 6.2 质量验收

- [ ] 单元测试覆盖率≥80%
- [ ] 所有单元测试通过
- [ ] 集成测试通过率100%
- [ ] 回归测试通过率≥95%
- [ ] 无P0/P1级别Bug

### 6.3 代码审查

- [ ] 代码符合项目编码规范
- [ ] 无硬编码，配置外部化
- [ ] 依赖注入正确（仅构造函数注入）
- [ ] 异步方法正确使用async/await
- [ ] XAML代码整洁，无冗余绑定

### 6.4 文档更新

- [ ] 架构文档更新（删除已废弃界面）
- [ ] 开发指南更新（新对话框使用说明）
- [ ] API文档更新（如有接口变更）
- [ ] CHANGELOG.md记录变更

### 6.5 性能验收

- [ ] 界面加载时间<500ms
- [ ] 对话框弹出时间<200ms
- [ ] 内存占用无明显增加（<10%）
- [ ] 无内存泄漏

---

## 7. 依赖和前置条件

### 7.1 前置条件
- ✅ ADR-009 Desktop端组件化模式已批准
- ✅ AR-001 MedicalCase聚合根约束已文档化
- ✅ Desktop端UI/UX重构方案已批准

### 7.2 技术依赖
- .NET 8.0 SDK
- Prism 9.0
- xUnit 2.6+
- NSubstitute 5.1+

### 7.3 环境依赖
- 开发环境: Visual Studio 2022 17.8+
- 数据库: SQL Server 2022
- 测试数据库: LYBTZYZS_Test

---

## 8. 后续Phase预览

### Phase 2: ViewModel组件化重构 (🟡 P1)
**预计开始**: Phase 1完成后1周
**工期**: 3-4周
**重点**:
- MedicalCaseFlowViewModel (600行 → <300行)
- PrescriptionEditorViewModel (500行 → <300行)
- HerbManagementViewModel (400行 → <300行)
- FormulaManagementViewModel (450行 → <300行)

### Phase 3: UX流程优化 (🟡 P1)
**预计开始**: Phase 2完成后1周
**工期**: 2-3周
**重点**:
- 实现MedicalCaseWizardView（三步流程Wizard模式）
- 优化处方打印流程（快速打印、批量打印）

### Phase 4: 现代化UI设计 (🟢 P2)
**预计开始**: Phase 3完成后2周
**工期**: 1-2个月
**重点**:
- 创建统一样式库（Styles/Themes.xaml）
- 标准化所有组件（原生WPF）
- 实现亮色/暗色主题切换

---

## 9. 参考资源

### 9.1 项目文档
- `docs/reports/ui-ux-refactoring-plan-2025-11-04.md` - UI/UX重构完整方案
- `docs/explanation/architecture/client/README.md` - Desktop端架构指南
- `docs/explanation/architecture/decisions/ADR-009-desktop-component-pattern.md` - 组件化ADR
- `docs/explanation/business-rules.md` - 业务规则（AR-001等）
- `.spec-workflow/steering/constitution.md` - 项目宪法

### 9.2 技术文档
- [Prism DialogService](https://prismlibrary.com/docs/dialogs.html)
- [Prism RegionManager](https://prismlibrary.com/docs/region-navigation/navigation-basics.html)
- [WPF MVVM Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)

---

## 10. 附录

### 10.1 待删除文件清单

```
src/Client/Desktop/
├── LYBT.Desktop.Users/
│   └── Views/
│       ├── UserCreateView.xaml (删除)
│       ├── UserCreateView.xaml.cs (删除)
│       ├── UserEditView.xaml (删除)
│       └── UserEditView.xaml.cs (删除)
├── LYBT.Desktop.MedicalCase/
│   └── Views/
│       ├── MedicalCaseListView.xaml (删除)
│       ├── MedicalCaseListView.xaml.cs (删除)
│       ├── OtherCasesQueryView.xaml (删除)
│       └── OtherCasesQueryView.xaml.cs (删除)
├── LYBT.Desktop.Consultation/
│   └── Views/
│       ├── ConsultationManagementView.xaml (删除)
│       └── ConsultationManagementView.xaml.cs (删除)
├── LYBT.Desktop.Prescriptions/
│   └── Views/
│       ├── PrescriptionsMainView.xaml (删除)
│       └── PrescriptionsMainView.xaml.cs (删除)
└── LYBT.Desktop.Formula/
    └── Views/
        ├── ViewFormulaDialog.xaml (删除)
        └── ViewFormulaDialog.xaml.cs (删除)

总计: 14个文件删除
```

### 10.2 待创建文件清单

```
src/Client/Desktop/
└── LYBT.Desktop.Users/
    ├── Views/
    │   ├── UserFormDialog.xaml (新增)
    │   └── UserFormDialog.xaml.cs (新增)
    └── ViewModels/
        └── UserFormDialogViewModel.cs (新增)

tests/UnitTests/Client/Desktop/
└── LYBT.Desktop.Users/
    └── UserFormDialogViewModelTests.cs (新增)

总计: 4个文件新增
```

### 10.3 变更影响分析

**影响范围**:
- **模块数量**: 5个模块 (Users, MedicalCase, Consultation, Prescriptions, Formula)
- **文件变更**: 删除14个文件，新增4个文件，修改~10个文件
- **代码行数**: 删除~2000行，新增~500行，净减少~1500行
- **测试用例**: 删除~50个测试，新增~20个测试

**用户影响**:
- **用户培训**: 需要（菜单结构变化）
- **操作习惯**: 中等影响（界面入口变化）
- **学习曲线**: 低（功能本质未变，只是入口统一）

---

## 📝 版本历史

| 版本 | 日期 | 变更内容 | 作者 |
|------|------|----------|------|
| v1.0 | 2025-11-04 | 初始版本，完整PRD文档 | Claude Code |

---

**文档状态**: ✅ 待评审
**下一步**: 等待用户确认后创建GitHub Issues
