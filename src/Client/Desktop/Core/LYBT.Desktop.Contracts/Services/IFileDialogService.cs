namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 文件对话框服务接口（P2-4：解耦 UI 层对 Microsoft.Win32.SaveFileDialog 的直接依赖）
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// 显示保存文件对话框
    /// </summary>
    /// <param name="filter">文件类型过滤器（如 "PDF 文件 (*.pdf)|*.pdf"）</param>
    /// <param name="defaultExt">默认扩展名（如 ".pdf"）</param>
    /// <param name="fileName">默认文件名</param>
    /// <returns>用户选择的完整路径；取消时返回 null</returns>
    string? ShowSaveFileDialog(string filter, string defaultExt, string fileName);
}
