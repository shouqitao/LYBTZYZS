# LYBT.Server.Interfaces - Server端服务接口层

## 📦 项目定位

- **层级**:Server端
- **类型**:核心库(服务接口层)
- **职责**:定义Server端所有业务服务的接口契约,实现依赖倒置原则(DIP)。为8个业务模块提供统一的服务接口定义,确保业务逻辑与具体实现解耦,支持依赖注入和单元测试。

## 📂 代码结构

```
LYBT.Server.Interfaces/
└── Services/
    ├── IAuthService.cs              # 认证服务接口(8个方法)
    │   ├── LoginAsync()              # 用户登录
    │   ├── LogoutAsync()             # 用户登出
    │   ├── RefreshTokenAsync()       # 刷新令牌
    │   ├── ValidateTokenAsync()      # 验证令牌
    │   ├── VerifyCredentialsAsync()  # 凭证验证
    │   ├── RevokeTokenAsync()        # 撤销令牌
    │   ├── GetSessionInfoAsync()     # 获取会话信息
    │   └── ChangeSysAdminPasswordAsync() # 超级管理员密码修改
    ├── IPatientService.cs           # 患者服务接口(8个方法)
    │   ├── GetPagedAsync()           # 分页查询患者
    │   ├── GetByIdAsync()            # 根据ID获取患者
    │   ├── CreateAsync()             # 创建患者
    │   ├── UpdateAsync()             # 更新患者
    │   ├── DeleteAsync()             # 删除患者
    │   ├── SearchAsync()             # 搜索患者
    │   ├── ImportFromExcelAsync()    # 从Excel导入患者
    │   └── GenerateImportTemplate()  # 生成导入模板
    ├── IMedicalCaseService.cs       # 病案服务接口(19个方法)
    │   ├── GetPagedAsync()           # 分页查询病案
    │   ├── GetByIdAsync()            # 根据ID获取病案
    │   ├── CreateAsync()             # 创建病案
    │   ├── UpdateAsync()             # 更新病案
    │   ├── DeleteAsync()             # 删除病案
    │   ├── BatchDeleteAsync()        # 批量删除病案
    │   ├── GetByPatientIdAsync()     # 根据患者ID获取病案
    │   ├── GetPendingCasesAsync()    # 获取待处理病案
    │   ├── CreateWithDetailsAsync()  # 创建病案(含详情)
    │   ├── GetByIdWithDetailsAsync() # 获取病案(含详情)
    │   ├── UpdateConsultationAsync() # 更新诊断记录
    │   ├── UpdatePrescriptionAsync() # 更新处方
    │   ├── CreatePrescriptionAsync() # 创建处方
    │   ├── DeletePrescriptionAsync() # 删除处方
    │   ├── QueryAsync()              # 复杂查询
    │   ├── CompleteStep1Async()      # 完成诊疗步骤1
    │   ├── ResetConsultationStepsAsync() # 重置诊疗步骤
    │   ├── ClearPrescriptionAsync()  # 清空处方
    │   └── ImportFormulaIntoPrescriptionAsync() # 导入方剂到处方
    ├── IConsultationService.cs      # 诊断服务接口
    ├── IPrescriptionService.cs      # 处方服务接口
    ├── IFormulaService.cs           # 方剂服务接口
    ├── IHerbService.cs              # 中药服务接口
    └── IUserService.cs              # 用户服务接口
```

**说明**:
- **Services/**:8个业务模块的服务接口定义
- **核心接口**:IAuthService(认证)、IPatientService(患者)、IMedicalCaseService(病案)
- **辅助接口**:IConsultationService(诊断)、IPrescriptionService(处方)、IFormulaService(方剂)、IHerbService(中药)、IUserService(用户)

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Shared.Models** - 共享DTO模型(所有接口的参数和返回值类型)

### 被依赖项目
1. **LYBT.Module.*** - 8个业务模块实现这些接口(如LYBT.Module.Auth实现IAuthService)
2. **LYBT.WebAPI** - API Controller通过依赖注入使用这些接口
3. **测试项目** - 单元测试通过Mock这些接口进行隔离测试

### NuGet包
- **无外部包依赖** - 纯接口定义项目,仅依赖.NET 8基础类型和LYBT.Shared.Models

## 🛠 技术栈

- **.NET 8**:基础框架
- **依赖倒置原则(DIP)**:高层模块(Controller)依赖接口而非具体实现
- **接口隔离原则(ISP)**:每个服务接口职责单一
- **依赖注入(DI)**:通过IServiceCollection注册接口与实现的映射

## 🚀 快速开始

此项目是一个类库,无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Core/LYBT.Server.Interfaces/LYBT.Server.Interfaces.csproj
```

**集成说明**:

### 1. 实现接口(在业务模块中)
```csharp
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Auth;

namespace LYBT.Module.Auth.Services
{
    public class AuthService : IAuthService
    {
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            // 实现登录逻辑
        }

        public async Task LogoutAsync(string userId)
        {
            // 实现登出逻辑
        }

        // 实现其他方法...
    }
}
```

### 2. 注册服务(在Startup.cs或模块注册文件中)
```csharp
public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册服务接口与实现的映射
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
```

### 3. 使用接口(在Controller中)
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    // 构造函数注入(依赖接口而非具体实现)
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }
}
```

### 4. Mock测试(在单元测试中)
```csharp
using NSubstitute;
using LYBT.Server.Interfaces.Services;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_Should_Return_Token()
    {
        // Arrange - Mock接口
        var mockAuthService = Substitute.For<IAuthService>();
        mockAuthService.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(new LoginResponse { Token = "test-token" });

        var controller = new AuthController(mockAuthService);

        // Act
        var result = await controller.Login(new LoginRequest { Username = "admin" });

        // Assert
        Assert.NotNull(result.Value);
        Assert.Equal("test-token", result.Value.Token);
    }
}
```

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/interfaces/](../../../../docs/reference/modules/interfaces/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/server/interfaces-layer-design.md](../../../../docs/explanation/architecture/server/interfaces-layer-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/interfaces-usage.md](../../../../docs/how-to-guides/server/interfaces-usage.md) *(待创建)*
- **依赖倒置原则**:[docs/reference/quick-reference/code-patterns.md](../../../../docs/reference/quick-reference/code-patterns.md) - 参见"SOLID原则"章节

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组
