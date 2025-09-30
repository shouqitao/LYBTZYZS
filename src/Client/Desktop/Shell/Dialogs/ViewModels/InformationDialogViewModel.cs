using LYBT.Desktop.Services.ErrorHandling;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 信息对话框视图模�?- 架构重构后简化版�?    /// TODO: 重构完成后重新实现业务逻辑
    /// </summary>
    public class InformationDialogViewModel : UnifiedViewModelBase
    {
        public InformationDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
        }
    }
}
