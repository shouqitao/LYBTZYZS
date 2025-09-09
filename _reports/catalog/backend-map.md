# 后端架构映射表 - Controller↔Service↔Repository调用关系

**生成时间**: 2025-01-09  
**映射范围**: LYBTZYZS后端Web API所有业务模块调用关系  
**架构模式**: 传统三层架构 + EF Core + UltraThink服务层

## 🎯 映射表说明

本映射表详细记录了后端各个Controller与Service、Repository的调用关系，以及数据库表结构和API端点的完整映射。

**映射关系层次**:
- **Controller层**: HTTP请求处理，RESTful API端点
- **Service层**: 业务逻辑处理，UltraThink双层架构
- **Repository层**: 数据访问抽象，EF Core集成
- **Entity层**: 领域实体模型，数据库映射
- **Database层**: SQL Server数据表，索引优化

## 📋 Web API控制器映射

### AuthController (身份认证控制器)

| HTTP端点 | 控制器方法 | Service调用 | Repository调用 | Entity模型 | 数据库表 | 描述 |
|---------|------------|-------------|----------------|-----------|----------|------|
| **POST** `/api/v1/auth/login` | `Login(LoginRequestDto)` | `IAuthService.LoginAsync()` | `IAuthRepository.ValidateCredentialsAsync()` | `UserModel` | `Users` | 用户登录验证 |
| | | | `IAuthSessionRepository.CreateSessionAsync()` | `AuthSessionModel` | `AuthSessions` | 创建登录会话 |
| **POST** `/api/v1/auth/logout` | `Logout()` | `IAuthService.LogoutAsync()` | `IAuthSessionRepository.EndSessionAsync()` | `AuthSessionModel` | `AuthSessions` | 用户登出 |
| **GET** `/api/v1/auth/current-user` | `GetCurrentUser()` | `IAuthService.GetCurrentUserAsync()` | `IUserRepository.GetByIdAsync()` | `UserModel` | `Users` | 获取当前用户信息 |
| **POST** `/api/v1/auth/change-password` | `ChangePassword(ChangePasswordDto)` | `IAuthService.ChangePasswordAsync()` | `IUserRepository.UpdatePasswordAsync()` | `UserModel` | `Users` | 修改用户密码 |
| **POST** `/api/v1/auth/admin-password` | `ChangeAdminPassword(ChangeAdminPasswordDto)` | `IAuthService.ChangeAdminPasswordAsync()` | `IAuthRepository.UpdateAdminSecretAsync()` | `AdminSecretModel` | `AdminSecrets` | 修改系统管理员密码 |
| **POST** `/api/v1/auth/refresh` | `RefreshToken()` | `IAuthService.RefreshTokenAsync()` | `IAuthSessionRepository.ValidateSessionAsync()` | `AuthSessionModel` | `AuthSessions` | 刷新JWT令牌 |

**AuthService业务层分解**:
- `AuthBusinessService`: 登录验证、密码管理、安全审计
- `AuthQueryService`: 用户状态查询、会话管理
- `JwtAuthenticationService`: JWT令牌生成、验证、刷新

### UsersController (用户管理控制器)

| HTTP端点 | 控制器方法 | Service调用 | Repository调用 | Entity模型 | 数据库表 | 描述 |
|---------|------------|-------------|----------------|-----------|----------|------|
| **GET** `/api/v1/users` | `GetList(PagedQueryDto)` | `IUserService.GetPagedAsync()` | `IUserRepository.GetPagedAsync()` | `UserModel` | `Users` | 分页查询用户列表 |
| **GET** `/api/v1/users/{id}` | `GetById(Guid id)` | `IUserService.GetByIdAsync()` | `IUserRepository.GetByIdAsync()` | `UserModel` | `Users` | 根据ID获取用户 |
| **POST** `/api/v1/users` | `Add(UserCreateDto)` | `IUserService.CreateAsync()` | `IUserRepository.CreateAsync()` | `UserModel` | `Users` | 创建新用户 |
| **PUT** `/api/v1/users/{id}` | `Update(Guid id, UserUpdateDto)` | `IUserService.UpdateAsync()` | `IUserRepository.UpdateAsync()` | `UserModel` | `Users` | 更新用户信息 |
| **DELETE** `/api/v1/users/{id}` | `Delete(Guid id)` | `IUserService.DeleteAsync()` | `IUserRepository.DeleteAsync()` | `UserModel` | `Users` | 删除用户 |
| **POST** `/api/v1/users/{id}/enable` | `Enable(Guid id)` | `IUserService.EnableAsync()` | `IUserRepository.SetStatusAsync()` | `UserModel` | `Users` | 启用用户 |
| **POST** `/api/v1/users/{id}/disable` | `Disable(Guid id)` | `IUserService.DisableAsync()` | `IUserRepository.SetStatusAsync()` | `UserModel` | `Users` | 禁用用户 |
| **GET** `/api/v1/users/search` | `Search(string keyword)` | `IUserService.SearchAsync()` | `IUserRepository.SearchByKeywordAsync()` | `UserModel` | `Users` | 搜索用户 |
| **GET** `/api/v1/users/doctors` | `GetDoctors()` | `IUserService.GetByRoleAsync("Doctor")` | `IUserRepository.GetByRoleAsync()` | `UserModel` | `Users` | 获取医生用户列表 |

