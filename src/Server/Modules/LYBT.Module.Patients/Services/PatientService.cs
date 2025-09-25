using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services;

/// <summary>
/// 患者服务 - 统一服务实现
/// 合并查询和业务逻辑，简化架构
/// </summary>
public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        IPatientRepository patientRepository,
        IMapper mapper,
        ILogger<PatientService> logger)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 查询操作

    /// <inheritdoc/>
    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientSearchDto query)
    {
        try
        {
            var pageIndex = Math.Max(query.PageIndex, 1);
            var pageSize = Math.Clamp(query.PageSize, 10, 100);

            // 获取分页数据
            var patients = await _patientRepository.GetPagedAsync(
                p => string.IsNullOrEmpty(query.Keyword) ||
                     p.Name.Contains(query.Keyword) ||
                     (p.PhoneNumber != null && p.PhoneNumber.Contains(query.Keyword)) ||
                     (p.IdNumber != null && p.IdNumber.Contains(query.Keyword)),
                pageIndex, 
                pageSize);

            var patientDtos = _mapper.Map<List<PatientDto>>(patients.Items);
            var pagedResult = new PagedResult<PatientDto>(
                patientDtos,
                patients.TotalCount,
                pageIndex,
                pageSize
            );

            return ServiceResult<PagedResult<PatientDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询患者失败");
            return ServiceResult<PagedResult<PatientDto>>.Failure($"分页查询患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<PatientDto>.Failure("患者ID不能为空");
            }

            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null)
            {
                return ServiceResult<PatientDto>.Failure("患者不存在");
            }

            var dto = _mapper.Map<PatientDto>(patient);
            return ServiceResult<PatientDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据ID获取患者失败: {Id}", id);
            return ServiceResult<PatientDto>.Failure($"根据ID获取患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(idCard))
            {
                return ServiceResult<PatientDto>.Failure("身份证号不能为空");
            }

            var patient = await _patientRepository.GetSingleAsync(p => p.IdNumber == idCard);
            if (patient == null)
            {
                return ServiceResult<PatientDto>.Failure("未找到患者");
            }

            var dto = _mapper.Map<PatientDto>(patient);
            return ServiceResult<PatientDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据身份证号获取患者失败: {IdCard}", idCard);
            return ServiceResult<PatientDto>.Failure($"根据身份证号获取患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return ServiceResult<List<PatientDto>>.Failure("手机号码不能为空");
            }

            var patients = await _patientRepository.FindAsync(p => p.PhoneNumber != null && p.PhoneNumber.Contains(phone));
            var dtos = _mapper.Map<List<PatientDto>>(patients);
            return ServiceResult<List<PatientDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据手机号获取患者列表失败: {Phone}", phone);
            return ServiceResult<List<PatientDto>>.Failure($"根据手机号获取患者列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            }

            var patients = await _patientRepository.FindAsync(
                p => p.Name.Contains(keyword) ||
                     (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                     (p.IdNumber != null && p.IdNumber.Contains(keyword)));
            patients = patients.Take(20).ToList();

            var dtos = _mapper.Map<List<PatientDto>>(patients);
            return ServiceResult<List<PatientDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索患者失败: {Keyword}", keyword);
            return ServiceResult<List<PatientDto>>.Failure($"搜索患者失败: {ex.Message}");
        }
    }

    #endregion 查询操作

    #region 业务操作

    /// <inheritdoc/>
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
    {
        try
        {
            // 数据验证
            if (dto == null)
            {
                return ServiceResult<PatientDto>.Failure("患者信息不能为空");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return ServiceResult<PatientDto>.Failure("姓名不能为空");
            }

            // 检查重复手机号
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var phoneExists = await _patientRepository.IsPhoneNumberExistsAsync(dto.PhoneNumber);
                if (phoneExists)
                {
                    return ServiceResult<PatientDto>.Failure("手机号码已存在");
                }
            }

            // 创建新患者
            var patient = _mapper.Map<Patient>(dto);
            patient.Id = Guid.NewGuid();
            patient.Status = CommonStatus.Enabled;
            patient.PinYinCode = string.Empty;

            var createdPatient = await _patientRepository.AddAsync(patient);

            _logger.LogInformation("创建患者成功: {Name} ({Id})", createdPatient.Name, createdPatient.Id);

            var resultDto = _mapper.Map<PatientDto>(createdPatient);
            return ServiceResult<PatientDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败: {Name}", dto?.Name);
            return ServiceResult<PatientDto>.Failure($"创建患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<PatientDto>.Failure("患者ID不能为空");
            }

            if (dto == null)
            {
                return ServiceResult<PatientDto>.Failure("更新信息不能为空");
            }

            var patient = await _patientRepository.GetByIdAsync(id, includeDisabled: true);
            if (patient == null)
            {
                return ServiceResult<PatientDto>.Failure("患者不存在");
            }

            // 检查手机号重复（排除自己）
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var phoneExists = await _patientRepository.IsPhoneNumberExistsAsync(dto.PhoneNumber, id);
                if (phoneExists)
                {
                    return ServiceResult<PatientDto>.Failure("手机号码已存在");
                }
            }

            // 更新字段
            _mapper.Map(dto, patient);
            patient.PinYinCode = string.Empty;

            var updatedPatient = await _patientRepository.UpdateAsync(patient);

            _logger.LogInformation("更新患者成功: {Name} ({Id})", patient.Name, patient.Id);

            var resultDto = _mapper.Map<PatientDto>(patient);
            return ServiceResult<PatientDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者失败: {Id}", id);
            return ServiceResult<PatientDto>.Failure($"更新患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("患者ID不能为空");
            }

            var patient = await _patientRepository.GetByIdAsync(id, includeDisabled: true);
            if (patient == null)
            {
                return ServiceResult<bool>.Failure("患者不存在");
            }

            // 软删除 - 标记为已删除状态
            patient.Status = CommonStatus.Disabled;
            await _patientRepository.UpdateAsync(patient);

            _logger.LogInformation("删除患者成功: {Name} ({Id})", patient.Name, patient.Id);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者失败: {Id}", id);
            return ServiceResult<bool>.Failure($"删除患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> EnableAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return ServiceResult.Failure("患者ID不能为空");
            }

            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null)
            {
                return ServiceResult.Failure("患者不存在");
            }

            patient.Status = CommonStatus.Enabled;
            await _patientRepository.UpdateAsync(patient);

            _logger.LogInformation("启用患者成功: {Name} ({Id})", patient.Name, patient.Id);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启用患者失败: {Id}", id);
            return ServiceResult.Failure($"启用患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> DisableAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return ServiceResult.Failure("患者ID不能为空");
            }

            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null)
            {
                return ServiceResult.Failure("患者不存在");
            }

            patient.Status = CommonStatus.Disabled;
            await _patientRepository.UpdateAsync(patient);

            _logger.LogInformation("禁用患者成功: {Name} ({Id})", patient.Name, patient.Id);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "禁用患者失败: {Id}", id);
            return ServiceResult.Failure($"禁用患者失败: {ex.Message}");
        }
    }

    #endregion 业务操作

    #region 批量操作

    /// <inheritdoc/>
    public async Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients)
    {
        try
        {
            if (patients == null || !patients.Any())
            {
                return ServiceResult<object>.Failure("导入数据不能为空");
            }

            var successfulPatients = new List<PatientDto>();
            var errors = new List<string>();

            foreach (var (patientDto, index) in patients.Select((dto, i) => (dto, i)))
            {
                try
                {
                    // 检查重复手机号
                    if (!string.IsNullOrEmpty(patientDto.PhoneNumber))
                    {
                        var phoneExists = await _patientRepository.IsPhoneNumberExistsAsync(patientDto.PhoneNumber);
                        if (phoneExists)
                        {
                            errors.Add($"行 {index + 1}: 患者 {patientDto.Name} 手机号 {patientDto.PhoneNumber} 已存在");
                            continue;
                        }
                    }

                    var patient = _mapper.Map<Patient>(patientDto);
                    patient.Id = Guid.NewGuid();
                    patient.Status = CommonStatus.Enabled;
                    patient.PinYinCode = string.Empty;

                    var createdPatient = await _patientRepository.AddAsync(patient);
                    var resultDto = _mapper.Map<PatientDto>(createdPatient);
                    successfulPatients.Add(resultDto);
                }
                catch (Exception ex)
                {
                    errors.Add($"行 {index + 1}: 患者 {patientDto.Name} 导入失败: {ex.Message}");
                    _logger.LogError(ex, "导入患者失败: {Name}", patientDto.Name);
                }
            }

            _logger.LogInformation("患者批量导入完成 - 成功: {SuccessCount}, 失败: {ErrorCount}",
                successfulPatients.Count, errors.Count);

            return ServiceResult<object>.Success(new { SuccessCount = successfulPatients.Count, ImportedPatients = successfulPatients, Errors = errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导入患者异常");
            return ServiceResult<object>.Failure($"批量导入患者异常: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query)
    {
        try
        {
            // 获取所有患者然后过滤
            var allPatients = await _patientRepository.GetAllAsync();
            var patients = allPatients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                patients = patients.Where(p => p.Name.Contains(query.Keyword) ||
                                             (p.PhoneNumber != null && p.PhoneNumber.Contains(query.Keyword)) ||
                                             (p.IdNumber != null && p.IdNumber.Contains(query.Keyword)));
            }

            var filteredPatients = patients.OrderBy(p => p.Name).ToList();
            var patientDtos = _mapper.Map<List<PatientDto>>(filteredPatients);

            // 转换为CSV字节数组
            var csvContent = "姓名,性别,出生日期,手机号码,身份证号,地址\n";

            foreach (var patient in patientDtos)
            {
                var gender = patient.Gender == Gender.Male ? "男" : "女";
                csvContent += $"{patient.Name},{gender},{patient.BirthDate:yyyy-MM-dd}," +
                             $"{patient.PhoneNumber},{patient.IdNumber},{patient.Address}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);

            _logger.LogInformation("导出患者数据成功 - 导出数量: {Count}", patientDtos.Count);
            return ServiceResult<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出患者数据失败");
            return ServiceResult<byte[]>.Failure($"导出患者数据失败: {ex.Message}");
        }
    }

    #endregion 批量操作
}