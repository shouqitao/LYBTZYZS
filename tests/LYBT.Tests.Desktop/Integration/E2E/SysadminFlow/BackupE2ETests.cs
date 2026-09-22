// ---------------------------------------------------------------------------
// BackupE2ETests — US-SYS-002 备份/部署运维面（HTTP 可测部分）
// ---------------------------------------------------------------------------
// 备份本体（B-06 起）为 LocalWebAPI 宿主侧共享引擎（LYBT.Infrastructure.Services.Backup），
// 经 /api/v1/backup/* 暴露——备份文件创建/列出/恢复/删除与权限边界的 HTTP 断言见
// Integration/LocalApi/BackupLocalApiTests.cs，引擎自身的 T-SQL 行为见
// tests/LYBT.Tests.Server/Unit/Backup/SqlServerBackupServiceTests.cs。
// 本文件覆盖其 HTTP 运维兄弟面：部署（DeployController，SysAdminOnly）的权限边界与
// 本地模式优雅降级。
// ---------------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LYBT.Tests.Desktop.E2E.SysadminFlow;

[Collection("E2ELocal")]
public class BackupE2ETests : E2ETestBase
{
    [Fact]
    public async Task Sysadmin_DeployRestart_ReturnsGracefulLocalFailure()
    {
        await LoginAsSysadminAsync();

        var response = await Client.PostAsync("/api/v1/deploy/restart", null);

        // 本地模式不支持部署：HTTP 422（业务失败信封，优雅提示，非 5xx）
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Contains("本地模式", json.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Admin_DeployRestart_Forbidden()
    {
        await LoginAsAdminAsync();

        var response = await Client.PostAsync("/api/v1/deploy/restart", null);

        // DEPLOY-PERM：部署属运维操作，仅 SuperAdmin
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
