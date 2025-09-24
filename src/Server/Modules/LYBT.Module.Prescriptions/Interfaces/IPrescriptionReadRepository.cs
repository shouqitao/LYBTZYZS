using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方只读仓储接口 - 专门为QueryService提供数据访问
    /// 继承IReadOnlyRepository提供基础查询功能，扩展处方特定的查询方法
    /// </summary>
    public interface IPrescriptionReadRepository : IReadOnlyRepository<LYBT.Entities.Prescriptions.Prescription>
    {
        /// <summary>
        /// 根据ID获取处方详情DTO（包含处方项目）
        /// </summary>
        Task<PrescriptionDto?> GetPrescriptionDtoByIdAsync(Guid id);

        /// <summary>
        /// 分页查询处方并映射为DTO
        /// </summary>
        Task<PagedResult<PrescriptionDto>> GetPagedPrescriptionDtosAsync(PrescriptionQueryDto query);

        /// <summary>
        /// 根据患者ID获取处方历史DTO列表
        /// </summary>
        Task<List<PrescriptionDto>> GetPrescriptionDtosByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医疗案例ID获取处方DTO列表
        /// </summary>
        Task<List<PrescriptionDto>> GetPrescriptionDtosByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 搜索处方并映射为DTO
        /// </summary>
        Task<List<PrescriptionDto>> SearchPrescriptionDtosAsync(string keyword, int maxResults = 50);

        /// <summary>
        /// 获取所有处方DTO列表
        /// </summary>
        Task<List<PrescriptionDto>> GetAllPrescriptionDtosAsync();

        /// <summary>
        /// 获取医生处方DTO列表（可用于"今日处方"等场景）
        /// </summary>
        Task<List<PrescriptionDto>> GetDoctorPrescriptionDtosAsync(Guid doctorId);

        /// <summary>
        /// 获取医生今日处方DTO列表
        /// </summary>
        Task<List<PrescriptionDto>> GetDoctorTodayPrescriptionDtosAsync(Guid doctorId);

        /// <summary>
        /// 获取处方统计信息
        /// </summary>
        Task<PrescriptionStatsDto> GetPrescriptionStatsAsync();
    }
}