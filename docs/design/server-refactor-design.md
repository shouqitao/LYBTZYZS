# Server端重构设计文档

**创建时间**: 2025-10-27
**版本**: v2.0
**对应需求**: `docs/requirements/server-refactor-requirements-v2.md`
**设计原则**: 有需求才有代码，不过度设计，不反复定义

---

## 📋 设计概览

### 总体目标
1. 删除所有超前设计的代码（~500行）
2. 补充PrescriptionsController的只读端点（~150行）
3. 强化聚合根约束（Repository改为internal）
4. 净删除代码：~350行

### 实施阶段
- **Phase 1**: 删除超前设计（1-2小时）
- **Phase 2**: 新增PrescriptionsController端点（2-3小时）
- **Phase 3**: Repository改为internal（0.5小时）

### 架构合规性
- ✅ 所有读操作可独立查询（符合AR-001）
- ✅ 所有写操作通过MedicalCase聚合根（符合AR-001）
- ✅ Repository不可被Controller直接访问（强制约束）

---

## Phase 1: 删除超前设计代码

### 1.1 删除PrescriptionService中的超前设计方法

#### 文件位置
`src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

#### 删除清单（6个方法）

| 方法名 | 行号 | 删除原因 | 代码量 |
|-------|------|---------|--------|
| `GetPagedAsync` | 55-98 | 无分页查询需求 | ~44行 |
| `RecalculatePriceAsync` | 169-194 | 无价格重算需求 | ~26行 |
| `GeneratePrintFormatAsync` | 201-217 | 无打印功能需求 | ~17行 |
| `GeneratePrescriptionNoAsync` | 278-303 | 无处方号生成需求 | ~26行 |
| `GetStatisticsAsync` | 308-347 | 无统计功能需求 | ~40行 |
| `GetRangeStatisticsAsync` | 352-397 | 无范围统计需求 | ~46行 |

**合计删除**: ~199行

#### 删除步骤

**Step 1**: 删除方法实现

```csharp
// 删除以下方法（Line 55-98）
public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(...)
{
    // 完整删除
}

// 删除以下方法（Line 169-194）
public async Task<ServiceResult<PrescriptionDto>> RecalculatePriceAsync(...)
{
    // 完整删除
}

// ... 其他4个方法同样删除
```

**Step 2**: 同步删除接口定义

**文件位置**: `src/Server/Interfaces/Services/IPrescriptionService.cs`

```csharp
// 删除以下接口方法签名
Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(
    int page = 1, int pageSize = 20, string? keyword = null,
    DateTime? startDate = null, DateTime? endDate = null);

Task<ServiceResult<PrescriptionDto>> RecalculatePriceAsync(Guid prescriptionId);

Task<ServiceResult<string>> GeneratePrintFormatAsync(Guid prescriptionId);

Task<ServiceResult<string>> GeneratePrescriptionNoAsync();

Task<ServiceResult<PrescriptionMainStatisticsDto>> GetStatisticsAsync();

Task<ServiceResult<PrescriptionRangeStatisticsDto>> GetRangeStatisticsAsync(
    DateTime startDate, DateTime endDate);
```

**Step 3**: 检查调用方（预期为0）

```bash
# 使用grep检查是否有调用
grep -r "GetPagedAsync\|RecalculatePriceAsync\|GeneratePrintFormatAsync\|GeneratePrescriptionNoAsync\|GetStatisticsAsync\|GetRangeStatisticsAsync" src/
```

**预期结果**: 无调用方（如有调用方，一并删除）

#### 验收标准
- ✅ PrescriptionService.cs删除6个方法
- ✅ IPrescriptionService.cs删除6个接口签名
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 无遗留调用

---

### 1.2 删除ConsultationService中的超前设计方法

#### 文件位置
`src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationService.cs`

#### 删除清单（2个方法）

| 方法名 | 行号 | 删除原因 | 代码量 |
|-------|------|---------|--------|
| `GetPagedAsync` | 32-62 | 无分页查询需求 | ~31行 |
| `SearchAsync` | 116-132 | 无搜索功能需求 | ~17行 |

**合计删除**: ~48行

#### 删除步骤

**Step 1**: 删除方法实现

```csharp
// 删除以下方法（Line 32-62）
public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(...)
{
    // 完整删除
}

