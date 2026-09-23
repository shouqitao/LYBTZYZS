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

    /// <summary>
    /// 显示打开文件对话框（2026-09-23：与 <see cref="ShowSaveFileDialog"/> 对称——导入类功能需选择既有文件，
    /// 此前 VM 直接使用 <c>Microsoft.Win32.OpenFileDialog</c>，导致 VM 不可测且与 P2-4 抽象目标相悖）
    /// </summary>
    /// <param name="filter">文件类型过滤器（如 "JSON 文件 (*.json)|*.json"）</param>
    /// <param name="defaultExt">默认扩展名（如 ".json"）</param>
    /// <param name="fileName">默认文件名（可选）</param>
    /// <returns>用户选择的完整路径；取消时返回 null</returns>
    string? ShowOpenFileDialog(string filter, string defaultExt, string? fileName = null);
}
