using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Repositories
{
    /// <summary>
    /// 用户数据仓储实现 - Phase 2模块化架构
    /// Issue #1114 - 支持CreateDto和UpdateDto
    /// </summary>
    public class UserRepository : BaseApiRepository<UserDto>, IUserRepository
    {
        public UserRepository(
            IApiService apiService,
            ILogger<UserRepository> logger)
            : base(apiService, logger, "api/v1/users")
        {
        }

        public override Task<List<UserDto>> GetAllAsync()
        {
            return base.GetAllAsync();
        }

        public override Task<UserDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        /// <summary>
        /// 创建新用户（使用CreateDto）
        /// </summary>
        public async Task<UserDto> CreateAsync(UserCreateDto user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return (await _apiService.PostAsync<UserCreateDto, UserDto>(_endpoint, user))!;
        }

        /// <summary>
        /// 更新用户信息（使用UpdateDto）
        /// </summary>
        public async Task<UserDto> UpdateAsync(UserUpdateDto user)
        {
            if (user?.Id == null || user.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update user with null or invalid id");
                throw new ArgumentException("User ID is required", nameof(user));
            }

            return (await _apiService.PutAsync<UserUpdateDto, UserDto>($"{_endpoint}/{user.Id}", user))!;
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public async Task<UserDto> GetByUsernameAsync(string username)
        {
            try
            {
                return (await _apiService.GetAsync<UserDto>($"{_endpoint}/username/{username}"))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"根据用户名获取用户失败: {username}");
                throw;
            }
        }

        public override Task<List<UserDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        public async Task<List<UserDto>> GetDoctorsAsync()
        {
            try
            {
                var result = await _apiService.GetAsync<List<UserDto>>($"{_endpoint}/doctors");
                return result ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctors");
                return new List<UserDto>();
            }
        }
    }
}
