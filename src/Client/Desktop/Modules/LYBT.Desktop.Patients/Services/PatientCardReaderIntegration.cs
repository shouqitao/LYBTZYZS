using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者读卡器集成服务实现
/// OpenSpec: integrate-cardreader-module - 实现IPatientCardReaderIntegration接口
/// 职责：将读卡结果与患者模块集成，支持查找和快速创建患者
/// </summary>
public class PatientCardReaderIntegration : IPatientCardReaderIntegration
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientCardReaderIntegration> _logger;

    public PatientCardReaderIntegration(
        IPatientRepository patientRepository,
        ILogger<PatientCardReaderIntegration> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;
    }

    /// <summary>
    /// 根据身份证号查找患者
    /// </summary>
    public async Task<PatientFromCardResult?> FindPatientByIdNumberAsync(string idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            _logger.LogWarning("身份证号为空，无法查找患者");
            return null;
        }

        try
        {
            _logger.LogInformation("开始根据身份证号查找患者");

            var patient = await _patientRepository.GetByIdNumberAsync(idNumber);
            if (patient == null)
            {
                _logger.LogInformation("未找到匹配的患者");
                return null;
            }

            return new PatientFromCardResult
            {
                PatientId = patient.Id,
                Name = patient.Name,
                IdNumber = patient.IdNumber ?? string.Empty,
                IsNewlyCreated = false,
                LastVisitTime = patient.LastVisitTime,
                VisitCount = patient.VisitCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查找患者时发生异常");
            return null;
        }
    }

    /// <summary>
    /// 根据读卡结果快速创建患者
    /// 注意：身份证不含电话号码，创建的患者需要后续补充电话等必填信息
    /// </summary>
    public async Task<Guid> QuickCreatePatientAsync(CardReadResult cardResult)
    {
        ArgumentNullException.ThrowIfNull(cardResult);

        if (!cardResult.IsSuccess)
        {
            throw new InvalidOperationException($"读卡失败，无法创建患者: {cardResult.ErrorMessage}");
        }

        try
        {
            _logger.LogInformation("从读卡结果创建患者：{Name}", cardResult.Name);

            var patientInput = MapCardResultToPatientInput(cardResult);
            var createdPatient = await _patientRepository.CreateAsync(patientInput);

            _logger.LogInformation("患者创建成功：{PatientId}, {Name}", createdPatient.Id, createdPatient.Name);
            return createdPatient.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者时发生异常");
            throw;
        }
    }

    /// <summary>
    /// 查找或创建患者
    /// 先根据身份证号查找，如果不存在则创建
    /// </summary>
    public async Task<PatientFromCardResult> FindOrCreatePatientAsync(CardReadResult cardResult)
    {
        ArgumentNullException.ThrowIfNull(cardResult);

        if (!cardResult.IsSuccess)
        {
            throw new InvalidOperationException($"读卡失败，无法处理: {cardResult.ErrorMessage}");
        }

        // 1. 先尝试查找现有患者
        var existingPatient = await FindPatientByIdNumberAsync(cardResult.IdNumber);
        if (existingPatient != null)
        {
            _logger.LogInformation("找到现有患者：{PatientId}", existingPatient.PatientId);
            return existingPatient;
        }

        // 2. 未找到则创建新患者
        _logger.LogInformation("未找到现有患者，开始创建新患者");
        var newPatientId = await QuickCreatePatientAsync(cardResult);

        // 3. 获取新创建的患者信息
        var newPatient = await _patientRepository.GetByIdAsync(newPatientId);
        if (newPatient == null)
        {
            throw new InvalidOperationException("创建患者后无法获取患者信息");
        }

        return new PatientFromCardResult
        {
            PatientId = newPatient.Id,
            Name = newPatient.Name,
            IdNumber = newPatient.IdNumber ?? string.Empty,
            IsNewlyCreated = true,
            LastVisitTime = null,
            VisitCount = 0
        };
    }

    /// <summary>
    /// 将读卡结果映射为患者输入DTO
    /// </summary>
    private static PatientInputDto MapCardResultToPatientInput(CardReadResult cardResult)
    {
        return new PatientInputDto
        {
            Name = cardResult.Name,
            IdNumber = cardResult.IdNumber,
            Gender = cardResult.Gender,
            BirthDate = cardResult.BirthDate,
            Address = cardResult.Address,
            // 身份证不包含电话号码，需要后续补充
            PhoneNumber = null,
            // 其他可选字段
            AllergyHistory = null,
            MedicalHistory = null
        };
    }

    /// <summary>
    /// 根据患者ID获取患者详情
    /// OpenSpec: integrate-cardreader-module - 供ViewModel获取完整患者信息
    /// </summary>
    public async Task<PatientDetailDto?> GetPatientDetailByIdAsync(Guid patientId)
    {
        if (patientId == Guid.Empty)
        {
            _logger.LogWarning("患者ID为空，无法获取详情");
            return null;
        }

        try
        {
            _logger.LogInformation("获取患者详情：{PatientId}", patientId);
            return await _patientRepository.GetByIdAsync(patientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情时发生异常：{PatientId}", patientId);
            return null;
        }
    }
}
