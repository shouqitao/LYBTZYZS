# API响应设计规范

## 📋 文档元数据
- **创建日期**：2025-01-21
- **关联Issue**：#1537
- **适用范围**：全项目（Server + Client）
- **架构层级**：Shared层（跨端契约）

---

## 🎯 核心原则

### 统一响应格式原则
**项目只使用一种 `ApiResponse<T>` 类型**，定义在：
```
LYBT.Shared.Models.Contracts.Common.ApiResponse<T>
```

**禁止使用**：
- ❌ `Refit.ApiResponse<T>`（Refit库提供的HTTP包装类型）
- ❌ 直接返回原始数据类型（无包装）

---

## 📦 ApiResponse<T> 结构定义

### 类型签名
```csharp
namespace LYBT.Shared.Models.Contracts.Common
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public object? Errors { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = string.Empty;
    }
}
```

### JSON格式示例

**成功响应**：
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [...],
    "totalCount": 100,
    "page": 1,
    "pageSize": 20
  },
  "timestamp": 1737475200000,
  "requestId": "abc-123-def-456"
}
```

**验证失败响应**：
```json
{
  "success": false,
  "message": "验证失败",
  "errors": {
    "Name": ["姓名不能为空"],
    "Phone": ["手机号格式错误"]
  },
  "timestamp": 1737475200000,
  "requestId": "xyz-789-uvw-012"
}
```

---

## 🏗️ 架构设计理念

### 1. 业务状态与HTTP状态分离

| HTTP状态码 | Success字段 | 场景说明 |
|-----------|------------|---------|
| 200 OK | true | 业务操作成功 |
| 200 OK | false | 业务验证失败（非异常） |
| 400 Bad Request | false | 客户端参数错误 |
| 401 Unauthorized | false | 未授权访问 |
| 500 Internal Error | false | 服务器异常 |

**设计优势**：
- ✅ HTTP状态码表示"请求是否被处理"
- ✅ Success字段表示"业务逻辑是否成功"
- ✅ 允许HTTP 200 + 业务失败（如验证失败）

### 2. 富元数据支持

| 字段 | 用途 | 示例 |
|-----|------|------|
| `success` | 业务状态判断 | `if (response.Success) { ... }` |
| `message` | 用户友好提示 | "查询成功"、"验证失败" |
| `data` | 实际业务数据 | `PagedResult<T>`、`UserDto` |
| `errors` | 结构化错误详情 | 字段级验证错误 |
| `timestamp` | 响应生成时间 | 缓存控制、日志关联 |
| `requestId` | 分布式链路追踪 | 跨服务日志关联 |

### 3. 客户端统一处理

**Repository层标准模式**：
```csharp
public async Task<ServiceResult<T>> GetDataAsync()
{
    var response = await _api.GetDataAsync();

    // 统一判断逻辑
    if (response.Success && response.Data != null)
    {
        return ServiceResult<T>.CreateSuccess(response.Data);
    }
    else
    {
        return ServiceResult<T>.CreateFail(response.Message);
    }
}
```

**优势**：
- ✅ 无需try-catch（成功/失败都是正常响应）
- ✅ 统一错误处理（Message/Errors字段）
- ✅ 减少样板代码（所有API使用相同模式）

---

## 🔧 Refit的角色定位

### Refit是工具，不是类型定义

**Refit的职责**：
- ✅ HTTP请求管理（GET/POST/PUT/DELETE）
- ✅ 参数序列化（`[Query]`/`[Body]`）
- ✅ JSON反序列化（将Server的JSON转换为 `ApiResponse<T>`）
- ✅ 认证集成（`[Headers("Authorization: Bearer")]`）
- ✅ 异常处理（网络错误、超时）

**Refit不强制使用 `Refit.ApiResponse<T>`**：
```csharp
// ❌ 错误：使用Refit库的包装类型
Task<Refit.ApiResponse<PagedResult<T>>> GetPatientsAsync(...);

// ✅ 正确：使用项目定义的类型
Task<ApiResponse<PagedResult<T>>> GetPatientsAsync(...);
```

### 工作流程图

```
┌──────────────────┐
│  Client调用API   │
└────────┬─────────┘
         │
         ↓
┌──────────────────────────────┐
│  Refit发送HTTP请求           │
│  GET /api/v1/patients?page=1 │
└────────┬─────────────────────┘
         │
         ↓
┌────────────────────────────────────┐
│  Server返回JSON                    │
│  {"success": true, "data": {...}}  │
└────────┬───────────────────────────┘
         │
         ↓
┌──────────────────────────────────────┐
│  Refit反序列化为 ApiResponse<T>      │
│  （项目定义的类型，不是Refit的）     │
└────────┬─────────────────────────────┘
         │
         ↓
┌──────────────────┐
│  Repository处理  │
│  response.Success│
└──────────────────┘
```

---

## 📝 API接口声明规范

### ✅ 正确示例（IAuthApi）

```csharp
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Auth;

public interface IAuthApi
{
    [Refit.Post("/api/v1/auth/login")]
    Task<ApiResponse<LoginResponse>> LoginAsync(
        [Refit.Body] LoginRequest loginRequest);

