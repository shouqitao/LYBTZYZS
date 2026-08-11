using NPOI.SS.UserModel;

namespace LYBT.Infrastructure.Excel;

/// <summary>
/// Excel 解析工具（B2 US-HERB-006: 服务端 .xlsx 解析路径——NPOI WorkbookFactory 读取）
/// </summary>
public static class ExcelImportHelper
{
    private static readonly NPOI.SS.UserModel.DataFormatter Formatter = new();

    /// <summary>
    /// 解析 .xlsx 工作簿为行数据（每行 string[]，跳过表头行可选；空行跳过）
    /// </summary>
    public static List<string[]> ParseWorkbook(Stream stream, int maxRows, bool skipHeader = true, int maxColumns = 16)
    {
        using var workbook = WorkbookFactory.Create(stream);
        var sheet = workbook.GetSheetAt(0);
        if (sheet == null)
            return new List<string[]>();

        var rows = new List<string[]>();
        var startRow = skipHeader ? 1 : 0;

        for (var r = startRow; r <= sheet.LastRowNum && rows.Count < maxRows; r++)
        {
            var row = sheet.GetRow(r);
            if (row == null)
                continue;

            var cells = new string[maxColumns];
            var hasValue = false;
            for (var c = 0; c < maxColumns; c++)
            {
                var value = Formatter.FormatCellValue(row.GetCell(c)).Trim();
                cells[c] = value;
                if (!string.IsNullOrWhiteSpace(value))
                    hasValue = true;
            }

            if (hasValue)
                rows.Add(cells);
        }

        return rows;
    }
}
