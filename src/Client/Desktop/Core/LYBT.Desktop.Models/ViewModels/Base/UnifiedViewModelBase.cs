using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 统一ViewModel基类 - UltraThink架构重构版本
    /// 提供统一的导航、错误处理、会话管理功能
    /// Issue #1240: 添加自定义 InitializeAsync 支持，优化异步导航模式
    /// Issue #1831: 添加返回主页统一命令
    /// </summary>
    public abstract class UnifiedViewModelBase : ViewModelBase, INavigationAware, IRegionMemberLifetime
    {
        #region 依赖服务

        protected readonly IRegionManager RegionManager;
        protected readonly ISessionManager? SessionManager;
        protected readonly IUserNotificationService? UserNotificationService;
        /// <summary>
        /// 通用对话框服务（Issue #2247: 统一MessageBox调用）
        /// </summary>
        protected readonly ICommonDialogService? CommonDialogService;

        #endregion

        #region 页面属性

        private string _pageTitle = string.Empty;

        /// <summary>
        /// 页面标题
        /// </summary>
        public string PageTitle
        {
            get => _pageTitle;
            protected set => SetProperty(ref _pageTitle, value);
        }

        #endregion

        #region 导航命令 (Issue #1831)

        /// <summary>
        /// 返回主页命令
        /// </summary>
        public DelegateCommand NavigateToHomeCommand { get; private set; }

        #endregion

        #region 构造函数

        protected UnifiedViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null,
            ICommonDialogService? commonDialogService = null)
            : base(eventAggregator, loggerFactory)
        {
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            SessionManager = sessionManager;
            UserNotificationService = userNotificationService;
            CommonDialogService = commonDialogService;

            // Issue #1831: 初始化返回主页命令
            NavigateToHomeCommand = new DelegateCommand(ExecuteNavigateToHome);
        }

        #endregion

        #region 导航支持

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        protected virtual void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)
        {
            try
            {
                Logger.LogDebug("导航到视图: {ViewName} (区域: {RegionName})", viewName, regionName);

                parameters ??= new NavigationParameters();
                RegionManager.RequestNavigate(regionName, viewName, parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航失败: {ViewName}", viewName);
                HandleError(ex, "导航");
            }
        }

        /// <summary>
        /// 导航回退
        /// </summary>
        protected virtual void NavigateBack(string regionName)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                if (region?.NavigationService?.Journal?.CanGoBack == true)
                {
                    region.NavigationService.Journal.GoBack();
                    Logger.LogDebug("导航回退成功: {RegionName}", regionName);
                }
                else
                {
                    Logger.LogWarning("无法回退，导航历史为空: {RegionName}", regionName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航回退失败");
                HandleError(ex, "导航回退");
            }
        }

        /// <summary>
        /// 导航回退（带参数）
        /// Issue #2166: 支持在回退时传递参数给上一个页面
        /// </summary>
        protected virtual void NavigateBack(string regionName, NavigationParameters parameters)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                if (region?.NavigationService?.Journal?.CanGoBack == true)
                {
                    // 获取上一个导航条目
                    var journal = region.NavigationService.Journal;
                    var currentEntry = journal.CurrentEntry;

                    if (currentEntry != null && journal.CanGoBack)
                    {
                        // 回退一步
                        journal.GoBack();

                        // 获取回退后的当前页面ViewModel（使用反射访问DataContext）
                        var currentView = region.ActiveViews.FirstOrDefault();
                        if (currentView != null)
                        {
                            var dataContextProperty = currentView.GetType().GetProperty("DataContext");
                            var dataContext = dataContextProperty?.GetValue(currentView);

                            if (dataContext is INavigationAware navigationAware)
                            {
                                // 创建导航上下文并调用OnNavigatedTo
                                var navigationContext = new NavigationContext(
                                    region.NavigationService,
                                    new Uri(currentEntry.Uri.OriginalString, UriKind.Relative),
                                    parameters);

                                navigationAware.OnNavigatedTo(navigationContext);
                                Logger.LogDebug("导航回退成功并传递参数: {RegionName}", regionName);
                            }
                            else
                            {
                                Logger.LogDebug("导航回退成功: {RegionName}", regionName);
                            }
                        }
                    }
                }
                else
                {
                    Logger.LogWarning("无法回退，导航历史为空: {RegionName}", regionName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航回退失败");
                HandleError(ex, "导航回退");
            }
        }

        /// <summary>
        /// 导航前进
        /// </summary>
        protected virtual void NavigateForward(string regionName)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                if (region?.NavigationService?.Journal?.CanGoForward == true)
                {
                    region.NavigationService.Journal.GoForward();
                    Logger.LogDebug("导航前进成功: {RegionName}", regionName);
                }
                else
                {
                    Logger.LogWarning("无法前进，导航历史为空: {RegionName}", regionName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航前进失败");
                HandleError(ex, "导航前进");
            }
        }

        /// <summary>
        /// 检查指定区域是否可以回退
        /// </summary>
        protected virtual bool CanNavigateBack(string regionName)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                return region?.NavigationService?.Journal?.CanGoBack ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查指定区域是否可以前进
        /// </summary>
        protected virtual bool CanNavigateForward(string regionName)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                return region?.NavigationService?.Journal?.CanGoForward ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 返回主页 (Issue #1831)
        /// </summary>
        protected virtual void ExecuteNavigateToHome()
        {
            try
            {
                var homeViewName = GetHomeViewName();
                Logger.LogDebug("返回主页: {HomeViewName}", homeViewName);
                NavigateTo("ContentRegion", homeViewName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回主页失败");
                HandleError(ex, "返回主页");
            }
        }

        /// <summary>
        /// 获取主页视图名称（子类可重写以指定不同的主页）
        /// 默认返回 AdminHomeView (Issue #1831)
        /// </summary>
        protected virtual string GetHomeViewName()
        {
            return "AdminHomeView";
        }

        #endregion

        #region INavigationAware 实现

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Logger.LogDebug("离开页面: {PageTitle}", PageTitle);
        }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("进入页面: {PageTitle}", PageTitle);

            try
            {
                ProcessNavigationParameters(navigationContext.Parameters);

                // Issue #1240: 使用 Dispatcher.InvokeAsync 调用 InitializeAsync，避免 Task.Run
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await InitializeAsync(navigationContext.Parameters);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "InitializeAsync 执行失败");
                        HandleError(ex, "数据初始化");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "页面导航处理失败");
                HandleError(ex, "页面加载");
            }
        }

        /// <summary>
        /// 处理导航参数（同步）
        /// Issue #1240: 从 InitializeAsync 中分离出来，用于立即设置导航参数
        /// </summary>
        protected virtual void ProcessNavigationParameters(NavigationParameters parameters)
        {
            // 子类可重写，用于立即处理导航参数（如设置 PatientId）
        }

        #endregion

        #region 自定义异步初始化支持

        /// <summary>
        /// 自定义异步初始化（推荐使用，替代 OnNavigatedTo 中的异步逻辑）
        /// Issue #1240: 子类应该重写此方法进行数据加载，OnNavigatedTo 会自动调用
        /// </summary>
        /// <param name="parameters">导航参数</param>
        /// <returns>初始化任务</returns>
        protected virtual Task InitializeAsync(NavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region 增强的验证功能

        /// <summary>
        /// 验证属性
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void ValidateProperty([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                return;

            // 清除当前属性的验证错误
            ClearValidationErrors(propertyName);

            // 获取属性值
            var property = GetType().GetProperty(propertyName);
            if (property == null)
                return;

            var value = property.GetValue(this);

            // 执行DataAnnotations验证
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this) { MemberName = propertyName };

            if (!Validator.TryValidateProperty(value, validationContext, validationResults))
            {
                foreach (var validationResult in validationResults)
                {
                    AddValidationError(propertyName, validationResult.ErrorMessage ?? "验证失败");
                }
            }
        }

        /// <summary>
        /// 验证所有属性
        /// </summary>
        protected virtual void ValidateAllProperties()
        {
            var properties = GetType().GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(ValidationAttribute), true).Any());

            foreach (var property in properties)
            {
                ValidateProperty(property.Name);
            }
        }

        /// <summary>
        /// 清除所有验证错误
        /// </summary>
        protected void ClearAllErrors()
        {
            ClearValidationErrors();
        }

        /// <summary>
        /// 添加错误
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="errorMessage">错误消息</param>
        protected void AddError(string propertyName, string errorMessage)
        {
            AddValidationError(propertyName, errorMessage);
        }

        #endregion

        #region 消息显示功能

        /// <summary>
        /// 显示成功消息
        /// Issue #2247: 使用ICommonDialogService，移除MessageBox.Show fallback
        /// </summary>
        /// <param name="message">消息内容</param>
        protected virtual async Task ShowSuccessMessageAsync(string message)
        {
            if (CommonDialogService != null)
            {
                await CommonDialogService.ShowInfoAsync(message, "成功");
                return;
            }

            // Issue #2247: 无CommonDialogService时记录日志，不显示MessageBox
            Logger.LogWarning("CommonDialogService不可用，成功消息未显示: {Message}", message);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 显示错误消息
        /// Issue #2247: 使用ICommonDialogService，移除MessageBox.Show fallback
        /// </summary>
        /// <param name="message">消息内容</param>
        protected virtual async Task ShowErrorMessageAsync(string message)
        {
            if (CommonDialogService != null)
            {
                await CommonDialogService.ShowErrorAsync(message, "错误");
                return;
            }

            // Issue #2247: 无CommonDialogService时记录日志，不显示MessageBox
            Logger.LogError("CommonDialogService不可用，错误消息未显示: {Message}", message);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 显示警告消息
        /// Issue #2247: 使用ICommonDialogService，移除MessageBox.Show fallback
        /// </summary>
        /// <param name="message">消息内容</param>
        protected virtual async Task ShowWarningMessageAsync(string message)
        {
            if (CommonDialogService != null)
            {
                await CommonDialogService.ShowWarningAsync(message, "警告");
                return;
            }

            // Issue #2247: 无CommonDialogService时记录日志，不显示MessageBox
            Logger.LogWarning("CommonDialogService不可用，警告消息未显示: {Message}", message);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 显示确认对话框
        /// Issue #2247: 使用ICommonDialogService，移除MessageBox.Show fallback
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择结果</returns>
        protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            if (CommonDialogService != null)
            {
                return await CommonDialogService.ShowConfirmAsync(message, title);
            }

            // Issue #2247: 无CommonDialogService时记录日志并返回false（安全默认值）
            Logger.LogWarning("CommonDialogService不可用，确认对话框未显示: {Message}，默认返回false", message);
            return await Task.FromResult(false);
        }

        // Issue #2146: ShowErrorMessage和ShowInfoMessage同步方法已删除
        // 所有调用已替换为异步版本或fire-and-forget模式

        /// <summary>
        /// 显示确认消息（异步版本）
        /// </summary>
        protected async Task<bool> ShowConfirmMessageAsync(string message, string title = "确认")
        {
            return await ShowConfirmationAsync(message, title);
        }

        /// <summary>
        /// 显示确认消息（同步版本）
        /// </summary>
        protected bool ShowConfirmMessage(string message, string title = "确认")
        {
            return ShowConfirmMessageAsync(message, title).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 处理错误 - Issue #2247: 重写基类方法以使用ICommonDialogService
        /// 同时支持UserNotificationService进行错误上报
        /// </summary>
        protected override void HandleError(Exception ex, string? context = null)
        {
            Logger.LogError(ex, "错误发生在: {Context}", context ?? "未知操作");
            ErrorMessage = GetUserFriendlyMessage(ex);

            // 使用UserNotificationService上报错误（如果可用）
            if (UserNotificationService != null)
            {
                var contextInfo = $"{context ?? "未知操作"} - 模块:{GetType().Name} - 用户:{SessionManager?.CurrentUser?.Id}";
                _ = Task.Run(async () => await UserNotificationService.HandleExceptionAsync(ex, contextInfo));
            }

            // Issue #2247: 使用CommonDialogService显示错误对话框
            if (CommonDialogService != null)
            {
                _ = CommonDialogService.ShowErrorAsync(ErrorMessage, "错误");
            }
            else
            {
                // Issue #2247: 基类已不再使用MessageBox.Show，仅记录日志
                base.HandleError(ex, context);
            }
        }

        #endregion

        #region 状态管理增强

        /// <summary>
        /// 设置忙碌状态并显示消息
        /// </summary>
        /// <param name="isBusy">是否忙碌</param>
        /// <param name="message">状态消息</param>
        protected void SetIsBusy(bool isBusy, string? message = null)
        {
            IsBusy = isBusy;
            if (!string.IsNullOrEmpty(message))
            {
                StatusMessage = message;
            }
            else if (!isBusy)
            {
                StatusMessage = string.Empty;
            }
        }

        #endregion

        #region 会话管理

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        protected virtual string GetCurrentUserInfo()
        {
            return SessionManager?.CurrentUser?.RealName ?? "未知用户";
        }

        /// <summary>
        /// 检查是否已登录
        /// </summary>
        protected virtual bool IsUserLoggedIn()
        {
            return SessionManager?.IsAuthenticated ?? false;
        }

        #endregion

        #region IRegionMemberLifetime 实现

        /// <summary>
        /// 控制视图在导航离开后是否保持活动状态（缓存）
        /// 默认为 false，子类可重写以启用视图缓存
        /// </summary>
        public virtual bool KeepAlive => false;

        #endregion
    }
}
