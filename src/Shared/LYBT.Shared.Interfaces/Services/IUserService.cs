using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 用户服务统一接口 - 标准CRUD模式
    /// Issue #1008: 重构为标准接口，移除过度设计方法
    /// 遵循单一服务原则，符合MVP适度设计原则
    /// </summary>
    public interface IUserService
    {
        #region 查询操作

        /// <summary>
        /// 分页获取用户列表
        /// </summary>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字（可选，搜索用户名/邮箱/真实姓名）</param>
        Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 搜索用户（返回所有匹配结果）
        /// </summary>
        /// <param name="keyword">搜索关键字</param>
        Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);

        #endregion

        #region 业务操作

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 禁用用户
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);

        /// <summary>
        /// 启用用户
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);

        /// <summary>
        /// 重置密码
        /// </summary>
        Task<ServiceResult> ResetPasswordAsync(Guid id, string newPassword);

        /// <summary>
        /// 更改密码
        /// </summary>
        Task<ServiceResult> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

        /// <summary>
        /// 修改个人信息
        /// </summary>
        Task<ServiceResult> ChangeProfileAsync(Guid userId, string realName, string phoneNumber);

        #endregion
    }
}
