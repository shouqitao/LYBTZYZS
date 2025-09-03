# 🎯 LYBT接口设计标准规范

> **企业级接口设计指南** | UltraThink双层架构支持 | 接口统一化完成  
> **版本**: v2.2 | **最后更新**: 2025-01-31 | **状态**: 统一接口体系  
> **重要里程碑**: 🎆 解决"接口重复定义横跨4层"严重架构问题

## 📋 规范概述

本文档定义了LYBT中医诊所系统中所有接口的设计标准、命名规范、注释要求和最佳实践。遵循本规范可确保接口的一致性、可维护性和企业级质量。

## 🏗️ 架构设计原则

### UltraThink双层架构适配

所有业务服务接口必须明确标识在UltraThink双层架构中的委托路径：

**统一接口体系 (2025-01-31完成) 🎆**:
```csharp
// ✅ 正确接口设计 - 单一IService接口
public class UserModule : IUserService
{
    /// <remarks>
    /// <para>委托: Module → QueryService.GetUserByIdAsync</para>
    /// <para>委托: Module → BusinessService.CreateUserAsync</para>
    /// </remarks>
    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);
}

// ❌ 禁止模式 - 重复接口定义
// public class UserModule : IUserService, IUserModule { } // 严禁！
```

**接口统一化原则**:
- **单一接口实现**: 所有Module类只实现IService接口，禁止IModule重复接口
- **依赖注入标准**: 所有ViewModel必须使用IService接口注入，禁止具体Module类型
- **方法签名匹配**: 接口方法签名与实现必须完全匹配，避免编译错误
- **架构问题防护**: 严防"接口重复定义横跨4层"问题重现

**职责分离原则**:
- **QueryService**: 复杂查询、搜索、统计、报表
- **BusinessService**: 业务逻辑、CRUD操作、事务管理
- **Module**: 纯委托模式，统一服务入口，实现IService接口

### 接口分类体系

#### 1. API客户端接口 (Api/)
- **命名规范**: I{Module}Api (如: IUserApi, IPatientApi)
- **技术特性**: 使用Refit属性标记HTTP方法和路径
- **响应格式**: 统一使用ApiResponse<T>包装
- **用途**: 前端WPF客户端调用后端Web API

#### 2. 业务服务接口 (Services/)
- **命名规范**: I{Module}Service (如: IUserService, IPatientService)
- **返回格式**: 统一使用ServiceResult<T>包装
- **方法模式**: 异步优先，所有方法返回Task<T>
- **用途**: WPF前端业务逻辑层服务

#### 3. 缓存服务接口 (Caching/)
- **命名规范**: I{Purpose}CacheService
- **操作模式**: 同步+异步双模式支持
- **用途**: 性能优化，减少重复数据库查询

## 📝 接口注释标准

### XML文档注释要求

每个接口和方法必须包含完整的XML注释：

```csharp
/// <summary>
/// 接口或方法的简要描述 - 功能概述
/// </summary>
/// <param name="paramName">参数描述 - 用途和约束</param>
/// <returns>返回值描述 - 数据内容和格式</returns>
/// <remarks>
/// <para>委托: Module → Service.MethodAsync</para>
/// <para>功能: 详细功能说明</para>
/// <para>权限: 权限要求描述</para>
/// <para>缓存: 缓存策略说明</para>
/// <para>示例: 使用示例代码</para>
/// </remarks>
[Description("组件描述 - 用于工具显示")]
```

### 注释内容规范

#### 1. Summary标准
- **格式**: 动词开头 + 简洁描述 + 技术特征
- **长度**: 1-2行，不超过100字符
- **示例**: `/// <summary>根据ID获取用户详情</summary>`

#### 2. Remarks详细说明
必须包含以下信息（适用时）:
- **委托**: UltraThink架构中的委托路径
- **功能**: 详细功能描述和业务场景
- **权限**: 权限要求和访问限制
- **缓存**: 缓存策略和过期时间
- **验证**: 输入验证规则
- **示例**: 典型使用场景代码

#### 3. 参数和返回值
- **参数**: 描述用途、格式要求、约束条件
- **返回值**: 说明数据结构、成功/失败情况

## 🎯 命名规范标准

### 接口命名
- **API接口**: I{Module}Api (IUserApi, IPatientApi)
- **服务接口**: I{Module}Service (IUserService, IPatientService)  
- **缓存接口**: I{Purpose}CacheService (ISimplifiedCacheService)

### 方法命名

#### CRUD操作命名
```csharp
// 查询操作 - Get前缀
Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();

// 创建操作 - Create前缀
Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);

// 更新操作 - Update前缀  
Task<ServiceResult<UserDto>> UpdateAsync(UserUpdateDto dto);

// 删除操作 - Delete前缀
Task<ServiceResult<bool>> DeleteAsync(Guid id);
```

#### 业务操作命名
```csharp
// 状态管理
Task<ServiceResult<bool>> EnableAsync(Guid id);
Task<ServiceResult<bool>> DisableAsync(Guid id);

// 批量操作 - Batch前缀
Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);

// 验证操作 - Validate前缀
Task<ServiceResult<bool>> ValidateUsernameAsync(string username);

// 搜索操作 - Search前缀
Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);
```

### 参数命名
- **ID参数**: 使用id (小写)
- **DTO参数**: 使用dto (小写)
- **列表参数**: 使用ids、items等复数形式
- **查询参数**: 使用query、request等后缀

## 📊 返回类型标准

### API响应格式
所有API接口必须使用统一的响应格式：

