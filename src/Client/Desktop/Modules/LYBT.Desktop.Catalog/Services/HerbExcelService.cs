using System.Globalization;
using System.IO;
using System.Text.Json;
using ClosedXML.Excel;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Catalog.Services;

/// <summary>
/// 药材 Excel 导入导出服务（US-SHELL-021 AC①：三类标准 Excel 模板对齐；与 <c>PatientExcelService</c>（B-12）同构）。
/// <para>职责边界：只做 Excel ↔ 契约转换——模板列以服务端 <c>GET /api/v1/herbs/import-template</c> 的
/// JSON 字段说明为 SSOT（不复制字段定义）；导入产出 <see cref="HerbBatchImportInputDto"/> 供既有 batch-import 使用；
/// 导出消费服务端 <c>GET /api/v1/herbs/export</c> 的 JSON 数组。</para>
/// <para>文件格式错误抛 <see cref="InvalidDataException"/>（消息含行号/列名——UI 直接展示）。</para>
/// </summary>
public class HerbExcelService : IHerbExcelService
{
    /// <summary>药材数据工作表名（模板与导出共用）</summary>
    public const string DataSheetName = "药材数据";

    /// <summary>填写说明工作表名（仅模板）</summary>
    public const string GuideSheetName = "填写说明";

    /// <summary>导入列（顺序 = 服务端模板字段顺序；Field = 服务端模板字段名 / camelCase JSON 属性名）</summary>
    private static readonly ColumnSpec[] ImportColumns =
    {
        new("Name", "药材名称", Required: true),
        new("PinYinCode", "拼音码", Required: false),
        new("Category", "分类", Required: false),
        new("Properties", "性味", Required: false),
        new("Origin", "产地", Required: false),
        new("Spec", "规格", Required: false),
        new("Unit", "单位", Required: false),
        new("Price", "单价", Required: false),
        new("CostPrice", "成本价", Required: false),
    };

    /// <summary>导出列（Property = 服务端导出 JSON 的 HerbListDto camelCase 字段）</summary>
    private static readonly ExportColumnSpec[] ExportColumns =
    {
        new("id", "药材ID", ExportKind.Text),
        new("name", "药材名称", ExportKind.Text),
        new("pinYinCode", "拼音码", ExportKind.Text),
        new("category", "分类", ExportKind.Text),
        new("origin", "产地", ExportKind.Text),
        new("spec", "规格", ExportKind.Text),
        new("unit", "单位", ExportKind.Text),
        new("price", "单价", ExportKind.Decimal),
        new("status", "状态", ExportKind.Status),
        new("createdAt", "创建时间", ExportKind.DateTime),
    };

    /// <summary>
    /// 生成导入模板（.xlsx）——数据表表头为稳定中文列名（保证模板可被本服务回读）；
    /// 「填写说明」表的字段说明/必填取自服务端 JSON 模板（SSOT——避免前端复制字段说明导致漂移）。
    /// 数据表仅含表头（示例值放在「填写说明」表，避免用户忘记删除示例行被当数据导入）。
    /// </summary>
    /// <param name="serverTemplateJson">服务端 import-template 响应体（ApiResponse 信封，camelCase）</param>
    /// <returns>.xlsx 字节</returns>
    public byte[] GenerateTemplate(byte[] serverTemplateJson)
    {
        ArgumentNullException.ThrowIfNull(serverTemplateJson);

        var fields = ReadTemplateFields(serverTemplateJson);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(DataSheetName);

        for (var i = 0; i < ImportColumns.Length; i++)
        {
            var spec = ImportColumns[i];
            var required = spec.Required || FindField(fields, spec.Field)?.Required == true;
            WriteHeaderCell(sheet.Cell(1, i + 1), required ? $"{spec.Header}（必填）" : spec.Header);
            sheet.Column(i + 1).Width = Math.Max(12, spec.Header.Length * 2 + 6);
        }

        sheet.SheetView.FreezeRows(1);

        var guide = workbook.Worksheets.Add(GuideSheetName);
        WriteHeaderCell(guide.Cell(1, 1), "字段");
        WriteHeaderCell(guide.Cell(1, 2), "是否必填");
        WriteHeaderCell(guide.Cell(1, 3), "说明");
        WriteHeaderCell(guide.Cell(1, 4), "示例");
        guide.Column(1).Width = 14;
        guide.Column(2).Width = 10;
        guide.Column(3).Width = 40;
        guide.Column(4).Width = 22;

        for (var i = 0; i < ImportColumns.Length; i++)
        {
            var spec = ImportColumns[i];
            var field = FindField(fields, spec.Field);
            var row = i + 2;
            guide.Cell(row, 1).Value = spec.Header;
            guide.Cell(row, 2).Value = spec.Required || field?.Required == true ? "是" : "否";
            guide.Cell(row, 3).Value = field?.Description ?? string.Empty;
            guide.Cell(row, 4).Value = spec.Example;
        }

        var noteRow = ImportColumns.Length + 3;
        guide.Cell(noteRow, 1).Value = "提示";
        guide.Cell(noteRow, 3).Value =
            "请勿修改「药材数据」表表头；重复数据处理策略默认为「跳过」；单价/成本价请填数字（如 10.50）；单位可空（默认 克）";

        return Save(workbook);
    }

