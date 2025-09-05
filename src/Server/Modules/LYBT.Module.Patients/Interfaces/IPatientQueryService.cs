using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者查询服务接口
    /// UltraThink架构 - Query层接口抽象
    /// 职责：患者查询、搜索、统计功能专业化处理
    /// </summary>
    public interface IPatientQueryService
    {
        /// <summary>
        /// 分页查询患者列表
        /// </summary>
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        Task<ServiceResult<PatientDto>> GetByIdAsync(Guid patientId);

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> GetAllAsync();

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> GetActivePatientsAsync();

        /// <summary>
        /// 根据身份证号查询患者
        /// </summary>
        Task<ServiceResult<PatientDto>> GetByIDNumberAsync(string idNumber);

        /// <summary>
        /// 根据手机号查询患者
        /// </summary>
        Task<ServiceResult<PatientDto>> GetByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// 根据身份证号获取患者
        /// </summary>
        Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard);

        /// <summary>
        /// 根据手机号获取患者列表
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone);

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 高级搜索患者
        /// </summary>
        Task<ServiceResult<PagedResult<PatientDto>>> AdvancedSearchAsync(PatientSearchDto searchDto);

        /// <summary>
        /// 检查重复患者
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> CheckDuplicatePatientsAsync(PatientCreateDto createDto);
    }
}