    [Refit.Post("/api/v1/auth/logout")]
    [Refit.Headers("Authorization: Bearer")]
    Task<ApiResponse> LogoutAsync(
        [Refit.Body] LogoutRequest logoutRequest);
}
```

**关键点**：
1. ✅ 使用 `ApiResponse<T>`（不带命名空间前缀，通过using引入）
2. ✅ 或使用 `LYBT.Shared.Models.Contracts.Common.ApiResponse<T>`（完整命名空间）
3. ✅ 泛型参数 `T` 是实际的业务数据类型（`LoginResponse`）

### ❌ 错误示例（IPatientApi修复前）

```csharp
public interface IPatientApi
{
    // ❌ 错误：使用了Refit.ApiResponse<T>
    [Refit.Get("/api/v1/patients")]
    Task<Refit.ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(...);

    // ❌ Refit会期望JSON格式：{"items": [...], "totalCount": 100}
    // ❌ 但Server实际返回：{"success": true, "data": {"items": [...]}}
    // ❌ 导致JSON反序列化失败
}
```

### 命名空间引入策略

**推荐方式1**：using引入
```csharp
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

public interface IPatientApi
{
    Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(...);
    //    ↑ 简洁，推荐用于新代码
}
```

**推荐方式2**：完整命名空间（IAuthApi当前用法）
```csharp
public interface IAuthApi
{
    Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>> LoginAsync(...);
    //    ↑ 明确，避免歧义，适合重要接口
}
```

---

## 🔍 Issue #1537 问题分析

### 问题描述
7个API接口错误使用 `Refit.ApiResponse<T>` 导致JSON反序列化失败。

### 受影响的文件
```
src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/
├── IPatientApi.cs       - 5个方法
├── IUserApi.cs          - 5个方法
├── IConsultationApi.cs  - 7个方法
├── IMedicalCaseApi.cs   - 8个方法
├── IPrescriptionApi.cs  - 8个方法
├── IHerbApi.cs          - 5个方法
└── IFormulaApi.cs       - 8个方法

总计：46个方法声明需要修复
```

### 根本原因
```csharp
// Client端期望的JSON格式（Refit.ApiResponse<T>）
{
  "items": [...],
  "totalCount": 100
}

// Server端实际返回的JSON格式（ApiResponse<T>）
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [...],
    "totalCount": 100
  }
}

// JSON结构不匹配 → 反序列化失败 → HTTP请求失败
```

### 修复方案
批量替换返回类型：
```csharp
// 修复前
Task<Refit.ApiResponse<T>> MethodAsync(...);

// 修复后
Task<ApiResponse<T>> MethodAsync(...);
```

---

## 🎓 常见问题解答

### Q1: 为什么不直接返回原始数据（无包装）？
**A**: 会丢失关键元数据：
- ❌ 无法区分"业务成功"和"业务失败"
- ❌ 无法提供用户友好的错误消息
- ❌ 无法进行分布式链路追踪（requestId）
- ❌ 错误处理复杂化（需要依赖HTTP状态码+异常）

### Q2: Refit.ApiResponse<T> 和项目的 ApiResponse<T> 有什么区别？
**A**:
- **Refit.ApiResponse<T>**：HTTP层包装，包含 `StatusCode`、`Headers`、`Content`
- **项目的 ApiResponse<T>**：业务层包装，包含 `Success`、`Message`、`Data`、`Errors`

当前架构选择业务层包装，因为：
- ✅ Repository层不需要访问HTTP层信息
- ✅ 业务逻辑与传输协议解耦
- ✅ 更丰富的业务元数据支持

### Q3: 如果将来需要HTTP状态码怎么办？
**A**: 可以扩展 `ApiResponse<T>` 添加 `HttpStatusCode` 字段，或者让Repository方法返回元组：
```csharp
(ApiResponse<T> response, int statusCode)
```

但MVP阶段不需要这些复杂性。

### Q4: 修复后Refit还有用吗？
**A**: **完全有用**！Refit负责：
- ✅ HTTP请求的发送和接收
- ✅ 参数的序列化和编码
- ✅ JSON的反序列化（反序列化为 `ApiResponse<T>`）
- ✅ 认证Token的自动附加
- ✅ 网络异常的自动处理

我们只是改变了"反序列化的目标类型"，从 `Refit.ApiResponse<T>` 改为 `ApiResponse<T>`。

---

## 📚 参考资料

### 相关文档
- `docs/explanation/architecture/server/README.md` - Server端三层架构
- `docs/explanation/architecture/shared/README.md` - 共享层契约设计
- `docs/how-to-guides/shared/task-workflow-checklist.md` - 开发流程清单

### 相关代码
- `src/Shared/LYBT.Shared.Models/Contracts/Common/ApiResponse.cs` - 类型定义
- `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs` - Server端创建逻辑
- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IAuthApi.cs` - 正确示例

### 行业最佳实践
- Google APIs - 使用 `error` 对象包装
- GitHub API v3 - 使用 `message` 字段
- Stripe API - 使用 `error.type` + `error.message`

---

## 📅 变更历史

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|---------|------|
| 2025-01-21 | 1.0 | 初始版本 - 固化API响应设计规范 | Claude Code |

---

**文档状态**：✅ 已生效
**下次审查**：Epic #1494 完成后
