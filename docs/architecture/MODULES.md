# 模块规格说明

## 目录

1. [概述](#概述)
2. [模块架构](#模块架构)
3. [核心模块列表](#核心模块列表)
4. [模块详细说明](#模块详细说明)
5. [模块间通信](#模块间通信)
6. [模块开发规范](#模块开发规范)
7. [前后端模块对应](#前后端模块对应)

## 概述

凌隐宝堂中医诊所诊疗系统采用模块化架构设计，将复杂的业务功能拆分为15个独立的业务模块。每个模块负责特定的业务领域，通过定义良好的接口进行通信，实现高内聚低耦合的设计目标。

## 模块架构

### 标准模块结构

```
LYBT.Module.[ModuleName]/
├── Interfaces/              # 服务接口定义
│   ├── I[ModuleName]Service.cs
│   └── I[ModuleName]Repository.cs
├── Services/               # 业务逻辑实现
│   └── [ModuleName]Service.cs
├── Repositories/           # 数据访问实现
│   └── [ModuleName]Repository.cs
├── Mapping/               # AutoMapper配置
│   └── [ModuleName]MappingProfile.cs
├── Controllers/           # API控制器（仅WebAPI项目）
│   └── [ModuleName]Controller.cs
└── [ModuleName]Module.cs  # 模块注册类
```

### 模块职责

1. **单一职责**：每个模块只负责一个业务领域
2. **自包含**：模块内部包含完整的业务逻辑
3. **接口隔离**：通过接口对外提供服务
4. **依赖管理**：明确声明对其他模块的依赖

## 核心模块列表

| 模块名称 | 英文名称 | 主要功能 | 依赖模块 |
|---------|---------|---------|---------|
| 身份认证 | Auth | 用户认证、授权管理 | Users |
| 用户管理 | Users | 系统用户管理 | - |
| 患者档案 | Patients | 患者信息管理 | - |
| 医生管理 | Doctors | 医生信息、排班管理 | Users |
| 挂号预约 | Registration | 患者挂号、预约管理 | Patients, Doctors |
| 诊断治疗 | DiagnosisTreatment | 诊断记录、治疗方案 | Patients, Doctors |
| 处方管理 | Prescriptions | 处方开具、智能推荐 | Herbs, FormulaTemplates |
| 中药材管理 | Herbs | 药材目录、库存管理 | - |
| 验方模板 | FormulaTemplates | 经典验方模板管理 | Herbs |
| 药房管理 | Pharmacy | 处方调配、发药管理 | Prescriptions, Herbs |
| 收费结算 | Billing | 费用计算、收费管理 | Registration, Prescriptions |
| 病历档案 | Records | 电子病历管理 | Patients, Doctors |
| 排队叫号 | Queueing | 患者排队、叫号管理 | Registration |
| 治疗室管理 | TreatmentRoom | 治疗室排班、使用管理 | Doctors |
| 数据同步 | Sync | 数据备份、同步管理 | 所有模块 |

## 模块详细说明

### 1. 身份认证模块（Auth）

**功能描述**：
- 用户登录/登出
- JWT Token生成和验证
- 权限验证
- 密码管理

**核心接口**：
```csharp
public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<bool> LogoutAsync(string userId);
    Task<TokenValidationResult> ValidateTokenAsync(string token);
    Task<bool> ChangePasswordAsync(ChangePasswordDto request);
}
```

**数据实体**：
- AdminSecret（管理员密钥）
- UserToken（用户令牌）
- RefreshToken（刷新令牌）

### 2. 用户管理模块（Users）

**功能描述**：
- 系统用户CRUD操作
- 角色管理
- 权限分配
- 用户状态管理

**核心接口**：
```csharp
public interface IUserService
{
    Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);
    Task<ApiResponse<UserDto>> CreateUserAsync(UserCreateDto dto);
    Task<ApiResponse<UserDto>> UpdateUserAsync(UserEditDto dto);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
}
```

**数据实体**：
- User（用户）
- Role（角色）
- Permission（权限）
- UserRole（用户角色关系）

### 3. 患者档案模块（Patients）

**功能描述**：
- 患者信息登记
- 患者档案管理
- 过敏史记录
- 就诊历史查询

**核心接口**：
```csharp
public interface IPatientService
{
    Task<ApiResponse<PatientDetailDto>> RegisterPatientAsync(PatientCreateDto dto);
    Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(PatientEditDto dto);
    Task<ApiResponse<IEnumerable<VisitHistoryDto>>> GetPatientHistoryAsync(Guid patientId);
    Task<ApiResponse<bool>> AddAllergyRecordAsync(AllergyRecordDto dto);
}
```

**数据实体**：
- Patient（患者）
- AllergyRecord（过敏记录）
- EmergencyContact（紧急联系人）

### 4. 医生管理模块（Doctors）

**功能描述**：
- 医生信息管理
- 专长设置
- 排班管理
- 出诊时间设置

**核心接口**：
```csharp
public interface IDoctorService
{
    Task<ApiResponse<DoctorDetailDto>> GetDoctorByIdAsync(Guid id);
    Task<ApiResponse<IEnumerable<DoctorScheduleDto>>> GetSchedulesAsync(Guid doctorId);
    Task<ApiResponse<bool>> UpdateScheduleAsync(ScheduleUpdateDto dto);
    Task<ApiResponse<IEnumerable<DoctorDto>>> GetAvailableDoctorsAsync(DateTime date);
}
```

**数据实体**：
- Doctor（医生）
- DoctorSpecialty（医生专长）
- DoctorSchedule（医生排班）

### 5. 挂号预约模块（Registration）

**功能描述**：
- 现场挂号
- 预约挂号
- 挂号查询
- 取消挂号

**核心接口**：
```csharp
public interface IRegistrationService
{
    Task<ApiResponse<RegistrationDto>> CreateRegistrationAsync(RegistrationCreateDto dto);
    Task<ApiResponse<bool>> CancelRegistrationAsync(Guid id, string reason);
    Task<ApiResponse<IEnumerable<RegistrationDto>>> GetTodayRegistrationsAsync();
    Task<ApiResponse<RegistrationStatisticsDto>> GetStatisticsAsync(DateRangeDto range);
}
```

**数据实体**：
- Registration（挂号记录）
- RegistrationType（挂号类型）
- RegistrationStatus（挂号状态）

### 6. 诊断治疗模块（DiagnosisTreatment）

**功能描述**：
- 诊断记录
- 辨证施治
- 治疗方案制定
- 诊断模板

**核心接口**：
```csharp
public interface IDiagnosisService
{
    Task<ApiResponse<DiagnosisDto>> CreateDiagnosisAsync(DiagnosisCreateDto dto);
    Task<ApiResponse<TreatmentPlanDto>> GenerateTreatmentPlanAsync(Guid diagnosisId);
    Task<ApiResponse<IEnumerable<DiagnosisTemplateDto>>> GetTemplatesAsync();
}
```

**数据实体**：
- Diagnosis（诊断）
- TreatmentPlan（治疗方案）
- DiagnosisTemplate（诊断模板）

### 7. 处方管理模块（Prescriptions）

**功能描述**：
- 处方开具
- 智能方剂推荐
- 用药指导
- 处方审核

**核心接口**：
```csharp
public interface IPrescriptionService
{
    Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync(PrescriptionCreateDto dto);
    Task<ApiResponse<IEnumerable<FormulaRecommendationDto>>> GetRecommendationsAsync(string symptoms);
    Task<ApiResponse<bool>> ApprovePrescriptionAsync(Guid id, ApprovalDto approval);
    Task<ApiResponse<PrescriptionPrintDto>> GetPrintDataAsync(Guid id);
}
```

**数据实体**：
- Prescription（处方）
- PrescriptionItem（处方项）
- PrescriptionApproval（处方审核）

### 8. 中药材管理模块（Herbs）

**功能描述**：
- 药材目录维护
- 库存管理
- 价格管理
- 药材属性设置

**核心接口**：
```csharp
public interface IHerbService
{
    Task<ApiResponse<HerbDto>> AddHerbAsync(HerbCreateDto dto);
    Task<ApiResponse<bool>> UpdateStockAsync(StockUpdateDto dto);
    Task<ApiResponse<bool>> UpdatePriceAsync(PriceUpdateDto dto);
    Task<ApiResponse<IEnumerable<HerbDto>>> SearchHerbsAsync(string keyword);
}
```

**数据实体**：
- Herb（药材）
- HerbCategory（药材分类）
- HerbStock（库存记录）
- HerbPrice（价格历史）

### 9. 验方模板模块（FormulaTemplates）

**功能描述**：
- 经典方剂管理
- 个人验方收藏
- 方剂分类管理
- 使用频率统计

**核心接口**：
```csharp
public interface IFormulaTemplateService
{
    Task<ApiResponse<FormulaTemplateDto>> CreateTemplateAsync(FormulaTemplateCreateDto dto);
    Task<ApiResponse<IEnumerable<FormulaTemplateDto>>> GetTemplatesByCategoryAsync(Guid categoryId);
    Task<ApiResponse<IEnumerable<UsageStatisticsDto>>> GetUsageStatisticsAsync();
}
```

**数据实体**：
- FormulaTemplate（验方模板）
- FormulaIngredient（方剂成分）
- FormulaCategory（方剂分类）

### 10. 药房管理模块（Pharmacy）

**功能描述**：
- 处方调配
- 发药管理
- 药品核对
- 调配记录

**核心接口**：
```csharp
public interface IPharmacyService
{
    Task<ApiResponse<DispenseDto>> DispensePrescriptionAsync(Guid prescriptionId);
    Task<ApiResponse<bool>> ConfirmDispenseAsync(Guid dispenseId);
    Task<ApiResponse<IEnumerable<DispenseRecordDto>>> GetDispenseRecordsAsync(DateRangeDto range);
}
```

**数据实体**：
- DispenseRecord（调配记录）
- DispenseItem（调配项）
- PharmacyStock（药房库存）

### 11. 收费结算模块（Billing）

**功能描述**：
- 费用计算
- 收费处理
- 退费管理
- 财务报表

**核心接口**：
```csharp
public interface IBillingService
{
    Task<ApiResponse<BillDto>> GenerateBillAsync(Guid registrationId);
    Task<ApiResponse<PaymentResultDto>> ProcessPaymentAsync(PaymentDto payment);
    Task<ApiResponse<RefundResultDto>> ProcessRefundAsync(RefundDto refund);
    Task<ApiResponse<FinancialReportDto>> GetFinancialReportAsync(DateRangeDto range);
}
```

**数据实体**：
- Bill（账单）
- Payment（支付记录）
- RefundRecord（退费记录）
- Invoice（发票）

### 12. 病历档案模块（Records）

**功能描述**：
- 电子病历创建
- 病历查询
- 病历共享
- 历史记录管理

**核心接口**：
```csharp
public interface IRecordService
{
    Task<ApiResponse<RecordDto>> CreateRecordAsync(RecordCreateDto dto);
    Task<ApiResponse<RecordDetailDto>> GetRecordByIdAsync(Guid id);
    Task<ApiResponse<bool>> ShareRecordAsync(RecordShareDto dto);
    Task<ApiResponse<IEnumerable<RecordSummaryDto>>> GetPatientRecordsAsync(Guid patientId);
}
```

**数据实体**：
- MedicalRecord（病历）
- RecordAttachment（病历附件）
- RecordShare（病历共享）

### 13. 排队叫号模块（Queueing）

**功能描述**：
- 排队管理
- 叫号服务
- 队列调整
- 等待时间预估

**核心接口**：
```csharp
public interface IQueueingService
{
    Task<ApiResponse<QueueItemDto>> AddToQueueAsync(Guid registrationId);
    Task<ApiResponse<CallResultDto>> CallNextAsync(Guid doctorId);
    Task<ApiResponse<int>> GetWaitingTimeAsync(Guid queueItemId);
    Task<ApiResponse<IEnumerable<QueueStatusDto>>> GetCurrentQueuesAsync();
}
```

**数据实体**：
- QueueItem（队列项）
- QueueStatus（队列状态）
- CallRecord（叫号记录）

### 14. 治疗室管理模块（TreatmentRoom）

**功能描述**：
- 治疗室排班
- 使用记录
- 设备管理
- 清洁消毒记录

**核心接口**：
```csharp
public interface ITreatmentRoomService
{
    Task<ApiResponse<RoomScheduleDto>> ScheduleRoomAsync(RoomScheduleCreateDto dto);
    Task<ApiResponse<IEnumerable<RoomDto>>> GetAvailableRoomsAsync(DateTime dateTime);
    Task<ApiResponse<bool>> RecordCleaningAsync(CleaningRecordDto dto);
}
```

**数据实体**：
- TreatmentRoom（治疗室）
- RoomSchedule（房间排班）
- Equipment（设备）
- CleaningRecord（清洁记录）

### 15. 数据同步模块（Sync）

**功能描述**：
- 数据备份
- 跨系统同步
- 数据导入导出
- 同步日志

**核心接口**：
```csharp
public interface ISyncService
{
    Task<ApiResponse<BackupResultDto>> BackupDataAsync(BackupOptionsDto options);
    Task<ApiResponse<RestoreResultDto>> RestoreDataAsync(string backupFile);
    Task<ApiResponse<SyncResultDto>> SyncWithRemoteAsync(SyncConfigDto config);
    Task<ApiResponse<IEnumerable<SyncLogDto>>> GetSyncLogsAsync(DateRangeDto range);
}
```

**数据实体**：
- SyncLog（同步日志）
- BackupRecord（备份记录）
- SyncConfiguration（同步配置）

## 模块间通信

### 通信原则

1. **接口隔离**：模块间只通过接口通信
2. **最小依赖**：只依赖必要的模块
3. **异步通信**：优先使用异步方法
4. **事件驱动**：使用事件进行松耦合通信

### 通信模式

#### 1. 直接调用
```csharp
public class RegistrationService : IRegistrationService
{
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    
    public RegistrationService(IPatientService patientService, IDoctorService doctorService)
    {
        _patientService = patientService;
        _doctorService = doctorService;
    }
}
```

#### 2. 事件通知
```csharp
// 发布事件
_eventBus.Publish(new RegistrationCreatedEvent
{
    RegistrationId = registration.Id,
    PatientId = registration.PatientId,
    DoctorId = registration.DoctorId
});

// 订阅事件
_eventBus.Subscribe<RegistrationCreatedEvent>(async (e) =>
{
    await _queueingService.AddToQueueAsync(e.RegistrationId);
});
```

### 依赖关系图

```
Auth ──────────> Users
     │
Registration ──> Patients
     │      └──> Doctors
     │
DiagnosisTreatment ──> Patients
     │            └──> Doctors
     │
Prescriptions ──> Herbs
     │       └──> FormulaTemplates
     │
Pharmacy ────> Prescriptions
     │   └──> Herbs
     │
Billing ────> Registration
     │   └──> Prescriptions
     │
Records ────> Patients
     │   └──> Doctors
     │
Queueing ───> Registration
     │
TreatmentRoom ──> Doctors
     │
Sync ───────> [All Modules]
```

## 模块开发规范

### 1. 命名规范

- 模块名称：`LYBT.Module.[ModuleName]`
- 接口命名：`I[ModuleName]Service`
- 实现命名：`[ModuleName]Service`
- DTO命名：`[Entity][Action]Dto`

### 2. 文件组织

```
src/Backend/Modules/LYBT.Module.[ModuleName]/
├── Interfaces/
├── Services/
├── Repositories/
├── Mapping/
├── Extensions/
└── [ModuleName]Module.cs
```

### 3. 依赖注入

```csharp
public class [ModuleName]Module : IModule
{
    public void RegisterServices(IServiceCollection services)
    {
        // 注册服务
        services.AddScoped<I[ModuleName]Service, [ModuleName]Service>();
        services.AddScoped<I[ModuleName]Repository, [ModuleName]Repository>();
        
        // 注册AutoMapper配置
        services.AddAutoMapper(typeof([ModuleName]MappingProfile));
    }
}
```

### 4. 数据访问

```csharp
public class [ModuleName]Repository : BaseRepository<[Entity]>, I[ModuleName]Repository
{
    public [ModuleName]Repository(AppDbContext context) : base(context)
    {
    }
    
    // 实现特定的数据访问方法
}
```

### 5. 业务逻辑

```csharp
public class [ModuleName]Service : I[ModuleName]Service
{
    private readonly I[ModuleName]Repository _repository;
    private readonly IMapper _mapper;
    
    public [ModuleName]Service(
        I[ModuleName]Repository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
    
    // 实现业务逻辑方法
}
```

## 前后端模块对应

### 前端模块结构

```
src/Frontend/Desktop/Modules/
├── Authentication/      # 登录认证
├── Common/             # 公共组件
├── Doctor/             # 医生工作台
├── FrontDesk/          # 前台接待
├── Cashier/            # 收费台
├── Pharmacist/         # 药房工作台
├── SystemManagement/   # 系统管理
│   ├── Users/         # 用户管理
│   ├── Patients/      # 患者管理
│   ├── Doctors/       # 医生管理
│   ├── Herbs/         # 药材管理
│   ├── FormulaTemplates/ # 验方管理
│   ├── Prescriptions/ # 处方管理
│   ├── Records/       # 病历管理
│   ├── Registrations/ # 挂号管理
│   └── Queueing/      # 排队管理
└── Physiotherapy/      # 理疗管理
```

### 模块映射关系

| 后端模块 | 前端模块 | 说明 |
|---------|---------|------|
| Auth | Authentication | 登录认证界面 |
| Users | SystemManagement/Users | 用户管理界面 |
| Patients | SystemManagement/Patients | 患者档案管理 |
| Doctors | SystemManagement/Doctors | 医生信息管理 |
| Registration | FrontDesk + SystemManagement/Registrations | 挂号服务 |
| DiagnosisTreatment | Doctor | 医生诊疗界面 |
| Prescriptions | Doctor + SystemManagement/Prescriptions | 处方开具 |
| Herbs | SystemManagement/Herbs | 药材管理 |
| FormulaTemplates | SystemManagement/FormulaTemplates | 验方管理 |
| Pharmacy | Pharmacist | 药房工作台 |
| Billing | Cashier | 收费工作台 |
| Records | SystemManagement/Records | 病历管理 |
| Queueing | FrontDesk + SystemManagement/Queueing | 排队叫号 |
| TreatmentRoom | Physiotherapy | 治疗室管理 |

### 前端模块职责

1. **Authentication**：用户登录、权限验证
2. **Doctor**：医生看诊、开处方、查看病历
3. **FrontDesk**：患者接待、挂号、排队
4. **Cashier**：费用结算、收费、退费
5. **Pharmacist**：处方调配、发药
6. **SystemManagement**：系统配置、基础数据维护
7. **Physiotherapy**：理疗预约、治疗记录

## 模块扩展指南

### 添加新模块步骤

1. **创建模块项目**
   ```bash
   dotnet new classlib -n LYBT.Module.[NewModule]
   ```

2. **定义接口**
   ```csharp
   public interface I[NewModule]Service
   {
       // 定义服务方法
   }
   ```

3. **实现服务**
   ```csharp
   public class [NewModule]Service : I[NewModule]Service
   {
       // 实现业务逻辑
   }
   ```

4. **配置依赖注入**
   ```csharp
   public class [NewModule]Module : IModule
   {
       public void RegisterServices(IServiceCollection services)
       {
           services.AddScoped<I[NewModule]Service, [NewModule]Service>();
       }
   }
   ```

5. **添加到主程序**
   ```csharp
   // 在Program.cs中注册模块
   builder.Services.RegisterModule<[NewModule]Module>();
   ```

### 模块测试

1. **单元测试**
   ```csharp
   [TestClass]
   public class [NewModule]ServiceTests
   {
       [TestMethod]
       public async Task Should_CreateEntity_Successfully()
       {
           // Arrange
           // Act
           // Assert
       }
   }
   ```

2. **集成测试**
   ```csharp
   [TestClass]
   public class [NewModule]IntegrationTests : IntegrationTestBase
   {
       [TestMethod]
       public async Task Should_HandleFullWorkflow()
       {
           // 测试完整业务流程
       }
   }
   ```

## 总结

模块化架构是凌隐宝堂中医诊所诊疗系统的核心设计理念。通过合理的模块划分和清晰的接口定义，系统实现了高内聚低耦合的设计目标，为系统的维护、扩展和演进提供了坚实的基础。每个模块都遵循统一的开发规范，确保了代码质量和系统的一致性。