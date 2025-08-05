# Module Specifications

## Table of Contents

1. [Overview](#overview)
2. [Module Architecture](#module-architecture)
3. [Core Module List](#core-module-list)
4. [Module Details](#module-details)
5. [Inter-Module Communication](#inter-module-communication)
6. [Module Development Standards](#module-development-standards)
7. [Frontend-Backend Module Mapping](#frontend-backend-module-mapping)

## Overview

The LYBT Traditional Chinese Medicine Clinic Management System adopts a modular architecture design, dividing complex business functions into 15 independent business modules. Each module is responsible for a specific business domain and communicates through well-defined interfaces to achieve high cohesion and low coupling design goals.

## Module Architecture

### Standard Module Structure

```
LYBT.Module.[ModuleName]/
├── Interfaces/              # Service interface definitions
│   ├── I[ModuleName]Service.cs
│   └── I[ModuleName]Repository.cs
├── Services/               # Business logic implementation
│   └── [ModuleName]Service.cs
├── Repositories/           # Data access implementation
│   └── [ModuleName]Repository.cs
├── Mapping/               # AutoMapper configuration
│   └── [ModuleName]MappingProfile.cs
├── Controllers/           # API controllers (WebAPI project only)
│   └── [ModuleName]Controller.cs
└── [ModuleName]Module.cs  # Module registration class
```

### Module Responsibilities

1. **Single Responsibility**: Each module is responsible for only one business domain
2. **Self-Contained**: Modules contain complete business logic internally
3. **Interface Segregation**: Provide services through interfaces
4. **Dependency Management**: Clearly declare dependencies on other modules

## Core Module List

| Module Name | English Name | Main Functions | Dependencies |
|------------|--------------|----------------|--------------|
| Authentication | Auth | User authentication, authorization management | Users |
| User Management | Users | System user management | - |
| Patient Records | Patients | Patient information management | - |
| Doctor Management | Doctors | Doctor information, scheduling management | Users |
| Registration | Registration | Patient registration, appointment management | Patients, Doctors |
| Diagnosis & Treatment | DiagnosisTreatment | Diagnosis records, treatment plans | Patients, Doctors |
| Prescription Management | Prescriptions | Prescription issuance, intelligent recommendations | Herbs, FormulaTemplates |
| Herb Management | Herbs | Herb catalog, inventory management | - |
| Formula Templates | FormulaTemplates | Classic formula template management | Herbs |
| Pharmacy Management | Pharmacy | Prescription dispensing, medication management | Prescriptions, Herbs |
| Billing & Settlement | Billing | Fee calculation, payment management | Registration, Prescriptions |
| Medical Records | Records | Electronic medical record management | Patients, Doctors |
| Queue Management | Queueing | Patient queuing, number calling management | Registration |
| Treatment Room | TreatmentRoom | Treatment room scheduling, usage management | Doctors |
| Data Sync | Sync | Data backup, synchronization management | All modules |

## Module Details

### 1. Authentication Module (Auth)

**Function Description**:
- User login/logout
- JWT Token generation and validation
- Permission verification
- Password management

**Core Interfaces**:
```csharp
public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<bool> LogoutAsync(string userId);
    Task<TokenValidationResult> ValidateTokenAsync(string token);
    Task<bool> ChangePasswordAsync(ChangePasswordDto request);
}
```

**Data Entities**:
- AdminSecret (Admin secret key)
- UserToken (User token)
- RefreshToken (Refresh token)

### 2. User Management Module (Users)

**Function Description**:
- System user CRUD operations
- Role management
- Permission assignment
- User status management

**Core Interfaces**:
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

**Data Entities**:
- User
- Role
- Permission
- UserRole (User-Role relationship)

### 3. Patient Records Module (Patients)

**Function Description**:
- Patient information registration
- Patient record management
- Allergy history records
- Visit history queries

**Core Interfaces**:
```csharp
public interface IPatientService
{
    Task<ApiResponse<PatientDetailDto>> RegisterPatientAsync(PatientCreateDto dto);
    Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(PatientEditDto dto);
    Task<ApiResponse<IEnumerable<VisitHistoryDto>>> GetPatientHistoryAsync(Guid patientId);
    Task<ApiResponse<bool>> AddAllergyRecordAsync(AllergyRecordDto dto);
}
```

**Data Entities**:
- Patient
- AllergyRecord
- EmergencyContact

### 4. Doctor Management Module (Doctors)

**Function Description**:
- Doctor information management
- Specialty settings
- Schedule management
- Consultation time settings

**Core Interfaces**:
```csharp
public interface IDoctorService
{
    Task<ApiResponse<DoctorDetailDto>> GetDoctorByIdAsync(Guid id);
    Task<ApiResponse<IEnumerable<DoctorScheduleDto>>> GetSchedulesAsync(Guid doctorId);
    Task<ApiResponse<bool>> UpdateScheduleAsync(ScheduleUpdateDto dto);
    Task<ApiResponse<IEnumerable<DoctorDto>>> GetAvailableDoctorsAsync(DateTime date);
}
```

**Data Entities**:
- Doctor
- DoctorSpecialty
- DoctorSchedule

### 5. Registration Module (Registration)

**Function Description**:
- Walk-in registration
- Appointment booking
- Registration queries
- Registration cancellation

**Core Interfaces**:
```csharp
public interface IRegistrationService
{
    Task<ApiResponse<RegistrationDto>> CreateRegistrationAsync(RegistrationCreateDto dto);
    Task<ApiResponse<bool>> CancelRegistrationAsync(Guid id, string reason);
    Task<ApiResponse<IEnumerable<RegistrationDto>>> GetTodayRegistrationsAsync();
    Task<ApiResponse<RegistrationStatisticsDto>> GetStatisticsAsync(DateRangeDto range);
}
```

**Data Entities**:
- Registration
- RegistrationType
- RegistrationStatus

### 6. Diagnosis & Treatment Module (DiagnosisTreatment)

**Function Description**:
- Diagnosis records
- TCM syndrome differentiation
- Treatment plan formulation
- Diagnosis templates

**Core Interfaces**:
```csharp
public interface IDiagnosisService
{
    Task<ApiResponse<DiagnosisDto>> CreateDiagnosisAsync(DiagnosisCreateDto dto);
    Task<ApiResponse<TreatmentPlanDto>> GenerateTreatmentPlanAsync(Guid diagnosisId);
    Task<ApiResponse<IEnumerable<DiagnosisTemplateDto>>> GetTemplatesAsync();
}
```

**Data Entities**:
- Diagnosis
- TreatmentPlan
- DiagnosisTemplate

### 7. Prescription Management Module (Prescriptions)

**Function Description**:
- Prescription issuance
- Intelligent formula recommendations
- Medication guidance
- Prescription approval

**Core Interfaces**:
```csharp
public interface IPrescriptionService
{
    Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync(PrescriptionCreateDto dto);
    Task<ApiResponse<IEnumerable<FormulaRecommendationDto>>> GetRecommendationsAsync(string symptoms);
    Task<ApiResponse<bool>> ApprovePrescriptionAsync(Guid id, ApprovalDto approval);
    Task<ApiResponse<PrescriptionPrintDto>> GetPrintDataAsync(Guid id);
}
```

**Data Entities**:
- Prescription
- PrescriptionItem
- PrescriptionApproval

### 8. Herb Management Module (Herbs)

**Function Description**:
- Herb catalog maintenance
- Inventory management
- Price management
- Herb property settings

**Core Interfaces**:
```csharp
public interface IHerbService
{
    Task<ApiResponse<HerbDto>> AddHerbAsync(HerbCreateDto dto);
    Task<ApiResponse<bool>> UpdateStockAsync(StockUpdateDto dto);
    Task<ApiResponse<bool>> UpdatePriceAsync(PriceUpdateDto dto);
    Task<ApiResponse<IEnumerable<HerbDto>>> SearchHerbsAsync(string keyword);
}
```

**Data Entities**:
- Herb
- HerbCategory
- HerbStock
- HerbPrice

### 9. Formula Templates Module (FormulaTemplates)

**Function Description**:
- Classic formula management
- Personal formula collection
- Formula category management
- Usage frequency statistics

**Core Interfaces**:
```csharp
public interface IFormulaTemplateService
{
    Task<ApiResponse<FormulaTemplateDto>> CreateTemplateAsync(FormulaTemplateCreateDto dto);
    Task<ApiResponse<IEnumerable<FormulaTemplateDto>>> GetTemplatesByCategoryAsync(Guid categoryId);
    Task<ApiResponse<IEnumerable<UsageStatisticsDto>>> GetUsageStatisticsAsync();
}
```

**Data Entities**:
- FormulaTemplate
- FormulaIngredient
- FormulaCategory

### 10. Pharmacy Management Module (Pharmacy)

**Function Description**:
- Prescription dispensing
- Medication distribution
- Drug verification
- Dispensing records

**Core Interfaces**:
```csharp
public interface IPharmacyService
{
    Task<ApiResponse<DispenseDto>> DispensePrescriptionAsync(Guid prescriptionId);
    Task<ApiResponse<bool>> ConfirmDispenseAsync(Guid dispenseId);
    Task<ApiResponse<IEnumerable<DispenseRecordDto>>> GetDispenseRecordsAsync(DateRangeDto range);
}
```

**Data Entities**:
- DispenseRecord
- DispenseItem
- PharmacyStock

### 11. Billing Module (Billing)

**Function Description**:
- Fee calculation
- Payment processing
- Refund management
- Financial reports

**Core Interfaces**:
```csharp
public interface IBillingService
{
    Task<ApiResponse<BillDto>> GenerateBillAsync(Guid registrationId);
    Task<ApiResponse<PaymentResultDto>> ProcessPaymentAsync(PaymentDto payment);
    Task<ApiResponse<RefundResultDto>> ProcessRefundAsync(RefundDto refund);
    Task<ApiResponse<FinancialReportDto>> GetFinancialReportAsync(DateRangeDto range);
}
```

**Data Entities**:
- Bill
- Payment
- RefundRecord
- Invoice

### 12. Medical Records Module (Records)

**Function Description**:
- Electronic medical record creation
- Record queries
- Record sharing
- History management

**Core Interfaces**:
```csharp
public interface IRecordService
{
    Task<ApiResponse<RecordDto>> CreateRecordAsync(RecordCreateDto dto);
    Task<ApiResponse<RecordDetailDto>> GetRecordByIdAsync(Guid id);
    Task<ApiResponse<bool>> ShareRecordAsync(RecordShareDto dto);
    Task<ApiResponse<IEnumerable<RecordSummaryDto>>> GetPatientRecordsAsync(Guid patientId);
}
```

**Data Entities**:
- MedicalRecord
- RecordAttachment
- RecordShare

### 13. Queue Management Module (Queueing)

**Function Description**:
- Queue management
- Number calling service
- Queue adjustment
- Wait time estimation

**Core Interfaces**:
```csharp
public interface IQueueingService
{
    Task<ApiResponse<QueueItemDto>> AddToQueueAsync(Guid registrationId);
    Task<ApiResponse<CallResultDto>> CallNextAsync(Guid doctorId);
    Task<ApiResponse<int>> GetWaitingTimeAsync(Guid queueItemId);
    Task<ApiResponse<IEnumerable<QueueStatusDto>>> GetCurrentQueuesAsync();
}
```

**Data Entities**:
- QueueItem
- QueueStatus
- CallRecord

### 14. Treatment Room Module (TreatmentRoom)

**Function Description**:
- Treatment room scheduling
- Usage records
- Equipment management
- Cleaning records

**Core Interfaces**:
```csharp
public interface ITreatmentRoomService
{
    Task<ApiResponse<RoomScheduleDto>> ScheduleRoomAsync(RoomScheduleCreateDto dto);
    Task<ApiResponse<IEnumerable<RoomDto>>> GetAvailableRoomsAsync(DateTime dateTime);
    Task<ApiResponse<bool>> RecordCleaningAsync(CleaningRecordDto dto);
}
```

**Data Entities**:
- TreatmentRoom
- RoomSchedule
- Equipment
- CleaningRecord

### 15. Data Sync Module (Sync)

**Function Description**:
- Data backup
- Cross-system synchronization
- Data import/export
- Sync logs

**Core Interfaces**:
```csharp
public interface ISyncService
{
    Task<ApiResponse<BackupResultDto>> BackupDataAsync(BackupOptionsDto options);
    Task<ApiResponse<RestoreResultDto>> RestoreDataAsync(string backupFile);
    Task<ApiResponse<SyncResultDto>> SyncWithRemoteAsync(SyncConfigDto config);
    Task<ApiResponse<IEnumerable<SyncLogDto>>> GetSyncLogsAsync(DateRangeDto range);
}
```

**Data Entities**:
- SyncLog
- BackupRecord
- SyncConfiguration

## Inter-Module Communication

### Communication Principles

1. **Interface Segregation**: Modules communicate only through interfaces
2. **Minimal Dependencies**: Depend only on necessary modules
3. **Asynchronous Communication**: Prefer asynchronous methods
4. **Event-Driven**: Use events for loose coupling

### Communication Patterns

#### 1. Direct Invocation
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

#### 2. Event Notification
```csharp
// Publish event
_eventBus.Publish(new RegistrationCreatedEvent
{
    RegistrationId = registration.Id,
    PatientId = registration.PatientId,
    DoctorId = registration.DoctorId
});

// Subscribe to event
_eventBus.Subscribe<RegistrationCreatedEvent>(async (e) =>
{
    await _queueingService.AddToQueueAsync(e.RegistrationId);
});
```

### Dependency Diagram

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

## Module Development Standards

### 1. Naming Conventions

- Module name: `LYBT.Module.[ModuleName]`
- Interface naming: `I[ModuleName]Service`
- Implementation naming: `[ModuleName]Service`
- DTO naming: `[Entity][Action]Dto`

### 2. File Organization

```
src/Backend/Modules/LYBT.Module.[ModuleName]/
├── Interfaces/
├── Services/
├── Repositories/
├── Mapping/
├── Extensions/
└── [ModuleName]Module.cs
```

### 3. Dependency Injection

```csharp
public class [ModuleName]Module : IModule
{
    public void RegisterServices(IServiceCollection services)
    {
        // Register services
        services.AddScoped<I[ModuleName]Service, [ModuleName]Service>();
        services.AddScoped<I[ModuleName]Repository, [ModuleName]Repository>();
        
        // Register AutoMapper configuration
        services.AddAutoMapper(typeof([ModuleName]MappingProfile));
    }
}
```

### 4. Data Access

```csharp
public class [ModuleName]Repository : BaseRepository<[Entity]>, I[ModuleName]Repository
{
    public [ModuleName]Repository(AppDbContext context) : base(context)
    {
    }
    
    // Implement specific data access methods
}
```

### 5. Business Logic

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
    
    // Implement business logic methods
}
```

## Frontend-Backend Module Mapping

### Frontend Module Structure

```
src/Frontend/Desktop/Modules/
├── Authentication/      # Login authentication
├── Common/             # Common components
├── Doctor/             # Doctor workstation
├── FrontDesk/          # Front desk reception
├── Cashier/            # Payment counter
├── Pharmacist/         # Pharmacy workstation
├── SystemManagement/   # System management
│   ├── Users/         # User management
│   ├── Patients/      # Patient management
│   ├── Doctors/       # Doctor management
│   ├── Herbs/         # Herb management
│   ├── FormulaTemplates/ # Formula management
│   ├── Prescriptions/ # Prescription management
│   ├── Records/       # Medical record management
│   ├── Registrations/ # Registration management
│   └── Queueing/      # Queue management
└── Physiotherapy/      # Physiotherapy management
```

### Module Mapping Relationships

| Backend Module | Frontend Module | Description |
|---------------|-----------------|-------------|
| Auth | Authentication | Login authentication interface |
| Users | SystemManagement/Users | User management interface |
| Patients | SystemManagement/Patients | Patient record management |
| Doctors | SystemManagement/Doctors | Doctor information management |
| Registration | FrontDesk + SystemManagement/Registrations | Registration service |
| DiagnosisTreatment | Doctor | Doctor consultation interface |
| Prescriptions | Doctor + SystemManagement/Prescriptions | Prescription issuance |
| Herbs | SystemManagement/Herbs | Herb management |
| FormulaTemplates | SystemManagement/FormulaTemplates | Formula management |
| Pharmacy | Pharmacist | Pharmacy workstation |
| Billing | Cashier | Payment workstation |
| Records | SystemManagement/Records | Medical record management |
| Queueing | FrontDesk + SystemManagement/Queueing | Queue management |
| TreatmentRoom | Physiotherapy | Treatment room management |

### Frontend Module Responsibilities

1. **Authentication**: User login, permission verification
2. **Doctor**: Doctor consultation, prescribing, medical record viewing
3. **FrontDesk**: Patient reception, registration, queuing
4. **Cashier**: Fee settlement, payment, refunds
5. **Pharmacist**: Prescription dispensing, medication distribution
6. **SystemManagement**: System configuration, basic data maintenance
7. **Physiotherapy**: Physiotherapy appointments, treatment records

## Module Extension Guide

### Steps to Add New Module

1. **Create Module Project**
   ```bash
   dotnet new classlib -n LYBT.Module.[NewModule]
   ```

2. **Define Interfaces**
   ```csharp
   public interface I[NewModule]Service
   {
       // Define service methods
   }
   ```

3. **Implement Services**
   ```csharp
   public class [NewModule]Service : I[NewModule]Service
   {
       // Implement business logic
   }
   ```

4. **Configure Dependency Injection**
   ```csharp
   public class [NewModule]Module : IModule
   {
       public void RegisterServices(IServiceCollection services)
       {
           services.AddScoped<I[NewModule]Service, [NewModule]Service>();
       }
   }
   ```

5. **Add to Main Program**
   ```csharp
   // Register module in Program.cs
   builder.Services.RegisterModule<[NewModule]Module>();
   ```

### Module Testing

1. **Unit Testing**
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

2. **Integration Testing**
   ```csharp
   [TestClass]
   public class [NewModule]IntegrationTests : IntegrationTestBase
   {
       [TestMethod]
       public async Task Should_HandleFullWorkflow()
       {
           // Test complete business workflow
       }
   }
   ```

## Summary

Modular architecture is the core design concept of the LYBT Traditional Chinese Medicine Clinic Management System. Through reasonable module division and clear interface definitions, the system achieves high cohesion and low coupling design goals, providing a solid foundation for system maintenance, expansion, and evolution. Each module follows unified development standards, ensuring code quality and system consistency.