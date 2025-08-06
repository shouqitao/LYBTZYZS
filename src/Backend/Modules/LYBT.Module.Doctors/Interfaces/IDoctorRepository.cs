using LYBT.Models.Doctors;

namespace LYBT.Module.Doctors.Interfaces {

    /// <summary>
    /// 医生仓储接口（简化版）
    /// </summary>
    public interface IDoctorRepository {

        /// <summary>
        /// 根据ID获取医生
        /// </summary>
        Task<DoctorModel?> GetByIdAsync(Guid id, bool includeDisabled = false);

        /// <summary>
        /// 根据用户ID获取医生
        /// </summary>
        Task<DoctorModel?> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 获取所有医生列表
        /// </summary>
        Task<List<DoctorModel>> GetAllAsync(bool includeDisabled = false);

        /// <summary>
        /// 新增医生
        /// </summary>
        Task<bool> AddAsync(DoctorModel model);

        /// <summary>
        /// 更新医生
        /// </summary>
        Task<bool> UpdateAsync(DoctorModel model);

        /// <summary>
        /// 检查医生是否存在
        /// </summary>
        Task<bool> ExistsAsync(Guid id);
    }
}