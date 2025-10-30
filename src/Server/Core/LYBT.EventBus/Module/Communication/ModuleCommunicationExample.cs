using LYBT.Core.EventBus.Abstractions;
using LYBT.Core.EventBus.Module.Events;
using Microsoft.Extensions.Logging;

namespace LYBT.EventBus.Module.Communication;

/// <summary>
/// 模块通信示例
/// 展示如何使用事件总线进行模块间通信
/// </summary>
public class ModuleCommunicationExample
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ModuleCommunicationExample> _logger;

    public ModuleCommunicationExample(IEventBus eventBus, ILogger<ModuleCommunicationExample> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 示例：模块注册通信
    /// </summary>
    /// <param name="moduleDescriptor">模块描述符</param>
    /// <returns>发布任务</returns>
    public async Task PublishModuleRegisteredAsync(ModuleDescriptor moduleDescriptor)
    {
        var moduleRegisteredEvent = new ModuleRegisteredEvent(moduleDescriptor);

        _logger.LogInformation("发布模块注册事件: {ModuleName} v{Version}",
            moduleDescriptor.Name, moduleDescriptor.Version);

        await _eventBus.PublishAsync(moduleRegisteredEvent);
    }

    /// <summary>
    /// 示例：模块状态变更通信
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="oldState">旧状态</param>
    /// <param name="newState">新状态</param>
    /// <param name="reason">变更原因</param>
    /// <returns>发布任务</returns>
    public async Task PublishModuleStateChangedAsync(
        string moduleId,
        string moduleName,
        ModuleState oldState,
        ModuleState newState,
        string? reason = null)
    {
        var stateChangedEvent = new ModuleStateChangedEvent(
            moduleId, moduleName, oldState, newState, reason);

        _logger.LogInformation("发布模块状态变更事件: {ModuleName} {OldState} -> {NewState}",
            moduleName, oldState.GetDisplayName(), newState.GetDisplayName());

        await _eventBus.PublishAsync(stateChangedEvent);
    }

    /// <summary>
    /// 示例：模块健康状态变更通信
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="oldStatus">旧健康状态</param>
    /// <param name="newHealthStatus">新健康状态</param>
    /// <returns>发布任务</returns>
    public async Task PublishModuleHealthChangedAsync(
        string moduleId,
        string moduleName,
        HealthStatus oldStatus,
        ModuleHealthStatus newHealthStatus)
    {
        var healthChangedEvent = new ModuleHealthChangedEvent(
            moduleId, moduleName, oldStatus, newHealthStatus);

        _logger.LogInformation("发布模块健康状态变更事件: {ModuleName} {OldStatus} -> {NewStatus}",
            moduleName, oldStatus.GetDisplayName(), newHealthStatus.Status.GetDisplayName());

        await _eventBus.PublishAsync(healthChangedEvent);
    }

    /// <summary>
    /// 示例：模块依赖事件通信
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="dependencyModuleId">依赖模块ID</param>
    /// <param name="dependencyModuleName">依赖模块名称</param>
    /// <param name="eventType">依赖事件类型</param>
    /// <param name="isOptional">是否为可选依赖</param>
    /// <returns>发布任务</returns>
    public async Task PublishModuleDependencyEventAsync(
        string moduleId,
        string moduleName,
        string dependencyModuleId,
        string dependencyModuleName,
        DependencyEventType eventType,
        bool isOptional = false)
    {
        var dependencyEvent = new ModuleDependencyEvent(
            moduleId, moduleName, eventType,
            dependencyModuleId, dependencyModuleName, isOptional);

        _logger.LogInformation("发布模块依赖事件: {ModuleName} -> {DependencyModule} ({EventType})",
            moduleName, dependencyModuleName, eventType);

        await _eventBus.PublishAsync(dependencyEvent);
    }
}

