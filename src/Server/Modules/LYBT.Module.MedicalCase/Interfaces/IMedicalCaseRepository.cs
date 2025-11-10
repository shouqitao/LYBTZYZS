using LYBT.Shared.Models.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IMedicalCaseRepository : IRepository<MedicalCaseEntity>
    {
        /// <summary>
        /// 根据患者ID获取医疗案例
        /// </summary>
        Task<List<MedicalCaseEntity>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据ID获取病案（包含所有关联数据）
        /// </summary>
        Task<MedicalCaseEntity> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// </summary>
        Task<PagedResult<MedicalCaseEntity>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 根据医生ID获取病案列表
        /// </summary>
        Task<List<MedicalCaseEntity>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 获取待看诊医案列表（Status=Active）
        /// Epic #1583 - Phase 5
        /// </summary>
        Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync();

        /// <summary>
        /// 查询病案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        /// <param name="patientName">患者姓名关键字（模糊匹配）</param>
        /// <param name="startDate">开始日期（过滤CreatedAt）</param>
        /// <param name="endDate">结束日期（过滤CreatedAt）</param>
        /// <param name="diagnosisKeyword">诊断关键字（搜索TCMDiagnosis）</param>
        Task<List<MedicalCaseEntity>> QueryAsync(
            string? patientName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? diagnosisKeyword = null);

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>未完成的医案实体，若无则返回null</returns>
        Task<MedicalCaseEntity?> GetUnfinishedCaseByPatientIdAsync(Guid patientId);
    }
}
