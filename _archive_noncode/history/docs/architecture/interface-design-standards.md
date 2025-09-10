# UltraThink接口设计标准规范

**版本**: v1.0  
**更新日期**: 2025-09-02  
**适用范围**: LYBTZYZS项目全栈接口设计

## 📋 规范概述

本规范建立LYBTZYZS项目的统一接口设计标准，解决接口重复、命名不一致、职责边界模糊等问题，确保前后端接口契约的一致性和可维护性。

## 🏗️ 接口层次架构标准

### 1. 三层接口架构体系

```
Shared层接口 (契约层)
├── LYBT.Shared.Interfaces.Services.*     # 前后端通信统一契约
├── LYBT.Shared.Interfaces.Api.*         # API客户端接口定义
└── LYBT.Shared.Interfaces.Caching.*     # 缓存服务接口

前端接口层 (UltraThink双层架构)
├── IXxxQueryService      # 查询专业服务接口
├── IXxxBusinessService   # 业务逻辑服务接口
└── IXxxService          # 统一入口服务接口(纯委托)

后端接口层 (传统三层架构)
├── IXxxQueryService      # 复杂查询服务接口  
├── IXxxBusinessService   # 业务流程服务接口
└── IXxxService          # 主服务接口(委托分发)
```

### 2. 接口职责边界定义

#### Shared层接口职责
- **IUserService, IPatientService等**: 前后端通信的统一契约
- **IAuthApi, IUserApi等**: 前端API客户端的类型安全接口
- **用途**: 确保前后端接口契约一致性，避免版本不同步

#### 前端接口职责  
- **IXxxQueryService**: 查询、搜索、统计、报表专用
- **IXxxBusinessService**: 业务逻辑和基础CRUD操作
- **IXxxService**: 纯委托入口，无业务逻辑

#### 后端接口职责
- **IXxxQueryService**: 复杂查询、数据分析专用
- **IXxxBusinessService**: 业务流程编排、事务管理
- **IXxxService**: 主服务入口，实现Shared契约

## 📝 命名规范标准

### 1. 接口命名规范

```csharp
// ✅ 正确命名模式
public interface IUserService          // 主服务接口
public interface IUserQueryService     // 查询专用接口  
public interface IUserBusinessService  // 业务专用接口
public interface IUserApi             // API客户端接口

// ❌ 错误命名模式
public interface IUserHelper          // 避免Helper后缀
public interface IUserManager         // 避免Manager后缀  
public interface IUserRepository      // Repository不是Service
public interface IUserCore            // 避免Core后缀
```

### 2. 方法命名规范

```csharp
// ✅ 查询方法命名(QueryService)
Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);
Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);
Task<ServiceResult<UserStatisticsDto>> GetStatisticsAsync();

// ✅ 业务方法命名(BusinessService)  
Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto);
Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserMutationDto dto);
Task<ServiceResult<bool>> DeleteAsync(Guid id);
Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

// ❌ 错误方法命名
Task<ServiceResult<UserDto>> GetUser(Guid id);           // 缺少Async后缀
Task<ServiceResult<UserDto>> CreateUserAsync();          // 业务方法应简洁
Task<ServiceResult<bool>> DoPasswordChange();            // 避免Do前缀
```

### 3. 参数命名规范

```csharp
// ✅ 正确参数命名
Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);                    // 简洁明确
Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserMutationDto dto); // DTO统一后缀
Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query); // Query统一后缀

// ❌ 错误参数命名  
Task<ServiceResult<UserDto>> GetByIdAsync(Guid userId);                // 避免冗余前缀
Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserMutationRequest request); // 统一使用Dto
```

## 🔗 接口依赖关系规范

### 1. 允许的依赖关系

```csharp
// ✅ 正确的接口依赖
// 1. 主Service可以依赖QueryService和BusinessService
public class UserService : IUserService
{
    private readonly IUserQueryService _queryService;
    private readonly IUserBusinessService _businessService;
}

// 2. BusinessService可以依赖QueryService(查询数据)
public class UserBusinessService : IUserBusinessService  
{
    private readonly IUserQueryService _queryService;  // 可以查询数据
}

// 3. 前端服务可以依赖API客户端
public class UserBusinessService : IUserBusinessService
{
    private readonly IUserApi _userApi;  // 调用后端API
}
```

