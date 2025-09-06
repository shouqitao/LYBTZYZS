# UltraThink架构XML文档标准与模式

> 🎯 **目标**: 提供一致、高质量的XML文档注释，增强IntelliSense体验和代码可维护性

## 📋 概述

本文档定义了UltraThink双层架构中各组件的XML文档注释标准，确保开发团队生成一致、有用的代码文档，最大化Visual Studio IntelliSense的价值。

### 文档化原则

- **一致性**: 所有组件遵循统一的文档格式
- **实用性**: 文档提供有价值的信息，不仅仅是重复代码
- **简洁性**: 清晰简练，避免冗余描述
- **上下文相关**: 考虑调用者的需求和使用场景

## 🏗️ 架构组件文档模板

### 主Module类文档模板

```csharp
/// <summary>
/// {EntityName}模块 - UltraThink双层架构纯委托层
/// <para>职责：统一服务入口，请求路由分发到QueryService和BusinessService</para>
/// <para>架构：纯委托模式，不包含业务逻辑</para>
/// </summary>
/// <remarks>
/// 此类实现UltraThink双层架构模式，所有方法都是对QueryService和BusinessService的纯委托调用。
/// 如需添加业务逻辑，请修改对应的QueryService或BusinessService。
/// <para>依赖服务：</para>
/// <list type="bullet">
/// <item><see cref="I{EntityName}QueryService"/> - 查询操作专用服务</item>
/// <item><see cref="I{EntityName}BusinessService"/> - 业务逻辑和CRUD操作服务</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // 通过依赖注入获取服务
/// var {entityName}Service = serviceProvider.GetRequiredService&lt;I{EntityName}Service&gt;();
/// 
/// // 查询操作自动路由到QueryService
/// var result = await {entityName}Service.GetByIdAsync(id);
/// 
/// // 业务操作自动路由到BusinessService  
/// var createResult = await {entityName}Service.CreateAsync(dto);
/// </code>
/// </example>
public class {EntityName}Module(
    I{EntityName}QueryService queryService,
    I{EntityName}BusinessService businessService) : I{EntityName}Service
{
    private readonly I{EntityName}QueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    private readonly I{EntityName}BusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

    /// <summary>
    /// 根据ID获取{EntityName}详情
    /// </summary>
    /// <param name="id">要查询的{EntityName}唯一标识符</param>
    /// <returns>
    /// 包含{EntityName}详情的服务结果。成功时返回{EntityName}数据，失败时返回错误信息。
    /// </returns>
    /// <exception cref="ArgumentNullException">当<paramref name="id"/>为空Guid时抛出</exception>
    /// <remarks>
    /// 此方法委托给<see cref="I{EntityName}QueryService.GetByIdAsync"/>执行实际查询。
    /// 不会修改任何数据，仅执行读取操作。
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await {entityName}Service.GetByIdAsync(userId);
    /// if (result.IsSuccess)
    /// {
    ///     Console.WriteLine($"{EntityName}名称: {result.Data.Name}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"查询失败: {result.Message}");
    /// }
    /// </code>
    /// </example>
    public async Task<ServiceResult<{EntityName}Dto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);
}
```

### QueryService类文档模板

