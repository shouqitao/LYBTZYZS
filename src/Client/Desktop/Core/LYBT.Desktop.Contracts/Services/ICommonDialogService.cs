namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 三选项对话框结果
    /// Issue #2247: 支持YesNoCancel类型对话框
    /// </summary>
    public enum TripleChoiceResult
    {
        /// <summary>选择"是"</summary>
        Yes,
        /// <summary>选择"否"</summary>
        No,
        /// <summary>选择"取消"</summary>
        Cancel
    }

    /// <summary>
    /// 通用对话框服务接口 - UltraThink架构对话框抽象
    /// 负责统一的用户交互对话框：确认、提示、输入等
    /// </summary>
    public interface ICommonDialogService
    {
        /// <summary>
        /// 显示信息消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        Task ShowInfoAsync(string message, string? title = null);

        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">警告内容</param>
        /// <param name="title">标题</param>
        Task ShowWarningAsync(string message, string? title = null);

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误内容</param>
        /// <param name="title">标题</param>
        Task ShowErrorAsync(string message, string? title = null);

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户确认结果</returns>
        Task<bool> ShowConfirmAsync(string message, string? title = null);

        /// <summary>
        /// 显示三选项对话框（是/否/取消）
        /// Issue #2247: 支持离开确认等三选项场景
        /// </summary>
        /// <param name="message">对话内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择结果</returns>
        Task<TripleChoiceResult> ShowTripleChoiceAsync(string message, string? title = null);

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <param name="message">提示内容</param>
        /// <param name="title">标题</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>用户输入结果，null表示取消</returns>
        Task<string?> ShowInputAsync(string message, string? title = null, string? defaultValue = null);

        /// <summary>
        /// 显示文件选择对话框
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <param name="title">标题</param>
        /// <returns>选中的文件路径，null表示取消</returns>
        Task<string?> ShowOpenFileDialogAsync(string? filter = null, string? title = null);

        /// <summary>
        /// 显示文件保存对话框
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <param name="title">标题</param>
        /// <param name="defaultFileName">默认文件名</param>
        /// <returns>保存的文件路径，null表示取消</returns>
        Task<string?> ShowSaveFileDialogAsync(string? filter = null, string? title = null, string? defaultFileName = null);

        /// <summary>
        /// 显示未完成医案四选项对话框
        /// OpenSpec: optimize-medicalcase-navigation
        /// </summary>
        /// <param name="patientName">患者姓名</param>
        /// <returns>用户选择结果</returns>
        Task<UnfinishedCaseChoice> ShowUnfinishedCaseDialogAsync(string patientName);
    }
}
