using System.Reactive.Disposables;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Services.Toast;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.ViewModels.Base
{
    /// <summary>
    /// 可导航ViewModel基类（合并原CoreViewModelBase）
    ///
    /// 提供:
    /// - 服务聚合 (IViewModelServices)
    /// - 日志、事件聚合器、UI线程调度器
    /// - 状态管理 (IsBusy, StatusMessage, ErrorMessage)
    /// - 异步执行包装 (ExecuteWithErrorHandlingAsync)
    /// - Prism导航支持 (INavigationAware, IRegionMemberLifetime, IConfirmNavigationRequest)
    /// - 区域导航方法、导航参数提取辅助
    /// - 未保存变更追踪
    /// - IDisposable实现
    /// </summary>
    public abstract partial class NavigableViewModelBase
        : ObservableObject, IDisposable, INavigationAware, IRegionMemberLifetime, IConfirmNavigationRequest, IEditable
    {
        #region 私有字段

        private readonly CompositeDisposable _disposables = new();
        private EventSubscriptionManager? _eventManager;
        private bool _disposed;

        #endregion

        #region 受保护属性

        /// <summary>
        /// ViewModel服务聚合
        /// </summary>
        protected IViewModelServices Services { get; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// 日志记录器工厂
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Prism事件聚合器
        /// </summary>
        protected IEventAggregator EventAggregator { get; }

        /// <summary>
        /// 事件订阅管理器 (延迟初始化)
        /// 使用此属性订阅事件，Dispose时自动清理
        /// </summary>
        protected EventSubscriptionManager Events => _eventManager ??= new EventSubscriptionManager(EventAggregator);

        /// <summary>
        /// UI线程调度器
        /// </summary>
        protected IUiThreadDispatcher UiDispatcher => Services.UiThreadDispatcher;

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
        /// Toast消息服务 (Phase 2.2: 替代MessageBox通知)
        /// </summary>
        protected IToastService ToastService { get; }

        /// <summary>
        /// 角色注册表
        /// </summary>
        protected IRoleRegistry RoleRegistry { get; }

        #endregion

        #region 可观察属性

        /// <summary>
        /// 是否正在执行操作
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage = string.Empty;

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
        /// 是否未在忙碌状态
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// 是否未在加载
        /// </summary>
        public bool IsNotLoading => !IsLoading;

        /// <summary>
        /// 是否在导航离开时保持活动
        /// </summary>
        public virtual bool KeepAlive => false;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数 - 使用IViewModelServices聚合服务
        /// Phase 2.2: 添加IToastService依赖以替代MessageBox通知
        /// </summary>
        /// <param name="services">ViewModel服务聚合</param>
        protected NavigableViewModelBase(IViewModelServices services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            LoggerFactory = services.LoggerFactory;
            EventAggregator = services.EventAggregator;
            Logger = services.LoggerFactory.CreateLogger(GetType());

            RegionManager = services.RegionManager;
            SessionManager = services.SessionManager;
            UserNotificationService = services.UserNotificationService;
            CommonDialogService = services.CommonDialogService;
            ToastService = services.ToastService;
            RoleRegistry = services.RoleRegistry;
        }

        #endregion

        #region 属性变更回调

        /// <summary>
        /// IsBusy属性变更时调用（源生成器回调）
        /// </summary>
        partial void OnIsBusyChanged(bool value)
        {
            OnIsBusyChangedCore(value);
        }

        /// <summary>
        /// 派生类可重写以响应IsBusy变更
        /// </summary>
        protected virtual void OnIsBusyChangedCore(bool value) { }

        #endregion

        #region 状态管理

        /// <summary>
        /// 设置忙碌状态
        /// </summary>
        /// <param name="isBusy">是否忙碌</param>
        /// <param name="message">状态消息</param>
        protected void SetBusy(bool isBusy, string? message = null)
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

        /// <summary>
        /// 清除错误状态
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// 设置错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        protected void SetError(string message)
        {
            ErrorMessage = message;
            Logger.LogWarning("设置错误消息: {Message}", message);
        }

        #endregion

        #region 异步执行包装

        /// <summary>
        /// 异步执行包装 - 统一异常处理
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称（用于日志和错误消息）</param>
        /// <param name="showBusy">是否显示忙碌状态</param>
        /// <param name="showErrorToUser">是否向用户显示错误消息</param>
        protected async Task ExecuteWithErrorHandlingAsync(
            Func<Task> action,
            string operationName,
            bool showBusy = true,
            bool showErrorToUser = true)
        {
            try
            {
                if (showBusy) SetBusy(true, $"正在{operationName}...");
                ClearError();
                await action();
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("{Operation} 已取消", operationName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Operation} 失败", operationName);
                if (showErrorToUser)
                {
                    SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage(operationName, ex));
                }
            }
            finally
            {
                if (showBusy) SetBusy(false);
            }
        }

        /// <summary>
        /// 异步执行包装 (带返回值)
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="defaultValue">失败时的默认值</param>
        /// <param name="showBusy">是否显示忙碌状态</param>
        /// <param name="showErrorToUser">是否向用户显示错误消息</param>
        /// <returns>操作结果或默认值</returns>
        protected async Task<T?> ExecuteWithErrorHandlingAsync<T>(
            Func<Task<T>> action,
            string operationName,
            T? defaultValue = default,
            bool showBusy = true,
            bool showErrorToUser = true)
        {
            try
            {
                if (showBusy) SetBusy(true, $"正在{operationName}...");
                ClearError();
                return await action();
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("{Operation} 已取消", operationName);
                return defaultValue;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Operation} 失败", operationName);
                if (showErrorToUser)
                {
                    SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage(operationName, ex));
                }
                return defaultValue;
            }
            finally
            {
                if (showBusy) SetBusy(false);
            }
        }

        #endregion

        #region UI线程操作

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            UiDispatcher.Invoke(action);
        }

        /// <summary>
        /// 在UI线程上异步执行操作
        /// </summary>
        protected Task RunOnUIThreadAsync(Func<Task> action)
        {
            return UiDispatcher.InvokeAsync(action);
        }

        #endregion

        #region Disposable管理

        /// <summary>
        /// 添加可释放对象到管理集合
        /// </summary>
        protected void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
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
                SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage("返回主页", ex));
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

        #region 对话框方法

        /// <summary>
        /// 显示成功消息
        /// Phase 2.2: 使用ToastService替代CommonDialogService（非阻塞通知）
        /// </summary>
        protected virtual Task ShowSuccessMessageAsync(string message)
        {
            ToastService.ShowSuccess(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示错误消息
        /// Phase 2.2: 使用ToastService替代CommonDialogService（非阻塞通知）
        /// </summary>
        protected virtual Task ShowErrorMessageAsync(string message)
        {
            ToastService.ShowError(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示警告消息
        /// Phase 2.2: 使用ToastService替代CommonDialogService（非阻塞通知）
        /// </summary>
        protected virtual Task ShowWarningMessageAsync(string message)
        {
            ToastService.ShowWarning(message);
            return Task.CompletedTask;
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

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _eventManager?.Dispose();
                _disposables.Dispose();
                OnDisposing();
            }

            _disposed = true;
        }

        /// <summary>
        /// 子类可重写以执行清理逻辑
        /// </summary>
        protected virtual void OnDisposing() { }

        #endregion
    }
}
