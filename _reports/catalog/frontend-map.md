# 前端架构映射表 - View↔ViewModel↔Service绑定关系

**生成时间**: 2025-01-09  
**映射范围**: LYBTZYZS前端WPF所有业务模块绑定关系  
**架构模式**: MVVM + Prism + UltraThink双层架构

## 🎯 映射表说明

本映射表详细记录了前端各个View与ViewModel的绑定关系，以及ViewModel中的Command、Property与后端Service的调用关系。

**映射关系层次**:

- **View层**: WPF XAML界面，用户交互入口
- **ViewModel层**: 业务逻辑处理，MVVM模式核心
- **Service层**: 后端API调用，UltraThink双层架构
- **Command层**: 用户操作命令，异步处理
- **Property层**: 数据绑定属性，双向同步

## 📋 Shell壳程序映射

### MainWindow + HomeView (系统主界面)

| View组件              | ViewModel             | 绑定属性/命令                              | Service调用                                 | 后端API                       | 描述          |
| ------------------- | --------------------- | ------------------------------------ | ----------------------------------------- | --------------------------- | ----------- |
| **MainWindow.xaml** | `MainWindowViewModel` | `Title`                              | -                                         | -                           | 主窗口标题       |
|                     |                       | `CurrentView`                        | -                                         | -                           | 当前显示的区域内容   |
| **HomeView.xaml**   | `HomeViewModel`       |                                      |                                           |                             | **系统仪表板主页** |
| 欢迎信息区域              |                       | `WelcomeMessage`                     | `IAuthService.GetCurrentUserAsync()`      | `/api/v1/auth/current-user` | 当前登录用户欢迎信息  |
|                     |                       | `SubTitle`                           | -                                         | -                           | 工作台标题       |
|                     |                       | `CurrentDateTime`                    | -                                         | -                           | 实时时间显示      |
| 角色权限控制              |                       | `IsAdminRole`                        | `IAuthService.GetCurrentUserAsync()`      | `/api/v1/auth/current-user` | 管理员角色判断     |
|                     |                       | `IsDoctorRole`                       | `IAuthService.GetCurrentUserAsync()`      | `/api/v1/auth/current-user` | 医生角色判断      |
| 统计数据区域              |                       | `TodayCompletedCount`                | `IMedicalCaseService.GetPagedAsync()`     | `/api/v1/medicalcases`      | 今日完成案例数     |
|                     |                       | `TodayInProgressCount`               | `IMedicalCaseService.GetPagedAsync()`     | `/api/v1/medicalcases`      | 今日进行中案例数    |
|                     |                       | `TodayTotalAmount`                   | 计算属性                                      | -                           | 今日收入统计      |
| 快速导航按钮              |                       | `StartConsultationCommand`           | -                                         | -                           | 开始看诊流程      |
|                     |                       | `NavigateToPatientReceptionCommand`  | -                                         | -                           | 导航至患者接待     |
|                     |                       | `NavigateToMedicalCaseCommand`       | -                                         | -                           | 导航至医疗案例     |
|                     |                       | `NavigateToPrescriptionQueryCommand` | -                                         | -                           | 导航至处方管理     |
|                     |                       | `NavigateToPatientManagementCommand` | -                                         | -                           | 导航至患者管理     |
|                     |                       | `NavigateToHerbsCommand`             | -                                         | -                           | 导航至药材管理     |
|                     |                       | `NavigateToFormulasCommand`          | -                                         | -                           | 导航至验方管理     |
| 管理员功能               |                       | `EnterSystemManagementCommand`       | -                                         | -                           | 进入系统管理      |
|                     |                       | `NavigateToUserManagementCommand`    | -                                         | -                           | 导航至用户管理     |
|                     |                       | `LogoutCommand`                      | `IAuthService.LogoutAsync()`              | `/api/v1/auth/logout`       | 用户登出        |
| 今日患者列表              |                       | `TodayPatients`                      | `IPatientService` + `IMedicalCaseService` | 多API组合                      | 今日就诊患者列表    |
|                     |                       | `SelectedPatient`                    | -                                         | -                           | 选中的患者       |
|                     |                       | `StartConsultationForPatientCommand` | 导航服务                                      | -                           | 为患者开始诊疗     |
|                     |                       | `ViewPatientDetailsCommand`          | 导航服务                                      | -                           | 查看患者详情      |
|                     |                       | `RefreshTodayPatientsCommand`        | 多Service调用                                | 多API                        | 刷新今日患者列表    |

**HomeViewModel核心逻辑流程**:

```
用户登录 → 获取用户信息 → 角色判断 → 加载统计数据 → 显示快速导航
├── Admin角色: 显示系统管理功能
└── Doctor角色: 显示诊疗功能 + 今日患者列表
```

## 🔐 Auth模块映射

### LoginView (登录界面)

| View组件             | ViewModel        | 绑定属性/命令                  | Service调用                   | 后端API                | 描述                   |
| ------------------ | ---------------- | ------------------------ | --------------------------- | -------------------- | -------------------- |
| **LoginView.xaml** | `LoginViewModel` |                          |                             |                      | **用户登录界面**           |
| 用户名输入框             |                  | `Username`               | -                           | -                    | 登录用户名绑定              |
| 密码输入框              |                  | `Password`               | -                           | -                    | 登录密码绑定(SecureString) |
| 记住密码复选框            |                  | `RememberMe`             | -                           | -                    | 记住密码选项               |
| 登录按钮               |                  | `LoginCommand`           | `IAuthService.LoginAsync()` | `/api/v1/auth/login` | 执行登录验证               |
| 密码变更事件             |                  | `PasswordChangedCommand` | -                           | -                    | 密码输入变更处理             |
| API状态指示            |                  | `ApiStatus`              | API连接检查                     | `/health`            | 后端API连接状态            |
|                    |                  | `IsApiOnline`            | 健康检查                        | `/health`            | API可用性状态             |
| 登录状态保存             |                  | `LoadSavedCredentials()` | 本地存储                        | -                    | 加载保存的凭据              |