**UserService业务层分解**:
- `UserBusinessService`: 用户CRUD操作、角色管理、状态控制
- `UserQueryService`: 用户搜索、分页查询、权限查询

### PatientsController (患者管理控制器)

| HTTP端点 | 控制器方法 | Service调用 | Repository调用 | Entity模型 | 数据库表 | 描述 |
|---------|------------|-------------|----------------|-----------|----------|------|
| **GET** `/api/v1/patients` | `GetList(PagedQueryDto)` | `IPatientService.GetPagedAsync()` | `IPatientRepository.GetPagedAsync()` | `PatientModel` | `Patients` | 分页查询患者列表 |
| **GET** `/api/v1/patients/{id}` | `GetById(Guid id)` | `IPatientService.GetByIdAsync()` | `IPatientRepository.GetByIdAsync()` | `PatientModel` | `Patients` | 根据ID获取患者 |
| **POST** `/api/v1/patients` | `Add(PatientCreateDto)` | `IPatientService.CreateAsync()` | `IPatientRepository.CreateAsync()` | `PatientModel` | `Patients` | 创建新患者 |
| **PUT** `/api/v1/patients/{id}` | `Update(Guid id, PatientUpdateDto)` | `IPatientService.UpdateAsync()` | `IPatientRepository.UpdateAsync()` | `PatientModel` | `Patients` | 更新患者信息 |
| **DELETE** `/api/v1/patients/{id}` | `Delete(Guid id)` | `IPatientService.DeleteAsync()` | `IPatientRepository.DeleteAsync()` | `PatientModel` | `Patients` | 删除患者 |
| **POST** `/api/v1/patients/{id}/enable` | `Enable(Guid id)` | `IPatientService.EnableAsync()` | `IPatientRepository.EnableAsync()` | `PatientModel` | `Patients` | 启用患者 |
| **POST** `/api/v1/patients/{id}/disable` | `Disable(Guid id)` | `IPatientService.DisableAsync()` | `IPatientRepository.DisableAsync()` | `PatientModel` | `Patients` | 禁用患者 |
| **GET** `/api/v1/patients/search` | `Search(string keyword)` | `IPatientService.SearchAsync()` | `IPatientRepository.SmartSearchAsync()` | `PatientModel` | `Patients` | 智能搜索患者 |
| **GET** `/api/v1/patients/by-idcard/{idCard}` | `GetByIdCard(string idCard)` | `IPatientService.GetByIdCardAsync()` | `IPatientRepository.GetByIdNumberAsync()` | `PatientModel` | `Patients` | 根据身份证查询 |
| **GET** `/api/v1/patients/by-phone/{phone}` | `GetByPhone(string phone)` | `IPatientService.GetByPhoneAsync()` | `IPatientRepository.GetByPhoneAsync()` | `PatientModel` | `Patients` | 根据电话查询 |
| **POST** `/api/v1/patients/import` | `ImportPatients(IFormFile file)` | `IPatientService.ImportPatientsAsync()` | `IPatientRepository.BatchImportAsync()` | `PatientModel` | `Patients` | Excel批量导入 |
| **GET** `/api/v1/patients/export` | `ExportPatients()` | `IPatientService.ExportPatientsAsync()` | `IPatientRepository.GetAllForExportAsync()` | `PatientModel` | `Patients` | Excel数据导出 |
| **GET** `/api/v1/patients/template` | `ExportImportTemplate()` | `IPatientService.GetImportTemplateAsync()` | - | - | - | 下载导入模板 |
| **POST** `/api/v1/patients/validate-import` | `ValidateImportData(IFormFile file)` | `IPatientService.ValidateImportDataAsync()` | - | - | - | 验证导入数据 |
| **GET** `/api/v1/patients/active` | `GetActivePatients()` | `IPatientService.GetActivePatientsAsync()` | `IPatientRepository.GetActivePatientsAsync()` | `PatientModel` | `Patients` | 获取活跃患者 |

**PatientService业务层分解**:
- `PatientBusinessService`: 患者CRUD操作、状态管理、数据验证、导入导出逻辑
- `PatientQueryService`: 智能搜索、分页查询、统计分析、条件筛选

**OptimizedPatientRepository优化特性**:
- 编译查询: `_compiledGetByPhone`, `_compiledSearchByName`
- 批量操作: `BatchImportAsync`, `BatchEnableAsync`, `BatchDisableAsync`
- 智能搜索: `SmartSearchAsync`, `BuildSearchQuery`, `ApplySmartOrdering`

