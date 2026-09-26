using System.IO;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Catalog.Services;

/// <summary>
/// 药材 Excel 导入/导出服务（US-SHELL-021 AC①：三类标准 Excel 模板对齐）。
/// </summary>
/// <remarks>
/// <para>与 <c>PatientExcelService</c>（B-12）同构：**列定义以服务端 JSON 模板为权威**——
/// <see cref="GenerateTemplate"/> 把服务端 <c>GET /herbs/import-template</c> 的字段说明渲染成
/// 带「填写说明」页的 .xlsx；<see cref="ParseImportFile"/> 把用户填写的 .xlsx 解析回批量导入 DTO。</para>
/// <para>解析失败（缺表头/格式非法）抛 <see cref="InvalidDataException"/>，消息含**行号与列名**，
/// 供 ViewModel 直接展示为「文件格式错误：…」（AC② 错误行定位）。</para>
/// </remarks>
public interface IHerbExcelService
{
    /// <summary>由服务端 JSON 模板生成 Excel 模板（含填写说明页）。</summary>
    /// <param name="serverTemplateJson">服务端 <c>import-template</c> 返回的 JSON 原始字节。</param>
    byte[] GenerateTemplate(byte[] serverTemplateJson);

    /// <summary>解析用户填写的 Excel 为批量导入请求（含重复策略占位，由调用方设置）。</summary>
    /// <param name="stream">Excel 文件流。</param>
    /// <exception cref="InvalidDataException">文件结构或单元格格式非法（消息含行号/列名）。</exception>
    HerbBatchImportInputDto ParseImportFile(Stream stream);

    /// <summary>由服务端导出的 JSON 数据生成 Excel 文件（导出链）。</summary>
    /// <param name="serverExportJson">服务端 <c>export</c> 返回的 JSON 原始字节。</param>
    byte[] GenerateExportFile(byte[] serverExportJson);
}
