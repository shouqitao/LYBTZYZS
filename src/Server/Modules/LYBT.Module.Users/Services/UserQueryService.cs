using AutoMapper;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services
{
    /// <summary>
    /// 用户查询服务 - 简化版
    /// </summary>
    public class UserQueryService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserQueryService> _logger;

        public UserQueryService(
            IUserRepository repository,
            IMapper mapper,
            ILogger<UserQueryService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var user = await _repository.GetByIdAsync(id);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                var dto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户失败 {Id}", id);
                return ServiceResult<UserDto>.Failure($"获取失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            try
            {
                var user = await _repository.GetByUsernameAsync(username);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                var dto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名获取用户失败 {Username}", username);
                return ServiceResult<UserDto>.Failure($"获取失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询用户
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto searchDto)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(1, 20);
                var dtos = _mapper.Map<List<UserDto>>(pagedResult.Items);
                
                var result = new PagedResult<UserDto>
                {
                    Items = dtos,
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                
                return ServiceResult<PagedResult<UserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询用户失败");
                return ServiceResult<PagedResult<UserDto>>.Failure($"分页查询失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            // 简化版：暂时返回空列表
            return Task.FromResult(ServiceResult<List<UserDto>>.Success(new List<UserDto>()));
        }

        /// <summary>
        /// 根据角色获取用户列表
        /// </summary>
        public Task<ServiceResult<List<UserDto>>> GetByRoleAsync(string role)
        {
            // 简化版：暂时返回空列表
            return Task.FromResult(ServiceResult<List<UserDto>>.Success(new List<UserDto>()));
        }

        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        public async Task<ServiceResult<bool>> ExistsAsync(string username)
        {
            try
            {
                var exists = await _repository.IsUsernameExistsAsync(username);
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查用户名存在性失败 {Username}", username);
                return ServiceResult<bool>.Failure($"检查失败: {ex.Message}");
            }
        }
    }
}