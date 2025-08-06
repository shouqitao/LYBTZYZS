using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentPlan;

namespace LYBT.Module.TreatmentPlan.Interfaces
{
    /// <summary>
    /// 治疗方案服务接口
    /// </summary>
    public interface ITreatmentPlanService
    {
        /// <summary>
        /// 获取治疗方案列表
        /// </summary>
        Task<List<TreatmentPlanDto>> GetListAsync();

        /// <summary>
        /// 分页获取治疗方案列表
        /// </summary>
        Task<PaginatedResult<TreatmentPlanDto>> GetPagedAsync(PaginationRequest request);

        /// <summary>
        /// 获取治疗方案详情
        /// </summary>
        Task<TreatmentPlanDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建治疗方案
        /// </summary>
        Task<TreatmentPlanDetailDto> CreateAsync(TreatmentPlanCreateDto dto);

        /// <summary>
        /// 更新治疗方案
        /// </summary>
        Task<bool> UpdateAsync(Guid id, TreatmentPlanUpdateDto dto);

        /// <summary>
        /// 删除治疗方案
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取治疗方案
        /// </summary>
        Task<TreatmentPlanDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 添加处方到治疗方案
        /// </summary>
        Task<bool> AddPrescriptionAsync(Guid id, PrescriptionDto prescription);

        /// <summary>
        /// 添加理疗项目到治疗方案
        /// </summary>
        Task<bool> AddPhysiotherapyItemAsync(Guid id, PhysiotherapyItemDto item);

        /// <summary>
        /// 移除处方
        /// </summary>
        Task<bool> RemovePrescriptionAsync(Guid id);

        /// <summary>
        /// 移除理疗项目
        /// </summary>
        Task<bool> RemovePhysiotherapyItemAsync(Guid id, Guid itemId);

        /// <summary>
        /// 确认治疗方案
        /// </summary>
        Task<bool> ConfirmPlanAsync(Guid id);
    }
}