using LYBT.Models.Patient;
using LYBT.Module.Patients.Models;

namespace LYBT.Module.Patients.Interfaces {
    /// <summary>
    /// 病人仓储接口，定义病人相关数据操作
    /// </summary>
    public interface IPatientRepository {
        /// <summary>
        /// 新增病人记录
        /// </summary>
        Task<bool> AddAsync(PatientModel patient);

        /// <summary>
        /// 根据主键ID查询病人
        /// </summary>
        Task<PatientModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取病人分页列表，支持关键词模糊搜索
        /// </summary>
        Task<List<PatientModel>> GetListAsync(string? keyword = null, int page = 1, int pageSize = 20);

        /// <summary>
        /// 更新病人信息
        /// </summary>
        Task<bool> UpdateAsync(PatientModel patient);

        /// <summary>
        /// 删除病人记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 通过身份证号查找病人
        /// </summary>
        Task<PatientModel?> GetByIDNumberAsync(string idNumber);

        /// <summary>
        /// 通过手机号查找病人
        /// </summary>
        Task<PatientModel?> GetByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// 获取病人总数（可用于分页）
        /// </summary>
        Task<int> GetCountAsync(string? keyword = null);

        /// <summary>
        /// 启用患者
        /// </summary>
        Task<bool> EnableAsync(Guid id);

        /// <summary>
        /// 禁用患者
        /// </summary>
        Task<bool> DisableAsync(Guid id);

        /// <summary>
        /// 批量禁用患者
        /// </summary>
        Task<int> BatchDisableAsync(List<Guid> ids);

        /// <summary>
        /// 根据关键词搜索患者
        /// </summary>
        Task<List<PatientModel>> SearchAsync(string keyword);

        /// <summary>
        /// 获取指定医生可访问的患者列表
        /// </summary>
        Task<List<PatientModel>> GetForDoctorAsync(Guid doctorId);

        /// <summary>
        /// 为患者授权医生
        /// </summary>
        Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId);
    }
}
