# 端到端调用链和序列图

## 系统概述

本文档提供LYBT中医诊所管理系统的端到端调用链分析，包括从前端WPF用户界面到后端数据库的完整数据流。基于UltraThink双层前端架构和传统三层后端架构的混合设计。

## 核心业务流程调用链

### 1. 用户登录流程

```mermaid
sequenceDiagram
    participant U as 用户
    participant LV as LoginView
    participant LVM as LoginViewModel
    participant AS as AuthService
    participant AC as AuthController
    participant ABS as AuthBusinessService
    participant AR as AuthRepository
    participant DB as Database
    participant TC as TokenCache

    U->>LV: 输入用户名密码
    LV->>LVM: LoginCommand.Execute()
    LVM->>AS: LoginAsync(credentials)
    AS->>AC: POST /api/v1/auth/login
    AC->>ABS: AuthenticateAsync(loginDto)
    ABS->>AR: GetByUsernameAsync(username)
    AR->>DB: SELECT * FROM Users WHERE Username
    DB-->>AR: User Entity
    AR-->>ABS: UserDto
    ABS->>ABS: ValidatePassword(password, hash)
    ABS->>ABS: GenerateJwtToken(user)
    ABS-->>AC: AuthResult with JWT
    AC-->>AS: ApiResponse<AuthResult>
    AS->>TC: CacheToken(jwt)
    AS-->>LVM: ServiceResult<AuthResult>
    LVM->>LVM: NavigateToHome()
    LVM-->>LV: UI Update
    LV-->>U: 登录成功，跳转主界面
```

### 2. 患者档案管理流程

```mermaid
sequenceDiagram
    participant U as 用户
    participant PMV as PatientManagementView
    participant PMVM as PatientManagementViewModel
    participant PS as PatientService
    participant PC as PatientsController
    participant PBS as PatientBusinessService
    participant PQS as PatientQueryService
    participant PR as PatientRepository
    participant DB as Database

    Note over U,DB: 创建患者档案
    U->>PMV: 点击新建患者
    PMV->>PMVM: CreatePatientCommand.Execute()
    PMVM->>PS: CreatePatientAsync(patientDto)
    PS->>PC: POST /api/v1/patients
    PC->>PBS: CreatePatientAsync(dto)
    PBS->>PBS: ValidatePatientData(dto)
    PBS->>PR: CreateAsync(patient)
    PR->>DB: INSERT INTO Patients
    DB-->>PR: Created Patient
    PR-->>PBS: Patient Entity
    PBS-->>PC: ServiceResult<PatientDto>
    PC-->>PS: ApiResponse<PatientDto>
    PS-->>PMVM: ServiceResult<PatientDto>
    PMVM->>PMVM: RefreshPatientList()
    PMVM-->>PMV: UI Refresh
    PMV-->>U: 患者创建成功

    Note over U,DB: 搜索患者
    U->>PMV: 输入搜索条件
    PMV->>PMVM: SearchCommand.Execute()
    PMVM->>PS: SearchPatientsAsync(criteria)
    PS->>PC: GET /api/v1/patients/search
    PC->>PQS: SearchPatientsAsync(searchDto)
    PQS->>PR: SearchAsync(criteria)
    PR->>DB: SELECT with LINQ filters
    DB-->>PR: List<Patient>
    PR-->>PQS: PagedResult<PatientDto>
    PQS-->>PC: ServiceResult<PagedResult>
    PC-->>PS: ApiResponse<PagedResult>
    PS-->>PMVM: ServiceResult<PagedResult>
    PMVM->>PMVM: UpdatePatientCollection()
    PMVM-->>PMV: DataGrid Update
    PMV-->>U: 搜索结果显示
```

### 3. 完整诊疗流程