### MedicalCaseController (医疗案例控制器)

| HTTP端点 | 控制器方法 | Service调用 | Repository调用 | Entity模型 | 数据库表 | 描述 |
|---------|------------|-------------|----------------|-----------|----------|------|
| **GET** `/api/v1/medicalcases` | `GetList(PagedQueryDto)` | `IMedicalCaseService.GetPagedAsync()` | `IMedicalCaseRepository.GetPagedAsync()` | `MedicalCaseModel` | `MedicalCases` | 分页查询医疗案例 |
| **GET** `/api/v1/medicalcases/{id}` | `GetById(Guid id)` | `IMedicalCaseService.GetByIdAsync()` | `IMedicalCaseRepository.GetByIdAsync()` | `MedicalCaseModel` | `MedicalCases` | 根据ID获取案例 |
| **POST** `/api/v1/medicalcases` | `Create(MedicalCaseCreateDto)` | `IMedicalCaseService.CreateAsync()` | `IMedicalCaseRepository.CreateAsync()` | `MedicalCaseModel` | `MedicalCases` | 创建新医疗案例 |
| **PUT** `/api/v1/medicalcases/{id}` | `Update(Guid id, MedicalCaseUpdateDto)` | `IMedicalCaseService.UpdateAsync()` | `IMedicalCaseRepository.UpdateAsync()` | `MedicalCaseModel` | `MedicalCases` | 更新案例信息 |
| **DELETE** `/api/v1/medicalcases/{id}` | `Delete(Guid id)` | `IMedicalCaseService.DeleteAsync()` | `IMedicalCaseRepository.DeleteAsync()` | `MedicalCaseModel` | `MedicalCases` | 删除案例 |
| **POST** `/api/v1/medicalcases/{id}/complete` | `Complete(Guid id)` | `IMedicalCaseService.SetStatusAsync()` | `IMedicalCaseRepository.UpdateStatusAsync()` | `MedicalCaseModel` | `MedicalCases` | 完成案例 |
| **POST** `/api/v1/medicalcases/{id}/cancel` | `Cancel(Guid id)` | `IMedicalCaseService.SetStatusAsync()` | `IMedicalCaseRepository.UpdateStatusAsync()` | `MedicalCaseModel` | `MedicalCases` | 取消案例 |
| **GET** `/api/v1/medicalcases/patient/{patientId}` | `GetByPatientId(Guid patientId)` | `IMedicalCaseService.GetByPatientIdAsync()` | `IMedicalCaseRepository.GetByPatientIdAsync()` | `MedicalCaseModel` | `MedicalCases` | 获取患者案例列表 |
| **GET** `/api/v1/medicalcases/statistics` | `GetStatistics()` | `IMedicalCaseService.GetStatisticsAsync()` | `IMedicalCaseRepository.GetStatisticsAsync()` | `MedicalCaseModel` | `MedicalCases` | 获取案例统计 |

**MedicalCaseService业务层分解**:
- `MedicalCaseBusinessService`: 案例状态流转、业务规则验证、创建更新逻辑
- `MedicalCaseQueryService`: 案例搜索、统计查询、患者关联查询

**案例状态流转**:
```
Registered → InConsultation → Completed
    ↓              ↓              
  Cancelled    Cancelled       
```

### ConsultationController (诊疗记录控制器)

| HTTP端点 | 控制器方法 | Service调用 | Repository调用 | Entity模型 | 数据库表 | 描述 |
|---------|------------|-------------|----------------|-----------|----------|------|
| **GET** `/api/v1/consultations` | `GetList(PagedQueryDto)` | `IConsultationService.GetPagedAsync()` | `IConsultationRepository.GetPagedAsync()` | `ConsultationModel` | `Consultations` | 分页查询诊疗记录 |
| **GET** `/api/v1/consultations/{id}` | `GetById(Guid id)` | `IConsultationService.GetByIdAsync()` | `IConsultationRepository.GetByIdAsync()` | `ConsultationModel` | `Consultations` | 根据ID获取诊疗记录 |
| **POST** `/api/v1/consultations` | `Create(ConsultationCreateDto)` | `IConsultationService.CreateAsync()` | `IConsultationRepository.CreateAsync()` | `ConsultationModel` | `Consultations` | 创建诊疗记录 |
| **PUT** `/api/v1/consultations/{id}` | `Update(Guid id, ConsultationUpdateDto)` | `IConsultationService.UpdateAsync()` | `IConsultationRepository.UpdateAsync()` | `ConsultationModel` | `Consultations` | 更新诊疗记录 |
| **GET** `/api/v1/consultations/medicalcase/{caseId}` | `GetByMedicalCaseId(Guid caseId)` | `IConsultationService.GetByMedicalCaseIdAsync()` | `IConsultationRepository.GetByMedicalCaseIdAsync()` | `ConsultationModel` | `Consultations` | 获取案例的诊疗记录 |
| **GET** `/api/v1/consultations/patient/{patientId}/history` | `GetPatientHistory(Guid patientId)` | `IConsultationService.GetPatientHistoryAsync()` | `IConsultationRepository.GetByPatientIdAsync()` | `ConsultationModel` | `Consultations` | 获取患者诊疗历史 |

