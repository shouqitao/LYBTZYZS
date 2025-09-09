# LYBT系统功能清单与架构分析

> **凌隐宝堂中医诊所管理系统** - 基于.NET 8的现代化中医诊所管理解决方案  
> **分析时间**: 2025-01-31  
> **架构标准**: UltraThink双层架构 + 传统三层架构混合设计

## 🎯 系统概览

### 技术架构
- **前端**: WPF + Prism.DryIoc + MVVM + UltraThink双层架构
- **后端**: ASP.NET Core 8 Web API + EF Core + 传统三层架构  
- **数据库**: SQL Server + 统一AppDbContext
- **通信**: Refit (类型安全REST客户端)
- **认证**: JWT Bearer Token + RBAC权限控制

### 核心业务模块 (8个)
1. **Auth** - 身份认证与授权
2. **Users** - 用户管理  
3. **Patients** - 患者档案
4. **MedicalCase** - 医疗案例
5. **Consultation** - 看诊诊断 (中医四诊)
6. **Prescriptions** - 处方管理
7. **Herbs** - 中药材管理
8. **Formula** - 验方管理

## 📋 Project级功能清单

### 🖥️ 前端项目 (WPF/Prism)

#### 1. Shell层 (主框架)
**项目**: `LYBT.Desktop.Shell`  
**路径**: `src/Client/Desktop/Shell/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | 关键依赖 |
|---------|--------|---------|------|---------|
| 主窗口管理 | `MainWindowViewModel` | Core | MainWindow.xaml | IRegionManager, IEventAggregator |
| 首页工作台 | `HomeViewModel.LoadTodayPatientsAsync()` | Business | HomeView.xaml | IPatientService, IMedicalCaseService |
| 应用启动 | `App.OnStartup()` | Core | App.xaml.cs | Prism容器初始化 |
| 依赖注入 | `ServiceCollectionExtensions.RegisterServices()` | Core | - | 所有业务模块服务 |

**证据链接**: 
- 主窗口: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:577`
- 首页工作台: `src/Client/Desktop/Shell/ViewModels/HomeViewModel.cs:LoadTodayPatientsAsync`
- 服务注册: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

#### 2. 核心基础设施
**项目**: `LYBT.Desktop.Core`  
**路径**: `src/Client/Desktop/Core/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | 关键依赖 |
|---------|--------|---------|------|---------|
| MVVM基类 | `SessionAwareViewModel` | Core | 所有ViewModel基类 | ISessionManager |
| 异步命令 | `AsyncRelayCommand<T>` | Core | UI绑定 | Task异步模式 |
| 数据分页 | `PaginationCoordinator` | Core | 列表视图 | IPaginationCoordinator |
| 全局异常处理 | `GlobalExceptionHandler.HandleException()` | Core | 全局异常 | ILogger, INotificationService |
| API服务基类 | `BaseApiService` | Core | 所有API调用 | HttpClient, Polly重试 |
| 打印服务 | `PrescriptionPrintService.PrintPrescriptionAsync()` | Business | 处方打印 | IPrescriptionPrintService |

**证据链接**:
- MVVM基类: `src/Client/Desktop/Core/ViewModels/Base/SessionAwareViewModel.cs`
- 异步命令: `src/Client/Desktop/Core/Mvvm/AsyncRelayCommand.cs`
- 全局异常: `src/Client/Desktop/Core/Services/GlobalExceptionHandler.cs:348-426`

#### 3. 业务模块 (8个核心模块)

##### 3.1 认证模块 (Auth)
**项目**: `LYBT.Desktop.Auth`  
**路径**: `src/Client/Desktop/Modules/Auth/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | DTO/实体 |
|---------|--------|---------|------|---------|
| 用户登录 | `LoginViewModel.LoginAsync()` | Business | LoginView.xaml | LoginRequest/LoginResponse |
| 登录验证 | `AuthBusinessService.LoginAsync()` | Business | 登录命令 | AuthDto |
| JWT令牌管理 | `AuthServiceAdapter.ValidateTokenAsync()` | Core | 令牌验证 | JWT Claims |
| 会话管理 | `AuthQueryService.GetCurrentUserAsync()` | Core | 用户状态 | UserDto |

