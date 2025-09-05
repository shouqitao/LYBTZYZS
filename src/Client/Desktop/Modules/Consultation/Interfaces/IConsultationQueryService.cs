using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces;

/// <summary>
/// 看诊查询服务接口 - UltraThink双层架构简化版
/// 职责：查询和搜索操作
/// </summary>
public interface IConsultationQueryService
{
    /// <summary>
    /// 分页查询看诊记录
    /// </summary>
    Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(ConsultationPagedQueryDto query);

    /// <summary>
    /// 根据ID获取看诊详情
    /// </summary>
    Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 搜索看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 获取看诊统计
    /// </summary>
    Task<ServiceResult<ConsultationStatisticsDto>> GetStatisticsAsync();
}
