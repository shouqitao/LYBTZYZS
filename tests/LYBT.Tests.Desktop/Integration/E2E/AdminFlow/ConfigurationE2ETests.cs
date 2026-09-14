// ---------------------------------------------------------------------------
// ConfigurationE2ETests — US-SHELL-003 系统配置读写 全链路
// 真实链路：原始 HTTP → LocalWebAPI ConfigurationController → IConfigurationStore → LocalDB
// 权限：SysAdminOnly（仅 SuperAdmin；Admin 403）
// 注：桌面 ConfigurationHttpApiClient 本地模式全部抛 NotSupported（既有设计），
// 故配置读写经原始 Client 直连本地端点验证
// 另含 US-CFG-006 诊所信息热更新（客户端文件链：ClientConfigurationStore → clinic-settings.json →
// IConfiguration 重载 → ClinicSettingsService 读取）
// ---------------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.Services.FeatureToggle;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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

    /// <summary>
    /// US-CFG-006：诊所信息（名称/地址/电话）保存 → 落盘 clinic-settings.json + 配置立即可读
    /// （IConfiguration 重载），且不影响其他配置节。
    /// 写入路径为客户端配置存储 ClientConfigurationStore（AppContext.BaseDirectory/clinic-settings.json），
    /// 与 Shell 启动时的 AddJsonFile("clinic-settings.json", reloadOnChange: true) 读取路径一致。
    /// </summary>
    /// <remarks>
    /// 已知缺口（登记见 13c）：① LocalWebAPI 的 /configuration/sections/ClinicSettings 被写入白名单拦下
    /// （ClinicSettings/FeatureToggles 不在 ConfigurationWritePolicy.AllowedSections）→ 本地端点 422，
    /// 诊所信息只能走客户端文件链；② 生产读取方 ClinicSettingsService/ConfigurationCenterViewModel 注入的是
    /// PrismConfigurationExtensions.RegisterOptions 在**启动时**绑定的静态 IOptions 快照 → 同进程内不会
    /// 观察到重载后的新值（本测试以「重启语义」绑定验证文件链本身正确）；③ SystemSettingsViewModel（US-CFG-006 UI）
    /// 经 IClinicSettingsService.SaveSettingsAsync 写入 <c>Directory.GetCurrentDirectory()/clinic-settings.json</c>，
    /// 与 Shell 读取的 <c>AppContext.BaseDirectory/clinic-settings.json</c> 可能不是同一文件（仅当进程工作目录=程序目录时重合）。
    /// </remarks>
    [Fact]
    public async Task UpdateClinicSettings_PersistsAndReloads()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "clinic-settings.json");
        var originalContent = File.Exists(settingsPath) ? await File.ReadAllTextAsync(settingsPath) : null;
        try
        {
            // Arrange — 预置诊所节（旧值）+ 无关节（FeatureToggles/App 不得被诊所更新牵连）
            await File.WriteAllTextAsync(settingsPath, """
            {
              "ClinicSettings": { "Name": "旧诊所", "Address": "旧地址", "Phone": "旧电话" },
              "FeatureToggles": { "EnableCardReader": "true" },
              "App": { "Name": "E2E 无关配置" }
            }
            """);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("clinic-settings.json", optional: false, reloadOnChange: true)
                .Build();
            var store = new ClientConfigurationStore(
                configuration,
                NullLogger<ClientConfigurationStore>.Instance);

            var newName = UniqueName("诊所");
            var newAddress = $"E2E 地址 {Guid.NewGuid():N}";
            var newPhone = "010-88886666";

            // Act — 保存诊所三项信息（节级覆盖，保留其他节）
            var saved = await store.SaveSectionAsync("ClinicSettings", new Dictionary<string, object>
            {
                ["Name"] = newName,
                ["Address"] = newAddress,
                ["Phone"] = newPhone,
                ["Department"] = "中医科"
            });

            // Assert — 1) 落盘持久化
            Assert.True(saved);
            var persisted = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath)).RootElement
                .GetProperty("ClinicSettings");
            Assert.Equal(newName, persisted.GetProperty("Name").GetString());
            Assert.Equal(newAddress, persisted.GetProperty("Address").GetString());
            Assert.Equal(newPhone, persisted.GetProperty("Phone").GetString());

            // 2) 配置立即可读（IConfiguration 重载生效，无需重启）
            var live = configuration.GetSection(ClinicSettingsOptions.SectionName).Get<ClinicSettingsOptions>();
            Assert.NotNull(live);
            Assert.Equal(newName, live!.Name);
            Assert.Equal(newAddress, live.Address);
            Assert.Equal(newPhone, live.Phone);

            // 3) 消费方（ClinicSettingsService）按新值读取
            var service = new ClinicSettingsService(
                Options.Create(live),
                NullLogger<ClinicSettingsService>.Instance);
            Assert.Equal(newName, service.ClinicName);
            Assert.Equal(newAddress, service.ClinicAddress);
            Assert.Equal(newPhone, service.ClinicPhone);

            // 4) 其他配置节不受影响（文件与内存配置均原样保留）
            var persistedRoot = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath)).RootElement;
            Assert.Equal("true", persistedRoot.GetProperty("FeatureToggles").GetProperty("EnableCardReader").GetString());
            Assert.Equal("E2E 无关配置", persistedRoot.GetProperty("App").GetProperty("Name").GetString());
            Assert.Equal(
                "true",
                configuration[$"{FeatureToggleOptions.SectionName}:EnableCardReader"]);
            Assert.Equal("E2E 无关配置", configuration["App:Name"]);
        }
        finally
        {
            if (originalContent is null)
                File.Delete(settingsPath);
            else
                await File.WriteAllTextAsync(settingsPath, originalContent);
        }
    }
}
