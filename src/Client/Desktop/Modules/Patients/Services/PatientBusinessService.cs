using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者业务服务实现 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public class PatientBusinessService(ILogger<PatientBusinessService> logger) : IPatientBusinessService
{
    private readonly ILogger<PatientBusinessService> _logger = logger;

    /// <summary>
    /// 创建患者
    /// </summary>
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto)
    {
        return ServiceResult<PatientDto>.Failure("简单诊所版本暂不支持创建患者");
    }

    /// <summary>
    /// 更新患者
    /// </summary>
    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto)
    {
        return ServiceResult<PatientDto>.Failure("简单诊所版本暂不支持更新患者信息");
    }

    /// <summary>
    /// 启用患者
    /// </summary>
    public async Task<ServiceResult<bool>> EnableAsync(Guid patientId)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 禁用患者
    /// </summary>
    public async Task<ServiceResult<bool>> DisableAsync(Guid patientId)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 删除患者
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid patientId)
    {
        return ServiceResult<bool>.Success(false);
    }
}