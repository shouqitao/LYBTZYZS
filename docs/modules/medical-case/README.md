# MedicalCase模块文档

## 📋 模块概述

**模块名称**: MedicalCase（医案管理）
**版本**: v2.0 (Epic #1612重构版)
**重构日期**: 2025-10-27
**架构模式**: 三层对齐 + 聚合根模式 + Write/Read/Helper Layer分离

### 业务价值

MedicalCase模块是凌隐宝堂中医诊所管理系统的核心业务模块，负责管理完整的患者就诊流程：

- **📝 诊疗记录管理**: 记录患者主诉、辨证诊断、治疗原则等中医诊疗信息
- **💊 处方关联**: 管理病案与处方的关联关系，实现"一诊一方"约束
- **🔄 流程控制**: 实现三步流程（辨证→标记→处方/完成），支持动态处方标记
- **📊 数据追溯**: 完整的就诊历史记录，支持查询和统计分析

### 核心功能

| 功能分类 | 功能描述 | 业务规则 |
|---------|---------|---------|
| **病案创建** | 为患者创建新的病案记录 | BR-001: 单患者单Active病案 |
| **辨证诊断** | 记录四诊信息和中医诊断 | AR-001: 聚合根管理 |
| **处方管理** | 创建、更新、删除关联处方 | AR-003: 一诊一方约束 |
| **流程控制** | 三步流程验证和状态管理 | BF-002: 三步流程控制 |
| **查询统计** | 病案列表、详情查询、历史记录 | - |

---

## 🏗️ 架构设计

### 三层对齐架构

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  MedicalCaseController.cs (WebAPI)                   │  │
│  │  - Write Layer: 8个端点（创建/更新/删除）            │  │
│  │  - Read Layer: 4个端点（查询/列表）                  │  │
│  │  - Helper Layer: 2个端点（验证）                     │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓ DTO
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  IMedicalCaseService / MedicalCaseService            │  │
│  │  - 业务规则验证 (BR-001, AR-003, BF-002)            │  │
│  │  - 聚合根协调 (AR-001)                               │  │
│  │  - 事务管理                                          │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓ Entity
┌─────────────────────────────────────────────────────────────┐
│                    Domain/Data Layer                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  MedicalCaseRepository.cs                            │  │
│  │  - GetByIdWithDetailsAsync (预加载)                  │  │
│  │  - GetPagedWithDetailsAsync (分页查询)               │  │
│  │  - CRUD操作                                          │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  MedicalCase.cs (聚合根实体)                        │  │
│  │  - Consultation (1:1导航属性)                       │  │
│  │  - Prescription (0..1导航属性)                      │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 聚合根边界

**MedicalCase聚合根**管理以下实体的完整生命周期：

```
MedicalCase (聚合根)
  ├── Consultation (辨证诊断记录) - 1:1关系
  │   ├── 主诉 (ChiefComplaint)
  │   ├── 四诊信息 (望闻问切)
  │   ├── 中医诊断 (TCMDiagnosis)
  │   └── 治疗原则 (TreatmentPrinciple)
  │
  └── Prescription (处方) - 0..1关系
      ├── 处方编号 (PrescriptionNumber)
      ├── 剂数 (DosageCount)
      └── PrescriptionItems (处方明细) - 1:N关系
```

**关键设计原则**:
- ✅ **聚合根唯一入口**: 所有Consultation和Prescription的创建/修改必须通过MedicalCase聚合根
- ✅ **事务边界**: 一个MedicalCase的完整修改在一个事务中完成
- ✅ **一致性保证**: 聚合根负责维护内部一致性（如一诊一方约束）

---

## 📡 API端点列表

**完整API文档**: [`docs/api/medicalcase-api.md`](../../api/medicalcase-api.md)

### Write Layer - 写操作（8个端点）

| 端点 | 方法 | 说明 | 业务规则 |
|-----|------|------|---------|
| `/api/v1/medicalcases` | POST | 创建新病案 | AR-001, BR-001 |
| `/api/v1/medicalcases/{id}/consultation` | PUT | 更新辨证信息（Step 1） | AR-001 |
| `/api/v1/medicalcases/{id}/prescription-flag` | PUT | 标记是否开处方（Step 2） | BF-002 |
| `/api/v1/medicalcases/{id}/prescriptions` | POST | 创建处方（Step 3a） | AR-003 |
| `/api/v1/medicalcases/{id}/prescriptions/{prescriptionId}` | PUT | 更新处方 | AR-001 |
| `/api/v1/medicalcases/{id}/prescriptions/{prescriptionId}` | DELETE | 删除处方 | AR-001 |
| `/api/v1/medicalcases/{id}/status` | PUT | 更新病案状态 | - |
| `/api/v1/medicalcases/{id}/complete` | PUT | 完成病案（Step 3b） | BF-002 |

### Read Layer - 读操作（4个端点）

| 端点 | 方法 | 说明 | 特性 |
|-----|------|------|------|
| `/api/v1/medicalcases/{id}` | GET | 获取病案详情 | 预加载Consultation/Prescription |
| `/api/v1/medicalcases` | GET | 查询病案列表（分页） | 支持status/patientId过滤 |
| `/api/v1/medicalcases/{medicalCaseId}/consultations` | GET | 查询辨证记录列表 | - |
| `/api/v1/medicalcases/{medicalCaseId}/prescriptions` | GET | 查询处方列表 | - |

### Helper Layer - 辅助操作（2个端点）

| 端点 | 方法 | 说明 |
|-----|------|------|
| `/api/v1/medicalcases/{id}/can-edit` | GET | 验证病案是否可编辑 |
| `/api/v1/medicalcases/{id}/prescriptions/{prescriptionId}/can-delete` | GET | 验证处方是否可删除 |

---

## 🗄️ 数据模型

### 实体关系图

```
┌─────────────────────────────────┐
│      MedicalCase (聚合根)        │
├─────────────────────────────────┤
│ Id: Guid                        │
│ PatientId: Guid                 │
│ PatientName: string             │
│ DoctorId: Guid                  │
│ DoctorName: string              │
│ ConsultationDate: DateTime      │
│ Status: MedicalCaseStatus       │
│ NeedsPrescription: bool         │
│ Remark: string?                 │
│ CreatedAt: DateTime             │
│ CreatedBy: Guid                 │
│ UpdatedAt: DateTime?            │
│ UpdatedBy: Guid?                │
│ RowVersion: byte[]              │
└─────────────────────────────────┘
           │ 1
           │
           ├──────────────┐
           │              │
       1:1 │          0..1│
           │              │
           ↓              ↓
┌──────────────────┐  ┌────────────────────┐
│   Consultation   │  │   Prescription     │
├──────────────────┤  ├────────────────────┤
│ Id: Guid         │  │ Id: Guid           │
│ MedicalCaseId    │  │ MedicalCaseId      │
│ ChiefComplaint   │  │ PrescriptionNumber │
│ PresentIllness   │  │ Indication         │
│ Inspection       │  │ DosageCount        │
│ Auscultation     │  │ Usage              │
│ Inquiry          │  │ Discount           │
│ Palpation        │  │ TotalAmount        │
│ TCMDiagnosis     │  │ Status             │
│ TreatmentPrinciple│ │ CreatedAt          │
│ MedicalAdvice    │  │ CreatedBy          │
│ Step1CompletedAt │  └────────────────────┘
│ Step2CompletedAt │            │
│ Remark           │            │ 1
└──────────────────┘            │
                                │ N
                                ↓
                  ┌──────────────────────────┐
                  │  PrescriptionItem        │
                  ├──────────────────────────┤
                  │ Id: Guid                 │
                  │ PrescriptionId: Guid     │
                  │ HerbId: Guid             │
                  │ HerbName: string         │
                  │ Quantity: decimal        │
                  │ Unit: string             │
                  │ UnitPrice: decimal       │
                  │ Subtotal: decimal        │
                  │ Remark: string?          │
                  └──────────────────────────┘
```

### 关键字段说明

**MedicalCase实体**:
- `Status`: 枚举值（Active, Completed, Cancelled）
- `NeedsPrescription`: 动态流程控制标志（Step 2）
- `RowVersion`: 乐观并发控制

**Consultation实体**:
- `Step1CompletedAt`: 辨证完成时间戳（Step 1）
- `Step2CompletedAt`: 标记处方需求完成时间戳（Step 2）
- 四诊字段: `Inspection`(望)、`AuscultationOlfaction`(闻)、`Inquiry`(问)、`Palpation`(切)

**Prescription实体**:
- `PrescriptionNumber`: 处方编号（可选，系统可自动生成）
- `DosageCount`: 剂数（默认7剂）
- `Discount`: 折扣（0.0-1.0，默认1.0）

---

## 📜 业务规则

### AR-001: 聚合根创建和管理

**规则描述**: MedicalCase作为聚合根，管理Consultation和Prescription的完整生命周期。

**实施细节**:
- ✅ 所有Consultation的创建/更新通过`MedicalCaseService.UpdateConsultationAsync()`
- ✅ 所有Prescription的创建/更新/删除通过`MedicalCaseService.*PrescriptionAsync()`
- ❌ 禁止直接访问ConsultationRepository或PrescriptionRepository

**代码示例**:
```csharp
// ✅ 正确: 通过聚合根创建处方
var prescription = await _medicalCaseService.CreatePrescriptionAsync(
    medicalCaseId, request);

// ❌ 错误: 直接创建处方（绕过聚合根）
var prescription = new Prescription { MedicalCaseId = id };
await _prescriptionRepository.AddAsync(prescription);
```

---

### AR-003: 一诊一方约束

**规则描述**: 一个MedicalCase只能关联一个Prescription。

**实施细节**:
- ✅ `CreatePrescriptionAsync`验证是否已存在Prescription
- ✅ 如需重新开方，必须先删除现有Prescription
- ❌ 禁止一个MedicalCase关联多个Active Prescription

**验证逻辑**:
```csharp
// MedicalCaseService.CreatePrescriptionAsync
var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
if (medicalCase.Prescription != null)
{
    throw new InvalidOperationException(
        "该病案已有处方，请先删除现有处方或使用更新接口");
}
```

**错误场景**:
```http
POST /api/v1/medicalcases/{id}/prescriptions
→ 422 Unprocessable Entity
{
  "error": "该病案已有处方（AR-003违规）"
}
```

---

### BF-002: 三步流程控制

**规则描述**: 病案完成必须遵循三步流程：辨证（Step 1） → 标记（Step 2） → 处方/完成（Step 3）。

**流程图**:
```
┌─────────────────┐
│ Step 1: 辨证     │  UpdateConsultationAsync()
│ (UpdateConsultation) │  → 设置 step1CompletedAt
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│ Step 2: 标记     │  SetPrescriptionFlagAsync()
│ (SetPrescriptionFlag) │  → 设置 needsPrescription + step2CompletedAt
└────────┬────────┘
         │
         ├───── needsPrescription = true ──→ Step 3a: 开处方 (CreatePrescription)
         │
         └───── needsPrescription = false ─→ Step 3b: 完成病案 (Complete)
```

**验证条件**:
```csharp
// CompleteAsync() 验证逻辑
if (medicalCase.Consultation?.Step1CompletedAt == null)
    throw new InvalidOperationException("未完成辨证（Step 1）");

if (medicalCase.Consultation?.Step2CompletedAt == null)
    throw new InvalidOperationException("未标记处方需求（Step 2）");

if (medicalCase.NeedsPrescription && medicalCase.Prescription == null)
    throw new InvalidOperationException("已标记需要处方但未开处方（Step 3）");
```

---

### BR-001: 单患者单Active病案

**规则描述**: 一个患者同一时间只能有一个Active状态的MedicalCase。

**实施细节**:
- ✅ `CreateAsync`验证患者是否已有Active病案
- ✅ 完成/取消病案后，状态变更为Completed/Cancelled，解除约束
- ❌ 禁止同一患者创建多个Active病案

**验证逻辑**:
```csharp
// MedicalCaseService.CreateAsync
var existingActiveCase = await _repository
    .GetByPatientIdAsync(patientId)
    .Where(mc => mc.Status == MedicalCaseStatus.Active)
    .FirstOrDefaultAsync();

if (existingActiveCase != null)
{
    throw new InvalidOperationException(
        $"患者{patientId}已有Active病案（BR-001违规）");
}
```

---

## 🔧 开发指南

### 如何扩展MedicalCase模块

#### 1. 添加新的业务字段

**步骤**:
1. 在`MedicalCase.cs`实体中添加新属性
2. 创建EF Core迁移：`dotnet ef migrations add AddNewField`
3. 更新DTO: `MedicalCaseDtos.cs`
4. 更新AutoMapper映射: `MedicalCaseMappingProfile.cs`
5. 更新Service接口和实现
6. 更新Controller端点
7. 更新API文档: `docs/api/medicalcase-api.md`

**示例**:
```csharp
// 1. 实体层
public class MedicalCase : BaseEntity
{
    [StringLength(200)]
    public string? SpecialNotes { get; set; } // 新增字段
}

// 2. DTO层
public class UpdateConsultationRequest
{
    public string? SpecialNotes { get; set; } // 新增字段
}

// 3. Mapping
CreateMap<UpdateConsultationRequest, MedicalCase>()
    .ForMember(dest => dest.SpecialNotes,
               opt => opt.MapFrom(src => src.SpecialNotes));

// 4. Service
public async Task<MedicalCase?> UpdateConsultationAsync(
    Guid medicalCaseId, UpdateConsultationRequest request)
{
    // ... 映射SpecialNotes
}
```

---

#### 2. 添加新的业务规则验证

**步骤**:
1. 在`MedicalCaseRules.cs`中添加验证方法
2. 在Service层调用验证
3. 更新单元测试验证覆盖
4. 更新业务规则文档: `docs/business-rules.md`

**示例**:
```csharp
// MedicalCaseRules.cs
public static class MedicalCaseRules
{
    public static void ValidateConsultationCompleteness(
        Consultation consultation)
    {
        if (string.IsNullOrWhiteSpace(consultation.ChiefComplaint))
            throw new ValidationException("主诉不能为空");

        if (string.IsNullOrWhiteSpace(consultation.TCMDiagnosis))
            throw new ValidationException("中医诊断不能为空");
    }
}

// MedicalCaseService.cs
public async Task<MedicalCase?> UpdateConsultationAsync(...)
{
    // 调用验证
    MedicalCaseRules.ValidateConsultationCompleteness(consultation);

    // 保存
    await _repository.UpdateAsync(medicalCase);
}
```

---

#### 3. 添加新的查询端点

**步骤**:
1. 在`IMedicalCaseService`接口添加方法签名
2. 在`MedicalCaseService`实现查询逻辑
3. 在`MedicalCaseRepository`添加Repository方法（如需）
4. 在`MedicalCaseController`添加API端点
5. 编写集成测试验证端点
6. 更新API文档

**示例**:
```csharp
// IMedicalCaseService.cs
Task<List<MedicalCase>> GetByDoctorAsync(
    Guid doctorId, DateTime? startDate, DateTime? endDate);

// MedicalCaseService.cs
public async Task<List<MedicalCase>> GetByDoctorAsync(
    Guid doctorId, DateTime? startDate, DateTime? endDate)
{
    return await _repository
        .GetQueryable()
        .Where(mc => mc.DoctorId == doctorId)
        .Where(mc => !startDate.HasValue || mc.ConsultationDate >= startDate)
        .Where(mc => !endDate.HasValue || mc.ConsultationDate <= endDate)
        .ToListAsync();
}

// MedicalCaseController.cs (Read Layer)
[HttpGet("doctor/{doctorId}")]
public async Task<ActionResult<List<MedicalCaseDto>>> GetByDoctor(
    Guid doctorId,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate)
{
    var cases = await _service.GetByDoctorAsync(doctorId, startDate, endDate);
    return Ok(_mapper.Map<List<MedicalCaseDto>>(cases));
}
```

---

### 常见问题（FAQ）

#### Q1: 为什么不能直接操作Consultation或Prescription？

**A**: 根据聚合根模式（AR-001），MedicalCase是聚合根，负责管理其子实体的完整生命周期。直接操作子实体会破坏聚合根的一致性保证，可能导致：
- 绕过业务规则验证（如AR-003一诊一方约束）
- 破坏事务边界
- 导致数据不一致

**正确做法**: 始终通过`MedicalCaseService`操作Consultation和Prescription。

---

#### Q2: 如何处理并发编辑冲突？

**A**: MedicalCase实体使用`RowVersion`字段实现乐观并发控制。

**处理方式**:
```csharp
try
{
    await _repository.UpdateAsync(medicalCase);
}
catch (DbUpdateConcurrencyException ex)
{
    // 方案1: 提示用户重新加载
    return Conflict("病案已被其他用户修改，请刷新后重试");

    // 方案2: 自动合并（需要业务逻辑支持）
    var databaseValues = await _context.MedicalCases
        .AsNoTracking()
        .FirstOrDefaultAsync(mc => mc.Id == medicalCase.Id);
    // 合并逻辑...
}
```

---

#### Q3: 为什么需要三步流程（BF-002）？

**A**: 三步流程实现了灵活的就诊流程控制：
- **Step 1（辨证）**: 记录诊断信息
- **Step 2（标记）**: 医生决定是否开处方（动态控制）
- **Step 3a（开处方）**: 如需开方，创建处方
- **Step 3b（完成）**: 如不开方，直接完成病案

**业务价值**:
- 支持"辨证后不开方"场景（如患者拒绝用药、建议食疗等）
- 避免强制开方导致的数据冗余
- 清晰的流程追踪（通过Step1/Step2CompletedAt时间戳）

---

#### Q4: 如何删除处方后重新开方？

**A**: 调用删除接口后，再调用创建接口。

**API调用示例**:
```http
# 1. 删除现有处方
DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}
→ 204 No Content

# 2. 重新创建处方
POST /api/v1/medicalcases/{id}/prescriptions
Content-Type: application/json
{
  "indication": "风寒感冒",
  "dosageCount": 7,
  "items": [...]
}
→ 200 OK
```

**注意**: AR-003约束确保同一时间只有一个处方，删除后才能重新创建。

---

#### Q5: GetByIdAsync为什么要预加载Consultation和Prescription？

**A**: 性能优化和N+1查询避免。

**问题场景**:
```csharp
// ❌ 错误: 导致N+1查询
var medicalCase = await _repository.GetByIdAsync(id);
var consultation = medicalCase.Consultation; // 触发第二次查询
var prescription = medicalCase.Prescription; // 触发第三次查询
```

**优化方案**:
```csharp
// ✅ 正确: 一次查询获取所有数据
var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
// Consultation和Prescription已预加载，无额外查询
```

**Repository实现**:
```csharp
public async Task<MedicalCase?> GetByIdWithDetailsAsync(Guid id)
{
    return await _context.MedicalCases
        .Include(mc => mc.Consultation)
        .Include(mc => mc.Prescription)
            .ThenInclude(p => p.PrescriptionItems)
        .FirstOrDefaultAsync(mc => mc.Id == id);
}
```

---

#### 4. Desktop端Repository使用指南（Epic #1676 Phase 4）

**架构模式**: Desktop端直接使用`IMedicalCaseRepository`，不经过Service层。

**核心组件**:
- `IMedicalCaseRepository.cs` - Repository接口定义（Desktop/Modules/MedicalCase/Interfaces）
- `MedicalCaseRepository.cs` - Repository实现（Desktop/Modules/MedicalCase/Repositories）
- Refit HTTP Client - 类型安全的REST API调用

**使用示例**:
```csharp
// ViewModel中注入Repository
public class PatientSelectionViewModel : BindableBase
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    public PatientSelectionViewModel(
        IPatientRepository patientRepository,
        IMedicalCaseRepository medicalCaseRepository)  // ⭐ 注入Repository
    {
        _patientRepository = patientRepository;
        _medicalCaseRepository = medicalCaseRepository;
    }

    // 查询未完成病案（Epic #1676 Phase 4新增）
    private async Task CheckUnfinishedCaseAsync(Guid patientId)
    {
        var unfinishedCase = await _medicalCaseRepository
            .GetUnfinishedCaseByPatientIdAsync(patientId);

        if (unfinishedCase != null)
        {
            // 提示用户有未完成的病案
            ShowUnfinishedCaseDialog(unfinishedCase);
        }
    }

    // 关闭病案（Epic #1676 Phase 4新增）
    private async Task CloseCaseAsync(Guid medicalCaseId)
    {
        var success = await _medicalCaseRepository.CloseCaseAsync(medicalCaseId);
        if (success)
        {
            MessageBox.Show("病案已关闭");
        }
    }
}
```

**Phase 4新增API**（2025-10-28）:
- `GetUnfinishedCaseByPatientIdAsync(Guid patientId)` - 查询患者未完成病案
- `CloseCaseAsync(Guid medicalCaseId)` - 关闭病案（直接标记为Completed）

**架构约束**:
- ✅ **统一Repository模式**: 所有ViewModel统一使用Repository，无例外
- ❌ **禁止临时Service**: 不再创建中间Service层（如MedicalCaseQueryService已移除）
- ✅ **API能力对齐**: Desktop端Repository方法完全对应Server端API端点

---

## 🧪 测试覆盖

**详细测试报告**: [`docs/deep/testing-strategies.md#模块测试覆盖报告`](../../deep/testing-strategies.md#模块测试覆盖报告)

### 单元测试

- **测试文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseServiceTests.cs`
- **测试数量**: 32个测试
- **行覆盖率**: 82.6%
- **分支覆盖率**: 57.14%
- **通过率**: 100%

### 集成测试

- **测试文件**: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs`
- **测试数量**: 18个测试
- **通过率**: 100%
- **覆盖范围**: 14个API端点全覆盖

### E2E测试

- **场景数量**: 4个完整业务流程
- **通过率**: 100%
- **测试方式**: WebAPI集成测试（符合MVVM架构）

---

## 📚 相关文档

### 核心文档
- **API参考**: [`docs/api/medicalcase-api.md`](../../api/medicalcase-api.md) - 14个API端点完整文档
- **架构指南**: [`docs/architecture/server/README.md`](../../architecture/server/README.md) - Server端三层架构
- **业务规则**: [`docs/business-rules.md`](../../business-rules.md) - 14条核心业务规则
- **测试策略**: [`docs/deep/testing-strategies.md`](../../deep/testing-strategies.md) - 测试金字塔和覆盖报告

### 快速参考
- **代码模式**: [`docs/quick-reference/code-patterns.md`](../../quick-reference/code-patterns.md) - Service/Repository模式示例
- **API速查**: [`docs/quick-reference/api-reference.md`](../../quick-reference/api-reference.md) - MedicalCase API速查表

### 报告文档
- **文档同步清单**: [`docs/reports/epic-1612-doc-sync-checklist.md`](../../reports/epic-1612-doc-sync-checklist.md)
- **E2E测试报告**: [`docs/reports/e2e-test-coverage-analysis.md`](../../reports/e2e-test-coverage-analysis.md)

---

## 🔄 变更历史

### v2.1 (Epic #1676 Phase 4) - 2025-10-28

**Desktop层架构优化**:
- ✅ Desktop端Repository模式统一化
- ✅ 移除临时Service层（MedicalCaseQueryService）
- ✅ Desktop ↔ Server API完全对齐
- ✅ 解除Patients ↔ MedicalCase循环依赖

**新增Desktop API** (IMedicalCaseRepository):
- ✅ `GetUnfinishedCaseByPatientIdAsync(Guid patientId)` - 查询未完成病案
- ✅ `CloseCaseAsync(Guid medicalCaseId)` - 关闭病案

**新增Server API** (IMedicalCaseApi):
- ✅ `GET /api/v1/medicalcases/patients/{patientId}/unfinished` - 查询未完成病案
- ✅ `PUT /api/v1/medicalcases/{id}/close` - 关闭病案

**测试覆盖**:
- ✅ 6个新增单元测试（Desktop Repository）
- ✅ 100% API端点测试覆盖

**文档更新**:
- ✅ Desktop端Repository使用指南（本README）
- ✅ Desktop架构文档更新（docs/architecture/client）

---

### v2.0 (Epic #1612) - 2025-10-27

**重构内容**:
- ✅ 三层对齐架构重构
- ✅ Controller Write/Read/Helper Layer分离
- ✅ Service层14个方法实现
- ✅ Repository层预加载优化
- ✅ 32个单元测试 + 18个集成测试
- ✅ 完整API文档和架构文档

**业务规则强化**:
- ✅ AR-001: 聚合根管理
- ✅ AR-003: 一诊一方约束
- ✅ BF-002: 三步流程控制
- ✅ BR-001: 单患者单Active病案

**文档完善**:
- ✅ API文档: 1,000+行完整参考
- ✅ 架构文档: Service/Repository详述
- ✅ 测试文档: 模块测试覆盖报告
- ✅ 模块文档: 本README（开发指南 + FAQ）

---

**维护团队**: Epic #1612开发团队 / Epic #1676开发团队
**最后更新**: 2025-10-28
**下次审查**: Epic #1676全部完成后
