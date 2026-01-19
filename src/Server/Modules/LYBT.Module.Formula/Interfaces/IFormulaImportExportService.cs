using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formulas.Interfaces;

/// <summary>
/// 验方导入导出服务接口
/// OpenSpec: refactor-server-srp-patterns - 从FormulaService拆分Import/Export职责
/// Issue #1166: 验方导入导出功能
/// </summary>
public interface IFormulaImportExportService
{
    /// <summary>
    /// 批量导入验方数据
    /// 架构原则：Server端只处理结构化DTO，Excel解析由Client端负责
    /// 返回FormulaBatchImportResultDto包含药材匹配统计
    /// </summary>
    /// <param name="formulas">待导入的验方数据列表</param>
    /// <param name="fileName">可选的文件名（用于记录导入来源）</param>
    /// <returns>导入结果，包含成功/失败统计和药材匹配情况</returns>
    Task<Result<FormulaBatchImportResultDto>> ImportFromDataAsync(
        List<FormulaImportItemDto> formulas, 
        string? fileName = null);

    /// <summary>
    /// 导出验方数据到Excel
    /// </summary>
    /// <param name="category">可选的分类筛选</param>
    /// <returns>Excel文件的内存流</returns>
    Task<MemoryStream> ExportAsync(string? category = null);

    /// <summary>
    /// 生成验方导入模板
    /// </summary>
    /// <returns>Excel模板文件的内存流</returns>
    MemoryStream GenerateImportTemplate();
}
