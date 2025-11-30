using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方服务接口 - 简化版，包含基础CRUD功能
    /// </summary>
    public interface IPrescriptionService
    {
        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        Task<Result<PrescriptionDto>> GetByIdAsync(Guid id);

        // ========== Write方法已移除（Issue #1601 Phase 1）==========
        // CreateAsync, UpdateAsync, DeleteAsync, PhysicalDeleteAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        /// <summary>
        /// 根据病例ID获取处方列表
        /// </summary>
        Task<Result<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        // ========== Clone/Import方法已移除（Issue #1601 Phase 1）==========
        // CloneAsync, ClonePrescriptionAsync, ImportFormulaIntoPrescriptionAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        /// <summary>
        /// 搜索处方 - 按患者姓名或症状/诊断关键字 (Issue #1372 ENTRY-14)
        /// </summary>
        /// <param name="patientName">患者姓名关键字（可空）</param>
        /// <param name="symptomKeyword">症状/诊断关键字（可空）</param>
        /// <returns>处方搜索结果列表</returns>
        Task<Result<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
            string? patientName = null,
            string? symptomKeyword = null);

        /// <summary>
        /// 获取患者最近处方列表 (Issue #1371 ENTRY-13)
        /// 按日期倒序排列，包含诊断信息
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="count">返回数量（默认5条）</param>
        /// <returns>患者最近处方列表</returns>
        Task<Result<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
            Guid patientId,
            int count = 5);
    }
}
