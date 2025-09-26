using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Core.ViewModels.Base.Refactored
{
    /// <summary>
    /// 简化的页面ViewModel基类 - Phase 1架构重构
    /// 整合导航功能，简化接口设计，减少依赖复杂度
    /// 为支持导航的页面提供统一基础功能
    /// </summary>
    public abstract class PageViewModel : ViewModelBase, INavigationAware, IConfirmNavigationRequest
    {
        #region 依赖服务
        
        protected readonly IRegionManager RegionManager;
        protected readonly ISessionManager? SessionManager;
        
        #endregion

        #region 页面属性
        
        private string _pageTitle = string.Empty;
        private bool _isNavigating;
        
        /// <summary>
        /// 页面标题
        /// </summary>
        public string PageTitle
        {
            get => _pageTitle;
            protected set => SetProperty(ref _pageTitle, value);
        }
        
        /// <summary>
        /// 是否正在导航
        /// </summary>
        public bool IsNavigating
        {
            get => _isNavigating;
            private set => SetProperty(ref _isNavigating, value);
        }
        
        #endregion

        #region 导航属性
        
        protected IRegionNavigationService? NavigationService { get; private set; }
        protected IRegionNavigationJournal? NavigationJournal { get; private set; }
        
        /// <summary>
        /// 是否保持存活（默认为false）
        /// </summary>
        public virtual bool KeepAlive => false;
        
        #endregion

        #region 导航命令
        
        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; private set; }
        
        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand? GoBackCommand { get; private set; }
        
        #endregion

        #region 构造函数
        
        protected PageViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            SessionManager = sessionManager;
            
            // 初始化命令
            RefreshCommand = new DelegateCommand(async () => await ExecuteRefreshAsync(), CanExecuteRefresh);
        }
        
        #endregion

        #region INavigationAware实现
        
        /// <summary>
        /// 导航到此页面时调用
        /// </summary>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("导航到页面: {PageType}", GetType().Name);
            
            NavigationService = navigationContext.NavigationService;
            NavigationJournal = navigationContext.NavigationService.Journal;
            
            // 初始化返回命令（如果有导航历史）
            if (NavigationJournal != null)
            {
                GoBackCommand ??= new DelegateCommand(ExecuteGoBack, () => NavigationJournal.CanGoBack);
            }
            
            // 处理导航参数
            ProcessNavigationParameters(navigationContext.Parameters);
            
            // 异步初始化数据
            _ = Task.Run(async () =>
            {
                try
            {
                    IsNavigating = true;
                    await OnNavigatedToAsync(navigationContext);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "页面初始化失败: {PageType}", GetType().Name);
                    await HandleErrorAsync(ex, "页面初始化");
                }
                finally
                {
                    IsNavigating = false;
                    RefreshCanExecuteChanged();
                }
            });
        }
        
        /// <summary>
        /// 从此页面导航离开时调用
        /// </summary>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Logger.LogDebug("从页面导航离开: {PageType}", GetType().Name);
            _ = OnNavigatedFromAsync(navigationContext);
        }
        
        /// <summary>
        /// 判断是否为导航目标
        /// </summary>
        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return KeepAlive;
        }
        
        #endregion

        #region IConfirmNavigationRequest实现
        
        /// <summary>
        /// 确认导航请求
        /// </summary>
        public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            var canNavigate = !HasUnsavedChanges();
            
            if (!canNavigate)
            {
                Logger.LogDebug("导航被阻止：存在未保存的更改");
            }
            
            continuationCallback(canNavigate);
        }
        
        #endregion

        #region 导航生命周期（异步版本）
        
        /// <summary>
        /// 导航到页面时的异步处理
        /// </summary>
        protected virtual async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            await InitializeDataAsync();
        }
        
        /// <summary>
        /// 导航离开页面时的异步处理
        /// </summary>
        protected virtual Task OnNavigatedFromAsync(NavigationContext navigationContext)
        {
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 处理导航参数
        /// </summary>
        protected virtual void ProcessNavigationParameters(Prism.Regions.NavigationParameters parameters)
        {
            // 处理页面标题
            if (parameters.TryGetValue("title", out object? titleObj) && titleObj is string title)
            {
                PageTitle = title;
            }
        }
        
        #endregion

        #region 数据初始化
        
        /// <summary>
        /// 初始化页面数据
        /// </summary>
        protected virtual async Task InitializeDataAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                await OnInitializeDataAsync();
            }, "初始化数据");
        }
        
        /// <summary>
        /// 子类重写以实现具体的数据初始化逻辑
        /// </summary>
        protected virtual Task OnInitializeDataAsync()
        {
            return Task.CompletedTask;
        }
        
        #endregion

        #region 命令实现
        
        /// <summary>
        /// 执行刷新
        /// </summary>
        protected virtual async Task ExecuteRefreshAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                await InitializeDataAsync();
            }, "刷新数据");
        }
        
        /// <summary>
        /// 是否可以刷新
        /// </summary>
        protected virtual bool CanExecuteRefresh()
        {
            return !IsLoading && !IsNavigating;
        }
        
        /// <summary>
        /// 执行后退导航
        /// </summary>
        private void ExecuteGoBack()
        {
            if (NavigationJournal?.CanGoBack == true)
            {
                NavigationJournal.GoBack();
            }
        }
        
        #endregion

        #region 导航辅助方法
        
        /// <summary>
        /// 导航到指定页面
        /// </summary>
        protected void NavigateTo(string regionName, string viewName, Prism.Regions.NavigationParameters? parameters = null)
        {
            try
            {
                RegionManager.RequestNavigate(regionName, new Uri(viewName, UriKind.RelativeOrAbsolute), parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航失败: {RegionName} -> {ViewName}", regionName, viewName);
                _ = HandleErrorAsync(ex, "导航");
            }
        }
        
        /// <summary>
        /// 导航到指定页面（异步）
        /// </summary>
        protected Task<NavigationResult> NavigateToAsync(string regionName, string viewName, Prism.Regions.NavigationParameters? parameters = null)
        {
            var tcs = new TaskCompletionSource<NavigationResult>();
            
            try
            {
                RegionManager.RequestNavigate(regionName, new Uri(viewName, UriKind.RelativeOrAbsolute), result =>
                {
                    tcs.SetResult(result);
                }, parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "异步导航失败: {RegionName} -> {ViewName}", regionName, viewName);
                tcs.SetException(ex);
            }
            
            return tcs.Task;
        }
        
        /// <summary>
        /// 检查是否有未保存的更改
        /// </summary>
        protected virtual bool HasUnsavedChanges()
        {
            return false;
        }
        
        #endregion

        #region 会话支持
        
        /// <summary>
        /// 获取当前用户
        /// </summary>
        protected LYBT.Shared.Models.Contracts.Users.UserDto? GetCurrentUser()
        {
            return SessionManager?.CurrentUser;
        }
        
        /// <summary>
        /// 获取当前患者
        /// </summary>
        protected LYBT.Shared.Models.Contracts.Patients.PatientDto? GetCurrentPatient()
        {
            return SessionManager?.CurrentPatient;
        }
        
        /// <summary>
        /// 获取当前诊疗记录
        /// </summary>
        protected LYBT.Shared.Models.Contracts.Consultation.ConsultationDto? GetCurrentConsultation()
        {
            return SessionManager?.ActiveConsultation;
        }
        
        /// <summary>
        /// 检查是否已登录
        /// </summary>
        protected bool IsAuthenticated()
        {
            return SessionManager?.IsLoggedIn ?? false;
        }
        
        #endregion

        #region 命令刷新
        
        /// <summary>
        /// 刷新命令可执行状态
        /// </summary>
        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();
            RefreshCommand?.RaiseCanExecuteChanged();
            GoBackCommand?.RaiseCanExecuteChanged();
        }
        
        #endregion

        #region 清理
        
        protected override void OnDisposing()
        {
            NavigationService = null;
            NavigationJournal = null;
            base.OnDisposing();
        }
        
        #endregion
    }
}