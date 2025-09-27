# LYBT架构改进实施方案

> 版本：1.0  
> 日期：2025-09-25  
> 目标：解决架构分析报告中发现的问题，建立健壮、可维护的系统架构

## 一、改进原则

1. **渐进式改进**：不影响现有功能，逐步重构
2. **业务优先**：优先实现业务价值，架构服务于业务
3. **测试驱动**：每个改进都要有测试保障
4. **最小改动**：用最小的改动获得最大的收益

## 二、优先级分级

### P0 - 立即修复（1-2天）
必须立即修复的严重问题，影响系统稳定性

### P1 - 本周完成（3-5天）
重要的架构问题，影响开发效率和代码质量

### P2 - 本月完成（2-4周）
改进型任务，提升系统性能和可维护性

### P3 - 计划内完成（1-2月）
长期优化任务，完善架构设计

## 三、具体改进方案

### 🔴 P0级任务：立即修复

#### 1. 修复内存泄漏问题（8小时）

**问题描述**：Desktop端ViewModel事件订阅未释放

**解决方案**：
```csharp
// 1. 为所有ViewModel添加IDisposable接口
public abstract class ViewModelBase : BindableBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    protected void RegisterDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }
    
    public virtual void Dispose()
    {
        _disposables?.Dispose();
    }
}

// 2. 修改所有ViewModel的事件订阅
public class PatientManagementViewModel : ViewModelBase
{
    public PatientManagementViewModel(IEventAggregator eventAggregator)
    {
        var token = eventAggregator.GetEvent<PatientUpdatedEvent>()
            .Subscribe(OnPatientUpdated);
        RegisterDisposable(token); // 自动管理订阅
    }
}
```

**实施步骤**：
1. 修改ViewModelBase基类添加Dispose模式
2. 全局搜索`.Subscribe(`，添加订阅管理
3. 在View的Unloaded事件中调用Dispose
4. 添加内存泄漏检测测试

#### 2. 移除Container.Resolve反模式（4小时）

**问题描述**：App.xaml.cs中大量直接调用Container.Resolve

**解决方案**：
```csharp
// 改用构造函数注入
public partial class App : PrismApplication
{
    private readonly IApplicationInitializationService _initService;
    
    // 通过属性注入（Prism支持）
    [Dependency]
    public IApplicationInitializationService InitService { get; set; }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _ = InitService.InitializeCoreServicesAsync();
    }
}
```

**实施步骤**：
1. 识别所有Container.Resolve调用
2. 改为构造函数注入或属性注入
3. 对于必须延迟解析的，使用`Func<T>`或`Lazy<T>`
4. 运行回归测试确保功能正常

#### 3. 修复BaseEntity的RowVersion（1小时）

**问题描述**：RowVersion不应手动初始化

**解决方案**：
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    [Timestamp]
    public byte[]? RowVersion { get; set; } // 改为可空，移除初始化
}
```

**实施步骤**：
1. 修改BaseEntity.cs
2. 检查所有使用RowVersion的地方
3. 更新数据库迁移（如需要）
4. 测试并发更新场景

### 🟡 P1级任务：本周完成

#### 4. 统一Service层架构（16小时）

**问题描述**：Service混合Query和Command职责

**解决方案**：
```csharp
// 分离为两个接口
public interface IUserQueryService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<PagedResult<UserDto>> GetPagedAsync(UserQueryDto query);
    Task<List<UserDto>> SearchAsync(string keyword);
}

public interface IUserCommandService  
{
    Task<UserDto> CreateAsync(UserCreateDto dto);
    Task<UserDto> UpdateAsync(Guid id, UserUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}

// 统一的Facade服务（可选）
public interface IUserService : IUserQueryService, IUserCommandService
{
}
```

**实施步骤**：
1. 创建Query和Command接口
2. 分离现有Service实现
3. 更新DI注册
4. 更新Controller引用
5. 逐模块迁移，保证兼容性

#### 5. 补充核心单元测试（24小时）

**问题描述**：测试覆盖率<5%

**解决方案**：
```csharp
// 使用测试基类简化测试编写
public abstract class ServiceTestBase<TService>
{
    protected Mock<ILogger<TService>> LoggerMock { get; }
    protected Mock<IMapper> MapperMock { get; }
    protected IFixture Fixture { get; }
    
    protected ServiceTestBase()
    {
        LoggerMock = new Mock<ILogger<TService>>();
        MapperMock = new Mock<IMapper>();
        Fixture = new Fixture();
    }
}

// 快速编写测试
public class UserServiceTests : ServiceTestBase<UserService>
{
    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsUser()
    {
        // Arrange
        var userId = Fixture.Create<Guid>();
        var user = Fixture.Create<User>();
        
        // Act & Assert
        // ...
    }
}
```

**实施步骤**：
1. 创建测试基类和辅助工具
2. 优先测试核心业务逻辑
3. 目标：核心模块覆盖率>60%
4. 集成到CI/CD流程

#### 6. 优化缓存策略（8小时）

**问题描述**：缓存键可能冲突

**解决方案**：
```csharp
public class CacheKeyBuilder
{
    public static string Build<TEntity>(object id) 
        => $"{typeof(TEntity).FullName}:{id}";
    
