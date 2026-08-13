using FluentAssertions;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// MedicalCasesController 状态端点守卫（P1-10/11 2026-08-14）：
/// - close 端点必须方法级 [Authorize(AdminOrSuperAdmin)]（强制关闭仅限 Admin——US-MC-012，
///   原 Controller 内 if(!isAdmin) 权限判断已移除）
/// - status 端点仍存在（Completed 分支路由已移入 StateService.UpdateStatus 统一处理）
/// </summary>
public class MedicalCaseStatusEndpointTests
{
    [Fact]
    public void Remote_CloseEndpoint_RequiresAdminOrSuperAdmin()
    {
        var m = typeof(LYBT.WebAPI.Controllers.MedicalCasesController).GetMethod("CloseMedicalCase");
        m.Should().NotBeNull("Remote 应有 close 端点");
        AssertCloseAuthorize(m!);
    }

    [Fact]
    public void StatusEndpoint_StillExists()
    {
        typeof(LYBT.WebAPI.Controllers.MedicalCasesController)
            .GetMethod("UpdateStatus")
            .Should().NotBeNull("UpdateStatus 应存在（P1-10 仅移分支路由，端点保留）");
    }

    private static void AssertCloseAuthorize(System.Reflection.MethodInfo m)
    {
        m.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Should().Contain(a => a.Policy == PolicyConstants.AdminOrSuperAdmin,
                "close 必须方法级 [Authorize(AdminOrSuperAdmin)]——强制关闭仅限 Admin（US-MC-012）");
    }
}
