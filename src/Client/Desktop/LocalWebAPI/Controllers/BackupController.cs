using LYBT.Infrastructure.Services.Backup;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 数据库备份/恢复 API（本地模式，B-06 / US-SHELL-013）。
/// 与本机 LocalDB（<c>(localdb)\MSSQLLocalDB</c>）同库执行 <c>BACKUP DATABASE</c> / <c>RESTORE DATABASE</c>，
/// 备份文件落客户端 <c>Backup:Directory</c>（默认 <c>%LOCALAPPDATA%\LYBT\Desktop\Backup</c>）。
/// 路由/权限与远程 <c>LYBT.WebAPI.Controllers.BackupController</c> 完全一致（同基类）。
/// </summary>
[ApiController]
[Route("api/v1/backup")]
public class BackupController : BaseBackupController
{
    /// <summary>构造函数</summary>
    public BackupController(IBackupService backupService, ILogger<BackupController> logger)
        : base(backupService, logger)
    {
    }
}
