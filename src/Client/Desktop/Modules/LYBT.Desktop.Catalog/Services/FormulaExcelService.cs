using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Catalog.Services;

/// <summary>
/// 验方 Excel 导入导出服务（US-SHELL-021 AC①：三类标准 Excel 模板对齐；与 <c>HerbExcelService</c> 同构）。
/// <para>职责边界：只做 Excel ↔ 契约转换——模板列以服务端 <c>GET /api/v1/formulas/import-template</c> 的
/// JSON 字段说明为 SSOT；导入产出 <see cref="FormulaBatchImportInputDto"/> 供既有 batch-import 使用；
/// 导出消费服务端 <c>GET /api/v1/formulas/export</c> 的 JSON 数组（含药材明细）。</para>
/// <para>验方模板额外含**药材明细列**（<c>药材N名称/药材N剂量/药材N单位</c>，N = 1..<see cref="HerbGroupCount"/>），
/// 解析时按序组装为 <see cref="FormulaImportItemDto.Herbs"/>（单位可空，默认 g）。</para>
/// <para>文件格式错误抛 <see cref="InvalidDataException"/>（消息含行号/列名——UI 直接展示）。</para>
/// </summary>
public class FormulaExcelService : IFormulaExcelService
{
    /// <summary>验方数据工作表名（模板与导出共用）</summary>
    public const string DataSheetName = "验方数据";

    /// <summary>填写说明工作表名（仅模板）</summary>
    public const string GuideSheetName = "填写说明";

    /// <summary>模板中每行可填写的药材明细列组数（药材1..药材N）</summary>
    private const int HerbGroupCount = 10;

    /// <summary>基础导入列（顺序 = 服务端模板字段顺序；Field = 服务端模板字段名 / camelCase 属性名）</summary>
    private static readonly ColumnSpec[] ImportColumns =
    {
        new("Name", "验方名称", Required: true),
        new("Category", "分类", Required: false),
        new("Effect", "功效", Required: false),
        new("Usage", "用法", Required: false),
    };

    /// <summary>基础导出列（Property = 服务端导出 JSON 的 FormulaDetailDto camelCase 字段）</summary>
    private static readonly ExportColumnSpec[] ExportColumns =
    {
        new("id", "验方ID", ExportKind.Text),
        new("name", "验方名称", ExportKind.Text),
        new("category", "分类", ExportKind.Text),
        new("effect", "功效", ExportKind.Text),
        new("usage", "用法", ExportKind.Text),
        new("property", "性味归经", ExportKind.Text),
        new("indication", "主治", ExportKind.Text),
        new("herbCount", "药材数", ExportKind.Integer),
        new("totalPrice", "总价", ExportKind.Decimal),
        new("status", "状态", ExportKind.Status),
        new("validationStatus", "校验状态", ExportKind.ValidationStatus),
        new("createdAt", "创建时间", ExportKind.DateTime),
    };

    /// <summary>药材明细列组表头（药材N名称/药材N剂量/药材N单位）</summary>
    private static readonly Regex HerbGroupHeader = new(@"^药材(\d+)(名称|剂量|单位)$", RegexOptions.Compiled);

    /// <summary>
    /// 生成导入模板（.xlsx）——数据表表头为稳定中文列名（保证模板可被本服务回读）；
    /// 「填写说明」表的字段说明/必填取自服务端 JSON 模板（SSOT），药材明细列在说明表逐列列出。
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

        // 基础列
        var column = 1;
        for (var i = 0; i < ImportColumns.Length; i++)
        {
            var spec = ImportColumns[i];
            var required = spec.Required || FindField(fields, spec.Field)?.Required == true;
            column = WriteHeader(sheet, column, required ? $"{spec.Header}（必填）" : spec.Header);
        }