**ConsultationService业务层分解**:
- `ConsultationBusinessService`: 诊疗记录保存、中医四诊数据处理、诊断逻辑
- `ConsultationQueryService`: 诊疗历史查询、患者诊疗记录、统计分析

**中医四诊数据结构** (ConsultationModel字段):
- **望诊**: `Complexion`, `TongueColor`, `TongueCoating`, `MentalState`
- **闻诊**: `VoiceQuality`, `BreathingPattern`, `BodyOdor`  
- **问诊**: `ChiefComplaint`, `PresentIllness`, `PastHistory`, `SystemReview`
- **切诊**: `PulseRate`, `PulseStrength`, `PulseRhythm`, `Palpation`
- **辨证论治**: `TCMDiagnosis`, `WesternDiagnosis`, `TreatmentPlan`

### PrescriptionsController (处方管理控制器)

| HTTP端点 | 控制器方法 | Service调用 | Repository调用 | Entity模型 | 数据库表 | 描述 |
|---------|------------|-------------|----------------|-----------|----------|------|
| **GET** `/api/v1/prescriptions` | `GetList(PagedQueryDto)` | `IPrescriptionsService.GetPagedAsync()` | `IPrescriptionRepository.GetPagedAsync()` | `PrescriptionModel` | `Prescriptions` | 分页查询处方列表 |
| **GET** `/api/v1/prescriptions/{id}` | `GetById(Guid id)` | `IPrescriptionsService.GetByIdAsync()` | `IPrescriptionRepository.GetByIdWithItemsAsync()` | `PrescriptionModel` + `PrescriptionItemModel` | `Prescriptions` + `PrescriptionItems` | 根据ID获取处方详情 |
| **POST** `/api/v1/prescriptions` | `Create(PrescriptionCreateDto)` | `IPrescriptionsService.CreateAsync()` | `IPrescriptionRepository.CreateWithItemsAsync()` | `PrescriptionModel` + `PrescriptionItemModel` | `Prescriptions` + `PrescriptionItems` | 创建新处方 |
| **PUT** `/api/v1/prescriptions/{id}` | `Update(Guid id, PrescriptionUpdateDto)` | `IPrescriptionsService.UpdateAsync()` | `IPrescriptionRepository.UpdateWithItemsAsync()` | `PrescriptionModel` + `PrescriptionItemModel` | `Prescriptions` + `PrescriptionItems` | 更新处方信息 |
| **DELETE** `/api/v1/prescriptions/{id}` | `Delete(Guid id)` | `IPrescriptionsService.DeleteAsync()` | `IPrescriptionRepository.DeleteWithItemsAsync()` | `PrescriptionModel` + `PrescriptionItemModel` | `Prescriptions` + `PrescriptionItems` | 删除处方 |
| **GET** `/api/v1/prescriptions/patient/{patientId}` | `GetByPatientId(Guid patientId)` | `IPrescriptionsService.GetByPatientIdAsync()` | `IPrescriptionRepository.GetByPatientIdAsync()` | `PrescriptionModel` | `Prescriptions` | 获取患者处方历史 |
| **GET** `/api/v1/prescriptions/search` | `Search(string keyword)` | `IPrescriptionsService.SearchAsync()` | `IPrescriptionRepository.SearchAsync()` | `PrescriptionModel` | `Prescriptions` | 搜索处方 |
| **POST** `/api/v1/prescriptions/compatibility` | `CheckCompatibility(CompatibilityCheckDto)` | `IIntelligentPrescriptionService.CheckCompatibilityAsync()` | `IHerbRepository.GetHerbsWithInteractionsAsync()` | `HerbModel` | `Herbs` | 配伍禁忌检查 |

**PrescriptionsService业务层分解**:
- `PrescriptionBusinessService`: 处方开具、药材配置、价格计算、处方验证
- `PrescriptionQueryService`: 处方搜索、患者用药历史、统计分析
- `IntelligentPrescriptionService`: 配伍禁忌检查、智能推荐、用药安全

**处方数据结构关系**:
```
PrescriptionModel (处方主表)
├── Id: Guid (主键)
├── PatientId: Guid (患者外键)
├── ConsultationId: Guid (诊疗外键)
├── DoctorId: Guid (医生外键)
└── PrescriptionItems: List<PrescriptionItemModel> (处方明细)
    ├── HerbId: Guid (药材外键)
    ├── Dosage: decimal (剂量)
    ├── Unit: string (单位)
    └── Usage: string (用法)
```

