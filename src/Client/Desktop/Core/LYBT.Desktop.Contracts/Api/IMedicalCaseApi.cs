using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 医疗案例API客户端接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IMedicalCaseApi
    {
        /// <summary>
        /// 获取医疗案例列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/medicalcases")]
        Task<ApiResponse<PagedResult<MedicalCaseDto>>> GetMedicalCasesAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取医疗案例详情
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<MedicalCaseDto>> GetMedicalCaseByIdAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/by-patient/{patientId}")]
        Task<ApiResponse<List<MedicalCaseDto>>> GetMedicalCasesByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取待看诊医案列表（Status=Active）
        /// Epic #1583 - Phase 5
        /// </summary>
        /// <summary>
        /// 获取待看诊医案列表（Status=Active）
        /// Epic #1583 - Phase 5
        /// Epic #2210 Phase 3: 添加doctorId参数实现多医生数据隔离
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/pending")]
        Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync([Refit.Query] Guid doctorId);

        /// <summary>
        /// 查询病案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/query")]
        Task<ApiResponse<List<MedicalCaseDto>>> QueryMedicalCasesAsync(
            [Refit.Query] string? patientName = null,
            [Refit.Query] DateTime? startDate = null,
            [Refit.Query] DateTime? endDate = null,
            [Refit.Query] string? diagnosisKeyword = null);

        /// <summary>
        /// 获取完整的医疗案例（包含所有关联数据）
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/{id}/with-details")]
        Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// </summary>
        [Refit.Post("/api/v1/medicalcases")]
        Task<ApiResponse<MedicalCaseDto>> CreateMedicalCaseAsync([Refit.Body] MedicalCaseInputDto request);

        /// <summary>
        /// 创建完整的医疗案例（包含诊疗和可选处方）
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/with-details")]
        Task<ApiResponse<MedicalCaseDto>> CreateMedicalCaseWithDetailsAsync([Refit.Body] MedicalCaseWithDetailsCreateDto request);

        /// <summary>
        /// 更新医疗案例
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<MedicalCaseDto>> UpdateMedicalCaseAsync(Guid id, [Refit.Body] MedicalCaseInputDto request);

        /// <summary>
        /// 更新医案的诊断信息（聚合根方法）
        /// Issue #1563 - 修复ConsultationFormViewModel违反聚合根模式
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/consultation")]
        Task<ApiResponse<ConsultationDto>> UpdateConsultationAsync(Guid medicalCaseId, [Refit.Body] ConsultationInputDto request);

        /// <summary>
        /// 删除医疗案例（物理删除）
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteMedicalCaseAsync(Guid id);

        /// <summary>
        /// 软删除医疗案例（标记为删除）
        /// Issue #1606 Phase 3 - 修复PrescriptionEditorViewModel软删除调用
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{id}/soft")]
        Task<ApiResponse<ApiResponse>> SoftDeleteMedicalCaseAsync(Guid id);

        // ========== Epic #1589 - 三步工作流辅助方法（Issue #1605 Phase 5）==========

        /// <summary>
        /// 完成辩证步骤（Step 1）
        /// Epic #1589 Phase 1 - 架构合规版本
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/{medicalCaseId}/complete-step1")]
        Task<ApiResponse<ConsultationStepDto>> CompleteStep1Async(
            Guid medicalCaseId,
            [Refit.Body] CompleteStep1Request request);

        /// <summary>
        /// 重置诊疗步骤
        /// Epic #1589 Phase 2 - 架构合规版本
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/reset-consultation-steps")]
        Task<ApiResponse> ResetConsultationStepsAsync(Guid medicalCaseId);

        /// <summary>
        /// 清空处方内容（保留处方框架）
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{medicalCaseId}/prescription/clear")]
        Task<ApiResponse> ClearPrescriptionAsync(Guid medicalCaseId);

        /// <summary>
        /// 从配方导入处方
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/{medicalCaseId}/prescription/import-formula/{formulaId}")]
        Task<ApiResponse<PrescriptionDto>> ImportFormulaIntoPrescriptionAsync(
            Guid medicalCaseId,
            Guid formulaId);

        /// <summary>
        /// 为已存在的医案创建处方（Issue #1608补充）
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/{medicalCaseId}/prescription")]
        Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync(
            Guid medicalCaseId,
            [Refit.Body] PrescriptionCreateDto request);

        /// <summary>
        /// 更新医案的处方（Issue #1608补充）
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/prescription")]
        Task<ApiResponse<PrescriptionDto>> UpdatePrescriptionAsync(
            Guid medicalCaseId,
            [Refit.Body] PrescriptionUpdateDto request);

        /// <summary>
        /// 删除医案的处方（Issue #1608补充）
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{medicalCaseId}/prescription")]
        Task<ApiResponse> DeletePrescriptionAsync(Guid medicalCaseId);

        /// <summary>
        /// 标记是否开处方
        /// Task 3.4 (#1661): RadioBox变化时自动保存
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/prescription-flag")]
        Task<ApiResponse<MedicalCaseDto>> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            [Refit.Body] SetPrescriptionFlagRequest request);

        /// <summary>
        /// 暂存病案（保存当前状态）
        /// Epic #1589 Phase 5 - 架构合规版本
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/save-as-draft")]
        Task<ApiResponse<MedicalCaseDto>> SaveAsDraftAsync(
            Guid medicalCaseId,
            [Refit.Body] MedicalCaseInputDto request);

        // ========== Epic #1676 Phase 4 Task 4.1 - 新增专用API ==========

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.4: 添加doctorId参数
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/patient/{patientId}/unfinished")]
        Task<ApiResponse<MedicalCaseDto>> GetUnfinishedCaseByPatientIdAsync(
            Guid patientId,
            [Refit.Query] Guid doctorId);

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/close")]
        Task<ApiResponse> CloseCaseAsync(Guid id);
    }
}
