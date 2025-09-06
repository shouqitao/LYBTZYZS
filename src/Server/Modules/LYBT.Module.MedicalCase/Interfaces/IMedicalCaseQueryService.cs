using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Interfaces {

    /// <summary>
    /// 医疗案例查询服务接口
    /// UltraThink架构 - Query层接口抽象
    /// 职责：医疗案例查询、搜索、统计功能专业化处理
    /// </summary>
    public interface IMedicalCaseQueryService {

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid caseId);

        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据患者ID获取活跃医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 搜索医疗案例
        /// </summary>
        Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取历史医疗案例
        /// </summary>
        Task<ServiceResult<List<MedicalCaseDto>>> GetHistoryAsync(Guid patientId);

        /// <summary>
        /// 检查是否有活跃案例
        /// </summary>
        Task<ServiceResult<bool>> HasActiveCaseAsync(Guid patientId);

        /// <summary>
        /// 获取医疗案例统计信息
        /// </summary>
        Task<ServiceResult<object>> GetStatisticsAsync();
    }
}