### HerbsController (药材管理控制器)

| HTTP端点 | 控制器方法 | Service调用 | Repository调用 | Entity模型 | 数据库表 | 描述 |
|---------|------------|-------------|----------------|-----------|----------|------|
| **GET** `/api/v1/herbs` | `GetList(PagedQueryDto)` | `IHerbService.GetPagedAsync()` | `IHerbRepository.GetPagedAsync()` | `HerbModel` | `Herbs` | 分页查询药材列表 |
| **GET** `/api/v1/herbs/{id}` | `GetById(Guid id)` | `IHerbService.GetByIdAsync()` | `IHerbRepository.GetByIdAsync()` | `HerbModel` | `Herbs` | 根据ID获取药材 |
| **POST** `/api/v1/herbs` | `Create(HerbCreateDto)` | `IHerbService.CreateAsync()` | `IHerbRepository.CreateAsync()` | `HerbModel` | `Herbs` | 创建新药材 |
| **PUT** `/api/v1/herbs/{id}` | `Update(Guid id, HerbUpdateDto)` | `IHerbService.UpdateAsync()` | `IHerbRepository.UpdateAsync()` | `HerbModel` | `Herbs` | 更新药材信息 |
| **DELETE** `/api/v1/herbs/{id}` | `Delete(Guid id)` | `IHerbService.DeleteAsync()` | `IHerbRepository.DeleteAsync()` | `HerbModel` | `Herbs` | 删除药材 |
| **POST** `/api/v1/herbs/{id}/enable` | `Enable(Guid id)` | `IHerbService.EnableAsync()` | `IHerbRepository.SetStatusAsync()` | `HerbModel` | `Herbs` | 启用药材 |
| **POST** `/api/v1/herbs/{id}/disable` | `Disable(Guid id)` | `IHerbService.DisableAsync()` | `IHerbRepository.SetStatusAsync()` | `HerbModel` | `Herbs` | 禁用药材 |
| **GET** `/api/v1/herbs/search` | `Search(string keyword)` | `IHerbService.SearchAsync()` | `IHerbRepository.SearchByNameAsync()` | `HerbModel` | `Herbs` | 搜索药材 |
| **GET** `/api/v1/herbs/categories` | `GetCategories()` | `IHerbService.GetCategoriesAsync()` | `IHerbRepository.GetDistinctCategoriesAsync()` | `HerbModel` | `Herbs` | 获取药材分类 |
| **GET** `/api/v1/herbs/active` | `GetActiveHerbs()` | `IHerbService.GetActiveHerbsAsync()` | `IHerbRepository.GetActiveHerbsAsync()` | `HerbModel` | `Herbs` | 获取活跃药材 |

**HerbsService业务层分解**:
- `HerbBusinessService`: 药材CRUD操作、价格管理、分类管理、状态控制
- `HerbQueryService`: 药材搜索、分类筛选、价格查询、库存状态

**药材数据结构** (HerbModel字段):
- **基础信息**: `Name`, `Alias`, `Category`, `Origin`, `Specification`
- **中药属性**: `Nature`, `Flavor`, `Meridian`, `Function`, `Indication`
- **价格信息**: `PurchasePrice`, `RetailPrice`, `Unit`
- **使用指导**: `Dosage`, `Contraindication`, `Attention`

### HerbImportExportController (药材导入导出控制器)

| HTTP端点 | 控制器方法 | Service调用 | Repository调用 | Entity模型 | 数据库表 | 描述 |
|---------|------------|-------------|----------------|-----------|----------|------|
| **POST** `/api/v1/herbs/import` | `ImportHerbs(IFormFile file)` | `IHerbService.ImportHerbsAsync()` | `IHerbRepository.BatchImportAsync()` | `HerbModel` | `Herbs` | Excel批量导入药材 |
| **GET** `/api/v1/herbs/export` | `ExportHerbs()` | `IHerbService.ExportHerbsAsync()` | `IHerbRepository.GetAllForExportAsync()` | `HerbModel` | `Herbs` | Excel导出药材数据 |
| **GET** `/api/v1/herbs/template` | `DownloadTemplate()` | `IHerbService.GetImportTemplateAsync()` | - | - | - | 下载导入模板 |
| **POST** `/api/v1/herbs/validate-import` | `ValidateImportData(IFormFile file)` | `IHerbService.ValidateImportDataAsync()` | - | - | - | 验证导入数据 |

### FormulasController (验方管理控制器)

