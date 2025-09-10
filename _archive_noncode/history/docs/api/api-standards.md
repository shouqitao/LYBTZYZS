# API接口设计标准

## 📋 API设计概述

凌隐宝堂中医诊所诊疗系统API基于ASP.NET Core Web API设计，遵循RESTful架构标准，所有接口统一使用 `ApiResponse<T>` 响应格式。

**设计原则**：
- **RESTful标准** - HTTP动词语义化，资源化URL设计
- **统一响应格式** - 所有接口使用ApiResponse包装
- **版本控制** - URL路径版本控制 (`/api/v1/`)
- **统一异常处理** - 通过BaseApiController统一处理
- **JWT认证** - Bearer Token认证，8小时有效期
- **参数验证** - 统一的参数验证和错误返回

## 🏗️ API架构设计

### 控制器继承体系

```
BaseControllerCore (核心基础层)
├── BaseApiController (业务API层) - 8个核心业务模块
│   ├── AuthController, UsersController, PatientsController
│   ├── ConsultationController, MedicalCaseController
│   ├── PrescriptionsController, HerbsController, FormulasController
│   └── HerbImportExportController
└── BaseSystemController (系统管理层) - 5个系统管理模块
    ├── HealthController, MonitoringController
    └── SecurityController, TestController, UnifiedConfigController
```

### 路由命名规范

| 模式 | 示例 | 说明 |
|------|------|------|
| **资源集合** | `GET /api/v1/herbs` | 获取资源列表，支持分页 |
| **单个资源** | `GET /api/v1/herbs/{id}` | 获取特定资源详情 |
| **创建资源** | `POST /api/v1/herbs` | 创建新资源 |
| **更新资源** | `PUT /api/v1/herbs/{id}` | 完整更新资源 |
| **删除资源** | `DELETE /api/v1/herbs/{id}` | 删除资源(软删除) |
| **子资源操作** | `POST /api/v1/prescriptions/{id}/apply-formula` | 业务操作 |
| **搜索功能** | `GET /api/v1/herbs/search?keyword=` | 搜索特定资源 |

## 📊 统一响应格式

### ApiResponse<T> 标准格式

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; }
    public string? RequestId { get; set; }
}
```

### 响应示例

#### 成功响应
```json
{
    "success": true,
    "message": "查询成功",
    "data": {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "name": "当归",
        "price": 15.50
    },
    "timestamp": "2025-09-01T10:30:00Z",
    "requestId": "req-123456"
}
```

#### 分页响应
```json
{
    "success": true,
    "message": "查询成功",
    "data": {
        "items": [
            {"id": "...", "name": "当归"},
            {"id": "...", "name": "川芎"}
        ],
        "totalCount": 150,
        "pageIndex": 1,
        "pageSize": 20,
        "totalPages": 8
    },
    "timestamp": "2025-09-01T10:30:00Z"
}
```

#### 错误响应
```json
{
    "success": false,
    "message": "参数验证失败",
    "data": null,
    "errors": [
        "药材名称不能为空",
        "价格必须大于0"
    ],
    "timestamp": "2025-09-01T10:30:00Z"
}
```

## 🎯 HTTP状态码标准

| 状态码 | 说明 | 使用场景 |
|--------|------|----------|
| **200 OK** | 请求成功 | 查询、更新、删除成功 |
| **201 Created** | 资源创建成功 | POST创建资源成功 |
| **400 Bad Request** | 参数错误 | 参数验证失败、业务规则错误 |
| **401 Unauthorized** | 认证失败 | JWT Token无效或过期 |
| **403 Forbidden** | 权限不足 | 角色权限不满足要求 |
| **404 Not Found** | 资源不存在 | 查询的资源ID不存在 |
| **409 Conflict** | 业务冲突 | 重复创建、状态冲突 |
| **500 Internal Server Error** | 服务器错误 | 系统内部异常 |

## 🔐 认证与授权

### JWT认证配置
```csharp
[Authorize] // 所有业务API需要认证
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class HerbsController : BaseApiController
```

### 权限控制
```csharp
[Authorize(Roles = "Admin")] // 管理员权限
[Authorize(Roles = "Admin,Doctor")] // 多角色权限
```

### Token使用
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## 📝 核心业务API接口

### 1. 认证模块 (AuthController)

#### 用户登录
```http
POST /api/v1/auth/login
Content-Type: application/json

