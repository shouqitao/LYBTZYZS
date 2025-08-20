using System.Linq;
using AutoMapper;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.Modules.Users.Api;
using LYBT.Shared.Models.Common;

namespace LYBT.Desktop.Users.Services
{
    /// <summary>
    /// User模块核心业务服务实现
    /// UltraThink v2.0架构：直接使用DTO，移除Info层转换逻辑
    /// </summary>
    public class UserModuleService
    {
        private readonly IUserApi _apiService;
        private readonly IMapper _mapper;
        
        public UserModuleService(IUserApi apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // UltraThink v2.0: 调用Refit API客户端
                var apiResponse = await _apiService.GetUsersAsync(
                    page: query.PageIndex,
                    pageSize: query.PageSize,
                    keyword: query.Keyword);
                
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<PagedResult<UserDto>>.Failure("获取用户列表失败");
                }
                
                // 转换Refit响应为标准格式
                var pagedData = apiResponse.Content;
                var result = new PagedResult<UserDto>(
                    pagedData.Items.ToList(),
                    pagedData.TotalCount,
                    pagedData.CurrentPage,
                    pagedData.PageSize);
                
                return ServiceResult<PagedResult<UserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<UserDto>>.Failure($"获取用户列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");
                }
                
                // UltraThink v2.0: 调用Refit API客户端
                var apiResponse = await _apiService.GetUserByIdAsync(id);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<UserDto>.Failure("获取用户详情失败");
                }
                
                return ServiceResult<UserDto>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.Failure($"获取用户详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto createDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用CreateDto进行业务验证
                var validationResult = await ValidateCreateDtoAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查用户名是否已存在
                var usernameExistsResult = await IsUsernameExistsAsync(createDto.Username);
                if (usernameExistsResult.IsSuccess && usernameExistsResult.Data)
                {
                    return ServiceResult<UserDto>.Failure("该用户名已被使用");
                }
                
                // 检查电话号码是否已存在
                if (!string.IsNullOrEmpty(createDto.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(createDto.PhoneNumber);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<UserDto>.Failure("该电话号码已被使用");
                    }
                }
                
                // UltraThink v2.0: 调用Refit API客户端
                var apiResponse = await _apiService.CreateUserAsync(createDto);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<UserDto>.Failure("创建用户失败");
                }
                
                return ServiceResult<UserDto>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.Failure($"创建用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<UserDto>> UpdateAsync(UserUpdateDto updateDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用UpdateDto进行业务验证
                var validationResult = await ValidateUpdateDtoAsync(updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查用户名是否已被其他用户使用
                var usernameExistsResult = await IsUsernameExistsAsync(updateDto.Username, updateDto.Id);
                if (usernameExistsResult.IsSuccess && usernameExistsResult.Data)
                {
                    return ServiceResult<UserDto>.Failure("该用户名已被其他用户使用");
                }
                
                // 检查电话号码是否已被其他用户使用
                if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(updateDto.PhoneNumber, updateDto.Id);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<UserDto>.Failure("该电话号码已被其他用户使用");
                    }
                }
                
                // UltraThink v2.0: 调用Refit API客户端
                var apiResponse = await _apiService.UpdateUserAsync(updateDto.Id, updateDto);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<UserDto>.Failure("更新用户失败");
                }
                
                return ServiceResult<UserDto>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.Failure($"更新用户异常: {ex.Message}");
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
                
                var apiResponse = await _apiService.ToggleStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("删除用户失败");
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
        
        public async Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<UserDto>>.Failure($"搜索用户异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<UserDto>.Failure("用户名不能为空");
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
                    return ServiceResult<UserDto>.Failure(result.ErrorMessage);
                }
                
                var user = result.Data.Items.FirstOrDefault(u => u.Username == username);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("未找到指定用户");
                }
                
                return ServiceResult<UserDto>.Success(user);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDto>.Failure($"根据用户名获取用户异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 简化验证方法 - 移除冗余的通用验证，合并Create/Update验证逻辑
        private async Task<ServiceResult> ValidateCreateDtoAsync(UserCreateDto createDto)
        {
            if (createDto == null) return ServiceResult.Failure("创建用户信息不能为空");
            if (string.IsNullOrWhiteSpace(createDto.Username)) return ServiceResult.Failure("用户名不能为空");
            if (createDto.Username.Length < 3 || createDto.Username.Length > 50) return ServiceResult.Failure("用户名长度必须在3到50个字符之间");
            if (string.IsNullOrWhiteSpace(createDto.RealName)) return ServiceResult.Failure("真实姓名不能为空");
            if (createDto.RealName.Length > 50) return ServiceResult.Failure("真实姓名长度不能超过50个字符");
            return ServiceResult.Success();
        }
        
        private async Task<ServiceResult> ValidateUpdateDtoAsync(UserUpdateDto updateDto)
        {
            if (updateDto == null) return ServiceResult.Failure("更新用户信息不能为空");
            if (string.IsNullOrWhiteSpace(updateDto.Username)) return ServiceResult.Failure("用户名不能为空");
            if (updateDto.Username.Length < 3 || updateDto.Username.Length > 50) return ServiceResult.Failure("用户名长度必须在3到50个字符之间");
            if (string.IsNullOrWhiteSpace(updateDto.RealName)) return ServiceResult.Failure("真实姓名不能为空");
            return ServiceResult.Success();
        }
        
        private async Task<ServiceResult<bool>> IsUsernameExistsAsync(string username, Guid? excludeId = null)
        {
            try
            {
                var searchResult = await SearchUsersAsync(new PagedQueryBaseDto { Keyword = username, PageSize = 50 });
                if (!searchResult.IsSuccess)
                {
                    return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
                }
                
                var exists = searchResult.Data.Items.Any(u => 
                    u.Username == username && 
                    (excludeId == null || u.Id != excludeId.Value));
                
                return ServiceResult<bool>.Success(exists);
            }
            catch
            {
                return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
            }
        }
        
        private async Task<ServiceResult<bool>> IsPhoneExistsAsync(string phoneNumber, Guid? excludeId = null)
        {
            try
            {
                var searchResult = await SearchUsersAsync(new PagedQueryBaseDto { Keyword = phoneNumber, PageSize = 50 });
                if (!searchResult.IsSuccess)
                {
                    return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
                }
                
                var exists = searchResult.Data.Items.Any(u => 
                    u.PhoneNumber == phoneNumber && 
                    (excludeId == null || u.Id != excludeId.Value));
                
                return ServiceResult<bool>.Success(exists);
            }
            catch
            {
                return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
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
                
                var apiResponse = await _apiService.ResetPasswordAsync(userId);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<string>.Failure("重置密码失败");
                }
                
                return ServiceResult<string>.Success("密码重置成功");
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
                
                var apiResponse = await _apiService.ChangePasswordAsync(changePasswordDto);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("更改密码失败");
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
                
                var apiResponse = await _apiService.ToggleStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("启用用户失败");
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
                
                var apiResponse = await _apiService.ToggleStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("禁用用户失败");
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
        
        // UltraThink v2.0: 移除统计查询功能 - 删除过度设计的统计功能
        
        // UltraThink v2.0: 移除导入导出功能 - 删除过度设计的导入导出功能
        
        #region 批量操作
        
        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResult<int>.Failure("用户ID列表不能为空");
                }

                var batchDto = new BatchIdsDto { Ids = ids };
                var apiResponse = await _apiService.BatchEnableAsync(batchDto);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<int>.Failure("批量启用用户失败");
                }

                return ServiceResult<int>.Success(ids.Count);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量启用用户异常: {ex.Message}");
            }
        }

        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResult<int>.Failure("用户ID列表不能为空");
                }

                var batchDto = new BatchIdsDto { Ids = ids };
                var apiResponse = await _apiService.BatchDisableAsync(batchDto);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<int>.Failure("批量禁用用户失败");
                }

                return ServiceResult<int>.Success(ids.Count);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量禁用用户异常: {ex.Message}");
            }
        }
        
        #endregion
    }
}