/// <summary>
/// 模块事件处理器示例
/// 展示如何处理模块相关事件
/// </summary>
public class ModuleEventHandlerExample :
    IIntegrationEventHandler<ModuleRegisteredEvent>,
    IIntegrationEventHandler<ModuleStateChangedEvent>,
    IIntegrationEventHandler<ModuleHealthChangedEvent>,
    IIntegrationEventHandler<ModuleDependencyEvent>
{
    private readonly ILogger<ModuleEventHandlerExample> _logger;

    public string HandlerName => "ModuleEventHandlerExample";
    public Type EventType => typeof(object); // 处理多种事件类型

    public ModuleEventHandlerExample(ILogger<ModuleEventHandlerExample> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理模块注册事件
    /// </summary>
    /// <param name="event">模块注册事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(ModuleRegisteredEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("处理模块注册事件: {Description}", @event.GetDescription());

        // 这里可以添加模块注册后的处理逻辑
        // 例如：更新模块注册表、通知其他系统组件等

        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理模块状态变更事件
    /// </summary>
    /// <param name="event">模块状态变更事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(ModuleStateChangedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("处理模块状态变更事件: {Description}", @event.GetDescription());

        // 根据状态变更类型执行相应操作
        if (@event.IsCriticalChange())
        {
            _logger.LogWarning("检测到关键模块状态变更: {Summary}", @event.GetChangeSummary());
            // 执行关键状态变更的处理逻辑
        }
        else if (@event.IsPositiveChange())
        {
            _logger.LogInformation("模块状态正向变更: {Summary}", @event.GetChangeSummary());
            // 执行正向变更的处理逻辑
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理模块健康状态变更事件
    /// </summary>
    /// <param name="event">模块健康状态变更事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(ModuleHealthChangedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("处理模块健康状态变更事件: {Description}", @event.GetDescription());

        // 根据健康状态变更严重程度执行相应操作
        var severity = @event.GetSeverity();
        switch (severity)
        {
            case HealthChangeSeverity.Critical:
                _logger.LogError("模块健康状态严重变更: {Summary}", @event.GetHealthSummary());
                // 执行严重健康问题的处理逻辑，如发送告警、尝试恢复等
                break;

            case HealthChangeSeverity.Warning:
                _logger.LogWarning("模块健康状态警告变更: {Summary}", @event.GetHealthSummary());
                // 执行警告级别的处理逻辑
                break;

            case HealthChangeSeverity.Info:
                _logger.LogInformation("模块健康状态信息变更: {Summary}", @event.GetHealthSummary());
                // 执行信息级别的处理逻辑
                break;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理模块依赖事件
    /// </summary>
    /// <param name="event">模块依赖事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(ModuleDependencyEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("处理模块依赖事件: {Description}", @event.GetDescription());

        // 根据依赖事件类型和严重程度执行相应操作
        if (@event.IsCritical())
        {
            _logger.LogError("检测到关键模块依赖问题: {Summary}", @event.GetDependencySummary());
            // 执行关键依赖问题的处理逻辑
        }

        var severity = @event.GetSeverity();
        switch (@event.EventType)
        {
            case DependencyEventType.DependencyUnavailable:
                _logger.LogWarning("模块依赖不可用: {ModuleName} -> {DependencyName}",
                    @event.ModuleName, @event.DependencyModuleName);
                // 处理依赖不可用的逻辑
                break;

            case DependencyEventType.CircularDependencyDetected:
                _logger.LogError("检测到循环依赖: {ModuleName} <-> {DependencyName}",
                    @event.ModuleName, @event.DependencyModuleName);
                // 处理循环依赖的逻辑
                break;

            case DependencyEventType.DependencyResolved:
                _logger.LogInformation("模块依赖已解析: {ModuleName} -> {DependencyName}",
                    @event.ModuleName, @event.DependencyModuleName);
                // 处理依赖解析成功的逻辑
                break;
        }

        await Task.CompletedTask;
    }
}