| HTTP端点 | 控制器方法 | Service调用 | Repository调用 | Entity模型 | 数据库表 | 描述 |
|---------|------------|-------------|----------------|-----------|----------|------|
| **GET** `/api/v1/formulas` | `GetList(PagedQueryDto)` | `IFormulaService.GetPagedAsync()` | `IFormulaRepository.GetPagedAsync()` | `FormulaModel` | `Formulas` | 分页查询验方列表 |
| **GET** `/api/v1/formulas/{id}` | `GetById(Guid id)` | `IFormulaService.GetByIdAsync()` | `IFormulaRepository.GetByIdWithHerbsAsync()` | `FormulaModel` + `FormulaHerbItem` | `Formulas` + `FormulaHerbs` | 根据ID获取验方详情 |
| **POST** `/api/v1/formulas` | `Create(FormulaCreateDto)` | `IFormulaService.CreateAsync()` | `IFormulaRepository.CreateWithHerbsAsync()` | `FormulaModel` + `FormulaHerbItem` | `Formulas` + `FormulaHerbs` | 创建新验方 |
| **PUT** `/api/v1/formulas/{id}` | `Update(Guid id, FormulaUpdateDto)` | `IFormulaService.UpdateAsync()` | `IFormulaRepository.UpdateWithHerbsAsync()` | `FormulaModel` + `FormulaHerbItem` | `Formulas` + `FormulaHerbs` | 更新验方信息 |
| **DELETE** `/api/v1/formulas/{id}` | `Delete(Guid id)` | `IFormulaService.DeleteAsync()` | `IFormulaRepository.DeleteWithHerbsAsync()` | `FormulaModel` + `FormulaHerbItem` | `Formulas` + `FormulaHerbs` | 删除验方 |
| **GET** `/api/v1/formulas/search` | `Search(string keyword)` | `IFormulaService.SearchAsync()` | `IFormulaRepository.SearchAsync()` | `FormulaModel` | `Formulas` | 搜索验方 |
| **GET** `/api/v1/formulas/categories` | `GetCategories()` | `IFormulaService.GetCategoriesAsync()` | `IFormulaRepository.GetDistinctCategoriesAsync()` | `FormulaModel` | `Formulas` | 获取验方分类 |
| **GET** `/api/v1/formulas/classic` | `GetClassicFormulas()` | `IFormulaService.GetClassicFormulasAsync()` | `IFormulaRepository.GetBySourceAsync("Classic")` | `FormulaModel` | `Formulas` | 获取经典验方 |

**FormulaService业务层分解**:
- `FormulaBusinessService`: 验方CRUD操作、药材组合管理、分类管理
- `FormulaQueryService`: 验方搜索、分类筛选、来源查询、应用统计

**验方数据结构关系**:
```
FormulaModel (验方主表)
├── Id: Guid (主键)
├── Name: string (验方名称)
├── Source: string (来源：经典/个人)
├── Category: string (分类)
├── Function: string (功效)
├── Indication: string (主治)
└── FormulaHerbs: List<FormulaHerbItem> (验方药材组成)
    ├── HerbId: Guid (药材外键)
    ├── Dosage: decimal (剂量)
    ├── Unit: string (单位)
    └── Function: string (在方中的作用)
```

## 🗄️ 数据库表结构映射

### 核心业务表

| 数据库表 | Entity模型 | 主要字段 | 索引优化 | 外键关系 | 描述 |
|----------|-----------|----------|----------|----------|------|
| **Users** | `UserModel` | `Id`, `UserName`, `RealName`, `Role`, `PasswordHash`, `IsActive` | `IX_Users_UserName`, `IX_Users_Role` | - | 用户账户表 |
| **AdminSecrets** | `AdminSecretModel` | `Id`, `AdminType`, `PasswordHash`, `LastChanged` | `IX_AdminSecrets_AdminType` | - | 系统管理员密钥表 |
| **AuthSessions** | `AuthSessionModel` | `Id`, `UserId`, `TokenId`, `LoginTime`, `ExpireTime`, `IsActive` | `IX_AuthSessions_UserId`, `IX_AuthSessions_TokenId` | `FK_UserId → Users.Id` | 认证会话表 |
| **Patients** | `PatientModel` | `Id`, `Name`, `Gender`, `BirthDate`, `IdNumber`, `PhoneNumber`, `Address`, `IsActive` | `IX_Patients_Name`, `IX_Patients_Phone`, `IX_Patients_IdNumber` | - | 患者档案表 |
| **MedicalCases** | `MedicalCaseModel` | `Id`, `PatientId`, `DoctorId`, `CaseStatus`, `ChiefComplaint`, `CreateTime` | `IX_MedicalCases_PatientId`, `IX_MedicalCases_Status` | `FK_PatientId → Patients.Id`, `FK_DoctorId → Users.Id` | 医疗案例表 |
| **Consultations** | `ConsultationModel` | `Id`, `MedicalCaseId`, `PatientId`, `四诊字段`, `TCMDiagnosis`, `TreatmentPlan` | `IX_Consultations_MedicalCaseId`, `IX_Consultations_PatientId` | `FK_MedicalCaseId → MedicalCases.Id`, `FK_PatientId → Patients.Id` | 诊疗记录表 |
| **Prescriptions** | `PrescriptionModel` | `Id`, `PatientId`, `ConsultationId`, `DoctorId`, `TotalCost`, `Status` | `IX_Prescriptions_PatientId`, `IX_Prescriptions_ConsultationId` | `FK_PatientId → Patients.Id`, `FK_ConsultationId → Consultations.Id` | 处方主表 |
| **PrescriptionItems** | `PrescriptionItemModel` | `Id`, `PrescriptionId`, `HerbId`, `Dosage`, `Unit`, `Usage` | `IX_PrescriptionItems_PrescriptionId`, `IX_PrescriptionItems_HerbId` | `FK_PrescriptionId → Prescriptions.Id`, `FK_HerbId → Herbs.Id` | 处方明细表 |
| **Herbs** | `HerbModel` | `Id`, `Name`, `Category`, `Nature`, `Flavor`, `Function`, `PurchasePrice`, `RetailPrice` | `IX_Herbs_Name`, `IX_Herbs_Category` | - | 药材基础表 |
| **Formulas** | `FormulaModel` | `Id`, `Name`, `Source`, `Category`, `Function`, `Indication`, `Usage` | `IX_Formulas_Name`, `IX_Formulas_Category` | - | 验方主表 |

