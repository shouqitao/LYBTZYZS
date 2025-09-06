namespace LYBT.Desktop.Core.Attributes {

    /// <summary>
    /// UltraThink Phase 3.1: 自动Command生成特性
    ///
    /// 用法:
    /// [AutoCommand]
    /// void SaveData() { /* 业务逻辑 */ }
    ///
    /// 自动生成:
    /// public DelegateCommand SaveDataCommand { get; }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AutoCommandAttribute : Attribute {

        /// <summary>
        /// Command名称（可选，默认使用方法名+Command）
        /// </summary>
        public string? CommandName { get; set; }

        /// <summary>
        /// 是否为异步命令
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// CanExecute方法名（可选，默认查找Can+方法名）
        /// </summary>
        public string? CanExecuteMethod { get; set; }

        /// <summary>
        /// 是否在加载状态时禁用
        /// </summary>
        public bool DisableWhenLoading { get; set; } = true;

        /// <summary>
        /// 错误处理策略
        /// </summary>
        public ErrorHandlingStrategy ErrorHandling { get; set; } = ErrorHandlingStrategy.ShowDialog;

        public AutoCommandAttribute() {
        }

        public AutoCommandAttribute(string commandName) {
            CommandName = commandName;
        }
    }

    /// <summary>
    /// 错误处理策略
    /// </summary>
    public enum ErrorHandlingStrategy {

        /// <summary>静默处理，仅记录日志</summary>
        Silent,

        /// <summary>设置错误状态，显示在界面</summary>
        SetError,

        /// <summary>显示错误对话框</summary>
        ShowDialog,

        /// <summary>抛出异常，由上层处理</summary>
        Throw
    }
}
