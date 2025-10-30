using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LYBT.Core.EventBus.Module;

/// <summary>
/// 模块基础实现类
/// 提供模块的基础功能实现
/// </summary>
public abstract class ModuleBase : IModule, IModuleLifecycle
{
    /// <summary>
    /// 日志记录器
    /// </summary>
    protected ILogger Logger { get; private set; } = default!;

    /// <summary>
    /// 服务提供程序
    /// </summary>
    protected IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// 配置对象
    /// </summary>
    protected IConfiguration? Configuration { get; private set; }

    /// <inheritdoc />
    public abstract ModuleDescriptor Descriptor { get; }

    /// <inheritdoc />
    public ModuleState State { get; private set; } = ModuleState.Uninitialized;

    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event EventHandler<ModuleStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// 构造函数
    /// </summary>
    protected ModuleBase()
    {
    }

    /// <inheritdoc />
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // 注册模块自身
        services.AddSingleton<IModule>(this);

        // 调用子类的服务配置
        OnConfigureServices(services, configuration);
    }

    /// <inheritdoc />
    public virtual void Configure(object app, object environment)
    {
        // 检查是否为ASP.NET Core应用程序构建器
        if (app is Microsoft.AspNetCore.Builder.IApplicationBuilder appBuilder)
        {
            ServiceProvider = appBuilder.ApplicationServices;
            Logger = ServiceProvider.GetRequiredService<ILoggerFactory>()
                                  .CreateLogger(GetType());
        }

        // 调用子类的应用配置
        OnConfigure(app, environment);
    }

    /// <inheritdoc />
    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (State != ModuleState.Uninitialized)
        {
            Logger?.LogWarning("模块 {ModuleName} 已经初始化，跳过重复初始化", Descriptor.Name);
            return;
        }

        try
        {
            ChangeState(ModuleState.Initializing);
            Logger?.LogInformation("开始初始化模块 {ModuleName} v{Version}", Descriptor.Name, Descriptor.Version);

            await OnInitializeAsync(cancellationToken);

            ChangeState(ModuleState.Initialized);
            Logger?.LogInformation("模块 {ModuleName} 初始化完成", Descriptor.Name);
        }
        catch (Exception ex)
        {
            ChangeState(ModuleState.Error);
            Logger?.LogError(ex, "模块 {ModuleName} 初始化失败", Descriptor.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!State.CanStart())
        {
            Logger?.LogWarning("模块 {ModuleName} 当前状态 {State} 不允许启动", Descriptor.Name, State);
            return;
        }

        try
        {
            ChangeState(ModuleState.Starting);
            Logger?.LogInformation("开始启动模块 {ModuleName}", Descriptor.Name);

            await OnStartAsync(cancellationToken);

            ChangeState(ModuleState.Running);
            Logger?.LogInformation("模块 {ModuleName} 启动完成", Descriptor.Name);
        }
        catch (Exception ex)
        {
            ChangeState(ModuleState.Error);
            Logger?.LogError(ex, "模块 {ModuleName} 启动失败", Descriptor.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!State.CanStop())
        {
            Logger?.LogWarning("模块 {ModuleName} 当前状态 {State} 不允许停止", Descriptor.Name, State);
            return;
        }

        try
        {
            ChangeState(ModuleState.Stopping);
            Logger?.LogInformation("开始停止模块 {ModuleName}", Descriptor.Name);

            await OnStopAsync(cancellationToken);

            ChangeState(ModuleState.Stopped);
            Logger?.LogInformation("模块 {ModuleName} 停止完成", Descriptor.Name);
        }
        catch (Exception ex)
        {
            ChangeState(ModuleState.Error);
            Logger?.LogError(ex, "模块 {ModuleName} 停止失败", Descriptor.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task DisposeAsync(CancellationToken cancellationToken = default)
    {
        if (State.IsDisposed())
        {
            Logger?.LogWarning("模块 {ModuleName} 已经被释放", Descriptor.Name);
            return;
        }

        try
        {
            ChangeState(ModuleState.Disposing);
            Logger?.LogInformation("开始清理模块 {ModuleName}", Descriptor.Name);

            // 如果模块正在运行，先停止
            if (State == ModuleState.Running)
            {
                await StopAsync(cancellationToken);
            }

            await OnDisposeAsync(cancellationToken);

            ChangeState(ModuleState.Disposed);
            Logger?.LogInformation("模块 {ModuleName} 清理完成", Descriptor.Name);
        }
        catch (Exception ex)
        {
            ChangeState(ModuleState.Error);
            Logger?.LogError(ex, "模块 {ModuleName} 清理失败", Descriptor.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<ModuleHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var healthData = await OnCheckHealthAsync(cancellationToken);
            var endTime = DateTime.UtcNow;

            var responseTime = (long)(endTime - startTime).TotalMilliseconds;

            // 如果子类返回了健康状态，使用它；否则根据模块状态判断
            if (healthData != null)
            {
                return ModuleHealthStatus.Create(
                    healthData.Status,
                    healthData.Description,
                    healthData.Data,
                    healthData.Exception,
                    responseTime);
            }

            // 根据模块状态返回默认健康状态
            return State switch
            {
                ModuleState.Running => ModuleHealthStatus.Healthy("模块运行正常"),
                ModuleState.Error => ModuleHealthStatus.Unhealthy("模块处于错误状态"),
                ModuleState.Stopped => ModuleHealthStatus.Degraded("模块已停止"),
                ModuleState.Disabled => ModuleHealthStatus.Degraded("模块已禁用"),
                _ => ModuleHealthStatus.Unknown($"模块状态: {State.GetDisplayName()}")
            };
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "模块 {ModuleName} 健康检查失败", Descriptor.Name);
            return ModuleHealthStatus.Unhealthy("健康检查失败", ex);
        }
    }

    /// <summary>
    /// 配置模块服务（子类重写）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    protected virtual void OnConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 默认实现为空，子类可重写
    }

    /// <summary>
    /// 配置模块（子类重写）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="env">环境信息</param>
    protected virtual void OnConfigure(object app, object environment)
    {
        // 默认实现为空，子类可重写
        // 子类可以检查app和environment的具体类型来处理特定平台
    }

    /// <summary>
    /// 初始化模块（子类重写）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化任务</returns>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 启动模块（子类重写）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动任务</returns>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止模块（子类重写）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 清理模块（子类重写）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理任务</returns>
    protected virtual Task OnDisposeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 检查模块健康状态（子类重写）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康状态</returns>
    protected virtual Task<ModuleHealthStatus?> OnCheckHealthAsync(CancellationToken cancellationToken) =>
        Task.FromResult<ModuleHealthStatus?>(null);

    /// <summary>
    /// 改变模块状态
    /// </summary>
    /// <param name="newState">新状态</param>
    protected void ChangeState(ModuleState newState)
    {
        var oldState = State;
        State = newState;

        Logger?.LogDebug("模块 {ModuleName} 状态从 {OldState} 变更为 {NewState}",
            Descriptor.Name, oldState.GetDisplayName(), newState.GetDisplayName());

        StateChanged?.Invoke(this, new ModuleStateChangedEventArgs(this, oldState, newState));
    }
}

/// <summary>
/// 模块状态变更事件参数
/// </summary>
public class ModuleStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 模块实例
    /// </summary>
    public IModule Module { get; }

    /// <summary>
    /// 旧状态
    /// </summary>
    public ModuleState OldState { get; }

    /// <summary>
    /// 新状态
    /// </summary>
    public ModuleState NewState { get; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime ChangeTime { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="module">模块实例</param>
    /// <param name="oldState">旧状态</param>
    /// <param name="newState">新状态</param>
    public ModuleStateChangedEventArgs(IModule module, ModuleState oldState, ModuleState newState)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        OldState = oldState;
        NewState = newState;
        ChangeTime = DateTime.UtcNow;
    }
}
