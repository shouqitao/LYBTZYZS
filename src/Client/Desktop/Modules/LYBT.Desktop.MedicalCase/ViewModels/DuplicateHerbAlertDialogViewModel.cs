using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 重复药材聚合提醒对话框ViewModel
    /// Epic #2175 BF-002 Task 3.10 - 实现重复药材聚合提醒
    /// </summary>
    public class DuplicateHerbAlertDialogViewModel : ViewModelBase, IDialogAware
    {
        #region 字段

        private ObservableCollection<DuplicateHerbInfo> _duplicateHerbs = new();

        #endregion

        #region 构造函数

        public DuplicateHerbAlertDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory)
        {
            // 初始化Commands
            ConfirmCommand = new DelegateCommand(ExecuteConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        #endregion

        #region 属性

        public ObservableCollection<DuplicateHerbInfo> DuplicateHerbs
        {
            get => _duplicateHerbs;
            private set => SetProperty(ref _duplicateHerbs, value);
        }

        #endregion

        #region Commands

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region Command实现

        private void ExecuteConfirm()
        {
            // 用户确认合并
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        private void ExecuteCancel()
        {
            // 用户取消合并
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion

        #region IDialogAware实现

        public string Title => "重复药材提醒";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            Logger.LogDebug("重复药材提醒对话框关闭");
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Logger.LogDebug("重复药材提醒对话框打开");

            // 从参数中获取重复药材列表
            if (parameters.TryGetValue<List<DuplicateHerbInfo>>("DuplicateHerbs", out var duplicates))
            {
                DuplicateHerbs.Clear();
                foreach (var herb in duplicates)
                {
                    DuplicateHerbs.Add(herb);
                }

                Logger.LogInformation("检测到 {Count} 个重复药材", DuplicateHerbs.Count);
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
        /// 合并后剂量（取最大值）
        /// </summary>
        public decimal MergedDosage => Math.Max(CurrentDosage, ImportedDosage);

        /// <summary>
        /// 显示文本
        /// </summary>
        public string DisplayText => $"{HerbName}: {CurrentDosage}g → {ImportedDosage}g (合并为{MergedDosage}g)";
    }

    #endregion
}
