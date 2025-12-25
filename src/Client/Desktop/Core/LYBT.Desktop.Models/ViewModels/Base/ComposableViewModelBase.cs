using System.Reactive.Disposables;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 可组合ViewModel基类
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 服务注入模式基类，支持INavigationAware、IDisposable
    /// 使用组合而非继承来获取功能
    /// </summary>
    public abstract partial class ComposableViewModelBase : LightViewModelBase, INavigationAware, IRegionMemberLifetime
    {
        private readonly CompositeDisposable _disposables = new();

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// 页面标题
        /// </summary>
        [ObservableProperty]
        private string _pageTitle = string.Empty;

        /// <summary>
        /// 是否在导航离开时保持活动
        /// </summary>
        public virtual bool KeepAlive => false;

        protected ComposableViewModelBase(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory?.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        #region INavigationAware

        /// <summary>
        /// 是否是导航目标
        /// </summary>
        /// <param name="navigationContext">导航上下文</param>
        /// <returns>是否接受导航</returns>
        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <summary>
        /// 导航离开时调用
        /// </summary>
        /// <param name="navigationContext">导航上下文</param>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }

        /// <summary>
        /// 导航到时调用
        /// </summary>
        /// <param name="navigationContext">导航上下文</param>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("导航到视图: {ViewType}", GetType().Name);
        }

        #endregion

        #region Disposable Management

        /// <summary>
        /// 添加可释放对象到管理列表
        /// </summary>
        /// <param name="disposable">可释放对象</param>
        protected void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            _disposables.Dispose();
            base.OnDisposing();
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// 处理错误
        /// </summary>
        /// <param name="ex">异常</param>
        /// <param name="context">上下文描述</param>
        protected virtual void HandleError(Exception ex, string? context = null)
        {
            Logger.LogError(ex, "错误发生在: {Context}", context ?? "未知操作");
        }

        #endregion
    }
}
