using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IMedicalCaseRepository
    {
        Task<PagedResult<MedicalCaseDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<MedicalCaseDto?> GetByIdAsync(Guid id);
        Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto dto);
        Task<MedicalCaseDto> UpdateAsync(MedicalCaseUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId);
        Task<MedicalCaseDto> CreateWithDetailsAsync(MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null);
        Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 更新医案的诊断信息（聚合根方法）
        /// Issue #1563 - 修复ConsultationFormViewModel违反聚合根模式
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="dto">诊断更新信息</param>
        /// <returns>更新后的诊断信息</returns>
        Task<ConsultationDto> UpdateConsultationAsync(Guid medicalCaseId, ConsultationUpdateDto dto);

        /// <summary>
        /// 查询病案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        Task<List<MedicalCaseDto>> QueryAsync(
            string? patientName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? diagnosisKeyword = null);

        // ========== Epic #1589 - 三步工作流辅助方法（Issue #1605 Phase 5）==========

        /// <summary>
        /// 完成辩证步骤（Step 1）
        /// Epic #1589 Phase 1 - 架构合规版本
        /// </summary>
        Task<ConsultationStepDto> CompleteStep1Async(Guid medicalCaseId, CompleteStep1Request request);

        /// <summary>
        /// 重置诊疗步骤
        /// Epic #1589 Phase 2 - 架构合规版本
        /// </summary>
        Task ResetConsultationStepsAsync(Guid medicalCaseId);

        /// <summary>
        /// 清空处方内容（保留处方框架）
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        Task ClearPrescriptionAsync(Guid medicalCaseId);

        /// <summary>
        /// 从配方导入处方
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        Task<PrescriptionDto> ImportFormulaIntoPrescriptionAsync(Guid medicalCaseId, Guid formulaId);

        /// <summary>
        /// 为已存在的医案创建处方（Issue #1608补充）
        /// </summary>
        Task<PrescriptionDto> CreatePrescriptionAsync(Guid medicalCaseId, PrescriptionCreateDto dto);

        /// <summary>
        /// 删除医案的处方（Issue #1608补充）
        /// </summary>
        Task DeletePrescriptionAsync(Guid medicalCaseId);

        /// <summary>
        /// 暂存病案（保存当前状态）
        /// Epic #1589 Phase 5 - 架构合规版本
        /// </summary>
        Task<MedicalCaseDto> SaveAsDraftAsync(Guid medicalCaseId, MedicalCaseUpdateDto dto);
    }
}
