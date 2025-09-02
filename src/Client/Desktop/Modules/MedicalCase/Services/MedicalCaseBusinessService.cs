using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Desktop.MedicalCase.Interfaces;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案业务服务实现 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public class MedicalCaseBusinessService(ILogger<MedicalCaseBusinessService> logger) : IMedicalCaseBusinessService
{
    private readonly ILogger<MedicalCaseBusinessService> _logger = logger;

    #region 基础医案业务操作 - 简化实现

    /// <summary>
    /// 创建医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
    {
        return ServiceResult<MedicalCaseDto>.Failure("简单诊所版本暂不支持创建医案");
    }

    /// <summary>
    /// 更新医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseDetailDto dto)
    {
        return ServiceResult<MedicalCaseDto>.Failure("简单诊所版本暂不支持更新医案");
    }

    /// <summary>
    /// 删除医案
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 开始医案
    /// </summary>
    public async Task<ServiceResult<bool>> StartAsync(Guid id)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 完成医案
    /// </summary>
    public async Task<ServiceResult<bool>> CompleteAsync(Guid id)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 取消医案
    /// </summary>
    public async Task<ServiceResult<bool>> CancelAsync(Guid id)
    {
        return ServiceResult<bool>.Success(false);
    }

    #endregion
}