**LoginViewModel业务逻辑**:

```
输入验证 → API连接检查 → 调用登录API → JWT令牌处理 → 用户信息缓存 → 导航主页
├── 记住密码: 保存凭据到本地存储
├── 登录失败: 显示错误信息，清除密码
└── 登录成功: 保存Token，发布登录事件，导航主页
```

## 👥 Users模块映射

### UserManagementView (用户管理)

| View组件                      | ViewModel                 | 绑定属性/命令               | Service调用                       | 后端API                       | 描述          |
| --------------------------- | ------------------------- | --------------------- | ------------------------------- | --------------------------- | ----------- |
| **UserManagementView.xaml** | `UserManagementViewModel` |                       |                                 |                             | **用户管理主界面** |
| 用户列表DataGrid                |                           | `Users`               | `IUserService.GetPagedAsync()`  | `/api/v1/users`             | 用户列表数据源     |
|                             |                           | `SelectedUser`        | -                               | -                           | 选中用户对象      |
| 搜索输入框                       |                           | `SearchKeyword`       | -                               | -                           | 搜索关键字       |
| 搜索按钮                        |                           | `SearchCommand`       | `IUserService.SearchAsync()`    | `/api/v1/users/search`      | 执行用户搜索      |
| 分页控制                        |                           | `CurrentPage`         | -                               | -                           | 当前页码        |
|                             |                           | `TotalPages`          | -                               | -                           | 总页数         |
|                             |                           | `FirstPageCommand`    | 分页查询                            | `/api/v1/users`             | 首页          |
|                             |                           | `PreviousPageCommand` | 分页查询                            | `/api/v1/users`             | 上一页         |
|                             |                           | `NextPageCommand`     | 分页查询                            | `/api/v1/users`             | 下一页         |
|                             |                           | `LastPageCommand`     | 分页查询                            | `/api/v1/users`             | 末页          |
| 用户操作按钮                      |                           | `AddCommand`          | 打开对话框                           | -                           | 添加新用户       |
|                             |                           | `EditCommand`         | `IUserService.GetByIdAsync()`   | `/api/v1/users/{id}`        | 编辑选中用户      |
|                             |                           | `DeleteCommand`       | `IUserService.DeleteAsync()`    | `/api/v1/users/{id}`        | 删除选中用户      |
|                             |                           | `ToggleStatusCommand` | `IUserService.SetStatusAsync()` | `/api/v1/users/{id}/status` | 启用/禁用用户     |
| 状态显示                        |                           | `StatusText`          | -                               | -                           | 操作状态提示      |

### UserAddEditDialogView (用户编辑对话框)

| View组件                     | ViewModel                    | 绑定属性/命令               | Service调用                                      | 后端API           | 描述                 |
| -------------------------- | ---------------------------- | --------------------- | ---------------------------------------------- | --------------- | ------------------ |
| **UserAddEditDialog.xaml** | `UserAddEditDialogViewModel` |                       |                                                |                 | **用户新增/编辑对话框**     |
| 用户信息输入                     |                              | `UserName`            | -                                              | -               | 用户名输入              |
|                            |                              | `RealName`            | -                                              | -               | 真实姓名输入             |
|                            |                              | `Role`                | -                                              | -               | 角色选择(Admin/Doctor) |
|                            |                              | `PhoneNumber`         | -                                              | -               | 电话号码输入             |
|                            |                              | `Email`               | -                                              | -               | 电子邮件输入             |
|                            |                              | `IsActive`            | -                                              | -               | 活跃状态开关             |
| 密码输入(新建)                   |                              | `Password`            | -                                              | -               | 初始密码设置             |
|                            |                              | `ConfirmPassword`     | -                                              | -               | 确认密码输入             |
| 对话框按钮                      |                              | `SaveCommand`         | `IUserService.CreateAsync()` / `UpdateAsync()` | `/api/v1/users` | 保存用户信息             |
|                            |                              | `CancelCommand`       | -                                              | -               | 取消编辑操作             |
| 验证逻辑                       |                              | `ValidateUserInput()` | 本地验证                                           | -               | 输入数据验证             |

## 🏥 Patients模块映射

### PatientManagementView (患者管理)

| View组件                         | ViewModel                    | 绑定属性/命令                   | Service调用                                   | 后端API                               | 描述          |
| ------------------------------ | ---------------------------- | ------------------------- | ------------------------------------------- | ----------------------------------- | ----------- |
| **PatientManagementView.xaml** | `PatientManagementViewModel` |                           |                                             |                                     | **患者管理主界面** |
| 患者列表DataGrid                   |                              | `Patients`                | `IPatientService.GetPagedAsync()`           | `/api/v1/patients`                  | 患者列表数据源     |
|                                |                              | `SelectedPatient`         | -                                           | -                                   | 选中患者对象      |
| 搜索功能区                          |                              | `SearchKeyword`           | -                                           | -                                   | 搜索关键字       |
|                                |                              | `SearchCommand`           | `IPatientService.SearchAsync()`             | `/api/v1/patients/search`           | 执行患者搜索      |
| 分页控制区                          |                              | `CurrentPage`             | -                                           | -                                   | 当前页码        |
|                                |                              | `TotalPages`              | -                                           | -                                   | 总页数         |
|                                |                              | `FirstPageCommand`        | 分页查询                                        | `/api/v1/patients`                  | 首页          |
|                                |                              | `PreviousPageCommand`     | 分页查询                                        | `/api/v1/patients`                  | 上一页         |
|                                |                              | `NextPageCommand`         | 分页查询                                        | `/api/v1/patients`                  | 下一页         |
|                                |                              | `LastPageCommand`         | 分页查询                                        | `/api/v1/patients`                  | 末页          |
| 患者操作按钮                         |                              | `AddCommand`              | 打开对话框                                       | -                                   | 添加新患者       |
|                                |                              | `EditCommand`             | `IPatientService.GetByIdAsync()`            | `/api/v1/patients/{id}`             | 编辑选中患者      |
|                                |                              | `DeleteCommand`           | `IPatientService.DeleteAsync()`             | `/api/v1/patients/{id}`             | 删除选中患者      |
|                                |                              | `ToggleStatusCommand`     | `IPatientService.SetStatusAsync()`          | `/api/v1/patients/{id}/status`      | 启用/禁用患者     |
|                                |                              | `ViewDetailsCommand`      | 导航服务                                        | -                                   | 查看患者详情      |
|                                |                              | `ViewHistoryCommand`      | `IMedicalCaseService.GetByPatientIdAsync()` | `/api/v1/medicalcases/patient/{id}` | 查看就诊历史      |
| 导入导出功能                         |                              | `ImportPatientsCommand`   | `IPatientService.ImportPatientsAsync()`     | `/api/v1/patients/import`           | Excel数据导入   |
|                                |                              | `ExportPatientsCommand`   | `IPatientService.ExportPatientsAsync()`     | `/api/v1/patients/export`           | Excel数据导出   |
|                                |                              | `DownloadTemplateCommand` | `IPatientService.GetImportTemplateAsync()`  | `/api/v1/patients/template`         | 下载导入模板      |
| 状态信息                           |                              | `StatusText`              | -                                           | -                                   | 操作状态提示      |

