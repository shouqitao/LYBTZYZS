using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace LYBT.Infrastructure.Excel;

/// <summary>
/// Excel 导出工具（T4 P0#4: NPOI 2.7.2 生成 .xlsx——此前 NPOI 被引用但零使用，
/// 患者/药材/验方 import-template/export/export-all 端点双端缺失，桌面客户端调用 404）
/// </summary>
public static class ExcelExportHelper
{
    /// <summary>
    /// 生成 .xlsx 工作簿（表头 + 数据行，可选示例行用于模板）
    /// </summary>
    public static byte[] CreateWorkbook(string sheetName, string[] headers, IEnumerable<string[]> rows)
    {
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);

        // 表头
        var headerRow = sheet.CreateRow(0);
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(headers[i]);
            cell.CellStyle = CreateHeaderStyle(workbook);
        }

        // 数据行
        var rowIndex = 1;
        foreach (var row in rows)
        {
            var dataRow = sheet.CreateRow(rowIndex++);
            for (var i = 0; i < row.Length && i < headers.Length; i++)
            {
                dataRow.CreateCell(i).SetCellValue(row[i] ?? string.Empty);
            }
        }

        // 自动列宽
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.AutoSizeColumn(i);
        }

        using var ms = new MemoryStream();
        workbook.Write(ms);
        return ms.ToArray();
    }

    private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
    {
        var style = workbook.CreateCellStyle();
        var font = workbook.CreateFont();
        font.IsBold = true;
        style.SetFont(font);
        return style;
    }
}
