using System.Reactive.Disposables;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Services.Toast;
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
    /// - Prism导航支持 (INavigationAware, IRegionMemberLifetime, IConfirmNavigationRequest)
    /// - 区域导航方法、导航参数提取辅助
    /// - 未保存变更追踪
    /// - IDisposable实现
    ///
    /// 该类为partial类，按职责拆分为:
    /// - NavigableViewModelBase.cs: 服务聚合、状态管理与核心字段
    /// - NavigableViewModelBase.Navigation.cs: Prism导航支持
    /// - NavigableViewModelBase.Editable.cs: 对话框、编辑状态与资源释放
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

        /// <summary>
        /// IsLoading属性变更时调用（源生成器回调）
        /// </summary>
        partial void OnIsLoadingChanged(bool value)
        {
            OnIsLoadingChangedCore(value);
        }

        /// <summary>
        /// 派生类可重写以响应IsLoading变更
        /// </summary>
        protected virtual void OnIsLoadingChangedCore(bool value) { }

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
    }
}
