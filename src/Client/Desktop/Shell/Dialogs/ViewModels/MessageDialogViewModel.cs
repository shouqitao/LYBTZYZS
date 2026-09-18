using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        /// <summary>成功消息</summary>
        Success,
        /// <summary>错误消息</summary>
        Error,
        /// <summary>警告消息</summary>
        Warning,
        /// <summary>信息提示</summary>
        Info
    }

    /// <summary>
    /// 统一消息对话框视图模型
    ///
    /// 统一处理 Success/Error/Warning/Info 四种消息类型，
    /// 通过 MessageType 参数区分，使用对应的图标和配色。
    /// </summary>
    public partial class MessageDialogViewModel : DialogViewModelBase
    {
        #region 可观察属性

        /// <summary>
        /// 消息内容
        /// </summary>
        [ObservableProperty]
        private string _message = string.Empty;

        /// <summary>
        /// 消息类型
        /// </summary>
        [ObservableProperty]
        private MessageType _messageType = MessageType.Info;

        /// <summary>
        /// 确认按钮文本
        /// </summary>
        [ObservableProperty]
        private string _okButtonText = "确定";

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public MessageDialogViewModel(IViewModelServices services)
            : base(services)
        {
            Title = "提示";
        }

        #endregion

        #region 对话框生命周期

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        protected override void OnDialogOpenedCore(IDialogParameters? parameters)
        {
            if (parameters == null) return;

            // PascalCase 键（DialogParams 统一契约）
            Message = GetDialogParameter(parameters, LYBT.Desktop.Infrastructure.Services.DialogParams.Message, string.Empty);

            Title = GetDialogParameter(parameters, LYBT.Desktop.Infrastructure.Services.DialogParams.Title, GetDefaultTitle());

            if (parameters.TryGetValue<string>(LYBT.Desktop.Infrastructure.Services.DialogParams.Type, out var typeStr))
            {
                MessageType = ParseMessageType(typeStr);
                if (!parameters.ContainsKey(LYBT.Desktop.Infrastructure.Services.DialogParams.Title))
                {
                    Title = GetDefaultTitle();
                }
            }

            Logger.LogInformation("MessageDialog - 打开对话框，类型：{MessageType}，标题：{Title}",
                MessageType, Title);
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        protected override void OnDialogClosedCore()
        {
            Logger.LogDebug("MessageDialog - 对话框已关闭");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 解析消息类型字符串
        /// </summary>
        private static MessageType ParseMessageType(string type)
        {
            return type?.ToLowerInvariant() switch
            {
                "success" => MessageType.Success,
                "error" => MessageType.Error,
                "warning" => MessageType.Warning,
                "info" => MessageType.Info,
                _ => MessageType.Info
            };
        }

        /// <summary>
        /// 获取默认标题
        /// </summary>
        private string GetDefaultTitle()
        {
            return MessageType switch
            {
                MessageType.Success => "成功",
                MessageType.Error => "错误",
                MessageType.Warning => "警告",
                MessageType.Info => "提示",
                _ => "提示"
            };
        }

        #endregion

        #region 命令

        /// <summary>
        /// 确认命令 - 关闭对话框
        /// </summary>
        protected override void Confirm()
        {
            Logger.LogDebug("MessageDialog - 确认关闭");
            CloseDialog(ButtonResult.OK);
        }

        #endregion
    }
}