// 删除以下方法（Line 116-132）
public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(...)
{
    // 完整删除
}
```

**Step 2**: 同步删除接口定义

**文件位置**: `src/Server/Interfaces/Services/IConsultationService.cs`

```csharp
// 删除以下接口方法签名
Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(
    int page = 1, int pageSize = 20, string? keyword = null);

Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);
```

#### 验收标准
- ✅ ConsultationService.cs删除2个方法
- ✅ IConsultationService.cs删除2个接口签名
- ✅ 编译通过（0 errors, 0 warnings）

---

### 1.3 删除ConsultationController中的超前设计端点

#### 文件位置
`src/Server/Services/LYBT.WebAPI/Controllers/ConsultationController.cs`

#### 删除清单（2个端点）

| 端点路径 | 方法名 | 行号 | 删除原因 | 代码量 |
|---------|-------|------|---------|--------|
| `GET /consultations` | GetConsultations | 38-54 | 无分页查询需求 | ~17行 |
| `GET /consultations/search` | Search | 118-136 | 无搜索功能需求 | ~19行 |

**合计删除**: ~36行

#### 删除步骤

**Step 1**: 删除端点方法

```csharp
// 删除以下端点（Line 38-54）
[HttpGet]
[ProducesResponseType(typeof(ApiResponse<PagedResult<ConsultationDto>>), 200)]
public async Task<ActionResult<ApiResponse<PagedResult<ConsultationDto>>>> GetConsultations(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? keyword = null)
{
    // 完整删除
}