```csharp
/// <summary>
/// {EntityName}查询服务 - UltraThink双层架构查询专业层
/// <para>职责：{EntityName}信息查询、搜索过滤、数据检索和统计分析</para>
/// <para>特性：只读操作，不修改数据状态，专注查询性能优化</para>
/// </summary>
/// <remarks>
/// <para>此服务专门处理所有与{EntityName}相关的查询操作，包括：</para>
/// <list type="bullet">
/// <item>单个{EntityName}详情查询</item>
/// <item>分页列表查询和搜索</item>
/// <item>关键字模糊匹配搜索</item>
/// <item>存在性验证检查</item>
/// <item>统计和聚合查询（如需要）</item>
/// </list>
/// <para><strong>重要限制</strong>：此服务不能执行任何数据修改操作（Create/Update/Delete），
/// 所有修改操作应使用<see cref="I{EntityName}BusinessService"/>。</para>
/// </remarks>
/// <seealso cref="I{EntityName}BusinessService"/>
/// <seealso cref="I{EntityName}Api"/>
public class {EntityName}QueryService(
    ILogger<{EntityName}QueryService> logger,
    I{EntityName}Api {entityName}Api) : I{EntityName}QueryService
{
    private readonly ILogger<{EntityName}QueryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly I{EntityName}Api _{entityName}Api = {entityName}Api ?? throw new ArgumentNullException(nameof({entityName}Api));

    /// <summary>
    /// 根据ID获取{EntityName}详情信息
    /// </summary>
    /// <param name="id">要查询的{EntityName}唯一标识符，不能为空Guid</param>
    /// <returns>
    /// 包含{EntityName}详情的异步服务结果：
    /// <list type="bullet">
    /// <item><see cref="ServiceResult{T}.IsSuccess"/> = true：成功获取数据</item>
    /// <item><see cref="ServiceResult{T}.IsSuccess"/> = false：查询失败或{EntityName}不存在</item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">当<paramref name="id"/>为<see cref="Guid.Empty"/>时</exception>
    /// <remarks>
    /// <para><strong>性能说明</strong>：此方法会记录调试日志，在高频调用场景下请考虑日志级别。</para>
    /// <para><strong>缓存策略</strong>：当前未实现缓存，每次都会发起API调用。</para>
    /// <para><strong>错误处理</strong>：网络异常和API错误都会被捕获并转换为友好的错误消息。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 基本用法
    /// var result = await queryService.GetByIdAsync(id);
    /// if (result.IsSuccess)
    /// {
    ///     var {entityName} = result.Data;
    ///     Console.WriteLine($"找到{EntityName}: {{{entityName}.Name}}");
    /// }
    /// 
    /// // 错误处理
    /// if (!result.IsSuccess)
    /// {
    ///     _logger.LogWarning("查询{EntityName}失败: {{Message}}", result.Message);
    ///     await ShowErrorMessageAsync(result.Message);
    /// }
    /// </code>
    /// </example>
    public async Task<ServiceResult<{EntityName}Dto>> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("查询{EntityName}详情: {{Id}}", id);

            var refitResponse = await _{entityName}Api.GetByIdAsync(id);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    return ServiceResult<{EntityName}Dto>.Success(apiResponse.Data);
                }

                return ServiceResult<{EntityName}Dto>.Failure(apiResponse.Message ?? "获取{EntityName}详情失败");
            }

            return ServiceResult<{EntityName}Dto>.Failure("获取{EntityName}详情网络请求失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询{EntityName}详情异常: {{Id}}", id);
            return ServiceResult<{EntityName}Dto>.Failure("获取{EntityName}详情失败");
        }
    }

    /// <summary>
    /// 执行分页查询获取{EntityName}列表
    /// </summary>
    /// <param name="query">
    /// 分页查询参数，包含页码、页大小和可选的搜索关键字
    /// </param>
    /// <returns>
    /// 分页结果，包含当前页数据和分页元信息（总数、页码、页大小等）
    /// </returns>
    /// <exception cref="ArgumentNullException">当<paramref name="query"/>为null时</exception>
    /// <exception cref="ArgumentOutOfRangeException">当页码小于1或页大小超出限制时</exception>
    /// <remarks>
    /// <para><strong>分页限制</strong>：</para>
    /// <list type="bullet">
    /// <item>最小页码：1</item>
    /// <item>最大页大小：100</item>
    /// <item>默认页大小：20</item>
    /// </list>
    /// <para><strong>搜索支持</strong>：如果提供关键字，将在{EntityName}名称和描述字段中进行模糊匹配。</para>
    /// <para><strong>排序规则</strong>：结果按创建时间降序排列，最新的{EntityName}排在前面。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 基本分页查询
    /// var query = new {EntityName}PagedQueryDto
    /// {
    ///     PageIndex = 1,
    ///     PageSize = 10
    /// };
    /// var result = await queryService.GetPagedAsync(query);
    /// 
    /// // 带搜索的分页查询
    /// var searchQuery = new {EntityName}PagedQueryDto
    /// {
    ///     PageIndex = 1,
    ///     PageSize = 20,
    ///     Keyword = "关键字"
    /// };
    /// var searchResult = await queryService.GetPagedAsync(searchQuery);
    /// 
    /// // 处理分页结果
    /// if (result.IsSuccess)
    /// {
    ///     var pagedData = result.Data;
    ///     Console.WriteLine($"找到{{pagedData.TotalCount}}个{EntityName}");
    ///     foreach (var item in pagedData.Items)
    ///     {
    ///         Console.WriteLine($"- {{item.Name}}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public async Task<ServiceResult<PagedResult<{EntityName}Dto>>> GetPagedAsync({EntityName}PagedQueryDto query);
}
```