{
    "username": "doctor01",
    "password": "password123",
    "rememberMe": false
}
```

#### 响应示例
```json
{
    "success": true,
    "message": "登录成功",
    "data": {
        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "user": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "username": "doctor01",
            "realName": "张医生",
            "role": "Doctor"
        },
        "expiresAt": "2025-09-01T18:30:00Z"
    }
}
```

#### 退出登录
```http
POST /api/v1/auth/logout
Authorization: Bearer {token}
Content-Type: application/json

{
    "username": "doctor01"
}
```

#### Token刷新
```http
POST /api/v1/auth/refresh
Content-Type: application/json

"refresh-token-here"
```

#### Token验证
```http
POST /api/v1/auth/validate
Content-Type: application/json

"token-to-validate"
```

#### 修改系统管理员密码
```http
POST /api/v1/auth/changeSysAdminPassword
Authorization: Bearer {token}
Content-Type: application/json

{
    "newPassword": "NewPassword123!"
}
```

### 2. 药材模块 (HerbsController)

#### 获取药材列表
```http
GET /api/v1/herbs?page=1&pageSize=20&keyword=当归
Authorization: Bearer {token}
```

#### 创建药材
```http
POST /api/v1/herbs
Authorization: Bearer {token}
Content-Type: application/json

{
    "name": "当归",
    "pinYinCode": "DG",
    "origin": "甘肃",
    "spec": "统片",
    "unit": "克",
    "price": 15.50,
    "costPrice": 12.00,
    "effect": "补血活血",
    "usage": "煎服9-15g",
    "remark": "质量优良"
}
```

#### 药材搜索 (供处方使用)
```http
GET /api/v1/herbs/search?keyword=当
Authorization: Bearer {token}
```

#### 获取药材分类
```http
GET /api/v1/herbs/categories
Authorization: Bearer {token}
```

#### 批量更新状态
```http
PATCH /api/v1/herbs/batch-status
Authorization: Bearer {token}
Content-Type: application/json

{
    "herbIds": ["id1", "id2", "id3"],
    "status": "Disabled"
}
```

#### 批量更新价格
```http
PATCH /api/v1/herbs/batch-price
Authorization: Bearer {token}
Content-Type: application/json

{
    "updates": [
        {
            "herbId": "id1",
            "newPrice": 25.00
        },
        {
            "herbId": "id2", 
            "newPrice": 30.00
        }
    ]
}
```

### 3. 验方模块 (FormulasController)

#### 获取验方列表
```http
GET /api/v1/formulas?page=1&pageSize=20&keyword=四物汤&category=补血剂
Authorization: Bearer {token}
```

#### 创建验方
```http
POST /api/v1/formulas
Authorization: Bearer {token}
Content-Type: application/json

{
    "name": "四物汤",
    "effect": "补血调经",
    "usage": "水煎服，日一剂",
    "property": "补血之代表方",
    "isShared": true,
    "herbs": [
        {
            "herbId": "...",
            "herbName": "当归",
            "quantity": 9,
            "unit": "g",
            "usage": "后下"
        }
    ]
}
```

#### 获取验方模板
```http
GET /api/v1/formulas/templates
Authorization: Bearer {token}
```

#### 验方推荐
```http
GET /api/v1/formulas/recommendations?symptoms=月经不调&diagnosis=血虚&doctorId={doctorId}
Authorization: Bearer {token}
```

#### 从处方创建验方
```http
POST /api/v1/formulas/from-prescription/{prescriptionId}
Authorization: Bearer {token}
Content-Type: application/json

{
    "name": "自定义验方名称"
}
```

#### 验方分析 (智能功能)
```http
POST /api/v1/formulas/{id}/analyze
Authorization: Bearer {token}
```

#### 响应示例 (分析结果)
```json
{
    "success": true,
    "message": "分析完成",
    "data": {
        "safetyLevel": "Safe",
        "complexity": "Medium", 
        "estimatedCost": 45.50,
        "warnings": [],
        "compatibility": "Compatible",
        "recommendations": ["建议配伍生姜"]
    }
}
```

#### 获取验方推荐 (按症候)
```http
GET /api/v1/formulas/recommendations/syndrome/风寒表证
Authorization: Bearer {token}
```

#### 复制验方
```http
POST /api/v1/formulas/{id}/copy
Authorization: Bearer {token}
Content-Type: application/json

