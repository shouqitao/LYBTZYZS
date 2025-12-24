# 任务清单: enhance-dataflow-logging

**Change ID**: enhance-dataflow-logging
**Created**: 2025-12-24
**Updated**: 2025-12-24
**Depends On**: unify-desktop-command-handler (已完成)
**Spec Deltas**: logging-infrastructure (LOG-012 ~ LOG-019)

---

## 模块覆盖范围

### Desktop端模块 (8个)
- Auth, Consultation, Formula, Herbs, MedicalCase, Patients, Prescriptions, Users

### Server端模块 (8个)
- Auth, Consultation, Formula, Herbs, MedicalCase, Patients, Prescriptions, Users

---

## Phase 1: Desktop ViewModel日志 (LOG-017)

### Task 1.1: MasterDetailViewModelBase日志增强
- **Requirement**: LOG-017 ViewModel操作日志
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/MasterDetailViewModelBase.cs`
- **内容**:
  - ExecuteSaveAsync: [VM] Save started/completed/failed
  - ExecuteDeleteCurrentAsync: [VM] Delete started/completed/failed
  - LoadDetailForSelectedItemAsync: [VM] LoadDetail started/completed
- **验收**: LOG-017保存、删除、加载详情Scenario通过

### Task 1.2: UnifiedListViewModelBase日志增强
- **Requirement**: LOG-017 ViewModel操作日志
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/UnifiedListViewModelBase.cs`
- **内容**:
  - RefreshAsync: [VM] Refresh started/completed
  - SearchAsync: [VM] Search started/completed
  - 日志包含返回记录数
- **验收**: LOG-017列表刷新Scenario通过

---

## Phase 2: Desktop HTTP日志 (LOG-012, LOG-013, LOG-016)

