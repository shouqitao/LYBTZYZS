using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Interfaces {

    /// <summary>
    /// 患者服务接口（简化版）
    /// 只提供基础的患者档案维护功能
    /// </summary>
    public interface IPatientService {

        /// <summary>
        /// 新增患者
        /// </summary>
        Task<PatientDetailDto?> CreateAsync(PatientDetailDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<PatientDetailDto?> UpdateAsync(Guid id, PatientDetailDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据ID获取患者信息
        /// </summary>
        Task<PatientDetailDto?> GetByIdAsync(Guid id, UserRole currentUserRole);

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        Task<List<PatientDetailDto>> GetAllAsync(UserRole currentUserRole);

        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<PaginatedResult<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query, UserRole currentUserRole);

        /// <summary>
        /// 搜索患者（根据姓名、手机号、身份证号）
        /// </summary>
        Task<List<PatientDetailDto>> SearchAsync(string keyword, UserRole currentUserRole);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 设置患者状态（启用/禁用）
        /// </summary>
        Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取可用患者列表（用于挂号选择）
        /// </summary>
        Task<List<PatientDetailDto>> GetActivePatientsAsync();

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        Task<PatientDetailDto?> GetByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        Task<PatientDetailDto?> GetByIDNumberAsync(string idNumber);
    }
}