using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者服务 - 简化版，只包含基础CRUD
    /// 同时实现 Module 内部接口和 Shared 跨平台接口
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            IPatientRepository repository,
            IMapper mapper,
            ILogger<PatientService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                // Bug #1587修复：支持关键字搜索（姓名/拼音码/手机号）
                var pagedResult = await _repository.GetPagedAsync(
                    string.IsNullOrWhiteSpace(keyword) ? null : 
                        p => p.Name.Contains(keyword) || 
                             (p.PinYinCode != null && p.PinYinCode.Contains(keyword)) || 
                             (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)),
                    page,
                    pageSize,
                    p => p.CreatedAt,
                    ascending: false
                );

                var dto = new PagedResult<PatientDto>
                {
                    Items = _mapper.Map<List<PatientDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<PatientDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者列表失败，关键字：{Keyword}", keyword);
                return ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
            }
        }

        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<PatientDto>.Failure("患者不存在");

                var dto = _mapper.Map<PatientDto>(entity);
                return ServiceResult<PatientDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者详情失败");
                return ServiceResult<PatientDto>.Failure("获取患者详情失败");
            }
        }

        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<Patient>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<PatientDto>(result);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者失败");
                return ServiceResult<PatientDto>.Failure("创建患者失败");
            }
        }

        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<PatientDto>.Failure("患者不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<PatientDto>(result);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者失败");
                return ServiceResult<PatientDto>.Failure("更新患者失败");
            }
        }

        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        {
            try
            {
                // 如果关键字为空，返回空列表
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
                }

                // 搜索匹配关键字的患者（姓名、电话或身份证号）
                var allPatients = await _repository.GetAllAsync();
                var patients = allPatients.Where(p =>
                    p.Name.Contains(keyword)).ToList();

                // 转换为DTO
                var patientDtos = _mapper.Map<List<PatientDto>>(patients);

                return ServiceResult<List<PatientDto>>.Success(patientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者时发生错误，关键字：{Keyword}", keyword);
                return ServiceResult<List<PatientDto>>.Failure($"搜索患者失败：{ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败");
                return ServiceResult.Failure("删除患者失败");
            }
        }

        /// <summary>
        /// 从Excel文件导入患者数据 (Issue #1165)
        /// </summary>
        public async Task<ServiceResult<ImportResultDto<PatientDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null)
        {
            var result = new ImportResultDto<PatientDto>
            {
                FileName = fileName,
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
                    result.IsSuccess = false;
                    result.Message = "Excel文件中没有工作表";
                    return ServiceResult<ImportResultDto<PatientDto>>.Failure("Excel文件格式错误");
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount <= 1)
                {
                    result.IsSuccess = false;
                    result.Message = "Excel文件中没有数据行";
                    return ServiceResult<ImportResultDto<PatientDto>>.Success(result);
                }

                result.TotalCount = rowCount - 1; // 排除表头

                // 从第2行开始读取数据（第1行是表头）
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var name = worksheet.Cells[row, 1].Text?.Trim();
                        var genderText = worksheet.Cells[row, 2].Text?.Trim();
                        var birthDateText = worksheet.Cells[row, 3].Text?.Trim();
                        var idCard = worksheet.Cells[row, 4].Text?.Trim();
                        var phoneNumber = worksheet.Cells[row, 5].Text?.Trim();
                        var address = worksheet.Cells[row, 6].Text?.Trim();

                        // 验证必填字段
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            result.FailureCount++;
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = $"第{row}行",
                                ErrorMessage = "姓名不能为空"
                            });
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(phoneNumber))
                        {
                            result.FailureCount++;
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = $"第{row}行",
                                ErrorMessage = "联系电话不能为空"
                            });
                            continue;
                        }

                        // 验证电话号码格式（11位数字）
                        if (phoneNumber.Length != 11 || !phoneNumber.All(char.IsDigit))
                        {
                            result.FailureCount++;
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = $"第{row}行",
                                ErrorMessage = "联系电话格式错误（需要11位数字）"
                            });
                            continue;
                        }

                        // 解析性别
                        Gender gender = Gender.Unknown;
                        if (!string.IsNullOrWhiteSpace(genderText))
                        {
                            if (genderText == "男")
                                gender = Gender.Male;
                            else if (genderText == "女")
                                gender = Gender.Female;
                            else
                            {
                                result.FailureCount++;
                                result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                                {
                                    RecordIdentifier = $"第{row}行",
                                    ErrorMessage = "性别格式错误（应为'男'或'女'）"
                                });
                                continue;
                            }
                        }

                        // 解析出生日期
                        DateTime? birthDate = null;
                        if (!string.IsNullOrWhiteSpace(birthDateText))
                        {
                            if (!DateTime.TryParse(birthDateText, out var parsedDate))
                            {
                                result.FailureCount++;
                                result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                                {
                                    RecordIdentifier = $"第{row}行",
                                    ErrorMessage = "出生日期格式错误（应为YYYY-MM-DD）"
                                });
                                continue;
                            }

                            if (parsedDate > DateTime.Today)
                            {
                                result.FailureCount++;
                                result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                                {
                                    RecordIdentifier = $"第{row}行",
                                    ErrorMessage = "出生日期不能晚于今天"
                                });
                                continue;
                            }

                            birthDate = parsedDate;
                        }

                        // 验证身份证号格式
                        if (!string.IsNullOrWhiteSpace(idCard))
                        {
                            if (idCard.Length != 18)
                            {
                                result.FailureCount++;
                                result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                                {
                                    RecordIdentifier = $"第{row}行",
                                    ErrorMessage = "身份证号格式错误（应为18位）"
                                });
                                continue;
                            }
                        }

                        // 创建患者实体
                        var patient = new Patient
                        {
                            Name = name,
                            Gender = gender,
                            BirthDate = birthDate,
                            IdNumber = idCard,
                            PhoneNumber = phoneNumber,
                            Address = address,
                            CreatedAt = DateTime.Now
                        };

                        // 保存到数据库
                        var savedPatient = await _repository.AddAsync(patient);
                        var patientDto = _mapper.Map<PatientDto>(savedPatient);

                        result.SuccessCount++;
                        result.SuccessfulIds.Add(savedPatient.Id);
                        result.ImportedData.Add(patientDto);
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                        {
                            RecordIdentifier = $"第{row}行",
                            ErrorMessage = $"导入失败：{ex.Message}"
                        });
                        _logger.LogError(ex, "导入第{Row}行时发生错误", row);
                    }
                }

                result.IsSuccess = true;
                result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

                return ServiceResult<ImportResultDto<PatientDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入患者数据时发生错误");
                result.IsSuccess = false;
                result.Message = $"导入失败：{ex.Message}";
                return ServiceResult<ImportResultDto<PatientDto>>.Failure($"导入失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 生成患者导入模板 (Issue #1165)
        /// </summary>
        public MemoryStream GenerateImportTemplate()
        {
            try
            {
                // 设置EPPlus许可证上下文（非商业用途）
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var stream = new MemoryStream();
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.Add("患者信息");

                    // 设置表头
                    worksheet.Cells[1, 1].Value = "姓名*";
                    worksheet.Cells[1, 2].Value = "性别";
                    worksheet.Cells[1, 3].Value = "出生日期";
                    worksheet.Cells[1, 4].Value = "身份证号";
                    worksheet.Cells[1, 5].Value = "联系电话*";
                    worksheet.Cells[1, 6].Value = "地址";

                    // 表头样式
                    using (var range = worksheet.Cells[1, 1, 1, 6])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 添加示例数据
                    worksheet.Cells[2, 1].Value = "张三";
                    worksheet.Cells[2, 2].Value = "男";
                    worksheet.Cells[2, 3].Value = "1980-01-01";
                    worksheet.Cells[2, 4].Value = "110101198001011234";
                    worksheet.Cells[2, 5].Value = "13800138000";
                    worksheet.Cells[2, 6].Value = "北京市朝阳区";

                    // 自动调整列宽
                    worksheet.Cells.AutoFitColumns();

                    package.Save();
                }

                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成导入模板时发生错误");
                throw;
            }
        }
    }
}
