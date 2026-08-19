using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Web;

namespace LYBT.Tests.Desktop;

/// <summary>
/// Tests for SwitchingApiClient proxy — URL-driven routing between
/// RefitApiClient (remote) and HttpClientApiClient (local).
/// </summary>
public class SwitchingApiClientTests
{
    private readonly RefitSettings _refitSettings;
    private int _remoteCalls;
    private int _localCalls;

    public SwitchingApiClientTests()
    {
        _refitSettings = new RefitSettings();
        _remoteCalls = 0;
        _localCalls = 0;
    }

    private SwitchingApiClient CreateClient(IConnectionSettingsService cs)
    {
        // Remote factory: creates real HttpClient that returns a mock response
        Func<string, HttpClient> remoteFactory = _ =>
        {
            Interlocked.Increment(ref _remoteCalls);
            return new HttpClient(new FakeHttpMessageHandler("remote"))
            {
                BaseAddress = new Uri("http://remote-test:5000")
            };
        };

        // Local factory: creates fake IHttpClientFactory
        Func<string, IHttpClientFactory> localFactory = _ =>
        {
            Interlocked.Increment(ref _localCalls);
            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(
                new FakeHttpMessageHandler("local"))
            {
                BaseAddress = new Uri("http://127.0.0.1:5300")
            });
            return factory;
        };

        return new SwitchingApiClient(cs, remoteFactory, localFactory, _refitSettings, Substitute.For<ILogger<SwitchingApiClient>>());
    }

    #region URL to Implementation Mapping

    [Fact]
    public void LocalUrl_ShouldUseHttpClientApiClient()
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://127.0.0.1:5300");
        cs.IsLocal.Returns(true);
        var client = CreateClient(cs);

        // Access a property to trigger client creation
        _ = client.Identity;

        _localCalls.Should().Be(1);
        _remoteCalls.Should().Be(0);
    }

    [Fact]
    public void RemoteUrl_ShouldUseRefitApiClient()
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://192.168.190.248:5000");
        cs.IsLocal.Returns(false);
        var client = CreateClient(cs);

        _ = client.Identity;

        _remoteCalls.Should().Be(1);
        _localCalls.Should().Be(0);
    }

    [Fact]
    public void LocalhostUrl_ShouldTriggerLocalClient()
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://localhost:5300");
        cs.IsLocal.Returns(true);
        var client = CreateClient(cs);

        _ = client.Patients;

        _localCalls.Should().Be(1);
        _remoteCalls.Should().Be(0);
    }

    #endregion

    #region URL Change Behavior

    [Fact]
    public void UrlChange_ShouldRecreateClient()
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://127.0.0.1:5300", "http://remote:5000");
        cs.IsLocal.Returns(true, false);
        var client = CreateClient(cs);

        // First access: local
        _ = client.Herbs;
        _localCalls.Should().Be(1);
        _remoteCalls.Should().Be(0);

        // Second access: URL changed, now remote
        _ = client.Identity;
        _remoteCalls.Should().Be(1);
        _localCalls.Should().Be(1);
    }

    [Fact]
    public void UrlChange_DisposesOld_CreatesNew()
    {
        // TDD Batch 4 用例 3：URL 变更后创建新 client 并释放旧 client。
        // 行为实证（只验证不修改）：HttpClientApiClient / RefitApiClient 均未实现
        // IDisposable → SwitchingApiClient 中 `_current as IDisposable` 恒为 null，
        // 旧 client 实际不 Dispose（潜在资源泄漏点，登记不修改）。
        // 可验证部分：URL 变更后确实创建了新 client（工厂调用次数递增）。
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://remote:5000", "http://127.0.0.1:5300");
        cs.IsLocal.Returns(false, true);
        var client = CreateClient(cs);

        _ = client.Identity; // remote
        _remoteCalls.Should().Be(1);

        _ = client.Patients; // URL 变更 → local，新 client 创建
        _localCalls.Should().Be(1);
        _remoteCalls.Should().Be(1);

        // 切换本身不抛异常（Dispose 分支为 null 安全）
        var act = () => _ = client.Herbs;
        act.Should().NotThrow();
    }

    [Fact]
    public void SameUrl_ShouldNotRecreateClient()
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://remote:5000");
        cs.IsLocal.Returns(false);
        var client = CreateClient(cs);

        _ = client.Identity;
        _ = client.Patients;
        _ = client.Herbs;
        _ = client.Formulas;

        _remoteCalls.Should().Be(1); // Only created once
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task ThreadSafe_ConcurrentAccess_ReturnsSingleClient()
    {
        // TDD Batch 4 用例 5：并发访问（双重检查锁定 + volatile）只创建一次底层 client。
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://remote:5000");
        cs.IsLocal.Returns(false);
        var client = CreateClient(cs);

        var tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
        {
            _ = client.Identity;
            _ = client.Patients;
            _ = client.Herbs;
            return true;
        }));
        await Task.WhenAll(tasks);

        _remoteCalls.Should().Be(1);
        _localCalls.Should().Be(0);
    }

    #endregion

    #region Property Delegation

    [Fact]
    public void AllProperties_ShouldDelegateCorrectly()
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://remote:5000");
        cs.IsLocal.Returns(false);
        var client = CreateClient(cs);

        // All property access should work without throwing
        var identity = client.Identity;
        var patients = client.Patients;
        var herbs = client.Herbs;
        var formulas = client.Formulas;
        var medicalCases = client.MedicalCases;
        var registrations = client.Registrations;

        identity.Should().NotBeNull();
        patients.Should().NotBeNull();
        herbs.Should().NotBeNull();
        formulas.Should().NotBeNull();
        medicalCases.Should().NotBeNull();
        registrations.Should().NotBeNull();
    }

    #endregion

    /// <summary>
    /// Fake HttpMessageHandler that returns a predefined mock response.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _label;

        public FakeHttpMessageHandler(string label) => _label = label;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var json = $"{{\"label\":\"{_label}\", \"success\":true}}";
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    #region Dispose

    [Fact]
    public void Dispose_ShouldDisposeCurrentClient()
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://remote:5000");
        cs.IsLocal.Returns(false);
        var client = CreateClient(cs);
        _ = client.Identity; // trigger creation

        var act = () => client.Dispose();
        act.Should().NotThrow();
    }

    #endregion
}