### BusinessService类文档模板

```csharp
/// <summary>
/// {EntityName}业务服务 - UltraThink双层架构业务逻辑层
/// <para>职责：处理{EntityName}业务逻辑、CRUD操作、状态管理、数据验证和业务流程编排</para>
/// <para>特性：包含所有数据修改操作，事务管理，业务规则验证</para>
/// </summary>
/// <remarks>
/// <para>此服务负责所有{EntityName}相关的业务操作，包括：</para>
/// <list type="bullet">
/// <item><strong>CRUD操作</strong>：创建、更新、删除{EntityName}</item>
/// <item><strong>状态管理</strong>：启用、禁用、状态转换</item>
/// <item><strong>业务验证</strong>：数据完整性、业务规则检查</item>
/// <item><strong>批量操作</strong>：批量创建、更新、状态变更</item>
/// <item><strong>事务协调</strong>：跨服务的事务处理</item>
/// </list>
/// 
/// <para><strong>架构约定</strong>：</para>
/// <list type="bullet">
/// <item>所有方法都返回<see cref="ServiceResult{T}"/>格式的结果</item>
/// <item>包含完整的异常处理和日志记录</item>
/// <item>参数验证在方法开始时执行</item>
/// <item>业务规则验证在API调用前执行</item>
/// </list>
/// 
/// <para><strong>事务管理</strong>：复杂业务操作会自动处理事务，确保数据一致性。</para>
/// </remarks>
/// <seealso cref="I{EntityName}QueryService"/>
/// <seealso cref="I{EntityName}Api"/>
/// <example>
/// <code>
/// // 创建{EntityName}的完整流程
/// var createDto = new {EntityName}MutationDto
/// {
///     Name = "{EntityName}名称",
///     Description = "描述信息"
/// };
/// 
/// var result = await businessService.CreateAsync(createDto);
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"创建成功: {{result.Data.Id}}");
/// }
/// else
/// {
///     Console.WriteLine($"创建失败: {{result.Message}}");
/// }
/// </code>
/// </example>
public class {EntityName}BusinessService(
    ILogger<{EntityName}BusinessService> logger,
    I{EntityName}Api {entityName}Api) : I{EntityName}BusinessService
{
    private readonly ILogger<{EntityName}BusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly I{EntityName}Api _{entityName}Api = {entityName}Api ?? throw new ArgumentNullException(nameof({entityName}Api));

    /// <summary>
    /// 创建新的{EntityName}
    /// </summary>
    /// <param name="createDto">
    /// 包含新{EntityName}信息的数据传输对象，不能为null
    /// </param>
    /// <returns>
    /// 创建操作的异步结果：
    /// <list type="bullet">
    /// <item>成功：返回新创建的{EntityName}详情，包含生成的ID和时间戳</item>
    /// <item>失败：返回具体的错误信息，可能包括验证错误或业务规则违反</item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException">当<paramref name="createDto"/>为null时</exception>
    /// <remarks>
    /// <para><strong>验证规则</strong>：</para>
    /// <list type="bullet">
    /// <item>必填字段不能为空</item>
    /// <item>名称长度限制：1-100字符</item>
    /// <item>名称唯一性检查（如适用）</item>
    /// <item>业务特定的验证规则</item>
    /// </list>
    /// 
    /// <para><strong>创建流程</strong>：</para>
    /// <list type="number">
    /// <item>参数null检查</item>
    /// <item>数据验证和业务规则检查</item>
    /// <item>调用后端API执行创建</item>
    /// <item>处理响应和错误</item>
    /// <item>记录操作日志</item>
    /// </list>
    /// 
    /// <para><strong>并发处理</strong>：此操作是线程安全的，可以并发调用。</para>
    /// <para><strong>事务处理</strong>：单个创建操作在后端是原子的，要么全部成功要么全部回滚。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 创建{EntityName}示例
    /// var createDto = new {EntityName}MutationDto
    /// {
    ///     Name = "新{EntityName}",
    ///     Description = "这是一个测试{EntityName}",
    ///     Status = {EntityName}Status.Active
    /// };
    /// 
    /// try
    /// {
    ///     var result = await businessService.CreateAsync(createDto);
    ///     if (result.IsSuccess)
    ///     {
    ///         var new{EntityName} = result.Data;
    ///         _logger.LogInformation("成功创建{EntityName}: {{Id}} - {{Name}}", 
    ///             new{EntityName}.Id, new{EntityName}.Name);
    ///         
    ///         // 可以继续使用创建的{EntityName}
    ///         await ProcessNew{EntityName}Async(new{EntityName});
    ///     }
    ///     else
    ///     {
    ///         // 处理业务错误
    ///         _logger.LogWarning("创建{EntityName}失败: {{Message}}", result.Message);
    ///         await ShowErrorToUserAsync(result.Message);
    ///     }
    /// }
    /// catch (Exception ex)
    /// {
    ///     _logger.LogError(ex, "创建{EntityName}时发生异常");
    ///     throw; // 或适当的错误处理
    /// }
    /// </code>
    /// </example>
    public async Task<ServiceResult<{EntityName}Dto>> CreateAsync({EntityName}MutationDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        try
        {
            _logger.LogInformation("开始处理{EntityName}创建");

            var refitResponse = await _{entityName}Api.CreateAsync(createDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    _logger.LogInformation("{EntityName}创建成功: {{Id}}", apiResponse.Data.Id);
                    return ServiceResult<{EntityName}Dto>.Success(apiResponse.Data);
                }

                _logger.LogWarning("{EntityName}创建业务失败: {{Message}}", apiResponse.Message);
                return ServiceResult<{EntityName}Dto>.Failure(apiResponse.Message ?? "创建{EntityName}失败，请检查输入信息");
            }

            _logger.LogWarning("{EntityName}创建HTTP请求失败，状态码: {{StatusCode}}", refitResponse.StatusCode);
            return ServiceResult<{EntityName}Dto>.Failure("创建{EntityName}网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{EntityName}创建过程发生异常");
            return ServiceResult<{EntityName}Dto>.Failure($"创建{EntityName}过程发生错误: {ex.Message}");
        }
    }
}
```

