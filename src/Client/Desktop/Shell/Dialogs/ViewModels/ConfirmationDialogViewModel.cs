using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 确认对话框视图模型 - Epic #1676 Phase 2 完整实现
    /// 支持可配置标题、消息、图标、按钮文本和删除选项（软删除/物理删除）
    /// 实现IDialogAware接口，符合Prism Dialog标准
    /// </summary>
    public class ConfirmationDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 私有字段

        private string _title = "确认操作";
        private string _message = "确定要执行此操作吗？";
        private string _iconSource = "/Assets/Icons/warning.png";
        private string _confirmButtonText = "确认";
        private string _cancelButtonText = "取消";
        private bool _showDeleteOptions;
        private bool _isSoftDelete = true;

        #endregion

        #region 公共属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// 图标路径
        /// </summary>
        public string IconSource
        {
            get => _iconSource;
            set => SetProperty(ref _iconSource, value);
        }

        /// <summary>
        /// 确认按钮文本
        /// </summary>
        public string ConfirmButtonText
        {
            get => _confirmButtonText;
            set => SetProperty(ref _confirmButtonText, value);
        }

        /// <summary>
        /// 取消按钮文本
        /// </summary>
        public string CancelButtonText
        {
            get => _cancelButtonText;
            set => SetProperty(ref _cancelButtonText, value);
        }

        /// <summary>
        /// 是否显示删除选项（软删除/物理删除）
        /// </summary>
        public bool ShowDeleteOptions
        {
            get => _showDeleteOptions;
            set => SetProperty(ref _showDeleteOptions, value);
        }

        /// <summary>
        /// 是否选择软删除
        /// </summary>
        public bool IsSoftDelete
        {
            get => _isSoftDelete;
            set => SetProperty(ref _isSoftDelete, value);
        }

        /// <summary>
        /// 是否选择物理删除（与IsSoftDelete互斥）
        /// </summary>
        public bool IsHardDelete
        {
            get => !_isSoftDelete;
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

        #region IDialogAware 实现

        /// <summary>
        /// 对话框关闭请求事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

        /// <summary>
        /// 是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog() => true;

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 从参数中读取配置
            if (parameters.ContainsKey("Title"))
                Title = parameters.GetValue<string>("Title");

            if (parameters.ContainsKey("Message"))
                Message = parameters.GetValue<string>("Message");

            if (parameters.ContainsKey("IconSource"))
                IconSource = parameters.GetValue<string>("IconSource");

            if (parameters.ContainsKey("ConfirmButtonText"))
                ConfirmButtonText = parameters.GetValue<string>("ConfirmButtonText");

            if (parameters.ContainsKey("CancelButtonText"))
                CancelButtonText = parameters.GetValue<string>("CancelButtonText");

            if (parameters.ContainsKey("ShowDeleteOptions"))
                ShowDeleteOptions = parameters.GetValue<bool>("ShowDeleteOptions");

            Logger.LogInformation("ConfirmationDialog - 打开对话框，标题：{Title}，显示删除选项：{ShowDeleteOptions}",
                Title, ShowDeleteOptions);
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed()
        {
            Logger.LogInformation("ConfirmationDialog - 对话框已关闭");
        }

        #endregion

        #region 命令

        /// <summary>
        /// 确认命令
        /// </summary>
        public DelegateCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public ConfirmationDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
            ConfirmCommand = new DelegateCommand(ExecuteConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        #endregion

        #region 私有方法

        private void ExecuteConfirm()
        {
            Logger.LogInformation("ConfirmationDialog - 确认操作，删除模式：{DeleteMode}",
                ShowDeleteOptions ? (IsSoftDelete ? "软删除" : "物理删除") : "普通确认");

            // 返回结果和参数
            var result = new DialogResult(ButtonResult.OK, new DialogParameters
            {
                { "IsSoftDelete", IsSoftDelete }
            });

            RequestClose?.Invoke(result);
        }

        private void ExecuteCancel()
        {
            Logger.LogInformation("ConfirmationDialog - 取消操作");

            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion
    }
}
