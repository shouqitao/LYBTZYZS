using System.Threading;
using System.Windows;
using LYBT.Desktop.Contracts.UI;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.Navigation;

/// <summary>
/// 导航协调器实现 - 统一导航入口
/// 职责：导航路由核心逻辑（~100行）
/// P2-13-3 评估：RequestNavigate 失败仅日志不抛异常，已评估静默失败可接受（RegionNames/ViewNames 注册遗漏时空白页，日志可定位），不引入异常冒泡。
/// 历史/面包屑/懒加载/Region监控 已提取到独立服务
/// </summary>
public class NavigationCoordinator : INavigationCoordinator
{
    private readonly INavigationServices _services;
    private readonly ILogger<NavigationCoordinator> _logger;
    private DateTime _lastNavigationTime = DateTime.MinValue;
    private string? _lastNavigationView;
    private const int NavigationDebounceMs = 300;
    private const int NavigationTimeoutSeconds = 10;

    /// <summary>
    /// D-4: 视图 → 允许角色映射（未列出的视图不限制角色）。
    /// 仅做客户端守卫；服务端仍有策略授权兜底。
    /// </summary>
    private static readonly IReadOnlyDictionary<string, UserRole[]> ViewRoleAccess = new Dictionary<string, UserRole[]>
    {
        [ViewNames.AdminHome] = [UserRole.Admin],
        [ViewNames.SysadminHome] = [UserRole.SuperAdmin],
        // B-07: 初始化向导——首次运行/系统管理重跑，运维操作仅 SuperAdmin
        [ViewNames.InitializationWizard] = [UserRole.SuperAdmin],
        [ViewNames.ClinicalHome] = [UserRole.Doctor],
        [ViewNames.ReceptionistHome] = [UserRole.Receptionist],
        [ViewNames.ClinicalWorkspace] = [UserRole.Doctor],
        [ViewNames.UserManagement] = [UserRole.Admin, UserRole.SuperAdmin],
        [ViewNames.LogLevelControl] = [UserRole.SuperAdmin],
        [ViewNames.Deployment] = [UserRole.SuperAdmin],
        // B-06: 备份/恢复为运维操作，服务端策略 SysAdminOnly——客户端守卫同步收紧为仅 SuperAdmin
        [ViewNames.BackupManagement] = [UserRole.SuperAdmin],
        [ViewNames.SecurityAuditLog] = [UserRole.Admin, UserRole.SuperAdmin],
        [ViewNames.SystemSettings] = [UserRole.Admin, UserRole.SuperAdmin],
        // N1: 接诊/管理查看需要；业务上前台 StartVisit 必达（Doctor+Receptionist+Admin+SuperAdmin）
        [ViewNames.MedicalCaseWorkspace] = [UserRole.Doctor, UserRole.Receptionist, UserRole.Admin, UserRole.SuperAdmin],
        [ViewNames.MedicalCaseMasterDetail] = [UserRole.Doctor, UserRole.Admin, UserRole.SuperAdmin],
        [ViewNames.PatientSelection] = [UserRole.Doctor, UserRole.Receptionist],
        [ViewNames.RegistrationList] = [UserRole.Doctor, UserRole.Receptionist],
        // R-17: 补全遗漏视图的角色映射
        [ViewNames.PatientManagement] = [UserRole.Doctor, UserRole.Receptionist, UserRole.Admin, UserRole.SuperAdmin],
        [ViewNames.MedicalCaseManagement] = [UserRole.Doctor, UserRole.Admin, UserRole.SuperAdmin],
        [ViewNames.HerbManagement] = [UserRole.Doctor, UserRole.Admin, UserRole.SuperAdmin],
        [ViewNames.FormulaManagement] = [UserRole.Doctor, UserRole.Admin, UserRole.SuperAdmin],
        [ViewNames.ReportsHome] = [UserRole.Doctor, UserRole.Admin, UserRole.SuperAdmin],
        [ViewNames.AuditLog] = [UserRole.Doctor, UserRole.Admin, UserRole.SuperAdmin],
    };

