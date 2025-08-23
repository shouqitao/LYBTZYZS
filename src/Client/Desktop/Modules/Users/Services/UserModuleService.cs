using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.Desktop.Modules.Users.Api;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Users.Services
{
    /// <summary>
    /// User模块核心业务服务实现
    /// UltraThink v2.0架构：直接使用DTO，移除Info层转换逻辑
    /// 实现IUserService接口以支持依赖注入
    /// </summary>
    public class UserModuleService : IUserService
    {
        private readonly IUserApi _apiService;
        private readonly IMapper _mapper;
        
        public UserModuleService(IUserApi apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
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
        
        /// <summary>
        /// 分页查询用户 - 实现IUserService接口
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
        {
            try
            {
                // 转换为基础查询DTO
                var baseQuery = new PagedQueryBaseDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = query.Keyword
                };
                
                // UltraThink v2.0: 调用Refit API客户端
                var apiResponse = await _apiService.GetUsersAsync(
                    page: baseQuery.PageIndex,
                    pageSize: baseQuery.PageSize,
                    keyword: baseQuery.Keyword);
                
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<PagedResult<UserDto>>.Failure("获取用户列表失败");
                }
                
                // UltraThink v2.0: API已返回标准PagedResult格式，直接使用
                var result = apiResponse.Content;
                
                return ServiceResult<PagedResult<UserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<UserDto>>.Failure($"获取用户列表异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建新用户 - UltraThink优化：使用统一变更DTO
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto)
        {
            try
            {
                // 设置为创建操作
                dto.IsCreateOperation = true;
                
                // UltraThink v2.0: 直接使用UserMutationDto进行业务验证
                var validationResult = await ValidateMutationDtoAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查用户名是否已存在
                var usernameExistsResult = await IsUsernameExistsAsync(dto.Username);
                if (usernameExistsResult.IsSuccess && usernameExistsResult.Data)
                {
                    return ServiceResult<UserDto>.Failure("该用户名已被使用");
                }
                
                // 检查电话号码是否已存在
                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(dto.PhoneNumber);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<UserDto>.Failure("该电话号码已被使用");
                    }
                }
                
                // UltraThink v2.0: 直接调用Refit API客户端使用UserMutationDto
                var apiResponse = await _apiService.CreateUserAsync(dto);
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
        
        /// <summary>
        /// 更新用户信息 - UltraThink优化：使用统一变更DTO
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto)
        {
            try
            {
                if (dto.Id == Guid.Empty)
                {
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");
                }
                
                // 设置为更新操作
                dto.IsCreateOperation = false;
                
                // UltraThink v2.0: 直接使用UserMutationDto进行业务验证
                var validationResult = await ValidateMutationDtoAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查用户名是否已被其他用户使用
                var usernameExistsResult = await IsUsernameExistsAsync(dto.Username, dto.Id);
                if (usernameExistsResult.IsSuccess && usernameExistsResult.Data)
                {
                    return ServiceResult<UserDto>.Failure("该用户名已被其他用户使用");
                }
                
                // 检查电话号码是否已被其他用户使用
                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(dto.PhoneNumber, dto.Id);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<UserDto>.Failure("该电话号码已被其他用户使用");
                    }
                }
                
                // UltraThink v2.0: 直接调用Refit API客户端使用UserMutationDto
                var apiResponse = await _apiService.UpdateUserAsync(dto.Id, dto);
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
        
        /// <summary>
        /// 删除用户（软删除）- 实现IUserService接口签名
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }
                
                var apiResponse = await _apiService.ToggleStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure("删除用户失败");
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除用户异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 启用用户 - 实现IUserService接口签名
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }
                
                var apiResponse = await _apiService.ToggleStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure("启用用户失败");
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"启用用户异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 禁用用户 - 实现IUserService接口签名
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }
                
                var apiResponse = await _apiService.ToggleStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure("禁用用户失败");
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"禁用用户异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 业务特定操作
        
        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<UserDto>.Failure("用户名不能为空");
                }
                
                var query = new UserPagedQueryDto
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
        
        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResult<int>.Failure("用户ID列表不能为空");
                }
                
                int successCount = 0;
                foreach (var id in ids)
                {
                    var result = await EnableAsync(id);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                }
                
                return ServiceResult<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量启用用户异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResult<int>.Failure("用户ID列表不能为空");
                }
                
                int successCount = 0;
                foreach (var id in ids)
                {
                    var result = await DisableAsync(id);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                }
                
                return ServiceResult<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量禁用用户异常: {ex.Message}");
            }
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
                    PageIndex = 1,
                    PageSize = 100, // 搜索时使用较大的页面大小
                    Keyword = keyword
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<UserDto>>.Failure(result.ErrorMessage);
                }
                
                return ServiceResult<List<UserDto>>.Success(result.Data.Items.ToList());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<UserDto>>.Failure($"搜索用户异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            try
            {
                var query = new UserPagedQueryDto
                {
                    PageIndex = 1,
                    PageSize = 1000, // 获取所有活跃用户
                    Keyword = string.Empty
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<UserDto>>.Failure(result.ErrorMessage);
                }
                
                // 过滤活跃用户
                var activeUsers = result.Data.Items.Where(u => u.IsActive).ToList();
                return ServiceResult<List<UserDto>>.Success(activeUsers);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<UserDto>>.Failure($"获取活跃用户列表异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 密码管理
        
        /// <summary>
        /// 重置用户密码 - 实现IUserService接口签名
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return ServiceResult<bool>.Failure("新密码不能为空");
                }
                
                if (newPassword.Length < 6)
                {
                    return ServiceResult<bool>.Failure("新密码长度不能少于6个字符");
                }
                
                var apiResponse = await _apiService.ResetPasswordAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure("重置密码失败");
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"重置密码异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 修改用户密码 - 实现IUserService接口签名
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(oldPassword))
                {
                    return ServiceResult<bool>.Failure("原密码不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return ServiceResult<bool>.Failure("新密码不能为空");
                }
                
                if (newPassword.Length < 6)
                {
                    return ServiceResult<bool>.Failure("新密码长度不能少于6个字符");
                }
                
                var changePasswordDto = new ChangePasswordDto
                {
                    UserId = id,
                    OldPassword = oldPassword,
                    NewPassword = newPassword
                };
                
                var apiResponse = await _apiService.ChangePasswordAsync(changePasswordDto);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure("更改密码失败");
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"更改密码异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 修改用户个人信息 - UltraThink优化：使用DTO模式保持一致性
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto)
        {
            try
            {
                if (dto.UserId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }
                
                // 获取现有用户信息
                var existingUserResult = await GetByIdAsync(dto.UserId);
                if (!existingUserResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("获取用户信息失败");
                }
                
                var existingUser = existingUserResult.Data;
                // 直接使用UserMutationDto，无需额外转换
                
                var updateResult = await UpdateAsync(new UserMutationDto
                {
                    Id = dto.UserId,
                    Username = existingUser.Username,
                    RealName = dto.RealName,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    Role = existingUser.Role,
                    Status = existingUser.IsActive ? CommonStatus.Enabled : CommonStatus.Disabled,
                    IsCreateOperation = false
                });
                
                if (!updateResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(updateResult.ErrorMessage);
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"修改个人信息异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 角色管理
        
        /// <summary>
        /// 获取所有角色列表
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            try
            {
                var roles = Enum.GetNames(typeof(UserRole))
                    .Select(name => new { 
                        Value = name, 
                        Text = name,
                        EnumValue = (int)Enum.Parse(typeof(UserRole), name)
                    })
                    .Cast<object>()
                    .ToList();
                
                return ServiceResult<List<object>>.Success(roles);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<object>>.Failure($"获取角色列表异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 验证和辅助方法
        
        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<bool>.Failure("用户名不能为空");
                }
                
                var existsResult = await IsUsernameExistsAsync(username);
                if (!existsResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("验证用户名时发生错误");
                }
                
                // 返回true表示用户名可用（不存在）
                return ServiceResult<bool>.Success(!existsResult.Data);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"验证用户名异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取用户操作日志
        /// </summary>
        public async Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query)
        {
            try
            {
                // UltraThink v2.0: 简化版实现 - 20人以下小诊所不需要复杂的操作日志
                // 返回空结果，避免过度设计
                var emptyResult = new PagedResult<object>(
                    new List<object>(), 
                    0, 
                    query.PageIndex, 
                    query.PageSize);
                
                return ServiceResult<PagedResult<object>>.Success(emptyResult);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<object>>.Failure($"获取操作日志异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 私有方法
        
        // UltraThink v2.0: 简化验证方法 - 移除冗余的通用验证，合并Create/Update验证逻辑
        /// <summary>
        /// 统一的UserMutationDto验证方法 - UltraThink v2.0优化
        /// </summary>
        private async Task<ServiceResult> ValidateMutationDtoAsync(UserMutationDto dto)
        {
            if (dto == null) return ServiceResult.Failure("用户信息不能为空");
            if (string.IsNullOrWhiteSpace(dto.Username)) return ServiceResult.Failure("用户名不能为空");
            if (dto.Username.Length < 3 || dto.Username.Length > 50) return ServiceResult.Failure("用户名长度必须在3到50个字符之间");
            if (string.IsNullOrWhiteSpace(dto.RealName)) return ServiceResult.Failure("真实姓名不能为空");
            if (dto.RealName.Length > 50) return ServiceResult.Failure("真实姓名长度不能超过50个字符");
            
            // 创建操作时密码必填
            if (dto.IsCreateOperation && string.IsNullOrWhiteSpace(dto.Password))
                return ServiceResult.Failure("创建用户时密码不能为空");
                
            return ServiceResult.Success();
        }
        
private async Task<ServiceResult<bool>> IsUsernameExistsAsync(string username, Guid? excludeId = null)
        {
            try
            {
                var searchResult = await SearchAsync(username);
                if (!searchResult.IsSuccess)
                {
                    return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
                }
                
                var exists = searchResult.Data.Any(u => 
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
                var searchResult = await SearchAsync(phoneNumber);
                if (!searchResult.IsSuccess)
                {
                    return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
                }
                
                var exists = searchResult.Data.Any(u => 
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
    }
}