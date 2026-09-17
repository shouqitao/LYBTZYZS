namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 文件对话框服务实现（P2-4：包装 Microsoft.Win32.SaveFileDialog，隔离 UI 依赖）
/// </summary>
public class FileDialogService : LYBT.Desktop.Contracts.Services.IFileDialogService
{
    /// <inheritdoc/>
    public string? ShowSaveFileDialog(string filter, string defaultExt, string fileName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExt,
            FileName = fileName
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
