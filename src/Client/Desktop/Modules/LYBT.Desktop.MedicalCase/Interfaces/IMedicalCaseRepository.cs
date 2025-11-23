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
        /// <summary>Epic #1961: 使用统一的 MedicalCaseInputDto</summary>
        Task<MedicalCaseDto> CreateAsync(MedicalCaseInputDto dto);
        /// <summary>Epic #1961: 使用统一的 MedicalCaseInputDto</summary>
        Task<MedicalCaseDto> UpdateAsync(MedicalCaseInputDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId);
        /// <summary>Epic #1961: 使用统一的 MedicalCaseInputDto</summary>
        Task<MedicalCaseDto> CreateWithDetailsAsync(MedicalCaseInputDto caseDto,
            ConsultationInputDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null);
        Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 更新医案的诊断信息（聚合根方法）
        /// Issue #1563 - 修复ConsultationFormViewModel违反聚合根模式
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="dto">诊断更新信息</param>
        /// <returns>更新后的诊断信息</returns>
        Task<ConsultationDto> UpdateConsultationAsync(Guid medicalCaseId, ConsultationInputDto dto);

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
        /// 更新医案的处方（Issue #1608补充）
        /// </summary>
        Task<PrescriptionDto> UpdatePrescriptionAsync(Guid medicalCaseId, PrescriptionUpdateDto dto);

        /// <summary>
        /// 删除医案的处方（Issue #1608补充）
        /// </summary>
        Task DeletePrescriptionAsync(Guid medicalCaseId);

        /// <summary>
        /// 暂存病案（保存当前状态）
        /// Epic #1589 Phase 5 - 架构合规版本
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// </summary>
        Task<MedicalCaseDto> SaveAsDraftAsync(Guid medicalCaseId, MedicalCaseInputDto dto);

        // ========== Epic #1676 Phase 4 Task 4.4 - Desktop端新增方法 ==========

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.4
        /// Epic #2210 Task 3.1.4: 添加doctorId参数
        /// </summary>
        Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId);

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.4
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        Task<bool> CloseCaseAsync(Guid medicalCaseId);
    }
}
