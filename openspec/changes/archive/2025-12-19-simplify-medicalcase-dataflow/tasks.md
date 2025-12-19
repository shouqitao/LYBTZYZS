# Tasks: simplify-medicalcase-dataflow

> 状态: Phase 5 已完成，准备归档
> 创建日期: 2025-12-19
> 更新日期: 2025-12-19
> 依赖: unify-medicalcase-input-dto (基础DTO简化已完成)

## Phase 0: 实体字段优化 + 权限逻辑统一 (2025-12-19)

### Task 0.1: MedicalCase实体字段变更 ✅
- [x] 删除 `ConsultationDate` 字段 (用CreatedAt代替)
- [x] 重命名 `DoctorId` → `UserId`
- [x] 新增 `CaseNumber` 字段 (StringLength 50, 业务编号)
- [x] 新增 `CompletedAt` 字段 (DateTime?, 完成时间)
- [x] 删除 `CanEdit()` 方法 (权限判断移到Service)
- [x] 更新 `IsLocked` 属性: `CompletedAt.HasValue || CreatedAt.Date < DateTime.Today`
- [x] 新增 `IsActive` 计算属性: `CaseStatus == Draft || Active`
- [x] 新增 `IsCompleted` 计算属性: `CaseStatus == Completed`
- [x] 更新Entity注释
- [x] 验证编译

### Task 0.2: Prescription实体字段优化 ✅
- [x] 删除 `Indication` 字段
- [x] 删除 `FormulaSource` 字段
- [x] 新增 `Usage` 字段 (StringLength 500, 处方用法)
- [x] 更新Entity注释
- [x] 验证编译

### Task 0.3: 创建数据库迁移 ⏳ (待用户确认后执行)
- [ ] 运行 `dotnet ef migrations add SimplifyMedicalCaseDataflow`
- [ ] 检查迁移脚本包含:
  - 删除 `MedicalCases.ConsultationDate`
  - 新增 `MedicalCases.CaseNumber`
  - 新增 `MedicalCases.CompletedAt`
  - 重命名 `MedicalCases.DoctorId` → `UserId`
  - 删除 `Prescriptions.Indication`
  - 删除 `Prescriptions.FormulaSource`
  - 新增 `Prescriptions.Usage`
- [ ] 应用迁移 `dotnet ef database update`

### Task 0.4: 更新DTO同步 ✅
- [x] MedicalCaseInputDto: 删除VisitDate(用CreatedAt), DoctorId→UserId
- [x] MedicalCaseDetailDto: 删除ConsultationDate, DoctorId→UserId, 新增CaseNumber/CompletedAt
- [x] MedicalCaseListDto: 删除ConsultationDate, DoctorId→UserId, 新增CaseNumber/CompletedAt
- [x] PrescriptionInputDto: 删除Indication, FormulaSource, Diagnosis; 新增Usage
- [x] PrescriptionDetailDto: 删除Indication, FormulaSource, Diagnosis; 新增Usage
- [x] 验证编译

### Task 0.5: 更新AutoMapper映射 ✅
- [x] MedicalCaseMappingProfile: 更新字段映射
- [x] PrescriptionMappingProfile: 更新字段映射
- [x] 验证编译

### Task 0.6: 权限逻辑统一 ✅
- [x] MedicalCaseRules.cs: 删除CanEdit/CanDelete/ValidateCaseUpdate方法
- [x] MedicalCasePermissionService.cs: 更新CanEdit使用Entity.IsLocked
- [x] MedicalCaseCommandService.cs: 改用PermissionService.CanEdit (注入服务)
- [x] MedicalCaseStateService.cs: 改用PermissionService.CanEdit (注入服务)
- [x] 验证编译

### Task 0.7: 更新Client层 ✅
- [x] MedicalCaseDetailModel: 删除ConsultationDate, 新增CaseNumber/CompletedAt
- [x] MedicalCaseItem: 确认CaseNumber/CompletedAt字段已存在
- [x] MedicalCaseMasterDetailViewModel: 更新SaveDetailAsync(DoctorId→UserId)
- [x] PrescriptionPrintModel: 删除FormulaSource(改用ReferencedFormulas)
- [x] PrescriptionPrintService: 更新Indication获取逻辑(从TCMDiagnosis获取)
- [x] 验证编译

---

## Phase 1: DTO重构 ✅

### Task 1.1: 分析现有Aggregate DTO使用情况 ✅
- [x] 搜索MedicalCaseAggregateInputDto所有使用位置
- [x] 搜索PrescriptionAggregateInputDto所有使用位置
- [x] 记录影响的文件列表

### Task 1.2: 扩展MedicalCaseInputDto ✅
- [x] 添加`Consultation`字段 (ConsultationInputDto?)
- [x] 添加`Prescription`字段 (PrescriptionInputDto?)
- [x] 添加`EditReason`字段 (string?, 从MedicalCaseAggregateInputDto迁移)
- [x] 更新XML文档注释
- [x] 验证编译