        // 药材明细列（第 1 味必填——服务端模板 Herbs 为必填）
        var herbsRequired = FindField(fields, "Herbs")?.Required == true;
        for (var group = 1; group <= HerbGroupCount; group++)
        {
            var nameHeader = $"药材{group}名称";
            column = WriteHeader(sheet, column, group == 1 && herbsRequired ? $"{nameHeader}（必填）" : nameHeader);
            column = WriteHeader(sheet, column, $"药材{group}剂量");
            column = WriteHeader(sheet, column, $"药材{group}单位");
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

        var guideRow = 2;
        for (var i = 0; i < ImportColumns.Length; i++)
        {
            var spec = ImportColumns[i];
            var field = FindField(fields, spec.Field);
            guide.Cell(guideRow, 1).Value = spec.Header;
            guide.Cell(guideRow, 2).Value = spec.Required || field?.Required == true ? "是" : "否";
            guide.Cell(guideRow, 3).Value = field?.Description ?? string.Empty;
            guide.Cell(guideRow, 4).Value = spec.Example;
            guideRow++;
        }

        // 药材明细列说明（SSOT：服务端 Herbs 字段说明落在第 1 味药材行）
        var herbsDescription = FindField(fields, "Herbs")?.Description;
        for (var group = 1; group <= HerbGroupCount; group++)
        {
            var isFirst = group == 1;
            guide.Cell(guideRow, 1).Value = $"药材{group}名称";
            guide.Cell(guideRow, 2).Value = isFirst && herbsRequired ? "是" : "否";
            guide.Cell(guideRow, 3).Value = isFirst
                ? herbsDescription ?? "药材组成（每行可填多味药材）"
                : $"第 {group} 味药材名称";
            guide.Cell(guideRow, 4).Value = isFirst ? "人参" : string.Empty;
            guideRow++;

            guide.Cell(guideRow, 1).Value = $"药材{group}剂量";
            guide.Cell(guideRow, 2).Value = "否";
            guide.Cell(guideRow, 3).Value = $"第 {group} 味药材剂量（1-500 的整数）";
            guide.Cell(guideRow, 4).Value = isFirst ? "10" : string.Empty;
            guideRow++;

            guide.Cell(guideRow, 1).Value = $"药材{group}单位";
            guide.Cell(guideRow, 2).Value = "否";
            guide.Cell(guideRow, 3).Value = $"第 {group} 味药材单位（默认 g）";
            guide.Cell(guideRow, 4).Value = isFirst ? "g" : string.Empty;
            guideRow++;
        }

        guide.Cell(guideRow + 1, 1).Value = "提示";
        guide.Cell(guideRow + 1, 3).Value =
            "请勿修改「验方数据」表表头；每行一味验方，可填多味药材（药材1名称/药材1剂量/药材1单位、药材2名称/…）；剂量为 1-500 的整数；单位可空（默认 g）";

        return Save(workbook);
    }

    /// <summary>
    /// 解析导入文件（.xlsx，首个工作表）→ 批量导入 DTO。
    /// 表头容忍「（必填）」后缀与空格；无法识别的列忽略；全空行跳过；每行按药材明细列顺序组装 Herbs；
    /// 单元格值非法（剂量非 1-500 整数、有剂量却缺药材名）抛 <see cref="InvalidDataException"/>（消息含行号与列名）。
    /// </summary>
    /// <param name="stream">.xlsx 文件流</param>
    /// <exception cref="InvalidDataException">文件结构或单元格格式非法（消息含行号/列名）</exception>
    public FormulaBatchImportInputDto ParseImportFile(Stream stream)
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