{
    "newName": "麻黄汤加减方"
}
```

#### 切换验方状态
```http
POST /api/v1/formulas/{id}/toggle-status
Authorization: Bearer {token}
```

#### 获取验方分类
```http
GET /api/v1/formulas/categories
Authorization: Bearer {token}
```

#### 分享验方
```http
POST /api/v1/formulas/{id}/share
Authorization: Bearer {token}
```

#### 取消分享验方
```http
POST /api/v1/formulas/{id}/unshare
Authorization: Bearer {token}
```

#### 搜索验方 (分页)
```http
GET /api/v1/formulas/search?page=1&pageSize=20&keyword=麻黄
Authorization: Bearer {token}
```

#### 根据类型获取验方
```http
GET /api/v1/formulas/by-type/解表剂
Authorization: Bearer {token}
```

### 4. 处方模块 (PrescriptionsController)

#### 获取处方列表
```http
GET /api/v1/prescriptions?page=1&pageSize=20&patientId={patientId}
Authorization: Bearer {token}
```

#### 创建处方
```http
POST /api/v1/prescriptions
Authorization: Bearer {token}
Content-Type: application/json

{
    "medicalCaseId": "...",
    "patientId": "...",
    "indication": "血虚月经不调",
    "dosageCount": 7,
    "discount": 1.0,
    "advice": "饭后温服",
    "formulaSource": "四物汤加减",
    "herbs": [
        {
            "herbId": "...",
            "herbName": "当归",
            "quantity": 9,
            "unit": "g",
            "unitPrice": 15.50,
            "usage": "后下"
        }
    ]
}
```

#### 应用验方到处方 (三模块协作核心)
```http
POST /api/v1/prescriptions/{prescriptionId}/apply-formula
Authorization: Bearer {token}
Content-Type: application/json

{
    "formulaId": "123e4567-e89b-12d3-a456-426614174000"
}
```

#### 重新计算处方费用
```http
POST /api/v1/prescriptions/{prescriptionId}/recalculate
Authorization: Bearer {token}
```

#### 根据患者获取处方历史
```http
GET /api/v1/prescriptions/patient/{patientId}
Authorization: Bearer {token}
```

#### 根据医案获取处方记录  
```http
GET /api/v1/prescriptions/medical-case/{caseId}
Authorization: Bearer {token}
```

#### 高级搜索处方
```http
POST /api/v1/prescriptions/search
Authorization: Bearer {token}
Content-Type: application/json

{
    "keyword": "感冒",
    "symptoms": ["头痛", "恶寒"],
    "herbs": ["桂枝", "白芍"],
    "dateRange": ["2025-01-01", "2025-12-31"],
    "minPrice": 50,
    "maxPrice": 200
}
```

#### 复制处方
```http
POST /api/v1/prescriptions/{id}/copy
Authorization: Bearer {token}
Content-Type: application/json

{
    "newName": "复制的处方"
}
```

#### 验证处方 (智能功能)
```http
POST /api/v1/prescriptions/validate
Authorization: Bearer {token}
Content-Type: application/json

{
    "patientId": "patient-id",
    "prescriptionItems": [
        {
            "herbId": "herb-id",
            "dosage": 10,
            "unit": "g"
        }
    ],
    "checkCompatibility": true,
    "checkDosage": true
}
```

### 5. 患者模块 (PatientsController)

#### 获取患者列表
```http
GET /api/v1/patients?page=1&pageSize=20&keyword=张三
Authorization: Bearer {token}
```

#### 创建患者
```http
POST /api/v1/patients
Authorization: Bearer {token}
Content-Type: application/json

{
    "name": "张三",
    "pinYinCode": "ZS",
    "gender": "Male",
    "birthDate": "1985-06-15",
    "idType": "IdCard",
    "idNumber": "123456789012345678",
    "phoneNumber": "13800138000",
    "address": "北京市朝阳区",
    "allergyHistory": "青霉素过敏"
}
```

#### 搜索患者
```http
POST /api/v1/patients/search
Authorization: Bearer {token}
Content-Type: application/json

{
    "keyword": "张三",
    "ageRange": [30, 60],
    "gender": "Male",
    "city": "北京"
}
```

#### 获取患者统计
```http
GET /api/v1/patients/statistics
Authorization: Bearer {token}
```

#### 导出患者数据
```http
POST /api/v1/patients/export
Authorization: Bearer {token}
Content-Type: application/json

