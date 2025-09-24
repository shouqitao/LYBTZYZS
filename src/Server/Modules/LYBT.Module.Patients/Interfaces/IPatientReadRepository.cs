using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者只读仓储接口 - 专门为QueryService提供数据访问
    /// 继承IReadOnlyRepository提供基础查询功能，扩展患者特定的查询方法
    /// </summary>
    public interface IPatientReadRepository : IReadOnlyRepository<LYBT.Entities.Patients.Patient>
    {
        /// <summary>
        /// 分页查询患者并映射为DTO
        /// </summary>
        Task<PagedResult<PatientDto>> GetPagedPatientDtosAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 根据ID获取患者详情DTO
        /// </summary>
        Task<PatientDto?> GetPatientDtoByIdAsync(Guid patientId);

        /// <summary>
        /// 获取所有患者DTO列表
        /// </summary>
        Task<List<PatientDto>> GetAllPatientDtosAsync();

        /// <summary>
        /// 获取活跃患者DTO列表
        /// </summary>
        Task<List<PatientDto>> GetActivePatientDtosAsync();

        /// <summary>
        /// 根据身份证号查询患者DTO
        /// </summary>
        Task<PatientDto?> GetPatientDtoByIdNumberAsync(string idNumber);

        /// <summary>
        /// 根据手机号查询患者DTO
        /// </summary>
        Task<PatientDto?> GetPatientDtoByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// 根据身份证号获取患者DTO
        /// </summary>
        Task<PatientDto?> GetPatientDtoByIdCardAsync(string idCard);

        /// <summary>
        /// 根据手机号获取患者DTO列表
        /// </summary>
        Task<List<PatientDto>> GetPatientDtosByPhoneAsync(string phone);

        /// <summary>
        /// 搜索患者并映射为DTO
        /// </summary>
        Task<List<PatientDto>> SearchPatientDtosAsync(string keyword, int maxResults = 20);

        /// <summary>
        /// 高级搜索患者并映射为DTO
        /// </summary>
        Task<PagedResult<PatientDto>> AdvancedSearchPatientDtosAsync(PatientSearchDto searchDto);

        /// <summary>
        /// 检查重复患者
        /// </summary>
        Task<List<PatientDto>> CheckDuplicatePatientDtosAsync(PatientCreateDto createDto);
    }
}