### 接口文档模板

```csharp
/// <summary>
/// {EntityName}服务主接口 - UltraThink双层架构统一入口
/// <para>提供{EntityName}相关的所有业务操作，包括查询、创建、更新、删除和状态管理</para>
/// </summary>
/// <remarks>
/// <para>此接口是{EntityName}模块的统一服务入口点，遵循UltraThink双层架构设计原则：</para>
/// <list type="bullet">
/// <item><strong>查询操作</strong>：委托给<see cref="I{EntityName}QueryService"/>执行</item>
/// <item><strong>业务操作</strong>：委托给<see cref="I{EntityName}BusinessService"/>执行</item>
/// <item><strong>统一响应</strong>：所有方法返回<see cref="ServiceResult{T}"/>格式</item>
/// </list>
/// 
/// <para><strong>使用场景</strong>：</para>
/// <list type="bullet">
/// <item>ViewModel层调用业务逻辑</item>
/// <item>控制器层处理Web请求</item>
/// <item>测试场景的mock对象</item>
/// <item>跨模块的服务调用</item>
/// </list>
/// 
/// <para><strong>依赖注入</strong>：建议通过构造函数注入获取此服务：</para>
/// <code>
/// public class {EntityName}ViewModel(I{EntityName}Service {entityName}Service)
/// {
///     private readonly I{EntityName}Service _{entityName}Service = {entityName}Service;
/// }
/// </code>
/// </remarks>
/// <seealso cref="I{EntityName}QueryService"/>
/// <seealso cref="I{EntityName}BusinessService"/>
/// <example>
/// <code>
/// // 依赖注入注册
/// services.AddSingleton&lt;I{EntityName}Service, {EntityName}Module&gt;();
/// 
/// // 在ViewModel中使用
/// public class {EntityName}ViewModel
/// {
///     private readonly I{EntityName}Service _{entityName}Service;
///     
///     public {EntityName}ViewModel(I{EntityName}Service {entityName}Service)
///     {
///         _{entityName}Service = {entityName}Service;
///     }
///     
///     public async Task Load{EntityName}Async(Guid id)
///     {
///         var result = await _{entityName}Service.GetByIdAsync(id);
///         if (result.IsSuccess)
///         {
///             Current{EntityName} = result.Data;
///         }
///     }
/// }
/// </code>
/// </example>
public interface I{EntityName}Service
{
    /// <summary>
    /// 根据ID获取{EntityName}详情
    /// </summary>
    /// <param name="id">要查询的{EntityName}唯一标识符</param>
    /// <returns>包含{EntityName}详情的异步操作结果</returns>
    /// <remarks>
    /// 此操作是只读的，不会修改任何数据。查询结果会包含{EntityName}的所有基本信息。
    /// </remarks>
    Task<ServiceResult<{EntityName}Dto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 分页查询{EntityName}列表
    /// </summary>
    /// <param name="query">分页查询参数，包含页码、页大小和可选搜索条件</param>
    /// <returns>包含分页{EntityName}列表的异步操作结果</returns>
    /// <remarks>
    /// 支持关键字搜索和分页，适用于列表展示场景。
    /// </remarks>
    Task<ServiceResult<PagedResult<{EntityName}Dto>>> GetPagedAsync({EntityName}PagedQueryDto query);

    /// <summary>
    /// 创建新的{EntityName}
    /// </summary>
    /// <param name="dto">{EntityName}创建信息</param>
    /// <returns>包含新创建{EntityName}详情的异步操作结果</returns>
    /// <remarks>
    /// 此操作会验证输入数据并创建新的{EntityName}记录。
    /// </remarks>
    Task<ServiceResult<{EntityName}Dto>> CreateAsync({EntityName}MutationDto dto);

    /// <summary>
    /// 更新{EntityName}信息
    /// </summary>
    /// <param name="dto">包含更新信息的{EntityName}数据</param>
    /// <returns>包含更新后{EntityName}详情的异步操作结果</returns>
    /// <remarks>
    /// 此操作会验证输入数据并更新现有的{EntityName}记录。
    /// </remarks>
    Task<ServiceResult<{EntityName}Dto>> UpdateAsync({EntityName}MutationDto dto);

    /// <summary>
    /// 删除{EntityName}
    /// </summary>
    /// <param name="id">要删除的{EntityName}ID</param>
    /// <returns>删除操作的异步结果</returns>
    /// <remarks>
    /// <para><strong>注意</strong>：删除操作通常是逻辑删除，数据仍保留在数据库中。</para>
    /// <para>如果{EntityName}被其他记录引用，删除操作可能会失败。</para>
    /// </remarks>
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
```

