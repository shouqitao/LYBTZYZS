using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LYBT.Desktop.Foundation.Http;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LYBT.Tests.Desktop.Unit.Foundation;

/// <summary>
/// HttpClientApiClient 信封契约测试 — LocalWebAPI 返回 ApiResponse&lt;T&gt; 信封，
/// 客户端必须反序列化信封并取 Data，与 Refit（Remote 模式）行为一致。
/// </summary>
public class HttpClientApiClientEnvelopeTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly string _json;
        public FakeHandler(string json) => _json = json;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
    }

    private static HttpClientApiClient CreateClient(string json)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(string.Empty)
            .ConfigurePrimaryHttpMessageHandler(() => new FakeHandler(json))
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://127.0.0.1:5300"));
        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        return new HttpClientApiClient(factory);
    }

    private static string Envelope(string dataJson) =>
        $$"""{"success":true,"message":"查询成功","data":{{dataJson}},"timestamp":1,"requestId":"r1"}""";

    [Fact]
    public async Task GetAndWrapAsync_Unwraps_Envelope_Data()
    {
        var client = CreateClient(Envelope("""{"items":[{"userName":"admin"}],"totalCount":42,"currentPage":1,"pageSize":20}"""));

        var result = await client.Users.GetUsersAsync(1, 20, null);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(42, "信封 data.totalCount 必须解出");
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items[0].UserName.Should().Be("admin");
    }

    [Fact]
    public async Task GetAndWrapAsync_Detail_Unwraps_Envelope_Data()
    {
        var client = CreateClient(Envelope("""{"id":"00000000-0000-0000-0000-000000000001","userName":"admin"}"""));

        var result = await client.Users.GetUserByIdAsync(Guid.Parse("00000000-0000-0000-0000-000000000001"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserName.Should().Be("admin");
    }

    [Fact]
    public async Task PostAndWrapAsync_Unwraps_Envelope_Data()
    {
        var client = CreateClient(Envelope("""{"id":"00000000-0000-0000-0000-000000000002","userName":"newuser"}"""));

        var result = await client.Users.CreateUserAsync(new UserInputDto { UserName = "newuser" });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserName.Should().Be("newuser");
    }

    [Fact]
    public async Task Login_Unwraps_Envelope_With_Nested_User()
    {
        // LocalWebAPI Login 返回 { success, message, data: { token, user: { username }, expiresAt } }
        var dataJson = """{"token":"t1","expiresAt":"2026-12-31T00:00:00Z","user":{"userName":"sysadmin"}}""";
        var client = CreateClient(Envelope(dataJson));

        var result = await client.Auth.LoginAsync(new LoginRequest { UserName = "sysadmin", Password = "SysAdmin@2026!" });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("t1");
        result.Data.User.UserName.Should().Be("sysadmin");
    }
}
