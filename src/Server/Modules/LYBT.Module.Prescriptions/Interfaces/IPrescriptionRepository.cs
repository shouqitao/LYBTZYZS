using System.Linq.Expressions;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Interfaces;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方仓储接口 - 继承IReadRepository标准接口（Epic #2016 Phase 3）
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - ⭐ 统一共性：继承IReadRepository&lt;Prescription&gt;获得5个标准只读方法
    /// - ⭐ 保持特性：保留处方模块特定业务方法
    /// - Read-only模式：所有写操作必须通过MedicalCase聚合根
    ///
    /// 特定业务方法说明：
    /// - GetByIdWithItemsAsync: 获取处方详情（包含处方项和药材信息）
    /// - GetPagedWithDetailsAsync: 分页查询（包含关联数据）
    /// - GetByPatientIdAsync: 患者处方列表查询
    /// - GetByMedicalCaseIdAsync: 病案关联查询
    /// - GetPrescriptionNumbersByPrefixAsync: 处方编号前缀查询（自动编号功能）
    /// </remarks>
    public interface IPrescriptionRepository : IReadRepository<Prescription>
    {
        /// <summary>
        /// 根据ID获取处方（包含处方项和药材信息）
        /// </summary>
        Task<Prescription?> GetByIdWithItemsAsync(Guid id);

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// </summary>
        Task<PagedResult<Prescription>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        Task<List<Prescription>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据病案ID获取处方
        /// </summary>
        Task<List<Prescription>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据前缀查询处方编号列表（用于编号生成）
        /// Issue #1551: 处方自动编号功能
        /// </summary>
        /// <param name="prefix">编号前缀（例如：RX-20251021-）</param>
        /// <returns>匹配的处方编号列表</returns>
        Task<List<string>> GetPrescriptionNumbersByPrefixAsync(string prefix);

        /// <summary>
        /// 批量获取处方详情（包含处方项和药材信息）
        /// Task 1.5: 解决N+1查询问题
        /// </summary>
        /// <param name="prescriptionIds">处方ID列表</param>
        /// <returns>处方详情列表（按ID匹配，不存在的ID不返回）</returns>
        Task<List<Prescription>> GetByIdsWithItemsAsync(IEnumerable<Guid> prescriptionIds);
    }
}