        // 表头 → 列索引映射（基础列 + 药材明细列组；忽略未知列）
        var columnMap = new List<(ColumnSpec Spec, int Column)>();
        var groupColumns = new Dictionary<int, int[]>();
        for (var column = 1; column <= lastColumn; column++)
        {
            var header = NormalizeHeader(sheet.Cell(headerRow, column).GetString());
            if (header.Length == 0)
                continue;

            var groupMatch = HerbGroupHeader.Match(header);
            if (groupMatch.Success)
            {
                var index = int.Parse(groupMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var slot = groupMatch.Groups[2].Value switch
                {
                    "名称" => 0,
                    "剂量" => 1,
                    _ => 2,
                };
                if (!groupColumns.TryGetValue(index, out var slots))
                {
                    slots = new int[3];
                    groupColumns[index] = slots;
                }

                slots[slot] = column;
                continue;
            }

            var spec = ImportColumns.FirstOrDefault(s =>
                string.Equals(NormalizeHeader(s.Header), header, StringComparison.Ordinal)
                || string.Equals(s.Field, header, StringComparison.OrdinalIgnoreCase));
            if (spec != null)
                columnMap.Add((spec, column));
        }

        if (columnMap.Count == 0)
            throw new InvalidDataException("未识别到验方字段列——请使用下载的导入模板填写");

        var groups = groupColumns.OrderBy(g => g.Key).ToList();
        var request = new FormulaBatchImportInputDto();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            // 全空行跳过（模板尾部空行/分隔行）
            if (columnMap.All(m => sheet.Cell(row, m.Column).IsEmpty())
                && groups.All(g => g.Value.All(c => c == 0 || sheet.Cell(row, c).IsEmpty())))
                continue;

            request.Formulas.Add(ReadFormulaRow(sheet, row, columnMap, groups));
        }

