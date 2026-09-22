using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Backup;
using LYBT.Shared.Models.Contracts.Common;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.LocalApi;

/// <summary>
/// 备份/恢复 HTTP 面 E2E（B-06 / US-SHELL-013）——真实 LocalWebAPI（LocalDB）+ 真实备份引擎。
/// </summary>
/// <remarks>
/// 备份目录由基类经 <c>Backup:Directory</c> 覆盖为本次测试专属临时目录
/// （<see cref="LocalWebApiTestBase.BackupDirectoryPath"/>），绝不触碰开发者真实
/// <c>%LOCALAPPDATA%\LYBT\Desktop\Backup</c>；基类 <c>DisposeAsync</c> 负责清理目录与 LocalDB。
/// </remarks>
[Trait("Category", "LocalApi")]
[Collection("LocalApi")]
public class BackupLocalApiTests : LocalWebApiTestBase
{
    /// <summary>
    /// 备份契约的 JSON 选项：继承基类 <see cref="LocalWebApiTestBase.Json"/>（大小写不敏感、保留属性名），
    /// 补充枚举字符串转换器——LocalWebAPI 枚举字符串化（ADR-0022），基类选项未注册该转换器时
    /// 读取含 <see cref="BackupKind"/> 的 <see cref="BackupFileDto"/> 会反序列化失败。
    /// </summary>
    private static readonly JsonSerializerOptions BackupJson = new(Json)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    [Trait("US", "US-SHELL-013")]
    public async Task Sysadmin_GetBackupStatus_ReturnsIsolatedDirectoryAndCurrentDatabase()
    {
        SetAuthHeader(await GetSysadminTokenAsync());

        var response = await Client.GetAsync("/api/v1/backup/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = E2EAssertionHelpers.AssertSuccess(await ReadAsync<BackupStatusDto>(response));
        status.BackupDirectory.Should().Be(BackupDirectoryPath,
            "备份状态应回报本次测试隔离的临时目录（Backup:Directory 覆盖生效）");
        status.RetentionDays.Should().Be(7, "未配置 Backup:RetentionDays 时默认 7 天（NFR-AVAIL-001）");
        status.DatabaseName.Should().Be(CurrentDatabaseName, "备份状态应回报当前宿主的测试库名");
    }

    [Fact]
    [Trait("US", "US-SHELL-013")]
    public async Task Sysadmin_GetBackups_InitiallyEmpty()
    {
        SetAuthHeader(await GetSysadminTokenAsync());

        var files = await ListBackupsAsync();

        files.Should().BeEmpty("全新隔离备份目录初始无任何备份文件");
    }

    [Fact]
    [Trait("US", "US-SHELL-013")]
    public async Task Sysadmin_CreateBackup_WritesFileAndListsIt()
    {
        SetAuthHeader(await GetSysadminTokenAsync());

        var created = await CreateBackupAsync();

        created.FileName.Should().NotBeNullOrWhiteSpace("创建备份应返回备份文件名");
        File.Exists(Path.Combine(BackupDirectoryPath, created.FileName))
            .Should().BeTrue($"备份文件应落在隔离目录 {BackupDirectoryPath} 下");

        var files = await ListBackupsAsync();
        files.Should().ContainSingle("创建一次备份后列表应恰好一条");
        files[0].Id.Should().Be(created.Id, "列表项 Id 应与创建结果一致，保证选中后可恢复/删除");
        files[0].FileName.Should().Be(created.FileName);
    }

    [Fact]
    [Trait("US", "US-SHELL-013")]
    public async Task Sysadmin_DeleteBackup_RemovesFileAndListBecomesEmpty()
    {
        SetAuthHeader(await GetSysadminTokenAsync());

        var created = await CreateBackupAsync();
        var filePath = Path.Combine(BackupDirectoryPath, created.FileName);
        File.Exists(filePath).Should().BeTrue();

        var response = await Client.DeleteAsync($"/api/v1/backup/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        E2EAssertionHelpers.AssertSuccess(await ReadAsync<BackupOperationResultDto>(response));

        File.Exists(filePath).Should().BeFalse("删除备份应同时移除磁盘文件");
        (await ListBackupsAsync()).Should().BeEmpty("删除后备份列表应恢复为空");
    }

    [Fact]
    [Trait("US", "US-SHELL-013")]
    public async Task Sysadmin_AutoBackup_ReturnsSuccessEnvelope()
    {
        SetAuthHeader(await GetSysadminTokenAsync());

        // 登录触发的自动备份：全新目录会执行一次全量；若距上次备份未满间隔则为空操作。
        // 两种路径都应返回成功信封（仅断言信封 + Success，不断言备份产物）。
        var response = await Client.PostAsync("/api/v1/backup/auto", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = E2EAssertionHelpers.AssertSuccess(await ReadAsync<BackupOperationResultDto>(response));
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("US", "US-SHELL-013")]
    public async Task Doctor_CreateBackup_IsForbidden()
    {
        SetAuthHeader(await GetDoctorTokenAsync());

        var response = await Client.PostAsJsonAsync(
            "/api/v1/backup",
            new BackupCreateRequestDto { Kind = BackupKind.Full, Compress = true },
            BackupJson);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "创建备份属运维操作——仅 SysAdminOnly（医生无权）");
    }

    [Fact]
    [Trait("US", "US-SHELL-013")]
    public async Task Doctor_GetBackups_IsForbidden()
    {
        SetAuthHeader(await GetDoctorTokenAsync());

        var response = await Client.GetAsync("/api/v1/backup");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "备份列表属运维操作——仅 SysAdminOnly（医生无权）");
    }

    private static async Task<ApiResponse<T>> ReadAsync<T>(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<ApiResponse<T>>(BackupJson))!;

    private async Task<BackupFileDto> CreateBackupAsync()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/backup",
            new BackupCreateRequestDto { Kind = BackupKind.Full, Compress = true },
            BackupJson);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = E2EAssertionHelpers.AssertSuccess(await ReadAsync<BackupOperationResultDto>(response));
        result.File.Should().NotBeNull("创建备份应返回备份文件信息");
        return result.File!;
    }

    private async Task<List<BackupFileDto>> ListBackupsAsync()
    {
        var response = await Client.GetAsync("/api/v1/backup");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return E2EAssertionHelpers.AssertSuccess(await ReadAsync<List<BackupFileDto>>(response));
    }
}