### Task 2.1: 创建LoggingHttpHandler
- **Requirement**: LOG-012 HTTP客户端请求日志
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Http/LoggingHttpHandler.cs`
- **内容**:
  - [HTTP] >>> Method URI CorrelationId
  - [HTTP] <<< StatusCode Duration CorrelationId
  - 失败时记录响应Body(脱敏后)
- **验收**: LOG-012所有Scenario通过

### Task 2.2: 实现URI敏感数据脱敏
- **Requirement**: LOG-016 URI敏感数据脱敏
- **文件**: `src/Shared/LYBT.Shared.Logging/Masking/SensitiveDataMasker.cs`
- **内容**:
  - 添加MaskUri方法
  - 脱敏password, token, key, secret, credential, apikey, access_token
- **验收**: LOG-016所有Scenario通过

### Task 2.3: 集成traceparent Header
- **Requirement**: LOG-013 分布式追踪Header传递
- **文件**: LoggingHttpHandler.cs (同Task 2.1)
- **内容**:
  - 从Activity.Current获取Id添加traceparent header
- **验收**: LOG-013请求追踪Header Scenario通过

### Task 2.4: 注册HttpHandler到所有Refit客户端
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- **内容**:
  - 注册LoggingHttpHandler到DI
  - 配置到所有API客户端 (IUserApi, IPatientApi, IConsultationApi, IFormulaApi, IHerbApi, IMedicalCaseApi, IPrescriptionApi, IAuthApi)
- **验收**: 所有模块API请求有日志

---

## Phase 3: Server端CorrelationId中间件 (LOG-013)

### Task 3.1: 创建CorrelationIdMiddleware
- **Requirement**: LOG-013 分布式追踪Header传递
- **文件**: `src/Server/Services/LYBT.WebAPI/Middleware/CorrelationIdMiddleware.cs`
- **内容**:
  - 从traceparent header提取CorrelationId
  - 无header时自动生成
  - 设置到HttpContext.TraceIdentifier和LogContext
  - 添加X-Correlation-Id响应头
- **验收**: LOG-013响应追踪Header和自动生成Scenario通过

### Task 3.2: 注册中间件
- **文件**: `src/Server/Services/LYBT.WebAPI/Program.cs`
- **内容**:
  - 在请求管道早期注册CorrelationIdMiddleware
- **验收**: 所有请求有CorrelationId

---

## Phase 4: Server端API日志 (LOG-014)

### Task 4.1: 创建ApiLoggingFilter
- **Requirement**: LOG-014 Server端API Action日志
- **文件**: `src/Server/Services/LYBT.WebAPI/Filters/ApiLoggingFilter.cs`
- **内容**:
  - [API] >>> Action started CorrelationId
  - [API] <<< Action completed Duration CorrelationId
  - 异常时记录Error级别
- **验收**: LOG-014所有Scenario通过

### Task 4.2: 注册全局Filter
- **文件**: `src/Server/Services/LYBT.WebAPI/Extensions/MvcExtensions.cs`
- **内容**:
  - 全局注册ApiLoggingFilter
- **验收**: 所有Controller Action有日志

---

## Phase 5: Repository日志 (LOG-015)

### Task 5.1: Repository基类日志增强
- **Requirement**: LOG-015 Repository操作日志
- **文件**: `src/Server/Core/LYBT.Infrastructure/Repositories/RepositoryBase.cs`
- **内容**:
  - [REPO] Entity.GetById(Id) → Found/NotFound
  - [REPO] Entity.Add
  - [REPO] Entity.Update
  - [REPO] Entity.Delete
- **验收**: LOG-015所有Scenario通过

### Task 5.2: 验证各模块Repository继承
- **文件**:
  - `LYBT.Module.Users/Repositories/UserRepository.cs`
  - `LYBT.Module.Patients/Repositories/PatientRepository.cs`
  - `LYBT.Module.Consultation/Repositories/ConsultationRepository.cs`
  - `LYBT.Module.Formula/Repositories/FormulaRepository.cs`
  - `LYBT.Module.Herbs/Repositories/HerbRepository.cs`
  - `LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`
  - `LYBT.Module.Prescriptions/Repositories/PrescriptionRepository.cs`
- **内容**:
  - 验证正确继承RepositoryBase
  - 自定义方法添加[REPO]前缀日志
- **验收**: 所有模块Repository有日志

---

## Phase 6: Desktop数据处理层日志规范化 (LOG-018, LOG-019)

### Task 6.1: CommandHandler规范化 - Users模块
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/Components/UserCommandHandler.cs`
- **前缀**: [CMD]
- **内容**: 所有CRUD操作日志统一[CMD]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 6.2: CommandHandler规范化 - Patients模块
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/PatientCommandHandler.cs`
- **前缀**: [CMD]
- **内容**: 所有CRUD操作日志统一[CMD]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 6.3: CommandHandler规范化 - Consultation模块
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Services/ConsultationCommandHandler.cs`
- **前缀**: [CMD]
- **内容**: 所有CRUD操作日志统一[CMD]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 6.4: CommandHandler规范化 - Formula模块
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaCommandHandler.cs`
- **前缀**: [CMD]
- **内容**: 所有CRUD操作日志统一[CMD]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 6.5: CommandHandler规范化 - Herbs模块 (新增)
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbCommandHandler.cs`
- **前缀**: [CMD]
- **说明**: unify-desktop-command-handler中新增，已有[CMD]前缀
- **内容**: 验证日志格式符合规范
- **验收**: 日志格式符合LOG-018规范

### Task 6.6: CommandHandler规范化 - MedicalCase模块
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseCommandHandler.cs`
- **前缀**: [CMD]
- **内容**: 所有CRUD操作日志统一[CMD]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 6.7: AggregateService日志 - MedicalCase模块
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseAggregateService.cs`
- **前缀**: [AGG]
- **说明**: 有状态聚合根管理器，使用[AGG]前缀
- **内容**:
  - InitializeAsync: [AGG] Initialize started/completed
  - SaveAsync: [AGG] Save started/completed
  - 聚合子实体操作日志