        return request;
    }

    /// <summary>
    /// 生成导出文件（.xlsx）——消费服务端 export 的 JSON 数组（ApiResponse 信封，camelCase，含药材明细）。
    /// 药材明细列数按数据中最多的药材味数展开。
    /// </summary>
    /// <param name="serverExportJson">服务端 export 响应体</param>
    /// <returns>.xlsx 字节</returns>
    public byte[] GenerateExportFile(byte[] serverExportJson)
    {
        ArgumentNullException.ThrowIfNull(serverExportJson);

        var rows = ReadExportRows(serverExportJson);
        var herbGroupCount = rows.Count == 0 ? 0 : rows.Max(r => ReadHerbItems(r).Count);

        var columns = new List<ExportColumnSpec>(ExportColumns);
        for (var group = 1; group <= herbGroupCount; group++)
        {
            columns.Add(new ExportColumnSpec($"herb:{group - 1}:herbName", $"药材{group}名称", ExportKind.Text));
            columns.Add(new ExportColumnSpec($"herb:{group - 1}:dosage", $"药材{group}剂量", ExportKind.Integer));
            columns.Add(new ExportColumnSpec($"herb:{group - 1}:unit", $"药材{group}单位", ExportKind.Text));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(DataSheetName);

        for (var i = 0; i < columns.Count; i++)
        {
            WriteHeaderCell(sheet.Cell(1, i + 1), columns[i].Header);
            sheet.Column(i + 1).Width = Math.Max(12, columns[i].Header.Length * 2 + 6);
        }

        sheet.SheetView.FreezeRows(1);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var herbs = ReadHerbItems(row);
            for (var c = 0; c < columns.Count; c++)
            {
                WriteExportCell(sheet.Cell(i + 2, c + 1), columns[c], ReadExportValue(row, herbs, columns[c].Property));
            }
        }

        return Save(workbook);
    }

    #region 单元格写入

    /// <summary>写表头并返回下一个可用列号</summary>
    private static int WriteHeader(IXLWorksheet sheet, int column, string text)
    {
        WriteHeaderCell(sheet.Cell(1, column), text);
        sheet.Column(column).Width = Math.Max(12, text.Length * 2 + 6);
        return column + 1;
    }

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
            case ExportKind.Integer:
                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                    cell.Value = integer;
                break;
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
            case ExportKind.ValidationStatus:
                cell.Value = string.Equals(text, "validated", StringComparison.OrdinalIgnoreCase) ? "已验证" : "待校验";
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

    private static FormulaImportItemDto ReadFormulaRow(
        IXLWorksheet sheet,
        int row,
        List<(ColumnSpec Spec, int Column)> columnMap,
        List<KeyValuePair<int, int[]>> groups)
    {
        var formula = new FormulaImportItemDto();
        foreach (var (spec, column) in columnMap)
        {
            var cell = sheet.Cell(row, column);
            switch (spec.Field)
            {
                case "Name":
                    formula.Name = cell.GetString().Trim();
                    break;
                case "Category":
                    formula.Category = NullIfBlank(cell.GetString());
                    break;
                case "Effect":
                    formula.Effect = NullIfBlank(cell.GetString());
                    break;
                case "Usage":
                    formula.Usage = NullIfBlank(cell.GetString());
                    break;
            }
        }

        foreach (var group in groups)
        {
            var herb = ReadHerbGroup(sheet, row, group.Key, group.Value);
            if (herb != null)
                formula.Herbs.Add(herb);
        }

        return formula;
    }

    private static FormulaHerbImportItemDto? ReadHerbGroup(IXLWorksheet sheet, int row, int groupIndex, int[] columns)
    {
        var name = columns[0] > 0 ? sheet.Cell(row, columns[0]).GetString().Trim() : string.Empty;
        var dosageText = columns[1] > 0 ? sheet.Cell(row, columns[1]).GetString().Trim() : string.Empty;
        var unit = columns[2] > 0 ? sheet.Cell(row, columns[2]).GetString().Trim() : string.Empty;

        if (name.Length == 0)
        {
            if (dosageText.Length == 0 && unit.Length == 0)
                return null;

            throw new InvalidDataException(
                $"第 {row} 行「药材{groupIndex}名称」不能为空（已填写剂量/单位）");
        }

        return new FormulaHerbImportItemDto
        {
            HerbName = name,
            Dosage = ParseDosage(dosageText, row, groupIndex),
            Unit = unit.Length > 0 ? unit : "g",
        };
    }

    private static int ParseDosage(string text, int row, int groupIndex)
    {
        if (text.Length == 0)
            throw new InvalidDataException(
                $"第 {row} 行「药材{groupIndex}剂量」不能为空（每味药材需填 1-500 的整数）");

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dosage)
            || dosage < 1
            || dosage > 500)
        {
            throw new InvalidDataException(
                $"第 {row} 行「药材{groupIndex}剂量」格式无效：{text}（请填 1-500 的整数）");
        }

        return dosage;
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

    /// <summary>读取导出行药材明细数组（缺失返回空列表）</summary>
    private static List<JsonElement> ReadHerbItems(JsonElement row)
    {
        if (!row.TryGetProperty("herbs", out var herbs) || herbs.ValueKind != JsonValueKind.Array)
            return new List<JsonElement>();

        return herbs.EnumerateArray().ToList();
    }

    /// <summary>导出行取值：基础列按属性名，药材明细列按 <c>herb:N:property</c> 约定</summary>
    private static string ReadExportValue(JsonElement row, List<JsonElement> herbs, string property)
    {
        if (!property.StartsWith("herb:", StringComparison.Ordinal))
            return row.GetPropertyText(property);

        var parts = property.Split(':');
        var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
        if (index >= herbs.Count)
            return string.Empty;

        return herbs[index].GetPropertyText(parts[2]);
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
            "Name" => "四君子汤",
            "Category" => "补益剂",
            "Effect" => "益气健脾",
            "Usage" => "水煎服",
            _ => string.Empty,
        };
    }

    /// <summary>导出列定义</summary>
    private sealed record ExportColumnSpec(string Property, string Header, ExportKind Kind);

    private enum ExportKind
    {
        Text,
        Integer,
        Decimal,
        DateTime,
        Status,
        ValidationStatus,
    }

    private sealed record TemplateField(string Field, string Description, bool Required);
}
