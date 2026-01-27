using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
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
    /// OpenSpec: fix-missing-dialogs - 系统性设计
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
        [NotifyPropertyChangedFor(nameof(IconSource))]
        [NotifyPropertyChangedFor(nameof(IconColor))]
        private MessageType _messageType = MessageType.Info;

        /// <summary>
        /// 确认按钮文本
        /// </summary>
        [ObservableProperty]
        private string _okButtonText = "确定";

        #endregion

        #region 计算属性

        /// <summary>
        /// 图标路径（根据消息类型）
        /// </summary>
        public string IconSource => MessageType switch
        {
            MessageType.Success => "/Assets/Icons/success.png",
            MessageType.Error => "/Assets/Icons/error.png",
            MessageType.Warning => "/Assets/Icons/warning.png",
            MessageType.Info => "/Assets/Icons/info.png",
            _ => "/Assets/Icons/info.png"
        };

        /// <summary>
        /// 图标颜色（根据消息类型）
        /// </summary>
        public string IconColor => MessageType switch
        {
            MessageType.Success => "#228B22",  // 木(青) - 成功
            MessageType.Error => "#DC143C",    // 火(赤) - 错误
            MessageType.Warning => "#DAA520",  // 土(黄) - 警告
            MessageType.Info => "#4682B4",     // 水(黑) - 信息
            _ => "#4682B4"
        };

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

            // 读取消息内容
            Message = GetDialogParameter(parameters, "message", string.Empty);

            // 读取标题（可选）
            Title = GetDialogParameter(parameters, "title", GetDefaultTitle());

            // 读取消息类型
            if (parameters.TryGetValue<string>("type", out var typeStr))
            {
                MessageType = ParseMessageType(typeStr);
                // 如果没有传入title，使用类型对应的默认标题
                if (!parameters.ContainsKey("title"))
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
