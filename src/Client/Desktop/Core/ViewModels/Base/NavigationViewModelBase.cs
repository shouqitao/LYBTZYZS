using System;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services.Navigation;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// 导航页面ViewModel基类 - 第2阶段架构重构
    /// 为所有支持导航的页面提供统一的基础功能
    /// 整合了NavigationViewModelBase和SessionAwareViewModel的功能
    /// </summary>
    public abstract class NavigationViewModelBase : ModernViewModelBase, INavigationAware, IRegionMemberLifetime, IConfirmNavigationRequest
    {
        #region 依赖服务
        
        protected readonly IRegionManager RegionManager;
        protected readonly ISessionManager? SessionManager;
        protected readonly ILogger<NavigationViewModelBase> Logger;
        protected IRegionNavigationService? NavigationService;
        
        #endregion

        #region 导航属性
        
        private string _pageTitle = string.Empty;
        private bool _isNavigating;
        private IRegionNavigationJournal? _navigationJournal;
        
        /// <summary>
        /// 页面标题
        /// </summary>
        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }
        
        /// <summary>
        /// 是否正在导航
        /// </summary>
        public bool IsNavigating
        {
            get => _isNavigating;
            private set => SetProperty(ref _isNavigating, value);
        }
        
        /// <summary>
        /// 导航日志
        /// </summary>
        protected IRegionNavigationJournal? NavigationJournal
        {
            get => _navigationJournal;
            private set => _navigationJournal = value;
        }
        
        #endregion

        #region IRegionMemberLifetime实现
        
        /// <summary>
        /// 是否保持存活（默认为false，导航离开时销毁）
        /// </summary>
        public virtual bool KeepAlive => false;
        
        #endregion

        #region 导航命令
        
        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand GoBackCommand { get; private set; }
        
        /// <summary>
        /// 前进命令
        /// </summary>
        public DelegateCommand GoForwardCommand { get; private set; }
        
        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; private set; }
        
        #endregion

        #region 构造函数
        
        protected NavigationViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            INavigationService? navigationService = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            SessionManager = sessionManager;
            Logger = loggerFactory.CreateLogger<NavigationViewModelBase>();
            // NavigationService will be set during navigation
            
            // 初始化命令
            GoBackCommand = new DelegateCommand(ExecuteGoBack, CanExecuteGoBack);
            GoForwardCommand = new DelegateCommand(ExecuteGoForward, CanExecuteGoForward);
            RefreshCommand = new DelegateCommand(async () => await ExecuteRefreshAsync());
        }
        
        #endregion

        #region INavigationAware实现
        
        /// <summary>
        /// 导航到此页面时调用
        /// </summary>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("导航到页面: {PageType}", GetType().Name);
            
            NavigationJournal = navigationContext.NavigationService.Journal;
            ProcessNavigationParameters(navigationContext.Parameters);
            
            // 异步初始化
            Task.Run(async () =>
            {
                try
                {
                    IsNavigating = true;
                    await OnNavigatedToAsync(navigationContext);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "页面加载失败");
                    var context = new ErrorContext { Operation = "页面加载", Module = GetType().Name };
                    _ = ErrorHandlingService?.HandleExceptionAsync(ex, context);
                }
                finally
                {
                    IsNavigating = false;
                }
            });
            
            UpdateNavigationCommands();
        }
        
        /// <summary>
        /// 从此页面导航离开时调用
        /// </summary>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Logger.LogDebug("从页面导航离开: {PageType}", GetType().Name);
            OnNavigatedFromAsync(navigationContext).ConfigureAwait(false);
        }
        
        /// <summary>
        /// 判断是否为导航目标
        /// </summary>
        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 默认创建新实例（除非KeepAlive为true）
            return KeepAlive;
        }
        
        #endregion

        #region IConfirmNavigationRequest实现
        
        /// <summary>
        /// 确认导航请求
        /// </summary>
        public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            // 检查是否有未保存的更改
            var canNavigate = !HasUnsavedChanges();
            
            if (!canNavigate)
            {
                // 可以在这里显示确认对话框
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
            await LoadDataAsync();
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
            // 尝试获取页面标题
            if (parameters.TryGetValue("title", out object titleObj) && titleObj is string title)
            {
                PageTitle = title;
            }
            
            // 子类重写以处理特定参数
        }
        
        #endregion

        #region 数据加载
        
        /// <summary>
        /// 加载页面数据
        /// </summary>
        protected virtual async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                
                await OnLoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载数据失败");
                var context = new ErrorContext { Operation = "加载数据", Module = GetType().Name };
                _ = ErrorHandlingService?.HandleExceptionAsync(ex, context);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 子类重写以实现具体的数据加载逻辑
        /// </summary>
        protected virtual Task OnLoadDataAsync()
        {
            return Task.CompletedTask;
        }
        
        #endregion

        #region 导航命令实现
        
        /// <summary>
        /// 执行后退导航
        /// </summary>
        private void ExecuteGoBack()
        {
            NavigationJournal?.GoBack();
        }
        
        /// <summary>
        /// 是否可以后退
        /// </summary>
        private bool CanExecuteGoBack()
        {
            return NavigationJournal?.CanGoBack ?? false;
        }
        
        /// <summary>
        /// 执行前进导航
        /// </summary>
        private void ExecuteGoForward()
        {
            NavigationJournal?.GoForward();
        }
        
        /// <summary>
        /// 是否可以前进
        /// </summary>
        private bool CanExecuteGoForward()
        {
            return NavigationJournal?.CanGoForward ?? false;
        }
        
        /// <summary>
        /// 执行刷新
        /// </summary>
        protected virtual async Task ExecuteRefreshAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "刷新数据失败");
                var context = new ErrorContext { Operation = "刷新数据", Module = GetType().Name };
                await ErrorHandlingService?.HandleExceptionAsync(ex, context);
            }
        }
        
        /// <summary>
        /// 更新导航命令状态
        /// </summary>
        private void UpdateNavigationCommands()
        {
            GoBackCommand?.RaiseCanExecuteChanged();
            GoForwardCommand?.RaiseCanExecuteChanged();
        }
        
        #endregion

        #region 导航辅助方法
        
        /// <summary>
        /// 导航到指定页面
        /// </summary>
        protected void NavigateTo(string regionName, string viewName, Prism.Regions.NavigationParameters? parameters = null)
        {
            RegionManager.RequestNavigate(regionName, new Uri(viewName, UriKind.RelativeOrAbsolute), parameters);
        }
        
        /// <summary>
        /// 导航到指定页面（异步）
        /// </summary>
        protected Task<Prism.Regions.NavigationResult> NavigateToAsync(string regionName, string viewName, Prism.Regions.NavigationParameters? parameters = null)
        {
            var tcs = new TaskCompletionSource<Prism.Regions.NavigationResult>();
            
            RegionManager.RequestNavigate(regionName, new Uri(viewName, UriKind.RelativeOrAbsolute), result =>
            {
                tcs.SetResult(result);
            }, parameters);
            
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

        #region 会话支持（来自SessionAwareViewModel）
        
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
        
        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            UpdateNavigationCommands();
            RefreshCommand?.RaiseCanExecuteChanged();
        }
        
        #endregion

        #region 清理
        
        protected override void OnDisposing()
        {
            base.OnDisposing();
            NavigationJournal = null;
        }
        
        #endregion
    }
}