// 删除以下端点（Line 118-136）
[HttpGet("search")]
[ProducesResponseType(typeof(ApiResponse<List<ConsultationDto>>), 200)]
public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> Search(
    [FromQuery] string keyword)
{
    // 完整删除
}
```

**Step 2**: 删除相关注释

删除以下注释行：
- Line 80-84: "Issue #1562 Phase 4: 已删除 CreateConsultation..."
- Line 110-111: "Write方法已移除（Issue #1600 Phase 4）..."

保留的端点：
- ✅ `GET /consultations/{id}` - GetById
- ✅ `GET /consultations/medicalcase/{medicalCaseId}` - GetByMedicalCaseId

#### 验收标准
- ✅ ConsultationController.cs删除2个端点方法
- ✅ 只保留2个只读端点
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ Swagger文档更新（移除2个端点）

---

### 1.4 Phase 1汇总

**删除代码总量**: ~283行（199 + 48 + 36）

**删除文件清单**:
1. `PrescriptionService.cs` - 删除6个方法（~199行）
2. `IPrescriptionService.cs` - 删除6个接口签名（~20行）
3. `ConsultationService.cs` - 删除2个方法（~48行）
4. `IConsultationService.cs` - 删除2个接口签名（~8行）
5. `ConsultationController.cs` - 删除2个端点（~36行）

**总编译验证**:
```bash
dotnet build LYBT.All.sln -c Release --no-restore
# 预期：0 errors, 0 warnings
```

---

## Phase 2: 新增PrescriptionsController端点

### 2.1 设计概览

#### 新增端点清单（4个）

| 端点路径 | HTTP方法 | 功能描述 | 对应需求 |
|---------|---------|---------|---------|
| `/api/v1/prescriptions/{id}` | GET | 获取处方详情（含药材明细） | 隐含需求 |
| `/api/v1/prescriptions/medicalcase/{medicalCaseId}` | GET | 查看病案的处方列表 | 隐含需求 |
| `/api/v1/prescriptions/search` | GET | 按病症/患者搜索处方 | **REQ-2** |
| `/api/v1/prescriptions/patient/{patientId}/recent` | GET | 获取患者最近处方 | **REQ-1** |

#### 参考模板
- ConsultationController的2个保留端点（GetById、GetByMedicalCaseId）
- 遵循相同的代码模式和错误处理

---

### 2.2 端点详细设计

#### 端点1: 获取处方详情

**路由**: `GET /api/v1/prescriptions/{id}`

**方法签名**:
```csharp
/// <summary>
/// 获取处方详情
/// </summary>
/// <param name="id">处方ID</param>
/// <returns>处方详情（含药材明细）</returns>
[HttpGet("{id}")]
[ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
[ProducesResponseType(404)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetById(Guid id)
```

**请求参数**:
- `id`: 处方ID（Guid，路由参数）

**返回值**:
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "uuid",
    "medicalCaseId": "uuid",
    "indication": "感冒发热，咳嗽痰多",
    "dosageCount": 7,
    "advice": "每日一剂，水煎服",
    "items": [
      {
        "herbId": "uuid",
        "herbName": "麻黄",
        "dosage": 10.0,
        "unit": "g"
      }
    ],
    "createdAt": "2025-10-27T10:00:00Z"
  }
}
```

**实现代码**:
```csharp
[HttpGet("{id}")]
[ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
[ProducesResponseType(404)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetById(Guid id)
{
    try
    {
        var validationResult = ValidateGuid<PrescriptionDto>(id, "处方ID");
        if (validationResult != null) return validationResult;

        var result = await _prescriptionService.GetByIdAsync(id);
        return HandleServiceResult(result);
    }
    catch (Exception ex)
    {
        return HandleException<PrescriptionDto>(ex, "获取处方详情", new { PrescriptionId = id });
    }
}
```

**错误处理**:
- 400: ID格式错误（Guid.Empty）
- 404: 处方不存在
- 500: 内部错误

---

#### 端点2: 查看病案的处方列表

**路由**: `GET /api/v1/prescriptions/medicalcase/{medicalCaseId}`

**方法签名**:
```csharp
/// <summary>
/// 根据病案ID获取处方列表
/// </summary>
/// <param name="medicalCaseId">病案ID</param>
/// <returns>处方列表</returns>
[HttpGet("medicalcase/{medicalCaseId}")]
[ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), 200)]
[ProducesResponseType(404)]
public async Task<ActionResult<ApiResponse<List<PrescriptionDto>>>> GetByMedicalCaseId(
    Guid medicalCaseId)
```

**请求参数**:
- `medicalCaseId`: 病案ID（Guid，路由参数）

**返回值**:
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "id": "uuid",
      "medicalCaseId": "uuid",
      "indication": "感冒发热",
      "dosageCount": 7,
      "items": [...]
    }
  ]
}
```

**实现代码**:
```csharp
[HttpGet("medicalcase/{medicalCaseId}")]
[ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), 200)]
[ProducesResponseType(404)]
public async Task<ActionResult<ApiResponse<List<PrescriptionDto>>>> GetByMedicalCaseId(
    Guid medicalCaseId)
{
    try
    {
        var validationResult = ValidateGuid<List<PrescriptionDto>>(medicalCaseId, "病案ID");
        if (validationResult != null) return validationResult;

        var result = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);
        return HandleServiceResult(result);
    }
    catch (Exception ex)
    {
        return HandleException<List<PrescriptionDto>>(ex, "根据病案ID获取处方",
            new { MedicalCaseId = medicalCaseId });
    }
}
```

---

#### 端点3: 按病症/患者搜索处方（REQ-2）

**路由**: `GET /api/v1/prescriptions/search`

**方法签名**:
```csharp
/// <summary>
/// 按病症/患者搜索处方
/// </summary>
/// <param name="patientName">患者姓名（可选）</param>
/// <param name="symptomKeyword">病症关键词（可选）</param>
/// <returns>匹配的处方列表</returns>
[HttpGet("search")]
[ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> Search(
    [FromQuery] string? patientName = null,
    [FromQuery] string? symptomKeyword = null)
```

**请求参数**:
- `patientName`: 患者姓名（可选，模糊匹配）
- `symptomKeyword`: 病症关键词（可选，匹配诊断和主诉）

**请求示例**:
```
GET /api/v1/prescriptions/search?symptomKeyword=感冒
GET /api/v1/prescriptions/search?patientName=张三
GET /api/v1/prescriptions/search?patientName=张三&symptomKeyword=头痛
```

**返回值**:
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "id": "uuid",
      "patientId": "uuid",
      "patientName": "张三",
      "indication": "感冒发热，咳嗽痰多",
      "tcmDiagnosis": "风寒感冒",
      "dosageCount": 7,
      "createdAt": "2025-10-27T10:00:00Z"
    }
  ]
}
```

**实现代码**:
```csharp
[HttpGet("search")]
[ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> Search(
    [FromQuery] string? patientName = null,
    [FromQuery] string? symptomKeyword = null)
{
    try
    {
        // 参数验证：至少提供一个搜索条件
        if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptomKeyword))
        {
            return BadRequest(ApiResponse<List<PrescriptionSearchResultDto>>.CreateFail(
                "请至少提供一个搜索条件（患者姓名或病症关键词）"));
        }

        var result = await _prescriptionService.SearchPrescriptionsAsync(
            patientName, symptomKeyword);
        return HandleServiceResult(result);
    }
    catch (Exception ex)
    {
        return HandleException<List<PrescriptionSearchResultDto>>(ex, "搜索处方",
            new { PatientName = patientName, SymptomKeyword = symptomKeyword });
    }
}
```

**业务逻辑**（在PrescriptionService.SearchPrescriptionsAsync中）:
- 搜索范围：`Consultation.TCMDiagnosis` + `Prescription.Indication`
- 支持模糊匹配（Contains）
- 支持组合搜索（患者 AND 病症）

---

#### 端点4: 获取患者最近处方（REQ-1）

**路由**: `GET /api/v1/prescriptions/patient/{patientId}/recent`

**方法签名**:
```csharp
/// <summary>
/// 获取患者最近处方
/// </summary>
/// <param name="patientId">患者ID</param>
/// <param name="count">返回数量（默认5条，最大20条）</param>
/// <returns>患者最近处方列表（按日期倒序）</returns>
[HttpGet("patient/{patientId}/recent")]
[ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
[ProducesResponseType(404)]
public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> GetRecentByPatient(
    Guid patientId,
    [FromQuery] int count = 5)
```

**请求参数**:
- `patientId`: 患者ID（Guid，路由参数）
- `count`: 返回数量（默认5，最大20）

**请求示例**:
```
GET /api/v1/prescriptions/patient/{patientId}/recent
GET /api/v1/prescriptions/patient/{patientId}/recent?count=10
```

**返回值**:
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "id": "uuid",
      "patientId": "uuid",
      "patientName": "张三",
      "indication": "复诊：感冒症状缓解",
      "tcmDiagnosis": "风寒感冒（恢复期）",
      "dosageCount": 3,
      "herbCount": 8,
      "items": [...],
      "createdAt": "2025-10-25T10:00:00Z"
    },
    {
      "id": "uuid",
      "patientId": "uuid",
      "patientName": "张三",
      "indication": "初诊：感冒发热",
      "tcmDiagnosis": "风寒感冒",
      "dosageCount": 7,
      "herbCount": 10,
      "items": [...],
      "createdAt": "2025-10-20T10:00:00Z"
    }
  ]
}
```

**实现代码**:
```csharp
[HttpGet("patient/{patientId}/recent")]
[ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
[ProducesResponseType(404)]
public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> GetRecentByPatient(
    Guid patientId,
    [FromQuery] int count = 5)
{
    try
    {
        var validationResult = ValidateGuid<List<PrescriptionSearchResultDto>>(patientId, "患者ID");
        if (validationResult != null) return validationResult;

        // 参数验证：count范围1-20
        if (count < 1 || count > 20)
        {
            return BadRequest(ApiResponse<List<PrescriptionSearchResultDto>>.CreateFail(
                "返回数量必须在1-20之间"));
        }

        var result = await _prescriptionService.GetPatientRecentPrescriptionsAsync(
            patientId, count);
        return HandleServiceResult(result);
    }
    catch (Exception ex)
    {
        return HandleException<List<PrescriptionSearchResultDto>>(ex, "获取患者最近处方",
            new { PatientId = patientId, Count = count });
    }
}
```

**业务逻辑**（在PrescriptionService.GetPatientRecentPrescriptionsAsync中）:
- 查询患者所有处方
- 按`CreatedAt`倒序排列
- 取前N条（count参数）
- 包含药材明细（`Items`字段）

---

### 2.3 Controller完整代码

**文件位置**: `src/Server/Services/LYBT.WebAPI/Controllers/PrescriptionsController.cs`

**完整实现**:
```csharp
using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 处方管理控制器 - Read Layer
    /// 职责：提供处方记录的只读查询功能
    /// 所有Write操作请使用MedicalCaseController
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/prescriptions")]
    [Authorize]
    public class PrescriptionsController : BaseApiController
    {
        private readonly IPrescriptionService _prescriptionService;

        public PrescriptionsController(
            IPrescriptionService prescriptionService,
            ILogger<PrescriptionsController> logger,
            IMemoryCache? cache = null)
            : base(logger, cache)
        {
            _prescriptionService = prescriptionService ??
                throw new ArgumentNullException(nameof(prescriptionService));
        }

        /// <summary>
        /// 获取处方详情
        /// </summary>
        /// <param name="id">处方ID</param>
        /// <returns>处方详情（含药材明细）</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetById(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<PrescriptionDto>(id, "处方ID");
                if (validationResult != null) return validationResult;

                var result = await _prescriptionService.GetByIdAsync(id);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "获取处方详情",
                    new { PrescriptionId = id });
            }
        }

        /// <summary>
        /// 根据病案ID获取处方列表
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>处方列表</returns>
        [HttpGet("medicalcase/{medicalCaseId}")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<List<PrescriptionDto>>>> GetByMedicalCaseId(
            Guid medicalCaseId)
        {
            try
            {
                var validationResult = ValidateGuid<List<PrescriptionDto>>(
                    medicalCaseId, "病案ID");
                if (validationResult != null) return validationResult;

                var result = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionDto>>(ex, "根据病案ID获取处方",
                    new { MedicalCaseId = medicalCaseId });
            }
        }

        /// <summary>
        /// 按病症/患者搜索处方（REQ-2）
        /// </summary>
        /// <param name="patientName">患者姓名（可选）</param>
        /// <param name="symptomKeyword">病症关键词（可选）</param>
        /// <returns>匹配的处方列表</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> Search(
            [FromQuery] string? patientName = null,
            [FromQuery] string? symptomKeyword = null)
        {
            try
            {
                // 参数验证：至少提供一个搜索条件
                if (string.IsNullOrWhiteSpace(patientName) &&
                    string.IsNullOrWhiteSpace(symptomKeyword))
                {
                    return BadRequest(ApiResponse<List<PrescriptionSearchResultDto>>.CreateFail(
                        "请至少提供一个搜索条件（患者姓名或病症关键词）"));
                }

                var result = await _prescriptionService.SearchPrescriptionsAsync(
                    patientName, symptomKeyword);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionSearchResultDto>>(ex, "搜索处方",
                    new { PatientName = patientName, SymptomKeyword = symptomKeyword });
            }
        }

        /// <summary>
        /// 获取患者最近处方（REQ-1）
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="count">返回数量（默认5条，最大20条）</param>
        /// <returns>患者最近处方列表（按日期倒序）</returns>
        [HttpGet("patient/{patientId}/recent")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>>
            GetRecentByPatient(
                Guid patientId,
                [FromQuery] int count = 5)
        {
            try
            {
                var validationResult = ValidateGuid<List<PrescriptionSearchResultDto>>(
                    patientId, "患者ID");
                if (validationResult != null) return validationResult;

                // 参数验证：count范围1-20
                if (count < 1 || count > 20)
                {
                    return BadRequest(
                        ApiResponse<List<PrescriptionSearchResultDto>>.CreateFail(
                            "返回数量必须在1-20之间"));
                }

                var result = await _prescriptionService.GetPatientRecentPrescriptionsAsync(
                    patientId, count);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionSearchResultDto>>(ex,
                    "获取患者最近处方",
                    new { PatientId = patientId, Count = count });
            }
        }
    }
}
```

**代码行数**: ~150行

---

### 2.4 Phase 2验收标准

#### 编译验证
```bash
dotnet build LYBT.All.sln -c Release --no-restore
# 预期：0 errors, 0 warnings
```

#### 运行时验证

**启动WebAPI**:
```bash
cd src/Server/Services/LYBT.WebAPI
dotnet run
```

**测试端点**:

1. **测试GetById**:
```bash
curl -X GET "https://localhost:5001/api/v1/prescriptions/{id}" \
  -H "Authorization: Bearer {token}"
