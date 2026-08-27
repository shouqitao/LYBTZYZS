// ---------------------------------------------------------------------------
// ConfigurationE2ETests — US-SHELL-003 系统配置读写 全链路
// 真实链路：原始 HTTP → LocalWebAPI ConfigurationController → IConfigurationStore → LocalDB
// 权限：SysAdminOnly（仅 SuperAdmin；Admin 403）
// 注：桌面 ConfigurationHttpApiClient 本地模式全部抛 NotSupported（既有设计），
// 故配置读写经原始 Client 直连本地端点验证
// ---------------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LYBT.Tests.Desktop.E2E.AdminFlow;

[Collection("E2ELocal")]
public class ConfigurationE2ETests : E2ETestBase
{
    [Fact]
    public async Task Sysadmin_GetConfiguration_Succeeds()
    {
        await LoginAsSysadminAsync();

        var response = await Client.GetAsync("/api/v1/configuration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(json.GetProperty("success").GetBoolean(), json.ToString());
    }

    [Fact]
    public async Task Sysadmin_SetAndGetValue_RoundTrips()
    {
        await LoginAsSysadminAsync();
        var key = $"e2e_key_{Guid.NewGuid():N}";
        var value = "e2e-value-42";

        var setResponse = await Client.PutAsJsonAsync($"/api/v1/configuration/{key}", value);
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
        var setJson = await setResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(setJson.GetProperty("success").GetBoolean(), setJson.ToString());

        var getResponse = await Client.GetAsync($"/api/v1/configuration/{key}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(value, getJson.GetProperty("data").GetProperty("value").GetString());
    }

    [Fact]
    public async Task Admin_GetConfiguration_Forbidden()
    {
        await LoginAsAdminAsync();

        var response = await Client.GetAsync("/api/v1/configuration");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