    public static string BuildWithTenant<TEntity>(Guid tenantId, object id)
        => $"{tenantId}:{typeof(TEntity).FullName}:{id}";
}

// 配置化缓存过期时间
public class CacheOptions
{
    public int DefaultExpirationMinutes { get; set; } = 60;
    public Dictionary<string, int> EntityExpirations { get; set; } = new();
}
```

**实施步骤**：
1. 创建CacheKeyBuilder工具类
2. 替换所有硬编码的缓存键
3. 添加缓存配置到appsettings
4. 添加缓存失效策略

### 🟢 P2级任务：本月完成

#### 7. 实现模块化加载（40小时）

**问题描述**：Shell启动时加载所有模块

**解决方案**：
```csharp
// 模块元数据
[Module(ModuleName = "Users", OnDemand = true)]
[ModuleDependency("Core")]
public class UsersModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }
    public void RegisterTypes(IContainerRegistry containerRegistry) { }
}

// 动态加载器
public class ModuleLoader
{
    public async Task LoadModuleAsync(string moduleName)
    {
        var moduleInfo = _moduleCatalog.Modules
            .FirstOrDefault(m => m.ModuleName == moduleName);
            
        if (moduleInfo != null)
        {
            await _moduleManager.LoadModuleAsync(moduleName);
        }
    }
}
```

**实施步骤**：
1. 添加模块元数据属性
2. 实现动态模块加载器
3. 修改Shell只加载核心模块
4. 基于用户角色按需加载
5. 添加模块加载进度提示

#### 8. 重构事件系统（16小时）

**问题描述**：事件定义分散，职责不清

**解决方案**：
```csharp
// 统一的事件基类
public abstract class DomainEventBase
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string UserId { get; set; }
}

// 分类明确的事件
namespace LYBT.Core.Events.Domain
{
    public class PatientCreatedEvent : DomainEventBase { }
    public class PatientUpdatedEvent : DomainEventBase { }
}

namespace LYBT.Core.Events.Application  
{
    public class NavigationRequestedEvent { }
    public class NotificationEvent { }
}
```

**实施步骤**：
1. 删除所有冗余事件定义
2. 按领域/应用分类重组事件
3. 统一事件命名规范
4. 添加事件文档
5. 逐步迁移现有事件使用

#### 9. 优化Repository层（24小时）

**问题描述**：Repository实现不一致，存在性能问题

**解决方案**：
```csharp
// 规范化的Repository基类
public abstract class RepositoryBase<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly IMemoryCache Cache;
    protected readonly ILogger Logger;
    
    // 统一的查询构建器
    protected IQueryable<TEntity> BuildQuery(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string? includeProperties = null,
        bool noTracking = true)
    {
        IQueryable<TEntity> query = Context.Set<TEntity>();
        
        if (noTracking)
            query = query.AsNoTracking();
            
        if (filter != null)
            query = query.Where(filter);
            
        // Include处理
        if (!string.IsNullOrEmpty(includeProperties))
        {
            foreach (var includeProperty in includeProperties.Split(','))
            {
                query = query.Include(includeProperty.Trim());
            }
        }
        
        return orderBy != null ? orderBy(query) : query;
    }
}
```

**实施步骤**：
1. 创建标准化Repository基类
2. 统一查询构建逻辑
3. 添加批量操作支持
4. 实现查询结果缓存
5. 添加性能监控

### 🔵 P3级任务：计划内完成

#### 10. 引入领域驱动设计（80小时）

**问题描述**：贫血模型，业务逻辑分散

**解决方案**：
```csharp
// 富领域模型示例
public class Patient : AggregateRoot
{
    private readonly List<Consultation> _consultations = new();
    
    public string Name { get; private set; }
    public Gender Gender { get; private set; }
    public IReadOnlyCollection<Consultation> Consultations => _consultations;
    
    // 业务行为封装在实体内
    public Consultation StartConsultation(Doctor doctor)
    {
        if (!IsEligibleForConsultation())
            throw new DomainException("患者当前不可就诊");
            
        var consultation = new Consultation(this, doctor);
        _consultations.Add(consultation);
        
        AddDomainEvent(new ConsultationStartedEvent(this.Id, consultation.Id));
        
        return consultation;
    }
    
    private bool IsEligibleForConsultation()
    {
        // 业务规则
        return !_consultations.Any(c => c.Status == ConsultationStatus.InProgress);
    }
}
```

**实施步骤**：
1. 识别核心域、支撑域、通用域
2. 定义聚合根和值对象
3. 将业务逻辑移入领域模型
4. 实现领域事件
5. 逐模块重构，保持向后兼容

#### 11. 实现CQRS模式（40小时）

**问题描述**：查询和命令混合，难以优化

**解决方案**：
```csharp
// 命令端
public class CreatePatientCommand : IRequest<PatientDto>
{
    public string Name { get; set; }
    public Gender Gender { get; set; }
}

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, PatientDto>
{
    public async Task<PatientDto> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        // 业务逻辑处理
    }
}

