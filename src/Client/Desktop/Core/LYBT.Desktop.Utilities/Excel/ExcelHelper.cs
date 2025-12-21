using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Reflection;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace LYBT.Desktop.Utilities.Excel;

/// <summary>
/// Excel 导入导出工具类
/// 支持泛型类型映射、Display特性识别、多种数据类型转换
/// </summary>
public static class ExcelHelper
{
    private const int MinColumnWidth = 3000;

    #region 导出方法

    /// <summary>
    /// 导出数据到Excel文件
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">数据列表</param>
    /// <param name="columns">列定义（属性名称, 列标题）</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="sheetName">工作表名称</param>
    public static void ExportToExcel<T>(
        IEnumerable<T> data,
        Dictionary<string, string> columns,
        string filePath,
        string sheetName = "Sheet1")
    {
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);
        var headerStyle = CreateHeaderStyle(workbook);

        // 创建标题行
        var headerRow = sheet.CreateRow(0);
        var columnIndex = 0;
        foreach (var column in columns)
        {
            var cell = headerRow.CreateCell(columnIndex++);
            cell.SetCellValue(column.Value);
            cell.CellStyle = headerStyle;
        }

        // 写入数据行
        var rowIndex = 1;
        foreach (var item in data)
        {
            var dataRow = sheet.CreateRow(rowIndex++);
            columnIndex = 0;

            foreach (var column in columns)
            {
                var cell = dataRow.CreateCell(columnIndex++);
                var property = item?.GetType().GetProperty(column.Key);
                if (property != null && item != null)
                {
                    SetCellValue(cell, property.GetValue(item));
                }
            }
        }

