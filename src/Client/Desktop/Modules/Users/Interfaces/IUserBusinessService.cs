using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户业务服务接口 - UltraThink三层架构业务逻辑层
/// 职责：业务流程编排、CRUD操作、状态转换、事件处理
/// </summary>
public interface IUserBusinessService
{
    #region CRUD业务操作
    
    /// <summary>
    /// 创建用户（完整业务流程）
    /// </summary>
    Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto createDto);
    
    /// <summary>
    /// 更新用户信息（完整业务流程）
    /// </summary>
    Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto updateDto);
    
    /// <summary>
    /// 删除用户（软删除业务流程）
    /// </summary>
    Task<ServiceResult<bool>> DeleteUserAsync(Guid id);
    
    /// <summary>
    /// 批量删除用户
    /// </summary>
    Task<ServiceResult<BatchOperationResult>> BatchDeleteUsersAsync(List<Guid> userIds);
    
    /// <summary>
    /// 恢复已删除用户
    /// </summary>
    Task<ServiceResult<UserDto>> RestoreUserAsync(Guid id);
    
    #endregion
    
    #region 用户状态管理业务
    
    /// <summary>
    /// 启用用户账户
    /// </summary>
    Task<ServiceResult<bool>> EnableUserAsync(Guid userId);
    
    /// <summary>
    /// 禁用用户账户
    /// </summary>
    Task<ServiceResult<bool>> DisableUserAsync(Guid userId);
    
    /// <summary>
    /// 切换用户状态
    /// </summary>
    Task<ServiceResult<bool>> ToggleUserStatusAsync(Guid userId);
    
    /// <summary>
    /// 批量更新用户状态
    /// </summary>
    Task<ServiceResult<BatchOperationResult>> BatchUpdateUserStatusAsync(List<Guid> userIds, bool isEnabled);
    
    /// <summary>
    /// 锁定用户账户
    /// </summary>
    Task<ServiceResult<bool>> LockUserAccountAsync(Guid userId, string reason);
    
    /// <summary>
    /// 解锁用户账户
    /// </summary>
    Task<ServiceResult<bool>> UnlockUserAccountAsync(Guid userId);
    
    #endregion
    
    #region 角色和权限管理
    
    /// <summary>
    /// 分配用户角色
    /// </summary>
    Task<ServiceResult<bool>> AssignUserRoleAsync(Guid userId, UserRole role);
    
    /// <summary>
    /// 变更用户角色
    /// </summary>
    Task<ServiceResult<bool>> ChangeUserRoleAsync(Guid userId, UserRole newRole);
    
    /// <summary>
    /// 批量角色分配
    /// </summary>
    Task<ServiceResult<BatchOperationResult>> BatchAssignRoleAsync(List<Guid> userIds, UserRole role);
    
    /// <summary>
    /// 验证用户权限
    /// </summary>
    ServiceResult<bool> ValidateUserPermission(Guid userId, string permission);
    
    /// <summary>
    /// 获取用户可用权限列表
    /// </summary>
    Task<ServiceResult<List<string>>> GetUserPermissionsAsync(Guid userId);
    
    #endregion
    
    #region 密码管理业务
    
    /// <summary>
    /// 重置用户密码
    /// </summary>
    Task<ServiceResult<bool>> ResetUserPasswordAsync(Guid userId);
    
    /// <summary>
    /// 修改用户密码
    /// </summary>
    Task<ServiceResult<bool>> ChangeUserPasswordAsync(Guid userId, UserPasswordChangeDto passwordChange);
    
    /// <summary>
    /// 批量重置密码
    /// </summary>
    Task<ServiceResult<BatchOperationResult>> BatchResetPasswordAsync(List<Guid> userIds);
    
    /// <summary>
    /// 强制用户下次登录修改密码
    /// </summary>
    Task<ServiceResult<bool>> ForcePasswordChangeAsync(Guid userId);
    
    /// <summary>
    /// 验证密码强度
    /// </summary>
    ServiceResult<PasswordStrengthDto> ValidatePasswordStrength(string password);
    
    #endregion
    
    #region 用户注册和激活
    
    /// <summary>
    /// 处理用户注册流程
    /// </summary>
    Task<ServiceResult<UserDto>> ProcessUserRegistrationAsync(UserRegistrationDto registrationDto);
    
    /// <summary>
    /// 激活用户账户
    /// </summary>
    Task<ServiceResult<bool>> ActivateUserAccountAsync(Guid userId, string activationCode);
    
    /// <summary>
    /// 重新发送激活码
    /// </summary>
    Task<ServiceResult<bool>> ResendActivationCodeAsync(Guid userId);
    
    /// <summary>
    /// 验证激活码
    /// </summary>
    ServiceResult<bool> ValidateActivationCode(string activationCode);
    
    #endregion
    
    #region 用户会话管理
    
    /// <summary>
    /// 记录用户登录
    /// </summary>
    Task<ServiceResult> RecordUserLoginAsync(Guid userId, UserLoginInfoDto loginInfo);
    
    /// <summary>
    /// 记录用户登出
    /// </summary>
    Task<ServiceResult> RecordUserLogoutAsync(Guid userId);
    
    /// <summary>
    /// 强制用户离线
    /// </summary>
    Task<ServiceResult<bool>> ForceUserOfflineAsync(Guid userId);
    
    /// <summary>
    /// 清除用户会话
    /// </summary>
    Task<ServiceResult> ClearUserSessionAsync(Guid userId);
    
    #endregion
    
    #region 数据导入导出
    
    /// <summary>
    /// 导入用户数据
    /// </summary>
    Task<ServiceResult<UserImportResultDto>> ImportUsersAsync(UserImportDto importDto);
    
    /// <summary>
    /// 导出用户数据
    /// </summary>
    Task<ServiceResult<UserExportResultDto>> ExportUsersAsync(UserExportQueryDto exportQuery);
    
    /// <summary>
    /// 验证导入数据
    /// </summary>
    ServiceResult<UserImportValidationDto> ValidateImportData(List<UserImportRecordDto> records);
    
    #endregion
    
    #region 业务规则和验证
    
    /// <summary>
    /// 应用业务规则验证
    /// </summary>
    ServiceResult ApplyBusinessRules(UserBusinessRuleDto rules);
    
    /// <summary>
    /// 验证用户业务约束
    /// </summary>
    Task<ServiceResult<bool>> ValidateUserConstraintsAsync(Guid userId);
    
    /// <summary>
    /// 检查用户名重复性
    /// </summary>
    Task<ServiceResult<bool>> CheckUsernameAvailabilityAsync(string username, Guid? excludeUserId = null);
    
    /// <summary>
    /// 检查邮箱重复性
    /// </summary>
    Task<ServiceResult<bool>> CheckEmailAvailabilityAsync(string email, Guid? excludeUserId = null);
    
    #endregion
    
    #region 用户偏好和配置
    
    /// <summary>
    /// 更新用户偏好设置
    /// </summary>
    Task<ServiceResult> UpdateUserPreferencesAsync(Guid userId, UserPreferencesDto preferences);
    
    /// <summary>
    /// 重置用户配置
    /// </summary>
    Task<ServiceResult> ResetUserConfigurationAsync(Guid userId);
    
    /// <summary>
    /// 同步用户配置
    /// </summary>
    Task<ServiceResult> SynchronizeUserConfigurationAsync(Guid userId);
    
    #endregion
    
    #region 审计和监控
    
    /// <summary>
    /// 记录用户操作审计
    /// </summary>
    Task<ServiceResult> RecordUserAuditAsync(UserAuditDto auditInfo);
    
    /// <summary>
    /// 检测异常用户行为
    /// </summary>
    Task<ServiceResult<UserBehaviorAnalysisDto>> AnalyzeUserBehaviorAsync(Guid userId);
    
    /// <summary>
    /// 生成用户活动报告
    /// </summary>
    Task<ServiceResult<UserActivityReportDto>> GenerateUserActivityReportAsync(Guid userId, DateTime from, DateTime to);
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 用户状态变更事件
    /// </summary>
    event EventHandler<UserStatusChangedEventArgs>? UserStatusChanged;
    
    /// <summary>
    /// 用户角色变更事件
    /// </summary>
    event EventHandler<UserRoleChangedEventArgs>? UserRoleChanged;
    
    /// <summary>
    /// 用户操作事件
    /// </summary>
    event EventHandler<UserOperationEventArgs>? UserOperation;
    
    #endregion
}

