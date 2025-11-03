using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Components
{
    /// <summary>
    /// Excel解析服务实现（Issue #1781 Task 8 Phase 1）
    ///
    /// 职责：
    /// 1. Excel文件解析：读取Excel文件为DataTable
    /// 2. 数据验证：验证患者导入数据的完整性和正确性
    /// 3. 格式检查：验证Excel文件格式和列定义
    ///
    /// 依赖：
    /// - ExcelHelper：底层Excel读取工具（NPOI封装）
    /// - ILogger：日志记录
    ///
    /// 测试要点：
    /// - ValidateImportData()：195行核心验证逻辑
    /// - 必需列检查、数据格式验证、重复检查
    /// </summary>
    public class ExcelParserService : IExcelParserService
    {
        private readonly ILogger<ExcelParserService> _logger;

        /// <summary>
        /// 支持的Excel文件扩展名
        /// </summary>
        private static readonly string[] SupportedExtensions = { ".xlsx", ".xls" };

        /// <summary>
        /// 患者导入模板列定义（按顺序）
        /// </summary>
        private static readonly string[] TemplateColumns = { "姓名", "性别", "年龄", "电话", "证件号", "地址", "过敏史" };

        public ExcelParserService(ILogger<ExcelParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 1. Excel文件解析

        /// <inheritdoc/>
        public async Task<DataTable> ParseExcelFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"找不到指定的Excel文件: {filePath}");
            }

            if (!ValidateExcelFormat(filePath, out var errorMessage))
            {
                throw new InvalidOperationException(errorMessage);
            }

            try
            {
                _logger.LogInformation("开始解析Excel文件: {FilePath}", filePath);

                // 使用ExcelHelper读取Excel文件（同步调用，使用Task.Run包装）
                var dataTable = await Task.Run(() => ExcelHelper.ImportFromExcel(filePath, hasHeader: true));

                _logger.LogInformation("Excel文件解析成功，共{RowCount}行数据", dataTable.Rows.Count);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析Excel文件失败: {FilePath}", filePath);
                throw new InvalidOperationException($"解析Excel文件失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 2. 数据验证

        /// <inheritdoc/>
        public ImportValidationResult ValidateImportData(DataTable dataTable)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }

            var result = new ImportValidationResult();
            var errors = new List<string>();
            var warnings = new List<string>();

            if (dataTable.Rows.Count == 0)
            {
                errors.Add("Excel文件中没有找到数据行");
                result.IsValid = false;
            }
            else
            {
                // 检查必需列
                var requiredColumns = new[] { "姓名", "性别" };
                var optionalColumns = new[] { "年龄", "电话", "证件号", "地址", "过敏史" };

                foreach (var column in requiredColumns)
                {
                    if (!dataTable.Columns.Contains(column))
                    {
                        errors.Add($"缺少必需列: {column}");
                    }
                }

                // 检查列格式并给出提示
                var allExpectedColumns = requiredColumns.Concat(optionalColumns).ToArray();
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (!allExpectedColumns.Contains(column.ColumnName))
                    {
                        warnings.Add($"未识别的列: {column.ColumnName}，此列数据将被忽略");
                    }
                }

                // 验证数据行
                int validRows = 0;
                int invalidRows = 0;
                var duplicateNames = new HashSet<string>();
                var phoneNumbers = new HashSet<string>();
                var idNumbers = new HashSet<string>();

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    var row = dataTable.Rows[i];
                    var rowErrors = new List<string>();
                    var rowWarnings = new List<string>();

                    // 检查是否为空行
                    bool isEmptyRow = true;
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        if (!string.IsNullOrWhiteSpace(row[col]?.ToString()))
                        {
                            isEmptyRow = false;
                            break;
                        }
                    }

                    if (isEmptyRow)
                    {
                        rowWarnings.Add("空行，将被跳过");
                        continue;
                    }

                    // 验证姓名
                    var name = row["姓名"]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        rowErrors.Add("姓名不能为空");
                    }
                    else if (name.Length > 50)
                    {
                        rowErrors.Add("姓名长度不能超过50个字符");
                    }
                    else if (duplicateNames.Contains(name))
                    {
                        rowWarnings.Add($"姓名'{name}'重复，请确认是否为同一人");
                    }
                    else
                    {
                        duplicateNames.Add(name);
                    }

                    // 验证性别
                    var gender = row["性别"]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(gender))
                    {
                        rowErrors.Add("性别不能为空");
                    }
                    else if (gender != "男" && gender != "女" && gender != "未知")
                    {
                        rowErrors.Add("性别只能是'男'、'女'或'未知'");
                    }

                    // 验证年龄（可选）
                    var ageText = row["年龄"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(ageText))
                    {
                        if (!int.TryParse(ageText, out var age) || age < 0 || age > 150)
                        {
                            rowErrors.Add("年龄必须是0-150之间的整数");
                        }
                    }

                    // 验证电话（可选）
                    var phone = row["电话"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(phone))
                    {
                        if (phone.Length < 7 || phone.Length > 15)
                        {
                            rowErrors.Add("电话号码长度应在7-15位之间");
                        }
                        else if (!Regex.IsMatch(phone, @"^[0-9\-\+\(\)\s]+$"))
                        {
                            rowErrors.Add("电话号码格式不正确，只能包含数字、横线、加号、括号和空格");
                        }
                        else if (phoneNumbers.Contains(phone))
                        {
                            rowWarnings.Add($"电话号码'{phone}'重复");
                        }
                        else
                        {
                            phoneNumbers.Add(phone);
                        }
                    }

                    // 验证证件号（可选）
                    var idNumber = row["证件号"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(idNumber))
                    {
                        if (idNumber.Length != 18 && idNumber.Length != 15)
                        {
                            rowWarnings.Add("证件号长度不是标准的15位或18位，请确认");
                        }
                        else if (idNumbers.Contains(idNumber))
                        {
                            rowErrors.Add($"证件号'{idNumber}'重复，不能导入重复证件号");
                        }
                        else
                        {
                            idNumbers.Add(idNumber);
                        }
                    }

                    // 验证地址（可选）
                    var address = row["地址"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(address) && address.Length > 200)
                    {
                        rowErrors.Add("地址长度不能超过200个字符");
                    }

                    // 验证过敏史（可选）
                    var allergy = row["过敏史"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(allergy) && allergy.Length > 500)
                    {
                        rowErrors.Add("过敏史长度不能超过500个字符");
                    }

                    // 统计结果
                    if (rowErrors.Count > 0)
                    {
                        invalidRows++;
                        errors.Add($"第{i + 2}行: {string.Join("; ", rowErrors)}");
                    }
                    else
                    {
                        validRows++;
                        if (rowWarnings.Count > 0)
                        {
                            warnings.Add($"第{i + 2}行: {string.Join("; ", rowWarnings)}");
                        }
                    }
                }

                result.ValidRowCount = validRows;
                result.InvalidRowCount = invalidRows;
                result.IsValid = errors.Count == 0 && validRows > 0;

                // 添加汇总信息
                if (validRows > 0 && invalidRows == 0)
                {
                    warnings.Add($"验证通过，共{validRows}行有效数据可以导入");
                }
                else if (validRows > 0 && invalidRows > 0)
                {
                    warnings.Add($"部分验证通过，{validRows}行有效数据可以导入，{invalidRows}行数据有错误将被跳过");
                }
            }

            result.Errors = errors;
            result.Warnings = warnings;

            _logger.LogInformation("数据验证完成，有效行: {ValidRows}, 无效行: {InvalidRows}",
                result.ValidRowCount, result.InvalidRowCount);

            return result;
        }

        /// <inheritdoc/>
        public bool ValidateExcelFormat(string filePath, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "文件路径不能为空";
                return false;
            }

            if (!File.Exists(filePath))
            {
                errorMessage = $"文件不存在: {filePath}";
                return false;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension))
            {
                errorMessage = $"不支持的文件格式: {extension}。支持的格式: {string.Join(", ", SupportedExtensions)}";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        #endregion

        #region 3. 辅助功能

        /// <inheritdoc/>
        public IEnumerable<string> GetSupportedExtensions()
        {
            return SupportedExtensions;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetTemplateColumns()
        {
            return TemplateColumns;
        }

        #endregion
    }
}
