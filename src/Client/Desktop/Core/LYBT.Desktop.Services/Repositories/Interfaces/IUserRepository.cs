using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Services.Repositories.Interfaces
{
    /// <summary>
    /// 用户数据仓储接口 - UltraThink架构
    /// </summary>
    public interface IUserRepository
    {
        Task<List<UserDto>> GetAllAsync();
        Task<PagedResult<UserDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<UserDto> GetByIdAsync(Guid id);
        Task<UserDto> CreateAsync(UserDto user);
        Task<UserDto> UpdateAsync(UserDto user);
        Task<bool> DeleteAsync(Guid id);
        Task<UserDto> GetByUsernameAsync(string username);
        Task<List<UserDto>> SearchAsync(string keyword);
        Task<List<UserDto>> GetDoctorsAsync();
    }
}
