# 架构修改建议文档

生成时间：2025-09-25
目标：简化架构，提升可维护性，加速业务开发

## 一、整体重构策略

### 1.1 重构原则

1. **简化优先**：删除冗余层次，保留核心功能
2. **统一标准**：全栈采用相同架构模式
3. **渐进实施**：分阶段进行，保证系统稳定
4. **业务优先**：先定义接口，后实现业务逻辑

### 1.2 目标架构

```
简化后的三层架构：
Server: Controller → Service → Repository → DbContext
Client: ViewModel → Service → ApiClient
```

## 二、服务器端修改建议

### 2.1 第一阶段：统一Service模式（优先级：P0）

#### 任务1：重构Auth模块
```csharp
// 删除文件：
- IAuthQueryService.cs
- IAuthBusinessService.cs  
- AuthQueryService.cs
- AuthBusinessService.cs

// 修改AuthService.cs：
public class AuthService : IAuthService
{
    // 合并所有Query和Business方法
    // 直接实现，不做委托
}
```

#### 任务2：删除ReadRepository层
```bash
# 删除所有ReadRepository相关文件
rm -rf src/Server/Modules/*/Repositories/*ReadRepository.cs
rm -rf src/Server/Modules/*/Interfaces/*ReadRepository.cs

# 修改Service直接使用Repository
```

#### 任务3：清理Controller引用
```csharp
// 修改所有OperationController
// 删除IBusinessService引用
// 统一使用IService接口
```

### 2.2 第二阶段：优化Repository模式（优先级：P1）

#### 任务1：统一Repository基类
```csharp
public abstract class RepositoryBase<T> : IRepository<T> 
    where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly IMemoryCache _cache;
    
    // 统一的CRUD操作
    // 统一的缓存策略
    // 统一的软删除处理
}
```

#### 任务2：简化Repository接口
```csharp
public interface IRepository<T> where T : BaseEntity
{
    // 基础CRUD
    Task<T> GetByIdAsync(Guid id);
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    
    // 通用查询
    Task<PagedResult<T>> GetPagedAsync(PagedQueryBaseDto query);
    Task<List<T>> SearchAsync(Expression<Func<T, bool>> predicate);
}
```

### 2.3 第三阶段：Service层优化（优先级：P1）

#### 任务1：提取公共Service基类
```csharp
public abstract class ServiceBase<TEntity, TDto, TCreateDto, TUpdateDto>
{
    protected readonly IRepository<TEntity> _repository;
    protected readonly IMapper _mapper;
    protected readonly ILogger _logger;
    
    // 通用CRUD实现
    public virtual async Task<ServiceResult<TDto>> CreateAsync(TCreateDto dto) { }
    public virtual async Task<ServiceResult<TDto>> UpdateAsync(Guid id, TUpdateDto dto) { }
    public virtual async Task<ServiceResult<bool>> DeleteAsync(Guid id) { }
}
```

#### 任务2：业务Service继承基类
```csharp
public class UserService : ServiceBase<User, UserDto, UserCreateDto, UserUpdateDto>
{
    // 仅实现特定业务逻辑
    public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword) 
    {
        // 特定业务逻辑
    }
}
```

## 三、客户端修改建议

### 3.1 第一阶段：移除UltraThink双层（优先级：P0）

#### 任务1：合并Service层
```csharp
// 删除纯委托的Module Service
// 合并QueryService和BusinessService为单一Service

public class PatientService : IPatientService
{
    private readonly IApiClient _apiClient;
    
    // 所有查询方法
    public async Task<PagedResult<PatientDto>> GetPagedAsync(PagedQueryDto query) { }
    
    // 所有业务方法
    public async Task<PatientDto> CreateAsync(PatientCreateDto dto) { }
}
```

#### 任务2：简化依赖注入
```csharp
services.AddScoped<IPatientService, PatientService>();
// 删除IPatientQueryService, IPatientBusinessService注册
```

### 3.2 第二阶段：统一API调用模式（优先级：P1）

#### 任务1：创建统一的ApiClient基类
```csharp
public abstract class ApiClientBase
{
    protected readonly HttpClient _httpClient;
    protected readonly IAuthenticationService _authService;
    
    protected async Task<ServiceResult<T>> GetAsync<T>(string endpoint) { }
    protected async Task<ServiceResult<T>> PostAsync<T>(string endpoint, object data) { }
    protected async Task<ServiceResult<T>> PutAsync<T>(string endpoint, object data) { }
    protected async Task<ServiceResult<bool>> DeleteAsync(string endpoint) { }
}
```

#### 任务2：Service使用统一ApiClient
```csharp
public class PatientApiClient : ApiClientBase
{
    private const string BaseUrl = "api/patients";
    
    public Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedQueryDto query)
        => GetAsync<PagedResult<PatientDto>>($"{BaseUrl}?{query.ToQueryString()}");
}
```

### 3.3 第三阶段：ViewModel优化（优先级：P2）

