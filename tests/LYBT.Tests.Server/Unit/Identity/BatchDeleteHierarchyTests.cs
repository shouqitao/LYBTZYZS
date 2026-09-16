using LYBT.Module.Identity.Application.Commands;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Server.Unit.Identity;

/// <summary>
/// P0-3 批量操作层级守卫回归 — 横向对比单体 Create/Delete 的一级管一级
/// </summary>
public class BatchDeleteHierarchyTests
{
    [Fact]
    public void Admin_BatchOperation_On_Admin_Should_Fail()
    {
        var adminId = Guid.NewGuid();
        var targetAdminId = Guid.NewGuid();
        var result = UserHierarchyGuard.Validate<object>(adminId, targetAdminId, UserRole.Admin, false, UserRole.Admin);
        Assert.NotNull(result);
        Assert.Equal("仅超级管理员可管理管理员账号", result!.Error);
    }

    [Fact]
    public void Doctor_BatchOperation_Should_Fail_Unauthorized()
    {
        var doctorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var result = UserHierarchyGuard.Validate<object>(doctorId, targetId, UserRole.Doctor, false, UserRole.Receptionist);
        Assert.NotNull(result);
        Assert.Equal("无权管理用户", result!.Error);
    }

    [Fact]
    public void Admin_BatchOperation_On_Doctor_Should_Pass()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var result = UserHierarchyGuard.Validate<object>(adminId, targetId, UserRole.Admin, false, UserRole.Doctor);
        Assert.Null(result);
    }

    [Fact]
    public void BatchOperation_On_SysAdmin_Should_Fail()
    {
        var adminId = Guid.NewGuid();
        var sysAdminTarget = Guid.NewGuid();
        var result = UserHierarchyGuard.Validate<object>(adminId, sysAdminTarget, UserRole.SuperAdmin, true, UserRole.SuperAdmin);
        Assert.NotNull(result);
        Assert.Contains("系统管理员", result!.Error);
    }

    [Fact]
    public void BatchEnable_Should_Use_HierarchyGuard_SelfCheck()
    {
        var adminId = Guid.NewGuid();
        var result = UserHierarchyGuard.Validate<object>(adminId, adminId, UserRole.Admin, false, UserRole.Admin);
        Assert.NotNull(result);
        Assert.Contains("不能操作自己", result!.Error);
    }
}
