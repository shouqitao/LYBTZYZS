namespace LYBT.Desktop.Contracts.Services.MasterDetail;

/// <summary>
/// 对话框管理接口
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 为MasterDetail模式提供统一的对话框操作
/// 注：实现时委托给ICommonDialogService
/// </summary>
public interface IDialogManager
{
    /// <summary>
    /// 显示确认对话框
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <returns>用户确认结果</returns>
    Task<bool> ShowConfirmAsync(string title, string message);

    /// <summary>
    /// 显示错误消息
    /// </summary>
    /// <param name="message">错误内容</param>
    Task ShowErrorAsync(string message);

    /// <summary>
    /// 显示成功消息
    /// </summary>
    /// <param name="message">成功内容</param>
    Task ShowSuccessAsync(string message);

    /// <summary>
    /// 显示警告消息
    /// </summary>
    /// <param name="message">警告内容</param>
    Task ShowWarningAsync(string message);

    /// <summary>
    /// 显示信息消息
    /// </summary>
    /// <param name="message">信息内容</param>
    Task ShowInfoAsync(string message);

    /// <summary>
    /// 显示删除确认对话框
    /// </summary>
    /// <param name="itemName">待删除项名称</param>
    /// <returns>用户确认结果</returns>
    Task<bool> ShowDeleteConfirmAsync(string itemName);

    /// <summary>
    /// 显示放弃更改确认对话框
    /// </summary>
    /// <returns>用户确认结果</returns>
    Task<bool> ShowDiscardChangesConfirmAsync();
}
