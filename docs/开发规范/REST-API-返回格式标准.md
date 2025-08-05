# REST API 返回格式标准

## 一、概述

本文档定义了 LYBT 系统中 REST API 的标准返回格式，确保 API 的一致性和符合 RESTful 最佳实践。

## 二、HTTP 方法返回格式标准

### 2.1 GET 方法

#### 获取单个资源
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ResourceDto>> GetById(Guid id)
{
    var resource = await _service.GetByIdAsync(id);
    if (resource == null)
    {
        return NotFound(new ProblemDetails 
        { 
            Title = "资源未找到", 
            Detail = "请求的资源不存在" 
        });
    }
    return Ok(resource); // 200 OK + 资源对象
}
```

#### 获取资源列表
```csharp
[HttpGet]
public async Task<ActionResult<List<ResourceDto>>> GetList()
{
    var list = await _service.GetListAsync();
    return Ok(list); // 200 OK + 资源列表
}
```

#### 分页查询
```csharp
[HttpGet("paged")]
public async Task<ActionResult<PaginatedResult<ResourceDto>>> GetPaged([FromQuery] PaginationRequest query)
{
    var result = await _service.GetPagedAsync(query);
    return Ok(result); // 200 OK + 分页结果
}
```

### 2.2 POST 方法

#### 创建资源
```csharp
[HttpPost]
public async Task<ActionResult<ResourceDto>> Create([FromBody] CreateDto dto)
{
    var created = await _service.CreateAsync(dto);
    return Ok(created); // 200 OK + 创建的资源
    // 或者
    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created); // 201 Created
}
```

#### 业务操作
```csharp
[HttpPost("{id}/disable")]
public async Task<ActionResult<ResourceDto>> Disable(Guid id)
{
    var updated = await _service.DisableAsync(id);
    return Ok(updated); // 200 OK + 更新后的资源
}
```

### 2.3 PUT 方法

**标准格式：返回更新后的完整资源**

```csharp
[HttpPut("{id}")]
public async Task<ActionResult<ResourceDto>> Update(Guid id, [FromBody] UpdateDto dto)
{
    if (id != dto.Id)
    {
        return BadRequest(new ProblemDetails 
        { 
            Title = "参数错误", 
            Detail = "URL中的ID与请求体中的ID不匹配" 
        });
    }

    var updated = await _service.UpdateAsync(dto);
    if (updated == null)
    {
        return NotFound(new ProblemDetails 
        { 
            Title = "资源未找到", 
            Detail = "要更新的资源不存在" 
        });
    }

    return Ok(updated); // 200 OK + 更新后的资源
}
```

❌ **错误示例**：
```csharp
// 不要返回简单消息
return Ok(new { message = "更新成功" });
```

### 2.4 DELETE 方法

**标准格式：返回 204 No Content 或删除的资源**

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id)
{
    var result = await _service.DeleteAsync(id);
    if (!result)
    {
        return NotFound(new ProblemDetails 
        { 
            Title = "资源未找到", 
            Detail = "要删除的资源不存在" 
        });
    }

    return NoContent(); // 204 No Content（推荐）
}
```

**软删除返回资源**：
```csharp
[HttpDelete("{id}")]
public async Task<ActionResult<ResourceDto>> SoftDelete(Guid id)
{
    var deleted = await _service.SoftDeleteAsync(id);
    if (deleted == null)
    {
        return NotFound(new ProblemDetails 
        { 
            Title = "资源未找到", 
            Detail = "要删除的资源不存在" 
        });
    }

    return Ok(deleted); // 200 OK + 标记为删除的资源
}
```

### 2.5 PATCH 方法

**用于资源的部分更新**

```csharp
[HttpPatch("{id}")]
public async Task<ActionResult<ResourceDto>> Patch(Guid id, [FromBody] JsonPatchDocument<UpdateDto> patchDoc)
{
    var updated = await _service.PatchAsync(id, patchDoc);
    if (updated == null)
    {
        return NotFound(new ProblemDetails 
        { 
            Title = "资源未找到", 
            Detail = "要更新的资源不存在" 
        });
    }

    return Ok(updated); // 200 OK + 更新后的资源
}
```

