using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Registration.Dtos;

namespace LYBT.Module.Registration.Interfaces {
    /// <summary>
    /// 挂号业务服务接口
    /// </summary>
    public interface IRegistrationService {
        /// <summary>
        /// 获取挂号详情
        /// </summary>
        Task<RegistrationDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取挂号列表
        /// </summary>
        Task<List<RegistrationDto>> GetListAsync();

        /// <summary>
        /// 新增挂号
        /// </summary>
        Task<bool> AddAsync(RegistrationCreateDto dto);

        /// <summary>
        /// 编辑挂号
        /// </summary>
        Task<bool> UpdateAsync(RegistrationEditDto dto);

        /// <summary>
        /// 删除挂号
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
