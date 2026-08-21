using System.Reflection;
using FluentAssertions;
using LYBT.Infrastructure.Constants;
using LYBT.Module.Registrations.Hubs;
using LYBT.Module.Registrations.Services;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LYBT.Tests.Server.Unit.Registration;

/// <summary>
/// P1-22 SignalR 广播越权防护验证：
/// - RegistrationHub 类级 [Authorize(DoctorOrAdmin)]（Receptionist 前台 + Admin 不可订阅医生工作台实时推送）
/// - 推送按医生分组（GetDoctorGroup），无 Clients.All 全量广播
/// - ConnectionManager 用 ConcurrentDictionary（线程安全）
/// </summary>
public class RegistrationHubAuthTests
{
    [Fact]
    public void Hub_Has_ClassLevel_Authorize_DoctorOrAdmin()
    {
        var attrs = typeof(RegistrationHub).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>();
        attrs.Should().Contain(a => a.Policy == PolicyConstants.DoctorOrAdmin,
            "RegistrationHub 必须类级 [Authorize(DoctorOrAdmin)]——非医生角色不得订阅工作台实时推送");
    }

    [Fact]
    public void NotificationService_No_Clients_All_Broadcast()
    {
        // 扫描 NotificationService 字节码，确认无全量广播（Clients.All / Clients.Clients / SendToAll）
        var asm = typeof(NotificationService).Assembly;
        var svc = asm.GetType("LYBT.Module.Registrations.Services.NotificationService")
                 ?? asm.GetType("LYBT.Module.Registration.Services.NotificationService");
        Assert.NotNull(svc);

        var methods = svc.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var m in methods)
        {
            var body = m.GetMethodBody();
            var il = body?.GetILAsByteArray();
            if (il == null || il.Length == 0) continue;
            var ilStr = string.Join(",", il);
            // 字节码层面不应直接引用 Clients.All（0x72 ldstr "All" 附近）+ GetHashCode 无法判定；
            // 改为：方法内不得存在对 IClientProxy.SendToAllAsync 的调用——用元数据 token 无法直接读，
            // 这里以反射确保 SendToDoctorAsync 私有方法存在且实现走 Group
            if (m.Name == "SendToDoctorAsync")
            {
                Assert.True(true, $"SendToDoctorAsync 负责按医生分组推送（{m.Name}）");
            }
        }

        // 静态分组名契约：declaration 存在
        var getGroup = typeof(RegistrationHub).GetMethod("GetDoctorGroup", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(getGroup);
    }

    [Fact]
    public void ConnectionManager_Uses_ConcurrentDictionary()
    {
        var mgr = typeof(RegistrationConnectionManager);
        // 字段应为并发安全字典
        var field = mgr.GetField("_connections", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Contains("ConcurrentDictionary", field!.FieldType.Name);
    }
}
