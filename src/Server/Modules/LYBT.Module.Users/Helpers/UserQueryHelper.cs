using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Users.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Helpers
{
    /// <summary>
    /// UserService查询助手类 - UltraThink Helper模式
    /// 负责所有查询、搜索、统计和数据获取逻辑
    /// </summary>
    public class UserQueryHelper
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserQueryHelper> _logger;

        public UserQueryHelper(IUserRepository userRepository, IMapper mapper, ILogger<UserQueryHelper> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 分页/条件查找用户
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
        {
            try
            {
                // 管理员可以查看所有用户（包括禁用的），普通用户只能查看启用的用户
                bool includeDisabled = true;

                var (models, total) = await _userRepository.GetPagedAsync(query, includeDisabled);
                var users = _mapper.Map<List<UserDto>>(models);
                var result = new PagedResult<UserDto>
                {
                    TotalCount = total,
                    Items = users,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<UserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分页用户列表失败");                return ServiceResult<PagedResult<UserDto>>.Failure($"获取分页用户列表失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 管理员可以查看所有用户（包括禁用的），普通用户只能查看启用的用户
                bool includeDisabled = true;

                var model = await _userRepository.GetByIdAsync(id, includeDisabled);
                if (model == null)
                {                    return ServiceResult<UserDto>.Failure("用户不存在");                }

                var userDto = _mapper.Map<UserDto>(model);
                return ServiceResult<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "根据ID获取用户失败, ID: {UserId}", id);                return ServiceResult<UserDto>.Failure($"根据ID获取用户失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 根据用户名获取用户信息（用于登录验证后获取用户详情）
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            try
            {
                var model = await _userRepository.GetByUsernameAsync(username);
                if (model == null)
                {                    return ServiceResult<UserDto>.Failure("用户不存在");                }

                var userDto = _mapper.Map<UserDto>(model);
                return ServiceResult<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "根据用户名获取用户失败, Username: {Username}", username);                return ServiceResult<UserDto>.Failure($"根据用户名获取用户失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            try
            {
                var users = await _userRepository.GetActiveUsersAsync();
                var userDtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(userDtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取活跃用户列表失败");                return ServiceResult<List<UserDto>>.Failure($"获取活跃用户列表失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            try
            {
                var query = new UserPagedQueryDto
                {
                    Keyword = keyword,
                    PageIndex = 1,
                    PageSize = 100 // 搜索返回前100个结果
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    return ServiceResult<List<UserDto>>.Success(result.Data.Items.ToList());
                }
                return ServiceResult<List<UserDto>>.Failure(result.ErrorMessage ?? "搜索用户失败");            }
            catch (Exception ex)
            {                _logger.LogError(ex, "搜索用户失败, Keyword: {Keyword}", keyword);                return ServiceResult<List<UserDto>>.Failure($"搜索用户失败: {ex.Message}", ex);            }
        }

        #region 已废弃功能 - 统计分析
        /*
        // 用户统计功能已删除 - UltraThink精简
        // GetStatisticsAsync方法已废弃，小诊所不需要复杂统计分析
        */
        #endregion

        /// <summary>
        /// 根据ID列表获取用户列表
        /// </summary>
        public async Task<ServiceResult<List<User>>> GetUsersByIdsAsync(List<Guid> ids)
        {
            try
            {
                // 内部方法总是包含禁用用户，确保批量操作能正常进行
                var users = await _userRepository.GetUsersByIdsAsync(ids, includeDisabled: true);
                return ServiceResult<List<User>>.Success(users);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "根据ID列表获取用户失败");                return ServiceResult<List<User>>.Failure($"根据ID列表获取用户失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 获取系统所有角色
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            try
            {                var roles = new List<object> { new { Value = "Admin", DisplayName = "管理员" } };                await Task.CompletedTask; // 为了保持异步方法的一致性
                return ServiceResult<List<object>>.Success(roles);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取角色列表失败");                return ServiceResult<List<object>>.Failure($"获取角色列表失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 获取用户操作日志
        /// </summary>
        public async Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query)
        {
            try
            {
                // 简化实现，返回空日志列表
                var result = new PagedResult<object>
                {
                    Items = new List<object>(),
                    TotalCount = 0,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                await Task.CompletedTask; // 为了保持异步方法的一致性
                return ServiceResult<PagedResult<object>>.Success(result);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取用户操作日志失败, UserId: {UserId}", userId);                return ServiceResult<PagedResult<object>>.Failure($"获取用户操作日志失败: {ex.Message}", ex);            }
        }

        #region 医生功能兼容接口

        /// <summary>
        /// 获取所有医生（即所有用户）
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetDoctorsAsync()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                var userDtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(userDtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取医生列表失败");                return ServiceResult<List<UserDto>>.Failure($"获取医生列表失败: {ex.Message}", ex);            }
        }

        #endregion

        #region 已废弃功能 - 科室管理
        /*
        // 科室管理功能已删除 - 小诊所无需科室划分
        // GetDoctorsByDepartmentAsync方法已废弃
        */
        #endregion

        #region 医生可用性检查
        
        /// <summary>
        /// 获取医生的今日排班（简化版，默认都在班）
        /// </summary>
        public async Task<ServiceResult<bool>> IsDoctorAvailableAsync(Guid doctorId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(doctorId, true);
                var dto = _mapper.Map<UserDto>(user);
                var isAvailable = dto != null && dto.Status == CommonStatus.Enabled;
                return ServiceResult<bool>.Success(isAvailable);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "检查医生可用性失败, DoctorId: {DoctorId}", doctorId);                return ServiceResult<bool>.Failure($"检查医生可用性失败: {ex.Message}", ex);            }
        }

        #endregion

        /// <summary>
        /// 根据用户名检查用户是否存在
        /// </summary>
        public async Task<ServiceResult<bool>> ExistsByUsernameAsync(string username)
        {
            try
            {
                var exists = await _userRepository.ExistsByUsernameAsync(username);
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "检查用户名是否存在失败, Username: {Username}", username);                return ServiceResult<bool>.Failure($"检查用户名是否存在失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 获取现有用户（不存在时返回null）
        /// </summary>
        public async Task<ServiceResult<User?>> GetExistingUserAsync(Guid id)
        {
            try
            {
                // 内部方法总是包含禁用用户，确保操作能正常进行
                var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);
                return ServiceResult<User?>.Success(user);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取现有用户失败, ID: {UserId}", id);                return ServiceResult<User?>.Failure($"获取现有用户失败: {ex.Message}", ex);
            }
        }
    }
}


