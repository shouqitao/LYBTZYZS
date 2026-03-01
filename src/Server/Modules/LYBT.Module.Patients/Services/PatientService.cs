using FluentValidation;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者服务 - 统一接口实现
    /// 包含DTO和Entity两种返回模式
    /// Phase 2: 继承BaseService<Patient>复用统一错误处理和验证逻辑
    /// OpenSpec: adopt-mapperly-unified-mapping - 使用PatientMapper替代AutoMapper
    /// </summary>
    public class PatientService : BaseService<Patient>, IPatientService
    {
        private readonly IPatientRepository _repository;
        private readonly IValidator<PatientInputDto> _validator;
        private readonly PatientMapper _mapper = new();
        private readonly AppDbContext _dbContext;
        private readonly ICacheInvalidationService _cacheInvalidation;

        public PatientService(
            IPatientRepository repository,
            ILogger<PatientService> logger,
            IValidator<PatientInputDto> validator,
            AppDbContext dbContext,
            ICacheInvalidationService cacheInvalidation)
            : base(logger)
        {
            _repository = repository;
            _validator = validator;
            _dbContext = dbContext;
            _cacheInvalidation = cacheInvalidation;
        }

        public async Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, bool filterDisabled = false)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // Bug #1587修复：支持关键字搜索（姓名/拼音码/手机号）
            // T5-P2-27: filterDisabled=true时只显示启用状态患者
            var pagedResult = filterDisabled
                ? await _repository.GetPagedWithStatusFilterAsync(page, pageSize, keyword, CommonStatus.Enabled)
                : await _repository.GetPagedAsync(page, pageSize, keyword);

            var items = _mapper.ToListDtos(pagedResult.Items.ToList());

            // 确保Age属性正确计算（从实体的计算属性复制到DTO）
            foreach (var item in items)
            {
                var entity = pagedResult.Items.FirstOrDefault(e => e.Id == item.Id);
                if (entity != null)
                {
                    item.Age = entity.Age;
                }
            }

            var dto = new PagedResult<PatientListDto>
            {
                Items = items,
                TotalCount = pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };
            return Result<PagedResult<PatientListDto>>.Success(dto);
        }

        /// <summary>
        /// 分页查询患者列表（返回PatientListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        public async Task<Result<PagedResult<PatientListDto>>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null, bool filterDisabled = false)
        {
            // eliminate-service-catch-return: 移除冗余try-catch
            // T5-P2-27: filterDisabled=true时只显示启用状态患者
            var pagedResult = filterDisabled
                ? await _repository.GetPagedWithStatusFilterAsync(page, pageSize, keyword, CommonStatus.Enabled)
                : await _repository.GetPagedAsync(page, pageSize, keyword);
            var dtos = _mapper.ToListDtos(pagedResult.Items.ToList());

            // 确保Age属性正确计算
            foreach (var dto in dtos)
            {
                var entity = pagedResult.Items.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.Age = entity.Age;
                }
            }

            var result = new PagedResult<PatientListDto>
            {
                Items = dtos,
                TotalCount = pagedResult.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
            return Result<PagedResult<PatientListDto>>.Success(result);
        }

        public async Task<Result<PatientDetailDto>> GetByIdAsync(Guid id)
        {
            // eliminate-service-catch-return: 业务逻辑检查保留在外部，无需ExecuteAsync包装
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientNotFound);

            var dto = _mapper.ToDetailDto(entity);
            // 确保Age属性正确计算（从实体的计算属性复制到DTO）
            dto.Age = entity.Age;

            return Result<PatientDetailDto>.Success(dto);
        }

        public async Task<Result<PatientDetailDto>> CreateAsync(PatientInputDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务验证
            // FluentValidation 验证（Phase 1 Task 1.7）
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] Patient.Create → ValidationFailed - Errors={Errors}", string.Join("; ", errors));
                return Result<PatientDetailDto>.Failure(errors);
            }

            // T5-P3-10: 检查手机号唯一性 (与 CreateEntityAsync 保持一致)
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var existingByPhone = await _repository.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existingByPhone != null && !existingByPhone.IsDeleted)
                {
                    return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientPhoneExists, $"手机号 {dto.PhoneNumber} 已存在");
                }
            }

            // T5-P3-10: 检查身份证号唯一性 (与 CreateEntityAsync 保持一致)
            if (!string.IsNullOrEmpty(dto.IdNumber))
            {
                var existingByIdNumber = await _repository.GetByIdNumberAsync(dto.IdNumber);
                if (existingByIdNumber != null)
                {
                    return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientIdCardExists, $"身份证号 {dto.IdNumber} 已存在");
                }
            }

            var entity = _mapper.ToEntity(dto);

            // 生成拼音码（基于姓名）
            entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);

            var result = await _repository.AddAsync(entity);
            await _cacheInvalidation.InvalidateAsync("patients");
            var resultDto = _mapper.ToDetailDto(result);

            // 确保Age属性正确计算
            resultDto.Age = result.Age;

            return Result<PatientDetailDto>.Success(resultDto);
        }

        public async Task<Result<PatientDetailDto>> UpdateAsync(Guid id, PatientInputDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务逻辑检查
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientNotFound);

            // FluentValidation 验证（Phase 1 Task 1.7）
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] Patient.Update → ValidationFailed - PatientId={PatientId} Errors={Errors}", id, string.Join("; ", errors));
                return Result<PatientDetailDto>.Failure(errors);
            }

            // T5-P2-25: 更新时检查手机号唯一性 (排除自身)
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var existingByPhone = await _repository.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existingByPhone != null && existingByPhone.Id != id)
                {
                    return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientPhoneExists, $"手机号 {dto.PhoneNumber} 已被其他患者使用");
                }
            }

            // T5-P2-26: 更新时检查身份证号唯一性 (排除自身)
            if (!string.IsNullOrEmpty(dto.IdNumber))
            {
                var existingByIdNumber = await _repository.GetByIdNumberAsync(dto.IdNumber);
                if (existingByIdNumber != null && existingByIdNumber.Id != id)
                {
                    return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientIdCardExists, $"身份证号 {dto.IdNumber} 已被其他患者使用");
                }
            }

            // 保存旧的姓名用于检测变化
            var oldName = entity.Name;

            _mapper.UpdateEntity(dto, entity);

            // 更新拼音码（仅当姓名发生变化时）
            if (entity.Name != oldName)
            {
                entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);
                _logger.LogDebug("[SVC] Patient.Update → PinYinRegenerated - OldName={OldName} NewName={NewName} PinYin={PinYin}",
                    oldName, entity.Name, entity.PinYinCode);
            }

            var result = await _repository.UpdateAsync(entity);
            await _cacheInvalidation.InvalidateAsync("patients");
            var resultDto = _mapper.ToDetailDto(result);

            // 确保Age属性正确计算
            resultDto.Age = result.Age;

            return Result<PatientDetailDto>.Success(resultDto);
        }

        public async Task<Result<List<PatientDetailDto>>> SearchAsync(string keyword)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，修复ERR-012违规(ex.Message)
            // 如果关键字为空，返回空列表
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Result<List<PatientDetailDto>>.Success(new List<PatientDetailDto>());
            }

            // Task 2.1: 优化搜索逻辑 - 使用Repository的GetPagedAsync方法避免全量加载
            // 搜索前100条匹配关键字的患者（姓名、电话或身份证号）
            var searchResult = await _repository.GetPagedAsync(1, 100, keyword);

            // 转换为DTO
            var patientDtos = _mapper.ToDetailDtos(searchResult.Items.ToList());

            return Result<List<PatientDetailDto>>.Success(patientDtos);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            // X7: 删除前强制引用检查
            var refCheck = await CheckReferenceAsync(id);
            if (refCheck.IsSuccess && refCheck.Data != null && refCheck.Data.HasReferences)
            {
                _logger.LogWarning("[SVC] Patient.Delete → HasReferences - PatientId={PatientId} ReferenceCount={Count}",
                    id, refCheck.Data.ReferenceCount);
                return Result.Failure(GenericErrorCode.PatientHasActiveCases, $"患者有 {refCheck.Data.ReferenceCount} 条医案记录，无法删除");
            }

            var result = await _repository.DeleteAsync(id);
            if (result)
            {
                await _cacheInvalidation.InvalidateAsync("patients");
            }
            return result ? Result.Success() : Result.Failure(GenericErrorCode.InternalError, "删除失败");
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

        #region IPatientServiceOptimized 实现 - Entity直接返回方法

        /// <summary>
        /// 获取分页患者数据（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除双重映射，提升性能15-20%
        /// </summary>
        public async Task<Result<PagedResult<Patient>>> GetPagedEntityAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch
            var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
            return Result<PagedResult<Patient>>.Success(pagedResult);
        }

        /// <summary>
        /// 根据ID获取患者（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除Entity→DTO映射，提升性能
        /// </summary>
        public async Task<Result<Patient>> GetByIdEntityAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务检查
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<Patient>.Failure(GenericErrorCode.PatientNotFound);

            return Result<Patient>.Success(entity);
        }

        /// <summary>
        /// 创建患者（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除DTO→Entity→DTO双重映射
        /// </summary>
        public async Task<Result<Patient>> CreateEntityAsync(PatientInputDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务验证
            // FluentValidation 验证
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] Patient.Create → ValidationFailed - Errors={Errors}", string.Join("; ", errors));
                return Result<Patient>.Failure(errors);
            }

            // Issue #2245 Fix: 检查手机号唯一性(防止重复)
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var existingPatient = await _repository.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existingPatient != null && !existingPatient.IsDeleted)
                {
                    return Result<Patient>.Failure(GenericErrorCode.PatientPhoneExists, $"手机号 {dto.PhoneNumber} 已存在");
                }
            }

            // T5-P2-24: 检查身份证号唯一性
            if (!string.IsNullOrEmpty(dto.IdNumber))
            {
                var existingByIdNumber = await _repository.GetByIdNumberAsync(dto.IdNumber);
                if (existingByIdNumber != null)
                {
                    return Result<Patient>.Failure(GenericErrorCode.PatientIdCardExists, $"身份证号 {dto.IdNumber} 已存在");
                }
            }

            var entity = _mapper.ToEntity(dto);

            // 生成拼音码（基于姓名）
            entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);

            var result = await _repository.AddAsync(entity);
            return Result<Patient>.Success(result);
        }

        /// <summary>
        /// 更新患者（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除DTO→Entity→DTO双重映射
        /// </summary>
        public async Task<Result<Patient>> UpdateEntityAsync(Guid id, PatientInputDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务逻辑检查
            // Issue #2245 Fix: 检查实体存在性(包括软删除状态)
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted)
                return Result<Patient>.Failure(GenericErrorCode.PatientNotFound);

            // FluentValidation 验证
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] Patient.Update → ValidationFailed - PatientId={PatientId} Errors={Errors}", id, string.Join("; ", errors));
                return Result<Patient>.Failure(errors);
            }

            // T5-P2-25: 更新时检查手机号唯一性 (排除自身)
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var existingByPhone = await _repository.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existingByPhone != null && existingByPhone.Id != id)
                {
                    return Result<Patient>.Failure(GenericErrorCode.PatientPhoneExists, $"手机号 {dto.PhoneNumber} 已被其他患者使用");
                }
            }

            // T5-P2-26: 更新时检查身份证号唯一性 (排除自身)
            if (!string.IsNullOrEmpty(dto.IdNumber))
            {
                var existingByIdNumber = await _repository.GetByIdNumberAsync(dto.IdNumber);
                if (existingByIdNumber != null && existingByIdNumber.Id != id)
                {
                    return Result<Patient>.Failure(GenericErrorCode.PatientIdCardExists, $"身份证号 {dto.IdNumber} 已被其他患者使用");
                }
            }

            // 保存旧的姓名用于检测变化
            var oldName = entity.Name;

            _mapper.UpdateEntity(dto, entity);

            // 更新拼音码（仅当姓名发生变化时）
            if (entity.Name != oldName)
            {
                entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);
                _logger.LogDebug("[SVC] Patient.Update → PinYinRegenerated - OldName={OldName} NewName={NewName} PinYin={PinYin}",
                    oldName, entity.Name, entity.PinYinCode);
            }

            var result = await _repository.UpdateAsync(entity);
            return Result<Patient>.Success(result);
        }

        #endregion

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法实现 ==========

        /// <summary>
        /// 切换患者状态（启用/禁用）
        /// </summary>
        public async Task<Result<PatientDetailDto>> ToggleStatusAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientNotFound);
            }

            // 切换状态
            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);
            dto.Age = result.Age;

            _logger.LogInformation("[SVC] Patient.ToggleStatus completed - PatientId={PatientId} Status={Status}", id, entity.Status);

            return Result<PatientDetailDto>.Success(dto);
        }

        /// <summary>
        /// 恢复软删除的患者
        /// </summary>
        public async Task<Result<PatientDetailDto>> RestoreAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务逻辑检查
            var entity = await _repository.GetByIdIncludingDeletedAsync(id);
            if (entity == null)
                return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientNotFound);

            if (!entity.IsDeleted)
                return Result<PatientDetailDto>.Failure(GenericErrorCode.InvalidPatientStatus, "该患者未被删除，无需恢复");

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);

            // 确保Age属性正确计算
            dto.Age = result.Age;

            _logger.LogInformation("[SVC] Patient.Restore completed - PatientId={PatientId} Name={Name}", id, entity.Name);
            return Result<PatientDetailDto>.Success(dto);
        }

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <inheritdoc/>
        /// <remarks>
        /// eliminate-service-catch-return: 保留项级错误隔离，修复ERR-012违规
        /// </remarks>
        public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
        {
            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            foreach (var id in ids)
            {
                try
                {
                    var entity = await _repository.GetByIdAsync(id);
                    if (entity == null)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "患者不存在"
                        });
                        continue;
                    }

                    // X7: 批量删除逐个引用检查
                    var refCount = await _dbContext.MedicalCases
                        .CountAsync(mc => mc.PatientId == id && !mc.IsDeleted);
                    if (refCount > 0)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = $"患者有 {refCount} 条医案记录，无法删除"
                        });
                        continue;
                    }

                    // 软删除
                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.Now;
                    await _repository.UpdateAsync(entity);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("[SVC] Patient.BatchDelete → ItemSuccess - PatientId={PatientId} Name={Name}", id, entity.Name);
                }
                catch (Exception ex)
                {
                    // 项级错误隔离：单项失败不影响其他项
                    // ERR-012: 使用安全消息替代ex.Message
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "删除操作失败"
                    });
                    _logger.LogError(ex, "[SVC] Patient.BatchDelete → ItemFailed - PatientId={PatientId}", id);
                }
            }

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

            return Result<BatchOperationResultDto>.Success(result);
        }

        // ========== OpenSpec: implement-data-sync - 引用检查 ==========

        /// <summary>
        /// 检查患者是否被医案引用
        /// </summary>
        public async Task<Result<PatientReferenceCheckDto>> CheckReferenceAsync(Guid patientId)
        {
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理
            var patient = await _repository.GetByIdAsync(patientId);
            if (patient == null)
            {
                return Result<PatientReferenceCheckDto>.Failure(GenericErrorCode.PatientNotFound);
            }

            // 查询医案引用计数
            var referenceCount = await _dbContext.MedicalCases
                .CountAsync(mc => mc.PatientId == patientId && !mc.IsDeleted);

            // 获取最近5条引用记录
            var recentMedicalCases = await _dbContext.MedicalCases
                .Where(mc => mc.PatientId == patientId && !mc.IsDeleted)
                .OrderByDescending(mc => mc.CreatedAt)
                .Take(5)
                .Select(mc => new MedicalCaseReferenceDto
                {
                    MedicalCaseId = mc.Id,
                    CaseNumber = mc.CaseNumber ?? string.Empty,
                    CreatedAt = mc.CreatedAt,
                    Status = mc.CaseStatus.ToString()
                })
                .ToListAsync();

            var hasReferences = referenceCount > 0;
            var result = new PatientReferenceCheckDto
            {
                PatientId = patientId,
                PatientName = patient.Name,
                HasReferences = hasReferences,
                ReferenceCount = referenceCount,
                CanDelete = !hasReferences, // X7: 有引用不可删除
                DeleteWarning = hasReferences ? $"该患者已有 {referenceCount} 个医案记录，无法删除" : null,
                RecentMedicalCases = recentMedicalCases
            };

            _logger.LogInformation("[SVC] Patient.CheckReference completed - PatientName={PatientName} HasReferences={HasReferences} ReferenceCount={ReferenceCount}",
                patient.Name, hasReferences, referenceCount);

            return Result<PatientReferenceCheckDto>.Success(result);
        }

        /// <summary>
        /// 批量检查患者引用关系
        /// </summary>
        public async Task<Result<List<PatientReferenceCheckDto>>> BatchCheckReferenceAsync(List<Guid> patientIds)
        {
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理
            const int MAX_CHECK_SIZE = 100;

            // 批量检查数量限制
            if (patientIds.Count > MAX_CHECK_SIZE)
            {
                return Result<List<PatientReferenceCheckDto>>.Failure(GenericErrorCode.ValidationFailed, $"批量检查最多支持{MAX_CHECK_SIZE}条记录");
            }

            var results = new List<PatientReferenceCheckDto>();

            foreach (var patientId in patientIds)
            {
                var checkResult = await CheckReferenceAsync(patientId);
                if (checkResult.IsSuccess && checkResult.Data != null)
                {
                    results.Add(checkResult.Data);
                }
            }

            _logger.LogInformation("[SVC] Patient.BatchCheckReference completed - Count={Count}", results.Count);

            return Result<List<PatientReferenceCheckDto>>.Success(results);
        }
    }
}