### PatientAddEditDialogView (患者编辑对话框)

| View组件                        | ViewModel                       | 绑定属性/命令                  | Service调用                                         | 后端API                       | 描述             |
| ----------------------------- | ------------------------------- | ------------------------ | ------------------------------------------------- | --------------------------- | -------------- |
| **PatientAddEditDialog.xaml** | `PatientAddEditDialogViewModel` |                          |                                                   |                             | **患者新增/编辑对话框** |
| 基础信息输入                        |                                 | `Name`                   | -                                                 | -                           | 患者姓名           |
|                               |                                 | `Gender`                 | -                                                 | -                           | 性别选择           |
|                               |                                 | `BirthDate`              | -                                                 | -                           | 出生日期           |
|                               |                                 | `Age`                    | 计算属性                                              | -                           | 年龄(自动计算)       |
|                               |                                 | `IdNumber`               | -                                                 | -                           | 身份证号           |
|                               |                                 | `PhoneNumber`            | -                                                 | -                           | 联系电话           |
|                               |                                 | `Address`                | -                                                 | -                           | 住址信息           |
| 医疗信息                          |                                 | `EmergencyContact`       | -                                                 | -                           | 紧急联系人          |
|                               |                                 | `EmergencyPhone`         | -                                                 | -                           | 紧急联系电话         |
|                               |                                 | `Allergies`              | -                                                 | -                           | 过敏史            |
|                               |                                 | `MedicalHistory`         | -                                                 | -                           | 既往病史           |
| 对话框操作                         |                                 | `SaveCommand`            | `IPatientService.CreateAsync()` / `UpdateAsync()` | `/api/v1/patients`          | 保存患者信息         |
|                               |                                 | `CancelCommand`          | -                                                 | -                           | 取消编辑           |
| 验证逻辑                          |                                 | `ValidatePatientInput()` | 本地验证 + 重复检查                                       | `/api/v1/patients/validate` | 输入数据验证         |

### PatientDetailView (患者详情)

| View组件                     | ViewModel                | 绑定属性/命令                    | Service调用                                     | 后端API                                | 描述           |
| -------------------------- | ------------------------ | -------------------------- | --------------------------------------------- | ------------------------------------ | ------------ |
| **PatientDetailView.xaml** | `PatientDetailViewModel` |                            |                                               |                                      | **患者详细信息查看** |
| 患者基础信息                     |                          | `Patient`                  | `IPatientService.GetByIdAsync()`              | `/api/v1/patients/{id}`              | 患者详细信息       |
| 就诊历史列表                     |                          | `MedicalHistory`           | `IMedicalCaseService.GetByPatientIdAsync()`   | `/api/v1/medicalcases/patient/{id}`  | 就诊历史记录       |
| 处方历史列表                     |                          | `PrescriptionHistory`      | `IPrescriptionsService.GetByPatientIdAsync()` | `/api/v1/prescriptions/patient/{id}` | 用药历史记录       |
| 操作按钮                       |                          | `EditPatientCommand`       | 打开编辑对话框                                       | -                                    | 编辑患者信息       |
|                            |                          | `CreateMedicalCaseCommand` | 导航服务                                          | -                                    | 为患者创建新医案     |
|                            |                          | `BackCommand`              | 导航服务                                          | -                                    | 返回患者列表       |

### PatientImportWizardView (导入向导)

| View组件                       | ViewModel                      | 绑定属性/命令                | Service调用                                   | 后端API                              | 描述           |
| ---------------------------- | ------------------------------ | ---------------------- | ------------------------------------------- | ---------------------------------- | ------------ |
| **PatientImportWizard.xaml** | `PatientImportWizardViewModel` |                        |                                             |                                    | **患者数据导入向导** |
| 文件选择步骤                       |                                | `SelectedFilePath`     | -                                           | -                                  | 选择的Excel文件路径 |
|                              |                                | `SelectFileCommand`    | 文件对话框                                       | -                                  | 打开文件选择对话框    |
| 数据预览步骤                       |                                | `PreviewData`          | Excel解析                                     | -                                  | 导入数据预览       |
|                              |                                | `ValidationResults`    | `IPatientService.ValidateImportDataAsync()` | `/api/v1/patients/validate-import` | 数据验证结果       |
|                              |                                | `PreviewCommand`       | Excel读取                                     | -                                  | 预览导入数据       |
| 导入执行步骤                       |                                | `ImportProgress`       | -                                           | -                                  | 导入进度         |
|                              |                                | `ImportResults`        | -                                           | -                                  | 导入结果统计       |
|                              |                                | `ExecuteImportCommand` | `IPatientService.ImportPatientsAsync()`     | `/api/v1/patients/import`          | 执行批量导入       |
| 向导控制                         |                                | `CanGoNext`            | -                                           | -                                  | 下一步可用性       |
|                              |                                | `CanGoPrevious`        | -                                           | -                                  | 上一步可用性       |
|                              |                                | `NextCommand`          | -                                           | -                                  | 下一步          |
|                              |                                | `PreviousCommand`      | -                                           | -                                  | 上一步          |
|                              |                                | `CancelCommand`        | -                                           | -                                  | 取消导入         |

