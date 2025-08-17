using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Users;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Users.Services.Interfaces
{
    /// <summary>
    /// User模块核心业务服务接口
    /// UltraThink模块化架构：模块内部服务，不依赖外部SharedServices
    /// </summary>
    public interface IUserModuleService
    {
        #region 基础CRUD操作
        
        /// <summary>
        /// 分页查询用户
        /// </summary>
        Task<ServiceResult<PagedResult<UserInfo>>> GetPagedAsync(PagedQueryBaseDto query);
        
        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<ServiceResult<UserInfo>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 创建用户
        /// </summary>
        Task<ServiceResult<UserInfo>> CreateAsync(UserCreateInfo createInfo);
        
        /// <summary>
        /// 更新用户
        /// </summary>
        Task<ServiceResult<UserInfo>> UpdateAsync(UserUpdateInfo updateInfo);
        
        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
        
        #endregion
        
        #region 业务特定操作
        
        /// <summary>
        /// 搜索用户
        /// </summary>
        Task<ServiceResult<PagedResult<UserInfo>>> SearchUsersAsync(PagedQueryBaseDto request);
        
        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<ServiceResult<UserInfo>> GetByUsernameAsync(string username);
        
        /// <summary>
        /// 验证用户数据
        /// </summary>
        Task<ServiceResult> ValidateAsync(UserInfo userInfo);
        
        /// <summary>
        /// 检查用户名是否已被使用
        /// </summary>
        Task<ServiceResult<bool>> IsUsernameExistsAsync(string username, Guid? excludeId = null);
        
        /// <summary>
        /// 检查电话号码是否已被使用
        /// </summary>
        Task<ServiceResult<bool>> IsPhoneExistsAsync(string phone, Guid? excludeId = null);
        
        #endregion
        
        #region 密码管理
        
        /// <summary>
        /// 重置用户密码
        /// </summary>
        Task<ServiceResult<string>> ResetPasswordAsync(Guid userId);
        
        /// <summary>
        /// 更改用户密码
        /// </summary>
        Task<ServiceResult> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
        
        /// <summary>
        /// 强制更改用户密码（管理员操作）
        /// </summary>
        Task<ServiceResult> ForceChangePasswordAsync(Guid userId, string newPassword);
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 启用用户
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用用户
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);
        
        /// <summary>
        /// 锁定用户
        /// </summary>
        Task<ServiceResult> LockAsync(Guid id);
        
        /// <summary>
        /// 解锁用户
        /// </summary>
        Task<ServiceResult> UnlockAsync(Guid id);
        
        #endregion
        
        #region 角色和权限
        
        /// <summary>
        /// 获取用户角色
        /// </summary>
        Task<ServiceResult<IEnumerable<string>>> GetUserRolesAsync(Guid userId);
        
        /// <summary>
        /// 设置用户角色
        /// </summary>
        Task<ServiceResult> SetUserRoleAsync(Guid userId, string role);
        
        /// <summary>
        /// 获取所有可用角色
        /// </summary>
        Task<ServiceResult<IEnumerable<string>>> GetAvailableRolesAsync();
        
        #endregion
        
        #region 统计查询
        
        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        Task<ServiceResult<UserStatisticsInfo>> GetStatisticsAsync();
        
        /// <summary>
        /// 获取在线用户列表
        /// </summary>
        Task<ServiceResult<IEnumerable<UserInfo>>> GetOnlineUsersAsync();
        
        /// <summary>
        /// 获取最近活跃用户
        /// </summary>
        Task<ServiceResult<IEnumerable<UserInfo>>> GetRecentActiveAsync(int count = 10);
        
        #endregion
        
        #region 导入导出功能
        
        /// <summary>
        /// 导入用户数据
        /// </summary>
        Task<ServiceResult<IEnumerable<UserInfo>>> ImportAsync(string filePath);
        
        /// <summary>
        /// 导出用户数据
        /// </summary>
        Task<ServiceResult> ExportAsync(IEnumerable<Guid> userIds, string filePath);
        
        /// <summary>
        /// 生成导入模板
        /// </summary>
        Task<ServiceResult> GenerateImportTemplateAsync(string filePath);
        
        #endregion
    }
    
    /// <summary>
    /// 用户统计信息
    /// </summary>
    public class UserStatisticsInfo
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int OnlineCount { get; set; }
        public int LockedCount { get; set; }
        public int NewThisMonthCount { get; set; }
        public Dictionary<string, int> RoleCounts { get; set; } = new();
        public DateTime LastLoginTime { get; set; }
        public string? MostActiveUser { get; set; }
    }
}