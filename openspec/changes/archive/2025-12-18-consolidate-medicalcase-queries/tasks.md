# Tasks: consolidate-medicalcase-queries

## Phase 1: Server层新增查询能力 ✅

### 1.1 MedicalCaseQueryService扩展
- [x] 1.1.1 添加 `SearchMedicalCasesAsync` 方法
  - 支持参数: patientName, diagnosisKeyword, startDate, endDate, page, pageSize
  - 返回: PagedResult<MedicalCaseDetailDto> (含嵌套Consultation/Prescription)
- [x] 1.1.2 添加 `GetPatientRecentMedicalCasesAsync` 方法
  - 支持参数: patientId, count
  - 返回: List<MedicalCaseDetailDto>
- [x] 1.1.3 优化EF Core查询，使用Include预加载Consultation/Prescription/Items

### 1.2 MedicalCaseController扩展
- [x] 1.2.1 添加 `[HttpGet("search")]` 端点
  - 路由: GET /api/v1/medicalcases/search
  - 调用MedicalCaseQueryService.SearchMedicalCasesAsync
- [x] 1.2.2 添加 `[HttpGet("patient/{patientId}/recent")]` 端点
  - 路由: GET /api/v1/medicalcases/patient/{patientId}/recent
  - 调用MedicalCaseQueryService.GetPatientRecentMedicalCasesAsync

## Phase 2: Desktop客户端迁移 ✅

### 2.1 IMedicalCaseApi扩展
- [x] 2.1.1 添加 `SearchMedicalCasesAsync` Refit方法
- [x] 2.1.2 添加 `GetPatientRecentMedicalCasesAsync` Refit方法

### 2.2 PrescriptionEditorService迁移
- [x] 2.2.1 注入IMedicalCaseApi替换IPrescriptionApi
- [x] 2.2.2 修改 `LoadRecentPrescriptionsAsync` 调用IMedicalCaseApi
- [x] 2.2.3 适配返回类型从PrescriptionSearchResultDto到MedicalCaseDetailDto

## Phase 3: WebApi清理 ✅

### 3.1 ConsultationController删除
- [x] 3.1.1 删除 `ConsultationController.cs` 文件
- [x] 3.1.2 验证无其他代码引用ConsultationController

### 3.2 PrescriptionsController删除
- [x] 3.2.1 删除 `PrescriptionsController.cs` 文件
- [x] 3.2.2 验证无其他代码引用PrescriptionsController

### 3.3 IConsultationApi删除
- [x] 3.3.1 删除 `IConsultationApi.cs` 文件
- [x] 3.3.2 从DI容器移除IConsultationApi注册
- [x] 3.3.3 验证无Desktop代码引用IConsultationApi

### 3.4 IPrescriptionApi删除
- [x] 3.4.1 删除 `IPrescriptionApi.cs` 文件
- [x] 3.4.2 从DI容器移除IPrescriptionApi注册
- [x] 3.4.3 验证无Desktop代码引用IPrescriptionApi

## Phase 4: Server Service层清理 ✅

### 4.1 PrescriptionService精简
- [x] 4.1.1 删除 `SearchPrescriptionsAsync` 方法
- [x] 4.1.2 删除 `GetPatientRecentPrescriptionsAsync` 方法
- [x] 4.1.3 从IPrescriptionService接口移除对应方法签名

## Phase 5: 测试与验证 ✅

### 5.1 编译验证
- [x] 5.1.1 执行 `dotnet build LYBT.All.sln` 确保无编译错误
- [x] 5.1.2 检查所有模块编译警告

### 5.2 功能验证
- [x] 5.2.1 测试MedicalCase搜索功能
- [x] 5.2.2 测试患者最近医案查询
- [x] 5.2.3 验证处方编辑器历史处方加载

### 5.3 回归测试
- [x] 5.3.1 确保现有MedicalCase CRUD功能正常
- [x] 5.3.2 确保处方打印功能正常

## Phase 6: DTO清理（级联清理） ✅

### 6.1 MedicalCaseBasicDto删除
- [x] 6.1.1 删除 `MedicalCaseBasicDto.cs` 文件
  - 位置: `src\Shared\LYBT.Shared.Models\Contracts\Common\MedicalCaseBasicDto.cs`
  - 原因: 唯一调用者是PrescriptionService.LoadMedicalCasesAsync，该方法在Phase 4中删除
