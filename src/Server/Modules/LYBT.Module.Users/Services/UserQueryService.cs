using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services
{

    /// <summary>
    /// 用户查询服务 - UltraThink架构重构版
    /// 职责：分页查询，搜索筛选，用户查询，角色获取
    /// 改为使用ReadRepository，移除直接的DbContext依赖
    /// </summary>
    public class UserQueryService(
        IUserReadRepository readRepository,
        ILogger<UserQueryService> logger) : IUserQueryService
    {
        private readonly IUserReadRepository _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
        private readonly ILogger<UserQueryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");
                }

                var user = await _readRepository.GetUserDtoByIdAsync(id);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                return ServiceResult<UserDto>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户详情失败: {Id}", id);
                return ServiceResult<UserDto>.Failure($"获取用户详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询用户
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto query)
        {
            try
            {
                var pagedResult = await _readRepository.GetPagedUserDtosAsync(query);
                return ServiceResult<PagedResult<UserDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询用户失败");
                return ServiceResult<PagedResult<UserDto>>.Failure($"分页查询用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据用户名获取用户信息
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return ServiceResult<UserDto>.Failure("用户名不能为空");
                }

                var user = await _readRepository.GetUserDtoByUsernameAsync(userName);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                return ServiceResult<UserDto>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名获取用户失败: {UserName}", userName);
                return ServiceResult<UserDto>.Failure($"获取用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            try
            {
                var users = await _readRepository.GetActiveUserDtosAsync();
                return ServiceResult<List<UserDto>>.Success(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活跃用户列表失败");
                return ServiceResult<List<UserDto>>.Failure($"获取活跃用户列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<UserDto>>.Success([]);
                }

                var users = await _readRepository.SearchUserDtosAsync(keyword.Trim());
                return ServiceResult<List<UserDto>>.Success(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户失败: {Keyword}", keyword);
                return ServiceResult<List<UserDto>>.Failure($"搜索用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取系统所有角色
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            try
            {
                var roles = await _readRepository.GetRolesAsync();
                return ServiceResult<List<object>>.Success(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色列表失败");
                return ServiceResult<List<object>>.Failure($"获取角色列表失败: {ex.Message}");
            }
        }


        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return ServiceResult<bool>.Failure("用户名不能为空");
                }

                var isAvailable = await _readRepository.IsUsernameAvailableAsync(userName);
                return ServiceResult<bool>.Success(isAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户名失败: {UserName}", userName);
                return ServiceResult<bool>.Failure($"验证用户名失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有医生（即角色为Doctor的用户）
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetDoctorsAsync()
        {
            try
            {
                var doctors = await _readRepository.GetDoctorDtosAsync();
                return ServiceResult<List<UserDto>>.Success(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生列表失败");
                return ServiceResult<List<UserDto>>.Failure($"获取医生列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查医生可用性（简化版，默认都可用）
        /// </summary>
        public async Task<ServiceResult<bool>> IsDoctorAvailableAsync(Guid doctorId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医生ID不能为空");
                }

                var isAvailable = await _readRepository.IsDoctorAvailableAsync(doctorId);
                return ServiceResult<bool>.Success(isAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查医生可用性失败: {DoctorId}", doctorId);
                return ServiceResult<bool>.Failure($"检查医生可用性失败: {ex.Message}");
            }
        }
    }
}
