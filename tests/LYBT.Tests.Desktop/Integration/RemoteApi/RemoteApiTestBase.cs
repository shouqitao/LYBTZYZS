using System.Net.Http;
using System.Net.Http.Headers;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.Logging;
using Refit;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.RemoteApi;

/// <summary>
/// 远程 API E2E 基类 —— 真实 Refit 调用 http://60.190.215.86:5000
/// 每个测试类一个角色，Collection 串行避免并发冲突
/// </summary>
[CollectionDefinition("RemoteApi", DisableParallelization = true)]
public class RemoteApiCollection
{
}

public abstract class RemoteApiTestBase : DesktopTestBase
{
    protected const string RemoteBaseUrl = "http://60.190.215.86:5000";

    protected HttpClient HttpClient { get; private set; } = null!;
    internal IAuthApi AuthApi { get; private set; } = null!;
    internal IUserApi UserApi { get; private set; } = null!;
    internal IPatientApi PatientApi { get; private set; } = null!;
    internal IHerbApi HerbApi { get; private set; } = null!;
    internal IFormulaApi FormulaApi { get; private set; } = null!;
    internal IMedicalCaseApi MedicalCaseApi { get; private set; } = null!;
    internal IRegistrationApi RegistrationApi { get; private set; } = null!;

    protected string AccessToken { get; private set; } = string.Empty;
    protected string RefreshToken { get; private set; } = string.Empty;

    protected abstract string Username { get; }
    protected abstract string Password { get; }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(RemoteBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        var settings = CreateRefitSettings();
        AuthApi = RestService.For<IAuthApi>(HttpClient, settings);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var loginResp = await AuthApi.LoginAsync(new LoginRequest
        {
            UserName = Username,
            Password = Password
        });

        if (loginResp.Success && loginResp.Data != null)
        {
            AccessToken = loginResp.Data.Token;
            RefreshToken = loginResp.Data.RefreshToken ?? string.Empty;
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            // 重新创建需要认证的客户端，使其携带 Token
            UserApi = RestService.For<IUserApi>(HttpClient, settings);
            PatientApi = RestService.For<IPatientApi>(HttpClient, settings);
            HerbApi = RestService.For<IHerbApi>(HttpClient, settings);
            FormulaApi = RestService.For<IFormulaApi>(HttpClient, settings);
            MedicalCaseApi = RestService.For<IMedicalCaseApi>(HttpClient, settings);
            RegistrationApi = RestService.For<IRegistrationApi>(HttpClient, settings);
        }
        else
        {
            // 未登录成功时仍创建客户端，后续测试会失败并提示
            UserApi = RestService.For<IUserApi>(HttpClient, settings);
            PatientApi = RestService.For<IPatientApi>(HttpClient, settings);
            HerbApi = RestService.For<IHerbApi>(HttpClient, settings);
            FormulaApi = RestService.For<IFormulaApi>(HttpClient, settings);
            MedicalCaseApi = RestService.For<IMedicalCaseApi>(HttpClient, settings);
            RegistrationApi = RestService.For<IRegistrationApi>(HttpClient, settings);
        }
    }

    public override async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await base.DisposeAsync();
    }

    private static RefitSettings CreateRefitSettings()
    {
        return new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })
        };
    }
}