- **验收**: 日志格式符合LOG-018规范

### Task 6.8: StateManager日志 - Patients模块
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/PatientStateManager.cs`
- **前缀**: [STATE]
- **说明**: 有状态实体管理器，使用[STATE]前缀
- **内容**:
  - InitializeAsync: [STATE] Initialize started/completed
  - SaveAsync: [STATE] Save started/completed
- **验收**: 日志格式符合LOG-018规范

### Task 6.9: 专用Handler日志 - MedicalCase模块
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseNavigationHandler.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseLifecycleHandler.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/PrescriptionSaveHandler.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/PrescriptionItemHandler.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/PrescriptionImportHandler.cs`
- **前缀**: [HDL]
- **说明**: 专用处理器，使用[HDL]前缀
- **内容**: 所有操作日志统一[HDL]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 6.10: 专用Handler日志 - Patients模块
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/UnfinishedCaseHandler.cs`
- **前缀**: [HDL]
- **内容**: 所有操作日志统一[HDL]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 6.11: Auth模块LoginViewModel (认证流程)
- **说明**: Auth模块仅有登录流程，无CRUD操作
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`
- **前缀**: [VM]
- **内容**:
  - Login started/success/failed
- **验收**: 登录流程有完整日志

### Task 6.12: Prescriptions模块PrintService (工具模块)
- **说明**: Prescriptions模块仅有打印服务，无CRUD操作
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`
- **前缀**: [SVC]
- **内容**:
  - PrintPrescriptionAsync: [SVC] Print started/completed
- **验收**: 打印操作有完整日志

---

## Phase 7: Server端Service日志规范化 (LOG-018, LOG-019)

### Task 7.1: Users模块Service
- **文件**: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`
- **前缀**: [SVC]
- **内容**: 所有日志添加[SVC]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 7.2: Patients模块Service
- **文件**: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`
- **前缀**: [SVC]
- **内容**: 所有日志添加[SVC]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 7.3: Consultation模块Service
- **文件**: `src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationService.cs`
- **前缀**: [SVC]
- **内容**: 所有日志添加[SVC]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 7.4: Formula模块Service
- **文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`
- **前缀**: [SVC]
- **内容**: 所有日志添加[SVC]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 7.5: Herbs模块Service
- **文件**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`
- **前缀**: [SVC]
- **内容**: 所有日志添加[SVC]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 7.6: Prescriptions模块Service
- **文件**:
  - `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`
  - `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionNumberService.cs`
- **前缀**: [SVC]
- **内容**: 所有日志添加[SVC]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 7.7: MedicalCase模块Service (5个)
- **文件**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseStateService.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCasePermissionService.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseAuditService.cs`
- **前缀**: [SVC]
- **内容**: 所有日志添加[SVC]前缀
- **验收**: 日志格式符合LOG-018规范

