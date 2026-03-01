namespace LYBT.Desktop.Herbs.ViewModels.Handlers;

/// <summary>
/// 药材导入导出处理接口
/// </summary>
public interface IHerbImportExportHandler
{
    /// <summary>
    /// 导入药材 (Excel)
    /// </summary>
    /// <returns>是否需要刷新列表</returns>
    Task<bool> ImportAsync();

    /// <summary>
    /// 导出药材 (Excel)
    /// </summary>
    /// <param name="searchText">当前搜索关键词</param>
    Task ExportAsync(string? searchText);
}
