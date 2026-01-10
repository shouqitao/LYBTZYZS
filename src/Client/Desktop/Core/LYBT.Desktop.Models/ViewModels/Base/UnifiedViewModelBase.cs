using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 统一ViewModel基类 - 提供导航、错误处理、会话管理功能
    /// OpenSpec: unify-navigation-architecture - 添加IConfirmNavigationRequest支持
    /// </summary>
    public abstract class UnifiedViewModelBase : ViewModelBase, INavigationAware, IRegionMemberLifetime, IConfirmNavigationRequest
    {
        protected readonly IRegionManager RegionManager;
        protected readonly ISessionManager? SessionManager;
        protected readonly IUserNotificationService? UserNotificationService;
        protected readonly ICommonDialogService? CommonDialogService;
        protected readonly IRoleRegistry? RoleRegistry;

        private string _pageTitle = string.Empty;
        public string PageTitle { get => _pageTitle; protected set => SetProperty(ref _pageTitle, value); }
        public DelegateCommand NavigateToHomeCommand { get; private set; }

        protected UnifiedViewModelBase(
            IEventAggregator eventAggregator, ILoggerFactory loggerFactory, IRegionManager regionManager,
            ISessionManager? sessionManager = null, IUserNotificationService? userNotificationService = null,
            ICommonDialogService? commonDialogService = null, IRoleRegistry? roleRegistry = null)
            : base(eventAggregator, loggerFactory)
        {
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            SessionManager = sessionManager;
            UserNotificationService = userNotificationService;
            CommonDialogService = commonDialogService;
            RoleRegistry = roleRegistry;
            NavigateToHomeCommand = new DelegateCommand(ExecuteNavigateToHome);
        }

        protected virtual void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)
        {
            try { Logger.LogDebug("导航到视图: {ViewName} (区域: {RegionName})", viewName, regionName); RegionManager.RequestNavigate(regionName, viewName, parameters ?? new NavigationParameters()); }
            catch (Exception ex) { Logger.LogError(ex, "导航失败: {ViewName}", viewName); HandleError(ex, "导航"); }
        }

        protected virtual void NavigateBack(string regionName)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                if (region?.NavigationService?.Journal?.CanGoBack == true) { region.NavigationService.Journal.GoBack(); Logger.LogDebug("导航回退成功: {RegionName}", regionName); }
                else Logger.LogWarning("无法回退，导航历史为空: {RegionName}", regionName);
            }
            catch (Exception ex) { Logger.LogError(ex, "导航回退失败"); HandleError(ex, "导航回退"); }
        }

        protected virtual void NavigateBack(string regionName, NavigationParameters parameters)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                if (region?.NavigationService?.Journal?.CanGoBack == true)
                {
                    var journal = region.NavigationService.Journal;
                    var currentEntry = journal.CurrentEntry;
                    if (currentEntry != null && journal.CanGoBack)
                    {
                        journal.GoBack();
                        var currentView = region.ActiveViews.FirstOrDefault();
                        if (currentView != null)
                        {
                            var dataContext = currentView.GetType().GetProperty("DataContext")?.GetValue(currentView);
                            if (dataContext is INavigationAware navigationAware)
                            {
                                var navigationContext = new NavigationContext(region.NavigationService, new Uri(currentEntry.Uri.OriginalString, UriKind.Relative), parameters);
                                navigationAware.OnNavigatedTo(navigationContext);
                            }
                        }
                        Logger.LogDebug("导航回退成功: {RegionName}", regionName);
                    }
                }
                else Logger.LogWarning("无法回退，导航历史为空: {RegionName}", regionName);
            }
            catch (Exception ex) { Logger.LogError(ex, "导航回退失败"); HandleError(ex, "导航回退"); }
        }

        protected virtual void NavigateForward(string regionName)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                if (region?.NavigationService?.Journal?.CanGoForward == true) { region.NavigationService.Journal.GoForward(); Logger.LogDebug("导航前进成功: {RegionName}", regionName); }
                else Logger.LogWarning("无法前进，导航历史为空: {RegionName}", regionName);
            }
            catch (Exception ex) { Logger.LogError(ex, "导航前进失败"); HandleError(ex, "导航前进"); }
        }

        protected virtual bool CanNavigateBack(string regionName) { try { return RegionManager.Regions[regionName]?.NavigationService?.Journal?.CanGoBack ?? false; } catch { return false; } }
        protected virtual bool CanNavigateForward(string regionName) { try { return RegionManager.Regions[regionName]?.NavigationService?.Journal?.CanGoForward ?? false; } catch { return false; } }

        protected virtual void ExecuteNavigateToHome()
        {
            try { var homeViewName = GetHomeViewName(); Logger.LogDebug("返回主页: {HomeViewName}", homeViewName); NavigateTo("ContentRegion", homeViewName); }
            catch (Exception ex) { Logger.LogError(ex, "返回主页失败"); HandleError(ex, "返回主页"); }
        }

        /// <summary>
        /// 获取主页视图名称
        /// OpenSpec: unify-navigation-architecture - 使用RoleRegistry作为权威源
        /// </summary>
        protected virtual string GetHomeViewName()
        {
            // 获取 SessionManager - 优先使用构造函数注入的，否则从容器获取
            var sessionManager = SessionManager;
            if (sessionManager == null)
            {
                try
                {
                    sessionManager = ContainerLocator.Container?.Resolve<ISessionManager>();
                    Logger.LogDebug("GetHomeViewName: SessionManager 为 null，从容器获取");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "GetHomeViewName: 无法从容器获取 SessionManager");
                }
            }

            var role = sessionManager?.CurrentUser?.Role ?? UserRole.Admin;
            Logger.LogDebug("GetHomeViewName: 当前用户角色 = {Role}", role);

            // 优先使用RoleRegistry
            var roleRegistry = RoleRegistry;
            if (roleRegistry == null)
            {
                try
                {
                    roleRegistry = ContainerLocator.Container?.Resolve<IRoleRegistry>();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "GetHomeViewName: 无法从容器获取 RoleRegistry");
                }
            }

            if (roleRegistry != null)
            {
                return roleRegistry.GetHomeViewName(role);
            }

            // OpenSpec: unify-navigation-architecture - 回退时使用ViewNames常量
            Logger.LogWarning("RoleRegistry不可用，使用默认主页映射");
            return role switch
            {
                UserRole.Admin or UserRole.SuperAdmin => ViewNames.AdminHome,
                UserRole.Doctor => ViewNames.ClinicalHome,
                _ => ViewNames.AdminHome
            };
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public virtual void OnNavigatedFrom(NavigationContext navigationContext) => Logger.LogDebug("离开页面: {PageTitle}", PageTitle);

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("进入页面: {PageTitle}", PageTitle);
            try
            {
                ProcessNavigationParameters(navigationContext.Parameters);
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try { await InitializeAsync(navigationContext.Parameters); }
                    catch (Exception ex) { Logger.LogError(ex, "InitializeAsync 执行失败"); HandleError(ex, "数据初始化"); }
                });
            }
            catch (Exception ex) { Logger.LogError(ex, "页面导航处理失败"); HandleError(ex, "页面加载"); }
        }

        protected virtual void ProcessNavigationParameters(NavigationParameters parameters) { }
        protected virtual Task InitializeAsync(NavigationParameters parameters) => Task.CompletedTask;

        #region IConfirmNavigationRequest

        /// <summary>
        /// 确认导航请求 - 子类可重写以实现未保存数据确认
        /// OpenSpec: unify-navigation-architecture - 统一导航确认行为
        /// </summary>
        public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            // 默认实现：检查是否可以离开
            if (CanNavigateAway())
            {
                continuationCallback(true);
                return;
            }

            // 有未保存更改时，显示确认对话框
            _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var canLeave = await ShowUnsavedChangesDialogAsync();
                continuationCallback(canLeave);
            });
        }

        /// <summary>
        /// 是否有未保存的更改 - 子类重写以提供状态
        /// </summary>
        protected virtual bool HasUnsavedChanges => false;

        /// <summary>
        /// 是否可以离开当前页面 - 子类可重写以添加自定义逻辑
        /// </summary>
        protected virtual bool CanNavigateAway() => !HasUnsavedChanges;

        /// <summary>
        /// 显示未保存更改确认对话框
        /// </summary>
        protected virtual async Task<bool> ShowUnsavedChangesDialogAsync()
        {
            return await ShowConfirmationAsync("有未保存的更改，是否放弃更改并离开？", "未保存的更改");
        }

        #endregion IConfirmNavigationRequest

        protected virtual void ValidateProperty([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) return;
            ClearValidationErrors(propertyName);
            var property = GetType().GetProperty(propertyName);
            if (property == null) return;
            var value = property.GetValue(this);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateProperty(value, new ValidationContext(this) { MemberName = propertyName }, validationResults))
                foreach (var result in validationResults) AddValidationError(propertyName, result.ErrorMessage ?? "验证失败");
        }

        protected virtual void ValidateAllProperties()
        {
            var properties = GetType().GetProperties().Where(p => p.GetCustomAttributes(typeof(ValidationAttribute), true).Any());
            foreach (var property in properties) ValidateProperty(property.Name);
        }

        protected void ClearAllErrors() => ClearValidationErrors();
        protected void AddError(string propertyName, string errorMessage) => AddValidationError(propertyName, errorMessage);

        protected virtual async Task ShowSuccessMessageAsync(string message)
        {
            if (CommonDialogService != null) { await CommonDialogService.ShowInfoAsync(message, "成功"); return; }
            Logger.LogWarning("CommonDialogService不可用，成功消息未显示: {Message}", message);
        }

        protected virtual async Task ShowErrorMessageAsync(string message)
        {
            if (CommonDialogService != null) { await CommonDialogService.ShowErrorAsync(message, "错误"); return; }
            Logger.LogError("CommonDialogService不可用，错误消息未显示: {Message}", message);
        }

        protected virtual async Task ShowWarningMessageAsync(string message)
        {
            if (CommonDialogService != null) { await CommonDialogService.ShowWarningAsync(message, "警告"); return; }
            Logger.LogWarning("CommonDialogService不可用，警告消息未显示: {Message}", message);
        }

        protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            if (CommonDialogService != null) return await CommonDialogService.ShowConfirmAsync(message, title);
            Logger.LogWarning("CommonDialogService不可用，确认对话框未显示: {Message}，默认返回false", message);
            return false;
        }

        protected async Task<bool> ShowConfirmMessageAsync(string message, string title = "确认") => await ShowConfirmationAsync(message, title);


        protected override void HandleError(Exception ex, string? context = null)
        {
            Logger.LogError(ex, "错误发生在: {Context}", context ?? "未知操作");
            ErrorMessage = GetUserFriendlyMessage(ex);
            if (UserNotificationService != null)
            {
                var contextInfo = $"{context ?? "未知操作"} - 模块:{GetType().Name} - 用户:{SessionManager?.CurrentUser?.Id}";
                _ = Task.Run(async () => await UserNotificationService.HandleExceptionAsync(ex, contextInfo));
            }
            if (CommonDialogService != null) _ = CommonDialogService.ShowErrorAsync(ErrorMessage, "错误");
            else base.HandleError(ex, context);
        }

        protected void SetIsBusy(bool isBusy, string? message = null) { IsBusy = isBusy; if (!string.IsNullOrEmpty(message)) StatusMessage = message; else if (!isBusy) StatusMessage = string.Empty; }
        protected virtual string GetCurrentUserInfo() => SessionManager?.CurrentUser?.RealName ?? "未知用户";
        protected virtual bool IsUserLoggedIn() => SessionManager?.IsAuthenticated ?? false;
        public virtual bool KeepAlive => false;
    }
}