{
    "format": "Excel",
    "patientIds": ["id1", "id2"],
    "includeHistory": true
}
```

#### 导入患者数据
```http
POST /api/v1/patients/import
Authorization: Bearer {token}
Content-Type: multipart/form-data

{
    "file": [Excel文件],
    "skipDuplicates": true,
    "validateOnly": false
}

### 6. 医疗案例模块 (MedicalCaseController)

#### 创建医疗案例
```http
POST /api/v1/medicalcases
Authorization: Bearer {token}
Content-Type: application/json

{
    "patientId": "...",
    "doctorId": "...",
    "remark": "初诊"
}
```

#### 完成医疗案例
```http
POST /api/v1/medicalcases/{id}/complete
Authorization: Bearer {token}
```

### 7. 看诊模块 (ConsultationController)

#### 创建看诊记录
```http
POST /api/v1/consultations
Authorization: Bearer {token}
Content-Type: application/json

{
    "medicalCaseId": "...",
    "patientId": "...",
    "chiefComplaint": "月经不调3月余",
    "presentIllness": "患者3月前...",
    "inspection": "面色淡白...",
    "auscultationOlfaction": "语声低微...",
    "inquiry": "月经周期延后...",
    "palpation": "脉细弱...",
    "tcmDiagnosis": "月经不调（血虚证）",
    "treatmentPrinciple": "补血调经",
    "medicalAdvice": "忌生冷"
}
```

### 8. 用户管理模块 (UsersController)

#### 获取用户列表
```http
GET /api/v1/users?page=1&pageSize=20&role=Doctor
Authorization: Bearer {token}
```

#### 创建医生用户
```http
POST /api/v1/users
Authorization: Bearer {token}
Content-Type: application/json

{
    "username": "doctor02",
    "realName": "李医生",
    "phoneNumber": "13900139000",
    "email": "doctor02@clinic.com",
    "role": "Doctor",
    "specialty": "妇科",
    "registrationFee": 50.00,
    "licenseNumber": "110123456789",
    "introduction": "专治妇科疾病"
}
```

#### 切换用户状态
```http
PATCH /api/v1/users/{id}/toggle-status
Authorization: Bearer {token}
```

#### 重置用户密码
```http
POST /api/v1/users/reset-password/{id}
Authorization: Bearer {token}
```

#### 修改密码
```http
PATCH /api/v1/users/password
Authorization: Bearer {token}
Content-Type: application/json

{
    "oldPassword": "OldPass123!",
    "newPassword": "NewPass123!"
}
```

#### 获取个人信息
```http
GET /api/v1/users/profile
Authorization: Bearer {token}
```

#### 修改个人信息
```http
PUT /api/v1/users/profile
Authorization: Bearer {token}
Content-Type: application/json

{
    "realName": "更新姓名",
    "email": "updated@email.com",
    "phoneNumber": "13900000000"
}
```

#### 获取角色列表
```http
GET /api/v1/users/roles
Authorization: Bearer {token}
```

#### 获取活跃用户列表
```http
GET /api/v1/users/active
Authorization: Bearer {token}
```

## 🔧 系统管理API

### 健康检查 (HealthController)
```http
GET /api/v1/health
```

### 系统监控 (MonitoringController)
```http
GET /api/v1/monitoring/status
Authorization: Bearer {admin-token}
```

### 安全管理 (SecurityController)  
```http
GET /api/v1/security/audit-logs
Authorization: Bearer {admin-token}
```

### 调试接口 (DebugController)
```http
GET /api/v1/debug/info
```

### 测试接口 (TestController)
```http
GET /api/v1/test/health
```

### 统一配置 (UnifiedConfigController)
```http
GET /api/v1/config/system
Authorization: Bearer {admin-token}
```

### 中药材导入导出 (HerbImportExportController)
```http
POST /api/v1/herb-import-export/import
Authorization: Bearer {token}
Content-Type: multipart/form-data

{
    "file": [Excel文件]
}

GET /api/v1/herb-import-export/export
Authorization: Bearer {token}
```

## 🔍 查询参数标准

### 分页参数
- `page`: 页码，从1开始
- `pageSize`: 每页记录数，1-100
- `keyword`: 搜索关键词