- [x] 6.1.2 验证无其他代码引用MedicalCaseBasicDto

### 6.2 ICrossModuleQueryService MedicalCase方法清理
- [x] 6.2.1 从ICrossModuleQueryService接口删除:
  - `GetMedicalCaseBasicInfoAsync(Guid medicalCaseId)` 方法签名
  - `GetMedicalCasesBasicInfoAsync(IEnumerable<Guid> medicalCaseIds)` 方法签名
- [x] 6.2.2 从CrossModuleQueryService实现删除对应方法体
- [x] 6.2.3 验证无其他代码引用这些方法

### 6.3 PrescriptionService辅助方法清理
- [x] 6.3.1 删除 `LoadMedicalCasesAsync` 私有方法
  - 原因: 该方法服务于被删除的SearchPrescriptionsAsync/GetPatientRecentPrescriptionsAsync

## Phase 7: Client API死代码清理 ✅

> **注**: Phase 7原计划包含DTO统一重构，但经分析后拆分为两部分:
> - 7.A 死代码清理（本Phase，已完成）
> - 7.B DTO统一重构（DEFERRED - 需单独OpenSpec提案）

### 7.A 死代码清理（已完成）

#### 7.A.1 IMedicalCaseApi清理
- [x] 删除 `CreateMedicalCaseWithDetailsAsync` 方法
  - 原因: Server端点POST /api/v1/medicalcases/with-details 不存在，且无调用者
- [x] 删除 `SoftDeleteMedicalCaseAsync` 方法
  - 原因: Server端点DELETE /api/v1/medicalcases/{id}/soft 不存在，且无调用者

#### 7.A.2 MedicalCaseDataManager清理
- [x] 删除 `SoftDeleteMedicalCaseAsync` 包装方法

#### 7.A.3 Repository层清理
- [x] 从 `IMedicalCaseRepository` 删除 `CreateWithDetailsAsync` 接口方法
- [x] 从 `MedicalCaseRepository` 删除 `CreateWithDetailsAsync` 实现

#### 7.A.4 DTO文件清理
- [x] 删除 `MedicalCaseCreateInputDto.cs` 文件
  - 原因: 唯一使用者是已删除的CreateMedicalCaseWithDetailsAsync

#### 7.A.5 验证
- [x] 构建验证通过 (0错误, 5警告与修改前一致)

### 7.B DTO统一重构（DEFERRED）

> **状态**: 延迟执行 - 需要单独的OpenSpec提案
> **原因**: DTO重命名影响范围大，需要完整的设计评审

#### 7.B.1 重构MedicalCaseInputDto (原MedicalCaseAggregateInputDto)
- [ ] 重命名: `MedicalCaseAggregateInputDto` → `MedicalCaseInputDto`
- [ ] 添加: `PatientId?` (创建时必填)
- [ ] 添加: `VisitDate?` (创建时必填，默认当天)
- [ ] 修改: `Id` 改为 `Guid?` (null=创建，有值=更新)
- [ ] 添加服务端验证逻辑

#### 7.B.2 删除冗余Input DTO
- [ ] 删除 `CreateMedicalCaseRequest` (Controller内部类)
- [ ] 更新所有引用点使用新的MedicalCaseInputDto

#### 7.B.3 Output DTO优化
- [ ] 验证 `MedicalCaseListDto` 含Diagnosis字段
- [ ] 验证 `MedicalCaseDetailDto` 聚合模式
- [ ] 验证List/Detail DTO与其他模块设计一致

#### 7.B.4 Server/Client API统一
- [ ] 修改 `POST /medicalcases` 使用统一的MedicalCaseInputDto
- [ ] 验证所有端点返回正确的DTO类型

### 7.C API路由不匹配问题（记录）

> **状态**: 已记录，需单独Issue跟踪

- `DeletePrescriptionAsync` API路由不匹配:
  - Client调用: DELETE /api/v1/medicalcases/{medicalCaseId}/prescription
  - Server端点: DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}
  - 分析: 有实际调用者(PrescriptionPanelViewModel.cs:386)，需要修复而非删除
