namespace LYBT.EventBus.Module;

/// <summary>
/// 模块管理器接口
/// 负责模块的注册、发现、生命周期管理和依赖解析
/// </summary>
public interface IModuleManager
{
    /// <summary>
    /// 所有已注册的模块
    /// </summary>
    IReadOnlyCollection<IModule> Modules { get; }

    /// <summary>
    /// 所有运行中的模块
    /// </summary>
    IReadOnlyCollection<IModule> RunningModules { get; }

    /// <summary>
    /// 模块状态变更事件
    /// </summary>
    event EventHandler<ModuleStateChangedEventArgs> ModuleStateChanged;

    /// <summary>
    /// 注册模块
    /// </summary>
    /// <param name="module">模块实例</param>
    /// <returns>注册结果</returns>
    Task<ModuleRegistrationResult> RegisterModuleAsync(IModule module);

    /// <summary>
    /// 批量注册模块
    /// </summary>
    /// <param name="modules">模块集合</param>
    /// <returns>注册结果集合</returns>
    Task<IReadOnlyList<ModuleRegistrationResult>> RegisterModulesAsync(IEnumerable<IModule> modules);

    /// <summary>
    /// 取消注册模块
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>是否成功</returns>
    Task<bool> UnregisterModuleAsync(string moduleId);

    /// <summary>
    /// 根据ID获取模块
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>模块实例</returns>
    IModule? GetModule(string moduleId);

    /// <summary>
    /// 根据类型获取模块
    /// </summary>
    /// <typeparam name="T">模块类型</typeparam>
    /// <returns>模块实例</returns>
    T? GetModule<T>() where T : class, IModule;

    /// <summary>
    /// 根据类别获取模块
    /// </summary>
    /// <param name="category">模块类别</param>
    /// <returns>模块集合</returns>
    IReadOnlyCollection<IModule> GetModulesByCategory(ModuleCategory category);

    /// <summary>
    /// 根据标签获取模块
    /// </summary>
    /// <param name="tag">标签</param>
    /// <returns>模块集合</returns>
    IReadOnlyCollection<IModule> GetModulesByTag(string tag);

    /// <summary>
    /// 初始化所有模块
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化结果</returns>
    Task<ModuleOperationResult> InitializeAllModulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动所有模块
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动结果</returns>
    Task<ModuleOperationResult> StartAllModulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止所有模块
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止结果</returns>
    Task<ModuleOperationResult> StopAllModulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动指定模块
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> StartModuleAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止指定模块
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> StopModuleAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重启指定模块
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RestartModuleAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查模块依赖关系
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>依赖检查结果</returns>
    ModuleDependencyResult CheckDependencies(string moduleId);

    /// <summary>
    /// 解析模块启动顺序
    /// </summary>
    /// <returns>按依赖关系排序的模块列表</returns>
    IReadOnlyList<IModule> ResolveStartupOrder();

    /// <summary>
    /// 验证模块配置
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>验证结果</returns>
    ModuleValidationResult ValidateModule(string moduleId);

    /// <summary>
    /// 验证所有模块
    /// </summary>
    /// <returns>验证结果</returns>
    ModuleValidationResult ValidateAllModules();

    /// <summary>
    /// 获取模块健康状态
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康状态</returns>
    Task<ModuleHealthStatus> GetModuleHealthAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有模块健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康状态集合</returns>
    Task<IReadOnlyDictionary<string, ModuleHealthStatus>> GetAllModuleHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取模块统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    ModuleStatistics GetStatistics();

    /// <summary>
    /// 启用模块
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>是否成功</returns>
    Task<bool> EnableModuleAsync(string moduleId);

    /// <summary>
    /// 禁用模块
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DisableModuleAsync(string moduleId);

    /// <summary>
    /// 检查模块是否已启用
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>是否已启用</returns>
    bool IsModuleEnabled(string moduleId);

    /// <summary>
    /// 搜索模块
    /// </summary>
    /// <param name="searchTerm">搜索词</param>
    /// <returns>匹配的模块集合</returns>
    IReadOnlyCollection<IModule> SearchModules(string searchTerm);
}