## 🩺 MedicalCase模块映射

### MedicalCaseListView (医疗案例列表)

| View组件                       | ViewModel                  | 绑定属性/命令                    | Service调用                              | 后端API                                | 描述           |
| ---------------------------- | -------------------------- | -------------------------- | -------------------------------------- | ------------------------------------ | ------------ |
| **MedicalCaseListView.xaml** | `MedicalCaseListViewModel` |                            |                                        |                                      | **医疗案例管理界面** |
| 案例列表DataGrid                 |                            | `MedicalCases`             | `IMedicalCaseService.GetPagedAsync()`  | `/api/v1/medicalcases`               | 医疗案例列表       |
|                              |                            | `SelectedMedicalCase`      | -                                      | -                                    | 选中的医疗案例      |
| 筛选控件                         |                            | `StatusFilter`             | -                                      | -                                    | 状态筛选条件       |
|                              |                            | `DateFromFilter`           | -                                      | -                                    | 开始日期筛选       |
|                              |                            | `DateToFilter`             | -                                      | -                                    | 结束日期筛选       |
|                              |                            | `ApplyFilterCommand`       | 筛选查询                                   | `/api/v1/medicalcases`               | 应用筛选条件       |
|                              |                            | `ClearFilterCommand`       | -                                      | -                                    | 清除筛选条件       |
| 分页控制                         |                            | `CurrentPage`              | -                                      | -                                    | 当前页码         |
|                              |                            | `TotalPages`               | -                                      | -                                    | 总页数          |
|                              |                            | `PageSizeOptions`          | -                                      | -                                    | 页大小选项        |
| 案例操作                         |                            | `CreateMedicalCaseCommand` | 打开创建对话框                                | -                                    | 创建新医疗案例      |
|                              |                            | `ViewDetailsCommand`       | `IMedicalCaseService.GetByIdAsync()`   | `/api/v1/medicalcases/{id}`          | 查看案例详情       |
|                              |                            | `StartConsultationCommand` | 导航服务                                   | -                                    | 开始诊疗流程       |
|                              |                            | `CompleteCommand`          | `IMedicalCaseService.SetStatusAsync()` | `/api/v1/medicalcases/{id}/complete` | 完成案例         |
|                              |                            | `CancelCommand`            | `IMedicalCaseService.SetStatusAsync()` | `/api/v1/medicalcases/{id}/cancel`   | 取消案例         |
| 状态统计                         |                            | `TotalCount`               | -                                      | -                                    | 总案例数         |
|                              |                            | `CompletedCount`           | -                                      | -                                    | 已完成案例数       |
|                              |                            | `InProgressCount`          | -                                      | -                                    | 进行中案例数       |

### CreateMedicalCaseView (创建医疗案例)

| View组件                     | ViewModel                    | 绑定属性/命令                             | Service调用                                  | 后端API                     | 描述            |
| -------------------------- | ---------------------------- | ----------------------------------- | ------------------------------------------ | ------------------------- | ------------- |
| **CreateMedicalCase.xaml** | `CreateMedicalCaseViewModel` |                                     |                                            |                           | **创建医疗案例对话框** |
| 患者选择区                      |                              | `Patients`                          | `IPatientService.GetActivePatientsAsync()` | `/api/v1/patients/active` | 活跃患者列表        |
|                            |                              | `SelectedPatient`                   | -                                          | -                         | 选中的患者         |
|                            |                              | `SearchPatientKeyword`              | -                                          | -                         | 患者搜索关键字       |
|                            |                              | `SearchPatientCommand`              | `IPatientService.SearchAsync()`            | `/api/v1/patients/search` | 搜索患者          |
| 案例信息输入                     |                              | `ChiefComplaint`                    | -                                          | -                         | 主诉            |
|                            |                              | `CaseDescription`                   | -                                          | -                         | 案例描述          |
|                            |                              | `Priority`                          | -                                          | -                         | 优先级           |
|                            |                              | `EstimatedDuration`                 | -                                          | -                         | 预估时长          |
| 操作按钮                       |                              | `CreateCommand`                     | `IMedicalCaseService.CreateAsync()`        | `/api/v1/medicalcases`    | 创建医疗案例        |
|                            |                              | `CreateAndStartConsultationCommand` | 创建+导航                                      | 多API调用                    | 创建并开始诊疗       |
|                            |                              | `CancelCommand`                     | -                                          | -                         | 取消创建          |
| 验证逻辑                       |                              | `ValidateInput()`                   | -                                          | -                         | 输入验证          |

## 🔬 Consultation模块映射

### ConsultationMainView (诊疗主界面)

