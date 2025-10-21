using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Server.Interfaces.Services
{
    /// <summary>
    /// 诊疗服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IConsultationService
    {
        /// <summary>
        /// 分页查询诊疗记录
        /// </summary>
        Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取诊疗详情
        /// </summary>
        Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新诊疗记录
        /// </summary>
        Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto);

        /// <summary>
        /// 更新诊疗记录
        /// </summary>
        Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto);

        /// <summary>
        /// 删除诊疗记录（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索诊疗记录 - 支持多条件搜索
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 根据医案ID获取诊疗记录列表
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        // Issue #1562 Phase 1: 已删除 StartAsync（工作流启动方法）
        // Issue #1562 Phase 1: 已删除 GetStatisticsAsync（统计功能属于过度设计）
    }
}