    public NavigationCoordinator(
        INavigationServices services,
        ILogger<NavigationCoordinator> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 导航路由核心

    /// <summary>当前视图名称</summary>
    public string? CurrentView
    {
        get
        {
            try
            {
                var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
                var activeView = region?.ActiveViews.FirstOrDefault();
                return activeView?.GetType().Name;
            }
            catch (Exception ex) { _logger.LogDebug(ex, "获取当前视图名称失败"); return null; }
        }
    }

    /// <summary>是否可以后退</summary>
    public bool CanNavigateBack
    {
        get
        {
            try
            {
                var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
                return region?.NavigationService?.Journal?.CanGoBack ?? false;
            }
            catch (Exception ex) { _logger.LogDebug(ex, "检查导航后退状态失败"); return false; }
        }
    }

    /// <summary>是否可以前进</summary>
    public bool CanNavigateForward
    {
        get
        {
            try
            {
                var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
                return region?.NavigationService?.Journal?.CanGoForward ?? false;
            }
            catch (Exception ex) { _logger.LogDebug(ex, "检查导航前进状态失败"); return false; }
        }
    }

    /// <summary>导航历史记录</summary>
    public IReadOnlyList<string> NavigationHistory => _services.HistoryService.NavigationHistory;

    /// <summary>当前面包屑列表</summary>
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs => _services.HistoryService.Breadcrumbs;

    /// <summary>导航变更事件</summary>
    public event EventHandler<NavigationChangedEventArgs>? NavigationChanged;

    /// <summary>导航到指定视图</summary>
    public async Task NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
    {
        try
        {
            // D-4: 角色守卫 — 导航前校验当前用户角色是否有权访问目标视图
            if (!IsViewAllowedForCurrentUser(viewName))
            {
                _logger.LogWarning("角色无权访问视图 {ViewName}，已拦截导航", viewName);
                _services.UserNotificationService?.ShowWarningAsync("当前角色无权访问该页面");
                return;
            }

            // 防抖：300ms 内不重复导航到同一视图
            if (viewName == _lastNavigationView &&
                viewName == CurrentView &&
                (DateTime.UtcNow - _lastNavigationTime).TotalMilliseconds < NavigationDebounceMs)
            {
                _logger.LogDebug("导航防抖：忽略重复请求 {ViewName}", viewName);
                return;
            }

            _lastNavigationTime = DateTime.UtcNow;
            _lastNavigationView = viewName;

            try
            {
                await _services.ModuleLazyLoader.EnsureModuleLoadedAsync(viewName);
            }
            catch (Exception loadEx)
            {
                // ModuleLazyLoader 已向用户展示错误；此处回退角色主页，避免空白页
                // 防重入：若目标已是主页则不再 NavigateToHome，避免加载失败时无限递归
                _logger.LogError(loadEx, "模块加载失败，回退主页: {ViewName}", viewName);
                var role = _services.SessionManager.CurrentUser?.Role;
                var homeViewName = role == null
                    ? ViewNames.ClinicalHome
                    : _services.RoleRegistry.GetHomeViewName(role.Value);
                if (!string.Equals(viewName, homeViewName, StringComparison.Ordinal))
                    await NavigateToHome();
                return;
            }
            var fromView = CurrentView;
            _logger.LogInformation("导航到 {ViewName}", viewName);
            var navParams = ConvertToNavigationParameters(parameters);

            var tcs = new TaskCompletionSource<bool>();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(NavigationTimeoutSeconds));
            var timeoutRegistration = timeoutCts.Token.Register(() => tcs.TrySetResult(false));

            _services.RegionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
            {
                tcs.TrySetResult(result.Result == true);

                if (result.Result == true)
                {
                    _services.HistoryService.RecordNavigation(fromView, viewName);
                    NavigationChanged?.Invoke(this, new NavigationChangedEventArgs(fromView, viewName, parameters));
                    _logger.LogDebug("导航成功: {FromView} -> {ToView}", fromView, viewName);
                }
                else
                {
                    var ex = result.Error;
                    _logger.LogError(ex, "导航失败：{ViewName}，异常完整信息：{ExFull}", viewName, ex?.ToString() ?? "未知错误");
                    var errorMessage = ex?.Message ?? "未知错误";
                    _services.UserNotificationService?.ShowErrorAsync($"无法打开页面：{errorMessage}");
                }
            }, navParams);

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!await tcs.Task)
                    {
                        _logger.LogWarning("导航超时: {ViewName} ({TimeoutSeconds}s)", viewName, NavigationTimeoutSeconds);
                        _services.UserNotificationService?.ShowWarningAsync($"页面加载超时：{viewName}");
                    }
                }
                finally
                {
                    timeoutCts.Dispose();
                    timeoutRegistration.Dispose();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到 {ViewName} 时发生异常", viewName);
            _services.UserNotificationService?.ShowErrorAsync("导航失败，请重试");
        }
    }

    /// <summary>导航到指定视图（强类型参数）</summary>
    public async Task NavigateTo<TParams>(string viewName, TParams parameters) where TParams : class
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var dict = new Dictionary<string, object>();
        foreach (var prop in typeof(TParams).GetProperties())
        {
            var value = prop.GetValue(parameters);
            if (value != null)
                dict[prop.Name] = value;
        }
        await NavigateTo(viewName, dict);
    }

    /// <summary>导航到当前角色主页</summary>
    public async Task NavigateToHome()
    {
        var role = _services.SessionManager.CurrentUser?.Role;
        var homeViewName = role == null
            ? ViewNames.ClinicalHome
            : _services.RoleRegistry.GetHomeViewName(role.Value);

        _logger.LogInformation("导航到主页: {HomeViewName}", homeViewName);
        await NavigateTo(homeViewName);
    }

    /// <summary>导航到指定角色主页</summary>
    public async Task NavigateToHome(UserRole role)
    {
        var homeViewName = _services.RoleRegistry.GetHomeViewName(role);
        _logger.LogInformation("导航到角色主页: Role={Role}, HomeView={HomeViewName}", role, homeViewName);
        await NavigateTo(homeViewName);
    }

    /// <summary>导航后退 — Journal 空时 fallback 到角色主页（设计 N4）</summary>
    public void NavigateBack()
    {
        try
        {
            var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
            if (region?.NavigationService?.Journal?.CanGoBack == true)
            {
                var fromView = CurrentView;
                region.NavigationService.Journal.GoBack();
                _logger.LogDebug("导航回退成功");
                // Journal.GoBack 不经过 NavigateTo，显式广播以便 Menu/Header 刷新 CanExecute
                NavigationChanged?.Invoke(this, new NavigationChangedEventArgs(fromView, CurrentView ?? "back"));
                return;
            }

            _logger.LogWarning("导航历史为空，fallback 返回角色主页");
            _ = _services.UserNotificationService?.ShowWarningAsync("已是最早的页面，已返回主页");
            _ = NavigateToHome();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航回退失败");
            _services.UserNotificationService?.ShowErrorAsync("导航回退失败，请重试");
        }
    }

    /// <summary>导航前进</summary>
    public void NavigateForward()
    {
        try
        {
            var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
            if (region?.NavigationService?.Journal?.CanGoForward == true)
            {
                var fromView = CurrentView;
                region.NavigationService.Journal.GoForward();
                _logger.LogDebug("导航前进成功");
                NavigationChanged?.Invoke(this, new NavigationChangedEventArgs(fromView, CurrentView ?? "forward"));
            }
            else
            {
                _logger.LogWarning("无法前进，无前进历史");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航前进失败");
            _services.UserNotificationService?.ShowErrorAsync("导航前进失败，请重试");
        }
    }

    /// <summary>清除导航历史</summary>
    public void ClearHistory() => _services.HistoryService.ClearHistory();

    #endregion

    #region Region 管理

    /// <summary>显示登录对话框</summary>
    public void ShowLoginDialog()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _services.RegionManager.RequestNavigate(RegionNames.LoginRegion, ViewNames.Login);
            _logger.LogDebug("显示登录对话框");
        });
    }

    /// <summary>清除登录区域</summary>
    public void ClearLoginRegion()
    {
        if (_services.RegionManager.Regions.ContainsRegionWithName(RegionNames.LoginRegion))
        {
            _services.RegionManager.Regions[RegionNames.LoginRegion].RemoveAll();
            _logger.LogDebug("登录区域已清除");
        }
    }

    /// <summary>清除内容区域</summary>
    public void ClearContentRegion()
    {
        if (_services.RegionManager.Regions.ContainsRegionWithName(RegionNames.ContentRegion))
        {
            _services.RegionManager.Regions[RegionNames.ContentRegion].RemoveAll();
            _logger.LogDebug("内容区域已清除");
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>D-4: 检查当前用户角色是否允许导航到目标视图</summary>
    private bool IsViewAllowedForCurrentUser(string viewName)
    {
        if (!ViewRoleAccess.TryGetValue(viewName, out var allowedRoles))
            return true; // 未列入映射的视图不限制

        // 登录页等匿名入口：无当前用户时放行
        var role = _services.SessionManager.CurrentUser?.Role;
        if (role == null)
            return true;

        var allowed = allowedRoles.Contains(role.Value);
        if (!allowed)
        {
            _logger.LogWarning(
                "导航守卫拒绝：视图 {ViewName} 不允许角色 {Role}（允许: {Allowed}）",
                viewName, role.Value, string.Join(",", allowedRoles));
        }
        return allowed;
    }

    private static NavigationParameters? ConvertToNavigationParameters(IDictionary<string, object>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return null;

        var navParams = new NavigationParameters();
        foreach (var kvp in parameters)
            navParams.Add(kvp.Key, kvp.Value);
        return navParams;
    }

    #endregion
}
