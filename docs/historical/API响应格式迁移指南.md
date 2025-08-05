# API 响应格式迁移指南

## 概述

本指南记录了从自定义 ApiResponse 包装器迁移到标准 RESTful API 响应格式的过程。

## 已完成的工作

### 后端改造（已完成）

1. ✅ 删除了 `ApiResponse.cs` 文件
2. ✅ 创建了全局异常处理器 `GlobalExceptionHandler`
3. ✅ 创建了自定义异常类 (`BusinessException`, `NotFoundException`)
4. ✅ 更新了 `Program.cs` 配置 ProblemDetails 和异常处理
5. ✅ 更新了所有控制器，移除 ApiResponse 包装：
   - AuthController
   - UsersController
   - PatientsController
   - DoctorsController
   - HerbsController
   - 其他所有控制器

### 前端改造（部分完成）

1. ✅ 更新了前端 API 服务接口定义（IAuthApiService, IUserApiService 等）
2. ✅ 更新了前端服务实现层的错误处理
3. ✅ 配置了 Refit 的 Polly 重试策略
4. ✅ 创建了 `ServiceResult` 类作为前端服务层的响应包装
5. ✅ 更新了前端 Core 项目中的接口定义

### 当前问题

1. **编译错误众多**：由于 ApiResponse 被广泛使用，需要系统性地更新所有相关文件
2. **Refit 生成的代码**：Refit 自动生成的代码仍在使用旧的 ApiResponse
3. **服务实现类**：许多服务实现类还未更新

## 后续迁移步骤

### 1. 更新所有 Refit API 接口

所有 `I*ApiService` 接口需要从：
```csharp
Task<ApiResponse<T>> GetAsync(...);
```

更新为：
```csharp
Task<Refit.ApiResponse<T>> GetAsync(...);
```

### 2. 更新服务实现类

所有服务实现类需要使用 `ApiErrorHandler` 来处理 Refit 响应：

```csharp
// 旧代码
var response = await _apiService.GetAsync();
if (response.IsSuccess)
{
    // 处理成功
}

// 新代码
var response = await ApiErrorHandler.HandleApiResponseAsync(
    async () => await _apiService.GetAsync()
);
```

### 3. 更新 ApiErrorHandler

需要更新 `ApiErrorHandler` 以返回 `ServiceResult` 而不是 `ApiResponse`：

```csharp
public static async Task<ServiceResult<T>> HandleApiResponseAsync<T>(
    Func<Task<Refit.ApiResponse<T>>> apiCall)
{
    // 实现代码
}
```

## API 响应格式对比

### 旧格式（ApiResponse 包装）
```json
{
    "isSuccess": true,
    "data": { ... },
    "message": null,
    "statusCode": 200
}
```

### 新格式（标准 RESTful）

**成功响应（200-299）**：
```json
// 直接返回数据
{ ... }
```

**错误响应（400-599）**：
```json
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "detail": "具体错误信息",
    "errors": { ... }
}
```

## 注意事项

1. **认证处理**：401 错误不应该被重试
2. **错误消息提取**：需要从 ProblemDetails 格式中提取友好的错误消息
3. **空响应处理**：某些 API 可能返回空响应，需要妥善处理
4. **向后兼容**：在迁移期间，可能需要同时支持新旧格式