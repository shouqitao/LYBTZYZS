using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 确认对话框视图模�?- 架构重构后简化版�?    /// TODO: 重构完成后重新实现业务逻辑
    /// </summary>
    public class ConfirmationDialogViewModel : UnifiedViewModelBase
    {
        public ConfirmationDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
        }
    }
}
