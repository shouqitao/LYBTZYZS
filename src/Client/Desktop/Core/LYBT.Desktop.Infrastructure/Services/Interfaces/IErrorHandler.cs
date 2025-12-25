using System.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 错误处理服务接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 提供异常处理、错误消息管理、INotifyDataErrorInfo集成
    /// </summary>
    public interface IErrorHandler : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        /// <summary>错误消息</summary>
        string? ErrorMessage { get; }

        /// <summary>是否有错误</summary>
        new bool HasErrors { get; }

        /// <summary>所有错误集合</summary>
        IReadOnlyDictionary<string, IReadOnlyList<string>> AllErrors { get; }

        /// <summary>
        /// 错误变更事件
        /// </summary>
        event EventHandler<ErrorChangedEventArgs>? ErrorChanged;

        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="context">上下文信息</param>
        void HandleException(Exception exception, string? context = null);

        /// <summary>
        /// 设置错误
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <param name="error">错误消息</param>
        void SetError(string propertyName, string error);

        /// <summary>
        /// 设置多个错误
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <param name="errors">错误消息集合</param>
        void SetErrors(string propertyName, IEnumerable<string> errors);

        /// <summary>
        /// 清除指定属性的错误
        /// </summary>
        /// <param name="propertyName">属性名</param>
        void ClearError(string propertyName);

        /// <summary>
        /// 清除所有错误
        /// </summary>
        void ClearAllErrors();

        /// <summary>
        /// 验证属性
        /// </summary>
        /// <param name="value">属性值</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>是否验证通过</returns>
        bool ValidateProperty(object? value, string propertyName);

        /// <summary>
        /// 验证所有属性
        /// </summary>
        /// <param name="target">验证目标对象</param>
        /// <returns>是否验证通过</returns>
        bool ValidateAll(object target);
    }

    /// <summary>
    /// 错误变更事件参数
    /// </summary>
    public class ErrorChangedEventArgs : EventArgs
    {
        /// <summary>属性名</summary>
        public string? PropertyName { get; }

        /// <summary>错误消息</summary>
        public IReadOnlyList<string> Errors { get; }

        public ErrorChangedEventArgs(string? propertyName, IReadOnlyList<string> errors)
        {
            PropertyName = propertyName;
            Errors = errors;
        }
    }
}
