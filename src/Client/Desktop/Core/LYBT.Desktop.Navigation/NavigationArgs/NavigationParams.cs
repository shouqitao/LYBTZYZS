using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Navigation.NavigationArgs;

/// <summary>用户管理导航参数</summary>
public record UserManagementNavParams(UserRole? DefaultRoleFilter = null);

/// <summary>挂号导航参数</summary>
public record RegistrationNavParams(Guid? DoctorId = null);
