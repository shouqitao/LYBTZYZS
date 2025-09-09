namespace LYBT.Desktop.Core.Models.Common
{

    /// <summary>
    /// 对话框结果接口
    /// 替代 Prism IDialogResult，兼容 Prism 8.1.97
    /// </summary>
    public interface IDialogResult
    {

        /// <summary>
        /// 按钮结果
        /// </summary>
        ButtonResult Result { get; }

        /// <summary>
        /// 传递的参数
        /// </summary>
        DialogParameters Parameters { get; }

        /// <summary>
        /// 异常信息（如果有）
        /// </summary>
        System.Exception? Exception { get; }
    }

    /// <summary>
    /// 默认对话框结果实现
    /// </summary>
    public class DialogResult : IDialogResult
    {
        public ButtonResult Result { get; set; } = ButtonResult.None;
        public DialogParameters Parameters { get; set; } = new DialogParameters();
        public System.Exception? Exception { get; set; }

        public DialogResult()
        {
        }

        public DialogResult(ButtonResult result)
        {
            Result = result;
        }

        public DialogResult(ButtonResult result, DialogParameters parameters)
        {
            Result = result;
            Parameters = parameters;
        }

        public static DialogResult OK(DialogParameters? parameters = null)
        {
            return new DialogResult(ButtonResult.OK, parameters ?? new DialogParameters());
        }

        public static DialogResult Cancel(DialogParameters? parameters = null)
        {
            return new DialogResult(ButtonResult.Cancel, parameters ?? new DialogParameters());
        }

        public static DialogResult Yes(DialogParameters? parameters = null)
        {
            return new DialogResult(ButtonResult.Yes, parameters ?? new DialogParameters());
        }

        public static DialogResult No(DialogParameters? parameters = null)
        {
            return new DialogResult(ButtonResult.No, parameters ?? new DialogParameters());
        }
    }
}
