using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Server.Interfaces.Services
{
    /// <summary>
    /// 医疗案例服务接口 - 聚合根Service（Issue #1600 v2.0架构）
    /// 职责：医疗案例及其关联实体（Consultation/Prescription）的所有Write操作
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
        /// 获取待看诊医案列表（Status=Active）
        /// Epic #1583 - Phase 5
        /// </summary>
        Task<ServiceResult<List<PendingMedicalCaseDto>>> GetPendingCasesAsync();

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

        /// <summary>
        /// 查询病案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        /// <param name="patientName">患者姓名关键字（模糊匹配）</param>
        /// <param name="startDate">开始日期（过滤CreatedAt）</param>
        /// <param name="endDate">结束日期（过滤CreatedAt）</param>
        /// <param name="diagnosisKeyword">诊断关键字（搜索TCMDiagnosis）</param>
        Task<ServiceResult<List<MedicalCaseDto>>> QueryAsync(
            string? patientName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? diagnosisKeyword = null);

        // ========== Epic #1589 - 三步工作流辅助方法（Issue #1600 Phase 3）==========

        /// <summary>
        /// 完成辩证步骤（Step 1）
        /// Epic #1589 Phase 1 - 架构合规版本
        /// 通过MedicalCase聚合根更新Consultation.Step1CompletedAt
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="request">Step1请求参数（是否开处方）</param>
        /// <returns>Step1完成状态</returns>
        Task<ServiceResult<ConsultationStepDto>> CompleteStep1Async(Guid medicalCaseId, CompleteStep1Request request);

        /// <summary>
        /// 重置诊疗步骤（清除所有Step完成时间）
        /// Epic #1589 - 辅助功能
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        Task<ServiceResult> ResetConsultationStepsAsync(Guid medicalCaseId);

        /// <summary>
        /// 清空处方内容（保留处方实体框架）
        /// Epic #1589 - 辅助功能
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        Task<ServiceResult> ClearPrescriptionAsync(Guid medicalCaseId);

        /// <summary>
        /// 从验方导入到处方（将Formula内容复制到Prescription）
        /// Epic #1589 - 辅助功能（TODO: 待实现）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="formulaId">验方ID</param>
        Task<ServiceResult<PrescriptionDto>> ImportFormulaIntoPrescriptionAsync(Guid medicalCaseId, Guid formulaId);
    }
}
