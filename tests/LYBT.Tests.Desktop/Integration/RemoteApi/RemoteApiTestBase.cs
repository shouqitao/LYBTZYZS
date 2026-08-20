using System.Net.Http;
using System.Net.Http.Headers;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Refit;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.RemoteApi;

/// <summary>
/// 远程 API E2E 基类（US 模式）—— 真实 Refit 调用 http://60.190.215.86:5000
/// 流程：sysadmin 登录 → 各测试类 SetupRoleAsync 创建角色账号并登录该角色
/// 每个测试类一个角色，Collection 串行避免并发冲突；测试数据唯一；每测 30s 超时
/// </summary>
[CollectionDefinition("RemoteApi", DisableParallelization = true)]
public class RemoteApiCollection
{
}

public abstract class RemoteApiTestBase : IAsyncLifetime
{
    protected const string BaseUrl = "http://60.190.215.86:5000";
    protected const string SysAdminUser = "sysadmin";
    protected const string SysAdminPass = "SysAdmin@2026!";
    protected const string RolePassword = "E2EPass123!";

    protected HttpClient HttpClient { get; private set; } = null!;
    internal IAuthApi AuthApi { get; private set; } = null!;
    internal IUserApi UserApi { get; private set; } = null!;
    internal IPatientApi PatientApi { get; private set; } = null!;
    internal IHerbApi HerbApi { get; private set; } = null!;
    internal IFormulaApi FormulaApi { get; private set; } = null!;
    internal IMedicalCaseApi MedicalCaseApi { get; private set; } = null!;
    internal IRegistrationApi RegistrationApi { get; private set; } = null!;

    protected string AccessToken { get; private set; } = string.Empty;
    protected string Username { get; private set; } = string.Empty;

    /// <summary>各角色测试类实现：创建专属角色账号并 <see cref="LoginAsAsync"/> 登录该角色</summary>
    protected abstract Task SetupRoleAsync();

    /// <summary>跟踪创建的用户 ID，DisposeAsync 用 sysadmin 权限清理</summary>
    protected readonly List<Guid> CreatedUserIds = new();
    protected readonly List<Guid> CreatedPatientIds = new();
    protected readonly List<Guid> CreatedMedicalCaseIds = new();
    protected readonly List<Guid> CreatedHerbIds = new();
    protected readonly List<Guid> CreatedRegistrationIds = new();

    protected static string UniquePhone() => $"138{Random.Shared.Next(10000000, 99999999)}";
    protected static string UniqueIdNumber() => $"110101{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
    protected static string UniqueUsername(string prefix) => $"{prefix}_{Guid.NewGuid():N}"[..Math.Min(16, prefix.Length + 1 + 12)];
    protected static string UniqueName(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..Math.Min(12, prefix.Length + 8)];

    public virtual async Task InitializeAsync()
    {
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        AuthApi = RestService.For<IAuthApi>(HttpClient, CreateRefitSettings());

        // 1. sysadmin 登录 → 2. 创建角色并切换登录
        await LoginAsAsync(SysAdminUser, SysAdminPass);
        await SetupRoleAsync();
    }

    public virtual async Task DisposeAsync()
    {
        // 清理测试数据（角色无删除权限——统一切 sysadmin 用系统权限清理，最佳努力）
        if (CreatedUserIds.Count + CreatedPatientIds.Count + CreatedMedicalCaseIds.Count
            + CreatedHerbIds.Count + CreatedRegistrationIds.Count > 0)
        {
            try { await LoginAsAsync(SysAdminUser, SysAdminPass); } catch { }
            foreach (var id in CreatedRegistrationIds) { try { await RegistrationApi.CancelAsync(id); } catch { } }
            foreach (var id in CreatedMedicalCaseIds) { try { await MedicalCaseApi.DeleteMedicalCaseAsync(id); } catch { } }
            foreach (var id in CreatedPatientIds) { try { await PatientApi.DeletePatientAsync(id); } catch { } }
            foreach (var id in CreatedHerbIds) { try { await HerbApi.DeleteHerbAsync(id); } catch { } }
            foreach (var id in CreatedUserIds) { try { await UserApi.DeleteUserAsync(id); } catch { } }
        }

        HttpClient?.Dispose();
    }

    /// <summary>登录指定用户并重建携带新 Token 的 Refit 客户端</summary>
    protected async Task LoginAsAsync(string username, string password)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await AuthApi.LoginAsync(new LoginRequest { UserName = username, Password = password });

        if (!resp.Success || resp.Data == null || string.IsNullOrWhiteSpace(resp.Data.Token))
        {
            throw new InvalidOperationException($"Login failed for {username}: {resp.Message}");
        }

        AccessToken = resp.Data.Token;
        Username = username;
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var settings = CreateRefitSettings();
        UserApi = RestService.For<IUserApi>(HttpClient, settings);
        PatientApi = RestService.For<IPatientApi>(HttpClient, settings);
        HerbApi = RestService.For<IHerbApi>(HttpClient, settings);
        FormulaApi = RestService.For<IFormulaApi>(HttpClient, settings);
        MedicalCaseApi = RestService.For<IMedicalCaseApi>(HttpClient, settings);
        RegistrationApi = RestService.For<IRegistrationApi>(HttpClient, settings);
    }

    /// <summary>用当前 JWT 创建用户（sysadmin 建 Admin；Admin 建 Doctor/Receptionist）</summary>
    protected async Task<UserDetailDto?> CreateUserAsync(string username, string password, UserRole role, string realName)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await UserApi.CreateUserAsync(new UserInputDto
        {
            UserName = username,
            Password = password,
            ConfirmPassword = password,
            RealName = realName,
            Role = role,
            Remark = "E2E测试自动创建"
        });
        if (resp.Success && resp.Data != null)
        {
            CreatedUserIds.Add(resp.Data.Id);
        }
        return resp.Success ? resp.Data : null;
    }

    /// <summary>创建 Admin 并登录该 Admin（admin 可再建 Doctor/Receptionist）</summary>
    protected async Task LoginAsCreatedAdminAsync()
    {
        var adminName = UniqueUsername("e2eadmin");
        var created = await CreateUserAsync(adminName, RolePassword, UserRole.Admin, "E2E管理员");
        if (created == null)
        {
            throw new InvalidOperationException("sysadmin 创建 Admin 账号失败");
        }
        await LoginAsAsync(adminName, RolePassword);
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
