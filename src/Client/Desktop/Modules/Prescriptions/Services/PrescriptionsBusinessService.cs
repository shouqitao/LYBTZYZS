using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方业务服务实现 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public class PrescriptionsBusinessService(ILogger<PrescriptionsBusinessService> logger) : IPrescriptionsBusinessService
{
    private readonly ILogger<PrescriptionsBusinessService> _logger = logger;

    /// <summary>
    /// 创建处方
    /// </summary>
    public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto createDto)
    {
        return ServiceResult<PrescriptionDto>.Failure("简单诊所版本暂不支持创建处方");
    }

    /// <summary>
    /// 更新处方
    /// </summary>
    public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto updateDto)
    {
        return ServiceResult<PrescriptionDto>.Failure("简单诊所版本暂不支持更新处方");
    }

    /// <summary>
    /// 删除处方
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid prescriptionId)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 启用处方
    /// </summary>
    public async Task<ServiceResult<bool>> EnableAsync(Guid prescriptionId)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 禁用处方
    /// </summary>
    public async Task<ServiceResult<bool>> DisableAsync(Guid prescriptionId)
    {
        return ServiceResult<bool>.Success(false);
    }
}