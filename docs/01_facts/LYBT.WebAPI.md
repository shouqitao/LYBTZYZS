# LYBT.WebAPI - 项目事实表

## 1) 基本信息
- **项目名称**: LYBT.WebAPI
- **相对路径**: src/Server/Services/LYBT.WebAPI  
- **项目类型**: WebAPI
- **目标框架**: net8.0
- **输出类型**: Exe
- **可空引用**: enable
- **语言版本**: (unknown)

## 2) 依赖与引用

### 项目引用 (11个)
- ../../Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj
- ../../Core/LYBT.Entities/LYBT.Entities.csproj  
- ../../Modules/LYBT.Module.Auth/LYBT.Module.Auth.csproj
- ../../Modules/LYBT.Module.Users/LYBT.Module.Users.csproj
- ../../Modules/LYBT.Module.Patients/LYBT.Module.Patients.csproj
- ../../Modules/LYBT.Module.MedicalCase/LYBT.Module.MedicalCase.csproj
- ../../Modules/LYBT.Module.Consultation/LYBT.Module.Consultation.csproj
- ../../Modules/LYBT.Module.Prescriptions/LYBT.Module.Prescriptions.csproj
- ../../Modules/LYBT.Module.Herbs/LYBT.Module.Herbs.csproj
- ../../Modules/LYBT.Module.Formula/LYBT.Module.Formula.csproj
- ../../../Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj

### NuGet包引用 (4个)
- Microsoft.AspNetCore.OpenApi
- Swashbuckle.AspNetCore
- Serilog.AspNetCore  
- Microsoft.EntityFrameworkCore.Design

## 3) 公共暴露面

### WebAPI控制器 (9个)

#### AuthController
- **路由**: api/v1/auth
- **HTTP方法**: POST
- **动作签名**:
  - Login(LoginRequest) -> ApiResponse<LoginResponse>
  - Logout(LogoutRequest) -> ApiResponse<object>
  - RefreshToken(string) -> ApiResponse<LoginResponse>
  - ChangePassword(ChangePasswordRequest) -> ApiResponse<object>

#### UsersController  
- **路由**: api/v1/users
- **HTTP方法**: GET, POST, PUT, DELETE
- **动作签名**:
  - GetAllUsers() -> ApiResponse<List<UserDto>>
  - GetUserById(Guid) -> ApiResponse<UserDto>
  - CreateUser(UserCreateDto) -> ApiResponse<UserDto>
  - UpdateUser(Guid, UserUpdateDto) -> ApiResponse<UserDto>
  - DeleteUser(Guid) -> ApiResponse<object>
  - SearchUsers(UserSearchDto) -> ApiResponse<PagedResult<UserDto>>

#### PatientsController
- **路由**: api/v1/patients  
- **HTTP方法**: GET, POST, PUT, DELETE
- **动作签名**:
  - GetAllPatients() -> ApiResponse<List<PatientDto>>
  - GetPatientById(Guid) -> ApiResponse<PatientDto>
  - CreatePatient(PatientCreateDto) -> ApiResponse<PatientDto>
  - UpdatePatient(Guid, PatientUpdateDto) -> ApiResponse<PatientDto>
  - DeletePatient(Guid) -> ApiResponse<object>
  - SearchPatients(PatientSearchDto) -> ApiResponse<PagedResult<PatientDto>>

#### MedicalCaseController
- **路由**: api/v1/medicalcases
- **HTTP方法**: GET, POST, PUT
- **动作签名**:
  - GetAllMedicalCases() -> ApiResponse<List<MedicalCaseDto>>
  - GetMedicalCaseById(Guid) -> ApiResponse<MedicalCaseDto>
  - CreateMedicalCase(MedicalCaseCreateDto) -> ApiResponse<MedicalCaseDto>
  - UpdateMedicalCase(Guid, MedicalCaseUpdateDto) -> ApiResponse<MedicalCaseDto>
  - CompleteMedicalCase(Guid) -> ApiResponse<object>

#### ConsultationController
- **路由**: api/v1/consultations
- **HTTP方法**: GET, POST, PUT  
- **动作签名**:
  - GetConsultationById(Guid) -> ApiResponse<ConsultationDto>
  - CreateConsultation(ConsultationCreateDto) -> ApiResponse<ConsultationDto>
  - UpdateConsultation(Guid, ConsultationUpdateDto) -> ApiResponse<ConsultationDto>

#### PrescriptionsController
- **路由**: api/v1/prescriptions
- **HTTP方法**: GET, POST, PUT
- **动作签名**:
  - GetAllPrescriptions() -> ApiResponse<List<PrescriptionDto>>
  - GetPrescriptionById(Guid) -> ApiResponse<PrescriptionDto>
  - CreatePrescription(PrescriptionCreateDto) -> ApiResponse<PrescriptionDto>
  - UpdatePrescription(Guid, PrescriptionUpdateDto) -> ApiResponse<PrescriptionDto>
  - CalculatePrescription(Guid) -> ApiResponse<PrescriptionCalculationDto>

#### HerbsController
- **路由**: api/v1/herbs
- **HTTP方法**: GET, POST, PUT, DELETE
- **动作签名**:
  - GetAllHerbs() -> ApiResponse<List<HerbDto>>
  - GetHerbById(Guid) -> ApiResponse<HerbDto>
  - CreateHerb(HerbCreateDto) -> ApiResponse<HerbDto>
  - UpdateHerb(Guid, HerbUpdateDto) -> ApiResponse<HerbDto>
  - DeleteHerb(Guid) -> ApiResponse<object>
  - SearchHerbs(HerbSearchDto) -> ApiResponse<PagedResult<HerbDto>>

#### FormulasController
- **路由**: api/v1/formulas  
- **HTTP方法**: GET, POST, PUT, DELETE
- **动作签名**:
  - GetAllFormulas() -> ApiResponse<List<FormulaDto>>
  - GetFormulaById(Guid) -> ApiResponse<FormulaDto>
  - CreateFormula(FormulaCreateDto) -> ApiResponse<FormulaDto>
  - UpdateFormula(Guid, FormulaUpdateDto) -> ApiResponse<FormulaDto>
  - DeleteFormula(Guid) -> ApiResponse<object>

#### HerbImportExportController
- **路由**: api/v1/herbs/import-export
- **HTTP方法**: GET, POST
- **动作签名**:
  - ImportHerbs(IFormFile) -> ApiResponse<object>
  - ExportHerbs() -> FileResult

## 4) 数据模型
- **DbContext**: 无 (使用Infrastructure中的AppDbContext)
- **DbSet列表**: 无
- **主要实体**: 无 (引用Entities项目)
- **DTO类型**: 无 (使用Shared.Models中的DTO)
- **实体↔DTO匹配**: 无

## 5) 测试特征
- **测试框架**: 不适用 (非测试项目)
- **测试夹具**: 不适用
- **启动方式**: 不适用
- **集成测试**: 不适用

## 6) 特殊标识
- **IsIntegrationTest**: false
- **IsCore**: false
- **备注**: ASP.NET Core Web API服务入口，包含9个控制器，50+个API端点，完整支持8个业务模块