namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 验证错误访问器 - 支持XAML索引器绑定
    /// 使用方式: Errors[PropertyName]
    /// OpenSpec: ui-validation-framework
    /// </summary>
    public class ValidationErrorsAccessor
    {
        private readonly Dictionary<string, List<string>> _errors;

        /// <summary>构造函数</summary>
        public ValidationErrorsAccessor(Dictionary<string, List<string>> errors) => _errors = errors;

        /// <summary>获取指定属性的第一个错误消息</summary>
        public string this[string propertyName] =>
            _errors.TryGetValue(propertyName, out var errors) && errors.Count > 0
                ? errors[0]
                : string.Empty;
    }

    /// <summary>
    /// 验证错误状态访问器 - 支持XAML索引器绑定
    /// 使用方式: HasErrorsDictionary[PropertyName]
    /// OpenSpec: ui-validation-framework
    /// </summary>
    public class ValidationHasErrorsAccessor
    {
        private readonly Dictionary<string, List<string>> _errors;

        /// <summary>构造函数</summary>
        public ValidationHasErrorsAccessor(Dictionary<string, List<string>> errors) => _errors = errors;

        /// <summary>检查指定属性是否有错误</summary>
        public bool this[string propertyName] =>
            _errors.TryGetValue(propertyName, out var errors) && errors.Count > 0;
    }
}