    /// <summary>
    /// 解析导入文件（.xlsx，首个工作表）→ 批量导入 DTO。
    /// 表头容忍「（必填）」后缀与空格；无法识别的列忽略；全空行跳过；重复策略保持 DTO 默认（由调用方设置）；
    /// 单元格值非法（单价/成本价非数字）抛 <see cref="InvalidDataException"/>（消息含行号与列名）。
    /// </summary>
    /// <param name="stream">.xlsx 文件流</param>
    /// <exception cref="InvalidDataException">文件结构或单元格格式非法（消息含行号/列名）</exception>
    public HerbBatchImportInputDto ParseImportFile(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var workbook = OpenWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("Excel 文件中没有工作表");

        var headerRow = sheet.FirstRowUsed()?.RowNumber()
            ?? throw new InvalidDataException("Excel 文件为空——请使用下载的导入模板填写");
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastColumn == 0)
            throw new InvalidDataException("Excel 文件为空——请使用下载的导入模板填写");

        // 表头 → 列索引映射（忽略未知列；识别不到任何已知列视为用错文件）
        var columnMap = new List<(ColumnSpec Spec, int Column)>();
        for (var column = 1; column <= lastColumn; column++)
        {
            var header = NormalizeHeader(sheet.Cell(headerRow, column).GetString());
            if (header.Length == 0)
                continue;

            var spec = ImportColumns.FirstOrDefault(s =>
                string.Equals(NormalizeHeader(s.Header), header, StringComparison.Ordinal)
                || string.Equals(s.Field, header, StringComparison.OrdinalIgnoreCase));
            if (spec != null)
                columnMap.Add((spec, column));
        }

        if (columnMap.Count == 0)
            throw new InvalidDataException("未识别到药材字段列——请使用下载的导入模板填写");

        var request = new HerbBatchImportInputDto();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            // 全空行跳过（模板尾部空行/分隔行）
            if (columnMap.All(m => sheet.Cell(row, m.Column).IsEmpty()))
                continue;

