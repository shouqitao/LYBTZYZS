using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 配方管理视图模型 - 架构重构后简化版本
    /// TODO: 重构完成后重新实现业务逻辑
    /// </summary>
    public class FormulaManagementViewModel : NavigationViewModelBase
    {
        public FormulaManagementViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager sessionManager,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, null, errorHandlingService)
        {
        }
    }
}