**批量状态更新**：
```csharp
[HttpPatch("batch-status")]
public async Task<ActionResult<BatchUpdateResult>> BatchUpdateStatus([FromBody] BatchStatusUpdateDto dto)
{
    var result = await _service.BatchUpdateStatusAsync(dto);
    return Ok(result); // 200 OK + 批量更新结果
}
```

## 三、错误响应格式

### 3.1 使用 Problem Details 标准

所有错误响应都应使用 RFC 7807 Problem Details 格式：

```csharp
return BadRequest(new ProblemDetails 
{
    Title = "参数错误",
    Detail = "详细的错误信息",
    Status = 400,
    Instance = HttpContext.Request.Path
});
```

### 3.2 常见错误响应

#### 400 Bad Request
```csharp
return BadRequest(new ProblemDetails 
{ 
    Title = "参数错误", 
    Detail = "具体的参数错误信息" 
});
```

#### 404 Not Found
```csharp
return NotFound(new ProblemDetails 
{ 
    Title = "资源未找到", 
    Detail = "请求的资源不存在" 
});
```

#### 409 Conflict
```csharp
return Conflict(new ProblemDetails 
{ 
    Title = "资源冲突", 
    Detail = "资源已存在或状态冲突" 
});
```

#### 500 Internal Server Error
```csharp
return StatusCode(500, new ProblemDetails 
{ 
    Title = "服务器内部错误", 
    Detail = "处理请求时发生错误" 
});
```

## 四、特殊场景处理

### 4.1 批量操作返回格式

```csharp
public class BatchOperationResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; }
    public List<Guid> ProcessedIds { get; set; }
}
```

### 4.2 业务操作返回格式

对于改变资源状态的业务操作，返回更新后的资源：

```csharp
[HttpPost("{id}/complete")]
public async Task<ActionResult<OrderDto>> Complete(Guid id)
{
    var completed = await _service.CompleteOrderAsync(id);
    return Ok(completed); // 返回完整的资源对象
}
```

### 4.3 无返回值操作

某些操作可能不需要返回值：

```csharp
[HttpPost("cleanup")]
public async Task<IActionResult> Cleanup()
{
    await _service.CleanupAsync();
    return NoContent(); // 204 No Content
}
```

## 五、迁移指南

### 5.1 PUT 方法迁移

**现有代码**：
```csharp
public async Task<ActionResult<object>> Update([FromBody] UpdateDto dto)
{
    var result = await _service.UpdateAsync(dto);
    if (!result)
        return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "更新失败" });
    
    return Ok(new { message = "更新成功" });
}
```

**迁移后**：
```csharp
public async Task<ActionResult<ResourceDto>> Update([FromBody] UpdateDto dto)
{
    var updated = await _service.UpdateAsync(dto);
    if (updated == null)
        return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "要更新的资源不存在" });
    
    return Ok(updated);
}
```

### 5.2 DELETE 方法迁移

**现有代码**：
```csharp
public async Task<ActionResult<object>> Delete(Guid id)
{
    var result = await _service.DeleteAsync(id);
    if (!result)
        return NotFound();
    
    return Ok(new { message = "删除成功" });
}
```

**迁移后**：
```csharp
public async Task<IActionResult> Delete(Guid id)
{
    var result = await _service.DeleteAsync(id);
    if (!result)
        return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "要删除的资源不存在" });
    
    return NoContent();
}
```

## 六、最佳实践总结

1. **GET** - 返回资源或资源列表
2. **POST** - 创建资源时返回创建的资源
3. **PUT** - 返回更新后的完整资源
4. **DELETE** - 返回 204 No Content（物理删除）或返回资源（软删除）
5. **PATCH** - 返回部分更新后的资源

6. **统一错误处理** - 使用 Problem Details 格式
7. **避免返回简单消息** - 返回实际的资源对象
8. **保持一致性** - 所有 API 遵循相同的模式
9. **文档化** - 清晰记录每个端点的返回格式

## 七、示例控制器

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : BaseController
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService, IMemoryCache cache, ILogger<ProductsController> logger)
        : base(logger, cache)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetList()
    {
        var list = await _productService.GetListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "产品不存在" });
        
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        var created = await _productService.CreateAsync(dto);
        return Ok(created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "ID不匹配" });

        var updated = await _productService.UpdateAsync(dto);
        if (updated == null)
            return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "产品不存在" });

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _productService.DeleteAsync(id);
        if (!result)
            return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "产品不存在" });

        return NoContent();
    }
}
```