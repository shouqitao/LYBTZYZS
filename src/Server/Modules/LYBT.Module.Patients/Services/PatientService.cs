using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using FluentValidation;
using LYBT.Shared.Utilities.Text;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者服务 - 简化版，只包含基础CRUD
    /// 同时实现 Module 内部接口和 Shared 跨平台接口
    /// Phase 3 Task 3.1: 实现优化版本，消除双重映射
    /// </summary>
    public class PatientService : IPatientService, IPatientServiceOptimized
    {
        private readonly IPatientRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientService> _logger;
        private readonly IValidator<PatientInputDto> _validator;

        public PatientService(
            IPatientRepository repository,
            IMapper mapper,
            ILogger<PatientService> logger,
            IValidator<PatientInputDto> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _validator = validator;
        }

        public async Task<Result<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                // Bug #1587修复：支持关键字搜索（姓名/拼音码/手机号）
                // IRepository<T>统一接口：GetPagedAsync(page, pageSize, keyword)
                var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);

                var items = _mapper.Map<List<PatientDto>>(pagedResult.Items);

                // 确保Age属性正确计算（从实体的计算属性复制到DTO）
                foreach (var item in items)
                {
                    var entity = pagedResult.Items.FirstOrDefault(e => e.Id == item.Id);
                    if (entity != null)
                    {
                        item.Age = entity.Age;
                    }
                }

                var dto = new PagedResult<PatientDto>
                {
                    Items = items,
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return Result<PagedResult<PatientDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者列表失败，关键字：{Keyword}", keyword);
                return Result<PagedResult<PatientDto>>.Failure("获取患者列表失败");
            }
        }

        public async Task<Result<PatientDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Result<PatientDto>.Failure("患者不存在");

                var dto = _mapper.Map<PatientDto>(entity);

                // 确保Age属性正确计算（从实体的计算属性复制到DTO）
                dto.Age = entity.Age;

                return Result<PatientDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者详情失败");
                return Result<PatientDto>.Failure("获取患者详情失败");
            }
        }

        public async Task<Result<PatientDto>> CreateAsync(PatientInputDto dto)
        {
            try
            {
                // FluentValidation 验证（Phase 1 Task 1.7）
                var validationResult = await _validator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("患者创建验证失败: {Errors}", string.Join("; ", errors));
                    return Result<PatientDto>.Failure(errors);
                }

                var entity = _mapper.Map<Patient>(dto);

                // 生成拼音码（基于姓名）
                entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);

                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<PatientDto>(result);

                // 确保Age属性正确计算
                resultDto.Age = result.Age;

                return Result<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者失败");
                return Result<PatientDto>.Failure("创建患者失败");
            }
        }

        public async Task<Result<PatientDto>> UpdateAsync(Guid id, PatientInputDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Result<PatientDto>.Failure("患者不存在");

                // FluentValidation 验证（Phase 1 Task 1.7）
                var validationResult = await _validator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("患者更新验证失败: {PatientId}, {Errors}", id, string.Join("; ", errors));
                    return Result<PatientDto>.Failure(errors);
                }

                // 保存旧的姓名用于检测变化
                var oldName = entity.Name;

                _mapper.Map(dto, entity);

                // 更新拼音码（仅当姓名发生变化时）
                if (entity.Name != oldName)
                {
                    entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);
                    _logger.LogDebug("患者姓名变化，重新生成拼音码: {OldName} -> {NewName}, PinYin: {PinYin}",
                        oldName, entity.Name, entity.PinYinCode);
                }

                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<PatientDto>(result);

                // 确保Age属性正确计算
                resultDto.Age = result.Age;

                return Result<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者失败");
                return Result<PatientDto>.Failure("更新患者失败");
            }
        }

        public async Task<Result<List<PatientDto>>> SearchAsync(string keyword)
        {
            try
            {
                // 如果关键字为空，返回空列表
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return Result<List<PatientDto>>.Success(new List<PatientDto>());
                }

                // 搜索匹配关键字的患者（姓名、电话或身份证号）
                var allPatients = await _repository.GetAllAsync();
                var patients = allPatients.Where(p =>
                    p.Name.Contains(keyword)).ToList();

                // 转换为DTO
                var patientDtos = _mapper.Map<List<PatientDto>>(patients);

                return Result<List<PatientDto>>.Success(patientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者时发生错误，关键字：{Keyword}", keyword);
                return Result<List<PatientDto>>.Failure($"搜索患者失败：{ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? Result.Success() : Result.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败");
                return Result.Failure("删除患者失败");
            }
        }

        /// <summary>
        /// 批量导入患者数据 (Epic #1934 FR-001)
        /// 实现BR-002失败恢复机制：部分成功模式 + 详细失败信息
        /// </summary>
        public async Task<Result<BatchImportResultDto>> BatchImportAsync(Stream stream, string? fileName = null)
        {
            var result = new BatchImportResultDto
            {
                ImportTime = DateTime.Now
            };

            try
            {
                // 设置EPPlus许可证上下文（非商业用途）
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    return Result<BatchImportResultDto>.Failure("Excel文件中没有工作表");
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount <= 1)
                {
                    return Result<BatchImportResultDto>.Success(result);
                }

                // BR-003: 限制最大导入行数
                if (rowCount > 1000)
                {
                    return Result<BatchImportResultDto>.Failure($"导入数据超过限制（最大1000行，实际{rowCount - 1}行）");
                }

                var patientsToCreate = new List<Patient>();

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
                            result.Failures.Add(new ImportFailureDetailDto
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

                        // BR-004: 检查手机号重复
                        if (!string.IsNullOrEmpty(inputDto.PhoneNumber))
                        {
                            var existing = await _repository.GetByPhoneNumberAsync(inputDto.PhoneNumber);
                            if (existing != null)
                            {
                                result.SkippedCount++;
                                result.Failures.Add(new ImportFailureDetailDto
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

                        // 映射到Patient实体
                        var patient = _mapper.Map<Patient>(inputDto);

                        // 生成拼音码（Task 2.6）
                        patient.PinYinCode = PinYinHelper.GetPinYinCode(patient.Name);

                        patientsToCreate.Add(patient);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"处理第{row}行数据时发生异常");
                        result.FailureCount++;
                        result.Failures.Add(new ImportFailureDetailDto
                        {
                            OriginalRowNumber = row,
                            FailureReason = $"数据解析异常: {ex.Message}",
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
                    var savedPatients = await _repository.BatchCreateAsync(patientsToCreate);
                    result.SuccessCount = savedPatients.Count;
                }

                return Result<BatchImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入患者数据失败");
                return Result<BatchImportResultDto>.Failure($"导入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析Excel行数据到PatientInputDto
        /// </summary>
        private PatientInputDto ParseExcelRow(ExcelWorksheet worksheet, int row)
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
        private Gender ParseGender(string? text)
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
        private DateTime? ParseDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (DateTime.TryParse(text, out var date)) return date;
            return null;
        }

        /// <summary>
        /// 获取字段值（用于失败详情）
        /// </summary>
        private string GetFieldValue(PatientInputDto dto, string propertyName)
        {
            var property = typeof(PatientInputDto).GetProperty(propertyName);
            return property?.GetValue(dto)?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 生成修复建议
        /// </summary>
        private string GenerateSuggestedFix(string fieldName, string errorMessage)
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
            worksheet.Cells[1, 4].Value = "身份证号";
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
            var patients = string.IsNullOrWhiteSpace(keyword)
                ? await _repository.SearchPatientsAsync(null, 1, 10000)
                : await _repository.SearchPatientsAsync(keyword, 1, 10000);

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
            foreach (var patient in patients.Items)
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

        #region IPatientServiceOptimized 实现 - Entity直接返回方法

        /// <summary>
        /// 获取分页患者数据（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除双重映射，提升性能15-20%
        /// </summary>
        public async Task<Result<PagedResult<Patient>>> GetPagedEntityAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                // 直接返回Repository查询结果，不进行DTO映射
                var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
                return Result<PagedResult<Patient>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者实体列表失败，关键字：{Keyword}", keyword);
                return Result<PagedResult<Patient>>.Failure("获取患者列表失败");
            }
        }

        /// <summary>
        /// 根据ID获取患者（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除Entity→DTO映射，提升性能
        /// </summary>
        public async Task<Result<Patient>> GetByIdEntityAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Result<Patient>.Failure("患者不存在");

                return Result<Patient>.Success(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者实体详情失败");
                return Result<Patient>.Failure("获取患者详情失败");
            }
        }

        /// <summary>
        /// 创建患者（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除DTO→Entity→DTO双重映射
        /// </summary>
        public async Task<Result<Patient>> CreateEntityAsync(PatientInputDto dto)
        {
            try
            {
                // FluentValidation 验证
                var validationResult = await _validator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("患者创建验证失败: {Errors}", string.Join("; ", errors));
                    return Result<Patient>.Failure(errors);
                }

                var entity = _mapper.Map<Patient>(dto);

                // 生成拼音码（基于姓名）
                entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);

                var result = await _repository.AddAsync(entity);
                return Result<Patient>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者实体失败");
                return Result<Patient>.Failure("创建患者失败");
            }
        }

        /// <summary>
        /// 更新患者（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除DTO→Entity→DTO双重映射
        /// </summary>
        public async Task<Result<Patient>> UpdateEntityAsync(Guid id, PatientInputDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Result<Patient>.Failure("患者不存在");

                // FluentValidation 验证
                var validationResult = await _validator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("患者更新验证失败: {PatientId}, {Errors}", id, string.Join("; ", errors));
                    return Result<Patient>.Failure(errors);
                }

                // 保存旧的姓名用于检测变化
                var oldName = entity.Name;

                _mapper.Map(dto, entity);

                // 更新拼音码（仅当姓名发生变化时）
                if (entity.Name != oldName)
                {
                    entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);
                    _logger.LogDebug("患者姓名变化，重新生成拼音码: {OldName} -> {NewName}, PinYin: {PinYin}",
                        oldName, entity.Name, entity.PinYinCode);
                }

                var result = await _repository.UpdateAsync(entity);
                return Result<Patient>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者实体失败");
                return Result<Patient>.Failure("更新患者失败");
            }
        }

        /// <summary>
        /// 搜索患者（直接返回Patient Entity列表）
        /// Phase 3 Task 3.1: 消除Entity→DTO映射
        /// </summary>
        public async Task<Result<List<Patient>>> SearchEntityAsync(string keyword)
        {
            try
            {
                // 如果关键字为空，返回空列表
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return Result<List<Patient>>.Success(new List<Patient>());
                }

                // 搜索匹配关键字的患者（姓名、电话或身份证号）
                var allPatients = await _repository.GetAllAsync();
                var patients = allPatients.Where(p =>
                    p.Name.Contains(keyword)).ToList();

                return Result<List<Patient>>.Success(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者实体时发生错误，关键字：{Keyword}", keyword);
                return Result<List<Patient>>.Failure($"搜索患者失败：{ex.Message}");
            }
        }

        #endregion
    }
}
