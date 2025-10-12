# LYBT.Desktop.Contracts

## 📋 项目说明

本项目包含 Desktop 客户端专用的 API 契约定义，主要用于 Refit HTTP 客户端接口。

## 🏗️ 架构定位

- **层级**：Desktop 核心层 (Core)
- **职责**：定义 Desktop 端调用 WebAPI 的接口契约
- **技术**：Refit (类型安全的 HTTP 客户端)

## 📁 目录结构

```
LYBT.Desktop.Contracts/
├── Api/                    # Refit API 接口定义
│   ├── IAuthApi.cs        # 认证 API
│   ├── IUserApi.cs        # 用户管理 API
│   ├── IPatientApi.cs     # 患者管理 API
│   ├── IConsultationApi.cs # 问诊 API
│   ├── IMedicalCaseApi.cs  # 病历 API
│   ├── IPrescriptionApi.cs # 处方 API
│   ├── IHerbApi.cs        # 中药 API
│   └── IFormulaApi.cs     # 方剂 API
└── README.md              # 本文件
```

## 🔗 依赖关系

### 直接依赖
- **LYBT.Shared.Models**: DTO 定义 (UserDto, PatientDto 等)
- **Refit**: HTTP 客户端框架

### 被依赖项
- LYBT.Desktop.Foundation (Refit 客户端注册)
- LYBT.Desktop.Models (Repository 实现)
- LYBT.Desktop.Presentation (ViewModel 调用)
- 所有业务模块 (Auth, Users, Patients 等)

## 📝 使用示例

### 接口定义
```csharp
namespace LYBT.Desktop.Contracts.Api
{
    public interface IUserApi
    {
        [Get("/api/v1/users")]
        Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null);

        [Get("/api/v1/users/{id}")]
        Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);

        [Post("/api/v1/users")]
        Task<ApiResponse<UserDto>> CreateUserAsync([Body] UserCreateDto request);
    }
}
```

### DI 注册 (Foundation 层)
```csharp
services.AddRefitClient<IUserApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthenticationHandler>();
```

### 使用 (Repository 层)
```csharp
public class UserRepository : IUserRepository
{
    private readonly IUserApi _userApi;

    public UserRepository(IUserApi userApi)
    {
        _userApi = userApi;
    }

    public async Task<ServiceResult<PagedResult<UserDto>>> GetUsersAsync(int page, int pageSize)
    {
        var response = await _userApi.GetUsersAsync(page, pageSize);
        return response.IsSuccessStatusCode
            ? ServiceResult<PagedResult<UserDto>>.Success(response.Content!)
            : ServiceResult<PagedResult<UserDto>>.Failure("获取用户列表失败");
    }
}
```

## 🎯 设计原则

1. **单一职责**：仅定义 API 契约，不包含业务逻辑
2. **类型安全**：使用 Refit 特性确保编译时检查
3. **契约驱动**：与 Server 端 WebAPI 保持一致
4. **无状态**：接口方法无状态，依赖 Refit 生成实现

## 📚 相关文档

- **架构标准**：`docs/architecture/client/unified-design-standard.md`
- **ADR-002**：Desktop 移除 Service 层决策
- **Issue #1204**：从 Shared.Interfaces 迁移至 Desktop.Contracts

## 🔄 历史变更

### v2.0 (2025-10-12)
- 从 `Shared.Interfaces/Api/` 迁移至 `Desktop.Contracts/Api/`
- 命名空间从 `LYBT.Shared.Interfaces.Api` 更改为 `LYBT.Desktop.Contracts.Api`
- 明确定位为 Desktop 专用契约，消除"Shared"误导

### v1.0 (初始版本)
- 位于 Shared.Interfaces 项目中
- 命名为"Shared"但实际仅被 Desktop 使用
