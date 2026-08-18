using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using System.Reflection;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// TDD Batch 7 L1 — TokenLifecycleService（Token 生命周期状态机，今天修改）单元测试。
/// 覆盖状态转换：有效→Active、临近过期→Warning、已过期→Expired。
/// Warning/Expired 由私有 CheckTokenState 驱动（定时器 30s 不可测），
/// 用反射调用私有方法验证转换逻辑（行为只读，不修改被测代码）。
/// </summary>
public class TokenLifecycleServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();
    private readonly ITokenStorageService _tokenStorage = Substitute.For<ITokenStorageService>();
    private readonly IEventAggregator _eventAggregator = Substitute.For<IEventAggregator>();
    private readonly ILogger<TokenLifecycleService> _logger = Substitute.For<ILogger<TokenLifecycleService>>();

    private TokenLifecycleService CreateSut()
    {
        var stateChangedEvent = Substitute.For<TokenLifecycleStateChangedEvent>();
        _eventAggregator.GetEvent<TokenLifecycleStateChangedEvent>().Returns(stateChangedEvent);
        var identity = Substitute.For<IApiClientIdentity>();
        _apiClient.Identity.Returns(identity);
        return new TokenLifecycleService(_apiClient, _tokenStorage, _eventAggregator, _logger);
    }

    private static void InvokeCheckTokenState(TokenLifecycleService svc)
    {
        var method = typeof(TokenLifecycleService).GetMethod(
            "CheckTokenState", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(svc, null);
    }

    [Fact]
    public void CheckTokenValidity_Valid_ReturnsActive()
    {
        using var svc = CreateSut();

        svc.StartMonitoring(DateTime.UtcNow.AddMinutes(30));

        svc.CurrentState.Should().Be(TokenLifecycleState.Active);
        svc.RemainingTime.Should().BeGreaterThan(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public async Task CheckTokenValidity_Expiring_ReturnsWarning()
    {
        using var svc = CreateSut();
        // 无 RefreshToken → 自动刷新失败 → 保持 Warning（不翻转回 Active）
        _tokenStorage.GetRefreshTokenAsync().Returns(Task.FromResult<string?>(null));

        svc.StartMonitoring(DateTime.UtcNow.AddMinutes(1)); // < 5min 阈值
        InvokeCheckTokenState(svc);

        svc.CurrentState.Should().Be(TokenLifecycleState.Warning);

        await Task.Delay(60); // 等自动刷新 Task.Run 落定
        svc.CurrentState.Should().Be(TokenLifecycleState.Warning);
    }

    [Fact]
    public void CheckTokenValidity_Expired_ReturnsExpired()
    {
        using var svc = CreateSut();

        svc.StartMonitoring(DateTime.UtcNow.AddMinutes(-1)); // 已过期
        InvokeCheckTokenState(svc);

        svc.CurrentState.Should().Be(TokenLifecycleState.Expired);
        svc.RemainingTime.Should().Be(TimeSpan.Zero);
    }
}