```

2. **测试GetByMedicalCaseId**:
```bash
curl -X GET "https://localhost:5001/api/v1/prescriptions/medicalcase/{medicalCaseId}" \
  -H "Authorization: Bearer {token}"
```

3. **测试Search（REQ-2）**:
```bash
curl -X GET "https://localhost:5001/api/v1/prescriptions/search?symptomKeyword=感冒" \
  -H "Authorization: Bearer {token}"
```

4. **测试GetRecentByPatient（REQ-1）**:
```bash
curl -X GET "https://localhost:5001/api/v1/prescriptions/patient/{patientId}/recent?count=5" \
  -H "Authorization: Bearer {token}"
```

#### Swagger验证
- 访问 `https://localhost:5001/swagger`
- 验证4个新端点出现在Swagger UI中
- 验证端点文档完整（参数、返回值、错误码）

---

## Phase 3: Repository改为internal

### 3.1 设计目标

**强化聚合根约束**:
- ✅ 防止Controller直接访问Repository
- ✅ 强制所有读操作通过Service
- ✅ 强制所有写操作通过MedicalCase聚合根

### 3.2 修改清单

#### Repository类清单（9个）

| Repository类 | 文件位置 | 当前可见性 | 修改后 |
|-------------|---------|-----------|--------|
| `ConsultationRepository` | `LYBT.Module.Consultation/Repositories/` | `public` | `internal` |
| `PrescriptionRepository` | `LYBT.Module.Prescriptions/Repositories/` | `public` | `internal` |
| `MedicalCaseRepository` | `LYBT.Module.MedicalCase/Repositories/` | `public` | `internal` |
| `PatientRepository` | `LYBT.Module.Patients/Repositories/` | `public` | `internal` |
| `UserRepository` | `LYBT.Module.Users/Repositories/` | `public` | `internal` |
| `HerbRepository` | `LYBT.Module.Herbs/Repositories/` | `public` | `internal` |
| `FormulaRepository` | `LYBT.Module.Formula/Repositories/` | `public` | `internal` |
| `AuthRepository` | `LYBT.Module.Auth/Repositories/` | `public` | `internal` |
| `PrescriptionItemRepository` | `LYBT.Module.Prescriptions/Repositories/` | `public` | `internal` |

