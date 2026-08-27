// ---------------------------------------------------------------------------
// DiagnosticsE2ETests — US-SYS-001 系统诊断：DB 信息 + 版本 + 日志级别 全链路
// 真实链路：桌面 IApiClientDiagnostics → LocalWebAPI DiagnosticsController → LocalDB
// 权限：AdminOrSuperAdmin（Admin/SuperAdmin 可用，Doctor 403）
// ---------------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LYBT.Tests.Desktop.E2E.SysadminFlow;

[Collection("E2ELocal")]
public class DiagnosticsE2ETests : E2ETestBase
{
    [Fact]
    public async Task Admin_GetLoggingStatus_Succeeds()
    {
        await LoginAsAdminAsync();

        var result = await DiagnosticsApi.GetLoggingStatusAsync();

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task Admin_SetLoggingLevel_Succeeds()
    {
        await LoginAsAdminAsync();

        var result = await DiagnosticsApi.SetLoggingLevelAsync(
            new LYBT.Shared.Models.Contracts.Diagnostics.SetLoggingLevelRequest { Level = "Debug" });

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public async Task Admin_EnableAndDisableDebugMode_Succeeds()
    {
        await LoginAsAdminAsync();

        var enabled = await DiagnosticsApi.EnableDebugModeAsync(
            new LYBT.Shared.Models.Contracts.Diagnostics.EnableDebugModeRequest { Level = "Debug", DurationMinutes = 60 });
        Assert.True(enabled.Success, enabled.Message);

        var disabled = await DiagnosticsApi.DisableDebugModeAsync();
        Assert.True(disabled.Success, disabled.Message);
    }

    [Fact]
    public async Task Admin_GetDbInfo_ReportsHealthy()
    {
        await LoginAsAdminAsync();

        var response = await Client.GetAsync("/api/v1/diagnostics/db-info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(json.GetProperty("success").GetBoolean(), json.ToString());
    }

    [Fact]
    public async Task Admin_GetVersion_ReturnsVersion()
    {
        await LoginAsAdminAsync();

        var response = await Client.GetAsync("/api/v1/diagnostics/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(json.GetProperty("success").GetBoolean(), json.ToString());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("data").GetProperty("assemblyVersion").GetString()));
    }

    [Fact]
    public async Task Doctor_GetLoggingStatus_Forbidden()
    {
        await LoginAsDoctorAsync();

        await AssertForbiddenAsync(() => DiagnosticsApi.GetLoggingStatusAsync());
    }
}