### Task 7.8: Auth模块Service (4个)
- **文件**:
  - `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
  - `src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs`
  - `src/Server/Modules/LYBT.Module.Auth/Services/TokenRevocationService.cs`
  - `src/Server/Modules/LYBT.Module.Auth/Services/SecurityAuditService.cs`
- **前缀**: [SVC]
- **内容**: 所有日志添加[SVC]前缀
- **验收**: 日志格式符合LOG-018规范

---

## Phase 8: 集成测试与文档

### Task 8.1: 端到端追踪测试 - 用户创建
- **内容**:
  - 执行用户创建操作
  - 验证日志链路: [VM] → [CMD] → [HTTP] → [API] → [SVC] → [REPO]
  - 验证CorrelationId贯穿全链路
- **验收**: 用户模块端到端追踪通过

### Task 8.2: 端到端追踪测试 - 患者创建
- **内容**:
  - 执行患者创建操作
  - 验证日志链路: [VM] → [STATE] → [CMD] → [HTTP] → [API] → [SVC] → [REPO]
  - 验证完整日志链路
- **验收**: 患者模块端到端追踪通过

### Task 8.3: 端到端追踪测试 - 医案操作
- **内容**:
  - 执行医案创建/编辑/提交操作
  - 验证日志链路: [VM] → [AGG] → [CMD] → [HTTP] → [API] → [SVC] → [REPO]
  - 验证完整日志链路(包含处方子操作)
- **验收**: 医案模块端到端追踪通过

### Task 8.4: 性能验证
- **内容**:
  - 测量日志额外开销
  - 确保<5ms延迟增加
- **验收**: 性能符合要求

### Task 8.5: 文档更新
- **文件**: `docs/reference/logging.md`
- **内容**:
  - 完整数据流转日志说明
  - 日志前缀规范表 (含[AGG], [STATE], [HDL]新前缀)
  - 各模块日志查询指南
  - 问题排查流程
- **验收**: 文档完整

---

## 完成标准

### Spec Requirements Checklist

- [ ] LOG-012: HTTP客户端请求日志 - 所有Scenario通过
- [ ] LOG-013: 分布式追踪Header传递 - 所有Scenario通过
- [ ] LOG-014: Server端API Action日志 - 所有Scenario通过
- [ ] LOG-015: Repository操作日志 - 所有Scenario通过
- [ ] LOG-016: URI敏感数据脱敏 - 所有Scenario通过
- [ ] LOG-017: ViewModel操作日志 - 所有Scenario通过
- [ ] LOG-018: 日志格式标准化 - 所有Scenario通过
- [ ] LOG-019: 现有日志规范化 - 所有Scenario通过

### Desktop数据处理层 Checklist (按架构模式分类)

**CommandHandler [CMD]** (无状态CRUD):
- [ ] Users - UserCommandHandler
- [ ] Patients - PatientCommandHandler
- [ ] Consultation - ConsultationCommandHandler
- [ ] Formula - FormulaCommandHandler
- [ ] Herbs - HerbCommandHandler (新增)
- [ ] MedicalCase - MedicalCaseCommandHandler

**AggregateService [AGG]** (有状态聚合根):
- [ ] MedicalCase - MedicalCaseAggregateService

**StateManager [STATE]** (有状态实体):
- [ ] Patients - PatientStateManager

**专用Handler [HDL]**:
- [ ] MedicalCase - MedicalCaseNavigationHandler
- [ ] MedicalCase - MedicalCaseLifecycleHandler
- [ ] MedicalCase - PrescriptionSaveHandler
- [ ] MedicalCase - PrescriptionItemHandler
- [ ] MedicalCase - PrescriptionImportHandler
- [ ] Patients - UnfinishedCaseHandler

**特殊模块**:
- [ ] Auth - LoginViewModel (仅登录流程)
- [ ] Prescriptions - PrescriptionPrintService (仅打印)

### Server Service Checklist [SVC]

- [ ] Users - UserService
- [ ] Patients - PatientService
- [ ] Consultation - ConsultationService
- [ ] Formula - FormulaService
- [ ] Herbs - HerbService
- [ ] Prescriptions - PrescriptionService, PrescriptionNumberService
- [ ] MedicalCase - 5个Service
- [ ] Auth - 4个Service

### Quality Checklist

- [ ] 编译通过
- [ ] 所有模块端到端测试通过
- [ ] 性能验证通过
- [ ] 文档更新完成

---

## 任务统计

| Phase | Tasks数 | 预计工作量 |
|-------|---------|-----------|
| Phase 1 | 2 | 小 |
| Phase 2 | 4 | 中 |
| Phase 3 | 2 | 小 |
| Phase 4 | 2 | 小 |
| Phase 5 | 2 | 小 |
| Phase 6 | 12 | 大 |
| Phase 7 | 8 | 中 |
| Phase 8 | 5 | 中 |
| **总计** | **37** | - |