| View组件                        | ViewModel                   | 绑定属性/命令                       | Service调用                            | 后端API                        | 描述          |
| ----------------------------- | --------------------------- | ----------------------------- | ------------------------------------ | ---------------------------- | ----------- |
| **ConsultationMainView.xaml** | `ConsultationMainViewModel` |                               |                                      |                              | **中医诊疗主界面** |
| 患者信息显示                        |                             | `Patient`                     | `IPatientService.GetByIdAsync()`     | `/api/v1/patients/{id}`      | 当前诊疗患者信息    |
|                               |                             | `MedicalCase`                 | `IMedicalCaseService.GetByIdAsync()` | `/api/v1/medicalcases/{id}`  | 当前医疗案例信息    |
| **望诊Tab**                     |                             |                               |                                      |                              | **望诊记录区域**  |
| 面色记录                          |                             | `Complexion`                  | -                                    | -                            | 面色观察记录      |
| 舌象记录                          |                             | `TongueColor`                 | -                                    | -                            | 舌色记录        |
|                               |                             | `TongueCoating`               | -                                    | -                            | 舌苔记录        |
|                               |                             | `TongueTexture`               | -                                    | -                            | 舌质记录        |
| 精神状态                          |                             | `MentalState`                 | -                                    | -                            | 精神状态观察      |
| 体形体态                          |                             | `BodyBuild`                   | -                                    | -                            | 体形观察        |
| **闻诊Tab**                     |                             |                               |                                      |                              | **闻诊记录区域**  |
| 声音记录                          |                             | `VoiceQuality`                | -                                    | -                            | 声音强弱        |
|                               |                             | `SpeechClarity`               | -                                    | -                            | 语言清晰度       |
| 呼吸观察                          |                             | `BreathingPattern`            | -                                    | -                            | 呼吸状态        |
| 气味记录                          |                             | `BodyOdor`                    | -                                    | -                            | 体味观察        |
| **问诊Tab**                     |                             |                               |                                      |                              | **问诊记录区域**  |
| 主诉                            |                             | `ChiefComplaint`              | -                                    | -                            | 患者主诉        |
| 现病史                           |                             | `PresentIllness`              | -                                    | -                            | 现病史详情       |
| 既往史                           |                             | `PastHistory`                 | -                                    | -                            | 既往病史        |
| 家族史                           |                             | `FamilyHistory`               | -                                    | -                            | 家族病史        |
| 生活史                           |                             | `LifeHistory`                 | -                                    | -                            | 生活习惯        |
| 症状系统回顾                        |                             | `SystemReview`                | -                                    | -                            | 系统症状回顾      |
| **切诊Tab**                     |                             |                               |                                      |                              | **切诊记录区域**  |
| 脉象记录                          |                             | `PulseRate`                   | -                                    | -                            | 脉搏频率        |
|                               |                             | `PulseStrength`               | -                                    | -                            | 脉搏强度        |
|                               |                             | `PulseRhythm`                 | -                                    | -                            | 脉律          |
|                               |                             | `PulseQuality`                | -                                    | -                            | 脉象性质        |
| 按诊记录                          |                             | `Palpation`                   | -                                    | -                            | 按诊结果        |
| **辨证论治Tab**                   |                             |                               |                                      |                              | **诊断与治疗**   |
| 中医诊断                          |                             | `TCMDiagnosis`                | -                                    | -                            | 中医诊断结论      |
| 西医诊断                          |                             | `WesternDiagnosis`            | -                                    | -                            | 西医诊断参考      |
| 证候分析                          |                             | `SyndromeAnalysis`            | -                                    | -                            | 证候辨识        |
| 治疗原则                          |                             | `TreatmentPrinciple`          | -                                    | -                            | 治疗原则        |
| 治疗方案                          |                             | `TreatmentPlan`               | -                                    | -                            | 具体治疗方案      |
| 诊疗操作                          |                             | `SaveConsultationCommand`     | `IConsultationService.CreateAsync()` | `/api/v1/consultations`      | 保存诊疗记录      |
|                               |                             | `UpdateConsultationCommand`   | `IConsultationService.UpdateAsync()` | `/api/v1/consultations/{id}` | 更新诊疗记录      |
|                               |                             | `PrescribeMedicineCommand`    | 导航服务                                 | -                            | 开具处方        |
|                               |                             | `CompleteConsultationCommand` | 完成诊疗                                 | 多API调用                       | 完成本次诊疗      |
|                               |                             | `CancelConsultationCommand`   | -                                    | -                            | 取消诊疗        |
| 导航控制                          |                             | `NavigationParameters`        | -                                    | -                            | 导航参数处理      |
|                               |                             | `CanNavigateAway`             | -                                    | -                            | 导航离开确认      |

## 💊 Prescriptions模块映射

### PrescriptionManagementView (处方管理)

| View组件                              | ViewModel                         | 绑定属性/命令                     | Service调用                               | 后端API                          | 描述          |
| ----------------------------------- | --------------------------------- | --------------------------- | --------------------------------------- | ------------------------------ | ----------- |
| **PrescriptionManagementView.xaml** | `PrescriptionManagementViewModel` |                             |                                         |                                | **处方管理主界面** |
| 处方列表DataGrid                        |                                   | `Prescriptions`             | `IPrescriptionsService.GetPagedAsync()` | `/api/v1/prescriptions`        | 处方列表数据      |
|                                     |                                   | `SelectedPrescription`      | -                                       | -                              | 选中的处方       |
| 筛选搜索区                               |                                   | `SearchKeyword`             | -                                       | -                              | 搜索关键字       |
|                                     |                                   | `PatientNameFilter`         | -                                       | -                              | 患者姓名筛选      |
|                                     |                                   | `DateFromFilter`            | -                                       | -                              | 开始日期筛选      |
|                                     |                                   | `DateToFilter`              | -                                       | -                              | 结束日期筛选      |
|                                     |                                   | `StatusFilter`              | -                                       | -                              | 处方状态筛选      |
|                                     |                                   | `SearchCommand`             | `IPrescriptionsService.SearchAsync()`   | `/api/v1/prescriptions/search` | 执行搜索        |
| 分页控制                                |                                   | `CurrentPage`               | -                                       | -                              | 当前页码        |
|                                     |                                   | `TotalPages`                | -                                       | -                              | 总页数         |
| 处方操作                                |                                   | `ViewDetailsCommand`        | `IPrescriptionsService.GetByIdAsync()`  | `/api/v1/prescriptions/{id}`   | 查看处方详情      |
|                                     |                                   | `EditPrescriptionCommand`   | 打开编辑器                                   | -                              | 编辑处方        |
|                                     |                                   | `PrintPrescriptionCommand`  | 打印服务                                    | -                              | 打印处方        |
|                                     |                                   | `CopyPrescriptionCommand`   | 复制处方                                    | -                              | 复制为新处方      |
|                                     |                                   | `DeletePrescriptionCommand` | `IPrescriptionsService.DeleteAsync()`   | `/api/v1/prescriptions/{id}`   | 删除处方        |
| 统计信息                                |                                   | `TotalCount`                | -                                       | -                              | 处方总数        |
|                                     |                                   | `TodayCount`                | -                                       | -                              | 今日处方数       |
|                                     |                                   | `MonthCount`                | -                                       | -                              | 本月处方数       |

