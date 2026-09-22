using Asp.Versioning;
using LYBT.Infrastructure.Services.Backup;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 数据库备份/恢复 API（B-06 / US-SHELL-013）。
/// 服务端针对远程 SQL Server 实例执行 <c>BACKUP DATABASE</c> / <c>RESTORE DATABASE</c>，
/// 备份文件落服务端 <c>Backup:Directory</c>（默认 <c>{内容根}/backup</c>）。
/// 权限：管理操作 SysAdminOnly；<c>POST auto</c> 为登录触发的自动备份（仅要求已认证）。
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/backup")]
public class BackupController : BaseBackupController
{
    /// <summary>构造函数</summary>
    public BackupController(IBackupService backupService, ILogger<BackupController> logger)
        : base(backupService, logger)
    {
    }
}
