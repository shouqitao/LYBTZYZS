using FluentValidation;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者导入导出服务 (Epic #1934)
    /// 负责Excel批量导入、模板导出、数据导出
    /// 从PatientService拆分，单一职责：Excel I/O操作
    /// </summary>
    public class PatientImportExportService : IPatientImportExportService
    {
        private readonly IPatientRepository _repository;
        private readonly IValidator<PatientInputDto> _validator;
        private readonly PatientMapper _mapper = new();
        private readonly ILogger<PatientImportExportService> _logger;

        public PatientImportExportService(
            IPatientRepository repository,
            IValidator<PatientInputDto> validator,
            ILogger<PatientImportExportService> logger)
        {
            _repository = repository;
            _validator = validator;
            _logger = logger;
        }

        /// <summary>
        /// 批量导入患者数据 (Epic #1934 FR-001)
        /// 实现BR-002失败恢复机制：部分成功模式 + 详细失败信息
        /// eliminate-service-catch-return: 移除外层try-catch，保留行级错误隔离
        /// </summary>
        public async Task<Result<PatientBatchImportResultDto>> BatchImportAsync(Stream stream, string? fileName = null)
        {
            var result = new PatientBatchImportResultDto
            {
                ImportTime = DateTime.Now
            };

            // 设置EPPlus许可证上下文（非商业用途）
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return Result<PatientBatchImportResultDto>.Failure(GenericErrorCode.InvalidRequest, "Excel文件中没有工作表");
            }

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1)
            {
                return Result<PatientBatchImportResultDto>.Success(result);
            }

            // BR-003: 限制最大导入行数 (T5-P3-11: 修复off-by-one，rowCount包含表头行)
            if (rowCount - 1 > 1000)
            {
                return Result<PatientBatchImportResultDto>.Failure(GenericErrorCode.PatientImportRowExceeded, $"导入数据超过限制（最大1000行，实际{rowCount - 1}行）");
            }

            var patientsToCreate = new List<Patient>();

            // T5-P2-28: 批量内去重跟踪
            var importedPhoneNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var importedIdNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 从第2行开始读取数据（第1行是表头）
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    // 解析Excel行数据到PatientInputDto
                    var inputDto = ParseExcelRow(worksheet, row);

                    // FluentValidation验证
                    var validationResult = await _validator.ValidateAsync(inputDto);

                    if (!validationResult.IsValid)
                    {
                        // 记录验证失败详情
                        result.FailureCount++;
                        var firstError = validationResult.Errors.First();
                        result.Failures.Add(new PatientImportFailureDto
                        {
                            OriginalRowNumber = row,
                            FailureReason = firstError.ErrorMessage,
                            FieldName = firstError.PropertyName,
                            OriginalValue = GetFieldValue(inputDto, firstError.PropertyName),
                            SuggestedFix = GenerateSuggestedFix(firstError.PropertyName, firstError.ErrorMessage),
                            DataSnapshot = inputDto
                        });
                        continue;
                    }

                    // BR-004: 检查手机号重复 (数据库 + 批量内)
                    if (!string.IsNullOrEmpty(inputDto.PhoneNumber))
                    {
                        if (importedPhoneNumbers.Contains(inputDto.PhoneNumber))
                        {
                            result.SkippedCount++;
                            result.Failures.Add(new PatientImportFailureDto
                            {
                                OriginalRowNumber = row,
                                FailureReason = "手机号在本次导入中重复",
                                FieldName = "PhoneNumber",
                                OriginalValue = inputDto.PhoneNumber,
                                SuggestedFix = "修改手机号或跳过该记录",
                                DataSnapshot = inputDto
                            });
                            continue;
                        }

                        var existing = await _repository.GetByPhoneNumberAsync(inputDto.PhoneNumber);
                        if (existing != null)
                        {
                            result.SkippedCount++;
                            result.Failures.Add(new PatientImportFailureDto
                            {
                                OriginalRowNumber = row,
                                FailureReason = "手机号已存在",
                                FieldName = "PhoneNumber",
                                OriginalValue = inputDto.PhoneNumber,
                                SuggestedFix = "修改手机号或跳过该记录",
                                DataSnapshot = inputDto
                            });
                            continue;
                        }
                    }

                    // T5-P2-28: 检查身份证号重复 (数据库 + 批量内)
                    if (!string.IsNullOrEmpty(inputDto.IdNumber))
                    {
                        if (importedIdNumbers.Contains(inputDto.IdNumber))
                        {
                            result.SkippedCount++;
                            result.Failures.Add(new PatientImportFailureDto
                            {
                                OriginalRowNumber = row,
                                FailureReason = "身份证号在本次导入中重复",
                                FieldName = "IdNumber",
                                OriginalValue = inputDto.IdNumber,
                                SuggestedFix = "修改身份证号或跳过该记录",
                                DataSnapshot = inputDto
                            });
                            continue;
                        }

                        var existingById = await _repository.GetByIdNumberAsync(inputDto.IdNumber);
                        if (existingById != null)
                        {
                            result.SkippedCount++;
                            result.Failures.Add(new PatientImportFailureDto
                            {
                                OriginalRowNumber = row,
                                FailureReason = "身份证号已存在",
                                FieldName = "IdNumber",
                                OriginalValue = inputDto.IdNumber,
                                SuggestedFix = "修改身份证号或跳过该记录",
                                DataSnapshot = inputDto
                            });
                            continue;
                        }
                    }

                    // 映射到Patient实体
                    var patient = _mapper.ToEntity(inputDto);

                    // 生成拼音码（Task 2.6）
                    patient.PinYinCode = PinYinHelper.GetPinYinCode(patient.Name);

                    // T5-P2-28: 记录已导入的手机号和身份证号用于批量内去重
                    if (!string.IsNullOrEmpty(inputDto.PhoneNumber))
                        importedPhoneNumbers.Add(inputDto.PhoneNumber);
                    if (!string.IsNullOrEmpty(inputDto.IdNumber))
                        importedIdNumbers.Add(inputDto.IdNumber);

                    patientsToCreate.Add(patient);
                }
                catch (Exception ex)
                {
                    // 行级错误隔离：单行解析失败不影响其他行
                    // ERR-012: 使用安全消息替代ex.Message
                    _logger.LogError(ex, "[SVC] Patient.BatchImport → RowError - Row={Row}", row);
                    result.FailureCount++;
                    result.Failures.Add(new PatientImportFailureDto
                    {
                        OriginalRowNumber = row,
                        FailureReason = "数据解析异常",
                        FieldName = "Unknown",
                        OriginalValue = string.Empty,
                        SuggestedFix = "检查该行数据格式是否正确",
                        DataSnapshot = new PatientInputDto()
                    });
                }
            }

            // 批量保存患者
            if (patientsToCreate.Count > 0)
            {
                var savedPatients = await _repository.AddRangeAsync(patientsToCreate);
                result.SuccessCount = savedPatients.Count();
            }

            return Result<PatientBatchImportResultDto>.Success(result);
        }

        /// <summary>
        /// 导出患者导入模板 (Epic #1934 FR-002)
        /// </summary>
        public async Task<MemoryStream> ExportTemplateAsync(ExportTemplateDto config)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("患者导入模板");

            // 设置表头
            worksheet.Cells[1, 1].Value = "姓名*";
            worksheet.Cells[1, 2].Value = "性别";
            worksheet.Cells[1, 3].Value = "出生日期";
            worksheet.Cells[1, 4].Value = "身份证号*";
            worksheet.Cells[1, 5].Value = "手机号码";
            worksheet.Cells[1, 6].Value = "地址";
            worksheet.Cells[1, 7].Value = "过敏史";
            worksheet.Cells[1, 8].Value = "既往病史"; // Epic #1934新增

            // 设置表头样式
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // 添加示例数据（如果配置要求）
            if (config.IncludeSampleData)
            {
                var sampleRowCount = Math.Min(config.SampleRowCount, 10);
                for (int i = 0; i < sampleRowCount; i++)
                {
                    int row = i + 2;
                    worksheet.Cells[row, 1].Value = $"张三{i + 1}";
                    worksheet.Cells[row, 2].Value = i % 2 == 0 ? "男" : "女";
                    worksheet.Cells[row, 3].Value = $"1990-0{(i % 9) + 1}-15";
                    worksheet.Cells[row, 4].Value = $"110101199001{(i % 9) + 1:D2}001{i}";
                    worksheet.Cells[row, 5].Value = $"1380000000{i}";
                    worksheet.Cells[row, 6].Value = $"北京市朝阳区示例地址{i + 1}号";
                    worksheet.Cells[row, 7].Value = i % 3 == 0 ? "青霉素过敏" : "";
                    worksheet.Cells[row, 8].Value = i % 4 == 0 ? "高血压病史" : "";
                }
            }

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();

            // 返回Excel文件流
            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// 导出患者数据到Excel (Epic #1934 FR-003)
        /// </summary>
        public async Task<MemoryStream> ExportPatientsAsync(string? keyword = null)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // 获取患者数据（使用分页查询，最大10000条）
            var patientResult = string.IsNullOrWhiteSpace(keyword)
                ? await _repository.GetPagedAsync(1, 10000)
                : await _repository.GetPagedAsync(1, 10000, keyword);
            var patients = patientResult.Items;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("患者数据");

            // 设置表头
            worksheet.Cells[1, 1].Value = "姓名";
            worksheet.Cells[1, 2].Value = "性别";
            worksheet.Cells[1, 3].Value = "出生日期";
            worksheet.Cells[1, 4].Value = "年龄";
            worksheet.Cells[1, 5].Value = "身份证号";
            worksheet.Cells[1, 6].Value = "手机号码";
            worksheet.Cells[1, 7].Value = "地址";
            worksheet.Cells[1, 8].Value = "过敏史";
            worksheet.Cells[1, 9].Value = "既往病史"; // Epic #1934新增
            worksheet.Cells[1, 10].Value = "最后就诊时间";
            worksheet.Cells[1, 11].Value = "就诊次数";
            worksheet.Cells[1, 12].Value = "状态";

            // 设置表头样式
            using (var range = worksheet.Cells[1, 1, 1, 12])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // 填充数据
            int row = 2;
            foreach (var patient in patients)
            {
                worksheet.Cells[row, 1].Value = patient.Name;
                worksheet.Cells[row, 2].Value = patient.Gender.ToString();
                worksheet.Cells[row, 3].Value = patient.BirthDate?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 4].Value = patient.Age;
                worksheet.Cells[row, 5].Value = patient.IdNumber;
                worksheet.Cells[row, 6].Value = patient.PhoneNumber;
                worksheet.Cells[row, 7].Value = patient.Address;
                worksheet.Cells[row, 8].Value = patient.AllergyHistory;
                worksheet.Cells[row, 9].Value = patient.MedicalHistory;
                worksheet.Cells[row, 10].Value = patient.LastVisitTime?.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cells[row, 11].Value = patient.VisitCount;
                worksheet.Cells[row, 12].Value = patient.Status.ToString();
                row++;
            }

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();

            // 返回Excel文件流
            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;
            return stream;
        }

        #region 私有辅助方法

        /// <summary>
        /// 解析Excel行数据到PatientInputDto
        /// </summary>
        private static PatientInputDto ParseExcelRow(ExcelWorksheet worksheet, int row)
        {
            return new PatientInputDto
            {
                Name = worksheet.Cells[row, 1].Text?.Trim() ?? string.Empty,
                Gender = ParseGender(worksheet.Cells[row, 2].Text?.Trim()),
                BirthDate = ParseDate(worksheet.Cells[row, 3].Text?.Trim()),
                IdNumber = worksheet.Cells[row, 4].Text?.Trim(),
                PhoneNumber = worksheet.Cells[row, 5].Text?.Trim(),
                Address = worksheet.Cells[row, 6].Text?.Trim(),
                AllergyHistory = worksheet.Cells[row, 7].Text?.Trim(),
                MedicalHistory = worksheet.Cells[row, 8].Text?.Trim() // Epic #1934新增
            };
        }

        /// <summary>
        /// 解析性别
        /// </summary>
        private static Gender ParseGender(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Gender.Unknown;
            return text switch
            {
                "男" => Gender.Male,
                "女" => Gender.Female,
                _ => Gender.Unknown
            };
        }

        /// <summary>
        /// 解析日期
        /// </summary>
        private static DateTime? ParseDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (DateTime.TryParse(text, out var date)) return date;
            return null;
        }

        /// <summary>
        /// 获取字段值（用于失败详情）
        /// </summary>
        private static string GetFieldValue(PatientInputDto dto, string propertyName)
        {
            var property = typeof(PatientInputDto).GetProperty(propertyName);
            return property?.GetValue(dto)?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 生成修复建议
        /// </summary>
        private static string GenerateSuggestedFix(string fieldName, string errorMessage)
        {
            return fieldName switch
            {
                "Name" => "请输入有效的患者姓名（1-50个字符）",
                "PhoneNumber" => "请输入11位手机号码",
                "IdNumber" => "请输入18位身份证号",
                "BirthDate" => "请输入有效的出生日期（YYYY-MM-DD）",
                "Age" => "年龄必须在0-150之间",
                _ => "请检查该字段的值是否符合要求"
            };
        }

        #endregion
    }
}
