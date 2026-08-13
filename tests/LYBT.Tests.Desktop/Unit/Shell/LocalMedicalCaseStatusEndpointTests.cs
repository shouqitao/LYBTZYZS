using FluentAssertions;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LYBT.Tests.Desktop.Unit.Shell;

/// <summary>
/// Local MedicalCasesController 状态端点守卫（P1-10/11 2026-08-14 双端同步）
/// </summary>
public class LocalMedicalCaseStatusEndpointTests
{
    [Fact]
    public void Local_CloseEndpoint_RequiresAdminOrSuperAdmin()
    {
        var m = typeof(LYBT.LocalWebAPI.Controllers.MedicalCasesController).GetMethod("CloseCase");
        m.Should().NotBeNull("Local 应有 close 端点");
        m!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Should().Contain(a => a.Policy == PolicyConstants.AdminOrSuperAdmin,
                "Local close 必须方法级 [Authorize(AdminOrSuperAdmin)]——强制关闭仅限 Admin（US-MC-012）");
    }

    [Fact]
    public void StatusEndpoint_StillExists()
    {
        typeof(LYBT.LocalWebAPI.Controllers.MedicalCasesController)
            .GetMethod("UpdateStatus")
            .Should().NotBeNull("UpdateStatus 应存在（P1-10 仅移分支路由，端点保留）");
    }
}
