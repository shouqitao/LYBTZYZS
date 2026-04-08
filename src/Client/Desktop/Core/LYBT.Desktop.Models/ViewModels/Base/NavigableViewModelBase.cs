using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 可导航ViewModel基类
    /// OpenSpec: enhance-viewmodel-architecture
    ///
    /// 继承CoreViewModelBase，添加:
    /// - Prism导航支持 (INavigationAware, IRegionMemberLifetime, IConfirmNavigationRequest)
    /// - 区域导航方法
    /// - 导航参数提取辅助
    /// - 未保存变更追踪
    /// </summary>
    public abstract partial class NavigableViewModelBase
        : CoreViewModelBase, INavigationAware, IRegionMemberLifetime, IConfirmNavigationRequest, IEditable
    {
        #region 服务

        /// <summary>
        /// 区域管理器
        /// </summary>
        protected IRegionManager RegionManager { get; }

        /// <summary>
        /// 会话管理器
        /// </summary>
        protected ISessionManager SessionManager { get; }

        /// <summary>
        /// 用户通知服务
        /// </summary>
        protected IUserNotificationService UserNotificationService { get; }

        /// <summary>
        /// 通用对话框服务
        /// </summary>
        protected ICommonDialogService CommonDialogService { get; }

        /// <summary>
        /// 角色注册表
        /// </summary>
        protected IRoleRegistry RoleRegistry { get; }

        #endregion

        #region 可观察属性

        /// <summary>
        /// 页面标题
        /// </summary>
        [ObservableProperty]
        private string _pageTitle = string.Empty;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoading))]
        private bool _isLoading;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        [ObservableProperty]
        private bool _isInitialized;

        /// <summary>
        /// 是否处于活动状态
        /// </summary>
        [ObservableProperty]
        private bool _isActive;

        /// <summary>
        /// 是否有未保存的变更
        /// </summary>
        private bool _hasUnsavedChanges;
        bool IEditable.HasUnsavedChanges => HasUnsavedChanges;
        protected virtual bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        /// <summary>
        /// 是否正在编辑
        /// </summary>
        [ObservableProperty]
        private bool _isEditing;

        #endregion

        #region 计算属性

        /// <summary>
        /// 是否未在加载
        /// </summary>
        public bool IsNotLoading => !IsLoading;

        /// <summary>
        /// 是否在导航离开时保持活动
        /// </summary>
        public virtual bool KeepAlive => true;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数 - 使用IViewModelServices聚合服务
        /// OpenSpec: enhance-viewmodel-architecture
        /// </summary>
        /// <param name="services">ViewModel服务聚合</param>
        protected NavigableViewModelBase(IViewModelServices services)
            : base(services)
        {
            RegionManager = services.RegionManager;
            SessionManager = services.SessionManager;
            UserNotificationService = services.UserNotificationService;
            CommonDialogService = services.CommonDialogService;
            RoleRegistry = services.RoleRegistry;
        }

        #endregion

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
                            SetError($"初始化失败: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "页面导航处理失败");
                SetError($"页面加载失败: {ex.Message}");
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

        #region 导航参数提取

        /// <summary>
        /// 获取必需的导航参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="context">导航上下文</param>
        /// <param name="key">参数键</param>
        /// <returns>参数值</returns>
        /// <exception cref="ArgumentException">参数不存在时抛出</exception>
        protected T GetNavigationParameter<T>(NavigationContext context, string key)
        {
            if (context.Parameters.TryGetValue(key, out T? value) && value != null)
            {
                return value;
            }

            throw new ArgumentException($"必需的导航参数 '{key}' 不存在或为null", key);
        }

        /// <summary>
        /// 获取可选的导航参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="context">导航上下文</param>
        /// <param name="key">参数键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值或默认值</returns>
        protected T GetNavigationParameter<T>(NavigationContext context, string key, T defaultValue)
        {
            if (context.Parameters.TryGetValue(key, out T? value) && value != null)
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// 尝试获取导航参数
        /// </summary>
        protected bool TryGetNavigationParameter<T>(NavigationContext context, string key, out T? value)
        {
            return context.Parameters.TryGetValue(key, out value);
        }

        #endregion

        #region 导航命令

        /// <summary>
        /// 返回主页命令
        /// </summary>
        [RelayCommand]
        protected virtual void NavigateToHome()
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
                SetError($"返回主页失败: {ex.Message}");
            }
        }

        #endregion

        #region 导航方法

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        protected virtual void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)
        {
            try
            {
                Logger.LogDebug("导航到视图: {ViewName} (区域: {RegionName})", viewName, regionName);
                RegionManager.RequestNavigate(regionName, viewName, parameters ?? new NavigationParameters());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航失败: {ViewName}", viewName);
                SetError($"导航失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导航返回
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
                SetError($"导航回退失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取主页视图名称
        /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices中的RoleRegistry
        /// </summary>
        protected virtual string GetHomeViewName()
        {
            var role = SessionManager.CurrentUser?.Role ?? UserRole.Admin;
            return RoleRegistry.GetHomeViewName(role);
        }

        #endregion

        #region 对话框方法

        /// <summary>
        /// 显示成功消息
        /// </summary>
        protected virtual async Task ShowSuccessMessageAsync(string message)
        {
            await CommonDialogService.ShowInfoAsync(message, "成功");
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        protected virtual async Task ShowErrorMessageAsync(string message)
        {
            await CommonDialogService.ShowErrorAsync(message, "错误");
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        protected virtual async Task ShowWarningMessageAsync(string message)
        {
            await CommonDialogService.ShowWarningAsync(message, "警告");
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        protected virtual async Task<bool> ShowConfirmMessageAsync(string message, string title = "确认")
        {
            return await CommonDialogService.ShowConfirmAsync(message, title);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        protected virtual string GetCurrentUserInfo()
        {
            return SessionManager.CurrentUser?.RealName ?? "未知用户";
        }

        /// <summary>
        /// 是否已登录
        /// </summary>
        protected virtual bool IsUserLoggedIn()
        {
            return SessionManager.IsAuthenticated;
        }

        /// <summary>
        /// 标记有未保存的变更
        /// </summary>
        protected void MarkAsChanged()
        {
            HasUnsavedChanges = true;
        }

        /// <summary>
        /// 标记变更已保存
        /// </summary>
        protected void MarkAsSaved()
        {
            HasUnsavedChanges = false;
        }

        #endregion

        #region IEditable Explicit Implementation

        void IEditable.MarkAsChanged() => MarkAsChanged();
        void IEditable.MarkAsSaved() => MarkAsSaved();

        #endregion

        #region IEditable Implementation

        /// <summary>
        /// 开始编辑
        /// </summary>
        public virtual void BeginEdit()
        {
            IsEditing = true;
            Logger.LogDebug("开始编辑: {ViewType}", GetType().Name);
            OnBeginEdit();
        }

        /// <summary>
        /// 取消编辑（恢复原始值）
        /// </summary>
        public virtual void CancelEdit()
        {
            IsEditing = false;
            HasUnsavedChanges = false;
            Logger.LogDebug("取消编辑: {ViewType}", GetType().Name);
            OnCancelEdit();
        }

        /// <summary>
        /// 确认编辑完成
        /// </summary>
        public virtual void EndEdit()
        {
            IsEditing = false;
            HasUnsavedChanges = false;
            Logger.LogDebug("结束编辑: {ViewType}", GetType().Name);
            OnEndEdit();
        }

        /// <summary>
        /// 开始编辑钩子（子类可重写）
        /// </summary>
        protected virtual void OnBeginEdit() { }

        /// <summary>
        /// 取消编辑钩子（子类可重写）
        /// </summary>
        protected virtual void OnCancelEdit() { }

        /// <summary>
        /// 结束编辑钩子（子类可重写）
        /// </summary>
        protected virtual void OnEndEdit() { }

        #endregion
    }
}