/// <summary>
/// 批量操作结果
/// </summary>
public class BatchOperationResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchOperationError> Errors { get; set; } = new();
}

/// <summary>
/// 批量操作错误
/// </summary>
public class BatchOperationError
{
    public Guid UserId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}

/// <summary>
/// 用户密码修改DTO
/// </summary>
public class UserPasswordChangeDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool ForceChange { get; set; } = false;
}

/// <summary>
/// 密码强度DTO
/// </summary>
public class PasswordStrengthDto
{
    public int Score { get; set; }
    public string Level { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public bool IsValid { get; set; }
}

/// <summary>
/// 用户注册DTO
/// </summary>
public class UserRegistrationDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public bool RequireActivation { get; set; } = true;
}

/// <summary>
/// 用户登录信息DTO
/// </summary>
public class UserLoginInfoDto
{
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public string LoginSource { get; set; } = string.Empty;
}

/// <summary>
/// 用户导入结果DTO
/// </summary>
public class UserImportResultDto
{
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<UserDto> ImportedUsers { get; set; } = new();
}

/// <summary>
/// 用户导出结果DTO
/// </summary>
public class UserExportResultDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}

/// <summary>
/// 用户导入DTO
/// </summary>
public class UserImportDto
{
    public List<UserImportRecordDto> Records { get; set; } = new();
    public bool SkipDuplicates { get; set; } = true;
    public bool ValidateData { get; set; } = true;
}

