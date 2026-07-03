namespace LYBT.Module.Herbs.Interfaces;

/// <summary>
/// 药材导入导出服务接口
/// </summary>
public interface IHerbImportExportService
{
    /// <summary>
    /// 导出药材数据到Excel
    /// </summary>
    /// <param name="category">可选的分类筛选</param>
    /// <returns>Excel文件的内存流</returns>
    Task<MemoryStream> ExportAsync(string? category = null);

    /// <summary>
    /// 生成药材导入模板
    /// </summary>
    /// <returns>Excel模板文件的内存流</returns>
    MemoryStream GenerateImportTemplate();
}
