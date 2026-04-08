using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Tests.Desktop.EndToEnd.Infrastructure;

/// <summary>
/// 管理员职责测试基类
/// 
/// 职责范围：
/// - 用户管理（CRUD、角色分配、密码重置）
/// - 药材管理（CRUD、批量操作）
/// - 验方管理（CRUD、批量操作）
/// - 系统诊断（仅 SuperAdmin）
/// - 审计日志查看
/// </summary>
public abstract class AdminTestBase : WebApiE2ETestBase
{
    /// <summary>
    /// 以管理员身份登录
    /// </summary>
    protected new async Task<LoginResponse> LoginAsAdminAsync()
    {
        var username = Configuration["TestCredentials:Admin:Username"] ?? "admin";
        var password = Configuration["TestCredentials:Admin:Password"] ?? "AdminPass123!";
        
        return await LoginAsAsync(username, password);
    }

    /// <summary>
    /// 以超级管理员身份登录
    /// </summary>
    protected async Task<LoginResponse> LoginAsSuperAdminAsync()
    {
        return await LoginAsSysadminAsync();
    }

    /// <summary>
    /// 验证管理员权限：可以访问用户管理端点
    /// </summary>
    protected async Task<bool> VerifyAdminUserManagementAccess()
    {
        var response = await UserApi.GetUsersAsync();
        return response.Success;
    }

    /// <summary>
    /// 验证管理员权限：可以访问系统诊断端点（仅 SuperAdmin）
    /// </summary>
    protected async Task<bool> VerifySuperAdminDiagnosticsAccess()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/diagnostics/logging/status");
        return response.IsSuccessStatusCode;
    }
}
