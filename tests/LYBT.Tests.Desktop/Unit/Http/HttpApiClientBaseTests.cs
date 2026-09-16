using FluentAssertions;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Foundation.Http;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// TDD Batch 1 — HttpApiClientBase（LocalWebAPI HTTP 基类）单元测试。
/// 覆盖 SendAsync（GET/POST/PUT/DELETE + 错误路径）、DeserializeEnvelopeAsync
/// （信封解包/裸 T 回退/空 JSON）、BuildPagedUrl（分页/过滤/空值跳过）。
/// 全部经 MockHttpMessageHandler 模拟 HTTP 响应，不依赖真实 WebAPI。
/// </summary>
public class HttpApiClientBaseTests
{
    private sealed class SampleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>记录最近一次请求并返回固定响应（不访问真实网络）。</summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public MockHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            // 对齐真实 HttpMessageHandler 行为：响应回填 RequestMessage
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
            response.RequestMessage = request;
            return response;
        }
    }

    private static (IHttpClientFactory Factory, MockHttpMessageHandler Handler) CreateMockFactory(
        string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(responseBody, statusCode);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5300") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);
        return (factory, handler);
    }

    /// <summary>暴露 HttpApiClientBase 的 protected 成员供测试。</summary>
    private sealed class TestableHttpApiClient : HttpApiClientBase
    {
        public TestableHttpApiClient(IHttpClientFactory factory, ILogger logger) : base(factory, logger) { }

        public new Task<HttpResponseMessage> SendAsync(string url, HttpMethod method, object? body = null, CancellationToken ct = default)
            => base.SendAsync(url, method, body, ct);

        public new Task<ApiResponse<T>> DeserializeEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken ct = default, ILogger? logger = null)
            => HttpApiClientBase.DeserializeEnvelopeAsync<T>(response, ct, logger);

        public new static string BuildPagedUrl(string baseUrl, int page, int pageSize, params (string Key, string? Value)[] filters)
            => HttpApiClientBase.BuildPagedUrl(baseUrl, page, pageSize, filters);
    }

    #region SendAsync — Happy path

    [Fact]
    public async Task SendAsync_Get_ReturnsSuccess()
    {
        var (factory, _) = CreateMockFactory("{}");
        var client = new TestableHttpApiClient(factory, Substitute.For<ILogger>());

        var response = await client.SendAsync("/api/v1/patients", HttpMethod.Get);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.RequestMessage!.Method.Should().Be(HttpMethod.Get);
        response.RequestMessage.RequestUri.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_Post_WithBody_SendsJson()
    {
        var (factory, handler) = CreateMockFactory("{}");
        var client = new TestableHttpApiClient(factory, Substitute.For<ILogger>());

        await client.SendAsync("/api/v1/patients", HttpMethod.Post, new SampleDto { Id = 7, Name = "ZhangSan" });

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        // 注意：System.Text.Json 默认转义非 ASCII（JavaScriptEncoder.Default）→ 中文名序列化为 \uXXXX
        handler.LastRequestBody.Should().Be("{\"id\":7,\"name\":\"ZhangSan\"}");
    }

    [Fact]
    public async Task SendAsync_Put_WorksCorrectly()
    {
        var (factory, handler) = CreateMockFactory("{}");
        var client = new TestableHttpApiClient(factory, Substitute.For<ILogger>());

        var response = await client.SendAsync("/api/v1/patients/1", HttpMethod.Put, new SampleDto { Id = 1, Name = "Updated" });

        response.IsSuccessStatusCode.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequestBody.Should().Be("{\"id\":1,\"name\":\"Updated\"}");
    }

    [Fact]
    public async Task SendAsync_Delete_WorksCorrectly()
    {
        var (factory, handler) = CreateMockFactory("{}");
        var client = new TestableHttpApiClient(factory, Substitute.For<ILogger>());

        var response = await client.SendAsync("/api/v1/patients/1", HttpMethod.Delete);

        response.IsSuccessStatusCode.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
    }

    #endregion

    #region SendAsync — Error paths

    [Fact]
    public async Task SendAsync_NonSuccessStatus_ThrowsApiClientException()
    {
        var (factory, _) = CreateMockFactory("boom", HttpStatusCode.InternalServerError);
        var client = new TestableHttpApiClient(factory, Substitute.For<ILogger>());

        var act = () => client.SendAsync("/api/v1/patients", HttpMethod.Get);

        // 领域错误层统一：非 2xx 抛 ApiClientException（继承 HttpRequestException，向后兼容既有 catch）
        var ex = await act.Should().ThrowAsync<ApiClientException>();
        ex.And.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex.And.Message.Should().Be("boom");
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatus_ExtractsEnvelopeMessageAndErrorCode()
    {
        const string envelope =
            "{\"success\":false,\"message\":\"验方名称已存在\",\"data\":null," +
            "\"errors\":{\"code\":\"LYBT-FORM-002\",\"correlationId\":\"abc\"},\"requestId\":\"abc\"}";
        var (factory, _) = CreateMockFactory(envelope, HttpStatusCode.UnprocessableEntity);
        var client = new TestableHttpApiClient(factory, Substitute.For<ILogger>());

        var act = () => client.SendAsync("/api/v1/formulas", HttpMethod.Get);

        var ex = await act.Should().ThrowAsync<ApiClientException>();
        ex.And.ServerMessage.Should().Be("验方名称已存在");
        ex.And.ErrorCode.Should().Be("LYBT-FORM-002");
    }

    /// <summary>X-3：Server 异常路径统一 ProblemDetails（RFC 7807），Desktop 需能解析 detail + 根级 errorCode。</summary>
    [Fact]
    public async Task SendAsync_NonSuccessStatus_ExtractsProblemDetailsMessageAndErrorCode()
    {
        const string problem =
            "{\"type\":\"https://tools.ietf.org/html/rfc4918#section-11.2\"," +
            "\"title\":\"Business Error\",\"status\":422," +
            "\"detail\":\"验方名称已存在\",\"instance\":\"/api/v1/formulas\"," +
            "\"errorCode\":\"ERR-60301\",\"traceId\":\"abc\"}";
        var (factory, _) = CreateMockFactory(problem, HttpStatusCode.UnprocessableEntity);
        var client = new TestableHttpApiClient(factory, Substitute.For<ILogger>());

        var act = () => client.SendAsync("/api/v1/formulas", HttpMethod.Get);

        var ex = await act.Should().ThrowAsync<ApiClientException>();
        ex.And.ServerMessage.Should().Be("验方名称已存在");
        ex.And.ErrorCode.Should().Be("ERR-60301");
    }

    [Fact]
    public async Task SendAsync_UnsupportedMethod_ThrowsArgumentException()
    {
        var (factory, _) = CreateMockFactory("{}");
        var client = new TestableHttpApiClient(factory, Substitute.For<ILogger>());

        var act = () => client.SendAsync("/api/v1/patients", HttpMethod.Options);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unsupported HTTP method: OPTIONS*");
    }

    #endregion

    #region DeserializeEnvelopeAsync

    [Fact]
    public async Task DeserializeEnvelope_EnvelopeFormat_UnwrapsData()
    {
        var client = new TestableHttpApiClient(CreateMockFactory("{}").Factory, Substitute.For<ILogger>());
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"Success\":true,\"Data\":{\"Id\":1,\"Name\":\"Test\"}}",
                Encoding.UTF8, "application/json")
        };

        var envelope = await client.DeserializeEnvelopeAsync<SampleDto>(response);

        envelope.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(1);
        envelope.Data.Name.Should().Be("Test");
    }

    [Fact]
    public async Task DeserializeEnvelope_RawFormat_WrapsInEnvelope()
    {
        // 裸 T 回退仅在信封反序列化抛 JsonException 时触发——JSON 对象形态会被
        // ApiResponse<T> 无感吞掉（未知属性跳过 → 空信封 Success=false/Data=null，
        // 属既有行为，本任务只验证不修改）。原始值形态（字符串/数字根）走回退分支。
        var client = new TestableHttpApiClient(CreateMockFactory("{}").Factory, Substitute.For<ILogger>());
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"raw-value\"", Encoding.UTF8, "application/json")
        };

        var envelope = await client.DeserializeEnvelopeAsync<string>(response);

        envelope.Success.Should().BeTrue();
        envelope.Data.Should().Be("raw-value");
    }

    [Fact]
    public async Task DeserializeEnvelope_EmptyJson_ReturnsEmptyEnvelope()
    {
        // "空 JSON" = {}（空对象）→ 默认空信封（Success=false, Data=null）。
        // 注：纯空串 "" 会在裸反序列化处抛 JsonException（既有行为，未修改）。
        var client = new TestableHttpApiClient(CreateMockFactory("{}").Factory, Substitute.For<ILogger>());
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var envelope = await client.DeserializeEnvelopeAsync<SampleDto>(response);

        envelope.Success.Should().BeFalse();
        envelope.Data.Should().BeNull();
    }

    #endregion

    #region BuildPagedUrl

    [Fact]
    public void BuildPagedUrl_BasicPagination()
    {
        var url = TestableHttpApiClient.BuildPagedUrl("http://localhost:5300/api/v1/patients", 1, 20);

        url.Should().Be("http://localhost:5300/api/v1/patients?page=1&pageSize=20");
    }

    [Fact]
    public void BuildPagedUrl_WithFilters()
    {
        var url = TestableHttpApiClient.BuildPagedUrl(
            "http://localhost:5300/api/v1/patients", 2, 10,
            ("Keyword", "张"), ("Status", "Enabled"));

        url.Should().Be("http://localhost:5300/api/v1/patients?page=2&pageSize=10&Keyword=%E5%BC%A0&Status=Enabled");
    }

    [Fact]
    public void BuildPagedUrl_NullFilters_Skipped()
    {
        var url = TestableHttpApiClient.BuildPagedUrl(
            "http://localhost:5300/api/v1/patients", 1, 20,
            ("Keyword", null), ("Status", ""), ("Name", "x"));

        url.Should().Be("http://localhost:5300/api/v1/patients?page=1&pageSize=20&Name=x");
    }

    #endregion
}