### 2. 禁止的依赖关系

```csharp
// ❌ 错误的接口依赖
// 1. QueryService不能依赖BusinessService
public class UserQueryService : IUserQueryService
{
    private readonly IUserBusinessService _businessService; // 禁止！
}

// 2. 接口之间不能循环依赖
public interface IUserService : IPatientService { }  // 禁止！
public interface IPatientService : IUserService { } // 禁止！

// 3. 底层服务不能依赖上层服务
public class UserBusinessService : IUserBusinessService
{
    private readonly IUserService _userService; // 禁止！主服务是上层
}
```

## 📊 返回类型标准

### 1. 统一返回类型

```csharp
// ✅ 标准返回类型
Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);           // 单个实体
Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();     // 实体列表
Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync();    // 分页结果
Task<ServiceResult<bool>> DeleteAsync(Guid id);               // 操作结果
Task<ServiceResult<int>> BatchOperationAsync();               // 数量结果

// ❌ 错误返回类型
Task<UserDto> GetByIdAsync(Guid id);                         // 缺少统一包装
Task<ServiceResult<UserDto?>> GetByIdAsync(Guid id);         // 避免可空实体
Task<List<UserDto>> GetActiveUsersAsync();                   // 缺少错误处理
```

### 2. 错误处理标准

```csharp
// ✅ 标准错误处理模式
public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
{
    try
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            return ServiceResult<UserDto>.Failure("用户不存在");
            
        return ServiceResult<UserDto>.Success(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取用户失败: {UserId}", id);
        return ServiceResult<UserDto>.Failure($"获取用户失败: {ex.Message}");
    }
}
```

## 🚀 API接口设计规范

### 1. API接口命名规范

```csharp
// ✅ 正确的API接口设计
public interface IUserApi
{
    [Get("/api/v1/users/{id}")]
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);
    
    [Post("/api/v1/users")]
    Task<ApiResponse<UserDto>> CreateUserAsync([Body] UserMutationDto dto);
    
    [Put("/api/v1/users/{id}")]  
    Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, [Body] UserMutationDto dto);
}

// ❌ 错误的API接口设计
public interface IUserClient  // 应该使用Api后缀
{
    [Get("/Users/{id}")]      // URL应该小写
    Task<UserDto> Get(Guid id);  // 缺少Async后缀和统一返回类型
}
```

### 2. API方法命名模式

| HTTP方法 | API接口方法名 | URL模式 |
|---------|-------------|---------|
| GET     | `GetXxxAsync`, `GetXxxByIdAsync` | `/api/v1/users/{id}` |
| POST    | `CreateXxxAsync` | `/api/v1/users` |
| PUT     | `UpdateXxxAsync` | `/api/v1/users/{id}` |
| DELETE  | `DeleteXxxAsync` | `/api/v1/users/{id}` |
| PATCH   | `PatchXxxAsync`, `ToggleXxxAsync` | `/api/v1/users/{id}/status` |

## 🔍 接口验证规范

### 1. 编译时验证

```csharp
// ✅ 使用编译时验证确保接口一致性
[CompilerGenerated]
public static void ValidateInterface<TInterface, TImplementation>() 
    where TImplementation : class, TInterface
{
    // 编译时接口契约检查
}

// 在单元测试中验证
[Fact]
public void UserService_Should_ImplementSharedInterface()
{
    ValidateInterface<IUserService, UserService>();
}
```

### 2. 运行时验证

```csharp
// ✅ DI容器注册时验证接口实现
services.AddScoped<IUserService>(provider =>
{
    var implementation = provider.GetRequiredService<UserService>();
    // 验证implementation实现了所有必需的方法
    return implementation;
});
```

## 📈 接口演进策略

### 1. 版本管理策略

```csharp
// ✅ 接口版本管理
namespace LYBT.Shared.Interfaces.Services.V1
{
    public interface IUserService { }
}

namespace LYBT.Shared.Interfaces.Services.V2  
{
    public interface IUserService : V1.IUserService  // 继承保证兼容性
    {
        // 新增方法
    }
}
```