#### 修改步骤

**Step 1**: 修改Repository类可见性

**示例**（ConsultationRepository）:

**修改前**:
```csharp
namespace LYBT.Module.Consultation.Repositories
{
    public class ConsultationRepository : BaseRepository<ConsultationEntity>, IConsultationRepository
    {
        // ...
    }
}
```

**修改后**:
```csharp
namespace LYBT.Module.Consultation.Repositories
{
    internal class ConsultationRepository : BaseRepository<ConsultationEntity>, IConsultationRepository
    {
        // ...
    }
}
```

**Step 2**: 保持接口可见性为public

**重要**: 接口必须保持`public`，因为DI需要在模块外部注册

**示例**（IConsultationRepository）:
```csharp
namespace LYBT.Module.Consultation.Interfaces
{
    public interface IConsultationRepository : IRepository<ConsultationEntity>
    {
        // ...
    }
}
```

**Step 3**: 批量修改脚本（可选）

```bash
# 查找所有Repository类
find src/Server/Modules -name "*Repository.cs" | grep -v "Interface"

# 批量替换（谨慎使用，建议手动逐个修改）
# sed -i 's/public class \([A-Z][a-zA-Z]*Repository\)/internal class \1/g' <文件路径>
```

---

### 3.3 影响分析

#### ✅ 不受影响的场景（正常使用）

