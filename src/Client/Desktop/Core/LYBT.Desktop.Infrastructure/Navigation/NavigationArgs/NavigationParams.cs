using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Navigation.NavigationArgs;

/// <summary>用户管理导航参数</summary>
public record UserManagementNavParams(UserRole? DefaultRoleFilter = null);
