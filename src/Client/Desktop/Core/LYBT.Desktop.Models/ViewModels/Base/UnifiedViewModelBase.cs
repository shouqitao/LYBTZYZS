using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 统一ViewModel基类 - UltraThink架构重构版本
    /// 提供统一的导航、错误处理、会话管理功能
    /// </summary>
    public abstract class UnifiedViewModelBase : ViewModelBase, INavigationAware, IRegionMemberLifetime
    {
        #region 依赖服务

        protected readonly IRegionManager RegionManager;
        protected readonly ISessionManager? SessionManager;
        protected readonly IUserNotificationService? UserNotificationService;

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

        #region 构造函数

        protected UnifiedViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory)
        {
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            SessionManager = sessionManager;
            UserNotificationService = userNotificationService;
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
                _ = Task.Run(async () => await OnNavigatedToAsync(navigationContext));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "页面导航处理失败");
                HandleError(ex, "页面加载");
            }
        }

        /// <summary>
        /// 处理导航参数
        /// </summary>
        protected virtual void ProcessNavigationParameters(NavigationParameters parameters)
        {
            // 子类可重写
        }

        /// <summary>
        /// 异步导航处理
        /// </summary>
        protected virtual async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            await Task.CompletedTask;
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
        /// </summary>
        /// <param name="message">消息内容</param>
        protected virtual async Task ShowSuccessMessageAsync(string message)
        {
            await Task.Run(() =>
            {
                RunOnUIThread(() =>
                {
                    System.Windows.MessageBox.Show(
                        message,
                        "成功",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                });
            });
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">消息内容</param>
        protected virtual async Task ShowErrorMessageAsync(string message)
        {
            await Task.Run(() =>
            {
                RunOnUIThread(() =>
                {
                    System.Windows.MessageBox.Show(
                        message,
                        "错误",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                });
            });
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">消息内容</param>
        protected virtual async Task ShowWarningMessageAsync(string message)
        {
            await Task.Run(() =>
            {
                RunOnUIThread(() =>
                {
                    System.Windows.MessageBox.Show(
                        message,
                        "警告",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                });
            });
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择结果</returns>
        protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            return await Task.Run(() =>
            {
                var result = false;
                RunOnUIThread(() =>
                {
                    result = System.Windows.MessageBox.Show(
                        message,
                        title,
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
                });
                return result;
            });
        }

        /// <summary>
        /// 显示错误消息（同步版本）
        /// </summary>
        protected void ShowErrorMessage(string message)
        {
            ShowErrorMessageAsync(message).Wait();
        }

        /// <summary>
        /// 显示信息消息（同步版本）
        /// </summary>
        protected void ShowInfoMessage(string message)
        {
            ShowSuccessMessageAsync(message).Wait();
        }

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

        #region 增强的错误处理

        protected override void HandleError(Exception ex, string? context = null)
        {
            Logger.LogError(ex, "错误发生在: {Context}", context ?? "未知操作");

            if (UserNotificationService != null)
            {
                var contextInfo = $"{context ?? "未知操作"} - 模块:{GetType().Name} - 用户:{SessionManager?.CurrentUser?.Id}";
                _ = Task.Run(async () => await UserNotificationService.HandleExceptionAsync(ex, contextInfo));
            }
            else
            {
                base.HandleError(ex, context);
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
