using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

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
        /// 分页查询挂号列表
        /// </summary>
        Task<PaginatedResult<RegistrationDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

        /// <summary>
        /// 新增挂号
        /// </summary>
        Task<bool> AddAsync(RegistrationCreateDto dto);

        /// <summary>
        /// 编辑挂号
        /// </summary>
        Task<bool> UpdateAsync(RegistrationEditDto dto);

        /// <summary>
        /// 删除挂号（物理删除，不推荐）
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 取消挂号，更新状态为已取消
        /// </summary>
        Task<bool> CancelAsync(Guid id);
    }
}