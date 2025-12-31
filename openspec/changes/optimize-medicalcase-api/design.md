# Design: optimize-medicalcase-api

## 一、当前架构分析

### 1.1 数据流架构

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Desktop Client                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  View → ViewModel → Service → Repository → API (Refit)                      │
│                                                                              │
│  关键文件:                                                                    │
│  ├── IMedicalCaseApi.cs (20个方法)                                           │
│  ├── MedicalCaseRepository.cs                                                │
│  ├── MedicalCaseService.cs                                                   │
│  └── MedicalCaseWorkspaceViewModel.cs                                        │
└─────────────────────────────────────────────────────────────────────────────┘
                              │ HTTP
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              WebAPI Server                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│  Controller → Service (CQRS) → Repository → Entity                          │
│                                                                              │
│  关键文件:                                                                    │
│  ├── MedicalCaseController.cs (23个端点)                                     │
│  ├── MedicalCaseQueryService.cs (11个方法)                                   │
│  ├── MedicalCaseCommandService.cs (27个方法)                                 │
│  ├── MedicalCaseStateService.cs (7个方法)                                    │
│  └── MedicalCaseRepository.cs                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 当前端点清单

#### Client端 IMedicalCaseApi (20个方法)

| # | 方法名 | HTTP | 路径 | 返回类型 | 状态 |
|---|--------|------|------|----------|------|
| 1 | GetMedicalCasesAsync | GET | / | `PagedResult<ListDto>` | 保留 |
| 2 | GetMedicalCaseByIdAsync | GET | /{id} | `DetailDto` | 合并 |
| 3 | GetMedicalCaseByIdWithDetailsAsync | GET | /{id}/details | `DetailDto` | 删除 |
| 4 | SearchMedicalCasesAsync | GET | /search | `PagedResult<DetailDto>` | 保留 |
| 5 | GetMedicalCasesByPatientIdAsync | GET | /patient/{patientId} | `List<ListDto>` | 整合 |
| 6 | GetPendingCasesAsync | GET | /pending | `List<PendingDto>` | 整合 |
| 7 | GetPatientRecentMedicalCasesAsync | GET | /patient/{patientId}/recent | `List<DetailDto>` | 整合 |
| 8 | GetUnfinishedCaseByPatientIdAsync | GET | /patient/{patientId}/unfinished | `DetailDto?` | 整合 |
| 9 | CreateMedicalCaseAsync | POST | / | `DetailDto` | 保留 |
| 10 | SaveAsync | PUT | /{id} | `DetailDto` | 保留 |
| 11 | SaveDraftAsync | PUT | /{id}/draft | `DetailDto` | 保留 |
| 12 | UpdateStatusAsync | PUT | /{id}/status | `DetailDto` | 保留 |
| 13 | CloseCaseAsync | POST | /{id}/close | `ApiResponse` | **修正返回类型** |
| 14 | CancelMedicalCaseAsync | POST | /{id}/cancel | `DetailDto` | 保留 |
| 15 | SetPrescriptionFlagAsync | PUT | /{id}/prescription-flag | `DetailDto` | 保留 |
| 16 | DeleteMedicalCaseAsync | DELETE | /{id} | `IApiResponse` | 保留 |
| 17 | BatchDeleteAsync | POST | /batch-delete | `ApiResponse` | 保留 |
| 18 | GetPermissionsAsync | GET | /{id}/permissions | `PermissionDto` | 保留 |
| 19 | GetAuditLogsAsync | GET | /{id}/audit-logs | `PagedResult<AuditLogDto>` | 移至审计模块 |
| 20 | (已删除QueryMedicalCasesAsync) | - | - | - | 已清理 |

---

## 二、优化设计方案

### 2.1 问题1: GetById端点简化

> **设计决策 DD-GetById**: 取消`includeDetails`参数设计
> 
> **分析结论**:
> 1. 当前两个端点底层执行相同的Include查询，`includeDetails`参数不产生性能优化
> 2. 简化端点只在映射层过滤数据，数据库查询开销相同
> 3. 调用场景分析：所有需要单条医案的场景都能处理完整DetailDto
> 4. v1.0功能完善阶段，优先简化API设计，降低维护成本

#### 变更前
```csharp
// IMedicalCaseApi.cs - 两个端点
[Get("/api/v1/medicalcases/{id}")]
Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id);

[Get("/api/v1/medicalcases/{id}/with-details")]
Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdWithDetailsAsync(Guid id);
```

#### 变更后
```csharp
// IMedicalCaseApi.cs - 统一为一个端点，始终返回完整数据
/// <summary>
/// 获取医案详情（包含诊断和处方）
/// </summary>
/// <param name="id">医案ID</param>
[Get("/api/v1/medicalcases/{id}")]
Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id);

// 删除旧端点（Phase 6清理）
// [Get("/api/v1/medicalcases/{id}/with-details")] - 已删除
```

#### Server端变更
```csharp
// MedicalCaseController.cs - 简化为单一端点
[HttpGet("{id:guid}")]
public async Task<ActionResult<ApiResponse<MedicalCaseDetailDto>>> GetById(Guid id)
{
    var medicalCase = await _queryService.GetByIdWithDetailsAsync(id);
    if (medicalCase == null)
        return NotFound(ApiResponse<MedicalCaseDetailDto>.Fail("医案不存在"));
    
    var detailDto = MapToMedicalCaseDetailDto(medicalCase);
    return Ok(ApiResponse<MedicalCaseDetailDto>.Success(detailDto));
}

// 删除旧端点 GetByIdWithDetails (Phase 6清理)
```

#### Client端变更
```csharp
// MedicalCaseRepository.cs - 删除GetByIdWithDetailsAsync
// 所有调用改为使用统一的GetByIdAsync
public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
{
    var response = await _api.GetMedicalCaseByIdAsync(id);
    return response.Data;
}

// 删除：GetByIdWithDetailsAsync方法
```

---

### 2.2 问题2: CloseCaseAsync返回类型统一

