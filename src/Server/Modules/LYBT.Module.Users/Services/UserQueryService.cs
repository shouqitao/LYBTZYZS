using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using System.Linq.Expressions;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services
{
    /// <summary>
    /// 用户查询服务 - 只负责读操作
    /// </summary>
    public class UserQueryService : IQueryService<UserDto>
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

        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var user = await _repository.GetByIdAsync(id);
                if (user == null)
                    return ServiceResult<UserDto>.Failure($"用户不存在: {id}");

                var dto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户失败 {UserId}", id);
                return ServiceResult<UserDto>.Failure($"获取用户失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<UserDto>>> GetAllAsync()
        {
            try
            {
                var users = await _repository.GetAllAsync();
                var dtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有用户失败");
                return ServiceResult<List<UserDto>>.Failure($"获取用户列表失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // 构建排序表达式
                Expression<Func<User, object>>? orderBy = null;
                if (!string.IsNullOrEmpty(query.SortField))
                {
                    orderBy = query.SortField.ToLower() switch
                    {
                        "username" => u => u.Username,
                        "realname" => u => u.RealName,
                        "email" => u => u.Email,
                        "createdat" => u => u.CreatedAt,
                        _ => u => u.Id
                    };
                }

                var pagedUsers = await _repository.GetPagedAsync(
                    null,  // predicate
                    query.PageIndex,
                    query.PageSize,
                    orderBy,
                    !query.IsDescending);

                var dtos = _mapper.Map<List<UserDto>>(pagedUsers.Items);
                var result = new PagedResult<UserDto>(
                    dtos,
                    pagedUsers.TotalCount,
                    query.PageIndex,
                    query.PageSize);

                return ServiceResult<PagedResult<UserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询用户失败");
                return ServiceResult<PagedResult<UserDto>>.Failure($"分页查询失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            try
            {
                var users = await _repository.SearchAsync(keyword, null, null, 50);
                var dtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户失败 {Keyword}", keyword);
                return ServiceResult<List<UserDto>>.Failure($"搜索失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据角色获取用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetByRoleAsync(string role)
        {
            try
            {
                // 转换字符串到 UserRole 枚举
                if (!Enum.TryParse<UserRole>(role, true, out var userRole))
                {
                    return ServiceResult<List<UserDto>>.Failure($"无效的角色: {role}");
                }
                
                var users = await _repository.GetByRoleAsync(userRole);
                var dtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据角色获取用户失败 {Role}", role);
                return ServiceResult<List<UserDto>>.Failure($"获取失败: {ex.Message}");
            }
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
                _logger.LogError(ex, "检查用户名存在失败 {Username}", username);
                return ServiceResult<bool>.Failure($"检查失败: {ex.Message}");
            }
        }
    }
}