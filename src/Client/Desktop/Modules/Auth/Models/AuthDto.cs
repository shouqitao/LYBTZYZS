using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 登录状态DTO
/// </summary>
public class LoginStatusDto
{
    public bool IsLoggedIn { get; set; }
    public string? Username { get; set; }
    public Guid UserId { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LastActivity { get; set; }
    public bool HasValidToken { get; set; }
}

/// <summary>
/// API连接状态DTO
/// </summary>
public class ApiConnectionStatusDto
{
    public bool IsOnline { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime LastCheckTime { get; set; }
    public TimeSpan? ResponseTime { get; set; }
}

/// <summary>
/// 连接延迟DTO
/// </summary>
public class ConnectionLatencyDto
{
    public TimeSpan Latency { get; set; }
    public DateTime Timestamp { get; set; }
    public string QualityLevel { get; set; } = string.Empty;
}

/// <summary>
/// 会话信息DTO
/// </summary>
public class SessionInfoDto
{
    public bool IsActive { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastActivity { get; set; }
    public int RemainingMinutes { get; set; }
    public DateTime ExpiryTime { get; set; }
}

/// <summary>
/// 保存的凭据信息DTO
/// </summary>
public class SavedCredentialInfoDto
{
    public string Username { get; set; } = string.Empty;
    public bool HasPassword { get; set; }
    public bool RememberMe { get; set; }
    public DateTime SavedTime { get; set; }
}

/// <summary>
/// 认证统计DTO
/// </summary>
public class AuthStatisticsDto
{
    public int TotalLoginAttempts { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedLogins { get; set; }
    public DateTime LastLoginTime { get; set; }
    public TimeSpan AverageSessionDuration { get; set; }
}

/// <summary>
/// 登录历史记录DTO
/// </summary>
public class RecentLoginHistoryDto
{
    public List<LoginHistoryItemDto> LoginHistory { get; set; } = new();
}

/// <summary>
/// 登录历史项DTO
/// </summary>
public class LoginHistoryItemDto
{
    public DateTime LoginTime { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 安全状态DTO
/// </summary>
public class SecurityStatusDto
{
    public string SecurityLevel { get; set; } = string.Empty;
    public string ThreatLevel { get; set; } = string.Empty;
    public DateTime LastSecurityCheck { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>
/// 认证风险等级DTO
/// </summary>
public class AuthRiskLevelDto
{
    public string RiskLevel { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public List<string> RiskFactors { get; set; } = new();
}

/// <summary>
/// 密码修改DTO
/// </summary>
public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// 密码重置DTO
/// </summary>
public class ResetPasswordDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ResetCode { get; set; }
    public string? NewPassword { get; set; }
}

/// <summary>
/// 密码强度DTO
/// </summary>
public class PasswordStrengthDto
{
    public string StrengthLevel { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 安全检查结果DTO
/// </summary>
public class SecurityCheckResultDto
{
    public bool IsSecure { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// 安全威胁DTO
/// </summary>
public class SecurityThreatDto
{
    public string ThreatType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedTime { get; set; }
}

/// <summary>
/// 登录体验优化DTO
/// </summary>
public class LoginExperienceDto
{
    public bool ShouldAutoFill { get; set; }
    public bool ShouldRememberChoice { get; set; }
    public List<string> OptimizationSuggestions { get; set; } = new();
}

/// <summary>
/// 离线模式DTO
/// </summary>
public class OfflineModeDto
{
    public bool IsOfflineMode { get; set; }
    public List<string> AvailableFeatures { get; set; } = new();
    public List<string> LimitedFeatures { get; set; } = new();
}

/// <summary>
/// 认证诊断DTO
/// </summary>
public class AuthDiagnosticsDto
{
    public bool HasIssues { get; set; }
    public List<string> DetectedIssues { get; set; } = new();
    public List<string> RepairSuggestions { get; set; } = new();
}

/// <summary>
/// 会话状态变更事件参数
/// </summary>
public class SessionStatusChangedEventArgs : EventArgs
{
    public bool IsActive { get; set; }
    public string? StatusMessage { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 安全事件参数
/// </summary>
public class SecurityEventArgs : EventArgs
{
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}