        AutoResizeColumns(sheet, columns.Count);
        SaveWorkbook(workbook, filePath);
    }

    /// <summary>
    /// 异步导出泛型列表到Excel文件（自动识别Display特性）
    /// </summary>
    public static async Task ExportAsync<T>(
        IEnumerable<T> data,
        string filePath,
        string sheetName = "Sheet1") where T : class
    {
        await Task.Run(() =>
        {
            var properties = GetReadableProperties<T>();

            using var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet(sheetName);
            var headerStyle = CreateHeaderStyle(workbook);

            // 创建标题行
            var headerRow = sheet.CreateRow(0);
            for (var i = 0; i < properties.Count; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(GetColumnHeader(properties[i]));
                cell.CellStyle = headerStyle;
            }

            // 写入数据行
            var rowIndex = 1;
            foreach (var item in data)
            {
                var dataRow = sheet.CreateRow(rowIndex++);
                for (var i = 0; i < properties.Count; i++)
                {
                    SetCellValue(dataRow.CreateCell(i), properties[i].GetValue(item));
                }
            }

            AutoResizeColumns(sheet, properties.Count);
            SaveWorkbook(workbook, filePath);
        });
    }

    #endregion

    #region 导入方法

    /// <summary>
    /// 从Excel文件导入数据到DataTable
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="hasHeader">是否有标题行</param>
    /// <returns>数据表</returns>
    public static DataTable ImportFromExcel(string filePath, bool hasHeader = true)
    {
        var dataTable = new DataTable();

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var workbook = new XSSFWorkbook(fs);
        var sheet = workbook.GetSheetAt(0);
        var headerRow = sheet.GetRow(0);
        var cellCount = headerRow.LastCellNum;

        // 创建列
        for (var i = 0; i < cellCount; i++)
        {
            var cell = headerRow.GetCell(i);
            dataTable.Columns.Add(hasHeader && cell != null ? cell.ToString() : $"Column{i + 1}");
        }

        // 读取数据行
        var startRow = hasHeader ? 1 : 0;
        for (var i = startRow; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i);
            if (row == null) continue;

            var dataRow = dataTable.NewRow();
            var hasValue = false;

            for (var j = 0; j < cellCount; j++)
            {
                var cell = row.GetCell(j);
                if (cell != null)
                {
                    dataRow[j] = GetCellValue(cell);
                    if (!string.IsNullOrWhiteSpace(dataRow[j]?.ToString()))
                    {
                        hasValue = true;
                    }
                }
            }

            if (hasValue)
            {
                dataTable.Rows.Add(dataRow);
            }
        }

        return dataTable;
    }

    /// <summary>
    /// 异步解析Excel文件为泛型列表（支持Display特性映射）
    /// </summary>
    public static async Task<List<T>> ParseAsync<T>(Stream stream, bool hasHeader = true) where T : class, new()
    {
        return await Task.Run(() =>
        {
            var result = new List<T>();
            var properties = GetWritableProperties<T>();

            using var workbook = new XSSFWorkbook(stream);
            var sheet = workbook.GetSheetAt(0);
            var columnPropertyMap = BuildColumnPropertyMap(sheet, properties, hasHeader);

            var startRow = hasHeader ? 1 : 0;
            for (var i = startRow; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                var instance = new T();
                var hasValue = false;

                foreach (var (columnIndex, property) in columnPropertyMap)
                {
                    var cell = row.GetCell(columnIndex);
                    if (cell == null) continue;

                    try
                    {
                        var cellValue = GetCellValue(cell);
                        if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                        {
                            hasValue = true;
                            property.SetValue(instance, ConvertToPropertyType(cellValue, property.PropertyType));
                        }
                    }
                    catch
                    {
                        // 忽略类型转换错误
                    }
                }

                if (hasValue)
                {
                    result.Add(instance);
                }
            }

            return result;
        });
    }

    #endregion

    #region 模板方法

    /// <summary>
    /// 创建Excel导入模板
    /// </summary>
    public static void CreateTemplate(
        string[] columns,
        string filePath,
        string sheetName = "Sheet1",
        List<string[]>? sampleData = null)
    {
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);
        var headerStyle = CreateHeaderStyle(workbook);

        // 创建标题行
        var headerRow = sheet.CreateRow(0);
        for (var i = 0; i < columns.Length; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(columns[i]);
            cell.CellStyle = headerStyle;
        }

        // 添加示例数据
        if (sampleData != null)
        {
            var sampleStyle = CreateSampleStyle(workbook);
            for (var i = 0; i < sampleData.Count; i++)
            {
                var dataRow = sheet.CreateRow(i + 1);
                for (var j = 0; j < sampleData[i].Length && j < columns.Length; j++)
                {
                    var cell = dataRow.CreateCell(j);
                    cell.SetCellValue(sampleData[i][j]);
                    cell.CellStyle = sampleStyle;
                }
            }
        }

        AutoResizeColumns(sheet, columns.Length);
        SaveWorkbook(workbook, filePath);
    }

    /// <summary>
    /// 异步生成泛型模板（自动识别Display特性）
    /// </summary>
    public static async Task GenerateTemplateAsync<T>(
        string filePath,
        string sheetName = "Sheet1",
        IEnumerable<T>? sampleData = null) where T : class
    {
        await Task.Run(() =>
        {
            var properties = GetWritableProperties<T>();

            using var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet(sheetName);
            var headerStyle = CreateHeaderStyle(workbook);

            // 创建标题行
            var headerRow = sheet.CreateRow(0);
            for (var i = 0; i < properties.Count; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(GetColumnHeader(properties[i]));
                cell.CellStyle = headerStyle;
            }

            // 添加示例数据
            if (sampleData != null)
            {
                var sampleStyle = CreateSampleStyle(workbook);
                var rowIndex = 1;
                foreach (var item in sampleData)
                {
                    var dataRow = sheet.CreateRow(rowIndex++);
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var cell = dataRow.CreateCell(i);
                        SetCellValue(cell, properties[i].GetValue(item));
                        cell.CellStyle = sampleStyle;
                    }
                }
            }

            AutoResizeColumns(sheet, properties.Count);
            SaveWorkbook(workbook, filePath);
        });
    }

    #endregion

    #region 私有辅助方法 - 样式

    private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
    {
        var style = workbook.CreateCellStyle();
        var font = workbook.CreateFont();
        font.IsBold = true;
        font.FontHeightInPoints = 12;
        style.SetFont(font);
        style.FillForegroundColor = HSSFColor.Grey25Percent.Index;
        style.FillPattern = FillPattern.SolidForeground;
        style.BorderBottom = BorderStyle.Thin;
        style.BorderTop = BorderStyle.Thin;
        style.BorderLeft = BorderStyle.Thin;
        style.BorderRight = BorderStyle.Thin;
        style.Alignment = HorizontalAlignment.Center;
        style.VerticalAlignment = VerticalAlignment.Center;
        return style;
    }

    private static ICellStyle CreateSampleStyle(IWorkbook workbook)
    {
        var style = workbook.CreateCellStyle();
        var font = workbook.CreateFont();
        font.IsItalic = true;
        font.Color = HSSFColor.Grey50Percent.Index;
        style.SetFont(font);
        return style;
    }

    private static void AutoResizeColumns(ISheet sheet, int columnCount)
    {
        for (var i = 0; i < columnCount; i++)
        {
            sheet.AutoSizeColumn(i);
            if (sheet.GetColumnWidth(i) < MinColumnWidth)
            {
                sheet.SetColumnWidth(i, MinColumnWidth);
            }
        }
    }

    private static void SaveWorkbook(IWorkbook workbook, string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        workbook.Write(fs);
    }

    #endregion

    #region 私有辅助方法 - 单元格操作

    private static void SetCellValue(ICell cell, object? value)
    {
        if (value == null)
        {
            cell.SetCellValue(string.Empty);
            return;
        }

        switch (value)
        {
            case string s:
                cell.SetCellValue(s);
                break;
            case DateTime dt:
                cell.SetCellValue(dt.ToString("yyyy-MM-dd HH:mm:ss"));
                break;
            case bool b:
                cell.SetCellValue(b ? "是" : "否");
                break;
            case decimal or double or float:
                cell.SetCellValue(Convert.ToDouble(value));
                break;
            case int or long or short:
                cell.SetCellValue(Convert.ToDouble(value));
                break;
            default:
                cell.SetCellValue(value.ToString() ?? string.Empty);
                break;
        }
    }

    private static object? GetCellValue(ICell cell)
    {
        return cell.CellType switch
        {
            CellType.Numeric => DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue : cell.NumericCellValue,
            CellType.String => cell.StringCellValue,
            CellType.Boolean => cell.BooleanCellValue,
            CellType.Formula => TryGetFormulaCellValue(cell),
            _ => string.Empty
        };
    }

    private static object? TryGetFormulaCellValue(ICell cell)
    {
        try { return cell.NumericCellValue; }
        catch { return cell.StringCellValue; }
    }

    #endregion

    #region 私有辅助方法 - 反射与类型转换

    private static List<PropertyInfo> GetReadableProperties<T>() =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

    private static List<PropertyInfo> GetWritableProperties<T>() =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

    private static string GetColumnHeader(PropertyInfo property)
    {
        var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
        if (!string.IsNullOrEmpty(displayAttr?.Name))
            return displayAttr.Name;

        var displayNameAttr = property.GetCustomAttribute<DisplayNameAttribute>();
        if (!string.IsNullOrEmpty(displayNameAttr?.DisplayName))
            return displayNameAttr.DisplayName;

        return property.Name;
    }

    private static Dictionary<int, PropertyInfo> BuildColumnPropertyMap(
        ISheet sheet,
        List<PropertyInfo> properties,
        bool hasHeader)
    {
        var map = new Dictionary<int, PropertyInfo>();

        if (hasHeader)
        {
            var headerRow = sheet.GetRow(0);
            if (headerRow == null) return map;

            for (var i = 0; i < headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell == null) continue;

                var columnName = cell.ToString() ?? string.Empty;
                var property = properties.FirstOrDefault(p =>
                {
                    var displayAttr = p.GetCustomAttribute<DisplayAttribute>();
                    var displayNameAttr = p.GetCustomAttribute<DisplayNameAttribute>();

                    return (displayAttr?.Name == columnName) ||
                           (displayNameAttr?.DisplayName == columnName) ||
                           p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase);
                });

                if (property != null)
                {
                    map[i] = property;
                }
            }
        }
        else
        {
            for (var i = 0; i < properties.Count; i++)
            {
                map[i] = properties[i];
            }
        }

        return map;
    }

    private static object? ConvertToPropertyType(object value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is string str && string.IsNullOrWhiteSpace(str))
        {
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                ? Activator.CreateInstance(targetType)
                : null;
        }

        if (underlyingType.IsInstanceOfType(value)) return value;
        if (underlyingType.IsEnum) return ConvertToEnum(value, underlyingType);
        if (underlyingType == typeof(DateTime)) return ConvertToDateTime(value);
        if (underlyingType == typeof(bool)) return ConvertToBoolean(value);

        try { return Convert.ChangeType(value, underlyingType); }
        catch { return null; }
    }

    private static object? ConvertToEnum(object value, Type enumType)
    {
        var enumString = value.ToString() ?? string.Empty;
        if (Enum.IsDefined(enumType, enumString)) return Enum.Parse(enumType, enumString);
        if (int.TryParse(enumString, out var enumValue)) return Enum.ToObject(enumType, enumValue);
        return null;
    }

    private static object? ConvertToDateTime(object value) => value switch
    {
        double d => DateTime.FromOADate(d),
        _ when DateTime.TryParse(value.ToString(), out var dt) => dt,
        _ => null
    };

    private static object? ConvertToBoolean(object value)
    {
        var str = value.ToString()?.Trim().ToLower() ?? string.Empty;
        return str switch
        {
            "true" or "是" or "1" => true,
            "false" or "否" or "0" => false,
            _ when bool.TryParse(str, out var result) => result,
            _ => null
        };
    }

    #endregion
}
