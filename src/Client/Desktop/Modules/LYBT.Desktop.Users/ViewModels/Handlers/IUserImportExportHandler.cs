namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户导入导出处理接口
/// OpenSpec: refactor-frontend-srp-patterns - Handler提取模式
/// </summary>
public interface IUserImportExportHandler
{
    /// <summary>
    /// 导入用户
    /// </summary>
    /// <returns>是否需要刷新列表</returns>
    Task<bool> ImportAsync();

    /// <summary>
    /// 导出用户
    /// </summary>
    /// <param name="searchText">搜索关键字</param>
    Task ExportAsync(string? searchText);

    /// <summary>
    /// 下载导入模板
    /// </summary>
    Task DownloadTemplateAsync();
}