### 筛选参数
- `status`: 状态筛选 (Enabled/Disabled)
- `category`: 分类筛选
- `startDate`/`endDate`: 时间范围筛选

### 排序参数
- `sortBy`: 排序字段
- `sortDirection`: 排序方向 (asc/desc)

### 示例
```http
GET /api/v1/herbs?page=1&pageSize=20&keyword=当归&category=补血类&status=Enabled&sortBy=name&sortDirection=asc
```

## ⚠️ 错误码标准

### 实际ApiErrorCodes (已实现)

| 错误码 | 说明 | HTTP状态码 |
|--------|------|------------|
| `AUTHENTICATION_FAILED` | 认证失败 | 401 |
| `TOKEN_EXPIRED` | Token过期 | 401 |
| `INVALID_CREDENTIALS` | 凭证无效 | 401 |
| `USER_NOT_FOUND` | 用户不存在 | 404 |
| `USERNAME_EXISTS` | 用户名已存在 | 409 |
| `PASSWORD_CHANGE_FAILED` | 密码修改失败 | 400 |
| `DATA_VALIDATION_FAILED` | 数据验证失败 | 400 |
| `DATA_SAVE_FAILED` | 数据保存失败 | 500 |
| `DATA_UPDATE_FAILED` | 数据更新失败 | 500 |
| `DATA_DELETE_FAILED` | 数据删除失败 | 500 |
| `PATIENT_NOT_FOUND` | 患者不存在 | 404 |
| `PRESCRIPTION_NOT_FOUND` | 处方不存在 | 404 |
| `HERB_NOT_FOUND` | 药材不存在 | 404 |
| `FORMULA_NOT_FOUND` | 验方不存在 | 404 |
| `INTERNAL_ERROR` | 内部错误 | 500 |
| `SERVICE_UNAVAILABLE` | 服务不可用 | 503 |

## 🔧 开发规范

### Controller开发标准

#### 1. 类定义
```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ExampleController : BaseApiController
{
    private readonly IExampleService _service;

    public ExampleController(
        IExampleService service,
        ILogger<ExampleController> logger,
        IMemoryCache cache) : base(logger, cache)
    {
        _service = service;
    }
}
```

#### 2. 方法模板
```csharp
/// <summary>
/// 获取资源详情 - 统一API响应格式
/// </summary>
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<ResourceDto>>> GetById(Guid id)
{
    try
    {
        var validation = ValidateGuid<ResourceDto>(id, "资源ID");
        if (validation != null) return validation;

        var result = await _service.GetByIdAsync(id);
        return HandleServiceResult(result, "查询成功");
    }
    catch (Exception ex)
    {
        return HandleException<ResourceDto>(ex, "获取资源详情", id);
    }
}
```

#### 3. 异常处理
- 所有public方法必须有try-catch
- 使用BaseApiController的HandleException方法
- 记录操作日志 `LogOperation()`

#### 4. 参数验证
- 使用 `ValidateGuid<T>()` 验证GUID参数
- 使用 `ValidateModel<T>()` 验证模型参数
- 业务规则验证在Service层实现

### Service调用标准

#### ServiceResult<T> 处理
```csharp
var result = await _service.GetByIdAsync(id);
return HandleServiceResult(result, "操作成功");
```

#### 分页结果处理
```csharp
var result = await _service.GetPagedAsync(query);
return HandlePagedServiceResult(result, "查询成功");
```

## 📈 性能优化

### 缓存策略
- 常用数据使用IMemoryCache缓存10分钟
- 分页查询结果缓存5分钟
- 用户权限信息缓存15分钟

### 查询优化
- 分页查询必须限制pageSize≤100
- 使用异步方法避免线程阻塞
- 数据库查询使用EF Core LINQ优化

### 响应优化
- 大对象查询使用流式处理
- API响应启用Gzip压缩
- 静态资源使用CDN分发

---

**文档版本**: v1.0  
**最后更新**: 2025-09-01  
**文档性质**: 需求文档 (始终保持最新)  
**维护者**: UltraThink项目组  
**更新状态**: ✅ 基于实际Controller实现完成 - 与代码100%一致

**重要说明**: 本API规格文档完全基于实际控制器代码分析，包含所有已实现的接口端点和功能。文档内容与源码实现保持完全一致，确保开发和测试的准确性。任何API变更必须同步更新此文档。