using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Shared.Interfaces.Services
{

    /// <summary>
    /// 医疗案例服务接口 - UltraThink统一标准
    /// </summary>
    public interface IMedicalCaseService
    {

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 创建新的医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);

        /// <summary>
        /// 删除医疗案例
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取医疗案例
        /// </summary>
        Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取患者的活跃医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 完成医疗案例
        /// </summary>
        Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason);

        /// <summary>
        /// 暂停医疗案例
        /// </summary>
        Task<ServiceResult<bool>> Suspend(Guid id, string reason);

        /// <summary>
        /// 恢复医疗案例
        /// </summary>
        Task<ServiceResult<bool>> Resume(Guid id);

        /// <summary>
        /// 取消咨询/诊断
        /// </summary>
        Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason);

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        Task<ServiceResult<bool>> UpdateStatus(Guid id, int status);

        /// <summary>
        /// 归档医疗案例
        /// </summary>
        Task<ServiceResult<bool>> Archive(Guid id, string archiveReason);

        /// <summary>
        /// 获取医疗案例统计信息
        /// </summary>
        Task<ServiceResult<object>> GetStatistics(DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// 搜索医疗案例
        /// </summary>
        Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取医疗案例历史记录
        /// </summary>
        Task<ServiceResult<List<object>>> GetHistory(Guid id);
    }
}
