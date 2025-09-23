using Microsoft.Extensions.Logging;
using Prism.Regions;
using System.Collections.Concurrent;

namespace LYBT.Desktop.Core.Services.Navigation;

/// <summary>
/// 集中式导航服务实现
/// 统一管理所有导航操作，提供导航历史记录和状态管理
/// 重构：Prism 8.1.97架构优化，解决导航逻辑分散问题
/// </summary>
public class NavigationService : ITypedNavigationService
{
    private readonly IRegionManager _regionManager;
    private readonly ILogger<NavigationService> _logger;
    private readonly ConcurrentDictionary<string, Stack<string>> _navigationHistory;
    private string? _currentView;
    private readonly object _navigationLock = new();

    /// <summary>
    /// 默认内容区域名称
    /// </summary>
    private const string DefaultContentRegion = "ContentRegion";

    public NavigationService(
        IRegionManager regionManager,
        ILogger<NavigationService> logger)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationHistory = new ConcurrentDictionary<string, Stack<string>>();
    }

    /// <inheritdoc/>
    public string? CurrentView => _currentView;

    /// <inheritdoc/>
    public bool CanNavigateBack
    {
        get
        {
            lock (_navigationLock)
            {
                return _navigationHistory.TryGetValue(DefaultContentRegion, out var history)
                    && history.Count > 1;
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<NavigationEventArgs>? Navigated;

    /// <inheritdoc/>
    public event EventHandler<NavigationFailedEventArgs>? NavigationFailed;

    /// <inheritdoc/>
    public void NavigateTo(string viewName, NavigationParameters? parameters = null)
    {
        NavigateTo(DefaultContentRegion, viewName, parameters);
    }

    /// <inheritdoc/>
    public void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(regionName))
            throw new ArgumentException("Region name cannot be null or empty", nameof(regionName));
        if (string.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException("View name cannot be null or empty", nameof(viewName));

        try
        {
            lock (_navigationLock)
            {
                _logger.LogInformation("Navigating to {ViewName} in region {RegionName}", viewName, regionName);

                // 执行导航
                _regionManager.RequestNavigate(regionName, viewName, navigationResult =>
                {
                    if (navigationResult.Result == true)
                    {
                        // 更新导航历史
                        UpdateNavigationHistory(regionName, viewName);

                        // 更新当前视图
                        if (regionName == DefaultContentRegion)
                        {
                            _currentView = viewName;
                        }

                        _logger.LogInformation("Navigation to {ViewName} succeeded", viewName);
                        OnNavigated(new NavigationEventArgs(viewName, regionName, parameters, true));
                    }
                    else
                    {
                        var error = navigationResult.Error;
                        _logger.LogError(error, "Navigation to {ViewName} failed", viewName);
                        OnNavigationFailed(new NavigationFailedEventArgs(
                            viewName,
                            error ?? new Exception($"Navigation to {viewName} failed"),
                            regionName,
                            parameters));
                    }
                }, parameters);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during navigation to {ViewName} in region {RegionName}",
                viewName, regionName);
            OnNavigationFailed(new NavigationFailedEventArgs(viewName, ex, regionName, parameters));
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task NavigateToAsync(string viewName, NavigationParameters? parameters = null)
    {
        await NavigateToAsync(DefaultContentRegion, viewName, parameters);
    }

    /// <inheritdoc/>
    public async Task NavigateToAsync(string regionName, string viewName, NavigationParameters? parameters = null)
    {
        var tcs = new TaskCompletionSource<bool>();

        void NavigationHandler(NavigationResult result)
        {
            if (result.Result == true)
            {
                UpdateNavigationHistory(regionName, viewName);
                if (regionName == DefaultContentRegion)
                {
                    _currentView = viewName;
                }
                OnNavigated(new NavigationEventArgs(viewName, regionName, parameters, true));
                tcs.SetResult(true);
            }
            else
            {
                var error = result.Error ?? new Exception($"Navigation to {viewName} failed");
                OnNavigationFailed(new NavigationFailedEventArgs(viewName, error, regionName, parameters));
                tcs.SetException(error);
            }
        }

        try
        {
            _logger.LogInformation("Async navigating to {ViewName} in region {RegionName}", viewName, regionName);
            _regionManager.RequestNavigate(regionName, viewName, NavigationHandler, parameters);
            await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during async navigation to {ViewName}", viewName);
            throw;
        }
    }

    /// <inheritdoc/>
    public void NavigateBack()
    {
        NavigateBack(DefaultContentRegion);
    }

    /// <inheritdoc/>
    public void NavigateBack(string regionName)
    {
        lock (_navigationLock)
        {
            if (_navigationHistory.TryGetValue(regionName, out var history) && history.Count > 1)
            {
                // 弹出当前视图
                history.Pop();

                // 获取上一个视图
                var previousView = history.Peek();

                _logger.LogInformation("Navigating back to {ViewName} in region {RegionName}",
                    previousView, regionName);

                // 导航到上一个视图
                NavigateTo(regionName, previousView);
            }
            else
            {
                _logger.LogWarning("Cannot navigate back - no navigation history for region {RegionName}",
                    regionName);
            }
        }
    }

    /// <inheritdoc/>
    public string? GetCurrentView(string regionName)
    {
        if (_navigationHistory.TryGetValue(regionName, out var history) && history.Count > 0)
        {
            return history.Peek();
        }
        return null;
    }

    /// <summary>
    /// 更新导航历史
    /// </summary>
    private void UpdateNavigationHistory(string regionName, string viewName)
    {
        var history = _navigationHistory.GetOrAdd(regionName, _ => new Stack<string>());

        // 如果栈顶不是当前视图才添加
        if (history.Count == 0 || history.Peek() != viewName)
        {
            history.Push(viewName);

            // 限制历史记录数量，防止内存泄漏
            const int MaxHistorySize = 20;
            if (history.Count > MaxHistorySize)
            {
                var items = history.ToArray();
                history.Clear();
                foreach (var item in items.Take(MaxHistorySize).Reverse())
                {
                    history.Push(item);
                }
            }
        }
    }

    /// <summary>
    /// 触发导航完成事件
    /// </summary>
    protected virtual void OnNavigated(NavigationEventArgs e)
    {
        Navigated?.Invoke(this, e);
    }

    /// <summary>
    /// 触发导航失败事件
    /// </summary>
    protected virtual void OnNavigationFailed(NavigationFailedEventArgs e)
    {
        NavigationFailed?.Invoke(this, e);
    }

    #region 强类型导航方法实现

    /// <inheritdoc/>
    public void NavigateTo<TContext>(string viewName, TContext context) where TContext : NavigationRequest
    {
        NavigateTo<TContext>(DefaultContentRegion, viewName, context);
    }

    /// <inheritdoc/>
    public void NavigateTo<TContext>(string regionName, string viewName, TContext context) where TContext : NavigationRequest
    {
        // 将强类型上下文转换为NavigationParameters
        var parameters = ConvertToNavigationParameters(context);
        NavigateTo(regionName, viewName, parameters);
    }

    /// <inheritdoc/>
    public async Task NavigateToAsync<TContext>(string viewName, TContext context) where TContext : NavigationRequest
    {
        await NavigateToAsync<TContext>(DefaultContentRegion, viewName, context);
    }

    /// <inheritdoc/>
    public async Task NavigateToAsync<TContext>(string regionName, string viewName, TContext context) where TContext : NavigationRequest
    {
        var parameters = ConvertToNavigationParameters(context);
        await NavigateToAsync(regionName, viewName, parameters);
    }

    /// <inheritdoc/>
    public void NavigateToPatient(string viewName, Guid patientId, NavigationAction action = NavigationAction.View)
    {
        var context = new PatientNavigationRequest 
        { 
            PatientId = patientId,
            Action = action
        };
        NavigateTo(viewName, context);
    }

    /// <inheritdoc/>
    public void NavigateToMedical(string viewName, Guid patientId, Guid? medicalCaseId = null, NavigationAction action = NavigationAction.View)
    {
        var context = new MedicalNavigationRequest
        {
            PatientId = patientId,
            MedicalCaseId = medicalCaseId,
            Action = action
        };
        NavigateTo(viewName, context);
    }

    /// <inheritdoc/>
    public void NavigateToManagement(string viewName, string entityType, Guid? entityId = null, NavigationAction action = NavigationAction.View)
    {
        var context = new ManagementNavigationRequest
        {
            EntityType = entityType,
            EntityId = entityId ?? Guid.Empty,
            Action = action
        };
        NavigateTo(viewName, context);
    }

    /// <summary>
    /// 将强类型上下文转换为NavigationParameters
    /// </summary>
    private Prism.Regions.NavigationParameters ConvertToNavigationParameters(NavigationRequest context)
    {
        var parameters = new Prism.Regions.NavigationParameters();

        // 添加通用属性
        parameters.Add("Action", context.Action.ToString());
        parameters.Add("IsWorkflowMode", context.IsWorkflowMode);
        
        if (!string.IsNullOrEmpty(context.SourceView))
        {
            parameters.Add("SourceView", context.SourceView);
        }

        // 根据具体类型添加特定属性
        switch (context)
        {
            case PatientNavigationRequest patientContext:
                parameters.Add("PatientId", patientContext.PatientId);
                if (!string.IsNullOrEmpty(patientContext.PatientName))
                {
                    parameters.Add("PatientName", patientContext.PatientName);
                }
                break;

            case MedicalNavigationRequest medicalContext:
                parameters.Add("PatientId", medicalContext.PatientId);
                if (medicalContext.MedicalCaseId.HasValue)
                {
                    parameters.Add("MedicalCaseId", medicalContext.MedicalCaseId.Value);
                }
                if (medicalContext.ConsultationId.HasValue)
                {
                    parameters.Add("ConsultationId", medicalContext.ConsultationId.Value);
                }
                if (medicalContext.PrescriptionId.HasValue)
                {
                    parameters.Add("PrescriptionId", medicalContext.PrescriptionId.Value);
                }
                break;

            case WorkflowNavigationRequest workflowContext:
                parameters.Add("CurrentStep", workflowContext.CurrentStep);
                if (!string.IsNullOrEmpty(workflowContext.TargetStep))
                {
                    parameters.Add("TargetStep", workflowContext.TargetStep);
                }
                if (workflowContext.WorkflowData != null)
                {
                    parameters.Add("WorkflowData", workflowContext.WorkflowData);
                }
                break;

            case ManagementNavigationRequest managementContext:
                parameters.Add("EntityId", managementContext.EntityId);
                parameters.Add("EntityType", managementContext.EntityType);
                if (managementContext.Filter != null)
                {
                    parameters.Add("Filter", managementContext.Filter);
                }
                break;
        }

        return parameters;
    }

    #endregion
}