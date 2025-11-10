using System.Data;
using System.IO;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace LYBT.Desktop.Infrastructure.Helpers
{

    /// <summary>
    /// Excel 操作帮助类
    /// </summary>
    public static class ExcelHelper
    {
        /// <summary>
        /// Excel列的最小宽度
        /// </summary>
        private const int MIN_COLUMN_WIDTH = 3000;

        /// <summary>
        /// 导出数据到Excel
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据列表</param>
        /// <param name="columns">列定义（属性名称, 列标题）</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="sheetName">工作表名称</param>
        public static void ExportToExcel<T>(IEnumerable<T> data, Dictionary<string, string> columns, string filePath, string sheetName = "Sheet1")
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 创建标题行
            IRow headerRow = sheet.CreateRow(0);
            var headerStyle = CreateHeaderStyle(workbook);

            int columnIndex = 0;
            foreach (var column in columns)
            {
                ICell cell = headerRow.CreateCell(columnIndex);
                cell.SetCellValue(column.Value);
                cell.CellStyle = headerStyle;
                columnIndex++;
            }

            // 写入数据
            int rowIndex = 1;
            foreach (var item in data)
            {
                IRow dataRow = sheet.CreateRow(rowIndex);
                columnIndex = 0;

                foreach (var column in columns)
                {
                    ICell cell = dataRow.CreateCell(columnIndex);
                    var property = item?.GetType().GetProperty(column.Key);
                    if (property != null && item != null)
                    {
                        var value = property.GetValue(item);
                        SetCellValue(cell, value);
                    }

                    columnIndex++;
                }

                rowIndex++;
            }

            // 自动调整列宽
            AutoResizeColumns(sheet, columns.Count);

            // 保存文件
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }

            workbook.Close();
        }

        /// <summary>
        /// 创建Excel模板
        /// </summary>
        /// <param name="columns">列定义（列标题）</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="sampleData">示例数据（可选）</param>
        public static void CreateTemplate(string[] columns, string filePath, string sheetName = "Sheet1", List<string[]>? sampleData = null)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 创建标题行
            IRow headerRow = sheet.CreateRow(0);
            var headerStyle = CreateHeaderStyle(workbook);

            for (int i = 0; i < columns.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(columns[i]);
                cell.CellStyle = headerStyle;
            }

            // 添加示例数据（如果提供）
            if (sampleData != null)
            {
                var sampleStyle = CreateSampleStyle(workbook);
                for (int i = 0; i < sampleData.Count; i++)
                {
                    IRow dataRow = sheet.CreateRow(i + 1);
                    for (int j = 0; j < sampleData[i].Length && j < columns.Length; j++)
                    {
                        ICell cell = dataRow.CreateCell(j);
                        cell.SetCellValue(sampleData[i][j]);
                        cell.CellStyle = sampleStyle;
                    }
                }
            }

            // 自动调整列宽
            AutoResizeColumns(sheet, columns.Length);

            // 保存文件
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }

            workbook.Close();
        }

        /// <summary>
        /// 自动调整列宽
        /// </summary>
        private static void AutoResizeColumns(ISheet sheet, int columnCount)
        {
            for (int i = 0; i < columnCount; i++)
            {
                sheet.AutoSizeColumn(i);
                if (sheet.GetColumnWidth(i) < MIN_COLUMN_WIDTH)
                {
                    sheet.SetColumnWidth(i, MIN_COLUMN_WIDTH);
                }
            }
        }

        /// <summary>
        /// 从Excel导入数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="hasHeader">是否有标题行</param>
        /// <returns>数据表</returns>
        public static DataTable ImportFromExcel(string filePath, bool hasHeader = true)
        {
            DataTable dataTable = new DataTable();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);
                ISheet sheet = workbook.GetSheetAt(0);

                // 获取标题行
                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;

                // 创建列
                for (int i = 0; i < cellCount; i++)
                {
                    ICell cell = headerRow.GetCell(i);
                    if (hasHeader && cell != null)
                    {
                        dataTable.Columns.Add(cell.ToString());
                    }
                    else
                    {
                        dataTable.Columns.Add($"Column{i + 1}");
                    }
                }

                // 读取数据
                int startRow = hasHeader ? 1 : 0;
                for (int i = startRow; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    DataRow dataRow = dataTable.NewRow();
                    bool hasValue = false;

                    for (int j = 0; j < cellCount; j++)
                    {
                        ICell cell = row.GetCell(j);
                        if (cell != null)
                        {
                            dataRow[j] = GetCellValue(cell);
                            if (!string.IsNullOrWhiteSpace(dataRow[j].ToString()))
                            {
                                hasValue = true;
                            }
                        }
                    }

                    if (hasValue) // 只添加非空行
                    {
                        dataTable.Rows.Add(dataRow);
                    }
                }

                workbook.Close();
            }

            return dataTable;
        }

        /// <summary>
        /// 创建标题样式
        /// </summary>
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

        /// <summary>
        /// 创建示例数据样式
        /// </summary>
        private static ICellStyle CreateSampleStyle(IWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.IsItalic = true;
            font.Color = HSSFColor.Grey50Percent.Index;
            style.SetFont(font);
            return style;
        }

        /// <summary>
        /// 设置单元格值
        /// </summary>
        private static void SetCellValue(ICell cell, object? value)
        {
            if (value == null)
            {
                cell.SetCellValue(string.Empty);
                return;
            }

            var type = value.GetType();
            if (type == typeof(string))
            {
                cell.SetCellValue(value.ToString() ?? string.Empty);
            }
            else if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                cell.SetCellValue(((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else if (type == typeof(bool) || type == typeof(bool?))
            {
                cell.SetCellValue(value.ToString() ?? string.Empty);
            }
            else if (type == typeof(decimal) || type == typeof(decimal?) ||
                       type == typeof(double) || type == typeof(double?) ||
                       type == typeof(float) || type == typeof(float?))
            {
                cell.SetCellValue(Convert.ToDouble(value));
            }
            else if (type == typeof(int) || type == typeof(int?) ||
                       type == typeof(long) || type == typeof(long?) ||
                       type == typeof(short) || type == typeof(short?))
            {
                cell.SetCellValue(Convert.ToDouble(value));
            }
            else
            {
                cell.SetCellValue(value.ToString() ?? string.Empty);
            }
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        private static object? GetCellValue(ICell cell)
        {
            if (cell == null)
            {
                return string.Empty;
            }

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue;
                    }

                    return cell.NumericCellValue;

                case CellType.String:
                    return cell.StringCellValue;

                case CellType.Boolean:
                    return cell.BooleanCellValue;

                case CellType.Formula:
                    try
                    {
                        return cell.NumericCellValue;
                    }
                    catch
                    {
                        return cell.StringCellValue;
                    }

                default:
                    return string.Empty;
            }
        }


        #region Issue #2002: 泛型异步方法

        /// <summary>
        /// 异步解析Excel文件为泛型列表
        /// Issue #2002 - Task 2.9: 支持泛型类型反射映射
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="stream">Excel文件流</param>
        /// <param name="hasHeader">是否有标题行</param>
        /// <returns>解析后的数据列表</returns>
        public static async Task<List<T>> ParseAsync<T>(Stream stream, bool hasHeader = true) where T : class, new()
        {
            return await Task.Run(() =>
            {
                var result = new List<T>();
                var type = typeof(T);
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .ToList();

                using (stream)
                {
                    IWorkbook workbook = new XSSFWorkbook(stream);
                    ISheet sheet = workbook.GetSheetAt(0);

                    // 获取标题行（如果存在）
                    IRow? headerRow = hasHeader ? sheet.GetRow(0) : null;
                    Dictionary<int, System.Reflection.PropertyInfo> columnPropertyMap = new Dictionary<int, System.Reflection.PropertyInfo>();

                    if (headerRow != null)
                    {
                        // 建立列索引到属性的映射（基于列名）
                        for (int i = 0; i < headerRow.LastCellNum; i++)
                        {
                            ICell? cell = headerRow.GetCell(i);
                            if (cell != null)
                            {
                                string columnName = cell.ToString() ?? string.Empty;
                                var property = properties.FirstOrDefault(p =>
                                {
                                    // 支持 Display 和 DisplayName 特性
                                    var displayAttr = p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false).FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;
                                    var displayNameAttr = p.GetCustomAttributes(typeof(System.ComponentModel.DisplayNameAttribute), false).FirstOrDefault() as System.ComponentModel.DisplayNameAttribute;

                                    return (displayAttr != null && displayAttr.Name == columnName) ||
                                           (displayNameAttr != null && displayNameAttr.DisplayName == columnName) ||
                                           p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase);
                                });

                                if (property != null)
                                {
                                    columnPropertyMap[i] = property;
                                }
                            }
                        }
                    }
                    else
                    {
                        // 无标题行：按列索引映射属性
                        for (int i = 0; i < properties.Count; i++)
                        {
                            columnPropertyMap[i] = properties[i];
                        }
                    }

                    // 读取数据行
                    int startRow = hasHeader ? 1 : 0;
                    for (int i = startRow; i <= sheet.LastRowNum; i++)
                    {
                        IRow? row = sheet.GetRow(i);
                        if (row == null)
                        {
                            continue;
                        }

                        var instance = new T();
                        bool hasValue = false;

                        foreach (var kvp in columnPropertyMap)
                        {
                            int columnIndex = kvp.Key;
                            var property = kvp.Value;
                            ICell? cell = row.GetCell(columnIndex);

                            if (cell != null)
                            {
                                try
                                {
                                    object? cellValue = GetCellValue(cell);
                                    if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                                    {
                                        hasValue = true;
                                        var convertedValue = ConvertValueToPropertyType(cellValue, property.PropertyType);
                                        property.SetValue(instance, convertedValue);
                                    }
                                }
                                catch
                                {
                                    // 忽略类型转换错误，继续处理下一个单元格
                                }
                            }
                        }

                        if (hasValue)
                        {
                            result.Add(instance);
                        }
                    }

                    workbook.Close();
                }

                return result;
            });
        }

        /// <summary>
        /// 异步导出泛型列表到Excel文件
        /// Issue #2002 - Task 2.9: 支持泛型类型反射映射
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据列表</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="sheetName">工作表名称</param>
        /// <returns>异步任务</returns>
        public static async Task ExportAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1") where T : class
        {
            await Task.Run(() =>
            {
                var type = typeof(T);
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(p => p.CanRead)
                    .ToList();

                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet(sheetName);

                // 创建标题行
                IRow headerRow = sheet.CreateRow(0);
                var headerStyle = CreateHeaderStyle(workbook);

                for (int i = 0; i < properties.Count; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    
                    // 获取列标题（优先使用Display特性）
                    string columnHeader = GetColumnHeader(properties[i]);
                    cell.SetCellValue(columnHeader);
                    cell.CellStyle = headerStyle;
                }

                // 写入数据行
                int rowIndex = 1;
                foreach (var item in data)
                {
                    IRow dataRow = sheet.CreateRow(rowIndex);
                    for (int i = 0; i < properties.Count; i++)
                    {
                        ICell cell = dataRow.CreateCell(i);
                        var value = properties[i].GetValue(item);
                        SetCellValue(cell, value);
                    }
                    rowIndex++;
                }

                // 自动调整列宽
                AutoResizeColumns(sheet, properties.Count);

                // 保存文件
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fs);
                }

                workbook.Close();
            });
        }

        /// <summary>
        /// 异步生成Excel导入模板
        /// Issue #2002 - Task 2.9: 支持泛型类型反射映射
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="sampleData">示例数据（可选）</param>
        /// <returns>异步任务</returns>
        public static async Task GenerateTemplateAsync<T>(string filePath, string sheetName = "Sheet1", IEnumerable<T>? sampleData = null) where T : class
        {
            await Task.Run(() =>
            {
                var type = typeof(T);
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .ToList();

                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet(sheetName);

                // 创建标题行
                IRow headerRow = sheet.CreateRow(0);
                var headerStyle = CreateHeaderStyle(workbook);

                for (int i = 0; i < properties.Count; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    string columnHeader = GetColumnHeader(properties[i]);
                    cell.SetCellValue(columnHeader);
                    cell.CellStyle = headerStyle;
                }

                // 添加示例数据（如果提供）
                if (sampleData != null)
                {
                    var sampleStyle = CreateSampleStyle(workbook);
                    int rowIndex = 1;
                    foreach (var item in sampleData)
                    {
                        IRow dataRow = sheet.CreateRow(rowIndex);
                        for (int i = 0; i < properties.Count; i++)
                        {
                            ICell cell = dataRow.CreateCell(i);
                            var value = properties[i].GetValue(item);
                            SetCellValue(cell, value);
                            cell.CellStyle = sampleStyle;
                        }
                        rowIndex++;
                    }
                }

                // 自动调整列宽
                AutoResizeColumns(sheet, properties.Count);

                // 保存文件
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fs);
                }

                workbook.Close();
            });
        }

        /// <summary>
        /// 获取列标题（支持Display和DisplayName特性）
        /// </summary>
        private static string GetColumnHeader(System.Reflection.PropertyInfo property)
        {
            // 优先使用 Display 特性的 Name
            var displayAttr = property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false).FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;
            if (displayAttr != null && !string.IsNullOrEmpty(displayAttr.Name))
            {
                return displayAttr.Name;
            }

            // 其次使用 DisplayName 特性
            var displayNameAttr = property.GetCustomAttributes(typeof(System.ComponentModel.DisplayNameAttribute), false).FirstOrDefault() as System.ComponentModel.DisplayNameAttribute;
            if (displayNameAttr != null && !string.IsNullOrEmpty(displayNameAttr.DisplayName))
            {
                return displayNameAttr.DisplayName;
            }

            // 最后使用属性名
            return property.Name;
        }

        /// <summary>
        /// 转换单元格值到目标属性类型
        /// </summary>
        private static object? ConvertValueToPropertyType(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            // 处理 Nullable 类型
            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // 空字符串处理
            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    return Activator.CreateInstance(targetType); // 返回值类型的默认值
                }
                return null;
            }

            // 直接类型匹配
            if (underlyingType.IsInstanceOfType(value))
            {
                return value;
            }

            // 字符串到枚举的转换
            if (underlyingType.IsEnum)
            {
                string enumString = value.ToString() ?? string.Empty;
                if (Enum.IsDefined(underlyingType, enumString))
                {
                    return Enum.Parse(underlyingType, enumString);
                }
                // 尝试按整数值解析
                if (int.TryParse(enumString, out int enumValue))
                {
                    return Enum.ToObject(underlyingType, enumValue);
                }
                return null;
            }

            // DateTime 特殊处理
            if (underlyingType == typeof(DateTime))
            {
                if (value is double doubleValue)
                {
                    return DateTime.FromOADate(doubleValue);
                }
                if (DateTime.TryParse(value.ToString(), out DateTime dateValue))
                {
                    return dateValue;
                }
            }

            // Boolean 特殊处理
            if (underlyingType == typeof(bool))
            {
                string boolString = value.ToString()?.Trim().ToLower() ?? string.Empty;
                if (boolString == "true" || boolString == "是" || boolString == "1")
                {
                    return true;
                }
                if (boolString == "false" || boolString == "否" || boolString == "0")
                {
                    return false;
                }
                if (bool.TryParse(boolString, out bool boolValue))
                {
                    return boolValue;
                }
            }

            // 通用类型转换
            try
            {
                return Convert.ChangeType(value, underlyingType);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
