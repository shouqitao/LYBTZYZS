using System.Linq.Expressions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Interfaces;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 诊疗仓储接口 - 继承IReadRepository标准接口（Epic #2016 Phase 3）
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - ⭐ 统一共性：继承IReadRepository&lt;ConsultationEntity&gt;获得5个标准只读方法
    /// - ⭐ 保持特性：保留诊疗模块特定业务方法
    /// - Read-only模式：所有写操作必须通过MedicalCase聚合根
    ///
    /// 特定业务方法说明：
    /// - GetByPatientIdAsync: 患者诊疗记录查询
    /// - GetPagedWithDetailsAsync: 分页查询（包含关联数据）
    /// - GetByIdWithDetailsAsync: 详情查询（包含所有关联数据）
    /// - GetByMedicalCaseIdAsync: 病案关联查询
    /// </remarks>
    public interface IConsultationRepository : IReadRepository<ConsultationEntity>
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
    }
}
