using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 重复药材提醒对话框ViewModel
    /// OpenSpec: enhance-duplicate-herb-dialog - 简化为单药材确认
    /// </summary>
    public class DuplicateHerbAlertDialogViewModel : ViewModelBase, IDialogAware
    {
        #region 字段

        private string _herbName = string.Empty;

        #endregion

        #region 构造函数

        public DuplicateHerbAlertDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory)
        {
            ConfirmCommand = new DelegateCommand(ExecuteConfirm);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 药材名称
        /// </summary>
        public string HerbName
        {
            get => _herbName;
            private set => SetProperty(ref _herbName, value);
        }

        #endregion

        #region Commands

        public DelegateCommand ConfirmCommand { get; }

        #endregion

        #region Command实现

        private void ExecuteConfirm()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        #endregion

        #region IDialogAware实现

        public string Title => "重复药材提醒";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
            Logger.LogDebug("重复药材提醒对话框关闭: {HerbName}", HerbName);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue<string>("HerbName", out var herbName))
            {
                HerbName = herbName;
                Logger.LogDebug("显示重复药材提醒: {HerbName}", herbName);
            }
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// 重复药材信息
    /// </summary>
    public class DuplicateHerbInfo
    {
        /// <summary>
        /// 药材ID
        /// </summary>
        public Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 当前剂量
        /// </summary>
        public decimal CurrentDosage { get; set; }

        /// <summary>
        /// 导入剂量
        /// </summary>
        public decimal ImportedDosage { get; set; }

        /// <summary>
        /// 合并后剂量（根据appsettings.json配置策略）
        /// 配置项: Prescription.DuplicateHerbMergeStrategy
        /// 通过PrescriptionSettingsService静态方法获取
        /// </summary>
        public decimal MergedDosage => PrescriptionSettingsService.GetMergedDosage(CurrentDosage, ImportedDosage);

        /// <summary>
        /// 显示文本
        /// </summary>
        public string DisplayText => $"{HerbName}: {CurrentDosage}g → {ImportedDosage}g (合并为{MergedDosage}g)";
    }

    #endregion
}