#### 任务1：提取ViewModel基类
```csharp
public abstract class CrudViewModelBase<TService, TDto, TCreateDto, TUpdateDto> : ViewModelBase
{
    protected readonly TService _service;
    
    // 通用CRUD命令
    public ICommand CreateCommand { get; }
    public ICommand UpdateCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
}
```

## 四、共享层修改建议

### 4.1 统一DTO规范（优先级：P1）

```csharp
// 命名规范
public class {Entity}Dto { }           // 基础DTO
public class {Entity}CreateDto { }     // 创建DTO
public class {Entity}UpdateDto { }     // 更新DTO
public class {Entity}SearchDto { }     // 搜索DTO
public class {Entity}DetailDto { }     // 详情DTO（包含关联数据）
```

### 4.2 统一错误处理（优先级：P1）

```csharp
public class GlobalExceptionHandler
{
    public static ServiceResult<T> HandleException<T>(Exception ex)
    {
        return ex switch
        {
            NotFoundException => ServiceResult<T>.Failure("资源不存在", 404),
            ValidationException => ServiceResult<T>.Failure(ex.Message, 400),
            UnauthorizedException => ServiceResult<T>.Failure("未授权", 401),
            _ => ServiceResult<T>.Failure("系统错误", 500)
        };
    }
}
```

## 五、实施计划

### 5.1 时间线

| 阶段 | 任务 | 预计工时 | 优先级 |
|-----|------|---------|--------|
| Week 1 | Server端Service统一 | 16h | P0 |
| Week 1 | Client端移除双层架构 | 16h | P0 |
| Week 2 | 删除ReadRepository层 | 8h | P1 |
| Week 2 | 优化Repository模式 | 12h | P1 |
| Week 3 | Service基类提取 | 12h | P1 |
| Week 3 | ApiClient统一 | 8h | P1 |
| Week 4 | ViewModel优化 | 8h | P2 |
| Week 4 | 文档更新 | 4h | P2 |

### 5.2 验收标准

1. **编译通过**：零编译错误和警告
2. **测试覆盖**：单元测试覆盖率>60%
3. **代码减少**：总代码量减少30%以上
4. **性能提升**：API响应时间降低20%
5. **文档完整**：架构文档与代码一致

## 六、风险控制

### 6.1 风险缓解策略

| 风险 | 缓解措施 |
|-----|---------|
| 功能回归 | 每个模块修改后立即测试 |
| 接口不兼容 | 保留旧接口，标记为Obsolete |
| 性能下降 | 进行性能基准测试对比 |
| 团队抵触 | 提供培训和过渡期 |

### 6.2 回滚计划

1. 使用Git分支策略，每个阶段一个分支
2. 保留原始代码备份
3. 准备快速回滚脚本
4. 设置功能开关（Feature Toggle）

## 七、预期收益

### 7.1 短期收益（1个月内）

- **开发效率提升30%**：减少文件跳转和代码量
- **调试时间减少40%**：简化的调用链
- **新功能开发加速50%**：统一的模式和基类

### 7.2 长期收益（3个月后）

- **维护成本降低60%**：代码更简洁易懂
- **团队培训时间减少70%**：架构简单统一
- **技术债务清零**：消除历史遗留问题

## 八、代码示例

### 8.1 重构前（5层架构）
```csharp
// Controller → Service → BusinessService → Repository → DbContext
[HttpPost]
public async Task<IActionResult> Create(UserCreateDto dto)
{
    var result = await _userService.CreateAsync(dto);          // 委托层
    // _userService → _businessService.CreateAsync(dto)        // 业务层
    // _businessService → _repository.CreateAsync(entity)      // 仓储层
    return Ok(result);
}
```

### 8.2 重构后（3层架构）
```csharp
// Controller → Service → Repository
[HttpPost]
public async Task<IActionResult> Create(UserCreateDto dto)
{
    var result = await _userService.CreateAsync(dto);          // 直接调用
    return Ok(result);
}
```

## 九、关键决策点

### 9.1 需要团队讨论的问题

1. 是否保留缓存层？如果保留，放在哪一层？
2. 是否使用CQRS模式区分读写？
3. 是否引入MediatR简化Controller？
4. 是否使用Repository模式还是直接使用DbContext？

### 9.2 架构决策记录（ADR）模板

```markdown
# ADR-001: 移除模块化双层架构

## 状态
提议

## 背景
模块化双层架构导致代码冗余，维护困难

## 决策
采用简单的三层架构

## 后果
- 正面：代码量减少40%，维护更简单
- 负面：需要重新培训团队
```

## 十、总结

本建议文档提供了详细的架构简化方案，重点是：

1. **删除冗余层次**，保持架构简单
2. **统一架构模式**，全栈保持一致
3. **提取公共基类**，减少重复代码
4. **渐进式实施**，降低风险

遵循这些建议，可以在1个月内完成主要重构，实现：
- 代码量减少30-40%
- 开发效率提升50%
- 维护成本降低60%

建议立即开始第一阶段的Service统一工作，这是整个重构的基础。

---

*本文档为架构改进的行动指南，请根据实际情况调整实施细节*