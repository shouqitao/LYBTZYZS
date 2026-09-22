using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Services.Backup;
using LYBT.Shared.Models.Contracts.Backup;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// 数据库备份/恢复 Controller 共享基类（B-06 / US-SHELL-013）。
/// </summary>
/// <remarks>
/// <para><b>双端一致</b>：远程 <c>LYBT.WebAPI.Controllers.BackupController</c> 与本地
/// <c>LYBT.LocalWebAPI.Controllers.BackupController</c> 均直接继承本类，路由/权限/契约唯一来源，
/// 避免双控制器树漂移（ADR-0010/0023）。</para>
/// <para><b>授权</b>：类级 <see cref="AuthorizeAttribute"/> 仅要求已认证——登录触发的自动备份
/// （NFR-AVAIL-001「登录成功后自动备份」，<c>POST auto</c>）由任意登录角色发起；
/// 其余管理操作（列表/创建/恢复/删除/清理/表清单）逐方法收紧为 <c>SysAdminOnly</c>。</para>
/// </remarks>
[ApiController]
[Authorize]
public abstract class BaseBackupController : BaseApiController
{
    private readonly IBackupService _backupService;

    /// <summary>构造函数</summary>
    protected BaseBackupController(IBackupService backupService, ILogger logger)
        : base(logger)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    /// <summary>
    /// 备份文件列表（按备份时间倒序）
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PolicyConstants.SysAdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BackupFileDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBackups(CancellationToken ct)
    {
        var files = await _backupService.ListAsync(ct);
        return Success(files, "查询成功");
    }

    /// <summary>
    /// 备份状态（上次备份时间/文件数量/总大小/备份目录/进行中作业与进度）
    /// </summary>
    [HttpGet("status")]
    [Authorize(Policy = PolicyConstants.SysAdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<BackupStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var status = await _backupService.GetStatusAsync(ct);
        return Success(status, "查询成功");
    }

    /// <summary>
    /// 可选择性恢复的表清单（含记录数与是否支持记录级选择）
    /// </summary>
    [HttpGet("tables")]
    [Authorize(Policy = PolicyConstants.SysAdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BackupTableDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTables(CancellationToken ct)
    {
        var tables = await _backupService.ListTablesAsync(ct);
        return Success(tables, "查询成功");
    }

    /// <summary>
    /// 创建备份（全量/差异，可选压缩与文件级加密）
    /// </summary>
    /// <param name="request">备份请求（类型/压缩/加密/口令）</param>
    /// <param name="ct">取消令牌</param>
    [HttpPost]
    [Authorize(Policy = PolicyConstants.SysAdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<BackupOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] BackupCreateRequestDto? request, CancellationToken ct)
    {
        var result = await _backupService.CreateAsync(request ?? new BackupCreateRequestDto(), ct);
        return result.Success ? Success(result, result.Message) : BusinessFail(result.Error ?? "备份失败");
    }

    /// <summary>
    /// 恢复指定备份（整库覆盖或按表/记录选择性回写；默认恢复前自动备份当前数据）
    /// </summary>
    /// <param name="id">备份 Id（取自备份列表）</param>
    /// <param name="request">恢复请求（模式/表清单/口令/是否预备份）</param>
    /// <param name="ct">取消令牌</param>
    [HttpPost("{id}/restore")]
    [Authorize(Policy = PolicyConstants.SysAdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<BackupOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Restore(
        string id,
        [FromBody] RestoreRequestDto? request,
        CancellationToken ct)
    {
        var payload = request ?? new RestoreRequestDto();
        payload.BackupId = id;

        var result = await _backupService.RestoreAsync(payload, ct);
        return result.Success ? Success(result, result.Message) : BusinessFail(result.Error ?? "恢复失败");
    }

    /// <summary>
    /// 删除指定备份文件（被差异备份引用的全量备份拒绝删除）
    /// </summary>
    /// <param name="id">备份 Id（取自备份列表）</param>
    /// <param name="ct">取消令牌</param>
    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyConstants.SysAdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<BackupOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _backupService.DeleteAsync(id, ct);
        return result.Success ? Success(result, result.Message) : BusinessFail(result.Error ?? "删除失败");
    }

    /// <summary>
    /// 清理超过保留期的备份文件（保护最新全量备份与其差异备份链）
    /// </summary>
    [HttpPost("cleanup")]
    [Authorize(Policy = PolicyConstants.SysAdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<BackupOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cleanup(CancellationToken ct)
    {
        var result = await _backupService.CleanupAsync(ct);
        return result.Success ? Success(result, result.Message) : BusinessFail(result.Error ?? "清理失败");
    }

    /// <summary>
    /// 自动备份（NFR-AVAIL-001：登录成功后触发；距上次备份未满间隔时为空操作）
    /// </summary>
    [HttpPost("auto")]
    [ProducesResponseType(typeof(ApiResponse<BackupOperationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AutoBackup(CancellationToken ct)
    {
        var result = await _backupService.AutoBackupAsync(ct);
        return result.Success ? Success(result, result.Message) : BusinessFail(result.Error ?? "自动备份失败");
    }
}