### Task 1.3: 简化PrescriptionInputDto ✅
- [x] 分析PrescriptionAggregateInputDto与PrescriptionInputDto差异
- [x] 添加`NeedsPrescription`字段 (从PrescriptionAggregateInputDto迁移)
- [x] 验证编译

### Task 1.4: 替换代码中Aggregate DTO引用 ✅
- [x] Server层: IMedicalCaseCommandService, MedicalCaseCommandService, MedicalCaseController
- [x] Desktop层: IMedicalCaseApi, IMedicalCaseRepository, MedicalCaseRepository
- [x] Desktop MedicalCase模块: IDataProvider, ViewModels, Coordinators
- [x] 集成测试: MedicalCaseControllerIntegrationTests
- [x] 修复Guid?到Guid类型转换问题
- [x] 验证编译

### Task 1.5: 删除Aggregate DTO文件 ✅
- [x] 删除MedicalCaseAggregateInputDto.cs
- [x] 删除PrescriptionAggregateInputDto.cs
- [x] 删除MedicalCaseAggregateInputDtoValidator.cs
- [x] 删除PrescriptionAggregateInputDtoValidator.cs
- [x] 删除对应的Validator测试文件
- [x] 验证编译

---

## Phase 2: Server端业务逻辑重构 ✅

### Task 2.1: 统一MedicalCaseService ✅
- [x] 分析CreateAsync和SaveAggregateAsync差异
- [x] 设计统一的SaveAsync方法
  - Id为null时创建（通过CreateFromInputDtoAsync）
  - Id有值时更新
  - Consultation有值时保存诊断
  - Prescription有值时保存处方
- [x] 实现新的SaveAsync逻辑（CreateFromInputDtoAsync私有方法）

### Task 2.2: 重命名SaveAggregateAsync为SaveAsync ✅
- [x] 重命名IMedicalCaseCommandService.SaveAggregateAsync → SaveAsync
- [x] 重命名MedicalCaseCommandService.SaveAggregateAsync → SaveAsync
- [x] 更新所有调用者（Controller, Client API, Repository, ViewModels）
- [x] 验证编译

### Task 2.3: 更新MedicalCasesController ✅
- [x] 更新POST端点使用统一SaveAsync（支持创建时包含Consultation/Prescription）
- [x] PUT /aggregate端点已使用SaveAsync
- [x] 保留/aggregate端点以兼容现有客户端
- [x] 验证编译

### Task 2.4: 验证AutoMapper映射 ✅
- [x] MedicalCaseMappingProfile已正确配置（UserId、无Aggregate DTO）
- [x] ConsultationInputDto → Consultation映射正确
- [x] PrescriptionInputDto → Prescription映射正确
- [x] 验证编译

---

## Phase 3: Client端适配 ✅

### Task 3.1: 更新MedicalCaseRepository ✅
- [x] IMedicalCaseApi.SaveAggregateAsync → SaveAsync
- [x] IMedicalCaseRepository.SaveAggregateAsync → SaveAsync
- [x] MedicalCaseRepository.SaveAggregateAsync → SaveAsync
- [x] 验证编译

### Task 3.2: 更新MedicalCaseDataManager ✅
- [x] 已使用MedicalCaseInputDto
- [x] Aggregate DTO已在Phase 1删除
- [x] 验证编译

### Task 3.3: 更新MedicalCaseMasterDetailViewModel ✅
- [x] SaveAggregateAsync → SaveAsync
- [x] 验证编译

### Task 3.4: 更新MedicalCaseWorkspaceViewModel ✅
- [x] SaveAggregateAsync → SaveAsync
- [x] SaveDraftWithAggregateAsync → SaveDraftAsync
- [x] CompleteWithAggregateAsync → CompleteAsync
- [x] CancelWithAggregateAsync → CancelAsync
- [x] MedicalCaseWorkspaceCoordinator同步更新
- [x] 验证编译

---

## Phase 4: 测试与验证 ✅

### Task 4.1: 更新Server单元测试 ✅
- [x] 更新MedicalCaseServiceTests (DoctorId→UserId)
- [x] 更新MedicalCaseMappingProfileTests (CaseNumber断言修正)
- [x] 更新MedicalCaseCommandServiceTests (异常类型和isAdmin参数修正)
- [x] 运行测试验证: Server模块324测试全部通过

### Task 4.2: 更新Client单元测试 ✅
- [x] 更新MedicalCaseDataManagerTests (DoctorId→UserId, ConsultationDate删除)
- [x] 更新MedicalCaseValidatorTests (ChiefComplaint已移除)
- [x] 更新MedicalCaseCommandHandlerTests
- [x] 运行测试验证: Desktop模块449测试全部通过

### Task 4.3: 更新集成测试 ✅
- [x] 更新MedicalCasesControllerIntegrationTests
- [x] 运行测试验证

