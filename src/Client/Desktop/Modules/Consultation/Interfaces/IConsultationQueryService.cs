using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces;

/// <summary>
/// 诊疗查询服务接口 - UltraThink双层架构简化版
/// 职责：查询和搜索操作
/// </summary>
public interface IConsultationQueryService
{

    /// <summary>
    /// 分页查询诊疗记录
    /// </summary>
    Task<ServiceResult<PagedResult<ConsultationDto>>> GetPaged(ConsultationSearchDto query);

    /// <summary>
    /// 根据ID获取诊疗详情
    /// </summary>
    Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 搜索诊疗记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 获取诊疗统计
    /// </summary>
    Task<ServiceResult<ConsultationStatisticsDto>> GetStatisticsAsync();
}
