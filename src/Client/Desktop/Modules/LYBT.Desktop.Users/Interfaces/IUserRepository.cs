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
        Task<UserDto> CreateAsync(UserCreateDto user);
        Task<UserDto> UpdateAsync(UserUpdateDto user);
        Task<bool> DeleteAsync(Guid id);
        Task<UserDto> GetByUsernameAsync(string username);
        Task<List<UserDto>> SearchAsync(string keyword);
        Task<List<UserDto>> GetDoctorsAsync();
    }
}
