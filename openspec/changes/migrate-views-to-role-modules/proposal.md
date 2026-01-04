# migrate-views-to-role-modules

## Why

当前业务模块中存在多个 View 文件，违反了项目的架构设计原则：

- **设计原则**: View 在角色台（Admin/Clinical），Control 在业务模块
- **现状问题**: 业务模块中仍有 11 个 View 文件，架构不统一
- **维护困难**: View 分散在业务模块和角色台，导航注册混乱

### 调用分析结果

经过代码搜索 `RequestNavigate` 调用，确认：

| 类别 | 数量 | 说明 |
|------|------|------|
| 无调用可删除 | 7个View | 未被任何代码调用，直接删除 |
| 有调用需迁移 | 3个View | PatientDetailView, ChangePasswordView, UserProfileView |
| 特殊保留 | 1个View | LoginView 不属于角色台概念 |

## What Changes

### Phase 1: 删除无调用的View（7个文件）

直接删除未被代码调用的View：
- `Consultation/Views/ConsultationFormView.xaml(.cs)` - 作为组件使用，非导航目标
- `Formula/Views/FormulaDetailView.xaml(.cs)` - 无调用
- `Formula/Views/FormulaValidationView.xaml(.cs)` - 无调用
- `Herbs/Views/HerbDetailView.xaml(.cs)` - 无调用
- `MedicalCase/Views/MedicalCaseDetailView.xaml(.cs)` - 无调用
- `MedicalCase/Views/MedicalCaseWorkspaceView.xaml(.cs)` - 重复，Clinical已有
- `Users/Views/UserDetailView.xaml(.cs)` - 无调用

### Phase 2: PatientDetailView 迁移

将 `PatientDetailView` 迁移到角色台：
- 业务模块创建 `PatientDetailControl`
- Admin/Clinical 创建薄包装 `PatientDetailView`

**调用点**:
- `PatientSelectionViewModel.cs:177`
- `MedicalCaseWorkspaceViewModel.cs:475,477`

### Phase 3: ChangePasswordView 迁移

将 `ChangePasswordView` 迁移到角色台：
- 业务模块创建 `ChangePasswordControl`
- Admin/Clinical 创建薄包装 `ChangePasswordView`

**调用点**:
- `MenuManager.cs:107`
- `ClinicalHomeViewModel.cs:272`
- `AdminHomeViewModel.cs:239`

### Phase 4: UserProfileView 迁移

将 `UserProfileView` 迁移到角色台：
- 业务模块创建 `UserProfileControl`
- Admin/Clinical 创建薄包装 `UserProfileView`

**调用点**:
- `MenuManager.cs:100`
- `ClinicalHomeViewModel.cs:252`
- `AdminHomeViewModel.cs:218`

### Phase 5: 清理与验证

- 删除业务模块中空的 Views 文件夹（如适用）
- 更新 Module.cs 注册
- 全量编译验证

## Architecture

### 目标架构

```
角色台 (Admin/Clinical)
├── Views/
│   ├── XxxManagementView.xaml      (薄包装，嵌入 XxxMasterDetailControl)
│   ├── PatientDetailView.xaml      (薄包装，嵌入 PatientDetailControl)
│   ├── ChangePasswordView.xaml     (薄包装，嵌入 ChangePasswordControl)
│   └── UserProfileView.xaml        (薄包装，嵌入 UserProfileControl)
│
业务模块
├── Controls/
│   ├── XxxMasterDetailControl.xaml (复用组件，内部解析ViewModel)
│   ├── PatientDetailControl.xaml   (复用组件，内部解析ViewModel)
│   ├── ChangePasswordControl.xaml  (复用组件，内部解析ViewModel)
│   └── UserProfileControl.xaml     (复用组件，内部解析ViewModel)
├── ViewModels/
│   └── 对应ViewModel
└── Views/
    └── (仅保留特殊View如LoginView)
```

### 迁移模式

**View → Control + 薄包装View**

```
Before:
  业务模块/Views/XxxView.xaml
    └─ prism:ViewModelLocator.AutoWireViewModel="True"

After:
  业务模块/Controls/XxxControl.xaml
    └─ DataContext = ContainerLocator.Container.Resolve<XxxViewModel>()

  角色台/Views/XxxView.xaml (薄包装)
    └─ <controls:XxxControl />
```

## Impact

- **文件变更**: ~20个文件（删除14个 + 新建6个）
- **模块注册**: 更新各Module.cs的RegisterForNavigation
- **导航调用**: View名称保持不变，调用方无需修改
- **编译验证**: 每个Phase完成后需编译验证

## Risks

| 风险 | 缓解措施 |
|------|----------|
| 导航中断 | 保持View名称不变，仅改变位置 |
| ViewModel绑定失败 | Control使用DI解析，与MasterDetailControl模式一致 |
| 遗漏引用 | 每个Phase编译验证 |

## References

- OpenSpec: refactor-admin-workspace (MasterDetail模式参考)
- OpenSpec: rename-reference-to-management (薄包装View命名规范)
