using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 医疗案例仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IMedicalCaseRepository : IRepository<MedicalCase>
    {
        /// <summary>
        /// 根据患者ID获取医疗案例
        /// </summary>
        Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据ID获取医案（包含所有关联数据）
        /// </summary>
        Task<MedicalCase> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 根据ID获取医案（包含所有关联数据）- 强制刷新版本
        /// 分离ChangeTracker中的缓存实体后重新查询，确保获取最新RowVersion
        /// 用于并发场景下避免DbUpdateConcurrencyException
        /// </summary>
        Task<MedicalCase?> GetByIdWithDetailsFreshAsync(Guid id);

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// </summary>
        Task<PagedResult<MedicalCase>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 获取分页列表（包含关联数据 + 全部筛选条件，DB 层执行）
        /// Sprint3-X6: 从 Service 内存过滤迁移到 Repository DB 查询
        /// </summary>
        Task<PagedResult<MedicalCase>> GetPagedWithDetailsAsync(
            int pageNumber, int pageSize,
            MedicalCaseStatus? status, Guid? patientId, Guid? doctorId,
            bool isAdmin, string? keyword = null);

        /// <summary>
        /// 获取待看诊医案列表（Status=Active）
        /// Epic #1583 - Phase 5
        /// Epic #2210 Phase 3: 添加doctorId参数实现多医生数据隔离
        /// OpenSpec: unify-pending-query-api - 添加patientId参数支持按患者筛选
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <param name="patientId">患者ID（可选）- 传入时仅返回该患者的待看诊医案</param>
        Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null);

        /// <summary>
        /// 获取所有待看诊医案列表（管理员专用）
        /// 查询所有Active状态的医案，不限定医生
        /// </summary>
        Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync();

        /// <summary>
        /// 查询医案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        /// <param name="patientName">患者姓名关键字（模糊匹配）</param>
        /// <param name="startDate">开始日期（过滤CreatedAt）</param>
        /// <param name="endDate">结束日期（过滤CreatedAt）</param>
        /// <param name="diagnosisKeyword">诊断关键字（搜索TcmDiagnosis）</param>
        Task<List<MedicalCase>> QueryAsync(
            string? patientName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? diagnosisKeyword = null);

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.1: 添加doctorId筛选
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID（为Guid.Empty时不筛选医生）</param>
        /// <returns>未完成的医案实体，若无则返回null</returns>
        Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId);

        /// <summary>
        /// 批量获取医案详情（包含所有关联数据）
        /// OpenSpec: consolidate-medicalcase-detail-queries
        /// </summary>
        /// <param name="ids">医案ID列表</param>
        /// <returns>医案实体列表</returns>
        Task<List<MedicalCase>> GetBatchWithDetailsAsync(List<Guid> ids);

        /// <summary>
        /// 按前缀统计医案编号数量（包含软删除，避免编号重复）
        /// T5-P2-11: 医案编号自动生成
        /// </summary>
        Task<int> CountByPrefixAsync(string prefix);

        /// <summary>
        /// 按前缀统计处方编号数量（包含软删除，避免编号重复）
        /// T5-P2-13: 处方编号自动生成
        /// </summary>
        Task<int> CountPrescriptionsByPrefixAsync(string prefix);
    }
}