// 查询端 - 可直接查询只读数据库
public class GetPatientQuery : IRequest<PatientDto>
{
    public Guid PatientId { get; set; }
}

public class GetPatientQueryHandler : IRequestHandler<GetPatientQuery, PatientDto>
{
    public async Task<PatientDto> Handle(GetPatientQuery request, CancellationToken cancellationToken)
    {
        // 优化的查询逻辑
    }
}
```

**实施步骤**：
1. 引入MediatR库
2. 定义Command和Query对象
3. 实现处理器（Handler）
4. 更新Controller使用MediatR
5. 可选：实现读写分离

#### 12. 建立架构适应度函数（24小时）

**问题描述**：缺少架构质量保障机制

**解决方案**：
```csharp
// 架构测试示例
public class ArchitectureTests
{
    [Fact]
    public void Controllers_Should_Not_Reference_Infrastructure()
    {
        var result = Types.InAssembly(typeof(UsersController).Assembly)
            .That().ResideInNamespace("Controllers")
            .Should().NotHaveDependencyOn("Infrastructure")
            .GetResult();
            
        Assert.True(result.IsSuccessful);
    }
    
    [Fact] 
    public void Domain_Should_Not_Reference_Application()
    {
        // 领域层不应依赖应用层
    }
}
```

**实施步骤**：
1. 引入NetArchTest库
2. 定义架构规则
3. 编写架构测试
4. 集成到CI流程
5. 定期评审和更新规则

## 四、实施路线图

### 第1周：紧急修复
- [ ] P0-1：修复内存泄漏
- [ ] P0-2：移除Container.Resolve
- [ ] P0-3：修复BaseEntity

### 第2-3周：核心改进
- [ ] P1-4：Service层重构
- [ ] P1-5：补充单元测试
- [ ] P1-6：优化缓存策略

### 第4-8周：架构优化
- [ ] P2-7：模块化加载
- [ ] P2-8：重构事件系统
- [ ] P2-9：优化Repository

### 第9-16周：长期演进
- [ ] P3-10：领域驱动设计
- [ ] P3-11：CQRS模式
- [ ] P3-12：架构适应度函数

## 五、风险控制

### 5.1 回滚策略
1. 每个改进创建feature分支
2. 保留原有代码，使用特性开关切换
3. 充分测试后再合并主分支

### 5.2 监控指标
- 内存使用趋势
- 响应时间P95
- 错误率
- 测试覆盖率

### 5.3 质量关卡
- Code Review必须通过
- 单元测试必须通过
- 集成测试必须通过
- 性能测试无退化

## 六、预期收益

### 6.1 短期收益（1个月）
- **稳定性提升**：内存泄漏修复，系统更稳定
- **开发效率**：代码结构清晰，开发更快
- **质量保障**：测试覆盖提升，缺陷减少

### 6.2 中期收益（3个月）
- **性能优化**：查询性能提升30%
- **维护成本**：代码复杂度降低40%
- **扩展能力**：新功能开发周期缩短50%

### 6.3 长期收益（6个月）
- **架构演进**：支持微服务化拆分
- **团队成长**：代码质量意识提升
- **技术资产**：可复用组件库形成

## 七、资源需求

### 7.1 人力投入
- 高级开发：2人×3个月
- 中级开发：2人×3个月
- 测试工程师：1人×3个月

### 7.2 工具支持
- 性能分析工具：dotMemory/PerfView
- 代码质量工具：SonarQube/ReSharper
- 架构分析工具：NDepend

### 7.3 培训需求
- DDD领域驱动设计
- CQRS和事件溯源
- 性能优化最佳实践

## 八、成功标准

### 8.1 技术指标
- [ ] 零内存泄漏
- [ ] 测试覆盖率>60%
- [ ] 代码复杂度<10
- [ ] 响应时间<200ms

### 8.2 业务指标
- [ ] 系统可用性>99.9%
- [ ] 新功能交付周期<2周
- [ ] 生产环境零故障

### 8.3 团队指标
- [ ] Code Review覆盖100%
- [ ] 技术债务持续减少
- [ ] 知识分享会每周1次

## 九、总结

本改进方案遵循**渐进式改进**原则，优先解决影响系统稳定性的问题，然后逐步改善架构质量。通过分阶段实施，既保证了业务连续性，又能持续提升系统质量。

关键成功因素：
1. **管理层支持**：需要时间和资源投入
2. **团队共识**：全员认同改进必要性
3. **持续跟踪**：定期评审改进效果
4. **灵活调整**：根据实际情况调整计划

建议成立**架构改进小组**，专门负责推动和跟踪改进计划的实施，确保改进目标达成。

---
*本方案为初版，将根据实施反馈持续优化调整。*