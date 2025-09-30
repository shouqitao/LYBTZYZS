using System;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 自定义对话框感知接口 - UltraThink架构实现
    /// 用于对话框视图模型与视图之间的通信
    /// </summary>
    public interface ICustomDialogAware
    {
        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        event Action<CustomDialogResult>? RequestClose;
    }

    /// <summary>
    /// 自定义对话框结果
    /// </summary>
    public class CustomDialogResult
    {
        /// <summary>
        /// 对话框结果
        /// </summary>
        public bool? Result { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static CustomDialogResult Success(object? data = null)
        {
            return new CustomDialogResult { Result = true, Data = data };
        }

        /// <summary>
        /// 创建取消结果
        /// </summary>
        public static CustomDialogResult Cancel(object? data = null)
        {
            return new CustomDialogResult { Result = false, Data = data };
        }

        /// <summary>
        /// 创建无结果
        /// </summary>
        public static CustomDialogResult None(object? data = null)
        {
            return new CustomDialogResult { Result = null, Data = data };
        }
    }
}