```csharp
Task<ApiResponse<T>> MethodAsync(RequestDto request);

// ApiResponse结构
{
    "success": true,
    "message": "操作成功",
    "data": { /* 业务数据 */ },
    "timestamp": "2025-09-02T10:30:00Z",
    "requestId": "req-123456"
}
```

### 服务结果格式
所有业务服务接口必须使用ServiceResult包装：

```csharp
Task<ServiceResult<T>> MethodAsync(RequestDto request);

// ServiceResult结构
{
    "Success": true,
    "Message": "操作成功", 
    "Data": { /* 业务数据 */ },
    "ErrorCode": null
}
```

### 分页结果格式
查询列表数据使用统一的分页格式：

```csharp
Task<ServiceResult<PagedResult<T>>> GetPagedAsync(PagedQueryDto query);

// PagedResult结构
{
    "Items": [ /* 数据项列表 */ ],
    "TotalCount": 100,
    "PageIndex": 1,
    "PageSize": 20,
    "TotalPages": 5
}
```

## 🔧 技术实现标准

### Refit API接口
```csharp
[Description("模块功能描述")]
public interface IModuleApi
{
    /// <summary>方法描述</summary>
    /// <param name="request">请求参数</param>
    /// <returns>响应数据</returns>
    /// <remarks>
    /// <para>功能: 具体功能说明</para>
    /// <para>权限: 权限要求</para>
    /// </remarks>
    [Post("/api/v1/module/action")]
    Task<ApiResponse<ResponseDto>> ActionAsync([Body] RequestDto request);
}
```

### 业务服务接口
```csharp
[Description("服务功能描述")]
public interface IModuleService
{
    #region 查询操作 - QueryService专业负责
    
    /// <summary>查询方法描述</summary>
    /// <param name="id">ID参数</param>
    /// <returns>查询结果</returns>
    /// <remarks>
    /// <para>委托: Module → QueryService.GetByIdAsync</para>
    /// <para>缓存: 10分钟缓存</para>
    /// </remarks>
    Task<ServiceResult<ModuleDto>> GetByIdAsync(Guid id);
    
    #endregion
    
    #region 业务操作 - BusinessService专业负责
    
    /// <summary>业务方法描述</summary>
    /// <param name="dto">业务数据</param>
    /// <returns>操作结果</returns>
    /// <remarks>
    /// <para>委托: Module → BusinessService.CreateAsync</para>
    /// <para>验证: 数据完整性验证</para>
    /// </remarks>
    Task<ServiceResult<ModuleDto>> CreateAsync(ModuleCreateDto dto);
    
    #endregion
}
```

### 缓存服务接口
```csharp
[Description("缓存功能描述")]
public interface ICacheService
{
    #region 同步操作 - 高频快速访问
    
    /// <summary>获取缓存项</summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存数据</returns>
    /// <remarks>
    /// <para>适用: 高频访问场景</para>
    /// <para>性能: 微秒级响应</para>
    /// </remarks>
    T? Get<T>(string key);
    
    #endregion
    
    #region 异步操作 - 复杂数据处理
    
    /// <summary>获取或设置缓存</summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">数据工厂</param>
    /// <param name="expiration">过期时间</param>
    /// <returns>缓存或新数据</returns>
    /// <remarks>
    /// <para>核心模式: 缓存未命中时自动获取并缓存</para>
    /// <para>典型用法: var data = await cache.GetOrSetAsync("key", async () => await service.GetDataAsync());</para>
    /// </remarks>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    
    #endregion
}
```

## ✅ 质量检查清单

### 接口定义检查
- [ ] 接口命名符合规范 (I{Module}{Type})
- [ ] 方法命名清晰表达意图
- [ ] 参数类型使用强类型DTO
- [ ] 返回类型使用统一包装格式
- [ ] 异步方法正确使用async/await

### 注释文档检查
- [ ] 每个接口有完整的XML注释
- [ ] Summary简洁明确
- [ ] 所有参数都有说明
- [ ] 返回值描述完整
- [ ] Remarks包含必要的技术细节
- [ ] Description属性用于工具显示

### UltraThink架构检查
- [ ] 明确标识委托路径 (Module → Service)
- [ ] 正确划分QueryService/BusinessService职责
- [ ] 缓存策略说明清晰
- [ ] 权限要求明确标注

### 代码规范检查
- [ ] 使用C# 12语法特性
- [ ] 正确的命名空间和using语句
- [ ] 适当的ComponentModel属性
- [ ] 遵循.NET编码约定

## 🛠️ 开发工具配置

### Visual Studio设置
```xml
<!-- .editorconfig -->
[*.cs]
# XML注释必需
dotnet_analyzer_rule.CS1591.severity = warning

# 接口命名检查
dotnet_naming_rule.interface_should_be_prefixed_with_i.severity = error
```

### 文档生成配置
```xml
<!-- 项目文件配置 -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <DocumentationFile>bin\$(Configuration)\$(TargetFramework)\$(AssemblyName).xml</DocumentationFile>
</PropertyGroup>
```

## 📚 相关资源

### 内部文档
- [LYBT.Shared.Interfaces项目文档](../../src/Shared/LYBT.Shared.Interfaces/README.md)
- [UltraThink架构指南](../architecture/ultrathink-architecture-guide.md)
- [前后端契约规范](../前后端契约规范.md)

### 技术参考
- [.NET API设计指南](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Refit使用文档](https://github.com/reactiveui/refit)
- [C# XML文档注释](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)

---

**LYBT接口设计标准规范** - 确保企业级接口质量，支撑UltraThink双层架构 ✨

**维护责任**: 架构组 | **审核周期**: 季度 | **版本**: v2.1.0 | **更新日期**: 2025-09-02