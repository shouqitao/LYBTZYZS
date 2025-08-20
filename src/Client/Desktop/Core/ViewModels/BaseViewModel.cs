using LYBT.Shared.Models.Contracts.Common;
using System;
using Prism.Events;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;

namespace LYBT.Desktop.Core.ViewModels
{
    /// <summary>
    /// 基础ViewModel（兼容性类）
    /// 推荐使用具体的基类：CoreViewModel、ServiceViewModel、DialogViewModel、NavigationViewModelBase
    /// </summary>
    [Obsolete("推荐使用 ServiceViewModel 或其他具体的基类", false)]
    public abstract class BaseViewModel : ServiceViewModel
    {
        /// <summary>
        /// 完整构造函数
        /// </summary>
        protected BaseViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
        }

        /// <summary>
        /// 兼容性构造函数
        /// </summary>
        protected BaseViewModel(IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
        }
    }
}