**API端点**:
- `POST /api/v1/auth/login` - 用户登录
- `POST /api/v1/auth/logout` - 用户退出  
- `POST /api/v1/auth/refresh` - 令牌刷新

**证据链接**:
- 登录视图模型: `src/Client/Desktop/Modules/Auth/ViewModels/LoginViewModel.cs`
- 业务服务: `src/Client/Desktop/Modules/Auth/Services/AuthBusinessService.cs`

##### 3.2 患者管理模块 (Patients)
**项目**: `LYBT.Desktop.Patients`  
**路径**: `src/Client/Desktop/Modules/Patients/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | DTO/实体 |
|---------|--------|---------|------|---------|
| 患者列表 | `PatientManagementViewModel.LoadDataAsync()` | Business | PatientManagementView.xaml | PatientDto |
| 患者CRUD | `PatientBusinessService.CreateAsync()` | Business | 添加/编辑对话框 | PatientCreateDto/PatientUpdateDto |
| 患者搜索 | `PatientQueryService.SearchAsync()` | Business | 搜索框 | PatientPagedQueryDto |
| 患者历史 | `PatientManagementViewModel.ViewHistoryAsync()` | Business | 历史按钮 | MedicalCaseDto列表 |
| Excel导入导出 | `PatientManagementViewModel.ExportPatientsAsync()` | Business | 导入导出按钮 | Excel文件 |

**API端点**:
- `GET /api/v1/patients` - 分页查询患者
- `GET /api/v1/patients/{id}` - 获取患者详情
- `POST /api/v1/patients` - 创建患者
- `PUT /api/v1/patients/{id}` - 更新患者
- `DELETE /api/v1/patients/{id}` - 删除患者

**证据链接**:
- 患者管理: `src/Client/Desktop/Modules/Patients/ViewModels/PatientManagementViewModel.cs:ViewHistoryAsync`
- 导入导出: `src/Client/Desktop/Modules/Patients/ViewModels/PatientManagementViewModel.cs:ExportPatientsAsync`

##### 3.3 医案管理模块 (MedicalCase)  
**项目**: `LYBT.Desktop.MedicalCase`
**路径**: `src/Client/Desktop/Modules/MedicalCase/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | DTO/实体 |
|---------|--------|---------|------|---------|
| 医案列表 | `MedicalCaseManagementViewModel.LoadDataAsync()` | Business | MedicalCaseManagementView.xaml | MedicalCaseDto |
| 医案详情 | `MedicalCaseDetailViewModel.LoadMedicalCaseAsync()` | Business | MedicalCaseDetailView.xaml | MedicalCaseDetailDto |
| 开始看诊 | `MedicalCaseDetailViewModel.StartConsultationAsync()` | Business | 开始看诊按钮 | 导航到ConsultationMainView |
| 医案状态管理 | `MedicalCaseBusinessService.UpdateStatusAsync()` | Business | 状态切换 | MedicalCaseStatus枚举 |

**API端点**:
- `GET /api/v1/medicalcase` - 分页查询医案
- `GET /api/v1/medicalcase/{id}` - 获取医案详情
- `POST /api/v1/medicalcase` - 创建医案
- `PUT /api/v1/medicalcase/{id}/status` - 更新医案状态

**证据链接**:
- 医案详情: `src/Client/Desktop/Modules/MedicalCase/ViewModels/MedicalCaseDetailViewModel.cs:StartConsultationAsync`
- 状态管理: `src/Client/Desktop/Modules/MedicalCase/ViewModels/MedicalCaseDetailViewModel.cs:CanStartConsultation`

