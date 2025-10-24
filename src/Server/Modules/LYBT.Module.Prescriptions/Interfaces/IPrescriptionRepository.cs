using System.Linq.Expressions;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方仓储接口 - Read-only版本（Issue #1600 Phase 1）
    /// 移除Write方法，所有写操作必须通过MedicalCase聚合根
    /// </summary>
    public interface IPrescriptionRepository
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

        // ========== 基础Read方法（Issue #1600 Phase 1）==========

        /// <summary>
        /// 根据ID获取实体（基础方法）
        /// </summary>
        Task<Prescription?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有实体（基础方法）
        /// </summary>
        Task<IEnumerable<Prescription>> GetAllAsync();

        /// <summary>
        /// 根据条件查找（基础方法）
        /// </summary>
        Task<IEnumerable<Prescription>> FindAsync(Expression<Func<Prescription, bool>> predicate);
    }
}
