using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Models.Users;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Users.Services.Interfaces;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Users.Services
{
    /// <summary>
    /// User模块核心业务服务实现
    /// UltraThink模块化架构：封装模块业务逻辑，使用AutoMapper进行DTO↔Info转换
    /// </summary>
    public class UserModuleService : IUserModuleService
    {
        private readonly IUserApiService _apiService;
        private readonly IMapper _mapper;
        
        public UserModuleService(IUserApiService apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<UserInfo>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // 转换为用户专用查询DTO
                var userQuery = new UserPagedQueryDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = query.Keyword,
                    SortField = query.SortField,
                    SortDirection = query.SortDirection
                };

                // UltraThink四层架构：API调用获取DTOs
                var apiResult = await _apiService.GetPagedAsync(userQuery);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PagedResult<UserInfo>>.Failure(
                        apiResult.ErrorMessage ?? "获取用户列表失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTOs → Infos
                var userInfos = _mapper.Map<List<UserInfo>>(apiResult.Data.Items);
                var result = new PagedResult<UserInfo>(
                    userInfos,
                    apiResult.Data.TotalCount,
                    apiResult.Data.CurrentPage,
                    apiResult.Data.PageSize);
                
                return ServiceResult<PagedResult<UserInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<UserInfo>>.Failure($"获取用户列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<UserInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<UserInfo>.Failure("用户ID不能为空");
                }
                
                // UltraThink四层架构：API调用获取DTO
                var apiResult = await _apiService.GetByIdAsync(id);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<UserInfo>.Failure(
                        apiResult.ErrorMessage ?? "获取用户详情失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var userInfo = _mapper.Map<UserInfo>(apiResult.Data);
                return ServiceResult<UserInfo>.Success(userInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserInfo>.Failure($"获取用户详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<UserInfo>> CreateAsync(UserCreateInfo createInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<UserInfo>(createInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查用户名是否已存在
                var usernameExistsResult = await IsUsernameExistsAsync(createInfo.Username);
                if (usernameExistsResult.IsSuccess && usernameExistsResult.Data)
                {
                    return ServiceResult<UserInfo>.Failure("该用户名已被使用");
                }
                
                // 检查电话号码是否已存在
                if (!string.IsNullOrEmpty(createInfo.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(createInfo.PhoneNumber);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<UserInfo>.Failure("该电话号码已被使用");
                    }
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var createDto = _mapper.Map<UserCreateDto>(createInfo);
                
                // API调用
                var apiResult = await _apiService.CreateAsync(createDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<UserInfo>.Failure(
                        apiResult.ErrorMessage ?? "创建用户失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var userInfo = _mapper.Map<UserInfo>(apiResult.Data);
                return ServiceResult<UserInfo>.Success(userInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserInfo>.Failure($"创建用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<UserInfo>> UpdateAsync(UserUpdateInfo updateInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<UserInfo>(updateInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查用户名是否已被其他用户使用
                var usernameExistsResult = await IsUsernameExistsAsync(updateInfo.Username, updateInfo.Id);
                if (usernameExistsResult.IsSuccess && usernameExistsResult.Data)
                {
                    return ServiceResult<UserInfo>.Failure("该用户名已被其他用户使用");
                }
                
                // 检查电话号码是否已被其他用户使用
                if (!string.IsNullOrEmpty(updateInfo.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(updateInfo.PhoneNumber, updateInfo.Id);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<UserInfo>.Failure("该电话号码已被其他用户使用");
                    }
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var updateDto = _mapper.Map<UserUpdateDto>(updateInfo);
                
                // API调用
                var apiResult = await _apiService.UpdateAsync(updateDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<UserInfo>.Failure(
                        apiResult.ErrorMessage ?? "更新用户失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var userInfo = _mapper.Map<UserInfo>(apiResult.Data);
                return ServiceResult<UserInfo>.Success(userInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserInfo>.Failure($"更新用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("用户ID不能为空");
                }
                
                var apiResult = await _apiService.DeleteAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "删除用户失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"删除用户异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 业务特定操作
        
        public async Task<ServiceResult<PagedResult<UserInfo>>> SearchUsersAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<UserInfo>>.Failure($"搜索用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<UserInfo>> GetByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<UserInfo>.Failure("用户名不能为空");
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1,
                    Keyword = username
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<UserInfo>.Failure(result.ErrorMessage);
                }
                
                var user = result.Data.Items.FirstOrDefault(u => u.Username == username);
                if (user == null)
                {
                    return ServiceResult<UserInfo>.Failure("未找到指定用户");
                }
                
                return ServiceResult<UserInfo>.Success(user);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserInfo>.Failure($"根据用户名获取用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ValidateAsync(UserInfo userInfo)
        {
            try
            {
                if (userInfo == null)
                {
                    return ServiceResult.Failure("用户信息不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(userInfo.Username))
                {
                    return ServiceResult.Failure("用户名不能为空");
                }
                
                if (userInfo.Username.Length < 3 || userInfo.Username.Length > 50)
                {
                    return ServiceResult.Failure("用户名长度必须在3到50个字符之间");
                }
                
                if (string.IsNullOrWhiteSpace(userInfo.RealName))
                {
                    return ServiceResult.Failure("真实姓名不能为空");
                }
                
                if (userInfo.RealName.Length > 50)
                {
                    return ServiceResult.Failure("真实姓名长度不能超过50个字符");
                }
                
                if (!string.IsNullOrEmpty(userInfo.PhoneNumber) && userInfo.PhoneNumber.Length > 20)
                {
                    return ServiceResult.Failure("电话号码长度不能超过20个字符");
                }
                
                if (!string.IsNullOrEmpty(userInfo.Email) && userInfo.Email.Length > 100)
                {
                    return ServiceResult.Failure("电子邮箱长度不能超过100个字符");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证用户信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> IsUsernameExistsAsync(string username, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<bool>.Success(false);
                }
                
                var userResult = await GetByUsernameAsync(username);
                if (!userResult.IsSuccess)
                {
                    // 如果找不到用户，说明用户名不存在
                    return ServiceResult<bool>.Success(false);
                }
                
                var exists = excludeId == null || userResult.Data.Id != excludeId.Value;
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查用户名异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> IsPhoneExistsAsync(string phone, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return ServiceResult<bool>.Success(false);
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 50,
                    Keyword = phone
                };
                
                var searchResult = await GetPagedAsync(query);
                if (!searchResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(searchResult.ErrorMessage);
                }
                
                var exists = searchResult.Data.Items.Any(u => 
                    u.PhoneNumber == phone && 
                    (excludeId == null || u.Id != excludeId.Value));
                
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查电话号码异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 密码管理
        
        public async Task<ServiceResult<string>> ResetPasswordAsync(Guid userId)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return ServiceResult<string>.Failure("用户ID不能为空");
                }
                
                var apiResult = await _apiService.ResetPasswordAsync(userId);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult<string>.Failure(apiResult.ErrorMessage ?? "重置密码失败");
                }
                
                return ServiceResult<string>.Success(apiResult.Data ?? "密码重置成功");
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Failure($"重置密码异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return ServiceResult.Failure("用户ID不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(oldPassword))
                {
                    return ServiceResult.Failure("原密码不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return ServiceResult.Failure("新密码不能为空");
                }
                
                if (newPassword.Length < 6)
                {
                    return ServiceResult.Failure("新密码长度不能少于6个字符");
                }
                
                var changePasswordDto = new ChangePasswordDto
                {
                    UserId = userId,
                    OldPassword = oldPassword,
                    NewPassword = newPassword
                };
                
                var apiResult = await _apiService.ChangePasswordAsync(changePasswordDto);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "更改密码失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"更改密码异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ForceChangePasswordAsync(Guid userId, string newPassword)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return ServiceResult.Failure("用户ID不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return ServiceResult.Failure("新密码不能为空");
                }
                
                if (newPassword.Length < 6)
                {
                    return ServiceResult.Failure("新密码长度不能少于6个字符");
                }
                
                // 这里应该调用API的强制更改密码接口
                // 目前模拟实现
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"强制更改密码异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("用户ID不能为空");
                }
                
                var apiResult = await _apiService.EnableAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "启用用户失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"启用用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("用户ID不能为空");
                }
                
                var apiResult = await _apiService.DisableAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "禁用用户失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"禁用用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> LockAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("用户ID不能为空");
                }
                
                // 这里应该调用API的锁定接口
                // 目前模拟实现
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"锁定用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> UnlockAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("用户ID不能为空");
                }
                
                // 这里应该调用API的解锁接口
                // 目前模拟实现
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"解锁用户异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 角色和权限
        
        public async Task<ServiceResult<IEnumerable<string>>> GetUserRolesAsync(Guid userId)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return ServiceResult<IEnumerable<string>>.Failure("用户ID不能为空");
                }
                
                var userResult = await GetByIdAsync(userId);
                if (!userResult.IsSuccess)
                {
                    return ServiceResult<IEnumerable<string>>.Failure(userResult.ErrorMessage);
                }
                
                var roles = new List<string> { userResult.Data.Role.ToString() };
                return ServiceResult<IEnumerable<string>>.Success(roles);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<string>>.Failure($"获取用户角色异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> SetUserRoleAsync(Guid userId, string role)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return ServiceResult.Failure("用户ID不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(role))
                {
                    return ServiceResult.Failure("角色不能为空");
                }
                
                if (!Enum.TryParse<UserRole>(role, out var userRole))
                {
                    return ServiceResult.Failure("无效的角色");
                }
                
                // 这里应该调用API的设置角色接口
                // 目前模拟实现
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"设置用户角色异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<string>>> GetAvailableRolesAsync()
        {
            try
            {
                var roles = Enum.GetNames(typeof(UserRole));
                return ServiceResult<IEnumerable<string>>.Success(roles);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<string>>.Failure($"获取可用角色异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 统计查询
        
        public async Task<ServiceResult<UserStatisticsInfo>> GetStatisticsAsync()
        {
            try
            {
                // 获取用户总数据进行统计
                var allUsersResult = await GetPagedAsync(new PagedQueryBaseDto 
                { 
                    PageIndex = 1, 
                    PageSize = 10000 // 获取足够多的数据进行统计
                });
                
                if (!allUsersResult.IsSuccess)
                {
                    return ServiceResult<UserStatisticsInfo>.Failure(allUsersResult.ErrorMessage);
                }
                
                var users = allUsersResult.Data.Items;
                var now = DateTime.Now;
                var thisMonthStart = new DateTime(now.Year, now.Month, 1);
                
                var statistics = new UserStatisticsInfo
                {
                    TotalCount = users.Count,
                    ActiveCount = users.Count(u => u.Status == CommonStatus.Enabled),
                    InactiveCount = users.Count(u => u.Status != CommonStatus.Enabled),
                    OnlineCount = users.Count(u => u.IsOnline),
                    LockedCount = 0, // 需要根据实际状态字段调整
                    NewThisMonthCount = users.Count(u => u.CreateTime >= thisMonthStart),
                    RoleCounts = users.GroupBy(u => u.Role.ToString())
                                     .ToDictionary(g => g.Key, g => g.Count()),
                    LastLoginTime = users.Where(u => u.LastLoginTime.HasValue)
                                        .Max(u => u.LastLoginTime) ?? DateTime.MinValue,
                    MostActiveUser = users.OrderByDescending(u => u.LastLoginTime ?? DateTime.MinValue)
                                         .FirstOrDefault()?.RealName
                };
                
                return ServiceResult<UserStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserStatisticsInfo>.Failure($"获取用户统计信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<UserInfo>>> GetOnlineUsersAsync()
        {
            try
            {
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1000
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<UserInfo>>.Failure(result.ErrorMessage);
                }
                
                var onlineUsers = result.Data.Items.Where(u => u.IsOnline);
                return ServiceResult<IEnumerable<UserInfo>>.Success(onlineUsers);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<UserInfo>>.Failure($"获取在线用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<UserInfo>>> GetRecentActiveAsync(int count = 10)
        {
            try
            {
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = count * 2, // 获取更多数据以便筛选活跃用户
                    SortField = "LastLoginTime",
                    SortDirection = "DESC"
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<UserInfo>>.Failure(result.ErrorMessage);
                }
                
                var recentActive = result.Data.Items
                    .Where(u => u.Status == CommonStatus.Enabled)
                    .OrderByDescending(u => u.LastLoginTime ?? DateTime.MinValue)
                    .Take(count);
                
                return ServiceResult<IEnumerable<UserInfo>>.Success(recentActive);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<UserInfo>>.Failure($"获取最近活跃用户异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 导入导出功能
        
        public async Task<ServiceResult<IEnumerable<UserInfo>>> ImportAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult<IEnumerable<UserInfo>>.Failure("文件路径不能为空");
                }
                
                // TODO: 实现实际的导入逻辑
                // 这里是预留功能，返回空列表表示功能开发中
                return ServiceResult<IEnumerable<UserInfo>>.Success(new List<UserInfo>());
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<UserInfo>>.Failure($"导入用户数据异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ExportAsync(IEnumerable<Guid> userIds, string filePath)
        {
            try
            {
                if (userIds == null || !userIds.Any())
                {
                    return ServiceResult.Failure("导出的用户ID列表不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult.Failure("导出文件路径不能为空");
                }
                
                // TODO: 实现实际的导出逻辑
                // 这里是预留功能，返回成功表示功能开发中
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"导出用户数据异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> GenerateImportTemplateAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult.Failure("模板文件路径不能为空");
                }
                
                // TODO: 实现实际的模板生成逻辑
                // 这里是预留功能，返回成功表示功能开发中
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"生成导入模板异常: {ex.Message}");
            }
        }
        
        #endregion
    }
}