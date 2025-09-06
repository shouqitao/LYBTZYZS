using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces;

/// <summary>
/// 看诊业务服务接口 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public interface IConsultationBusinessService {

    /// <summary>
    /// 创建看诊
    /// </summary>
    Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto createDto);

    /// <summary>
    /// 更新看诊
    /// </summary>
    Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto updateDto);

    /// <summary>
    /// 删除看诊
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid consultationId);

    /// <summary>
    /// 启用看诊
    /// </summary>
    Task<ServiceResult<bool>> EnableAsync(Guid consultationId);

    /// <summary>
    /// 禁用看诊
    /// </summary>
    Task<ServiceResult<bool>> DisableAsync(Guid consultationId);
}
