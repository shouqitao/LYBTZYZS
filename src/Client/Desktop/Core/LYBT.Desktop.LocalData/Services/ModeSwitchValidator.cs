using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Services;

/// <summary>
/// 模式切换验证器 - 切换前置检查 (US-SYNC-008)
/// SYNC-D01: 本地有 Active/Suspended 医案时阻断切换到远程模式
/// </summary>
public sealed class ModeSwitchValidator : IModeSwitchValidator
{
    private readonly IMedicalCaseDataSource _medicalCaseDataSource;
    private readonly string _localConnectionString;
    private readonly ILogger<ModeSwitchValidator> _logger;

    public ModeSwitchValidator(
        IMedicalCaseDataSource medicalCaseDataSource,
        string localConnectionString,
        ILogger<ModeSwitchValidator> logger)
    {
        _medicalCaseDataSource = medicalCaseDataSource ?? throw new ArgumentNullException(nameof(medicalCaseDataSource));
        _localConnectionString = localConnectionString ?? throw new ArgumentNullException(nameof(localConnectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 验证本地 -> 远程切换 (SYNC-D01)
    /// 查询本地 Active/Suspended 医案数量，有则阻断
    /// </summary>
    public async Task<ModeSwitchValidationResult> ValidateLocalToRemoteSwitchAsync(CancellationToken ct = default)
    {
        try
        {
            var (_, activeCount) = await _medicalCaseDataSource.QueryAsync(
                status: MedicalCaseStatus.Active, page: 1, pageSize: 1, ct: ct);

            var (_, suspendedCount) = await _medicalCaseDataSource.QueryAsync(
                status: MedicalCaseStatus.Suspended, page: 1, pageSize: 1, ct: ct);

            var totalUnfinished = activeCount + suspendedCount;

            if (totalUnfinished > 0)
            {
                _logger.LogWarning(
                    "[ModeSwitchValidator] Local->Remote blocked: {Count} unfinished cases (Active={Active}, Suspended={Suspended})",
                    totalUnfinished, activeCount, suspendedCount);

                return ModeSwitchValidationResult.Failed(
                    $"本地有 {totalUnfinished} 个未完成的医案，请先完成或取消后再切换模式",
                    totalUnfinished);
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
