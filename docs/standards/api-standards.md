# API 设计规范

本文档定义了 LYBTZYZS 项目的 RESTful API 设计规范。

## 基本原则

1. **RESTful** - 遵循 REST 架构风格
2. **一致性** - 保持 API 命名和结构的一致性
3. **版本化** - 支持 API 版本管理
4. **安全性** - 所有 API 都需要认证和授权
5. **文档化** - 使用 Swagger 自动生成 API 文档

## URL 结构

### 基本格式

```
https://api.example.com/api/v{version}/{resource}/{id}/{action}
```

示例：
- `GET /api/v1/patients` - 获取患者列表
- `GET /api/v1/patients/123` - 获取特定患者
- `POST /api/v1/patients` - 创建新患者
- `PUT /api/v1/patients/123` - 更新患者信息
- `DELETE /api/v1/patients/123` - 删除患者
- `POST /api/v1/patients/123/disable` - 禁用患者（特殊操作）

### 命名规范

- 使用小写字母
- 使用连字符分隔单词（kebab-case）
- 使用名词复数形式表示资源
- 避免使用动词（除非是特殊操作）

```
✅ 正确：/api/v1/patient-records
❌ 错误：/api/v1/PatientRecords
❌ 错误：/api/v1/getPatientRecords
```

## HTTP 方法

| 方法 | 用途 | 幂等性 | 安全性 |
|------|------|--------|--------|
| GET | 获取资源 | ✅ | ✅ |
| POST | 创建资源 | ❌ | ❌ |
| PUT | 完整更新资源 | ✅ | ❌ |
| PATCH | 部分更新资源 | ✅ | ❌ |
| DELETE | 删除资源 | ✅ | ❌ |

## 请求和响应

### 请求头

```http
Content-Type: application/json
Accept: application/json
Authorization: Bearer {token}
X-Request-ID: {uuid}
```

### 统一响应格式

```json
{
  "success": true,
  "data": {
    // 实际数据
  },
  "message": "操作成功",
  "timestamp": "2024-01-29T10:30:00Z",
  "traceId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 错误响应格式

```json
{
  "success": false,
  "error": {
    "code": "PATIENT_NOT_FOUND",
    "message": "患者不存在",
    "details": {
      "patientId": "123"
    }
  },
  "timestamp": "2024-01-29T10:30:00Z",
  "traceId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## 状态码使用

| 状态码 | 含义 | 使用场景 |
|--------|------|----------|
| 200 | OK | GET 请求成功，PUT/PATCH 更新成功 |
| 201 | Created | POST 创建资源成功 |
| 204 | No Content | DELETE 删除成功 |
| 400 | Bad Request | 请求参数错误 |
| 401 | Unauthorized | 未认证 |
| 403 | Forbidden | 无权限 |
| 404 | Not Found | 资源不存在 |
| 409 | Conflict | 资源冲突（如重复创建） |
| 422 | Unprocessable Entity | 请求格式正确但语义错误 |
| 500 | Internal Server Error | 服务器内部错误 |

## 分页

### 请求参数

```
GET /api/v1/patients?page=1&pageSize=20&sortBy=createdAt&sortOrder=desc
```

### 响应格式

```json
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 100,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

## 过滤和搜索

### 简单过滤

```
GET /api/v1/patients?status=active&gender=male
```

### 复杂查询

```
GET /api/v1/patients?filter=age>18,status=active&search=张
```

### 字段选择

```
GET /api/v1/patients?fields=id,name,phone
```

## 版本管理

### URL 版本控制

```
/api/v1/patients
/api/v2/patients
```

### 版本迁移策略

1. 新版本发布后，旧版本继续维护至少 6 个月
2. 在响应头中提示版本废弃信息
3. 文档中明确标注版本支持状态

```http
X-API-Deprecation-Date: 2024-12-31
X-API-Deprecation-Info: Please migrate to v2
```

## 安全规范

### 认证

- 使用 JWT Bearer Token
- Token 有效期：8 小时
- Refresh Token 有效期：30 天

### 授权

- 基于角色的访问控制（RBAC）
- 在控制器或方法级别使用 `[Authorize]` 特性
- 敏感操作需要额外的权限验证

### 数据验证

```csharp
[HttpPost]
public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    
    // 业务逻辑
}
```

## API 文档

### Swagger 配置

```csharp
/// <summary>
/// 创建患者档案
/// </summary>
/// <param name="dto">患者信息</param>
/// <returns>创建的患者信息</returns>
/// <response code="201">创建成功</response>
/// <response code="400">请求参数错误</response>
/// <response code="401">未授权</response>
[HttpPost]
[ProducesResponseType(typeof(ApiResponse<PatientDto>), 201)]
[ProducesResponseType(typeof(ApiResponse<object>), 400)]
[ProducesResponseType(401)]
public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto)
{
    // 实现
}
```

## 最佳实践

1. **幂等性** - GET、PUT、DELETE 操作应该是幂等的
2. **无状态** - API 不应该在服务器端保存客户端状态
3. **缓存** - 合理使用 HTTP 缓存头
4. **限流** - 实施 API 调用频率限制
5. **日志** - 记录所有 API 调用日志
6. **监控** - 监控 API 性能和可用性

## 示例

### 完整的控制器示例

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PatientsController : BaseController
{
    private readonly IPatientService _patientService;
    
    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }
    
    /// <summary>
    /// 获取患者列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientDto>>), 200)]
    public async Task<IActionResult> GetPatients([FromQuery] PatientQueryDto query)
    {
        var result = await _patientService.GetPatientsAsync(query);
        return Ok(ApiResponse<PagedResult<PatientDto>>.Success(result));
    }
    
    /// <summary>
    /// 获取患者详情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetPatient(Guid id)
    {
        var patient = await _patientService.GetByIdAsync(id);
        if (patient == null)
        {
            return NotFound(ApiResponse<object>.Fail("患者不存在"));
        }
        return Ok(ApiResponse<PatientDto>.Success(patient));
    }
}
```

---

遵循这些规范将确保 API 的一致性、可维护性和易用性。