1. **Controller通过Service访问Repository**:
```csharp
// ✅ 正常工作
public class PrescriptionsController
{
    private readonly IPrescriptionService _service; // Service注入

    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id); // Service调用Repository
        // ...
    }
}
```

2. **Service通过DI注入Repository**:
```csharp
// ✅ 正常工作
public class PrescriptionService : IPrescriptionService
{
    private readonly IPrescriptionRepository _repository; // 接口注入

    public PrescriptionService(IPrescriptionRepository repository)
    {
        _repository = repository; // DI容器解析internal实现
    }
}
```

#### ❌ 被阻止的场景（违反架构）

1. **Controller直接访问Repository**:
```csharp
// ❌ 编译错误
public class PrescriptionsController
{
    private readonly ConsultationRepository _repository; // 编译失败：internal类型不可访问
}
```

2. **跨模块直接使用Repository**:
```csharp
// ❌ 编译错误
namespace LYBT.Module.OtherModule
{
    public class SomeService
    {
        private readonly ConsultationRepository _repo; // 编译失败：internal类型不可访问
    }
}
```

---

### 3.4 DI注册验证

**文件位置**: `src/Server/Modules/LYBT.Module.*/ServiceExtensions.cs`

**验证DI注册正确**:

```csharp
// 示例：ConsultationModule的DI注册
public static class ServiceExtensions
{
    public static IServiceCollection AddConsultationModule(this IServiceCollection services)
    {
        // ✅ 正确：注册接口和internal实现
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IConsultationService, ConsultationService>();

        return services;
    }
}
```

**预期行为**:
- ✅ DI容器可以解析`IConsultationRepository`接口
- ✅ DI容器返回`ConsultationRepository` internal实现
- ✅ Controller/Service可以注入接口，无法直接使用类

---

### 3.5 Phase 3验收标准

#### 编译验证
```bash
dotnet build LYBT.All.sln -c Release --no-restore
# 预期：0 errors, 0 warnings
```

#### 强制约束验证

**测试案例**：尝试在Controller中直接访问Repository

**文件位置**: `src/Server/Services/LYBT.WebAPI/Controllers/TestController.cs`（临时创建）

```csharp
using LYBT.Module.Consultation.Repositories;

public class TestController
{
    // ❌ 应编译失败
    private readonly ConsultationRepository _repo;

    public TestController(ConsultationRepository repo)
    {
        _repo = repo;
    }
}
```

**预期结果**: 编译错误

```
error CS0122: 'ConsultationRepository' is inaccessible due to its protection level
```

**验证成功后删除TestController**

---

## 前端实现指引

### REQ-3: 历史处方复制到当前处方

#### 前端ViewModel实现建议

**场景**: 用户在"开处方"界面，点击"从历史处方复制"按钮

**实现流程**:

```csharp
// PrescriptionFormViewModel.cs（伪代码）
public class PrescriptionFormViewModel : BindableBase
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IMedicalCaseService _medicalCaseService;

    // 当前病案ID
    public Guid CurrentMedicalCaseId { get; set; }

    // 表单数据
    public string Indication { get; set; }
    public int DosageCount { get; set; }
    public string Advice { get; set; }
    public ObservableCollection<PrescriptionItemViewModel> Items { get; set; }

    // Step 1: 显示患者历史处方列表
    public async Task ShowHistoryPrescriptionsAsync()
    {
        // 获取当前患者ID
        var currentCase = await _medicalCaseService.GetByIdAsync(CurrentMedicalCaseId);
        var patientId = currentCase.PatientId;

        // 调用API获取历史处方
        var historyPrescriptions = await _prescriptionService
            .GetPatientRecentPrescriptionsAsync(patientId, count: 10);

        // 显示在弹窗或侧边栏
        ShowHistoryListDialog(historyPrescriptions);
    }

    // Step 2: 用户选择某个历史处方
    public async Task LoadFromHistoryAsync(Guid historyPrescriptionId)
    {
        // 调用API获取历史处方详情（含药材明细）
        var history = await _prescriptionService.GetByIdAsync(historyPrescriptionId);

        // 映射到当前表单（用户可编辑）
        this.Indication = history.Indication;
        this.DosageCount = history.DosageCount;
        this.Advice = history.Advice;

        // 复制药材明细（ObservableCollection）
        this.Items.Clear();
        foreach (var item in history.Items)
        {
            this.Items.Add(new PrescriptionItemViewModel
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Dosage = item.Dosage,
                Unit = item.Unit,
                IsEditable = true // 允许用户修改
            });
        }

        // 关闭历史列表弹窗
        CloseHistoryListDialog();
    }

    // Step 3: 用户在UI上调整后保存
    public async Task SavePrescriptionAsync()
    {
        var request = new CreatePrescriptionRequest
        {
            Indication = this.Indication,
            DosageCount = this.DosageCount,
            Advice = this.Advice,
            Items = this.Items.Select(i => new PrescriptionItemDto
            {
                HerbId = i.HerbId,
                Dosage = i.Dosage,
                Unit = i.Unit
            }).ToList()
        };

        // 调用现有API保存
        await _medicalCaseService.CreatePrescriptionAsync(CurrentMedicalCaseId, request);

        // 保存成功，刷新界面
        ShowSuccessMessage("处方保存成功");
    }
}
```

#### 涉及的API调用

| 步骤 | API端点 | 用途 |
|-----|---------|------|
| 1 | `GET /prescriptions/patient/{id}/recent?count=10` | 获取历史处方列表 |
| 2 | `GET /prescriptions/{id}` | 获取历史处方详情（含药材明细） |
| 3 | `POST /medicalcases/{id}/prescriptions` | 保存新处方 |

#### UI交互流程

```
[开处方界面]
   ↓
[点击"从历史处方复制"按钮]
   ↓
[弹窗显示患者历史处方列表] ← API: GET /prescriptions/patient/{id}/recent
   ↓
[用户选择某个历史处方]
   ↓
[ViewModel加载历史数据到表单] ← API: GET /prescriptions/{id}
   ↓
[用户在表单中调整数据]
   - 修改剂数
   - 增减药材
   - 调整用量
   ↓
[点击"保存"按钮]
   ↓
[调用API保存新处方] ← API: POST /medicalcases/{id}/prescriptions
   ↓
[保存成功，关闭表单]
```

---

### REQ-4: 历史处方转存成验方

#### 前端ViewModel实现建议

**场景**: 用户在"验方库"界面，点击"从处方创建验方"按钮

**实现流程**:

```csharp
// FormulaFormViewModel.cs（伪代码）
public class FormulaFormViewModel : BindableBase
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IFormulaService _formulaService;

    // 表单数据
    public string FormulaName { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public ObservableCollection<FormulaItemViewModel> Items { get; set; }

    // Step 1: 显示患者历史处方列表（同REQ-3）
    public async Task ShowHistoryPrescriptionsAsync(Guid patientId)
    {
        var historyPrescriptions = await _prescriptionService
            .GetPatientRecentPrescriptionsAsync(patientId, count: 10);

        ShowHistoryListDialog(historyPrescriptions);
    }

    // Step 2: 用户选择某个历史处方
    public async Task LoadFromPrescriptionAsync(Guid prescriptionId)
    {
        // 获取历史处方详情
        var prescription = await _prescriptionService.GetByIdAsync(prescriptionId);

        // 映射到验方表单（用户可编辑）
        this.FormulaName = $"{prescription.Indication}方"; // 自动生成验方名称
        this.Category = "经验方"; // 默认分类
        this.Description = $"原处方主诉：{prescription.Indication}\n" +
                          $"原中医诊断：{prescription.TCMDiagnosis}\n" +
                          $"疗效：（待用户补充）";

        // 复制药材配方
        this.Items.Clear();
        foreach (var item in prescription.Items)
        {
            this.Items.Add(new FormulaItemViewModel
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Dosage = item.Dosage,
                Unit = item.Unit,
                IsEditable = true // 允许用户调整
            });
        }

        CloseHistoryListDialog();
    }

    // Step 3: 用户编辑调整后保存为验方
    public async Task SaveFormulaAsync()
    {
        var request = new CreateFormulaRequest
        {
            Name = this.FormulaName,
            Category = this.Category,
            Description = this.Description,
            Items = this.Items.Select(i => new FormulaItemDto
            {
                HerbId = i.HerbId,
                Dosage = i.Dosage,
                Unit = i.Unit
            }).ToList()
        };

        // 调用Formula API保存
        await _formulaService.CreateFormulaAsync(request);

        ShowSuccessMessage("验方保存成功");
    }
}
```