#### 变更前
```csharp
// IMedicalCaseApi.cs
[Post("/api/v1/medicalcases/{id}/close")]
Task<ApiResponse> CloseCaseAsync(Guid id);

// MedicalCaseController.cs
[HttpPost("{id:guid}/close")]
public async Task<ActionResult<ApiResponse>> CloseCase(Guid id)
{
    await _stateService.CloseCaseAsync(id);
    return Ok(new ApiResponse { Success = true, Message = "医案已关闭" });
}
```

#### 变更后
```csharp
// IMedicalCaseApi.cs
/// <summary>
/// 关闭医案（标记为已完成）
/// </summary>
/// <returns>更新后的医案详情</returns>
[Post("/api/v1/medicalcases/{id}/close")]
Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id);

// MedicalCaseController.cs
[HttpPost("{id:guid}/close")]
public async Task<ActionResult<ApiResponse<MedicalCaseDetailDto>>> CloseCase(Guid id)
{
    var result = await _stateService.CloseCaseAsync(id);
    var detailDto = MapToMedicalCaseDetailDto(result);
    return Ok(new ApiResponse<MedicalCaseDetailDto>
    {
        Success = true,
        Message = "医案已关闭",
        Data = detailDto
    });
}

// MedicalCaseStateService.cs - 返回类型调整
public async Task<MedicalCase> CloseCaseAsync(Guid id)
{
    // ... 原有逻辑
    await _repository.SaveChangesAsync();
    return medicalCase; // 返回实体而非void
}
```

---

### 2.3 问题3: 查询端点整合

#### 整合策略

| 原端点 | 整合方式 | 新参数 |
|--------|----------|--------|
| GetMedicalCasesAsync | 保留，增强 | 增加patientId, status, queryType参数 |
| GetMedicalCasesByPatientIdAsync | 整合 | queryType=null, patientId=指定值 |
| GetPendingCasesAsync | 整合 | queryType=pending |
| GetPatientRecentMedicalCasesAsync | 整合 | queryType=recent, patientId=指定值 |
| GetUnfinishedCaseByPatientIdAsync | 整合 | queryType=unfinished, patientId=指定值 |

#### 变更后
```csharp
// IMedicalCaseApi.cs
/// <summary>
/// 统一查询医案列表
/// </summary>
/// <param name="page">页码</param>
/// <param name="pageSize">每页大小</param>
/// <param name="keyword">搜索关键词</param>
/// <param name="patientId">患者ID筛选</param>
/// <param name="status">状态筛选</param>
/// <param name="queryType">查询类型: pending(待看诊), recent(最近), unfinished(未完成)</param>
/// <param name="includeAllDoctors">是否包含所有医生的记录</param>
[Get("/api/v1/medicalcases")]
Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync(
    [Query] int page = 1,
    [Query] int pageSize = 20,
    [Query] string? keyword = null,
    [Query] Guid? patientId = null,
    [Query] MedicalCaseStatus? status = null,
    [Query] string? queryType = null,
    [Query] bool includeAllDoctors = false);

// 旧端点标记Obsolete但保留
[Obsolete("使用 GetMedicalCasesAsync(patientId: patientId) 替代")]
[Get("/api/v1/medicalcases/patient/{patientId}")]
Task<ApiResponse<List<MedicalCaseListDto>>> GetMedicalCasesByPatientIdAsync(Guid patientId);

[Obsolete("使用 GetMedicalCasesAsync(queryType: \"pending\") 替代")]
[Get("/api/v1/medicalcases/pending")]
Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(...);

// ... 其他旧端点类似处理
```

---

### 2.4 DTO字段映射优化

#### 当前问题
```csharp
// MedicalCaseDetailModel.cs - 手动映射每个字段
public static MedicalCaseDetailModel FromDto(MedicalCaseDetailDto dto)
{
    var model = new MedicalCaseDetailModel
    {
        Id = dto.Id,
        PatientId = dto.PatientId,
        PatientName = dto.PatientName ?? string.Empty,
        // ... 20+个字段手动赋值
    };
    return model;
}
```

#### 优化方案
```csharp
// 方案A: 使用AutoMapper (推荐)
// 在Module中注册映射配置
public class MedicalCaseAutoMapperProfile : Profile
{
    public MedicalCaseAutoMapperProfile()
    {
        CreateMap<MedicalCaseDetailDto, MedicalCaseDetailModel>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.CaseStatus))
            .ForMember(dest => dest.PresentIllness, opt => opt.MapFrom(src => src.Consultation != null ? src.Consultation.PresentIllness : null))
            // ...
            ;
    }
}

// 方案B: 使用扩展方法 (轻量级)
public static class MedicalCaseDtoExtensions
{
    public static MedicalCaseDetailModel ToModel(this MedicalCaseDetailDto dto)
    {
        return new MedicalCaseDetailModel
        {
            // 使用object initializer减少重复代码
        };
    }
}
```

**决策**: 本次优化暂不引入AutoMapper，使用扩展方法优化。后续可考虑统一引入。

---

## 三、Server端CQRS服务调整

### 3.1 MedicalCaseQueryService变更

```csharp
// 新增统一查询方法
public async Task<PagedResult<MedicalCaseListDto>> GetListAsync(
    MedicalCaseQueryParameters parameters)
{
    var query = _repository.GetQueryable();

    // 应用筛选条件
    if (parameters.PatientId.HasValue)
        query = query.Where(x => x.PatientId == parameters.PatientId.Value);

    if (parameters.Status.HasValue)
        query = query.Where(x => x.CaseStatus == parameters.Status.Value);

    // 根据queryType应用特殊筛选
    query = parameters.QueryType switch
    {
        "pending" => query.Where(x => x.CaseStatus == MedicalCaseStatus.Active),
        "recent" => query.OrderByDescending(x => x.CreatedAt).Take(10),
        "unfinished" => query.Where(x => x.CaseStatus != MedicalCaseStatus.Completed),
        _ => query
    };

    // 分页
    return await query.ToPagedResultAsync(parameters.Page, parameters.PageSize);
}
```

