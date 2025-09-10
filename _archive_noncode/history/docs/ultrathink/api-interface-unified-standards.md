# API接口统一规范文档

## 📋 当前API设计现状分析

### 已统一的规范 ✅
基于对现有Controller的分析，发现系统已经有很好的API统一性：

1. **路由模式统一**: 所有控制器都使用 `[Route("api/v{version:apiVersion}/[controller]")]`
2. **版本控制统一**: 所有API都使用 `[ApiVersion("1")]`
3. **响应格式统一**: 都继承 `BaseApiController` 使用 `ApiResponse<T>` 格式
4. **异常处理统一**: 都使用 `HandleException<T>()` 方法
5. **验证机制统一**: 都使用 `ValidateGuid<T>()` 和 `ValidateModel<T>()` 方法

### 三个核心模块API现状

#### Herbs API (药材模块)
```csharp
[Route("api/v{version:apiVersion}/[controller]")]
public class HerbsController : BaseApiController
{
    [HttpGet] // GET api/v1/herbs
    [HttpGet("{id}")] // GET api/v1/herbs/{id}
    [HttpPost] // POST api/v1/herbs
    [HttpPut("{id}")] // PUT api/v1/herbs/{id}
    [HttpDelete("{id}")] // DELETE api/v1/herbs/{id}
    [HttpGet("categories")] // GET api/v1/herbs/categories
    [HttpGet("search")] // GET api/v1/herbs/search
}
```

#### Formulas API (验方模块)
```csharp
[Route("api/v{version:apiVersion}/[controller]")]
public class FormulasController : BaseApiController
{
    // 基础CRUD操作 (推测，需要确认)
    [HttpGet] // GET api/v1/formulas
    [HttpGet("{id}")] // GET api/v1/formulas/{id}
    [HttpPost] // POST api/v1/formulas
    [HttpPut("{id}")] // PUT api/v1/formulas/{id}
    [HttpDelete("{id}")] // DELETE api/v1/formulas/{id}
}
```

#### Prescriptions API (处方模块)
```csharp
[Route("api/v{version:apiVersion}/[controller]")]
public class PrescriptionsController : BaseApiController
{
    // 基础CRUD + 业务操作 (推测，需要确认)
    [HttpGet] // GET api/v1/prescriptions
    [HttpGet("{id}")] // GET api/v1/prescriptions/{id}
    [HttpPost] // POST api/v1/prescriptions
    [HttpPut("{id}")] // PUT api/v1/prescriptions/{id}
    [HttpDelete("{id}")] // DELETE api/v1/prescriptions/{id}
    
    // 业务操作
    [HttpPost("{id}/apply-formula")] // 应用验方
    [HttpPost("{id}/recalculate")] // 重新计算费用
}
```

## 🎯 API接口命名统一规范

### RESTful设计标准 ✅
系统已经采用标准RESTful设计：

| HTTP方法 | 路径模式 | 功能描述 | 响应类型 |
|---------|----------|----------|----------|
| GET | `/api/v1/{resource}` | 获取资源列表(分页) | `ApiResponse<PagedResult<T>>` |
| GET | `/api/v1/{resource}/{id}` | 获取单个资源 | `ApiResponse<T>` |
| POST | `/api/v1/{resource}` | 创建新资源 | `ApiResponse<T>` |
| PUT | `/api/v1/{resource}/{id}` | 更新完整资源 | `ApiResponse<T>` |
| DELETE | `/api/v1/{resource}/{id}` | 删除资源(软删除) | `ApiResponse` |

### 命名规范统一 ✅
- **资源名称**: 使用复数形式 (`herbs`, `formulas`, `prescriptions`)
- **URL大小写**: 全小写 (自动由`[controller]`生成)
- **版本控制**: v1, v2... 数字版本号
- **子资源操作**: 使用动词短语 (`apply-formula`, `recalculate`)

## 🎯 命名统一修正需求

### 处方模块的命名一致性问题
根据之前的分析，处方模块存在命名不统一问题，需要在API层面保持一致：

```csharp
// API响应中的命名统一 (需要确认当前状态)
public class PrescriptionDto
{
    // ✅ 统一后应该使用
    public List<PrescriptionHerbItemDto> Herbs { get; set; } = new();
    
    // ❌ 如果当前使用的是这个，需要修改
    // public List<PrescriptionItemDto> Items { get; set; } = new();
}

// 对应的业务操作API
[HttpPost("{id}/apply-formula")]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ApplyFormula(
    Guid id, [FromBody] ApplyFormulaRequest request)
{
    // 内部调用统一后的Service方法
    var result = await _prescriptionService.ApplyFormulaAsync(id, request.FormulaId);
    return HandleServiceResult(result, "验方应用成功");
}
```

## 🚀 三模块协作API设计

