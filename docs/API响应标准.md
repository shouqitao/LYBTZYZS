# API 响应标准

本文档定义了凌隐宝堂中医诊所诊疗系统 (LYBTZYZS) 的 API 响应格式标准。

## 目的

统一后端 API 的响应格式，确保前后端数据交互的一致性，提高开发效率和代码可维护性。

## 响应格式标准

### 1. POST 方法（创建资源）

**成功响应**：
- 状态码：200 OK
- 响应体：返回创建的完整对象
- 格式：`返回类型为具体的 DTO 对象（如 HerbDto, PatientDto, FormulaTemplateDto 等）`

```csharp
// 控制器示例
[HttpPost]
public async Task<ActionResult<HerbDto>> Create([FromBody] HerbCreateDto dto) {
    var result = await _service.AddAsync(dto);
    if (result != null) {
        _logger.LogInformation("资源创建成功");
        return Ok(result);  // 返回创建的对象
    }
    return BadRequest(new ProblemDetails { ... });
}

// 前端 API 服务接口示例
[Post("/api/v1/herbs")]
Task<Refit.ApiResponse<HerbDto>> CreateHerbAsync([Body] HerbCreateDto dto);
```

**失败响应**：
- 状态码：400 Bad Request / 401 Unauthorized / 403 Forbidden / 500 Internal Server Error
- 响应体：ProblemDetails 对象

### 2. PUT 方法（更新资源）

**成功响应**：
- 状态码：200 OK
- 响应体：`{ message: "更新成功" }`

```csharp
// 控制器示例
[HttpPut("{id}")]
public async Task<ActionResult<object>> Update(Guid id, [FromBody] UpdateDto dto) {
    var result = await _service.UpdateAsync(dto);
    if (result) {
        return Ok(new { message = "更新成功" });
    }
    return BadRequest(new ProblemDetails { ... });
}
```

**失败响应**：
- 状态码：400 Bad Request / 404 Not Found / 500 Internal Server Error
- 响应体：ProblemDetails 对象

### 3. DELETE 方法（删除资源）

**成功响应**：
- 状态码：200 OK
- 响应体：`{ message: "删除成功" }`

```csharp
// 控制器示例
[HttpDelete("{id}")]
public async Task<ActionResult<object>> Delete(Guid id) {
    var result = await _service.DeleteAsync(id);
    if (result) {
        return Ok(new { message = "删除成功" });
    }
    return NotFound(new ProblemDetails { ... });
}
```

**失败响应**：
- 状态码：404 Not Found / 400 Bad Request / 500 Internal Server Error
- 响应体：ProblemDetails 对象

### 4. GET 方法（查询资源）

**成功响应**：
- 状态码：200 OK
- 响应体：返回查询的数据（单个对象、列表或分页结果）

```csharp
// 获取单个资源
[HttpGet("{id}")]
public async Task<ActionResult<HerbDto>> GetById(Guid id) {
    var result = await _service.GetByIdAsync(id);
    if (result != null) {
        return Ok(result);
    }
    return NotFound(new ProblemDetails { ... });
}

// 获取列表
[HttpGet]
public async Task<ActionResult<List<HerbDto>>> GetList() {
    var result = await _service.GetListAsync();
    return Ok(result);
}

// 分页查询
[HttpPost("paged")]
public async Task<ActionResult<PaginatedResult<HerbDto>>> GetPaged([FromBody] QueryDto query) {
    var result = await _service.GetPagedAsync(query);
    return Ok(result);
}
```

**失败响应**：
- 状态码：404 Not Found / 400 Bad Request / 500 Internal Server Error
- 响应体：ProblemDetails 对象

### 5. PATCH 方法（部分更新）

**成功响应**：
- 状态码：200 OK
- 响应体：`{ message: "操作成功" }`

```csharp
// 状态切换示例
[HttpPatch("{id}/toggle-status")]
public async Task<ActionResult<object>> ToggleStatus(Guid id) {
    var result = await _service.ToggleStatusAsync(id);
    if (result) {
        return Ok(new { message = "状态切换成功" });
    }
    return BadRequest(new ProblemDetails { ... });
}
```

## ProblemDetails 标准格式

所有错误响应都应使用 ASP.NET Core 的 ProblemDetails 格式：

```json
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "参数验证失败",
    "status": 400,
    "detail": "药材名称不能为空",
    "instance": "/api/v1/herbs"
}
```

## 服务层返回类型标准

### 创建操作
- 服务接口：`Task<TDto?> AddAsync(...)`
- 返回创建的对象，失败返回 null

### 更新操作
- 服务接口：`Task<bool> UpdateAsync(...)`
- 返回 true/false 表示成功/失败

### 删除操作
- 服务接口：`Task<bool> DeleteAsync(...)`
- 返回 true/false 表示成功/失败

### 查询操作
- 单个对象：`Task<TDto?> GetByIdAsync(...)`
- 列表：`Task<List<TDto>> GetListAsync(...)`
- 分页：`Task<PaginatedResult<TDto>> GetPagedAsync(...)`

## 前端服务接口标准

前端 Refit API 服务接口应与后端返回类型保持一致：

```csharp
// POST - 返回创建的对象
Task<Refit.ApiResponse<HerbDto>> CreateHerbAsync([Body] HerbCreateDto dto);

// PUT - 返回操作结果消息
Task<Refit.ApiResponse<object>> UpdateHerbAsync([Body] HerbUpdateDto dto);

// DELETE - 返回操作结果消息
Task<Refit.ApiResponse<object>> DeleteHerbAsync(Guid id);

// GET - 返回查询的数据
Task<Refit.ApiResponse<HerbDto>> GetHerbByIdAsync(Guid id);
Task<Refit.ApiResponse<List<HerbDto>>> GetHerbsAsync();
```

## 迁移指南

对于现有代码的迁移：

1. **POST 方法迁移**：
   - 将 `StatusCode(201, new { message = "xxx" })` 改为 `Ok(createdObject)`
   - 修改服务层方法返回创建的对象而不是 bool

2. **前端接口迁移**：
   - 将 POST 方法的返回类型从 `object` 改为具体的 DTO 类型

3. **错误处理**：
   - 统一使用 ProblemDetails 格式返回错误信息

## 注意事项

1. 不再使用 201 Created 状态码，统一使用 200 OK
2. POST 方法必须返回创建的完整对象，便于前端获取生成的 ID 等信息
3. 所有操作都应记录日志
4. 保持前后端接口定义的一致性