### 3.2 MedicalCaseStateService变更

```csharp
// 修改CloseCaseAsync返回类型
public async Task<MedicalCase> CloseCaseAsync(Guid id)
{
    var medicalCase = await _repository.GetByIdAsync(id)
        ?? throw new NotFoundException($"医案不存在: {id}");

    if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
        throw new BusinessException("医案已完成，无需重复关闭");

    medicalCase.CaseStatus = MedicalCaseStatus.Completed;
    medicalCase.CompletedAt = DateTime.Now;

    await _repository.SaveChangesAsync();

    // 审计日志由独立审计模块提供，此处不处理

    return medicalCase; // 返回实体
}
```

---

## 四、文件变更清单

### 4.1 Client端文件

| 文件路径 | 变更类型 | 说明 |
|----------|----------|------|
| `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs` | 修改 | 合并GetById、修改CloseCaseAsync返回类型、标记旧端点Obsolete |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs` | 修改 | 适配API变更 |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs` | 修改 | 更新CloseCaseAsync调用 |
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseService.cs` | 修改 | 更新接口定义 |

### 4.2 Server端文件

| 文件路径 | 变更类型 | 说明 |
|----------|----------|------|
| `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` | 修改 | 合并端点、修改返回类型 |
| `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseQueryService.cs` | 修改 | 新增统一查询接口 |
| `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs` | 修改 | 实现统一查询 |
| `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseStateService.cs` | 修改 | CloseCaseAsync返回类型 |
| `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseStateService.cs` | 修改 | CloseCaseAsync返回实体 |

### 4.3 Shared文件

| 文件路径 | 变更类型 | 说明 |
|----------|----------|------|
| `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseQueryParameters.cs` | 新增 | 统一查询参数DTO |

### 4.4 测试文件

| 文件路径 | 变更类型 | 说明 |
|----------|----------|------|
| `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs` | 修改 | 更新测试用例 |
| `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseStateServiceTests.cs` | 修改 | 更新CloseCaseAsync测试 |

---

## 五、设计决策记录

### DR-1: 保留旧端点兼容性

**决策**: 旧端点标记`[Obsolete]`但保留，不立即删除

**理由**:
- 避免破坏现有调用方
- 给予迁移缓冲期
- 编译警告提醒开发者迁移

**后续**: 在下个主版本(v2.0)中移除Obsolete端点

### DR-2: 查询参数使用string类型的queryType

**决策**: `queryType`使用string而非enum

**理由**:
- API层面更灵活，易于扩展
- 避免Client/Server enum同步问题
- 支持未来新增查询类型

**约束**: 服务端需验证queryType有效性

### DR-3: 不引入AutoMapper

**决策**: 本次优化不引入AutoMapper

**理由**:
- 避免增加新依赖
- 项目规模尚可接受手动映射
- 扩展方法已能简化代码

**后续**: 如果模块增多，可统一评估引入AutoMapper

---

## 六、保存逻辑详细设计

### 6.1 保存场景分类

医案模块有4种保存场景，设计上需要清晰区分:

| 场景 | 端点 | 触发条件 | 保存内容 | 状态变化 |
|------|------|----------|----------|----------|
| **场景1: 创建医案** | `POST /` | 选择患者开始看诊 | MedicalCase基础字段 | → Active |
| **场景2: 保存草稿** | `PUT /{id}/draft` | 临时保存，不完成 | Consultation诊断信息 | → Draft |
| **场景3: 聚合保存** | `PUT /{id}` | 保存全部并继续 | MedicalCase + Consultation + Prescription | 不变 |
| **场景4: 完成医案** | `POST /{id}/close` | 确认完成看诊 | 无内容更新 | → Completed |

### 6.2 聚合保存数据流 (核心场景)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Desktop Client                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  MedicalCaseWorkspaceViewModel                                               │
│      │                                                                       │
│      │ 用户点击"保存"                                                         │
│      ▼                                                                       │
│  MedicalCaseService.SaveAsync()                                              │
│      │                                                                       │
│      │ 1. 检查 HasChanges                                                    │
│      │ 2. 构建 MedicalCaseInputDto                                           │
│      │    ├── 基础字段 (Remark, EditReason)                                  │
│      │    ├── Consultation (嵌套)                                            │
│      │    │   └── PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis
│      │    └── Prescription (嵌套，可选)                                       │
│      │        ├── DosageCount, Usage, Advice, Discount                       │
│      │        ├── ReferencedFormulas                                         │
│      │        └── Items[] (药材列表)                                          │
│      ▼                                                                       │
│  MedicalCaseRepository.SaveAsync(id, inputDto)                               │
│      │                                                                       │
│      │ 调用 IMedicalCaseApi.SaveAsync()                                      │
│      ▼                                                                       │
│  HTTP PUT /api/v1/medicalcases/{id}                                          │
│      Body: MedicalCaseInputDto (JSON)                                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  WebAPI Server                                                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  MedicalCaseController.Save(id, dto)                                         │
│      │                                                                       │
│      │ 1. 验证请求 (FluentValidation)                                        │
│      │ 2. 权限检查 (IAuthorizationService)                                   │
│      ▼                                                                       │
│  MedicalCaseCommandService.SaveAsync(id, dto)                                │
│      │                                                                       │
│      │ ┌─────────────────────────────────────────────────────────────────┐  │
│      │ │ 事务开始 (DbContext.BeginTransaction)                           │  │
│      │ ├─────────────────────────────────────────────────────────────────┤  │
│      │ │ Step 1: 加载聚合根                                               │  │
│      │ │   var medicalCase = await _repository.GetByIdAsync(id)          │  │
│      │ │     .Include(x => x.Consultation)                               │  │
│      │ │     .Include(x => x.Prescription)                               │  │
│      │ │       .ThenInclude(p => p.Items);                               │  │
│      │ ├─────────────────────────────────────────────────────────────────┤  │
│      │ │ Step 2: 验证编辑权限                                             │  │
│      │ │   - 检查医案状态 (非Completed/Cancelled)                         │  │
│      │ │   - 检查用户权限 (本人 或 历史编辑授权)                           │  │
│      │ │   - 历史编辑需要EditReason                                       │  │
│      │ ├─────────────────────────────────────────────────────────────────┤  │
│      │ │ Step 3: 更新MedicalCase基础字段                                  │  │
│      │ │   medicalCase.Remark = dto.Remark;                              │  │
│      │ │   medicalCase.UpdatedAt = DateTime.Now;                         │  │
│      │ ├─────────────────────────────────────────────────────────────────┤  │
│      │ │ Step 4: 更新Consultation (如有)                                  │  │
│      │ │   if (dto.Consultation != null)                                 │  │
│      │ │   {                                                             │  │
│      │ │       UpdateConsultationFields(medicalCase.Consultation, dto);  │  │
│      │ │   }                                                             │  │
│      │ ├─────────────────────────────────────────────────────────────────┤  │
│      │ │ Step 5: 处理Prescription (复杂逻辑)                              │  │
│      │ │   HandlePrescriptionUpdate(medicalCase, dto);                   │  │
│      │ │   // 详见 6.3 处方处理逻辑                                       │  │
│      │ ├─────────────────────────────────────────────────────────────────┤  │
│      │ │ Step 6: 保存变更                                                 │  │
│      │ │   await _repository.SaveChangesAsync();                         │  │
│      │ └─────────────────────────────────────────────────────────────────┘  │
│      │ 事务提交                                                              │
│      ▼                                                                       │
│  返回 ApiResponse<MedicalCaseDetailDto>                                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.3 处方处理逻辑 (HandlePrescriptionUpdate)

> **设计决策 Q2**: Server自动控制处方存在性，移除`NeedsPrescription`字段
> - 业务规则: 有药材=有处方，无药材=无处方
> - 判断逻辑: `Items.Any()` 替代 `NeedsPrescription`

处方处理简化为2种场景:

```
┌────────────────────────────────────────────────────────────────────────────┐
│  处方处理决策树 (简化版)                                                    │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  输入参数:                                                                   │
│  - dto.Prescription?.Items: List<PrescriptionItemInputDto>? (药材列表)      │
│  - medicalCase.Prescription: Prescription? (现有处方)                        │
│                                                                             │
│  决策逻辑:                                                                   │
│                                                                             │
│  var hasItems = dto.Prescription?.Items?.Any() == true;                     │
│                                                                             │
│  if (hasItems)                                                              │
│  {                                                                          │
│      // 场景A: 有药材 → 创建/更新处方                                        │
│      if (medicalCase.Prescription == null)                                  │
│      {                                                                      │
│          // A1: 没有现有处方 → 创建新处方                                    │
│          medicalCase.Prescription = CreateNewPrescription(dto.Prescription);│
│      }                                                                      │
│      else                                                                   │
│      {                                                                      │
│          // A2: 有现有处方 → 全量替换Items                                   │
│          ReplaceAllItems(medicalCase.Prescription, dto.Prescription);       │
│      }                                                                      │
│  }                                                                          │
│  else                                                                       │
│  {                                                                          │
│      // 场景B: 无药材 → 删除处方                                             │
│      if (medicalCase.Prescription != null)                                  │
│      {                                                                      │
│          _context.Prescriptions.Remove(medicalCase.Prescription);           │
│      }                                                                      │
│  }                                                                          │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

