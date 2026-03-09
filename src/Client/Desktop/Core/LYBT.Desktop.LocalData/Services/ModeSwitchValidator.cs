using LYBT.Desktop.Contracts.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Services;

/// <summary>
/// 模式切换验证器 - 切换前置检查 (US-SYNC-008)
/// SYNC-D01: 本地有 Active/Suspended 医案时阻断切换到远程模式
/// SYNC-D02: 直接使用 SQL 查询本地库计数，不再依赖 DataSource/Repository 层
/// </summary>
public sealed class ModeSwitchValidator : IModeSwitchValidator
{
    private readonly string _localConnectionString;
    private readonly ILogger<ModeSwitchValidator> _logger;

    /// <summary>
    /// 查询 Active(0) + Suspended(4) 状态的医案计数
    /// CaseStatus 枚举值: Active=0, Completed=1, Cancelled=2, Deleted=3, Suspended=4
    /// </summary>
    private const string CountUnfinishedCasesSql =
        "SELECT COUNT(*) FROM MedicalCases WHERE IsDeleted = 0 AND (CaseStatus = 0 OR CaseStatus = 4)";

    public ModeSwitchValidator(
        string localConnectionString,
        ILogger<ModeSwitchValidator> logger)
    {
        _localConnectionString = localConnectionString ?? throw new ArgumentNullException(nameof(localConnectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 验证本地 -> 远程切换 (SYNC-D01)
    /// 直接查询本地数据库中 Active/Suspended 医案数量，有则阻断
    /// </summary>
    public async Task<ModeSwitchValidationResult> ValidateLocalToRemoteSwitchAsync(CancellationToken ct = default)
    {
        try
        {
            await using var connection = new SqlConnection(_localConnectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(CountUnfinishedCasesSql, connection);
            var count = (int)(await command.ExecuteScalarAsync(ct))!;

            if (count > 0)
            {
                _logger.LogWarning(
                    "[ModeSwitchValidator] Local->Remote blocked: {Count} unfinished cases",
                    count);

                return ModeSwitchValidationResult.Failed(
                    $"本地有 {count} 个未完成的医案，请先完成或取消后再切换模式",
                    count);
            }

            _logger.LogInformation("[ModeSwitchValidator] Local->Remote validation passed");
            return ModeSwitchValidationResult.Valid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ModeSwitchValidator] Local->Remote validation failed with exception");
            return ModeSwitchValidationResult.Failed($"检查失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证远程 -> 本地切换
    /// 检查 LocalDB 是否可连接
    /// </summary>
    public async Task<ModeSwitchValidationResult> ValidateRemoteToLocalSwitchAsync(CancellationToken ct = default)
    {
        try
        {
            await using var connection = new SqlConnection(_localConnectionString);
            await connection.OpenAsync(ct);

            _logger.LogInformation("[ModeSwitchValidator] Remote->Local validation passed (LocalDB accessible)");
            return ModeSwitchValidationResult.Valid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ModeSwitchValidator] Remote->Local blocked: LocalDB not accessible");
            return ModeSwitchValidationResult.Failed($"本地数据库不可用: {ex.Message}");
        }
    }
}
