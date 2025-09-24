using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services
{

    /// <summary>
    /// 患者业务服务实现
    /// UltraThink架构 - Business层接口抽象
    /// 职责：患者业务逻辑、CRUD操作、状态管理
    /// </summary>
    public class PatientBusinessService : IPatientBusinessService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientBusinessService> _logger;

        public PatientBusinessService(
            IPatientRepository patientRepository,
            IMapper mapper,
            ILogger<PatientBusinessService> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建患者
        /// </summary>
        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // 数据验证
                if (createDto == null)
                {
                    return ServiceResult<PatientDto>.Failure("患者信息不能为空");
                }

                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    return ServiceResult<PatientDto>.Failure("姓名不能为空");
                }

                // 检查重复手机号
                if (!string.IsNullOrWhiteSpace(createDto.PhoneNumber))
                {
                    var phoneExists = await _patientRepository.IsPhoneNumberExistsAsync(createDto.PhoneNumber);
                    if (phoneExists)
                    {
                        return ServiceResult<PatientDto>.Failure("手机号码已存在");
                    }
                }

                // 创建新患者
                var patient = _mapper.Map<Patient>(createDto);
                patient.Id = Guid.NewGuid();
                patient.Status = CommonStatus.Enabled;
                patient.PinYinCode = string.Empty; // 移除CommonHelper依赖，拼音码功能暂不实现


                var createdPatient = await _patientRepository.AddAsync(patient);

                _logger.LogInformation("创建患者成功: {Name} ({Id})", createdPatient.Name, createdPatient.Id);

                var resultDto = _mapper.Map<PatientDto>(createdPatient);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者失败: {Name}", createDto?.Name);
                return ServiceResult<PatientDto>.Failure($"创建患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid patientId, PatientUpdateDto updateDto)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<PatientDto>.Failure("患者ID不能为空");
                }

                if (updateDto == null)
                {
                    return ServiceResult<PatientDto>.Failure("更新信息不能为空");
                }

                var patient = await _patientRepository.GetByIdAsync(patientId, includeDisabled: true);

                if (patient == null)
                {
                    return ServiceResult<PatientDto>.Failure("患者不存在");
                }

                // 检查手机号重复（排除自己）
                if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                {
                    var phoneExists = await _patientRepository.IsPhoneNumberExistsAsync(updateDto.PhoneNumber, patientId);
                    if (phoneExists)
                    {
                        return ServiceResult<PatientDto>.Failure("手机号码已存在");
                    }
                }

                // 更新字段
                _mapper.Map(updateDto, patient);
                patient.PinYinCode = string.Empty; // 移除CommonHelper依赖，拼音码功能暂不实现


                var updatedPatient = await _patientRepository.UpdateAsync(patient);

                _logger.LogInformation("更新患者成功: {Name} ({Id})", patient.Name, patient.Id);

                var resultDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者失败: {Id}", patientId);
                return ServiceResult<PatientDto>.Failure($"更新患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除患者
        /// </summary>
        public async Task<ServiceResult<PatientDto>> DeleteAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<PatientDto>.Failure("患者ID不能为空");
                }

                var patient = await _patientRepository.GetByIdAsync(patientId, includeDisabled: true);

                if (patient == null)
                {
                    return ServiceResult<PatientDto>.Failure("患者不存在");
                }

                // 软删除 - 标记为已删除状态
                patient.Status = CommonStatus.Disabled;


                await _patientRepository.UpdateAsync(patient);

                _logger.LogInformation("删除患者成功: {Name} ({Id})", patient.Name, patient.Id);

                var resultDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败: {Id}", patientId);
                return ServiceResult<PatientDto>.Failure($"删除患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量删除患者
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(List<Guid> patientIds)
        {
            try
            {
                if (patientIds == null || !patientIds.Any())
                {
                    return ServiceResult<bool>.Failure("患者ID列表不能为空");
                }

                // 逐个软删除患者
                var count = 0;
                foreach (var id in patientIds)
                {
                    var patient = await _patientRepository.GetByIdAsync(id);
                    if (patient != null)
                    {
                        patient.Status = CommonStatus.Disabled;
        
                        await _patientRepository.UpdateAsync(patient);
                        count++;
                    }
                }
                
                _logger.LogInformation("批量删除患者成功 - 影响数量: {Count}", count);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除患者失败");
                return ServiceResult<bool>.Failure($"批量删除患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置患者状态
        /// </summary>
        public async Task<ServiceResult<bool>> SetStatusAsync(List<Guid> patientIds, string status)
        {
            try
            {
                if (patientIds == null || !patientIds.Any())
                {
                    return ServiceResult<bool>.Failure("患者ID列表不能为空");
                }

                var commonStatus = status.ToLower() == "enabled" ? CommonStatus.Enabled : CommonStatus.Disabled;
                // 逐个设置患者状态
                var count = 0;
                foreach (var id in patientIds)
                {
                    var patient = await _patientRepository.GetByIdAsync(id);
                    if (patient != null)
                    {
                        patient.Status = commonStatus;
        
                        await _patientRepository.UpdateAsync(patient);
                        count++;
                    }
                }

                _logger.LogInformation("批量设置患者状态成功 - 状态: {Status}, 影响数量: {Count}", status, count);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量设置患者状态失败");
                return ServiceResult<bool>.Failure($"批量设置患者状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启用患者
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(List<Guid> patientIds)
        {
            try
            {
                if (patientIds == null || !patientIds.Any())
                {
                    return ServiceResult<bool>.Failure("患者ID列表不能为空");
                }

                // 逐个启用患者
                var count = 0;
                foreach (var id in patientIds)
                {
                    var patient = await _patientRepository.GetByIdAsync(id);
                    if (patient != null)
                    {
                        patient.Status = CommonStatus.Enabled;
        
                        await _patientRepository.UpdateAsync(patient);
                        count++;
                    }
                }

                _logger.LogInformation("批量启用患者成功 - 影响数量: {Count}", count);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用患者失败");
                return ServiceResult<bool>.Failure($"批量启用患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 禁用患者
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(List<Guid> patientIds)
        {
            try
            {
                if (patientIds == null || !patientIds.Any())
                {
                    return ServiceResult<bool>.Failure("患者ID列表不能为空");
                }

                // 检查是否有活跃的医疗案例 - 暂时简化处理
                var count = 0;
                foreach (var id in patientIds)
                {
                    var patient = await _patientRepository.GetByIdAsync(id);
                    if (patient != null)
                    {
                        patient.Status = CommonStatus.Disabled;
        
                        await _patientRepository.UpdateAsync(patient);
                        count++;
                    }
                }

                _logger.LogInformation("批量禁用患者成功 - 影响数量: {Count}", count);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用患者失败");
                return ServiceResult<bool>.Failure($"批量禁用患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导入患者数据
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> ImportPatientsAsync(List<PatientImportDto> importDtos)
        {
            try
            {
                if (importDtos == null || !importDtos.Any())
                {
                    return ServiceResult<List<PatientDto>>.Failure("导入数据不能为空");
                }

                var successfulPatients = new List<PatientDto>();
                var errors = new List<string>();

                foreach (var (importDto, index) in importDtos.Select((dto, i) => (dto, i)))
                {
                    try
                    {
                        // 检查重复手机号
                        if (!string.IsNullOrEmpty(importDto.PhoneNumber))
                        {
                            var phoneExists = await _patientRepository.IsPhoneNumberExistsAsync(importDto.PhoneNumber);
                            if (phoneExists)
                            {
                                errors.Add($"行 {index + 1}: 患者 {importDto.Name} 手机号 {importDto.PhoneNumber} 已存在");
                                continue;
                            }
                        }

                        // 解析性别
                        var gender = importDto.GenderText?.ToLower() switch
                        {
                            "男" or "male" => Gender.Male,
                            "女" or "female" => Gender.Female,
                            _ => Gender.Male
                        };

                        // 解析出生日期
                        DateTime birthDate = DateTime.Today.AddYears(-30);
                        if (!string.IsNullOrEmpty(importDto.BirthDateText))
                        {
                            DateTime.TryParse(importDto.BirthDateText, out birthDate);
                        }
                        else if (importDto.Age.HasValue)
                        {
                            birthDate = DateTime.Today.AddYears(-importDto.Age.Value);
                        }

                        var patient = new Patient
                        {
                            Id = Guid.NewGuid(),
                            Name = importDto.Name,
                            Gender = gender,
                            BirthDate = birthDate,
                            PhoneNumber = importDto.PhoneNumber,
                            IdNumber = importDto.IdCardNumber,
                            Address = importDto.Address,
                            EmergencyContactName = importDto.EmergencyContactName,
                            EmergencyContactPhone = importDto.EmergencyContactPhone,
                            AllergyHistory = importDto.AllergyHistory,
                            Status = CommonStatus.Enabled,
                            PinYinCode = string.Empty,

                        };

                        var createdPatient = await _patientRepository.AddAsync(patient);
                        var patientDto = _mapper.Map<PatientDto>(createdPatient);
                        successfulPatients.Add(patientDto);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"行 {index + 1}: 患者 {importDto.Name} 导入失败: {ex.Message}");
                        _logger.LogError(ex, "导入患者失败: {Name}", importDto.Name);
                    }
                }

                _logger.LogInformation("患者批量导入完成 - 成功: {SuccessCount}, 失败: {ErrorCount}", 
                    successfulPatients.Count, errors.Count);

                return ServiceResult<List<PatientDto>>.Success(successfulPatients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入患者异常");
                return ServiceResult<List<PatientDto>>.Failure($"批量导入患者异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> ExportPatientsAsync(PatientExportDto exportDto)
        {
            try
            {
                // 简化导出查询 - 获取所有患者然后过滤
                var allPatients = await _patientRepository.GetAllAsync();
                var patients = allPatients.AsQueryable();

                if (!string.IsNullOrWhiteSpace(exportDto.Name))
                {
                    patients = patients.Where(p => p.Name.Contains(exportDto.Name));
                }

                if (!string.IsNullOrWhiteSpace(exportDto.PhoneNumber))
                {
                    patients = patients.Where(p => p.PhoneNumber != null && p.PhoneNumber.Contains(exportDto.PhoneNumber));
                }

                var filteredPatients = patients.OrderBy(p => p.Name).ToList();

                var patientDtos = _mapper.Map<List<PatientDto>>(filteredPatients);

                _logger.LogInformation("导出患者数据成功 - 导出数量: {Count}", patientDtos.Count);
                return ServiceResult<List<PatientDto>>.Success(patientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出患者数据失败");
                return ServiceResult<List<PatientDto>>.Failure($"导出患者数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证患者数据
        /// </summary>
        public async Task<ServiceResult<List<string>>> ValidatePatientAsync(PatientCreateDto createDto)
        {
            try
            {
                var validationResults = new List<string>();

                // 基础验证
                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    validationResults.Add("姓名不能为空");
                }

                if (string.IsNullOrWhiteSpace(createDto.PhoneNumber))
                {
                    validationResults.Add("手机号码不能为空");
                }
                else if (createDto.PhoneNumber.Length != 11)
                {
                    validationResults.Add("手机号码格式不正确");
                }

                if (createDto.BirthDate == default)
                {
                    validationResults.Add("出生日期不能为空");
                }
                else if (createDto.BirthDate > DateTime.Today)
                {
                    validationResults.Add("出生日期不能大于当前日期");
                }

                // 检查重复
                if (!string.IsNullOrWhiteSpace(createDto.PhoneNumber))
                {
                    var phoneExists = await _patientRepository.IsPhoneNumberExistsAsync(createDto.PhoneNumber);
                    if (phoneExists)
                    {
                        validationResults.Add("手机号码已存在");
                    }
                }

                if (!string.IsNullOrWhiteSpace(createDto.IdNumber))
                {
                    // 暂时简化处理 - 实际应该添加IsIdNumberExistsAsync方法到Repository
                    var existingPatients = await _patientRepository.GetAllAsync();
                    var idExists = existingPatients.Any(p => p.IdNumber == createDto.IdNumber);
                    if (idExists)
                    {
                        validationResults.Add("身份证号已存在");
                    }
                }

                return ServiceResult<List<string>>.Success(validationResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者信息失败");
                return ServiceResult<List<string>>.Failure($"验证患者信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取导入模板
        /// </summary>
        public Task<ServiceResult<object>> GetImportTemplate()
        {
            try
            {
                var template = new
                {
                    Headers = new[] { "姓名", "性别", "出生日期", "手机号码", "身份证号", "地址", "紧急联系人", "紧急联系人电话", "过敏史" },
                    SampleData = new[]
                    {
                        new { 姓名 = "张三", 性别 = "男", 出生日期 = "1990-01-01", 手机号码 = "13800138001", 身份证号 = "110101199001011234", 地址 = "北京市朝阳区", 紧急联系人 = "李四", 紧急联系人电话 = "13800138002", 过敏史 = "无" },
                        new { 姓名 = "王五", 性别 = "女", 出生日期 = "1985-05-15", 手机号码 = "13800138003", 身份证号 = "110101198505151234", 地址 = "北京市海淀区", 紧急联系人 = "赵六", 紧急联系人电话 = "13800138004", 过敏史 = "青霉素" }
                    }
                };

                _logger.LogInformation("获取患者导入模板成功");
                return Task.FromResult(ServiceResult<object>.Success(template));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者导入模板失败");
                return Task.FromResult(ServiceResult<object>.Failure($"获取患者导入模板失败: {ex.Message}"));
            }
        }
    }
}
