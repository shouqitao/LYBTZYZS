namespace LYBT.Desktop.Infrastructure.ViewModels.Base
{
    /// <summary>
    /// 楠岃瘉閿欒璁块棶鍣?- 鏀寔XAML绱㈠紩鍣ㄧ粦瀹?    /// 浣跨敤鏂瑰紡: Errors[PropertyName]
    /// OpenSpec: ui-validation-framework
    /// </summary>
    public class ValidationErrorsAccessor
    {
        private readonly Dictionary<string, List<string>> _errors;

        /// <summary>鏋勯€犲嚱鏁?/summary>
        public ValidationErrorsAccessor(Dictionary<string, List<string>> errors) => _errors = errors;

        /// <summary>鑾峰彇鎸囧畾灞炴€х殑绗竴涓敊璇秷鎭?/summary>
        public string this[string propertyName] =>
            _errors.TryGetValue(propertyName, out var errors) && errors.Count > 0
                ? errors[0]
                : string.Empty;
    }

    /// <summary>
    /// 楠岃瘉閿欒鐘舵€佽闂櫒 - 鏀寔XAML绱㈠紩鍣ㄧ粦瀹?    /// 浣跨敤鏂瑰紡: HasErrorsDictionary[PropertyName]
    /// OpenSpec: ui-validation-framework
    /// </summary>
    public class ValidationHasErrorsAccessor
    {
        private readonly Dictionary<string, List<string>> _errors;

        /// <summary>鏋勯€犲嚱鏁?/summary>
        public ValidationHasErrorsAccessor(Dictionary<string, List<string>> errors) => _errors = errors;

        /// <summary>妫€鏌ユ寚瀹氬睘鎬ф槸鍚︽湁閿欒</summary>
        public bool this[string propertyName] =>
            _errors.TryGetValue(propertyName, out var errors) && errors.Count > 0;
    }
}
