using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Linq;

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

        /// <summary>
        /// 获取所有医生用户（Desktop端本地筛选实现）
        /// Issue #1155 - 使用本地角色筛选替代不存在的Server API
        /// </summary>
        public async Task<List<UserDto>> GetDoctorsAsync()
        {
            try
            {
                _logger.LogDebug("获取所有医生用户");

                // 调用现有接口获取所有用户（第1页，100条，足够覆盖小诊所全部用户）
                var result = await GetPagedAsync(1, 100, null);

                if (result?.Items == null)
                {
                    _logger.LogWarning("获取用户列表失败或返回空");
                    return new List<UserDto>();
                }

                // Desktop端本地筛选：角色=医生 && 状态=启用
                var doctors = result.Items
                    .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                    .ToList();

                _logger.LogInformation("成功获取{Count}名医生用户", doctors.Count);
                return doctors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生用户列表时发生异常");
                return new List<UserDto>();
            }
        }
    }
}
