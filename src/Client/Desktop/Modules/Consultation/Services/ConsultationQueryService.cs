using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Services;

/// <summary>
/// 看诊查询服务实现 - UltraThink双层架构简化版
/// 职责：查询和搜索操作
/// </summary>
public class ConsultationQueryService(ILogger<ConsultationQueryService> logger) : IConsultationQueryService
{
    private readonly ILogger<ConsultationQueryService> _logger = logger;

    /// <summary>
    /// 分页查询看诊记录
    /// </summary>
    public Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(ConsultationPagedQueryDto query)
    {
        var emptyResult = new PagedResult<ConsultationDto>
        {
            Items = new List<ConsultationDto>(),
            TotalCount = 0
        };
        
        return Task.FromResult(ServiceResult<PagedResult<ConsultationDto>>.Success(emptyResult));
    }

    /// <summary>
    /// 根据ID获取看诊详情
    /// </summary>
    public Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
    {
        return Task.FromResult(ServiceResult<ConsultationDto>.Failure("简单诊所版本暂不支持看诊查询"));
    }

    /// <summary>
    /// 搜索看诊记录
    /// </summary>
    public Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
    {
        var emptyList = new List<ConsultationDto>();
        return Task.FromResult(ServiceResult<List<ConsultationDto>>.Success(emptyList));
    }

    /// <summary>
    /// 获取看诊统计
    /// </summary>
    public Task<ServiceResult<ConsultationStatisticsDto>> GetStatisticsAsync()
    {
        var stats = new ConsultationStatisticsDto();
        return Task.FromResult(ServiceResult<ConsultationStatisticsDto>.Success(stats));
    }
}