**优势**:
- 逻辑简化，无需Client显式声明`NeedsPrescription`
- 业务规则由Server统一控制，减少出错可能

### 6.4 处方Items更新策略

> **设计决策 Q1**: 采用**全量替换**策略
> - 业务场景分析: 打印/查询/导入/审计均不需要ItemId稳定
> - V2发药设计: 采用快照模式(Prescription+HerbId)，不引用ItemId
> - 性能考量: 处方药材数量有限(5-15味)，全量替换开销可接受

```csharp
private void ReplaceAllItems(Prescription existing, PrescriptionInputDto dto)
{
    // 1. 更新基础字段
    existing.DosageCount = dto.DosageCount;
    existing.Usage = dto.Usage;
    existing.Advice = dto.Advice;
    existing.Discount = dto.Discount;
    existing.ReferencedFormulas = dto.ReferencedFormulas;
    existing.UpdatedAt = DateTime.Now;

    // 2. Items更新策略: 全量替换
    //    删除旧Items → 插入新Items（原子操作）
    
    // 2.1 清空所有现有Items
    existing.Items.Clear();

    // 2.2 创建新Items
    foreach (var itemDto in dto.Items)
    {
        existing.Items.Add(new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            PrescriptionId = existing.Id,
            HerbId = itemDto.HerbId,
            HerbName = itemDto.HerbName,
            Dosage = itemDto.Dosage,
            Unit = itemDto.Unit,
            UnitPrice = itemDto.UnitPrice,
            ProcessingMethod = itemDto.ProcessingMethod,
            Remark = itemDto.Remark,
            SortOrder = itemDto.SortOrder
        });
    }

    // 3. 重新计算价格
    existing.SingleDosePrice = existing.Items.Sum(i => i.Dosage * i.UnitPrice);
    existing.TotalPrice = existing.SingleDosePrice * existing.DosageCount * existing.Discount;
    existing.TotalWeight = existing.Items.Sum(i => i.Dosage);
}
```

**优势**:
1. 实现简单，代码清晰
2. 事务安全，原子操作
3. 无需复杂的差异对比算法
4. 为V2发药系统预留正确的设计空间
```

### 6.5 暂存医案逻辑 (SaveDraft)

> **设计决策 Q3**: 暂存医案机制
> - 使用现有`Draft`状态表示暂存（术语统一：旧"挂起"→新"暂存"）
> - 保存诊断内容 + 处方药材（不校验完整性）
> - 同一患者最多1个Draft状态医案
> - Draft永不过期，手动取消或下次挂号时提醒

```
暂存医案 vs 聚合保存 区别:

┌──────────────────┬─────────────────────┬─────────────────────┐
│      对比项       │     暂存医案         │      聚合保存        │
├──────────────────┼─────────────────────┼─────────────────────┤
│ 端点              │ PUT /{id}/draft     │ PUT /{id}           │
│ 状态变化          │ Active → Draft      │ 不变                 │
│ 保存内容          │ Consultation + 处方  │ 全部                 │
│ 验证规则          │ 不校验（允许不完整）  │ 严格校验             │
│ UI状态变化        │ 编辑状态 → 查看状态  │ 保持编辑状态         │
│ 适用场景          │ 临时离开，稍后继续   │ 正式保存继续编辑     │
└──────────────────┴─────────────────────┴─────────────────────┘
```

**三个入口场景**:

| 入口 | 触发条件 | 行为 |
|------|----------|------|
| 入口1 | 医案编辑界面点击"暂存医案"按钮 | 编辑模式→查看模式，状态Active→Draft，列表状态显示"暂存" |
| 入口2 | 待诊列表双击暂存医案 | 直接进入编辑模式 |
| 入口3 | 患者列表选择患者进入 | 检测有暂存→弹出四项选择对话框 |

**入口3的四项选择**:

```
┌─────────────────────────────────────────────┐
│  检测到该患者存在暂存医案                      │
├─────────────────────────────────────────────┤
│  ○ 继续暂存医案      → 打开暂存医案继续编辑    │
│  ○ 关闭暂存医案后新建  → 取消旧的，创建新医案   │
│  ○ 仅关闭暂存医案     → 取消旧的，返回患者列表  │
│  ○ 取消              → 返回患者列表           │
└─────────────────────────────────────────────┘
```

> "关闭暂存医案" = 状态变为Cancelled

**业务规则**:
- 同一患者最多1个Draft状态医案
- 新建医案前检测：如有Draft，必须先处理（继续/关闭）
- Draft永久保留，直到：手动取消 / 继续编辑后完成或取消 / 下次挂号时触发提醒处理

### 6.6 并发控制 (RowVersion)

```csharp
// MedicalCaseWorkspaceContext中保存RowVersion
public class MedicalCaseWorkspaceContext
{
    public byte[]? MedicalCaseRowVersion { get; set; }
    public byte[]? ConsultationRowVersion { get; set; }
    public byte[]? PrescriptionRowVersion { get; set; }
}

// 保存时传递RowVersion (通过HTTP Header)
// Header: X-RowVersion-MedicalCase: base64encoded
// Header: X-RowVersion-Consultation: base64encoded
// Header: X-RowVersion-Prescription: base64encoded

// Server端并发检查
public async Task<MedicalCase> SaveAsync(Guid id, MedicalCaseInputDto dto, RowVersions versions)
{
    var medicalCase = await _repository.GetByIdAsync(id);
    
    // 检查RowVersion
    if (versions.MedicalCase != null && 
        !medicalCase.RowVersion.SequenceEqual(versions.MedicalCase))
    {
        throw new ConcurrencyException("医案已被其他用户修改，请刷新后重试");
    }
    
    // ... 执行保存
}
```

### 6.7 保存错误处理

| 错误类型 | HTTP状态码 | 处理方式 |
|----------|-----------|----------|
| 验证失败 | 400 Bad Request | 返回详细验证错误 |
| 权限不足 | 403 Forbidden | 返回权限不足提示 |
| 医案不存在 | 404 Not Found | 返回资源不存在 |
| 并发冲突 | 409 Conflict | 返回冲突信息，提示刷新 |
| 业务规则违反 | 422 Unprocessable Entity | 返回业务错误信息 |
| 服务器错误 | 500 Internal Server Error | 记录日志，返回通用错误 |

### 6.8 已完成/已取消医案的修改权限

> **设计决策 Q4**: 基于角色+时间+所有权的权限控制
> - 医生只能操作自己的医案（通过UserId判断）
> - "当天"使用本地时间，按日期判断，无宽限期

**权限矩阵**:

| 医案状态 | 本人医生(当天) | 本人医生(非当天) | 其他医生 | 管理员 |
|----------|---------------|-----------------|----------|--------|
| Active | 编辑 | 编辑 | 无权限 | 编辑 |
| Draft | 编辑 | 编辑 | 无权限 | 编辑 |
| Completed | 修改 | 只读 | 无权限 | 修改 |
| Cancelled | 修改 | 只读 | 无权限 | 修改/恢复 |

**判断基准**:
- 所有权: `medicalCase.UserId == user.Id`
- 当天(Completed): `CompletedAt.Date == DateTime.Today`（本地时间）
- 当天(Cancelled): `CancelledAt.Date == DateTime.Today`（本地时间）

```csharp
public class MedicalCaseAuthorizationService : IMedicalCaseAuthorizationService
{
    public bool CanModify(MedicalCaseModel medicalCase, UserContext user)
    {
        // 管理员：无任何限制
        if (user.IsAdmin)
            return true;
        
        // 医生：只能操作自己的医案
        if (medicalCase.UserId != user.Id)
            return false;
        
        // Active/Draft：自己的可编辑
        if (medicalCase.Status is MedicalCaseStatus.Active or MedicalCaseStatus.Draft)
            return true;
        
        // Completed：自己的 + 当天完成的（本地日期）
        if (medicalCase.Status == MedicalCaseStatus.Completed)
            return medicalCase.CompletedAt?.Date == DateTime.Today;
        
        // Cancelled：自己的 + 当天取消的（本地日期）
        if (medicalCase.Status == MedicalCaseStatus.Cancelled)
            return medicalCase.CancelledAt?.Date == DateTime.Today;
        
        return false;
    }
}
```

> **审计说明**: 审计功能由独立审计模块(`create-audit-module`)提供，通过IAuditService注入集成

---

## 七、兼容性矩阵

| 调用场景 | 迁移前代码 | 迁移后代码 | 兼容性 |
|----------|-----------|-----------|--------|
| 获取详情 | `GetByIdWithDetailsAsync(id)` | `GetMedicalCaseByIdAsync(id, true)` | 旧方法可用，有警告 |
| 关闭医案 | `CloseCaseAsync(id)` 返回`ApiResponse` | 返回`ApiResponse<DetailDto>` | **破坏性变更** |
| 待看诊列表 | `GetPendingCasesAsync()` | `GetMedicalCasesAsync(queryType: "pending")` | 旧方法可用，有警告 |

**注意**: CloseCaseAsync是破坏性变更，需更新所有调用方。

---

## 八、废弃与清理策略

### 8.1 渐进式重构原则

本次重构采用**渐进式废弃策略**，确保：
1. **向后兼容**: 旧API在重构期间仍可使用
2. **编译提醒**: 使用`[Obsolete]`特性生成编译警告
3. **迁移窗口**: 给调用方足够时间迁移
4. **统一清理**: 重构完成后统一删除废弃代码

### 8.2 废弃标注规范

```csharp
// Server端 - Controller/Service
[Obsolete("Use GetMedicalCases with QueryType=Pending instead. Will be removed in v2.0")]
[HttpGet("pending")]
public async Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCases(...)