### 关系映射图

```
Users (用户)
├── 1:N → MedicalCases (医疗案例) [DoctorId]
├── 1:N → AuthSessions (认证会话) [UserId]
└── 1:N → Prescriptions (处方) [DoctorId]

Patients (患者)
├── 1:N → MedicalCases (医疗案例) [PatientId]
├── 1:N → Consultations (诊疗记录) [PatientId]
└── 1:N → Prescriptions (处方) [PatientId]

MedicalCases (医疗案例)
├── 1:1 → Consultations (诊疗记录) [MedicalCaseId]
└── 1:N → Prescriptions (处方) [ConsultationId]

Prescriptions (处方)
└── 1:N → PrescriptionItems (处方明细) [PrescriptionId]

Herbs (药材)
├── 1:N → PrescriptionItems (处方明细) [HerbId]
└── 1:N → FormulaHerbItems (验方药材) [HerbId]

Formulas (验方)
└── 1:N → FormulaHerbItems (验方药材) [FormulaId]
```

## ⚡ Repository层优化特性

### 编译查询优化 (Compiled Queries)

```csharp
public class OptimizedPatientRepository : BaseRepository<PatientModel>
{
    // 编译查询 - 提升查询性能
    private static readonly Func<AppDbContext, string, PatientModel?> _compiledGetByPhone =
        EF.CompileQuery((AppDbContext context, string phone) =>
            context.Patients.FirstOrDefault(p => p.PhoneNumber == phone && p.IsDeleted == false));

    private static readonly Func<AppDbContext, string, IEnumerable<PatientModel>> _compiledSearchByName =
        EF.CompileQuery((AppDbContext context, string keyword) =>
            context.Patients.Where(p => EF.Functions.Like(p.Name, $"%{keyword}%") && p.IsDeleted == false));
}
```

### 批量操作优化 (Batch Operations)

```csharp
// 批量启用 - 使用ExecuteUpdateAsync避免内存加载
public async Task<int> BatchEnableAsync(List<Guid> ids)
{
    return await Context.Patients
        .Where(p => ids.Contains(p.Id))
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(p => p.IsActive, true)
            .SetProperty(p => p.UpdateTime, DateTime.Now));
}

// 批量导入 - 使用AddRange减少数据库往返
public async Task<BatchImportResult> BatchImportAsync(List<PatientModel> patients)
{
    using var transaction = await Context.Database.BeginTransactionAsync();
    Context.Patients.AddRange(patients);
    var affectedRows = await Context.SaveChangesAsync();
    await transaction.CommitAsync();
    return new BatchImportResult { Success = true, ImportedCount = affectedRows };
}
```

### 智能搜索优化 (Smart Search)

```csharp
public async Task<PagedResult<PatientModel>> SmartSearchAsync(
    PatientSearchCriteria criteria, int pageIndex, int pageSize)
{
    var query = Context.Patients.AsQueryable();
    
    // 动态查询构建
    query = BuildSearchQuery(query, criteria);
    
    // 智能排序
    query = ApplySmartOrdering(query, criteria);
    
    // 高效分页
    var total = await query.CountAsync();
    var items = await query
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
        
    return new PagedResult<PatientModel>(items, total, pageIndex, pageSize);
}
```

## 🔄 Service层UltraThink架构

### 双层服务架构

每个业务模块都遵循UltraThink双层架构模式：

```csharp
// 主Service层 - 纯委托模式
public class PatientService : IPatientService
{
    private readonly PatientBusinessService _businessService;
    private readonly PatientQueryService _queryService;

    // 查询操作委托给QueryService
    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedQueryBaseDto query)
        => await _queryService.GetPagedAsync(query);

    // 业务操作委托给BusinessService
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        => await _businessService.CreateAsync(dto);
}
```