### Prism模块文档模板

```csharp
/// <summary>
/// {EntityName}管理模块 - UltraThink双层架构Prism模块
/// <para>负责{EntityName}模块的服务注册、视图注册和模块初始化</para>
/// <para>对应后端：LYBT.Module.{EntityName}</para>
/// </summary>
/// <remarks>
/// <para>此模块负责{EntityName}相关组件的依赖注入配置，遵循UltraThink双层架构标准：</para>
/// <list type="bullet">
/// <item><strong>服务层注册</strong>：QueryService、BusinessService、主Module</item>
/// <item><strong>视图层注册</strong>：Views和ViewModels的导航注册</item>
/// <item><strong>API客户端</strong>：Refit HTTP客户端配置（在Shell中配置）</item>
/// </list>
/// 
/// <para><strong>注册策略</strong>：</para>
/// <list type="bullet">
/// <item>所有服务使用单例模式注册</item>
/// <item>主Module通过工厂模式映射到接口</item>
/// <item>视图和ViewModel为导航注册</item>
/// </list>
/// 
/// <para><strong>模块生命周期</strong>：</para>
/// <list type="number">
/// <item><see cref="RegisterTypes"/>：注册服务和视图</item>
/// <item><see cref="OnInitialized"/>：模块初始化完成</item>
/// </list>
/// </remarks>
/// <seealso cref="I{EntityName}Service"/>
/// <seealso cref="{EntityName}Module"/>
/// <example>
/// <code>
/// // 在App.xaml.cs中注册模块
/// protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
/// {
///     moduleCatalog.AddModule&lt;{EntityName}Module&gt;();
/// }
/// 
/// // 服务使用示例
/// var {entityName}Service = Container.Resolve&lt;I{EntityName}Service&gt;();
/// </code>
/// </example>
public class {EntityName}Module : IModule
{
    /// <summary>
    /// 模块初始化完成回调
    /// </summary>
    /// <param name="containerProvider">容器提供程序，用于解析已注册的服务</param>
    /// <remarks>
    /// 此方法在所有服务注册完成后调用，可用于执行模块特定的初始化逻辑。
    /// 当前实现为空，如需要可添加：
    /// <list type="bullet">
    /// <item>缓存预热</item>
    /// <item>后台任务启动</item>
    /// <item>事件订阅</item>
    /// </list>
    /// </remarks>
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化完成后的操作
        // 例如：预加载数据、订阅事件等
    }

    /// <summary>
    /// 注册模块相关的服务和视图类型
    /// </summary>
    /// <param name="containerRegistry">容器注册器，用于注册服务依赖</param>
    /// <remarks>
    /// <para>注册顺序很重要，依赖服务必须先注册：</para>
    /// <list type="number">
    /// <item><strong>基础服务</strong>：QueryService和BusinessService</item>
    /// <item><strong>主服务</strong>：Module类及其接口映射</item>
    /// <item><strong>UI组件</strong>：Views和ViewModels</item>
    /// </list>
    /// 
    /// <para><strong>生命周期管理</strong>：</para>
    /// <list type="bullet">
    /// <item>服务类：单例模式，应用程序生命周期内保持单个实例</item>
    /// <item>ViewModel：导航时创建，页面销毁时释放</item>
    /// <item>View：与ViewModel生命周期绑定</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 手动解析服务示例（通常不需要）
    /// var queryService = containerRegistry.GetContainer().Resolve&lt;I{EntityName}QueryService&gt;();
    /// var businessService = containerRegistry.GetContainer().Resolve&lt;I{EntityName}BusinessService&gt;();
    /// </code>
    /// </example>
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // UltraThink双层架构：注册Query和Business服务
        // 注意：这些服务的API依赖在Shell模块中配置
        containerRegistry.RegisterSingleton<I{EntityName}QueryService, {EntityName}QueryService>();
        containerRegistry.RegisterSingleton<I{EntityName}BusinessService, {EntityName}BusinessService>();
        
        // UltraThink架构：注册主Module并映射到服务接口
        // 使用工厂模式确保依赖正确注入
        containerRegistry.RegisterSingleton<{EntityName}Module>();
        containerRegistry.RegisterSingleton<I{EntityName}Service>(container => 
            container.Resolve<{EntityName}Module>());

        // 注册视图和视图模型用于导航
        // 主管理页面
        containerRegistry.RegisterForNavigation<{EntityName}ManagementView, {EntityName}ManagementViewModel>();
        
        // 添加/编辑对话框
        containerRegistry.RegisterForNavigation<{EntityName}AddEditDialog, {EntityName}AddEditDialogViewModel>();
        
        // 详情页面（可选）
        // containerRegistry.RegisterForNavigation<{EntityName}DetailView, {EntityName}DetailViewModel>();
    }
}
```

