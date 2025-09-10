# Prescriptions模块保留清单 (keep-surface.md)

**分析目标**: 识别符合"最小职责收敛"目标的公共/内部类型、DTO、Controller与方法
**最小职责定义**: Prescription(处方)=药材集合 + 少量辅助信息；仅负责创建/查询/删除

## 🎯 核心保留类型 (Core Types)

### 数据模型层
```
✅ 保留原因：符合最小数据结构要求
```

#### Entities (继承现有DB结构)
- `src/Shared/LYBT.Shared.Models/Entities/Prescription.cs`
  - Properties: Id, PatientId, DoctorId, MedicalCaseId, Indication, TotalPrice, Status, CreateTime, UpdateTime
- `src/Shared/LYBT.Shared.Models/Entities/PrescriptionItem.cs`  
  - Properties: Id, PrescriptionId, HerbId, Quantity, UnitPrice, Subtotal, UsageInstruction, Notes

#### DTOs
- `src/Shared/LYBT.Shared.Models/DTOs/PrescriptionDto.cs`
- `src/Shared/LYBT.Shared.Models/DTOs/PrescriptionCreateDto.cs`
- `src/Shared/LYBT.Shared.Models/DTOs/PrescriptionUpdateDto.cs`
- `src/Shared/LYBT.Shared.Models/DTOs/PrescriptionItemDto.cs`
- `src/Shared/LYBT.Shared.Models/DTOs/PrescriptionListDto.cs`

### 接口层 (Interfaces)
```
✅ 保留原因：UltraThink架构标准接口，支持最小CRUD功能
```

- `src/Server/Modules/LYBT.Module.Prescriptions/Interfaces/IPrescriptionRepository.cs`
  - 方法: GetByIdAsync, GetByPatientIdAsync, CreateAsync, UpdateAsync, DeleteAsync, SearchAsync
- `src/Server/Modules/LYBT.Module.Prescriptions/Interfaces/IPrescriptionQueryService.cs`
  - 方法: SearchPrescriptionsAsync, GetPrescriptionsByPatientAsync, GetPrescriptionHistoryAsync
- `src/Server/Modules/LYBT.Module.Prescriptions/Interfaces/IPrescriptionBusinessService.cs`
  - 方法: CreatePrescriptionAsync, UpdatePrescriptionAsync, DeletePrescriptionAsync, ValidateBasicCompatibilityAsync
- `src/Server/Modules/LYBT.Module.Prescriptions/Interfaces/IPrescriptionService.cs` (主接口)
  - 方法: CreateAsync, GetByIdAsync, GetByPatientIdAsync, UpdateAsync, DeleteAsync, SearchAsync

### 仓储层 (Repository)
```
✅ 保留原因：数据访问基础设施，安全的EF Core实现
```

- `src/Server/Modules/LYBT.Module.Prescriptions/Repositories/PrescriptionRepository.cs`
  - 保留方法: GetByIdAsync, GetByPatientIdAsync, CreateAsync, UpdateAsync, DeleteAsync
  - 保留方法: SearchPrescriptionsAsync (分页查询)
  - 保留方法: GetPrescriptionItemsAsync, AddPrescriptionItemAsync, RemovePrescriptionItemAsync

### 服务层 (Services) - UltraThink架构
```
✅ 保留原因：标准UltraThink三层架构，委托模式正确
```

#### 主委托层
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`
  - 委托方法: CreateAsync → BusinessService.CreateAsync
  - 委托方法: GetByIdAsync → QueryService.GetByIdAsync  
  - 委托方法: SearchAsync → QueryService.SearchAsync
  - 委托方法: UpdateAsync → BusinessService.UpdateAsync
  - 委托方法: DeleteAsync → BusinessService.DeleteAsync

#### 查询专业层
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionQueryService.cs`
  - 保留方法: GetByIdAsync, GetByPatientIdAsync, SearchPrescriptionsAsync
  - 保留方法: GetPrescriptionHistoryAsync (患者处方历史)
  - 保留方法: GetPrescriptionStatsAsync (基础统计)

