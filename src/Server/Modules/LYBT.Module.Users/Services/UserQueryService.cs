using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services
{

    /// <summary>
    /// 用户查询服务 - UltraThink架构
    /// 职责：分页查询，搜索筛选，用户查询，角色获取
    /// </summary>
    public class UserQueryService(
        AppDbContext context,
        IMapper mapper,
        ILogger<UserQueryService> logger) : IUserQueryService
    {
        private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                var dto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(dto);
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
                var queryable = _context.Users.AsQueryable();

                // 基础筛选 - 排除已删除的用户
                queryable = queryable.Where(u => u.Status != CommonStatus.Disabled);

                // 应用搜索条件（如果有）
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    var keyword = query.Keyword.Trim();
                    queryable = queryable.Where(u =>
                        u.Username.Contains(keyword) ||
                        u.RealName.Contains(keyword) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)) ||
                        (u.Email != null && u.Email.Contains(keyword)));
                }

                // 角色筛选
                if (query.Role.HasValue)
                {
                    queryable = queryable.Where(u => u.Role == query.Role.Value);
                }

                // 状态筛选
                if (query.Status.HasValue)
                {
                    queryable = queryable.Where(u => u.Status == query.Status.Value);
                }

                // 获取总数
                var totalCount = await queryable.CountAsync();

                // 排序和分页
                var users = await queryable
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<UserDto>>(users);

                var pagedResult = new PagedResult<UserDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

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

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == userName);

                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                var dto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(dto);
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
                var users = await _context.Users
                    .Where(u => u.Status == CommonStatus.Enabled)
                    .OrderBy(u => u.RealName)
                    .ToListAsync();

                var dtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(dtos);
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

                var searchTerm = keyword.Trim();
                var users = await _context.Users
                    .Where(u => u.Status != CommonStatus.Disabled &&
                               (u.Username.Contains(searchTerm) ||
                                u.RealName.Contains(searchTerm) ||
                                (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)) ||
                                (u.Email != null && u.Email.Contains(searchTerm))))
                    .OrderBy(u => u.RealName)
                    .Take(50) // 限制搜索结果数量
                    .ToListAsync();

                var dtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(dtos);
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
                // 简化角色获取 - 返回枚举值
                var roles = new List<object>
                {
                    new { Value = (int)UserRole.Admin, Text = "管理员" },
                    new { Value = (int)UserRole.Doctor, Text = "医生" }
                };

                await Task.CompletedTask; // 保持异步签名
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

                var exists = await _context.Users
                    .AnyAsync(u => u.Username == userName);

                return ServiceResult<bool>.Success(!exists);
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
                var doctors = await _context.Users
                    .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                    .OrderBy(u => u.RealName)
                    .ToListAsync();

                var dtos = _mapper.Map<List<UserDto>>(doctors);
                return ServiceResult<List<UserDto>>.Success(dtos);
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

                var doctorExists = await _context.Users
                    .AnyAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled);

                return ServiceResult<bool>.Success(doctorExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查医生可用性失败: {DoctorId}", doctorId);
                return ServiceResult<bool>.Failure($"检查医生可用性失败: {ex.Message}");
            }
        }
    }
}