### PrescriptionEditorDialogView (处方编辑器)

| View组件                      | ViewModel                           | 绑定属性/命令                     | Service调用                                               | 后端API                                 | 描述          |
| --------------------------- | ----------------------------------- | --------------------------- | ------------------------------------------------------- | ------------------------------------- | ----------- |
| **PrescriptionEditor.xaml** | `PrescriptionEditorDialogViewModel` |                             |                                                         |                                       | **处方编辑对话框** |
| 基本信息区                       |                                     | `Patient`                   | `IPatientService.GetByIdAsync()`                        | `/api/v1/patients/{id}`               | 患者信息显示      |
|                             |                                     | `PrescriptionDate`          | -                                                       | -                                     | 开方日期        |
|                             |                                     | `Doctor`                    | 当前医生信息                                                  | -                                     | 开方医生        |
| 药材选择区                       |                                     | `AvailableHerbs`            | `IHerbService.GetAllAsync()`                            | `/api/v1/herbs`                       | 可选药材列表      |
|                             |                                     | `SearchHerbKeyword`         | -                                                       | -                                     | 药材搜索关键字     |
|                             |                                     | `SearchHerbCommand`         | `IHerbService.SearchAsync()`                            | `/api/v1/herbs/search`                | 搜索药材        |
|                             |                                     | `AddHerbCommand`            | -                                                       | -                                     | 添加药材到处方     |
| 处方药材列表                      |                                     | `PrescriptionItems`         | -                                                       | -                                     | 处方药材明细      |
|                             |                                     | `SelectedPrescriptionItem`  | -                                                       | -                                     | 选中的药材项      |
|                             |                                     | `RemoveHerbCommand`         | -                                                       | -                                     | 移除药材        |
|                             |                                     | `UpdateDosageCommand`       | -                                                       | -                                     | 更新药材剂量      |
| 剂量设置                        |                                     | `Dosage`                    | -                                                       | -                                     | 药材剂量        |
|                             |                                     | `Unit`                      | -                                                       | -                                     | 剂量单位        |
|                             |                                     | `Usage`                     | -                                                       | -                                     | 用法用量        |
|                             |                                     | `Frequency`                 | -                                                       | -                                     | 服用频率        |
| 验方选择                        |                                     | `AvailableFormulas`         | `IFormulaService.GetAllAsync()`                         | `/api/v1/formulas`                    | 可用验方列表      |
|                             |                                     | `SelectedFormula`           | -                                                       | -                                     | 选中的验方       |
|                             |                                     | `ApplyFormulaCommand`       | 验方应用逻辑                                                  | -                                     | 应用验方到处方     |
| 配伍检查                        |                                     | `CompatibilityResults`      | `IPrescriptionsService.CheckCompatibilityAsync()`       | `/api/v1/prescriptions/compatibility` | 配伍禁忌检查      |
|                             |                                     | `CheckCompatibilityCommand` | 配伍检查服务                                                  | -                                     | 执行配伍检查      |
| 处方操作                        |                                     | `SavePrescriptionCommand`   | `IPrescriptionsService.CreateAsync()` / `UpdateAsync()` | `/api/v1/prescriptions`               | 保存处方        |
|                             |                                     | `PreviewPrintCommand`       | 打印预览                                                    | -                                     | 预览打印效果      |
|                             |                                     | `CancelCommand`             | -                                                       | -                                     | 取消编辑        |
| 计算显示                        |                                     | `TotalCost`                 | 计算属性                                                    | -                                     | 处方总费用       |
|                             |                                     | `ItemCount`                 | 计算属性                                                    | -                                     | 药材种类数量      |

## 🌿 Herbs模块映射

### HerbManagementView (药材管理)

| View组件                      | ViewModel                 | 绑定属性/命令                    | Service调用                           | 后端API                       | 描述          |
| --------------------------- | ------------------------- | -------------------------- | ----------------------------------- | --------------------------- | ----------- |
| **HerbManagementView.xaml** | `HerbManagementViewModel` |                            |                                     |                             | **药材管理主界面** |
| 药材列表DataGrid                |                           | `Herbs`                    | `IHerbService.GetPagedAsync()`      | `/api/v1/herbs`             | 药材列表数据      |
|                             |                           | `SelectedHerb`             | -                                   | -                           | 选中的药材       |
| 搜索筛选区                       |                           | `SearchKeyword`            | -                                   | -                           | 搜索关键字       |
|                             |                           | `CategoryFilter`           | -                                   | -                           | 分类筛选        |
|                             |                           | `ActiveOnlyFilter`         | -                                   | -                           | 仅显示活跃药材     |
|                             |                           | `SearchCommand`            | `IHerbService.SearchAsync()`        | `/api/v1/herbs/search`      | 执行搜索        |
| 分类管理                        |                           | `Categories`               | `IHerbService.GetCategoriesAsync()` | `/api/v1/herbs/categories`  | 药材分类列表      |
| 分页控制                        |                           | `CurrentPage`              | -                                   | -                           | 当前页码        |
|                             |                           | `TotalPages`               | -                                   | -                           | 总页数         |
| 药材操作                        |                           | `AddHerbCommand`           | 打开编辑对话框                             | -                           | 添加新药材       |
|                             |                           | `EditHerbCommand`          | `IHerbService.GetByIdAsync()`       | `/api/v1/herbs/{id}`        | 编辑选中药材      |
|                             |                           | `DeleteHerbCommand`        | `IHerbService.DeleteAsync()`        | `/api/v1/herbs/{id}`        | 删除药材        |
|                             |                           | `ToggleStatusCommand`      | `IHerbService.SetStatusAsync()`     | `/api/v1/herbs/{id}/status` | 启用/禁用药材     |
| 批量操作                        |                           | `BatchImportCommand`       | `IHerbService.ImportHerbsAsync()`   | `/api/v1/herbs/import`      | 批量导入药材      |
|                             |                           | `BatchExportCommand`       | `IHerbService.ExportHerbsAsync()`   | `/api/v1/herbs/export`      | 批量导出药材      |
|                             |                           | `BatchUpdatePricesCommand` | 批量价格更新                              | -                           | 批量更新价格      |
| 统计信息                        |                           | `TotalHerbCount`           | -                                   | -                           | 药材总数        |
|                             |                           | `ActiveHerbCount`          | -                                   | -                           | 活跃药材数       |
|                             |                           | `CategoryCount`            | -                                   | -                           | 分类数量        |