#### 业务逻辑层  
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs`
  - 保留方法: CreatePrescriptionAsync (基础创建+基本验证)
  - 保留方法: UpdatePrescriptionAsync (简单更新+状态检查)
  - 保留方法: DeletePrescriptionAsync (软删除+关联检查)
  - 保留方法: ValidateBasicCompatibilityAsync (18反19畏基础检查)

### 控制器层 (API Endpoints)
```
✅ 保留原因：标准RESTful API，/api/v1/prescriptions路由
```

#### 主处方API (需要实现)
- `src/Server/Modules/LYBT.Module.Prescriptions/Controllers/PrescriptionsController.cs`
  - `POST /api/v1/prescriptions` → CreateAsync
  - `GET /api/v1/prescriptions/{id}` → GetByIdAsync
  - `GET /api/v1/prescriptions/patient/{patientId}` → GetByPatientIdAsync  
  - `PUT /api/v1/prescriptions/{id}` → UpdateAsync
  - `DELETE /api/v1/prescriptions/{id}` → DeleteAsync
  - `GET /api/v1/prescriptions/search` → SearchAsync

#### 基础配伍检查API
- `src/Server/Modules/LYBT.Module.Prescriptions/Controllers/CompatibilityNotesController.cs`
  - `GET /api/v1/prescriptions/compatibility/check` → 基础18反19畏检查
  - `GET /api/v1/prescriptions/compatibility/notes` → 配伍记录查询

### 映射配置 (Mapping)
```
✅ 保留原因：DTO与Entity转换必需
```

- `src/Server/Modules/LYBT.Module.Prescriptions/Mapping/PrescriptionMappingProfile.cs`
  - Entity ↔ DTO 基础映射配置
  - PrescriptionCreateDto → Prescription
  - Prescription → PrescriptionDto
  - PrescriptionItem → PrescriptionItemDto

### 模块注册
```
✅ 保留原因：依赖注入和模块集成必需
```

- `src/Server/Modules/LYBT.Module.Prescriptions/PrescriptionsModule.cs`
  - AddPrescriptionsModule() 扩展方法
  - Repository、Service、Controller 注册
  - AutoMapper Profile 注册

## 📋 保留方法清单

### Repository层方法
```csharp
// PrescriptionRepository.cs
Task<Prescription?> GetByIdAsync(Guid id)
Task<List<Prescription>> GetByPatientIdAsync(Guid patientId) 
Task<Prescription> CreateAsync(Prescription prescription)
Task<Prescription> UpdateAsync(Prescription prescription)
Task<bool> DeleteAsync(Guid id)
Task<PagedResult<Prescription>> SearchPrescriptionsAsync(SearchCriteria criteria)
Task<List<PrescriptionItem>> GetPrescriptionItemsAsync(Guid prescriptionId)
```

### QueryService层方法
```csharp
// PrescriptionQueryService.cs
Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
Task<ServiceResult<PagedResult<PrescriptionDto>>> SearchPrescriptionsAsync(PrescriptionSearchDto criteria)
Task<ServiceResult<List<PrescriptionDto>>> GetPrescriptionHistoryAsync(Guid patientId, int months = 12)
```

### BusinessService层方法
```csharp
// PrescriptionBusinessService.cs  
Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(PrescriptionCreateDto dto)
Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(Guid id, PrescriptionUpdateDto dto)
Task<ServiceResult<bool>> DeletePrescriptionAsync(Guid id)
Task<ServiceResult<CompatibilityCheckResult>> ValidateBasicCompatibilityAsync(List<Guid> herbIds)
```

### 主Service层方法 (委托)
```csharp
// PrescriptionService.cs
Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto) 
    => await _businessService.CreatePrescriptionAsync(dto)
Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
    => await _queryService.GetByIdAsync(id)
// ... 其他委托方法
```

## 🎯 总结

**保留原则**:
1. **数据层**: 现有Prescription、PrescriptionItem实体和相关DTO
2. **接口层**: UltraThink标准接口(Repository/Query/Business/Service)
3. **服务层**: 三层架构+主委托层，仅保留基础CRUD和简单验证
4. **API层**: 标准RESTful端点，路由固定在`/api/v1/prescriptions`
5. **基础配伍**: 18反19畏检查，移除智能推荐

**文件统计**:
- 保留文件: ~12个核心文件
- 保留接口: 4个标准接口  
- 保留服务: 4个服务类(Query/Business/主Service/Repository)
- 保留API: 6个基础RESTful端点 + 2个配伍检查端点