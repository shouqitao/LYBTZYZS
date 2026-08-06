using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// AuthSession 实体测试 — 撤销行为（Token 族旋转）。
/// </summary>
public class AuthSessionTests
{
    private static AuthSession CreateSession(Guid userId)
        => AuthSession.Create(userId, "test-token-hash", DateTime.UtcNow.AddHours(2), "127.0.0.1", "TestAgent");

    [Fact]
    public void Revoke_WithReason_SetsRevokedStateAndReason()
    {
        var session = CreateSession(Guid.NewGuid());

        session.Revoke("用户已被禁用");

        session.IsRevoked.Should().BeTrue();
        session.RevokedReason.Should().Be("用户已被禁用");
        session.LogoutTime.Should().NotBeNull();
        session.Status.Should().Be(CommonStatus.Disabled);
        session.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithoutReason_LeavesRevokedReasonNull()
    {
        var session = CreateSession(Guid.NewGuid());

        session.Revoke();

        session.IsRevoked.Should().BeTrue();
        session.RevokedReason.Should().BeNull();
    }

    [Fact]
    public void Logout_DoesNotSetRevokedReason()
    {
        var session = CreateSession(Guid.NewGuid());

        session.Logout();

        session.IsRevoked.Should().BeFalse();
        session.RevokedReason.Should().BeNull();
        session.IsValid().Should().BeFalse();
    }
}
