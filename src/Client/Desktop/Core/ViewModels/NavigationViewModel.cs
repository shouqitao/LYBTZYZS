using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using Prism.Events;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;

namespace LYBT.Desktop.Core.ViewModels
{
    /// <summary>
    /// 支持导航的视图模型基类（兼容性类）
    /// 推荐直接使用 NavigationViewModelBase
    /// </summary>
    [Obsolete("推荐直接使用 NavigationViewModelBase", false)]
    public abstract class NavigationViewModel : NavigationViewModelBase
    {
        /// <summary>
        /// 完整构造函数
        /// </summary>
        protected NavigationViewModel(
            IEventAggregator eventAggregator, 
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
        }

        /// <summary>
        /// 兼容性构造函数
        /// </summary>
        protected NavigationViewModel(IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
        }

        /// <summary>
        /// 重写此方法以支持旧的NavigationParameters类型
        /// </summary>
        public virtual void OnNavigatedTo(NavigationParameters parameters)
        {
            var dict = parameters != null ? new Dictionary<string, object>(parameters) : null;
            OnNavigatedTo(dict);
        }
    }

    /// <summary>
    /// 导航参数类（兼容性）
    /// </summary>
    public class NavigationParameters : Dictionary<string, object>
    {
        /// <summary>
        /// 创建空的导航参数
        /// </summary>
        public NavigationParameters() { }

        /// <summary>
        /// 创建包含单个参数的导航参数
        /// </summary>
        public NavigationParameters(string key, object value)
        {
            Add(key, value);
        }
    }
}