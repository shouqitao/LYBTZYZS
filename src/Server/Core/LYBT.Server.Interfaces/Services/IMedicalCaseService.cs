using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Server.Interfaces.Services
{
    /// <summary>
    /// 医疗案例服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IMedicalCaseService
    {
        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);

        /// <summary>
        /// 更新医疗案例信息
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除医疗案例（软删除）(Issue #1169)
        /// </summary>
        /// <param name="ids">医疗案例ID列表</param>
        Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 创建完整的医疗案例（包含诊疗记录和可选的处方）
        /// 作为聚合根统一管理整个诊疗流程
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null);

        /// <summary>
        /// 根据ID获取完整的医疗案例（包含所有关联数据）
        /// </summary>
        Task<ServiceResult<MedicalCaseDetailDto>> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 更新病案的诊断信息 (Issue #1477 架构纠正v2)
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="dto">诊断更新信息</param>
        /// <returns>更新后的诊断信息</returns>
        Task<ServiceResult<ConsultationDto>> UpdateConsultationAsync(Guid medicalCaseId, ConsultationUpdateDto dto);

        /// <summary>
        /// 更新病案的处方信息 (Issue #1477 架构纠正v2)
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="dto">处方更新信息</param>
        /// <returns>更新后的处方信息</returns>
        Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(Guid medicalCaseId, PrescriptionUpdateDto dto);
    }
}