##### 3.4 看诊诊断模块 (Consultation)
**项目**: `LYBT.Desktop.Consultation`  
**路径**: `src/Client/Desktop/Modules/Consultation/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | DTO/实体 |
|---------|--------|---------|------|---------|
| 四诊录入 | `ConsultationMainViewModel.SaveConsultationAsync()` | Business | ConsultationMainView.xaml | ConsultationDto |
| 中医四诊数据 | `ConsultationDto.{Inspection,Auscultation,Inquiry,Palpation}` | Business | 四诊输入框 | 望闻问切字段 |
| 诊断记录 | `ConsultationBusinessService.CreateAsync()` | Business | 保存诊疗按钮 | ConsultationCreateDto |
| 看诊管理 | `ConsultationManagementViewModel.LoadDataAsync()` | Business | ConsultationManagementView.xaml | 看诊记录列表 |

**API端点**:
- `GET /api/v1/consultation` - 分页查询看诊记录
- `POST /api/v1/consultation/start` - 开始看诊
- `PUT /api/v1/consultation/{id}` - 更新看诊记录

**证据链接**:
- 四诊录入界面: `src/Client/Desktop/Modules/Consultation/Views/ConsultationMainView.xaml:102-143`
- 看诊业务服务: `src/Client/Desktop/Modules/Consultation/Services/ConsultationBusinessService.cs`

##### 3.5 处方管理模块 (Prescriptions)
**项目**: `LYBT.Desktop.Prescriptions`  
**路径**: `src/Client/Desktop/Modules/Prescriptions/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | DTO/实体 |
|---------|--------|---------|------|---------|
| 处方编辑 | `PrescriptionEditorDialogViewModel.SaveAsync()` | Business | PrescriptionEditorDialog.xaml | PrescriptionDto |
| 处方组合 | `PrescriptionComposerViewModel.AddHerbAsync()` | Business | 添加药材按钮 | PrescriptionItemDto |
| 价格计算 | `PrescriptionCalculator.CalculateTotal()` | Business | 价格计算 | PrescriptionCalculationDto |
| 处方打印 | `PrescriptionCommandHandler.PrintPreviewAsync()` | Business | 打印预览按钮 | PrintPreviewDialog |
| 验方模板 | `FormulaTemplateDialogViewModel.SelectFormulaAsync()` | Business | 验方选择对话框 | FormulaDto |

**API端点**:
- `GET /api/v1/prescriptions` - 分页查询处方
- `POST /api/v1/prescriptions` - 创建处方  
- `PUT /api/v1/prescriptions/{id}` - 更新处方

**证据链接**:
- 处方编辑器: `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionEditorDialogViewModel.cs:CreatePrintData`
- 打印命令: `src/Client/Desktop/Modules/Prescriptions/ViewModels/Components/PrescriptionCommandHandler.cs:PrintPreviewAsync`

##### 3.6 药材管理模块 (Herbs)
**项目**: `LYBT.Desktop.Herbs`  
**路径**: `src/Client/Desktop/Modules/Herbs/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | DTO/实体 |
|---------|--------|---------|------|---------|
| 药材管理 | `HerbManagementViewModel.LoadDataAsync()` | Business | HerbManagementView.xaml | HerbDto |
| 药材CRUD | `HerbBusinessService.CreateAsync()` | Business | 添加编辑对话框 | HerbCreateDto/HerbUpdateDto |
| 药材详情 | `HerbDetailViewModel.LoadHerbAsync()` | Business | HerbDetailView.xaml | 药材详细信息 |

**API端点**:
- `GET /api/v1/herbs` - 分页查询药材
- `POST /api/v1/herbs` - 创建药材
- `PUT /api/v1/herbs/{id}` - 更新药材

##### 3.7 验方管理模块 (Formula)
**项目**: `LYBT.Desktop.Formula`  
**路径**: `src/Client/Desktop/Modules/Formula/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | DTO/实体 |
|---------|--------|---------|------|---------|
| 验方管理 | `FormulaManagementViewModel.LoadDataAsync()` | Business | FormulaManagementView.xaml | FormulaDto |
| 验方CRUD | `FormulaBusinessService.CreateAsync()` | Business | 添加编辑对话框 | FormulaCreateDto/FormulaUpdateDto |
| 验方详情 | `FormulaDetailViewModel.LoadFormulaAsync()` | Business | FormulaDetailView.xaml | 验方药材组成 |

**API端点**:
- `GET /api/v1/formulas` - 分页查询验方
- `POST /api/v1/formulas` - 创建验方
- `PUT /api/v1/formulas/{id}` - 更新验方

##### 3.8 用户管理模块 (Users)
**项目**: `LYBT.Desktop.Users`  
**路径**: `src/Client/Desktop/Modules/Users/`

