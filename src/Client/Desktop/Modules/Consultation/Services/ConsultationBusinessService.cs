using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Services;

/// <summary>
/// 看诊业务服务实现 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public class ConsultationBusinessService(ILogger<ConsultationBusinessService> logger) : IConsultationBusinessService
{
    private readonly ILogger<ConsultationBusinessService> _logger = logger;

    /// <summary>
    /// 创建看诊
    /// </summary>
    public Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto createDto)
    {
        return Task.FromResult(ServiceResult<ConsultationDto>.Failure("简单诊所版本暂不支持创建看诊"));
    }

    /// <summary>
    /// 更新看诊
    /// </summary>
    public Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto updateDto)
    {
        return Task.FromResult(ServiceResult<ConsultationDto>.Failure("简单诊所版本暂不支持更新看诊"));
    }

    /// <summary>
    /// 删除看诊
    /// </summary>
    public Task<ServiceResult<bool>> DeleteAsync(Guid consultationId)
    {
        return Task.FromResult(ServiceResult<bool>.Success(false));
    }

    /// <summary>
    /// 启用看诊
    /// </summary>
    public Task<ServiceResult<bool>> EnableAsync(Guid consultationId)
    {
        return Task.FromResult(ServiceResult<bool>.Success(false));
    }

    /// <summary>
    /// 禁用看诊
    /// </summary>
    public Task<ServiceResult<bool>> DisableAsync(Guid consultationId)
    {
        return Task.FromResult(ServiceResult<bool>.Success(false));
    }
}