## 📚 常见场景文档示例

### 异步方法文档

```csharp
/// <summary>
/// 批量更新{EntityName}状态
/// </summary>
/// <param name="ids">要更新的{EntityName}ID列表，不能为null或空</param>
/// <param name="status">目标状态值</param>
/// <param name="cancellationToken">取消令牌，用于取消长时间运行的操作</param>
/// <returns>
/// 批量更新结果，包含成功更新的数量和失败详情
/// </returns>
/// <exception cref="ArgumentNullException">当<paramref name="ids"/>为null时</exception>
/// <exception cref="ArgumentException">当<paramref name="ids"/>为空集合时</exception>
/// <exception cref="OperationCanceledException">当操作被取消时</exception>
/// <remarks>
/// <para><strong>批处理策略</strong>：此方法会将ID列表分批处理，避免单次请求数据量过大。</para>
/// <para><strong>并发控制</strong>：最多同时处理5个批次，避免服务器过载。</para>
/// <para><strong>错误处理</strong>：部分失败的情况下，会返回成功数量和失败原因。</para>
/// <para><strong>性能建议</strong>：建议批量大小不超过100个ID。</para>
/// </remarks>
/// <example>
/// <code>
/// // 批量启用{EntityName}
/// var ids = selectedItems.Select(x => x.Id).ToList();
/// var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
/// 
/// try
/// {
///     var result = await businessService.BatchUpdateStatusAsync(ids, Status.Active, cts.Token);
///     if (result.IsSuccess)
///     {
///         var updateResult = result.Data;
///         Console.WriteLine($"成功更新{{updateResult.SuccessCount}}个，失败{{updateResult.FailedCount}}个");
///     }
/// }
/// catch (OperationCanceledException)
/// {
///     Console.WriteLine("操作已取消");
/// }
/// </code>
/// </example>
public async Task<ServiceResult<BatchUpdateResult>> BatchUpdateStatusAsync(
    List<Guid> ids, 
    Status status, 
    CancellationToken cancellationToken = default)
```