```mermaid
sequenceDiagram
    participant U as 医生用户
    participant CV as ConsultationView
    participant CVM as ConsultationViewModel
    participant CS as ConsultationService
    participant MCS as MedicalCaseService
    participant PS as PrescriptionService
    participant CC as ConsultationController
    participant MCC as MedicalCaseController
    participant PC as PrescriptionsController
    participant CBS as ConsultationBusinessService
    participant MCBS as MedicalCaseBusinessService
    participant PBS as PrescriptionBusinessService
    participant CR as ConsultationRepository
    participant MCR as MedicalCaseRepository
    participant PR as PrescriptionRepository
    participant DB as Database

    Note over U,DB: 创建医疗案例和开始诊断
    U->>CV: 选择患者开始诊断
    CV->>CVM: StartConsultationCommand.Execute()
    CVM->>MCS: CreateMedicalCaseAsync(patientId)
    MCS->>MCC: POST /api/v1/medical-cases
    MCC->>MCBS: CreateAsync(medicalCaseDto)
    MCBS->>MCR: CreateAsync(medicalCase)
    MCR->>DB: INSERT INTO MedicalCases
    DB-->>MCR: Created MedicalCase

    CVM->>CS: CreateConsultationAsync(caseId)
    CS->>CC: POST /api/v1/consultations
    CC->>CBS: CreateConsultationAsync(dto)
    CBS->>CR: CreateAsync(consultation)
    CR->>DB: INSERT INTO Consultations
    DB-->>CR: Created Consultation

    Note over U,DB: 记录四诊信息
    U->>CV: 输入望闻问切信息
    CV->>CVM: SaveDiagnosisCommand.Execute()
    CVM->>CS: UpdateConsultationAsync(consultationDto)
    CS->>CC: PUT /api/v1/consultations/{id}
    CC->>CBS: UpdateAsync(id, dto)
    CBS->>CR: UpdateAsync(consultation)
    CR->>DB: UPDATE Consultations SET 望闻问切 data

    Note over U,DB: 开具处方
    U->>CV: 点击开处方
    CV->>CVM: CreatePrescriptionCommand.Execute()
    CVM->>PS: CreatePrescriptionAsync(prescriptionDto)
    PS->>PC: POST /api/v1/prescriptions
    PC->>PBS: CreateAsync(dto)
    PBS->>PR: CreateAsync(prescription)
    PR->>DB: INSERT INTO Prescriptions

    Note over U,DB: 完成诊断
    U->>CV: 完成诊断
    CV->>CVM: CompleteConsultationCommand.Execute()
    CVM->>MCS: UpdateStatusAsync(caseId, Completed)
    MCS->>MCC: PUT /api/v1/medical-cases/{id}/status
    MCC->>MCBS: UpdateStatusAsync(id, status)
    MCBS->>MCR: UpdateStatusAsync(id, status)
    MCR->>DB: UPDATE MedicalCases SET Status = 'Completed'
```

### 4. 处方管理和验方应用流程

```mermaid
sequenceDiagram
    participant U as 医生
    participant PV as PrescriptionView  
    participant PVM as PrescriptionViewModel
    participant PS as PrescriptionService
    participant FS as FormulaService
    participant HS as HerbService
    participant PC as PrescriptionsController
    participant FC as FormulasController
    participant HC as HerbsController
    participant PBS as PrescriptionBusinessService
    participant FBS as FormulaBusinessService
    participant HBS as HerbBusinessService
    participant PR as PrescriptionRepository
    participant FR as FormulaRepository
    participant HR as HerbRepository
    participant DB as Database

    Note over U,DB: 应用验方模板
    U->>PV: 选择验方模板
    PV->>PVM: SelectFormulaCommand.Execute()
    PVM->>FS: GetFormulaByIdAsync(formulaId)
    FS->>FC: GET /api/v1/formulas/{id}
    FC->>FBS: GetByIdAsync(id)
    FBS->>FR: GetByIdAsync(id)
    FR->>DB: SELECT * FROM Formulas WHERE Id
    DB-->>FR: Formula with Herbs
    FR-->>FBS: FormulaDto
    FBS-->>FC: ServiceResult<FormulaDto>
    FC-->>FS: ApiResponse<FormulaDto>
    FS-->>PVM: ServiceResult<FormulaDto>
    PVM->>PVM: PopulateHerbList(formula.Herbs)

    Note over U,DB: 调整药材用量
    U->>PV: 修改药材用量
    PV->>PVM: UpdateHerbDosageCommand.Execute()
    PVM->>HS: GetHerbDetailsAsync(herbId)
    HS->>HC: GET /api/v1/herbs/{id}
    HC->>HBS: GetByIdAsync(id)
    HBS->>HR: GetByIdAsync(id)
    HR->>DB: SELECT * FROM Herbs WHERE Id
    DB-->>HR: Herb Entity
    PVM->>PVM: CalculateTotalPrice()

    Note over U,DB: 保存最终处方
    U->>PV: 保存处方
    PV->>PVM: SavePrescriptionCommand.Execute()
    PVM->>PS: CreatePrescriptionAsync(prescriptionDto)
    PS->>PC: POST /api/v1/prescriptions
    PC->>PBS: CreateAsync(dto)
    PBS->>PBS: ValidatePrescription(dto)
    PBS->>PBS: CheckDrugInteractions(herbs)
    PBS->>PR: CreateAsync(prescription)
    PR->>DB: BEGIN TRANSACTION
    PR->>DB: INSERT INTO Prescriptions
    PR->>DB: INSERT INTO PrescriptionHerbs (关联表)
    PR->>DB: COMMIT TRANSACTION
    DB-->>PR: Created Prescription with Relations
    PR-->>PBS: Prescription Entity
    PBS-->>PC: ServiceResult<PrescriptionDto>
    PC-->>PS: ApiResponse<PrescriptionDto>
    PS-->>PVM: ServiceResult<PrescriptionDto>
    PVM-->>PV: 处方保存成功提示
```

## 关键架构组件调用关系

### 前端UltraThink双层架构调用流