| 功能模块 | 类/方法 | 功能类型 | 入口 | DTO/实体 |
|---------|--------|---------|------|---------|
| 用户管理 | `UserManagementViewModel.LoadDataAsync()` | Business | UserManagementView.xaml | UserDto |
| 用户CRUD | `UserBusinessService.CreateAsync()` | Business | 添加编辑对话框 | UserCreateDto/UserUpdateDto |
| 密码管理 | `UserBusinessService.ChangePasswordAsync()` | Business | 修改密码对话框 | ChangePasswordDto |

**API端点**:
- `GET /api/v1/users` - 分页查询用户
- `POST /api/v1/users` - 创建用户
- `PUT /api/v1/users/{id}` - 更新用户
- `PUT /api/v1/users/{id}/password` - 修改密码

### 🔧 后端项目 (ASP.NET Core Web API)

#### 1. Web API入口
**项目**: `LYBT.WebAPI`  
**路径**: `src/Server/Services/LYBT.WebAPI/`

| 功能模块 | 类/方法 | 功能类型 | HTTP路由 | 授权策略 |
|---------|--------|---------|---------|---------|
| 认证控制器 | `AuthController.LoginAsync()` | Business | `POST /api/v1/auth/login` | AllowAnonymous |
| 患者控制器 | `PatientsController.GetPagedAsync()` | Business | `GET /api/v1/patients` | [Authorize] |
| 医案控制器 | `MedicalCaseController.CreateAsync()` | Business | `POST /api/v1/medicalcase` | [Authorize] |
| 看诊控制器 | `ConsultationController.StartConsultationAsync()` | Business | `POST /api/v1/consultation/start` | [Authorize] |
| 处方控制器 | `PrescriptionsController.GetByIdAsync()` | Business | `GET /api/v1/prescriptions/{id}` | [Authorize] |
| 药材控制器 | `HerbsController.GetPagedAsync()` | Business | `GET /api/v1/herbs` | [Authorize] |
| 验方控制器 | `FormulasController.CreateAsync()` | Business | `POST /api/v1/formulas` | [Authorize] |
| 用户控制器 | `UsersController.GetPagedAsync()` | Business | `GET /api/v1/users` | [Authorize(Roles="Admin")] |

**证据链接**:
- 控制器基类: `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`
- 认证控制器: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`

#### 2. 业务模块服务 (8个模块)

##### 2.1 认证模块 (Auth)
**项目**: `LYBT.Module.Auth`  
**路径**: `src/Server/Modules/LYBT.Module.Auth/`

| 功能模块 | 类/方法 | 功能类型 | 数据访问 | 关键依赖 |
|---------|--------|---------|---------|---------|
| JWT认证服务 | `JwtAuthenticationService.GenerateTokenAsync()` | Core | AuthSessionRepository | IOptions<JwtOptions> |
| 认证业务服务 | `AuthBusinessService.AuthenticateAsync()` | Business | AuthRepository | 密码哈希验证 |
| 认证查询服务 | `AuthQueryService.ValidateTokenAsync()` | Core | JWT Token解析 | ITokenValidation |
| 会话管理 | `AuthSessionRepository.CreateSessionAsync()` | Infra | AuthSession表 | EF Core DbContext |

**证据链接**:
- JWT服务: `src/Server/Modules/LYBT.Module.Auth/Services/JwtAuthenticationService.cs`
- 业务服务: `src/Server/Modules/LYBT.Module.Auth/Services/AuthBusinessService.cs`

##### 2.2 患者模块 (Patients)
**项目**: `LYBT.Module.Patients`  
**路径**: `src/Server/Modules/LYBT.Module.Patients/`

| 功能模块 | 类/方法 | 功能类型 | 数据访问 | LINQ查询特征 |
|---------|--------|---------|---------|---------|
| 患者业务服务 | `PatientBusinessService.CreateAsync()` | Business | OptimizedPatientRepository | ExecuteUpdate批量操作 |
| 患者查询服务 | `PatientQueryService.GetPagedAsync()` | Business | 分页查询 | AsNoTracking, Skip/Take分页 |
| 患者仓储 | `OptimizedPatientRepository.FindByConditionAsync()` | Infra | Patients表 | 条件查询+性能优化 |

**证据链接**:
- 优化仓储: `src/Server/Modules/LYBT.Module.Patients/Repositories/OptimizedPatientRepository.cs`

##### 2.3 其他业务模块
类似结构，每个模块都包含：
- **BusinessService**: 业务逻辑处理
- **QueryService**: 复杂查询专用
- **Repository**: 数据访问层
- **MappingProfile**: AutoMapper配置

#### 3. 基础设施层
**项目**: `LYBT.Infrastructure`  
**路径**: `src/Server/Core/LYBT.Infrastructure/`

| 功能模块 | 类/方法 | 功能类型 | 关键特性 |
|---------|--------|---------|---------|
| 统一数据上下文 | `AppDbContext.OnModelCreating()` | Core | 所有实体配置 |
| 基础仓储 | `BaseRepository<T>.GetPagedAsync()` | Core | 通用CRUD+分页 |
| 数据库迁移 | `DatabaseInitializationService.MigrateAsync()` | Core | 自动迁移+种子数据 |
| 全局异常处理 | `GlobalExceptionMiddleware.InvokeAsync()` | Core | 统一异常处理 |

**证据链接**:
- 数据上下文: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`
- 基础仓储: `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`