// Client端 - API接口
[Obsolete("Use GetMedicalCasesAsync with queryType parameter. Will be removed in v2.0")]
[Get("/api/v1/medicalcases/pending")]
Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(...);
```

### 8.3 废弃清单

#### Server端（WebAPI Controller）

| 端点 | 废弃原因 | 替代方案 | 清理阶段 |
|------|----------|----------|----------|
| `GET /{id}/with-details` | 合并到GetById | `GET /{id}` (统一返回完整数据) | Phase 6.1 |
| `GET /pending` | 整合到统一查询 | `GET /?queryType=Pending` | Phase 6.1 |
| `GET /patient/{id}` | 整合到统一查询 | `GET /?queryType=ByPatient&patientId={id}` | Phase 6.1 |
| `GET /patient/{id}/unfinished` | 整合到统一查询 | `GET /?queryType=Unfinished&patientId={id}` | Phase 6.1 |
| `GET /patient/{id}/recent` | 整合到统一查询 | `GET /?queryType=Recent&patientId={id}` | Phase 6.1 |

#### Server端（Service/Repository）

| 方法 | 废弃原因 | 替代方案 | 清理阶段 |
|------|----------|----------|----------|
| `GetByIdWithDetailsAsync` | 合并到GetByIdAsync | `GetByIdAsync(id)` (返回完整数据) | Phase 6.2 |
| `GetPendingCasesAsync` | 整合到QueryAsync | `QueryAsync(queryType: Pending)` | Phase 6.2 |
| `GetByPatientIdAsync` | 整合到QueryAsync | `QueryAsync(queryType: ByPatient)` | Phase 6.2 |
| `GetUnfinishedByPatientAsync` | 整合到QueryAsync | `QueryAsync(queryType: Unfinished)` | Phase 6.2 |

#### Client端（API接口）

| 方法 | 废弃原因 | 替代方案 | 清理阶段 |
|------|----------|----------|----------|
| `GetMedicalCaseByIdWithDetailsAsync` | 合并 | `GetMedicalCaseByIdAsync(id)` (返回完整数据) | Phase 6.3 |
| `GetPendingCasesAsync` | 整合 | `GetMedicalCasesAsync(queryType: Pending)` | Phase 6.3 |
| `GetMedicalCasesByPatientIdAsync` | 整合 | `GetMedicalCasesAsync(queryType: ByPatient)` | Phase 6.3 |
| `GetUnfinishedCaseByPatientIdAsync` | 整合 | `GetMedicalCasesAsync(queryType: Unfinished)` | Phase 6.3 |
| `GetPatientRecentMedicalCasesAsync` | 整合 | `GetMedicalCasesAsync(queryType: Recent)` | Phase 6.3 |

#### DTO/Entity字段

| 字段 | 废弃原因 | 替代方案 | 清理阶段 |
|------|----------|----------|----------|
| `ConsultationModel.NeedsPrescription` | Server自动控制 | `Prescription.Items.Any()` | Phase 6.4 |
| `ConsultationInputDto.NeedsPrescription` | 同上 | 移除该字段 | Phase 6.4 |
| `ConsultationDetailDto.NeedsPrescription` | 同上 | 移除该字段 | Phase 6.4 |

### 8.4 清理时机

```
重构阶段:
├── Phase 1-5: 实现新功能 + 标注[Obsolete]
│   ├── 新旧API并存
│   ├── 编译产生Obsolete警告
│   └── 调用方逐步迁移
│
└── Phase 6: 废弃代码清理 (重构完成后执行)
    ├── 6.1 清理Controller废弃端点
    ├── 6.2 清理Service/Repository废弃方法
    ├── 6.3 清理Client API废弃方法
    ├── 6.4 清理DTO/Entity废弃字段
    └── 6.5 执行数据库迁移删除列
