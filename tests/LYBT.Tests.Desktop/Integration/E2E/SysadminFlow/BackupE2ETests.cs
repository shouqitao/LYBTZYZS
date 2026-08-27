// ---------------------------------------------------------------------------
// BackupE2ETests — US-SYS-002 备份/部署运维面（HTTP 可测部分）
// ---------------------------------------------------------------------------
// 备份本体（ILocalDbBackupService）为桌面本地文件服务：T-SQL BACKUP DATABASE 到
// %AppData%/LYBTZYZS/Backup/，依赖固定 LYBTDesktop 库 + IEmbeddedLocalWebApiService
// 编排停止/重启内嵌服务器——在「不 mock」约束下无法对测试库执行（连接串硬编码），
// 本文件覆盖其 HTTP 运维兄弟面：部署（DeployController，SysAdminOnly）的权限边界与
// 本地模式优雅降级。备份文件的创建/列出/恢复由 Shell 侧 LocalDbBackupService 单测覆盖。
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