### BusinessService业务逻辑层

```csharp
public class PatientBusinessService : IPatientBusinessService
{
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
    {
        try
        {
            // 1. 输入验证
            var validation = ValidatePatientInput(dto);
            if (!validation.IsSuccess) return validation;

            // 2. 重复检查
            var duplicate = await CheckDuplicatePatientAsync(dto);
            if (duplicate.Exists) return ServiceResult.Error<PatientDto>("患者已存在");

            // 3. 创建实体
            var entity = _mapper.Map<PatientModel>(dto);
            entity.Id = Guid.NewGuid();
            entity.CreateTime = DateTime.Now;

            // 4. 保存数据
            var created = await _repository.CreateAsync(entity);
            
            // 5. 返回结果
            return ServiceResult.Success(_mapper.Map<PatientDto>(created), "患者创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败");
            return ServiceResult.Error<PatientDto>("创建患者失败，请重试");
        }
    }
}
```

### QueryService查询优化层

```csharp
public class PatientQueryService : IPatientQueryService
{
    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedQueryBaseDto query)
    {
        try
        {
            var result = await _repository.GetPagedAsync(
                query.PageIndex, query.PageSize, query.SortField, query.IsDescending);
                
            var dtos = _mapper.Map<List<PatientDto>>(result.Items);
            
            return ServiceResult.Success(
                new PagedResult<PatientDto>(dtos, result.TotalCount, result.PageIndex, result.PageSize),
                "查询成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询患者列表失败");
            return ServiceResult.Error<PagedResult<PatientDto>>("查询失败，请重试");
        }
    }
}
```

## 📊 API调用统计总览

### 控制器和端点统计

| 控制器 | HTTP端点数量 | Service方法调用 | Repository方法调用 | Entity模型 | 数据库表 |
|--------|-------------|-----------------|-------------------|-----------|----------|
| **AuthController** | 6 | 8 | 12 | 3 | 3 |
| **UsersController** | 9 | 12 | 15 | 1 | 1 |
| **PatientsController** | 15 | 18 | 25 | 1 | 1 |
| **MedicalCaseController** | 9 | 12 | 15 | 1 | 1 |
| **ConsultationController** | 6 | 8 | 10 | 1 | 1 |
| **PrescriptionsController** | 8 | 12 | 18 | 2 | 2 |
| **HerbsController** | 10 | 14 | 18 | 1 | 1 |
| **HerbImportExportController** | 4 | 5 | 6 | 1 | 1 |
| **FormulasController** | 8 | 11 | 15 | 2 | 1 |
| **合计** | **75** | **100** | **134** | **13** | **12** |

### 数据库操作模式统计

| 操作类型 | Repository方法数量 | 优化技术 | 示例方法 |
|----------|------------------|----------|----------|
| **基础CRUD** | 40 | EF Core LINQ | `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetByIdAsync` |
| **分页查询** | 12 | 智能排序 + 分页算法 | `GetPagedAsync`, `GetPagedWithFilterAsync` |
| **搜索查询** | 18 | 编译查询 + 模糊匹配 | `SearchAsync`, `SmartSearchAsync`, `SearchByNameAsync` |
| **批量操作** | 15 | ExecuteUpdateAsync | `BatchEnableAsync`, `BatchImportAsync`, `BatchDeleteAsync` |
| **统计查询** | 8 | 聚合函数优化 | `GetStatisticsAsync`, `GetCountAsync` |
| **关联查询** | 25 | Include预加载 | `GetByIdWithItemsAsync`, `GetWithRelatedDataAsync` |
| **状态管理** | 16 | 状态机模式 | `SetStatusAsync`, `UpdateStatusAsync` |

### 安全特性统计

| 安全特性 | 实现位置 | 技术方案 | 覆盖范围 |
|----------|----------|----------|----------|
| **JWT认证** | AuthController + Middleware | JWT Bearer Token | 所有API端点 |
| **角色授权** | Controller Attributes | [Authorize(Roles)] | 管理员功能 |
| **参数验证** | Controller + DTO | Model Validation | 所有输入参数 |
| **SQL注入防护** | Repository层 | EF Core LINQ | 所有数据查询 |
| **输入清理** | BusinessService层 | 数据验证规则 | 所有业务操作 |
| **异常处理** | 全局中间件 | GlobalExceptionHandler | 所有API调用 |
| **审计日志** | AuthSessions表 | 会话跟踪 | 登录/登出操作 |

---

**总结**: 后端架构映射表展示了LYBTZYZS系统后端Web API的完整调用关系和数据流转。通过传统三层架构实现了清晰的职责分离，UltraThink双层服务架构提供了高效的业务处理，EF Core优化确保了数据访问性能，为中医诊所管理系统提供了稳定可靠的后端服务支撑。