/// <summary>
/// 模块注册结果
/// </summary>
public class ModuleRegistrationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 模块ID
    /// </summary>
    public string ModuleId { get; init; } = string.Empty;

    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 验证结果
    /// </summary>
    public ModuleValidationResult? ValidationResult { get; init; }

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime RegistrationTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleName">模块名称</param>
    /// <returns>注册结果</returns>
    public static ModuleRegistrationResult Success(string moduleId, string moduleName)
    {
        return new ModuleRegistrationResult
        {
            IsSuccess = true,
            ModuleId = moduleId,
            ModuleName = moduleName
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="error">错误消息</param>
    /// <param name="validationResult">验证结果</param>
    /// <returns>注册结果</returns>
    public static ModuleRegistrationResult Failure(string moduleId, string moduleName, string error, ModuleValidationResult? validationResult = null)
    {
        return new ModuleRegistrationResult
        {
            IsSuccess = false,
            ModuleId = moduleId,
            ModuleName = moduleName,
            ErrorMessage = error,
            ValidationResult = validationResult
        };
    }
}

/// <summary>
/// 模块操作结果
/// </summary>
public class ModuleOperationResult
{
    /// <summary>
    /// 是否全部成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 成功的模块数量
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// 失败的模块数量
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// 操作详情
    /// </summary>
    public IReadOnlyList<ModuleOperationDetail> Details { get; init; } = Array.Empty<ModuleOperationDetail>();

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 总耗时（毫秒）
    /// </summary>
    public long TotalTimeMs { get; init; }

    /// <summary>
    /// 获取失败的模块详情
    /// </summary>
    /// <returns>失败的模块详情</returns>
    public IEnumerable<ModuleOperationDetail> GetFailures()
    {
        return Details.Where(d => !d.IsSuccess);
    }

    /// <summary>
    /// 获取成功的模块详情
    /// </summary>
    /// <returns>成功的模块详情</returns>
    public IEnumerable<ModuleOperationDetail> GetSuccesses()
    {
        return Details.Where(d => d.IsSuccess);
    }
}

/// <summary>
/// 模块操作详情
/// </summary>
public class ModuleOperationDetail
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public string ModuleId { get; init; } = string.Empty;

    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// 操作耗时（毫秒）
    /// </summary>
    public long ElapsedMs { get; init; }
}

/// <summary>
/// 模块依赖结果
/// </summary>
public class ModuleDependencyResult
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public string ModuleId { get; init; } = string.Empty;

    /// <summary>
    /// 依赖关系是否满足
    /// </summary>
    public bool IsSatisfied { get; init; }

    /// <summary>
    /// 缺失的依赖
    /// </summary>
    public IReadOnlyList<string> MissingDependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 循环依赖
    /// </summary>
    public IReadOnlyList<string> CircularDependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 可选依赖状态
    /// </summary>
    public IReadOnlyDictionary<string, bool> OptionalDependencies { get; init; } =
        new Dictionary<string, bool>();
}

/// <summary>
/// 模块统计信息
/// </summary>
public class ModuleStatistics
{
    /// <summary>
    /// 总模块数
    /// </summary>
    public int TotalModules { get; init; }

    /// <summary>
    /// 运行中的模块数
    /// </summary>
    public int RunningModules { get; init; }

    /// <summary>
    /// 已停止的模块数
    /// </summary>
    public int StoppedModules { get; init; }

    /// <summary>
    /// 错误状态的模块数
    /// </summary>
    public int ErrorModules { get; init; }

    /// <summary>
    /// 禁用的模块数
    /// </summary>
    public int DisabledModules { get; init; }

    /// <summary>
    /// 按类别分组的模块数
    /// </summary>
    public IReadOnlyDictionary<ModuleCategory, int> ModulesByCategory { get; init; } =
        new Dictionary<ModuleCategory, int>();

    /// <summary>
    /// 按状态分组的模块数
    /// </summary>
    public IReadOnlyDictionary<ModuleState, int> ModulesByState { get; init; } =
        new Dictionary<ModuleState, int>();

    /// <summary>
    /// 平均启动时间（毫秒）
    /// </summary>
    public double AverageStartupTimeMs { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}
