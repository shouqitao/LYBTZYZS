namespace LYBT.Desktop.Patients.ViewModels.Handlers;

/// <summary>
/// 患者导入导出处理接口
/// </summary>
public interface IPatientImportExportHandler
{
    /// <summary>
    /// 导入患者 (Excel)
    /// </summary>
    /// <returns>是否需要刷新列表</returns>
    Task<bool> ImportAsync();

    /// <summary>
    /// 导出患者 (Excel)
    /// </summary>
    /// <param name="searchText">当前搜索关键词</param>
    Task ExportAsync(string? searchText);

    /// <summary>
    /// 下载导入模板
    /// </summary>
    Task DownloadTemplateAsync();
}
