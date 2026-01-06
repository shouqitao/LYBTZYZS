using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 确认对话框视图模型 - Epic #1676 Phase 2 完整实现
    /// 支持可配置标题、消息、图标、按钮文本和删除选项（软删除/物理删除）
    /// OpenSpec: standardize-viewmodel-framework - 迁移到DialogViewModelBase
    /// </summary>
    public partial class ConfirmationDialogViewModel : DialogViewModelBase
    {
        #region 可观察属性

        /// <summary>
        /// 消息内容
        /// </summary>
        [ObservableProperty]
        private string _message = "确定要执行此操作吗？";

        /// <summary>
        /// 图标路径
        /// </summary>
        [ObservableProperty]
        private string _iconSource = "/Assets/Icons/warning.png";

        /// <summary>
        /// 确认按钮文本
        /// </summary>
        [ObservableProperty]
        private string _confirmButtonText = "确认";

        /// <summary>
        /// 取消按钮文本
        /// </summary>
        [ObservableProperty]
        private string _cancelButtonText = "取消";

        /// <summary>
        /// 是否显示删除选项（软删除/物理删除）
        /// </summary>
        [ObservableProperty]
        private bool _showDeleteOptions;

        /// <summary>
        /// 是否选择软删除
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsHardDelete))]
        private bool _isSoftDelete = true;

        #endregion

        #region 计算属性

        /// <summary>
        /// 是否选择物理删除（与IsSoftDelete互斥）
        /// </summary>
        public bool IsHardDelete
        {
            get => !IsSoftDelete;
            set
            {
                if (value)
                {
                    IsSoftDelete = false;
                }
            }
        }

        /// <summary>
        /// 是否选择了软删除（用于调用方读取）
        /// </summary>
        public bool IsSoftDeleteSelected => IsSoftDelete;

        #endregion

        #region 构造函数

        public ConfirmationDialogViewModel(
            ILoggerFactory loggerFactory,
            IEventAggregator eventAggregator)
            : base(loggerFactory, eventAggregator)
        {
            Title = "确认操作";
        }

        #endregion

        #region 对话框生命周期

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        protected override void OnDialogOpenedCore(IDialogParameters? parameters)
        {
            if (parameters == null) return;

            // 从参数中读取配置
            Title = GetDialogParameter(parameters, "Title", "确认操作");
            Message = GetDialogParameter(parameters, "Message", "确定要执行此操作吗？");
            IconSource = GetDialogParameter(parameters, "IconSource", "/Assets/Icons/warning.png");
            ConfirmButtonText = GetDialogParameter(parameters, "ConfirmButtonText", "确认");
            CancelButtonText = GetDialogParameter(parameters, "CancelButtonText", "取消");
            ShowDeleteOptions = GetDialogParameter(parameters, "ShowDeleteOptions", false);

            Logger.LogInformation("ConfirmationDialog - 打开对话框，标题：{Title}，显示删除选项：{ShowDeleteOptions}",
                Title, ShowDeleteOptions);
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        protected override void OnDialogClosedCore()
        {
            Logger.LogInformation("ConfirmationDialog - 对话框已关闭");
        }

        #endregion

        #region 命令

        /// <summary>
        /// 确认命令
        /// </summary>
        protected override void Confirm()
        {
            Logger.LogInformation("ConfirmationDialog - 确认操作，删除模式：{DeleteMode}",
                ShowDeleteOptions ? (IsSoftDelete ? "软删除" : "物理删除") : "普通确认");

            // 返回结果和参数
            var parameters = new DialogParameters
            {
                { "IsSoftDelete", IsSoftDelete }
            };

            CloseDialog(parameters, ButtonResult.OK);
        }

        /// <summary>
        /// 取消命令
        /// </summary>
        protected override void Cancel()
        {
            Logger.LogInformation("ConfirmationDialog - 取消操作");
            CloseDialog(ButtonResult.Cancel);
        }

        #endregion
    }
}