#### 4. 共享模型层
**项目**: `LYBT.Shared.Models`  
**路径**: `src/Shared/LYBT.Shared.Models/`

| 功能模块 | 类/方法 | 功能类型 | 使用范围 |
|---------|--------|---------|---------|
| DTO契约 | `Contracts/*/Dtos.cs` | Core | 前后端数据传输 |
| 业务枚举 | `Enums/*.cs` | Core | 状态和类型定义 |
| 服务结果 | `ServiceResult<T>` | Core | 统一返回格式 |
| API响应 | `ApiResponse<T>` | Core | Web API标准响应 |

## 🔗 质量标签统计

### 前端质量标签
| 模块 | 分页 | 异步 | 错误处理 | MVVM绑定 | 导航 |
|------|------|------|---------|---------|------|
| Auth | ❌ | ✅ | ✅ | ✅ | ✅ |
| Patients | ✅ | ✅ | ✅ | ✅ | ✅ |
| MedicalCase | ✅ | ✅ | ✅ | ✅ | ✅ |
| Consultation | ❌ | ✅ | ✅ | ✅ | ✅ |
| Prescriptions | ❌ | ✅ | ✅ | ✅ | ✅ |
| Herbs | ✅ | ✅ | ✅ | ✅ | ✅ |
| Formula | ✅ | ✅ | ✅ | ✅ | ✅ |
| Users | ✅ | ✅ | ✅ | ✅ | ✅ |

### 后端质量标签  
| 模块 | 分页 | AsNoTracking | 授权 | 验证 | 审计日志 |
|------|------|-------------|------|------|---------|
| Auth | ❌ | ❌ | AllowAnonymous | ✅ | ✅ |
| Patients | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| MedicalCase | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| Consultation | ❌ | ❌ | ✅ | ✅ | ⚠️ |
| Prescriptions | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| Herbs | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| Formula | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| Users | ✅ | ✅ | Admin Role | ✅ | ✅ |

## 🎯 架构特征总结

### 前端架构特征 (UltraThink双层)
- **主Service层**: 纯委托模式，统一服务入口
- **QueryService层**: 复杂查询专用，搜索统计功能
- **BusinessService层**: 业务逻辑+CRUD操作
- **零Helper类**: 完全移除传统Helper模式

### 后端架构特征 (传统三层)  
- **Controller层**: RESTful API端点，统一ApiResponse格式
- **Service层**: 业务逻辑处理，事务管理
- **Repository层**: 数据访问，LINQ查询优化

### 关键技术决策
1. **混合架构设计**: 前端UltraThink + 后端传统三层
2. **统一数据访问**: 所有模块共享AppDbContext  
3. **类型安全通信**: Refit客户端 + 强类型DTO
4. **现代异步模式**: async/await贯穿整个调用链
5. **零SQL注入风险**: 纯LINQ查询 + 参数化操作

---

**下一步**: 继续生成frontend-map.md和backend-map.md详细映射表