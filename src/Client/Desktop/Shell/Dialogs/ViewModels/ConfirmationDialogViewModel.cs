using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 确认对话框视图模型 - Phase 4B 骨架实现
    /// </summary>
    public class ConfirmationDialogViewModel : UnifiedViewModelBase
    {
        /// <summary>
        /// 是命令
        /// </summary>
        public DelegateCommand YesCommand { get; }

        /// <summary>
        /// 否命令
        /// </summary>
        public DelegateCommand NoCommand { get; }

        public ConfirmationDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
            YesCommand = new DelegateCommand(ExecuteYes);
            NoCommand = new DelegateCommand(ExecuteNo);
        }

        private void ExecuteYes()
        {
            Logger.LogInformation("ConfirmationDialog - 是命令执行（骨架实现）");
            // 骨架实现,已由 UnifiedViewModelBase.ShowConfirmationAsync 替代
        }

        private void ExecuteNo()
        {
            Logger.LogInformation("ConfirmationDialog - 否命令执行（骨架实现）");
            // 骨架实现,已由 UnifiedViewModelBase.ShowConfirmationAsync 替代
        }
    }
}
