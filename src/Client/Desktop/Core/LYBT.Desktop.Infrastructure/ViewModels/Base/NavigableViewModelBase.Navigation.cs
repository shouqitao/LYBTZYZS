using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.ViewModels.Base
{
    /// <summary>
    /// 可导航ViewModel基类 - Prism导航支持部分
    ///
    /// 包含:
    /// - INavigationAware导航生命周期 (OnNavigatedTo/OnNavigatedFrom)
    /// - IConfirmNavigationRequest未保存变更确认
    /// - 导航命令与区域导航方法
    /// </summary>
    public abstract partial class NavigableViewModelBase
    {
        #region INavigationAware

        /// <inheritdoc/>
        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <inheritdoc/>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            IsActive = true;
            Logger.LogDebug("导航到: {ViewType}, 参数: {@Parameters}",
                GetType().Name,
                navigationContext.Parameters?.Keys);

            try
            {
                OnNavigatedToCore(navigationContext);

                // 首次导航时初始化
                if (!IsInitialized)
                {
                    _ = Services.UiThreadDispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            await InitializeAsync(navigationContext);
                            IsInitialized = true;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "InitializeAsync 执行失败");
                            SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage("初始化", ex));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "页面导航处理失败");
                SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage("页面加载", ex));
            }
        }

        /// <inheritdoc/>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            IsActive = false;
            Logger.LogDebug("离开页面: {PageTitle}", PageTitle);
            OnNavigatedFromCore(navigationContext);
        }

        #endregion

        #region IConfirmNavigationRequest

        /// <inheritdoc/>
        public virtual void ConfirmNavigationRequest(
            NavigationContext navigationContext,
            Action<bool> continuationCallback)
        {
            if (HasUnsavedChanges)
            {
                _ = ConfirmNavigationWithUnsavedChangesAsync(continuationCallback);
            }
            else
            {
                continuationCallback(CanNavigateAway());
            }
        }

        /// <summary>
        /// 显示未保存变更确认对话框
        /// </summary>
        private async Task ConfirmNavigationWithUnsavedChangesAsync(Action<bool> continuationCallback)
        {
            try
            {
                var result = await ShowUnsavedChangesDialogAsync();
                continuationCallback(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "显示未保存变更对话框失败");
                continuationCallback(true); // 默认允许导航
            }
        }

        /// <summary>
        /// 显示未保存变更对话框
        /// </summary>
        /// <returns>true表示继续导航，false表示取消</returns>
        protected virtual async Task<bool> ShowUnsavedChangesDialogAsync()
        {
            return await CommonDialogService.ShowConfirmAsync(
                "有未保存的更改，确定要离开吗？",
                "未保存的更改");
        }

        /// <summary>
        /// 检查是否可以导航离开（子类可重写）
        /// </summary>
        protected virtual bool CanNavigateAway() => true;

        #endregion

        #region 可重写钩子

        /// <summary>
        /// 导航到此页面时的核心处理（子类重写）
        /// </summary>
        protected virtual void OnNavigatedToCore(NavigationContext context) { }

        /// <summary>
        /// 导航离开此页面时的核心处理（子类重写）
        /// </summary>
        protected virtual void OnNavigatedFromCore(NavigationContext context) { }

        /// <summary>
        /// 首次导航时的初始化（子类重写）
        /// </summary>
        protected virtual Task InitializeAsync(NavigationContext context) => Task.CompletedTask;

        #endregion

        #region 导航命令

        /// <summary>
        /// 返回主页命令 — 委托 INavigationCoordinator（唯一导航门面，禁止直调 RegionManager）
        /// </summary>
        [RelayCommand]
        protected virtual void NavigateToHome()
        {
            try
            {
                var coordinator = Services.NavigationCoordinator;
                if (coordinator == null)
                {
                    Logger.LogWarning("INavigationCoordinator 未注入，无法返回主页");
                    return;
                }

                Logger.LogDebug("返回主页（经 INavigationCoordinator.NavigateToHome）");
                _ = coordinator.NavigateToHome();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回主页失败");
                SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage("返回主页", ex));
            }
        }

        #endregion

        #region 导航方法

        /// <summary>
        /// 导航到指定视图 — 委托 INavigationCoordinator（唯一导航门面；区域固定 ContentRegion 由协调器决定）
        /// </summary>
        protected virtual void NavigateTo(string viewName, NavigationParameters? parameters = null)
        {
            try
            {
                var coordinator = Services.NavigationCoordinator;
                if (coordinator == null)
                {
                    Logger.LogWarning("INavigationCoordinator 未注入，无法导航到 {ViewName}", viewName);
                    return;
                }

                Logger.LogDebug("导航到视图: {ViewName}（经 INavigationCoordinator）", viewName);
                IDictionary<string, object>? dict = null;
                if (parameters != null && parameters.Count > 0)
                {
                    dict = new Dictionary<string, object>();
                    foreach (var kvp in parameters)
                        dict[kvp.Key] = kvp.Value;
                }

                _ = coordinator.NavigateTo(viewName, dict);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航失败: {ViewName}", viewName);
                SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导航", ex));
            }
        }

        /// <summary>
        /// 获取主页视图名称
        /// </summary>
        protected virtual string GetHomeViewName()
        {
            var role = SessionManager.CurrentUser?.Role ?? UserRole.Admin;
            return RoleRegistry.GetHomeViewName(role);
        }

        #endregion
    }
}