### 验方应用到处方API
```csharp
// POST api/v1/prescriptions/{prescriptionId}/apply-formula
[HttpPost("{prescriptionId}/apply-formula")]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ApplyFormula(
    Guid prescriptionId,
    [FromBody] ApplyFormulaRequest request)
{
    try
    {
        var validation = ValidateGuid<PrescriptionDto>(prescriptionId, "处方ID");
        if (validation != null) return validation;

        var result = await _prescriptionService.ApplyFormulaAsync(prescriptionId, request.FormulaId);
        return HandleServiceResult(result, "验方应用成功");
    }
    catch (Exception ex)
    {
        return HandleException<PrescriptionDto>(ex, "应用验方", prescriptionId);
    }
}

// 请求体
public class ApplyFormulaRequest
{
    public Guid FormulaId { get; set; }
}
```

### 费用重新计算API
```csharp
// POST api/v1/prescriptions/{prescriptionId}/recalculate
[HttpPost("{prescriptionId}/recalculate")]
public async Task<ActionResult<ApiResponse<decimal>>> RecalculateAmount(Guid prescriptionId)
{
    try
    {
        var validation = ValidateGuid<decimal>(prescriptionId, "处方ID");
        if (validation != null) return validation;

        var result = await _prescriptionService.RecalculateAmountAsync(prescriptionId);
        return HandleServiceResult(result, "费用重新计算完成");
    }
    catch (Exception ex)
    {
        return HandleException<decimal>(ex, "重新计算费用", prescriptionId);
    }
}
```

### 药材搜索API (已实现)
```csharp
// GET api/v1/herbs/search?keyword={keyword}
[HttpGet("search")]
public async Task<ActionResult<ApiResponse<List<HerbDto>>>> Search([FromQuery] string keyword)
{
    // 已实现，用于处方配药时搜索药材
}
```

## 📊 API响应格式统一规范

### 标准API响应格式 ✅
系统已经统一使用 `ApiResponse<T>` 格式：

```csharp
// 成功响应
{
    "success": true,
    "message": "操作成功",
    "data": { /* 具体数据 */ },
    "timestamp": "2025-09-01T10:30:00Z",
    "requestId": "req-123456"
}

// 分页响应
{
    "success": true,
    "message": "查询成功",
    "data": {
        "items": [ /* 数据项 */ ],
        "totalCount": 100,
        "pageIndex": 1,
        "pageSize": 20,
        "totalPages": 5
    },
    "timestamp": "2025-09-01T10:30:00Z"
}

// 错误响应
{
    "success": false,
    "message": "操作失败",
    "data": null,
    "errors": ["具体错误信息"],
    "timestamp": "2025-09-01T10:30:00Z"
}
```

### 业务状态码规范 ✅
- **200 OK**: 成功响应
- **400 Bad Request**: 参数验证失败
- **401 Unauthorized**: 未授权访问
- **404 Not Found**: 资源不存在
- **500 Internal Server Error**: 服务器内部错误

## 📝 API文档规范

### Swagger文档注解 ✅
所有API已经包含完整的XML注释：

```csharp
/// <summary>
/// 创建新药材 - 统一API响应格式
/// </summary>
[HttpPost]
public async Task<ActionResult<ApiResponse<HerbDto>>> Create([FromBody] HerbCreateDto dto)
```

### 接口示例文档
每个模块的README.md已经包含完整的API使用示例。

## 🎯 需要确认和完善的内容

### 1. 处方模块API确认
需要确认PrescriptionsController中的具体实现：
- [ ] 确认当前响应中是否使用了统一的`Herbs`命名
- [ ] 确认是否已实现验方应用API
- [ ] 确认是否已实现费用计算API

### 2. 验方模块API确认
需要确认FormulasController中的具体实现：
- [ ] 确认基础CRUD操作是否完整
- [ ] 确认是否有获取验方药材组成的API
- [ ] 确认响应格式中的`Herbs`命名是否统一

### 3. 命名统一实施
- [ ] 确保所有DTO类使用统一的`Herbs`命名
- [ ] 确保API响应数据结构一致
- [ ] 更新前端调用相关代码

## 📊 API测试要点

### 三模块协作测试
1. **验方应用流程**: GET formulas/{id} → POST prescriptions/{id}/apply-formula
2. **药材搜索选择**: GET herbs/search → 添加到处方
3. **费用计算更新**: 修改处方后 → POST prescriptions/{id}/recalculate

### 命名一致性测试
1. **跨模块数据格式**: 确保Formula.Herbs → Prescription.Herbs 数据结构匹配
2. **API响应字段**: 确保三个模块的药材相关字段命名一致
3. **前后端协作**: 确保前端调用使用正确的字段名称

## 📝 开发实施建议

### 当前阶段重点 (遵循用户要求)
根据用户指示"**不做功能扩展，以实现当前需求为前提，精简过多的设计**"：

1. **✅ 优先级最高**: 确认并修正命名不统一问题
2. **✅ 必须完成**: 验证现有API是否正常工作
3. **✅ 需要确认**: 三个模块的关键API是否已实现
4. **❌ 暂不实施**: 复杂的API版本管理、高级搜索功能等

### 实施检查清单
- [ ] 检查PrescriptionsController具体实现
- [ ] 检查FormulasController具体实现
- [ ] 验证API响应中的命名一致性
- [ ] 测试三模块协作API功能
- [ ] 更新相关API文档

---

**文档版本**: v1.0  
**创建时间**: 2025-09-01  
**更新状态**: API规范分析完成，需要进一步确认具体实现