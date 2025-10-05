using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 信息对话框视图模型 - Phase 4B 骨架实现
    /// </summary>
    public class InformationDialogViewModel : UnifiedViewModelBase
    {
        /// <summary>
        /// 确定命令
        /// </summary>
        public DelegateCommand OkCommand { get; }

        public InformationDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
            OkCommand = new DelegateCommand(ExecuteOk);
        }

        private void ExecuteOk()
        {
            Logger.LogInformation("InformationDialog - 确定命令执行（骨架实现）");
            // 骨架实现,已由 UnifiedViewModelBase.ShowSuccessMessageAsync 替代
        }
    }
}