### 2. 废弃方法处理

```csharp
// ✅ 标准废弃方法处理
public interface IUserService
{
    [Obsolete("使用CreateUserAsync替代，将在v2.0中移除")]
    Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto);
    
    // 新方法
    Task<ServiceResult<UserDto>> CreateUserAsync(UserMutationDto dto);
}
```

## 🛡️ 质量保证措施

### 1. 代码审查检查清单

- [ ] 接口命名符合规范 (IXxxService, IXxxApi)
- [ ] 方法命名包含Async后缀
- [ ] 返回类型使用ServiceResult包装
- [ ] 参数使用标准DTO类型
- [ ] 接口职责单一清晰
- [ ] 无循环依赖关系
- [ ] API接口使用正确的HTTP方法标注

### 2. 自动化检查

```csharp
// 单元测试验证接口规范
public class InterfaceStandardsTests
{
    [Theory]
    [InlineData(typeof(IUserService))]
    [InlineData(typeof(IPatientService))]
    public void Services_Should_FollowNamingConvention(Type interfaceType)
    {
        // 验证接口命名规范
        Assert.True(interfaceType.Name.StartsWith("I"));
        Assert.True(interfaceType.Name.EndsWith("Service"));
    }
    
    [Fact]
    public void AllServiceMethods_Should_BeAsync()
    {
        // 验证所有Service方法都是异步的
        var serviceTypes = GetAllServiceInterfaces();
        foreach (var type in serviceTypes)
        {
            var methods = type.GetMethods();
            foreach (var method in methods)
            {
                Assert.True(method.Name.EndsWith("Async"));
                Assert.True(typeof(Task).IsAssignableFrom(method.ReturnType));
            }
        }
    }
}
```

## 📚 实施指南

### 1. 现有接口迁移策略

1. **Phase 1**: 审查现有接口，识别不合规项
2. **Phase 2**: 创建符合规范的新接口
3. **Phase 3**: 逐步迁移实现类
4. **Phase 4**: 废弃旧接口，更新调用方
5. **Phase 5**: 删除已废弃接口

### 2. 新接口开发流程

1. **设计阶段**: 按照本规范设计接口
2. **实现阶段**: 实现接口并编写单元测试
3. **审查阶段**: 代码审查验证规范合规性
4. **集成阶段**: 集成测试验证接口契约
5. **文档阶段**: 更新接口文档和示例

## 🎯 成功度量

### 1. 量化指标

- **接口命名合规率**: 目标 95%+
- **方法异步化率**: 目标 100%
- **返回类型标准化率**: 目标 100%  
- **接口重复度**: 目标 <5%
- **循环依赖数量**: 目标 0

### 2. 质量指标

- **编译警告数**: 接口相关警告 = 0
- **接口测试覆盖率**: 目标 90%+
- **文档完整度**: 每个接口都有完整说明
- **向后兼容性**: 接口变更不破坏现有功能

---

## 📋 附录

### A. 接口设计检查清单

**创建新接口时的验证清单:**

- [ ] 接口命名符合 `IXxxService` 或 `IXxxApi` 模式
- [ ] 所有方法都有 `Async` 后缀
- [ ] 返回类型使用 `Task<ServiceResult<T>>` 包装
- [ ] 参数使用标准DTO类型 (如 `UserMutationDto`)
- [ ] 接口职责单一，符合SRP原则
- [ ] 无循环依赖，依赖关系清晰
- [ ] API接口正确使用HTTP方法标注
- [ ] 接口有完整的XML文档注释
- [ ] 编写了接口契约的单元测试

### B. 常见反模式

**避免的接口设计反模式:**

1. **God Interface**: 一个接口包含过多职责
2. **Marker Interface**: 空接口，无实际方法
3. **Fat Interface**: 接口方法过多，难以实现
4. **Unstable Interface**: 接口频繁变更，破坏兼容性
5. **Leaky Abstraction**: 接口暴露实现细节

---

**文档版本**: v1.0  
**最后更新**: 2025-09-02  
**维护者**: UltraThink架构团队  
**审核状态**: ✅ 已通过架构委员会审核