### HerbAddEditDialogView (药材编辑对话框)

| View组件                     | ViewModel                    | 绑定属性/命令               | Service调用                                      | 后端API           | 描述             |
| -------------------------- | ---------------------------- | --------------------- | ---------------------------------------------- | --------------- | -------------- |
| **HerbAddEditDialog.xaml** | `HerbAddEditDialogViewModel` |                       |                                                |                 | **药材新增/编辑对话框** |
| 基础信息输入                     |                              | `Name`                | -                                              | -               | 药材名称           |
|                            |                              | `Alias`               | -                                              | -               | 别名             |
|                            |                              | `Category`            | -                                              | -               | 药材分类           |
|                            |                              | `Origin`              | -                                              | -               | 产地             |
|                            |                              | `Specification`       | -                                              | -               | 规格             |
| 中药属性                       |                              | `Nature`              | -                                              | -               | 性味 (寒、热、温、凉、平) |
|                            |                              | `Flavor`              | -                                              | -               | 味 (酸、苦、甘、辛、咸)  |
|                            |                              | `Meridian`            | -                                              | -               | 归经             |
|                            |                              | `Function`            | -                                              | -               | 功效             |
|                            |                              | `Indication`          | -                                              | -               | 主治             |
| 价格管理                       |                              | `PurchasePrice`       | -                                              | -               | 采购价格           |
|                            |                              | `RetailPrice`         | -                                              | -               | 零售价格           |
|                            |                              | `Unit`                | -                                              | -               | 计量单位           |
| 使用信息                       |                              | `Dosage`              | -                                              | -               | 常用剂量           |
|                            |                              | `Contraindication`    | -                                              | -               | 禁忌             |
|                            |                              | `Attention`           | -                                              | -               | 注意事项           |
|                            |                              | `IsActive`            | -                                              | -               | 活跃状态           |
| 对话框操作                      |                              | `SaveCommand`         | `IHerbService.CreateAsync()` / `UpdateAsync()` | `/api/v1/herbs` | 保存药材信息         |
|                            |                              | `CancelCommand`       | -                                              | -               | 取消编辑           |
| 验证逻辑                       |                              | `ValidateHerbInput()` | 本地验证                                           | -               | 输入数据验证         |

## 📋 Formula模块映射

### FormulaManagementView (验方管理)

| View组件                         | ViewModel                    | 绑定属性/命名                 | Service调用                         | 后端API                     | 描述          |
| ------------------------------ | ---------------------------- | ----------------------- | --------------------------------- | ------------------------- | ----------- |
| **FormulaManagementView.xaml** | `FormulaManagementViewModel` |                         |                                   |                           | **验方管理主界面** |
| 验方列表DataGrid                   |                              | `Formulas`              | `IFormulaService.GetPagedAsync()` | `/api/v1/formulas`        | 验方列表数据      |
|                                |                              | `SelectedFormula`       | -                                 | -                         | 选中的验方       |
| 搜索筛选区                          |                              | `SearchKeyword`         | -                                 | -                         | 搜索关键字       |
|                                |                              | `CategoryFilter`        | -                                 | -                         | 分类筛选        |
|                                |                              | `SourceFilter`          | -                                 | -                         | 来源筛选        |
|                                |                              | `SearchCommand`         | `IFormulaService.SearchAsync()`   | `/api/v1/formulas/search` | 执行搜索        |
| 分页控制                           |                              | `CurrentPage`           | -                                 | -                         | 当前页码        |
|                                |                              | `TotalPages`            | -                                 | -                         | 总页数         |
| 验方操作                           |                              | `AddFormulaCommand`     | 打开编辑对话框                           | -                         | 添加新验方       |
|                                |                              | `EditFormulaCommand`    | `IFormulaService.GetByIdAsync()`  | `/api/v1/formulas/{id}`   | 编辑选中验方      |
|                                |                              | `DeleteFormulaCommand`  | `IFormulaService.DeleteAsync()`   | `/api/v1/formulas/{id}`   | 删除验方        |
|                                |                              | `ViewDetailsCommand`    | 显示详情                              | -                         | 查看验方详情      |
|                                |                              | `CopyFormulaCommand`    | 复制验方                              | -                         | 复制为新验方      |
|                                |                              | `ApplyToPatientCommand` | 导航到处方                             | -                         | 将验方应用到患者    |
| 统计信息                           |                              | `TotalFormulaCount`     | -                                 | -                         | 验方总数        |
|                                |                              | `ClassicFormulaCount`   | -                                 | -                         | 经典验方数       |
|                                |                              | `PersonalFormulaCount`  | -                                 | -                         | 个人验方数       |

### FormulaAddEditDialogView (验方编辑对话框)

