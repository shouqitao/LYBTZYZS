using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace LYBT.WebAPI.Services;

/// <summary>
/// Excel 通用工具服务（NPOI XSSFWorkbook，.xlsx 格式）— 提供数据导出、模板生成与文件解析。
/// </summary>
public class ExcelService
{
    /// <summary>xlsx MIME 类型</summary>
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// 将数据导出为 Excel 文件字节流。columnMapping：表头 → 取值委托。
    /// </summary>
    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName, Dictionary<string, Func<T, object?>> columnMapping)
    {
        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);
        var headerStyle = CreateHeaderStyle(workbook);

        var headers = columnMapping.Keys.ToList();
        var headerRow = sheet.CreateRow(0);
        for (var i = 0; i < headers.Count; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(headers[i]);
            cell.CellStyle = headerStyle;
        }

        var rowIndex = 1;
        foreach (var item in data)
        {
            var row = sheet.CreateRow(rowIndex++);
            var colIndex = 0;
            foreach (var selector in columnMapping.Values)
                SetCellValue(row.CreateCell(colIndex++), selector(item));
        }

        AutoSizeColumns(sheet, headers.Count);

        using var ms = new MemoryStream();
        workbook.Write(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// 生成导入模板：第一行为表头，第二行为示例数据。columnHeaders：表头 → 示例值。
    /// </summary>
    public byte[] GenerateTemplate(string sheetName, Dictionary<string, string> columnHeaders)
    {
        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);
        var headerStyle = CreateHeaderStyle(workbook);

        var headers = columnHeaders.Keys.ToList();
        var headerRow = sheet.CreateRow(0);
        for (var i = 0; i < headers.Count; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(headers[i]);
            cell.CellStyle = headerStyle;
        }

        var sampleRow = sheet.CreateRow(1);
        for (var i = 0; i < headers.Count; i++)
            sampleRow.CreateCell(i).SetCellValue(columnHeaders[headers[i]] ?? string.Empty);

        AutoSizeColumns(sheet, headers.Count);

        using var ms = new MemoryStream();
        workbook.Write(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// 解析 Excel 文件：第一行为表头（匹配 propertySetters 的 key），第二行起为数据，跳过空行。
    /// </summary>
    public List<T> ParseExcel<T>(Stream excelStream, Dictionary<string, Action<T, string>> propertySetters) where T : new()
    {
        var result = new List<T>();
        using var workbook = new XSSFWorkbook(excelStream);
        var sheet = workbook.GetSheetAt(0);
        if (sheet == null) return result;

        var headerRow = sheet.GetRow(sheet.FirstRowNum);
        if (headerRow == null) return result;

        var columnIndexByHeader = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = headerRow.FirstCellNum; col < headerRow.LastCellNum; col++)
        {
            var header = headerRow.GetCell(col)?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(header))
                columnIndexByHeader[header] = col;
        }

        for (var row = sheet.FirstRowNum + 1; row <= sheet.LastRowNum; row++)
        {
            var dataRow = sheet.GetRow(row);
            if (dataRow == null || IsRowEmpty(dataRow)) continue;

            var item = new T();
            foreach (var (header, setter) in propertySetters)
            {
                if (!columnIndexByHeader.TryGetValue(header, out var col)) continue;
                setter(item, GetCellString(dataRow.GetCell(col)));
            }
            result.Add(item);
        }

        return result;
    }

    private static ICellStyle CreateHeaderStyle(XSSFWorkbook workbook)
    {
        var style = workbook.CreateCellStyle();
        var font = workbook.CreateFont();
        font.IsBold = true;
        style.SetFont(font);
        style.FillForegroundColor = IndexedColors.Grey25Percent.Index;
        style.FillPattern = FillPattern.SolidForeground;
        return style;
    }

    private static void SetCellValue(ICell cell, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case DateTime date:
                // 以文本形式写入日期，避免 XSSF 格式表污染导致其他数字列被误判为日期
                cell.SetCellValue(date.ToString("yyyy-MM-dd"));
                break;
            case bool b:
                cell.SetCellValue(b);
                break;
            case decimal d:
                cell.SetCellValue((double)d);
                break;
            case double dbl:
                cell.SetCellValue(dbl);
                break;
            case int i:
                cell.SetCellValue(i);
                break;
            case long l:
                cell.SetCellValue(l);
                break;
            case float f:
                cell.SetCellValue(f);
                break;
            case Enum e:
                cell.SetCellValue(e.ToString());
                break;
            default:
                cell.SetCellValue(value.ToString() ?? string.Empty);
                break;
        }
    }

    private static string GetCellString(ICell? cell)
    {
        if (cell == null) return string.Empty;
        if (cell.CellType == CellType.Numeric)
        {
            // 仅当显式设置了非 General 格式（如手动填写的日期单元格）才按日期读取，避免普通数字被误判
            var style = cell.CellStyle;
            if (style != null && style.DataFormat != 0 && DateUtil.IsCellDateFormatted(cell))
                return cell.DateCellValue?.ToString("yyyy-MM-dd") ?? string.Empty;
            return NumberToTextConverter.ToText(cell.NumericCellValue);
        }
        return cell.ToString()?.Trim() ?? string.Empty;
    }

    private static bool IsRowEmpty(IRow row)
    {
        for (var col = row.FirstCellNum; col < row.LastCellNum; col++)
        {
            if (row.GetCell(col) != null && !string.IsNullOrWhiteSpace(GetCellString(row.GetCell(col))))
                return false;
        }
        return true;
    }

    private static void AutoSizeColumns(ISheet sheet, int columnCount)
    {
        for (var i = 0; i < columnCount; i++)
            sheet.AutoSizeColumn(i);
    }
}