### 泛型方法文档

```csharp
/// <summary>
/// 通用搜索方法，支持多种搜索条件类型
/// </summary>
/// <typeparam name="TSearchCriteria">搜索条件类型，必须实现<see cref="ISearchCriteria"/></typeparam>
/// <param name="criteria">具体的搜索条件实例</param>
/// <returns>符合条件的{EntityName}列表</returns>
/// <remarks>
/// <para>此方法支持多种搜索条件类型：</para>
/// <list type="bullet">
/// <item><see cref="TextSearchCriteria"/>：文本关键字搜索</item>
/// <item><see cref="DateRangeSearchCriteria"/>：日期范围搜索</item>
/// <item><see cref="StatusSearchCriteria"/>：状态筛选搜索</item>
/// </list>
/// <para>可以组合使用多个条件类型获得精确的搜索结果。</para>
/// </remarks>
/// <example>
/// <code>
/// // 文本搜索示例
/// var textCriteria = new TextSearchCriteria("关键字");
/// var textResult = await queryService.SearchAsync(textCriteria);
/// 
/// // 日期范围搜索示例  
/// var dateRange = new DateRangeSearchCriteria(startDate, endDate);
/// var dateResult = await queryService.SearchAsync(dateRange);
/// </code>
/// </example>
public async Task<ServiceResult<List<{EntityName}Dto>>> SearchAsync<TSearchCriteria>(TSearchCriteria criteria)
    where TSearchCriteria : class, ISearchCriteria
```

### 事件和委托文档

```csharp
/// <summary>
/// {EntityName}状态发生变化时触发的事件
/// </summary>
/// <remarks>
/// <para>此事件在以下情况下触发：</para>
/// <list type="bullet">
/// <item>单个{EntityName}状态更改</item>
/// <item>批量状态更新完成</item>
/// <item>状态重置操作</item>
/// </list>
/// <para><strong>订阅建议</strong>：在UI层订阅此事件以及时更新界面状态。</para>
/// <para><strong>线程安全</strong>：事件可能在后台线程触发，UI更新需要调度到主线程。</para>
/// </remarks>
/// <example>
/// <code>
/// // 订阅状态变化事件
/// {entityName}Service.StatusChanged += On{EntityName}StatusChanged;
/// 
/// private void On{EntityName}StatusChanged(object sender, {EntityName}StatusChangedEventArgs e)
/// {
///     // 确保UI更新在主线程执行
///     Dispatcher.Invoke(() =>
///     {
///         UpdateUI{EntityName}Status(e.{EntityName}Id, e.NewStatus);
///     });
/// }
/// 
/// // 取消订阅避免内存泄漏
/// {entityName}Service.StatusChanged -= On{EntityName}StatusChanged;
/// </code>
/// </example>
public event EventHandler<{EntityName}StatusChangedEventArgs> StatusChanged;
```

## 🎯 IntelliSense优化技巧

### 使用结构化注释

```csharp
/// <summary>
/// 高级{EntityName}查询方法
/// <para><strong>功能特点</strong>：</para>
/// <list type="bullet">
/// <item>支持复杂条件组合</item>
/// <item>自动性能优化</item>
/// <item>结果缓存支持</item>
/// </list>
/// </summary>
/// <param name="criteria">
/// 查询条件配置：
/// <list type="table">
/// <listheader>
/// <term>属性</term>
/// <description>说明</description>
/// </listheader>
/// <item>
/// <term><see cref="AdvancedSearchCriteria.Keywords"/></term>
/// <description>关键字列表，支持AND/OR逻辑</description>
/// </item>
/// <item>
/// <term><see cref="AdvancedSearchCriteria.Filters"/></term>
/// <description>属性过滤器配置</description>
/// </item>
/// </list>
/// </param>
/// <returns>
/// <para>查询结果对象包含：</para>
/// <list type="bullet">
/// <item><c>Items</c>：匹配的{EntityName}列表</item>
/// <item><c>Statistics</c>：查询统计信息</item>
/// <item><c>Suggestions</c>：搜索建议（如适用）</item>
/// </list>
/// </returns>
public async Task<ServiceResult<AdvancedSearchResult<{EntityName}Dto>>> AdvancedSearchAsync(AdvancedSearchCriteria criteria)
```