### Task 4.4: 功能验证 ✅
- [x] 修复PrescriptionListDto.Indication字段遗漏 (从Entity删除后DTO未同步)
- [x] 所有Server单元测试通过 (Auth 81, Herbs 33, MedicalCase 41, Patients 54, Users 31, Prescriptions 34, WebAPI 50)
- [x] 所有Desktop单元测试通过 (MedicalCase 228, Consultation 8, Foundation 57, Shell 156)

---

## Phase 5: 清理与文档 ✅

### Task 5.1: 代码清理 ✅
- [x] Aggregate DTO文件已在Phase 1删除
- [x] 清理未使用的引用
- [x] 验证编译(0错误0警告)

### Task 5.2: 更新文档 ✅
- [x] 更新CHANGELOG.md
- [x] 归档本提案

---

## Progress Tracking

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 0 | completed | 实体字段优化 + 权限逻辑统一 (Task 0.3迁移待确认) |
| Phase 1 | completed | DTO重构 - 删除Aggregate DTO，统一到InputDto |
| Phase 2 | completed | Server端业务逻辑 - 统一SaveAsync，POST使用SaveAsync |
| Phase 3 | completed | Client端适配 - 方法重命名，去除Aggregate后缀 |
| Phase 4 | completed | 测试验证 - Server 324测试 + Desktop 449测试全部通过 |
| Phase 5 | completed | 清理文档 - 0错误0警告，CHANGELOG已更新 |

---

## 变更摘要

### MedicalCase实体变更
| 字段 | 操作 | 说明 |
|------|------|------|
| `ConsultationDate` | 删除 | 用CreatedAt代替 |
| `DoctorId` | 重命名 | → UserId |
| `CaseNumber` | 新增 | StringLength 50, 业务编号 |
| `CompletedAt` | 新增 | DateTime?, 完成时间 |
| `CanEdit()` | 删除 | 方法移到PermissionService |
| `IsLocked` | 更新 | `CompletedAt.HasValue \|\| CreatedAt.Date < Today` |
| `IsActive` | 新增 | 计算属性 |
| `IsCompleted` | 新增 | 计算属性 |

### Prescription实体变更
| 字段 | 操作 | 说明 |
|------|------|------|
| `Indication` | 删除 | 打印时从TCMDiagnosis获取 |
| `FormulaSource` | 删除 | 与ReferencedFormulas重复 |
| `Usage` | 新增 | StringLength 500, 处方用法 |

### DTO层字段同步
| DTO | 字段 | 操作 |
|-----|------|------|
| MedicalCaseInputDto | `VisitDate` | 删除(用CreatedAt) |
| MedicalCaseInputDto | `DoctorId` | 重命名→UserId |
| MedicalCaseDetailDto | `ConsultationDate` | 删除 |
| MedicalCaseDetailDto | `CaseNumber/CompletedAt` | 新增 |
| MedicalCaseListDto | `ConsultationDate` | 删除 |
| MedicalCaseListDto | `CaseNumber/CompletedAt` | 新增 |
| PrescriptionInputDto | `Indication/FormulaSource/Diagnosis` | 删除 |
| PrescriptionInputDto | `Usage` | 新增 |
| PrescriptionDetailDto | `Indication/FormulaSource/Diagnosis` | 删除 |
| PrescriptionDetailDto | `Usage` | 新增 |

### Client层字段同步
| 文件 | 字段 | 操作 |
|------|------|------|
| MedicalCaseDetailModel | `ConsultationDate` | 删除 |
| MedicalCaseDetailModel | `CaseNumber/CompletedAt` | 新增 |
| PrescriptionPrintModel | `FormulaSource` | 删除(改用ReferencedFormulas) |

### 权限逻辑统一
| 文件 | 变更 |
|------|------|
| MedicalCaseModel.cs | 删除CanEdit()方法 |
| MedicalCaseRules.cs | 删除CanEdit/CanDelete/ValidateCaseUpdate |
| MedicalCasePermissionService.cs | 唯一权限判断入口 |
| MedicalCaseCommandService.cs | 改用PermissionService |
| MedicalCaseStateService.cs | 改用PermissionService |

### 删除的DTO
- `MedicalCaseAggregateInputDto`
- `PrescriptionAggregateInputDto`

### 统一后的MedicalCaseInputDto
```csharp
public class MedicalCaseInputDto
{
    public Guid? Id { get; set; }
    public required Guid PatientId { get; set; }
    public Guid? UserId { get; set; }  // 重命名自DoctorId
    public string? Remark { get; set; }
    // 无VisitDate - 用CreatedAt代替
    public ConsultationInputDto? Consultation { get; set; }
    public PrescriptionInputDto? Prescription { get; set; }
}
```

### API变更
- 创建: `POST /api/v1/medicalcases` + MedicalCaseInputDto
- 更新: `PUT /api/v1/medicalcases/{id}` + MedicalCaseInputDto
- 删除: SaveAggregate独立端点(如果存在)