            request.Herbs.Add(ReadHerbRow(sheet, row, columnMap));
        }

        return request;
    }

    /// <summary>
    /// 生成导出文件（.xlsx）——消费服务端 export 的 JSON 数组（ApiResponse 信封，camelCase）。
    /// </summary>
    /// <param name="serverExportJson">服务端 export 响应体</param>
    /// <returns>.xlsx 字节</returns>
    public byte[] GenerateExportFile(byte[] serverExportJson)
    {
        ArgumentNullException.ThrowIfNull(serverExportJson);

        var rows = ReadExportRows(serverExportJson);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(DataSheetName);

        for (var i = 0; i < ExportColumns.Length; i++)
        {
            WriteHeaderCell(sheet.Cell(1, i + 1), ExportColumns[i].Header);
            sheet.Column(i + 1).Width = Math.Max(12, ExportColumns[i].Header.Length * 2 + 6);
        }

        sheet.SheetView.FreezeRows(1);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            for (var c = 0; c < ExportColumns.Length; c++)
            {
                WriteExportCell(sheet.Cell(i + 2, c + 1), ExportColumns[c], row.GetPropertyText(ExportColumns[c].Property));
            }
        }

        return Save(workbook);
    }

    #region 单元格写入

    private static void WriteHeaderCell(IXLCell cell, string text)
    {
        cell.Value = text;
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFEFEF");
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void WriteExportCell(IXLCell cell, ExportColumnSpec spec, string text)
    {
        if (text.Length == 0)
            return;

        switch (spec.Kind)
        {
            case ExportKind.Decimal:
                if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    cell.Value = number;
                break;
            case ExportKind.DateTime:
                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
                {
                    cell.Value = timestamp;
                    cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                }
                break;
            case ExportKind.Status:
                cell.Value = ToStatusLabel(text);
                break;
            default:
                cell.Value = text;
                break;
        }
    }

    /// <summary>状态标签（导出面向人工阅读——枚举字符串转中文，对齐 Catalog 模块「启用/禁用」口径）</summary>
    private static string ToStatusLabel(string value)
        => value.ToLowerInvariant() switch
        {
            "enabled" => "启用",
            "disabled" => "禁用",
            _ => value,
        };

    #endregion

    #region 行解析

    private static HerbInputDto ReadHerbRow(
        IXLWorksheet sheet,
        int row,
        List<(ColumnSpec Spec, int Column)> columnMap)
    {
        var herb = new HerbInputDto();
        foreach (var (spec, column) in columnMap)
        {
            var cell = sheet.Cell(row, column);
            switch (spec.Field)
            {
                case "Name":
                    herb.Name = cell.GetString().Trim();
                    break;
                case "PinYinCode":
                    herb.PinYinCode = NullIfBlank(cell.GetString());
                    break;
                case "Category":
                    herb.Category = NullIfBlank(cell.GetString());
                    break;
                case "Properties":
                    herb.Properties = NullIfBlank(cell.GetString());
                    break;
                case "Origin":
                    herb.Origin = NullIfBlank(cell.GetString());
                    break;
                case "Spec":
                    herb.Spec = NullIfBlank(cell.GetString());
                    break;
                case "Unit":
                    // 空值保留 DTO 默认「克」（对齐服务端模板「单位（默认 克）」）
                    var unit = cell.GetString().Trim();
                    if (unit.Length > 0)
                        herb.Unit = unit;
                    break;
                case "Price":
                    herb.Price = ParseDecimal(cell, row, spec) ?? 0m;
                    break;
                case "CostPrice":
                    herb.CostPrice = ParseDecimal(cell, row, spec);
                    break;
            }
        }

        return herb;
    }

    private static decimal? ParseDecimal(IXLCell cell, int row, ColumnSpec spec)
    {
        if (cell.IsEmpty())
            return null;

        if (cell.TryGetValue(out decimal number))
            return number;

        var text = cell.GetString().Trim();
        if (text.Length == 0)
            return null;

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw new InvalidDataException(
            $"第 {row} 行「{spec.Header}」格式无效：{text}（请填数字，如 10.50）");
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>表头归一化：去空白 + 去「（必填）」/「(必填)」/「*」——容忍用户不删标记</summary>
    private static string NormalizeHeader(string header)
        => header
            .Replace("（必填）", string.Empty, StringComparison.Ordinal)
            .Replace("(必填)", string.Empty, StringComparison.Ordinal)
            .Replace("*", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();

    #endregion

    #region JSON 读取

    /// <summary>解析 ApiResponse 信封，返回 data 元素（camelCase）</summary>
    private static JsonElement ReadEnvelopeData(byte[] responseBytes, string operationName)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(responseBytes);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"服务端返回内容不是合法 JSON（{operationName}）", ex);
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("data", out var data))
                throw new InvalidDataException($"服务端返回缺少 data 字段（{operationName}）");

            return data.Clone();
        }
    }

    private static List<TemplateField> ReadTemplateFields(byte[] serverTemplateJson)
    {
        var data = ReadEnvelopeData(serverTemplateJson, "导入模板");
        if (!data.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
            return new List<TemplateField>();

        var result = new List<TemplateField>();
        foreach (var field in fields.EnumerateArray())
        {
            result.Add(new TemplateField(
                field.GetPropertyText("field"),
                field.GetPropertyText("description"),
                field.TryGetProperty("required", out var required)
                    && required.ValueKind == JsonValueKind.True));
        }

        return result;
    }

    private static List<JsonElement> ReadExportRows(byte[] serverExportJson)
    {
        var data = ReadEnvelopeData(serverExportJson, "导出");
        if (data.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("服务端导出返回的 data 不是数组");

        return data.EnumerateArray().ToList();
    }

    private static TemplateField? FindField(List<TemplateField> fields, string fieldName)
        => fields.FirstOrDefault(f => string.Equals(f.Field, fieldName, StringComparison.OrdinalIgnoreCase));

    #endregion

    #region 基础设施

    private static XLWorkbook OpenWorkbook(Stream stream)
    {
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("无法读取 Excel 文件（请使用 .xlsx 格式并确认文件未损坏）", ex);
        }
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var buffer = new MemoryStream();
        workbook.SaveAs(buffer);
        return buffer.ToArray();
    }

    #endregion

    /// <summary>导入列定义（Field = 服务端模板字段名 / camelCase 属性名）</summary>
    private sealed record ColumnSpec(string Field, string Header, bool Required)
    {
        /// <summary>「填写说明」表示例值</summary>
        public string Example => Field switch
        {
            "Name" => "人参",
            "PinYinCode" => "renshen",
            "Category" => "补益药",
            "Properties" => "甘微苦温",
            "Origin" => "吉林",
            "Spec" => "一等",
            "Unit" => "克",
            "Price" => "10.50",
            "CostPrice" => "5.00",
            _ => string.Empty,
        };
    }

    /// <summary>导出列定义</summary>
    private sealed record ExportColumnSpec(string Property, string Header, ExportKind Kind);

    private enum ExportKind
    {
        Text,
        Decimal,
        DateTime,
        Status,
    }

    private sealed record TemplateField(string Field, string Description, bool Required);
}

/// <summary>JsonElement 读取扩展（缺失/空值返回空串）</summary>
internal static class JsonElementCatalogExtensions
{
    /// <summary>读取字符串属性（camelCase；数字/布尔转文本，缺失返回空串）</summary>
    public static string GetPropertyText(this JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out var value))
            return string.Empty;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty,
        };
    }
}
