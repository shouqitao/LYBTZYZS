# migrate-views-to-role-modules Tasks

## Phase 1: 删除无调用的View

- [ ] 1.1 删除 `Consultation/Views/ConsultationFormView.xaml(.cs)`
- [ ] 1.2 更新 ConsultationModule.cs 移除相关注册
- [ ] 1.3 删除 `Formula/Views/FormulaDetailView.xaml(.cs)`
- [ ] 1.4 删除 `Formula/Views/FormulaValidationView.xaml(.cs)`
- [ ] 1.5 更新 FormulaModule.cs 移除相关注册
- [ ] 1.6 删除 `Herbs/Views/HerbDetailView.xaml(.cs)`
- [ ] 1.7 更新 HerbsModule.cs 移除相关注册
- [ ] 1.8 删除 `MedicalCase/Views/MedicalCaseDetailView.xaml(.cs)`
- [ ] 1.9 删除 `MedicalCase/Views/MedicalCaseWorkspaceView.xaml(.cs)`
- [ ] 1.10 更新 MedicalCaseModule.cs 移除相关注册
- [ ] 1.11 删除 `Users/Views/UserDetailView.xaml(.cs)`
- [ ] 1.12 更新 UsersModule.cs 移除相关注册
- [ ] 1.13 编译验证 Phase 1

## Phase 2: PatientDetailView 迁移

- [ ] 2.1 创建 `Patients/Controls/PatientDetailControl.xaml(.cs)`
- [ ] 2.2 从 PatientDetailView 迁移内容到 Control
- [ ] 2.3 修改为 Control 模式（DI 解析 ViewModel）
- [ ] 2.4 创建 `Admin/Views/PatientDetailView.xaml(.cs)` (薄包装)
- [ ] 2.5 创建 `Clinical/Views/PatientDetailView.xaml(.cs)` (薄包装)
- [ ] 2.6 删除 `Patients/Views/PatientDetailView.xaml(.cs)`
- [ ] 2.7 更新 PatientsModule.cs 移除 RegisterForNavigation
- [ ] 2.8 更新 AdminModule.cs 添加 RegisterForNavigation
- [ ] 2.9 更新 ClinicalModule.cs 添加 RegisterForNavigation
- [ ] 2.10 编译验证 Phase 2

## Phase 3: ChangePasswordView 迁移

- [ ] 3.1 创建 `Users/Controls/ChangePasswordControl.xaml(.cs)`
- [ ] 3.2 从 ChangePasswordView 迁移内容到 Control
- [ ] 3.3 修改为 Control 模式（DI 解析 ViewModel）
- [ ] 3.4 创建 `Admin/Views/ChangePasswordView.xaml(.cs)` (薄包装)
- [ ] 3.5 创建 `Clinical/Views/ChangePasswordView.xaml(.cs)` (薄包装)
- [ ] 3.6 删除 `Users/Views/ChangePasswordView.xaml(.cs)`
- [ ] 3.7 更新 UsersModule.cs 移除 RegisterForNavigation
- [ ] 3.8 更新 AdminModule.cs 添加 RegisterForNavigation
- [ ] 3.9 更新 ClinicalModule.cs 添加 RegisterForNavigation
- [ ] 3.10 编译验证 Phase 3

## Phase 4: UserProfileView 迁移

- [ ] 4.1 创建 `Users/Controls/UserProfileControl.xaml(.cs)`
- [ ] 4.2 从 UserProfileView 迁移内容到 Control
- [ ] 4.3 修改为 Control 模式（DI 解析 ViewModel）
- [ ] 4.4 创建 `Admin/Views/UserProfileView.xaml(.cs)` (薄包装)
- [ ] 4.5 创建 `Clinical/Views/UserProfileView.xaml(.cs)` (薄包装)
- [ ] 4.6 删除 `Users/Views/UserProfileView.xaml(.cs)`
- [ ] 4.7 更新 UsersModule.cs 移除 RegisterForNavigation
- [ ] 4.8 更新 AdminModule.cs 添加 RegisterForNavigation
- [ ] 4.9 更新 ClinicalModule.cs 添加 RegisterForNavigation
- [ ] 4.10 编译验证 Phase 4

## Phase 5: 清理与验证

- [ ] 5.1 检查并删除业务模块中空的 Views 文件夹
- [ ] 5.2 全量编译验证
- [ ] 5.3 运行时导航测试
- [ ] 5.4 更新架构文档

## 完成标准

- [ ] 业务模块 Views 文件夹仅保留 LoginView
- [ ] 3个有调用的View已迁移为 Control + 薄包装View 模式
- [ ] 7个无调用的View已删除
- [ ] 所有角色台 View 正常导航
- [ ] 编译 0 错误
