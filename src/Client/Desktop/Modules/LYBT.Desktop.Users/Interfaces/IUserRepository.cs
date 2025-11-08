using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces
{
    /// <summary>
    /// 用户数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IUserRepository
    {
        Task<List<UserDto>> GetAllAsync();
        Task<PagedResult<UserDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<UserDto> CreateAsync(UserInputDto user);
        Task<UserDto> UpdateAsync(UserInputDto user);
        Task<bool> DeleteAsync(Guid id);
        Task<UserDto> GetByUsernameAsync(string username);
        Task<List<UserDto>> SearchAsync(string keyword);
        Task<List<UserDto>> GetDoctorsAsync();

        /// <summary>
        /// 修改个人资料 (Issue #1891)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="dto">个人资料DTO</param>
        Task<UserDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto);

        /// <summary>
        /// 修改密码 (Issue #1887-1892)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="request">修改密码请求</param>
        Task<ServiceResult> ChangePasswordAsync(Guid userId, LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request);
    }
}
