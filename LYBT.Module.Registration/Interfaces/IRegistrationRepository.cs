using LYBT.Models.Registration;

namespace LYBT.Module.Registration.Interfaces {

    /// <summary>
    /// 挂号仓储接口，定义挂号数据操作
    /// </summary>
    public interface IRegistrationRepository {

        /// <summary>
        /// 根据ID获取挂号详情
        /// </summary>
        Task<RegistrationModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有挂号列表
        /// </summary>
        Task<List<RegistrationModel>> GetListAsync();

        /// <summary>
        /// 新增挂号
        /// </summary>
        Task<bool> AddAsync(RegistrationModel model);

        /// <summary>
        /// 更新挂号
        /// </summary>
        Task<bool> UpdateAsync(RegistrationModel model);

        /// <summary>
        /// 删除挂号
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}