/// <summary>
/// 用户导入记录DTO
/// </summary>
public class UserImportRecordDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// 用户导入验证DTO
/// </summary>
public class UserImportValidationDto
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<UserImportRecordDto> ValidRecords { get; set; } = new();
    public List<UserImportRecordDto> InvalidRecords { get; set; } = new();
}

/// <summary>
/// 用户业务规则DTO
/// </summary>
public class UserBusinessRuleDto
{
    public bool RequireEmailVerification { get; set; }
    public bool RequirePhoneVerification { get; set; }
    public int PasswordExpirationDays { get; set; }
    public int MaxLoginAttempts { get; set; }
    public bool AllowDuplicateEmails { get; set; }
}

/// <summary>
/// 用户偏好DTO
/// </summary>
public class UserPreferencesDto
{
    public string Language { get; set; } = "zh-CN";
    public string Theme { get; set; } = "Light";
    public string TimeZone { get; set; } = "Asia/Shanghai";
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// 用户审计DTO
/// </summary>
public class UserAuditDto
{
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// 用户行为分析DTO
/// </summary>
public class UserBehaviorAnalysisDto
{
    public Guid UserId { get; set; }
    public int LoginFrequency { get; set; }
    public DateTime LastActiveTime { get; set; }
    public List<string> SuspiciousActivities { get; set; } = new();
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}

/// <summary>
/// 用户活动报告DTO
/// </summary>
public class UserActivityReportDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime ReportStartDate { get; set; }
    public DateTime ReportEndDate { get; set; }
    public int LoginCount { get; set; }
    public TimeSpan TotalActiveTime { get; set; }
    public List<string> PerformedActions { get; set; } = new();
    public Dictionary<string, int> ActivityStatistics { get; set; } = new();
}

/// <summary>
/// 用户状态变更事件参数
/// </summary>
public class UserStatusChangedEventArgs : EventArgs
{
    public Guid UserId { get; set; }
    public bool IsEnabled { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// 用户角色变更事件参数
/// </summary>
public class UserRoleChangedEventArgs : EventArgs
{
    public Guid UserId { get; set; }
    public UserRole OldRole { get; set; }
    public UserRole NewRole { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// 用户操作事件参数
/// </summary>
public class UserOperationEventArgs : EventArgs
{
    public Guid UserId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}