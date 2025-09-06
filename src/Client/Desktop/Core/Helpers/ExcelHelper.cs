using System.Data;
using System.IO;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace LYBT.Desktop.Core.Helpers {

    /// <summary>
    /// Excel 操作帮助类
    /// </summary>
    public static class ExcelHelper {

        /// <summary>
        /// 导出数据到Excel
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据列表</param>
        /// <param name="columns">列定义（属性名称, 列标题）</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="sheetName">工作表名称</param>
        public static void ExportToExcel<T>(IEnumerable<T> data, Dictionary<string, string> columns, string filePath, string sheetName = "Sheet1") {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 创建标题行
            IRow headerRow = sheet.CreateRow(0);
            var headerStyle = CreateHeaderStyle(workbook);

            int columnIndex = 0;
            foreach (var column in columns) {
                ICell cell = headerRow.CreateCell(columnIndex);
                cell.SetCellValue(column.Value);
                cell.CellStyle = headerStyle;
                columnIndex++;
            }

            // 写入数据
            int rowIndex = 1;
            foreach (var item in data) {
                IRow dataRow = sheet.CreateRow(rowIndex);
                columnIndex = 0;

                foreach (var column in columns) {
                    ICell cell = dataRow.CreateCell(columnIndex);
                    var property = item?.GetType().GetProperty(column.Key);
                    if (property != null && item != null) {
                        var value = property.GetValue(item);
                        SetCellValue(cell, value);
                    }
                    columnIndex++;
                }
                rowIndex++;
            }

            // 自动调整列宽
            for (int i = 0; i < columns.Count; i++) {
                sheet.AutoSizeColumn(i);
                // 设置最小列宽
                if (sheet.GetColumnWidth(i) < 3000) {
                    sheet.SetColumnWidth(i, 3000);
                }
            }

            // 保存文件
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
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
        public static void CreateTemplate(string[] columns, string filePath, string sheetName = "Sheet1", List<string[]>? sampleData = null) {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 创建标题行
            IRow headerRow = sheet.CreateRow(0);
            var headerStyle = CreateHeaderStyle(workbook);

            for (int i = 0; i < columns.Length; i++) {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(columns[i]);
                cell.CellStyle = headerStyle;
            }

            // 添加示例数据（如果提供）
            if (sampleData != null) {
                var sampleStyle = CreateSampleStyle(workbook);
                for (int i = 0; i < sampleData.Count; i++) {
                    IRow dataRow = sheet.CreateRow(i + 1);
                    for (int j = 0; j < sampleData[i].Length && j < columns.Length; j++) {
                        ICell cell = dataRow.CreateCell(j);
                        cell.SetCellValue(sampleData[i][j]);
                        cell.CellStyle = sampleStyle;
                    }
                }
            }

            // 自动调整列宽
            for (int i = 0; i < columns.Length; i++) {
                sheet.AutoSizeColumn(i);
                // 设置最小列宽
                if (sheet.GetColumnWidth(i) < 3000) {
                    sheet.SetColumnWidth(i, 3000);
                }
            }

            // 保存文件
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
                workbook.Write(fs);
            }
            workbook.Close();
        }

        /// <summary>
        /// 从Excel导入数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="hasHeader">是否有标题行</param>
        /// <returns>数据表</returns>
        public static DataTable ImportFromExcel(string filePath, bool hasHeader = true) {
            DataTable dataTable = new DataTable();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                IWorkbook workbook = new XSSFWorkbook(fs);
                ISheet sheet = workbook.GetSheetAt(0);

                // 获取标题行
                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;

                // 创建列
                for (int i = 0; i < cellCount; i++) {
                    ICell cell = headerRow.GetCell(i);
                    if (hasHeader && cell != null) {
                        dataTable.Columns.Add(cell.ToString());
                    } else {
                        dataTable.Columns.Add($"Column{i + 1}");
                    }
                }

                // 读取数据
                int startRow = hasHeader ? 1 : 0;
                for (int i = startRow; i <= sheet.LastRowNum; i++) {
                    IRow row = sheet.GetRow(i);
                    if (row == null) {
                        continue;
                    }

                    DataRow dataRow = dataTable.NewRow();
                    bool hasValue = false;

                    for (int j = 0; j < cellCount; j++) {
                        ICell cell = row.GetCell(j);
                        if (cell != null) {
                            dataRow[j] = GetCellValue(cell);
                            if (!string.IsNullOrWhiteSpace(dataRow[j].ToString())) {
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
        private static ICellStyle CreateHeaderStyle(IWorkbook workbook) {
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
        private static ICellStyle CreateSampleStyle(IWorkbook workbook) {
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
        private static void SetCellValue(ICell cell, object? value) {
            if (value == null) {
                cell.SetCellValue(string.Empty);
                return;
            }

            var type = value.GetType();
            if (type == typeof(string)) {
                cell.SetCellValue(value.ToString() ?? string.Empty);
            } else if (type == typeof(DateTime) || type == typeof(DateTime?)) {
                cell.SetCellValue(((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"));
            } else if (type == typeof(bool) || type == typeof(bool?)) {
                cell.SetCellValue(value.ToString() ?? string.Empty);
            } else if (type == typeof(decimal) || type == typeof(decimal?) ||
                       type == typeof(double) || type == typeof(double?) ||
                       type == typeof(float) || type == typeof(float?)) {
                cell.SetCellValue(Convert.ToDouble(value));
            } else if (type == typeof(int) || type == typeof(int?) ||
                       type == typeof(long) || type == typeof(long?) ||
                       type == typeof(short) || type == typeof(short?)) {
                cell.SetCellValue(Convert.ToDouble(value));
            } else {
                cell.SetCellValue(value.ToString() ?? string.Empty);
            }
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        private static object? GetCellValue(ICell cell) {
            if (cell == null) {
                return string.Empty;
            }

            switch (cell.CellType) {
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell)) {
                        return cell.DateCellValue;
                    }
                    return cell.NumericCellValue;

                case CellType.String:
                    return cell.StringCellValue;

                case CellType.Boolean:
                    return cell.BooleanCellValue;

                case CellType.Formula:
                    try {
                        return cell.NumericCellValue;
                    } catch {
                        return cell.StringCellValue;
                    }
                default:
                    return string.Empty;
            }
        }
    }
}