### 提供使用示例

```csharp
/// <summary>
/// 执行{EntityName}数据导入操作
/// </summary>
/// <param name="importData">导入的{EntityName}数据集合</param>
/// <param name="importOptions">导入选项配置</param>
/// <param name="progress">进度报告回调</param>
/// <returns>导入操作结果，包含成功和失败的详细信息</returns>
/// <example>
/// <para><strong>基本导入示例</strong>：</para>
/// <code>
/// var importData = LoadFromExcel("data.xlsx");
/// var options = new ImportOptions 
/// { 
///     SkipDuplicates = true, 
///     ValidateBeforeImport = true 
/// };
/// 
/// var progress = new Progress&lt;ImportProgress&gt;(p => 
/// {
///     Console.WriteLine($"导入进度: {{p.Percentage}}%");
/// });
/// 
/// var result = await businessService.ImportAsync(importData, options, progress);
/// if (result.IsSuccess)
/// {
///     var summary = result.Data;
///     Console.WriteLine($"导入完成：成功{{summary.SuccessCount}}，失败{{summary.FailedCount}}");
/// }
/// </code>
/// 
/// <para><strong>高级配置示例</strong>：</para>
/// <code>
/// var advancedOptions = new ImportOptions
/// {
///     BatchSize = 50,                    // 批处理大小
///     SkipDuplicates = true,            // 跳过重复项
///     ValidateBeforeImport = true,      // 导入前验证
///     CreateMissingReferences = false,  // 不创建缺失引用
///     OnDuplicateAction = DuplicateAction.Update  // 重复时更新
/// };
/// 
/// var result = await businessService.ImportAsync(importData, advancedOptions, progress);
/// </code>
/// </example>
public async Task<ServiceResult<ImportSummary>> ImportAsync(
    IEnumerable<{EntityName}ImportDto> importData,
    ImportOptions importOptions,
    IProgress<ImportProgress> progress = null)
```

## 📋 文档质量检查清单

### 必需元素
- [ ] `<summary>`标签提供清晰的方法描述
- [ ] 所有参数包含`<param>`标签和说明
- [ ] 返回值包含`<returns>`标签和详细说明
- [ ] 可能的异常包含`<exception>`标签
- [ ] 复杂逻辑包含`<remarks>`说明

### 可选增强元素
- [ ] `<example>`标签提供使用示例
- [ ] `<seealso>`标签关联相关类型和方法
- [ ] 结构化列表说明复杂参数或返回值
- [ ] 性能注意事项和最佳实践建议
- [ ] 线程安全性和并发考虑

### 质量标准
- [ ] 文档内容准确反映代码实际行为
- [ ] 避免重复代码信息，提供额外价值
- [ ] 使用一致的术语和格式
- [ ] 示例代码可编译且有意义
- [ ] 考虑了不同技能水平的开发者需求

## 🔧 工具集成

### Visual Studio设置

```xml
<!-- 在项目文件中启用XML文档生成 -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- 禁用缺失XML文档警告 -->
</PropertyGroup>
```

### 文档生成工具

推荐工具链：
- **DocFX**: 生成API文档网站
- **Sandcastle**: .NET文档生成工具
- **XML Documentation Comments Analyzer**: 实时文档质量检查

### 代码片段集成

在Visual Studio中创建XML文档代码片段，快速插入标准格式的文档注释。

---

## 📞 维护与更新

### 文档同步策略
- 代码变更时同步更新XML文档
- 定期审查文档准确性和完整性
- 根据用户反馈改进文档质量

### 团队协作
- 建立文档审查流程
- 共享文档编写最佳实践
- 持续改进文档模板和标准

**良好的文档是代码的延伸，是团队协作的桥梁！**