namespace LYBT.Desktop.Core.Models.Common
{

    /// <summary>
    /// 自定义对话框结果
    /// 替代 Prism IDialogResult，兼容 Prism 8.1.97
    /// </summary>
    public class CustomDialogResult
    {

        /// <summary>
        /// 对话框返回结果
        /// true: 确认/成功，false: 取消/失败，null: 其他状态
        /// </summary>
        public bool? Result { get; set; }

        /// <summary>
        /// 传递的参数数据
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// 从对话框返回的数据
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <param name="data">返回数据</param>
        /// <returns>成功结果</returns>
        public static CustomDialogResult Success(object? data = null)
        {
            return new CustomDialogResult
            {
                Result = true,
                Data = data
            };
        }

        /// <summary>
        /// 创建取消结果
        /// </summary>
        /// <returns>取消结果</returns>
        public static CustomDialogResult Cancel()
        {
            return new CustomDialogResult
            {
                Result = false
            };
        }

        /// <summary>
        /// 创建带参数的结果
        /// </summary>
        /// <param name="result">结果状态</param>
        /// <param name="parameters">参数</param>
        /// <param name="data">数据</param>
        /// <returns>对话框结果</returns>
        public static CustomDialogResult Create(bool? result, Dictionary<string, object>? parameters = null, object? data = null)
        {
            return new CustomDialogResult
            {
                Result = result,
                Parameters = parameters ?? new Dictionary<string, object>(),
                Data = data
            };
        }
    }
}