| View组件                        | ViewModel                       | 绑定属性/命令                   | Service调用                                         | 后端API              | 描述             |
| ----------------------------- | ------------------------------- | ------------------------- | ------------------------------------------------- | ------------------ | -------------- |
| **FormulaAddEditDialog.xaml** | `FormulaAddEditDialogViewModel` |                           |                                                   |                    | **验方新增/编辑对话框** |
| 验方基础信息                        |                                 | `Name`                    | -                                                 | -                  | 验方名称           |
|                               |                                 | `Source`                  | -                                                 | -                  | 方剂来源           |
|                               |                                 | `Category`                | -                                                 | -                  | 方剂分类           |
|                               |                                 | `Function`                | -                                                 | -                  | 方剂功效           |
|                               |                                 | `Indication`              | -                                                 | -                  | 主治病症           |
|                               |                                 | `Composition`             | -                                                 | -                  | 方剂组成说明         |
| 药材组成管理                        |                                 | `FormulaHerbs`            | -                                                 | -                  | 方剂药材组成列表       |
|                               |                                 | `AvailableHerbs`          | `IHerbService.GetAllAsync()`                      | `/api/v1/herbs`    | 可选药材列表         |
|                               |                                 | `SelectedHerb`            | -                                                 | -                  | 选中的药材          |
|                               |                                 | `AddHerbCommand`          | -                                                 | -                  | 添加药材到验方        |
|                               |                                 | `RemoveHerbCommand`       | -                                                 | -                  | 从验方中移除药材       |
|                               |                                 | `UpdateHerbDosageCommand` | -                                                 | -                  | 更新药材剂量         |
| 药材剂量设置                        |                                 | `HerbDosage`              | -                                                 | -                  | 药材剂量           |
|                               |                                 | `HerbUnit`                | -                                                 | -                  | 剂量单位           |
|                               |                                 | `HerbFunction`            | -                                                 | -                  | 该药材在方中的作用      |
| 使用指导                          |                                 | `Usage`                   | -                                                 | -                  | 用法用量           |
|                               |                                 | `Preparation`             | -                                                 | -                  | 制备方法           |
|                               |                                 | `Contraindication`        | -                                                 | -                  | 禁忌症            |
|                               |                                 | `Attention`               | -                                                 | -                  | 注意事项           |
|                               |                                 | `ModernResearch`          | -                                                 | -                  | 现代研究           |
| 对话框操作                         |                                 | `SaveFormulaCommand`      | `IFormulaService.CreateAsync()` / `UpdateAsync()` | `/api/v1/formulas` | 保存验方信息         |
|                               |                                 | `PreviewCommand`          | -                                                 | -                  | 预览验方           |
|                               |                                 | `CancelCommand`           | -                                                 | -                  | 取消编辑           |
| 验证逻辑                          |                                 | `ValidateFormulaInput()`  | 本地验证                                              | -                  | 输入数据验证         |

## 📊 数据绑定和命令模式总结

### 通用绑定模式

#### 1. 列表数据绑定模式

```xaml
<DataGrid ItemsSource="{Binding Items}" 
          SelectedItem="{Binding SelectedItem}"
          AutoGenerateColumns="False">
```

#### 2. 搜索和分页绑定模式

```xaml
<TextBox Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"/>
<Button Command="{Binding SearchCommand}" Content="搜索"/>
<TextBlock Text="{Binding CurrentPage}"/>
<Button Command="{Binding FirstPageCommand}" Content="首页"/>
```

#### 3. 命令绑定模式

```xaml
<Button Command="{Binding SaveCommand}" Content="保存"/>
<Button Command="{Binding CancelCommand}" Content="取消"/>
<Button Command="{Binding DeleteCommand}" 
        CommandParameter="{Binding SelectedItem}"/>
```

### Service调用模式

#### 1. 异步数据加载模式

```csharp
public async Task LoadDataAsync()
{
    try
    {
        ShowLoading("正在加载数据...");
        var result = await _service.GetPagedAsync(query);

        if (result.IsSuccess)
        {
            Items = new ObservableCollection<T>(result.Data.Items);
            TotalPages = result.Data.TotalPages;
        }
    }
    catch (Exception ex)
    {
        LogError(ex, "加载数据失败");
        ShowError("加载数据失败，请重试");
    }
    finally
    {
        HideLoading();
    }
}
```

#### 2. 命令处理模式

```csharp
public AsyncRelayCommand<T> EditCommand { get; }

private async Task EditItemAsync(T item)
{
    try
    {
        var result = await _service.UpdateAsync(item.Id, updateDto);

        if (result.IsSuccess)
        {
            ShowSuccess("编辑成功");
            await LoadDataAsync(); // 刷新列表
        }
        else
        {
            ShowError(result.Message);
        }
    }
    catch (Exception ex)
    {
        LogError(ex, "编辑失败");
        ShowError("编辑失败，请重试");
    }
}
```

### API调用统计

| 模块                | ViewModel数量 | Command数量 | Service调用数量 | API端点数量 |
| ----------------- | ----------- | --------- | ----------- | ------- |
| **Shell**         | 2           | 15        | 5           | 8       |
| **Auth**          | 1           | 3         | 3           | 4       |
| **Users**         | 2           | 12        | 8           | 9       |
| **Patients**      | 4           | 28        | 15          | 12      |
| **MedicalCase**   | 2           | 10        | 6           | 7       |
| **Consultation**  | 1           | 8         | 4           | 5       |
| **Prescriptions** | 2           | 15        | 9           | 8       |
| **Herbs**         | 2           | 14        | 10          | 10      |
| **Formula**       | 2           | 12        | 8           | 8       |
| **合计**            | **18**      | **117**   | **68**      | **71**  |

---

**总结**: 前端架构映射表展示了LYBTZYZS系统前端WPF/Prism架构的完整数据绑定和服务调用关系。通过MVVM模式实现了清晰的职责分离，UltraThink双层架构确保了高效的后端API集成，为中医诊所管理系统提供了结构化、可维护的前端解决方案。