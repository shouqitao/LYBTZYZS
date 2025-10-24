using System.Linq.Expressions;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 诊疗仓储接口 - Read-only版本（Issue #1600 Phase 1）
    /// 移除Write方法，所有写操作必须通过MedicalCase聚合根
    /// </summary>
    public interface IConsultationRepository
    {
        /// <summary>
        /// 根据患者ID获取诊疗记录
        /// </summary>
        Task<List<ConsultationEntity>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// </summary>
        Task<PagedResult<ConsultationEntity>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 根据ID获取诊疗记录（包含所有关联数据）
        /// </summary>
        Task<ConsultationEntity> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 根据病案ID获取诊疗记录
        /// </summary>
        Task<ConsultationEntity> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        // ========== 基础Read方法（Issue #1600 Phase 1）==========

        /// <summary>
        /// 根据ID获取实体（基础方法）
        /// </summary>
        Task<ConsultationEntity?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有实体（基础方法）
        /// </summary>
        Task<IEnumerable<ConsultationEntity>> GetAllAsync();

        /// <summary>
        /// 根据条件查找（基础方法）
        /// </summary>
        Task<IEnumerable<ConsultationEntity>> FindAsync(Expression<Func<ConsultationEntity, bool>> predicate);
    }
}
