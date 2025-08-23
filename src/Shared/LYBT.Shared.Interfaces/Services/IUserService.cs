using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 用户服务接口 - UltraThink统一标准
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 分页查询用户
        /// </summary>
        Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);
        
        /// <summary>
        /// 创建新用户 - UltraThink优化：使用统一变更DTO
        /// </summary>
        Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto);
        
        /// <summary>
        /// 更新用户信息 - UltraThink优化：消除ID参数重复
        /// </summary>
        Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto);
        
        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
        
        /// <summary>
        /// 启用用户
        /// </summary>
        Task<ServiceResult<bool>> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用用户
        /// </summary>
        Task<ServiceResult<bool>> DisableAsync(Guid id);
        
        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);
        
        /// <summary>
        /// 批量启用用户
        /// </summary>
        Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);
        
        /// <summary>
        /// 批量禁用用户
        /// </summary>
        Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);
        
        /// <summary>
        /// 重置用户密码
        /// </summary>
        Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword);
        
        /// <summary>
        /// 修改用户密码
        /// </summary>
        Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
        
        /// <summary>
        /// 修改用户个人信息 - UltraThink优化：使用DTO模式保持一致性
        /// </summary>
        Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto);
        
        /// <summary>
        /// 获取所有角色列表
        /// </summary>
        Task<ServiceResult<List<object>>> GetRolesAsync();
        
        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();
        
        /// <summary>
        /// 搜索用户
        /// </summary>
        Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);
        
        #region 已废弃功能 - UltraThink精简
        /*
        /// <summary>
        /// 获取用户统计信息 (已废弃)
        /// </summary>
        Task<ServiceResult<object>> GetStatisticsAsync();
        */
        #endregion

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        Task<ServiceResult<bool>> ValidateUsernameAsync(string username);
        
        /// <summary>
        /// 获取用户操作日志
        /// </summary>
        Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query);
    }
}