#### 涉及的API调用

| 步骤 | API端点 | 用途 |
|-----|---------|------|
| 1 | `GET /prescriptions/patient/{id}/recent?count=10` | 获取历史处方列表 |
| 2 | `GET /prescriptions/{id}` | 获取历史处方详情（含药材明细） |
| 3 | `POST /formulas` | 保存为验方 |

#### 前提条件

⚠️ **Formula模块需实现基础API**:
- `POST /formulas` - 创建验方
- `GET /formulas/{id}` - 获取验方详情
- `PUT /formulas/{id}` - 更新验方
- `DELETE /formulas/{id}` - 删除验方

如Formula模块尚未实现，需优先实施Formula模块的基础CRUD功能。

---

## 📋 实施检查清单

### Phase 1检查清单（删除超前设计）

- [ ] 删除PrescriptionService的6个方法
  - [ ] `GetPagedAsync`
  - [ ] `RecalculatePriceAsync`
  - [ ] `GeneratePrintFormatAsync`
  - [ ] `GeneratePrescriptionNoAsync`
  - [ ] `GetStatisticsAsync`
  - [ ] `GetRangeStatisticsAsync`
- [ ] 删除IPrescriptionService接口的6个方法签名
- [ ] 删除ConsultationService的2个方法
  - [ ] `GetPagedAsync`
  - [ ] `SearchAsync`
- [ ] 删除IConsultationService接口的2个方法签名
- [ ] 删除ConsultationController的2个端点
  - [ ] `GET /consultations`
  - [ ] `GET /consultations/search`
- [ ] 编译验证（0 errors, 0 warnings）

### Phase 2检查清单（新增PrescriptionsController）

- [ ] 创建PrescriptionsController.cs
- [ ] 实现端点1：`GET /prescriptions/{id}`
- [ ] 实现端点2：`GET /prescriptions/medicalcase/{medicalCaseId}`
- [ ] 实现端点3：`GET /prescriptions/search`（REQ-2）
- [ ] 实现端点4：`GET /prescriptions/patient/{patientId}/recent`（REQ-1）
- [ ] 添加Controller注释文档
- [ ] 编译验证（0 errors, 0 warnings）
- [ ] 运行时验证（启动WebAPI，测试4个端点）
- [ ] Swagger文档验证（4个端点出现在Swagger UI）

### Phase 3检查清单（Repository改为internal）

- [ ] 修改ConsultationRepository为internal
- [ ] 修改PrescriptionRepository为internal
- [ ] 修改MedicalCaseRepository为internal
- [ ] 修改PatientRepository为internal
- [ ] 修改UserRepository为internal
- [ ] 修改HerbRepository为internal
- [ ] 修改FormulaRepository为internal
- [ ] 修改AuthRepository为internal
- [ ] 修改PrescriptionItemRepository为internal
- [ ] 验证所有接口保持public
- [ ] 编译验证（0 errors, 0 warnings）
- [ ] 强制约束验证（尝试直接访问Repository应编译失败）

### 整体验收检查清单

- [ ] 所有Phase编译通过（0 errors, 0 warnings）
- [ ] 运行时验证通过（启动应用，测试核心功能）
- [ ] REQ-1测试通过（查询患者历史处方）
- [ ] REQ-2测试通过（按病症关键词搜索处方）
- [ ] Repository约束验证通过（Controller无法直接访问Repository）
- [ ] Swagger文档更新（新增4个端点，删除2个端点）

---

## 📝 后续文档更新清单

完成实施后需同步更新：

- [ ] `docs/architecture/server/README.md` - 更新Consultation/Prescription模块说明
- [ ] `docs/api/prescriptions-api.md` - 新增Prescription API文档
- [ ] `docs/api/consultation-api.md` - 更新Consultation API（删除2个端点）
- [ ] `docs/index.md` - 更新导航链接
- [ ] `docs/quick-reference/api-reference.md` - 更新API快速参考

---

**生成者**: Claude Code
**版本**: v2.0
**对应需求**: `docs/requirements/server-refactor-requirements-v2.md`
**下一步**: 等待用户确认设计文档，然后开始实施或生成任务分解清单
