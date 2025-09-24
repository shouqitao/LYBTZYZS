using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例只读仓储接口 - 专门为QueryService提供数据访问
    /// 继承IReadOnlyRepository提供基础查询功能，扩展医疗案例特定的查询方法
    /// </summary>
    public interface IMedicalCaseReadRepository : IReadOnlyRepository<LYBT.Entities.MedicalCase.MedicalCase>
    {
        /// <summary>
        /// 根据ID获取医疗案例详情DTO
        /// </summary>
        Task<MedicalCaseDto?> GetMedicalCaseDtoByIdAsync(Guid caseId);

        /// <summary>
        /// 分页查询医疗案例并映射为DTO
        /// </summary>
        Task<PagedResult<MedicalCaseDto>> GetPagedMedicalCaseDtosAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 根据患者ID获取医疗案例DTO列表
        /// </summary>
        Task<List<MedicalCaseDto>> GetMedicalCaseDtosByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取患者的活跃医疗案例DTO
        /// </summary>
        Task<MedicalCaseDto?> GetActiveMedicalCaseDtoByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 搜索医疗案例并映射为DTO
        /// </summary>
        Task<List<MedicalCaseDto>> SearchMedicalCaseDtosAsync(string keyword, int maxResults = 50);

        /// <summary>
        /// 检查患者是否有活跃案例
        /// </summary>
        Task<bool> HasActiveCaseAsync(Guid patientId);

        /// <summary>
        /// 获取历史医疗案例DTO列表
        /// </summary>
        Task<List<MedicalCaseDto>> GetHistoryMedicalCaseDtosAsync(Guid patientId);

        /// <summary>
        /// 根据医生ID获取医疗案例DTO列表
        /// </summary>
        Task<List<MedicalCaseDto>> GetMedicalCaseDtosByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 根据状态获取医疗案例DTO列表
        /// </summary>
        Task<List<MedicalCaseDto>> GetMedicalCaseDtosByStatusAsync(MedicalCaseStatus status);

        /// <summary>
        /// 获取医疗案例统计信息
        /// </summary>
        Task<object> GetMedicalCaseStatisticsAsync();
    }
}