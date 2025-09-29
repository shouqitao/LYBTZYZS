using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Navigation;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// 导航感知的ViewModel基类
    /// 继承自UnifiedViewModelBase，添加导航历史管理功能
    /// </summary>
    public abstract class NavigationAwareViewModel : UnifiedViewModelBase
    {
        #region 字段

        private readonly IEnhancedNavigationService _navigationService;
        private bool _canGoBack;
        private bool _canGoForward;
        private string? _navigationTitle;
        private Prism.Regions.NavigationParameters? _navigationParameters;

        #endregion

        #region 属性

        /// <summary>
        /// 增强导航服务
        /// </summary>
        protected IEnhancedNavigationService NavigationService => _navigationService;

        /// <summary>
        /// 是否可以后退
        /// </summary>
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        /// <summary>
        /// 是否可以前进
        /// </summary>
        public bool CanGoForward
        {
            get => _canGoForward;
            set => SetProperty(ref _canGoForward, value);
        }

        /// <summary>
        /// 导航标题
        /// </summary>
        public string? NavigationTitle
        {
            get => _navigationTitle;
            set => SetProperty(ref _navigationTitle, value);
        }

        /// <summary>
        /// 当前导航参数
        /// </summary>
        protected Prism.Regions.NavigationParameters? NavigationParameters
        {
            get => _navigationParameters;
            private set => _navigationParameters = value;
        }

        #endregion

        #region 导航命令

        /// <summary>
        /// 后退命令
        /// </summary>
        public DelegateCommand GoBackCommand { get; private set; }

        /// <summary>
        /// 前进命令
        /// </summary>
        public DelegateCommand GoForwardCommand { get; private set; }

        /// <summary>
        /// 导航到主页命令
        /// </summary>
        public DelegateCommand NavigateHomeCommand { get; private set; }

        #endregion

        #region 构造函数

        protected NavigationAwareViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IEnhancedNavigationService navigationService,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            InitializeNavigationCommands();
            SubscribeToNavigationEvents();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化导航命令
        /// </summary>
        private void InitializeNavigationCommands()
        {
            GoBackCommand = new DelegateCommand(
                async () => await ExecuteGoBackAsync(),
                () => CanGoBack);

            GoForwardCommand = new DelegateCommand(
                async () => await ExecuteGoForwardAsync(),
                () => CanGoForward);

            NavigateHomeCommand = new DelegateCommand(
                async () => await ExecuteNavigateHomeAsync());
        }

        /// <summary>
        /// 订阅导航事件
        /// </summary>
        private void SubscribeToNavigationEvents()
        {
            _navigationService.Navigating += OnNavigating;
            _navigationService.Navigated += OnNavigated;
            _navigationService.NavigationFailed += OnNavigationFailed;
        }

        #endregion

        #region 导航事件处理

        /// <summary>
        /// 导航开始时的处理
        /// </summary>
        protected virtual void OnNavigating(object? sender, LYBT.Desktop.Core.Interfaces.Navigation.NavigatingEventArgs e)
        {
            IsNavigating = true;
            Logger.LogDebug("开始导航: {Region} -> {View}", e.RegionName, e.ViewName);
        }

        /// <summary>
        /// 导航完成时的处理
        /// </summary>
        protected virtual void OnNavigated(object? sender, LYBT.Desktop.Core.Interfaces.Navigation.NavigatedEventArgs e)
        {
            IsNavigating = false;
            UpdateNavigationState(e.RegionName);
            Logger.LogDebug("导航完成: {Region} -> {View}", e.RegionName, e.ViewName);
        }

        /// <summary>
        /// 导航失败时的处理
        /// </summary>
        protected virtual void OnNavigationFailed(object? sender, LYBT.Desktop.Core.Interfaces.Navigation.NavigationFailedEventArgs e)
        {
            IsNavigating = false;
            Logger.LogError(e.Error, "导航失败: {Region} -> {View}", e.RegionName, e.ViewName);

            var context = new ErrorContext
            {
                Operation = "导航",
                Module = GetType().Name
            };
            context.AdditionalData["TargetView"] = e.ViewName;
            _ = ErrorHandlingService?.HandleExceptionAsync(e.Error ?? new Exception(e.ErrorMessage), context);
        }

        #endregion

        #region 导航覆盖方法

        /// <summary>
        /// 导航到此页面时调用（增强版）
        /// </summary>
        public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)
        {
            NavigationParameters = navigationContext.Parameters;

            // 更新导航状态
            var regionName = GetCurrentRegionName();
            if (!string.IsNullOrEmpty(regionName))
            {
                UpdateNavigationState(regionName);
            }

            // 处理导航标题
            if (NavigationParameters?.TryGetValue("NavigationTitle", out object titleObj) == true)
            {
                NavigationTitle = titleObj?.ToString();
            }

            base.OnNavigatedTo(navigationContext);
        }

        /// <summary>
        /// 从此页面导航离开时调用（增强版）
        /// </summary>
        public override void OnNavigatedFrom(Prism.Regions.NavigationContext navigationContext)
        {
            // 保存视图状态
            SaveViewState();

            base.OnNavigatedFrom(navigationContext);
        }

        #endregion

        #region 导航命令实现

        /// <summary>
        /// 执行后退导航
        /// </summary>
        protected virtual async Task ExecuteGoBackAsync()
        {
            try
            {
                var regionName = GetCurrentRegionName();
                if (!string.IsNullOrEmpty(regionName))
                {
                    await _navigationService.GoBackAsync(regionName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "后退导航失败");
                var context = new ErrorContext { Operation = "后退导航", Module = GetType().Name };
                await ErrorHandlingService?.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 执行前进导航
        /// </summary>
        protected virtual async Task ExecuteGoForwardAsync()
        {
            try
            {
                var regionName = GetCurrentRegionName();
                if (!string.IsNullOrEmpty(regionName))
                {
                    await _navigationService.GoForwardAsync(regionName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "前进导航失败");
                var context = new ErrorContext { Operation = "前进导航", Module = GetType().Name };
                await ErrorHandlingService?.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 执行导航到主页
        /// </summary>
        protected virtual async Task ExecuteNavigateHomeAsync()
        {
            try
            {
                await NavigateToAsync(RegionNames.ContentRegion, "HomeView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到主页失败");
                var context = new ErrorContext { Operation = "导航到主页", Module = GetType().Name };
                await ErrorHandlingService?.HandleExceptionAsync(ex, context);
            }
        }

        #endregion

        #region 导航辅助方法

        /// <summary>
        /// 异步导航到指定页面
        /// </summary>
        protected async Task<bool> NavigateToAsync(string regionName, string viewName, Prism.Regions.NavigationParameters? parameters = null)
        {
            try
            {
                var result = await _navigationService.NavigateAsync(regionName, viewName, parameters);
                return result?.Success == true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航失败: {Region} -> {View}", regionName, viewName);
                return false;
            }
        }

        /// <summary>
        /// 更新导航状态
        /// </summary>
        protected virtual void UpdateNavigationState(string regionName)
        {
            CanGoBack = _navigationService.CanGoBack(regionName);
            CanGoForward = _navigationService.CanGoForward(regionName);

            GoBackCommand?.RaiseCanExecuteChanged();
            GoForwardCommand?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 获取当前区域名称
        /// </summary>
        protected virtual string GetCurrentRegionName()
        {
            // 默认返回主内容区域
            // 子类可以重写以返回特定区域
            return RegionNames.ContentRegion;
        }

        /// <summary>
        /// 保存视图状态
        /// </summary>
        protected virtual void SaveViewState()
        {
            // 子类可以重写以保存特定的视图状态
            Logger.LogDebug("保存视图状态: {ViewType}", GetType().Name);
        }

        /// <summary>
        /// 恢复视图状态
        /// </summary>
        protected virtual void RestoreViewState()
        {
            // 子类可以重写以恢复特定的视图状态
            Logger.LogDebug("恢复视图状态: {ViewType}", GetType().Name);
        }

        #endregion

        #region 清理

        protected override void OnDisposing()
        {
            // 取消订阅导航事件
            if (_navigationService != null)
            {
                _navigationService.Navigating -= OnNavigating;
                _navigationService.Navigated -= OnNavigated;
                _navigationService.NavigationFailed -= OnNavigationFailed;
            }

            base.OnDisposing();
        }

        #endregion
    }
}