```

### 8.5 清理验证

清理前必须确认：
1. **无调用**: 全局搜索确认无代码调用废弃方法
2. **无警告**: 编译无Obsolete警告（除即将清理的）
3. **测试通过**: 所有单元测试和集成测试通过
4. **文档更新**: API文档已移除废弃端点说明

---

## 九、层级职责划分

### 9.1 总体架构

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Desktop Client                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   View (XAML)                  │ UI呈现、用户交互                            │
│        ↓                       │                                             │
│   ViewModel                    │ UI状态管理、命令处理、对话框控制             │
│        ↓                       │                                             │
│   Service                      │ 业务逻辑协调、Draft检查、权限预判            │
│        ↓                       │                                             │
│   Repository                   │ API调用封装、数据缓存                        │
│        ↓                       │                                             │
│   IMedicalCaseApi (Refit)      │ HTTP接口定义                                │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                              │ HTTP
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              WebAPI Server                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   Controller                   │ 请求路由、参数验证、响应封装                │
│        ↓                       │                                             │
│   AuthorizationService         │ 权限验证（UserId+角色+时间）                │
│        ↓                       │                                             │
│   CommandService/StateService  │ 业务逻辑、状态转换、处方生命周期             │
│        ↓                       │                                             │
│   QueryService                 │ 查询逻辑、统一查询分发                       │
│        ↓                       │                                             │
│   Repository                   │ 数据访问、EF Core操作                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 9.2 职责分配详表

| 功能点 | Client层 | Server层 | 说明 |
|--------|----------|----------|------|
| **Draft存在检查** | Service | - | 选择患者前查询，决定是否弹出四选对话框 |
| **四选对话框** | ViewModel | - | UI层处理，根据选择调用不同API |
| **权限预判断** | Service (可选) | AuthorizationService | Client可预判减少无效请求，Server必须验证 |
| **权限最终验证** | - | AuthorizationService | 所有写操作前必须调用CanModify |
| **状态转换验证** | - | StateService | Active↔Draft, Active→Completed等 |
| **处方生命周期** | - | CommandService | Items.Any()→创建/删除Prescription |
| **全量替换Items** | - | CommandService | 删除旧Items→添加新Items |
| **RowVersion并发** | ViewModel/Service | CommandService | Client保存传递，Server验证 |
| **统一查询分发** | Repository | QueryService | 根据QueryType路由到对应方法 |

### 9.3 关键判断逻辑层级

#### 9.3.1 权限判断 (双层)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Client端 (可选预判断)                                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│  MedicalCaseService.CanModify(medicalCase, currentUser)                     │
│  - 目的: 减少无效API调用，提升用户体验                                        │
│  - 实现: 复制Server端逻辑（简化版）                                           │
│  - 注意: 仅用于UI提示（禁用按钮等），不能替代Server验证                        │
└─────────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  Server端 (必须验证)                                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  IMedicalCaseAuthorizationService.CanModify(medicalCase, user)              │
│  - 位置: 所有写操作API的第一步                                               │
│  - 验证: UserId归属 + 角色 + 时间限制                                        │
│  - 失败: 返回403 Forbidden                                                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### 9.3.2 Draft检查 (Client主导)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  触发: 患者列表选择患者                                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  PatientSelectionViewModel                                                   │
│      │                                                                       │
│      │ OnPatientSelected(patientId)                                         │
│      ▼                                                                       │
│  MedicalCaseService.GetPatientDraftCaseAsync(patientId)                     │
│      │                                                                       │
│      │ 调用 API: GET /medicalcases?queryType=Unfinished&patientId=xxx       │
│      │ 过滤: Status == Draft                                                 │
│      ▼                                                                       │
│  判断结果:                                                                    │
│      ├── Draft存在 → 弹出UnfinishedCaseDialog (四选对话框)                   │
│      └── Draft不存在 → 直接创建新Active医案                                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### 9.3.3 处方生命周期 (Server独占)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  触发: 任何保存操作 (SaveAsync)                                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  MedicalCaseCommandService.SaveAsync(id, inputDto)                          │
│      │                                                                       │
│      │ 检查 dto.Prescription?.Items                                         │
│      ▼                                                                       │
│  决策:                                                                       │
│      │                                                                       │
│      ├── Items非空 + Prescription为null                                      │
│      │   → 创建Prescription，关联Items                                       │
│      │                                                                       │
│      ├── Items非空 + Prescription存在                                        │
│      │   → 全量替换Items                                                     │
│      │                                                                       │
│      └── Items为空/null + Prescription存在                                   │
│          → 删除Prescription                                                  │
│                                                                              │
│  结果: Prescription存在性 = Items.Any()                                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 十、现有代码分析与简化

### 10.1 代码分析概述

通过对现有代码的系统性分析，识别出以下问题和优化点：

### 10.2 Server端分析

#### 10.2.1 MedicalCaseController (29个方法)

**当前端点分布**:
| 分类 | 端点数 | 示例 |
|------|--------|------|
| 查询端点 | 11个 | GetById, GetByIdWithDetails, GetPending, GetByPatientId等 |
| 写操作端点 | 8个 | Save, SaveDraft, Close, Cancel, Create等 |
| 辅助端点 | 5个 | GetPermissions, GetAuditLogs, GetConsultationList等 |
| 私有映射方法 | 5个 | MapToMedicalCaseDto, MapToMedicalCaseDetailDto等 |

**识别的问题**:
1. **查询端点碎片化**: GetById/GetByIdWithDetails可合并
2. **多个分散的列表查询**: GetPending, GetByPatientId, GetUnfinished, GetPatientRecent应统一
3. **重复的映射逻辑**: 多处手动Entity→DTO映射代码

#### 10.2.2 MedicalCaseCommandService (27个方法)

**核心方法分析**:
```csharp
// 当前处方处理逻辑 - 依赖NeedsPrescription字段
private void HandlePrescriptionUpdate(MedicalCase medicalCase, PrescriptionInputDto prescriptionDto)
{
    medicalCase.NeedsPrescription = prescriptionDto.NeedsPrescription;  // ← 需移除

    if (!prescriptionDto.NeedsPrescription)
    {
        SoftDeletePrescriptionIfExists(medicalCase);
        return;
    }
    // ...
}
```

**需简化**:
- 移除对`NeedsPrescription`字段的依赖
- 简化为基于`Items.Any()`的自动管理

#### 10.2.3 MedicalCaseStateService (7个方法)

**CloseCaseAsync当前实现**:
```csharp
public async Task<bool> CloseCaseAsync(Guid id)  // ← 返回bool，需改为MedicalCase
{
    // ...
    await _repository.UpdateAsync(medicalCase);
    return true;
}
```

**需修改**: 返回完整的MedicalCase实体而非bool

### 10.3 Client端分析

#### 10.3.1 已实现的功能

**好的设计（保留）**:
1. **MedicalCaseService.SaveAsync()** - 已实现聚合保存模式
2. **HandleSuspendedCaseAsync()** - 四选对话框已实现
3. **ExecuteSaveDraft()** - 暂存命令已实现

#### 10.3.2 需要调整的部分

1. **CloseCaseAsync调用** - 适配新返回类型`MedicalCaseDetailDto`
2. **查询方法** - 迁移到统一查询端点

### 10.4 过重设计识别

| 问题 | 当前状态 | 简化方案 | 优先级 |
|------|----------|----------|--------|
| 查询端点碎片 | 5个分散端点 | 统一QueryType参数 | P0 |
| 两个详情端点 | GetById + GetByIdWithDetails | 合并为一个(返回完整数据) | P0 |
| NeedsPrescription | Client传递，Server存储 | Server自动管理，移除字段 | P1 |
| CloseCaseAsync返回 | 返回bool | 返回MedicalCaseDetailDto | P1 |
| 重复映射代码 | 手动映射多处 | 统一MapTo私有方法 | P2 |

### 10.5 简化实施计划

#### Phase A: API层整合 (P0)
1. 合并GetById端点
2. 统一列表查询端点
3. 标记旧端点Obsolete

#### Phase B: 业务逻辑简化 (P1)
1. 修改CloseCaseAsync返回类型
2. 简化处方生命周期逻辑
3. 移除NeedsPrescription依赖

#### Phase C: 代码清理 (P2)
1. 统一映射方法
2. 删除废弃代码
3. 数据库迁移删除废弃字段

---

## 十一、功能清单与验收标准

### 10.1 API端点清单

#### 新增/修改端点

| 端点 | 方法 | 功能 | 返回类型 | 状态 |
|------|------|------|----------|------|
| `/api/v1/medicalcases/{id}` | GET | 获取医案详情(含诊断处方) | `MedicalCaseDetailDto` | 简化(删除with-details) |
| `/api/v1/medicalcases` | GET | 统一列表查询 | `PagedResult<MedicalCaseListDto>` | 修改(+QueryType) |
| `/api/v1/medicalcases/{id}/close` | POST | 关闭医案 | `MedicalCaseDetailDto` | 修改(返回类型) |
| `/api/v1/medicalcases/{id}/suspend` | POST | 暂存医案 | `MedicalCaseDetailDto` | 新增 |
| `/api/v1/medicalcases/{id}` | PUT | 聚合保存 | `MedicalCaseDetailDto` | 保留(处方生命周期) |

#### 废弃端点 (Phase 6清理)

| 端点 | 替代方案 |
|------|----------|
| `GET /{id}/with-details` | `GET /{id}` (统一返回完整数据) |
| `GET /pending` | `GET /?queryType=Pending` |
| `GET /patient/{patientId}` | `GET /?queryType=ByPatient&patientId=xxx` |
| `GET /patient/{patientId}/unfinished` | `GET /?queryType=Unfinished&patientId=xxx` |
| `GET /patient/{patientId}/recent` | `GET /?queryType=Recent&patientId=xxx` |

### 10.2 Client端功能清单

| 功能 | 负责层 | 实现要点 |
|------|--------|----------|
| Draft存在检查 | Service | `GetPatientDraftCaseAsync(patientId)` |
| 四选对话框 | ViewModel/View | `UnfinishedCaseDialog.xaml` |
| 权限预判断(可选) | Service | `CanModify()`简化版 |
| 暂存按钮命令 | ViewModel | `SuspendCaseCommand` |
| 编辑/查看模式切换 | ViewModel | 暂存后自动切换为只读 |

### 10.3 Server端功能清单

| 功能 | 负责层 | 实现要点 |
|------|--------|----------|
| 权限验证 | AuthorizationService | `CanModify(medicalCase, user)` |
| 暂存状态转换 | StateService | `SuspendCaseAsync()` |
| 处方生命周期 | CommandService | `SavePrescriptionAsync()` |
| 统一查询分发 | QueryService | `QueryAsync(queryDto)` |
| 全量替换Items | CommandService | `ReplaceAllItems()` |

### 10.4 验收标准

#### 编译验收
- [ ] `dotnet build LYBT.All.sln` 无错误
- [ ] 仅产生预期的Obsolete警告

#### 功能验收 - API层
- [ ] `GET /{id}` 返回完整DetailDto(含Consultation+Prescription)
- [ ] `GET /?queryType=Pending` 返回待看诊列表
- [ ] `GET /?queryType=ByPatient&patientId=xxx` 返回患者医案列表
- [ ] `GET /?queryType=Unfinished&patientId=xxx` 返回未完成医案
- [ ] `GET /?queryType=Recent&patientId=xxx` 返回最近医案
- [ ] `POST /{id}/close` 返回完整DetailDto
- [ ] `POST /{id}/suspend` 保存数据并返回Draft状态

#### 功能验收 - 处方生命周期
- [ ] 首次添加Item时自动创建Prescription
- [ ] 移除所有Items时自动删除Prescription
- [ ] 空Items保存不创建Prescription

#### 功能验收 - 暂存机制
- [ ] 入口1: 点击暂存按钮，Active→Draft，切换只读
- [ ] 入口2: 双击Draft医案，Draft→Active，进入编辑
- [ ] 入口3: 选择有Draft的患者，弹出四选对话框
- [ ] 四选对话框各选项功能正确

#### 功能验收 - 权限控制
- [ ] 医生只能操作自己的医案
- [ ] 医生当天可修改自己的Completed/Cancelled医案
- [ ] 医生隔天不可修改Completed/Cancelled医案
- [ ] 管理员无任何限制

#### 测试验收
- [ ] 单元测试覆盖率 > 80%
- [ ] 集成测试全部通过
- [ ] 权限场景测试全覆盖
