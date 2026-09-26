using System.IO;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Catalog.Services;

/// <summary>
/// 验方 Excel 导入/导出服务（US-SHELL-021 AC①：三类标准 Excel 模板对齐）。
/// </summary>
/// <remarks>
/// <para>与 <see cref="IHerbExcelService"/> 同构：列定义以服务端 JSON 模板为权威。
/// 验方模板额外含**药材明细列**（药材名 + 剂量），解析时组装为 <c>FormulaImportItemDto.Herbs</c>。</para>
/// <para>解析失败（缺表头/格式非法）抛 <see cref="InvalidDataException"/>，消息含行号与列名（AC②）。</para>
/// </remarks>
public interface IFormulaExcelService
{
    /// <summary>由服务端 JSON 模板生成 Excel 模板（含填写说明页）。</summary>
    /// <param name="serverTemplateJson">服务端 <c>import-template</c> 返回的 JSON 原始字节。</param>
    byte[] GenerateTemplate(byte[] serverTemplateJson);

    /// <summary>解析用户填写的 Excel 为批量导入请求。</summary>
    /// <param name="stream">Excel 文件流。</param>
    /// <exception cref="InvalidDataException">文件结构或单元格格式非法（消息含行号/列名）。</exception>
    FormulaBatchImportInputDto ParseImportFile(Stream stream);

    /// <summary>由服务端导出的 JSON 数据生成 Excel 文件（导出链）。</summary>
    /// <param name="serverExportJson">服务端 <c>export</c> 返回的 JSON 原始字节。</param>
    byte[] GenerateExportFile(byte[] serverExportJson);
}
