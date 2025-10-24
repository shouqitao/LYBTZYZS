using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方删除确认对话框ViewModel
    /// Issue #1593 - Phase 4
    /// </summary>
    public class PrescriptionDeleteConfirmDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 数据属性

        private bool _isSoftDelete = true;
        /// <summary>
        /// 是否软删除（默认选中）
        /// </summary>
        public bool IsSoftDelete
        {
            get => _isSoftDelete;
            set
            {
                if (SetProperty(ref _isSoftDelete, value))
                {
                    RaisePropertyChanged(nameof(IsPhysicalDelete));
                }
            }
        }

        /// <summary>
        /// 是否物理删除（反向绑定）
        /// </summary>
        public bool IsPhysicalDelete
        {
            get => !_isSoftDelete;
            set => IsSoftDelete = !value;
        }

        #endregion

        #region 对话框属性

        public string Title => "确认删除处方";

        public event Action<IDialogResult>? RequestClose;

        #endregion

        #region 命令

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionDeleteConfirmDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            ConfirmCommand = new DelegateCommand(OnConfirm);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        #endregion

        #region IDialogAware实现

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            Logger.LogInformation("删除确认对话框已关闭");
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Logger.LogInformation("打开删除确认对话框");
        }

        #endregion

        #region 命令实现

        private void OnConfirm()
        {
            var result = new DialogResult(ButtonResult.OK, new DialogParameters
            {
                { "IsSoftDelete", IsSoftDelete }
            });

            Logger.LogInformation("用户确认删除，删除方式：{DeleteType}", IsSoftDelete ? "软删除" : "物理删除");
            RequestClose?.Invoke(result);
        }

        private void OnCancel()
        {
            Logger.LogInformation("用户取消删除操作");
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion
    }
}
