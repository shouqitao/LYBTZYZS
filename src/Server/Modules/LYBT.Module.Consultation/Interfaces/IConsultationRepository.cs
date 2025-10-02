using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 诊疗仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IConsultationRepository : IRepository<ConsultationEntity>
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