```mermaid
graph TD
    A[View Layer] -->|Command Binding| B[ViewModel Layer]
    B -->|Service Injection| C[主Service层 - 纯委托]
    C -->|委托调用| D[QueryService - 查询专业层]
    C -->|委托调用| E[BusinessService - 业务逻辑层]
    D -->|HTTP Client| F[Refit API Client]
    E -->|HTTP Client| F
    F -->|REST API| G[Backend Controllers]

    style A fill:#e1f5fe
    style B fill:#f3e5f5
    style C fill:#fff3e0
    style D fill:#e8f5e8
    style E fill:#fff8e1
    style F fill:#fce4ec
    style G fill:#f1f8e9
```

### 后端传统三层架构调用流

```mermaid
graph TD
    A[Controller Layer] -->|依赖注入| B[Service Layer]
    B -->|依赖注入| C[Repository Layer]
    C -->|EF Core| D[AppDbContext]
    D -->|LINQ查询| E[SQL Server Database]

    A -->|异常处理| F[BaseController]
    F -->|统一响应| G[ApiResponse<T>]

    B -->|业务验证| H[ValidationService]
    B -->|缓存管理| I[MemoryCache]
    B -->|JWT认证| J[TokenService]

    style A fill:#ffebee
    style B fill:#f3e5f5
    style C fill:#e8f5e8
    style D fill:#fff3e0
    style E fill:#e1f5fe
```

### 数据流向图

```mermaid
graph LR
    subgraph "前端 WPF Client"
        A[用户交互] --> B[View]
        B --> C[ViewModel]
        C --> D[Service层]
        D --> E[Refit HTTP Client]
    end

    subgraph "网络通信"
        E -->|HTTPS/JSON| F[Web API Endpoint]
    end

    subgraph "后端 Web API"
        F --> G[Controller]
        G --> H[Business Service]
        H --> I[Repository]
        I --> J[EF Core]
        J --> K[SQL Server]
    end

    K -->|数据返回| J
    J --> I
    I --> H
    H --> G
    G -->|JSON Response| F
    F -->|ApiResponse<T>| E
    E --> D
    D --> C
    C --> B
    B --> A
```

## 性能关键点分析

### 1. 数据库查询优化点

- **批量操作**: Repository层使用EF Core的ExecuteUpdateAsync避免N+1查询
- **分页查询**: QueryService层实现PagedResult<T>减少内存占用
- **索引策略**: 关键查询字段(Username, PatientId, 电话号码)建立索引
- **连接池**: 配置Max=20, Min=2适合小型诊所规模

### 2. 缓存策略

- **前端缓存**: ViewModel层缓存常用数据(药材列表、验方模板)
- **后端缓存**: MemoryCache缓存静态数据(角色权限、系统配置)
- **API响应缓存**: 对于读多写少的数据应用响应缓存

### 3. 网络通信优化

- **JWT Token**: 8小时有效期减少认证请求
- **压缩传输**: API响应启用Gzip压缩
- **异步调用**: 所有HTTP请求使用async/await模式

## 错误处理链路

```mermaid
sequenceDiagram
    participant VM as ViewModel
    participant S as Service
    participant C as Controller
    participant BS as BusinessService
    participant R as Repository
    participant DB as Database

    VM->>S: ServiceCall()
    S->>C: HTTP Request
    C->>BS: BusinessOperation()
    BS->>R: DataOperation()
    R->>DB: SQL Query
    DB-->>R: SQLException
    R-->>BS: RepositoryException
    BS-->>C: BusinessException
    C-->>C: HandleException()
    C-->>S: ApiResponse<Error>
    S-->>VM: ServiceResult<Error>
    VM-->>VM: ShowErrorMessage()
```

## 安全验证链路

```mermaid
sequenceDiagram
    participant C as Client
    participant AC as AuthController
    participant JWT as JWTService
    participant BC as BusinessController
    participant Auth as AuthorizeAttribute

    C->>AC: POST /auth/login
    AC->>JWT: GenerateToken()
    JWT-->>AC: JWT Token
    AC-->>C: AuthResult with Token

    C->>BC: GET /api/resource (with Bearer Token)
    Auth->>JWT: ValidateToken()
    JWT-->>Auth: ClaimsPrincipal
    Auth-->>BC: Authorized Request
    BC-->>C: Resource Data
```

## 总结

本系统采用混合架构设计，前端UltraThink双层架构确保了代码的精简和职责清晰，后端传统三层架构保证了系统的稳定性和可维护性。通过端到端的调用链分析，可以看出：

1. **数据流清晰**: 从用户交互到数据持久化的完整链路可追溯
2. **错误处理完善**: 每层都有相应的异常处理机制
3. **性能优化到位**: 关键路径都有相应的缓存和优化策略
4. **安全机制健全**: JWT认证和RBAC权限控制覆盖全链路
5. **架构协调性好**: 前后端架构虽然不同但配合良好

这种混合架构设计适合中小型诊所的业务特点，既保证了开发效率又确保了系统稳定性。