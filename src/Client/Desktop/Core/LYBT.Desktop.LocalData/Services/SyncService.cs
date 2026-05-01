using System.Text.Json;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Sync;
using Microsoft.EntityFrameworkCore;
using LYBT.Desktop.LocalData.Helpers;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Desktop.LocalData.Services;

/// <summary>
/// 数据同步服务实现
/// 负责协调本地数据与服务器之间的同步操作
/// </summary>
public class SyncService : ISyncService
{
    private readonly ISyncApi _syncApi;
    private readonly LocalDbContext _context;
    private readonly ILogger<SyncService> _logger;
    private readonly FeatureToggleOptions _featureToggleOptions;
    private readonly IDesktopCacheManager? _cacheManager;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SyncService(
        ISyncApi syncApi,
        LocalDbContext context,
        ILogger<SyncService> logger,
        IOptions<FeatureToggleOptions> featureToggleOptions,
        IDesktopCacheManager? cacheManager = null)
    {
        _syncApi = syncApi;
        _context = context;
        _logger = logger;
        _featureToggleOptions = featureToggleOptions.Value;
        _cacheManager = cacheManager;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSupportedEntityTypesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[SyncService] 获取支持的实体类型");

        var response = await _syncApi.GetEntityTypesAsync();
        if (!response.Success || response.Data == null)
        {
            _logger.LogWarning("[SyncService] 获取实体类型失败: {Message}", response.Message);
            return [];
        }

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<SyncCheckResult> CheckDifferencesAsync(string entityType, CancellationToken ct = default)
    {
        _logger.LogInformation("[SyncService] 检查差异 - EntityType={EntityType}", entityType);

        var result = new SyncCheckResult { EntityType = entityType };

        // 1. 获取本地元数据
        var localMetadata = await GetLocalMetadataAsync(entityType, ct);
        _logger.LogDebug("[SyncService] 本地数据: {Count} 条", localMetadata.Count);

        // 2. 获取服务器元数据
        var serverResponse = await _syncApi.GetMetadataAsync(entityType);
        if (!serverResponse.Success || serverResponse.Data == null)
        {
            _logger.LogWarning("[SyncService] 获取服务器元数据失败: {Message}", serverResponse.Message);
            return result;
        }

        var serverMetadata = serverResponse.Data.ToDictionary(m => m.EntityId);
        _logger.LogDebug("[SyncService] 服务器数据: {Count} 条", serverMetadata.Count);

        // 3. 比对差异
        foreach (var local in localMetadata)
        {
            if (serverMetadata.TryGetValue(local.EntityId, out var server))
            {
                // 双方都有，比较 Checksum
                if (local.Checksum != server.Checksum)
                {
                    result.Conflicts.Add(new SyncDiffDto
                    {
                        EntityType = entityType,
                        EntityId = local.EntityId,
                        DiffType = SyncDiffType.Modified,
                        EntityName = local.EntityName,
                        LocalChecksum = local.Checksum,
                        ServerChecksum = server.Checksum,
                        LocalChangedAt = local.LastModifiedAt,
                        ServerChangedAt = server.LastModifiedAt
                    });
                }
                serverMetadata.Remove(local.EntityId);
            }
            else
            {
                // 仅本地有
                result.LocalOnly.Add(new SyncDiffDto
                {
                    EntityType = entityType,
                    EntityId = local.EntityId,
                    DiffType = SyncDiffType.LocalOnly,
                    EntityName = local.EntityName,
                    LocalChecksum = local.Checksum,
                    LocalChangedAt = local.LastModifiedAt
                });
            }
        }

        // 剩余的服务器数据是仅服务器有的
        foreach (var server in serverMetadata.Values)
        {
            result.ServerOnly.Add(new SyncDiffDto
            {
                EntityType = entityType,
                EntityId = server.EntityId,
                DiffType = SyncDiffType.ServerOnly,
                ServerChecksum = server.Checksum,
                ServerChangedAt = server.LastModifiedAt
            });
        }

        _logger.LogInformation(
            "[SyncService] 差异检查完成 - LocalOnly={LocalOnly}, ServerOnly={ServerOnly}, Conflicts={Conflicts}",
            result.LocalOnly.Count, result.ServerOnly.Count, result.Conflicts.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<SyncUploadResultDto> UploadAsync(
        string entityType,
        List<Guid> entityIds,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[SyncService] 上传数据 - EntityType={EntityType}, Count={Count}",
            entityType, entityIds.Count);

        // 获取本地实体数据
        var entities = await GetLocalEntitiesAsJsonAsync(entityType, entityIds, ct);

        var input = new SyncUploadInputDto
        {
            EntityType = entityType,
            Entities = entities,
            OverwriteConflicts = _featureToggleOptions.OverwriteConflicts
        };

        var response = await _syncApi.UploadAsync(input);
        if (!response.Success || response.Data == null)
        {
            _logger.LogWarning("[SyncService] 上传失败: {Message}", response.Message);
            return new SyncUploadResultDto
            {
                ErrorCount = entityIds.Count,
                Results = entityIds.Select(id => new SyncUploadItemResult
                {
                    EntityId = id,
                    Success = false,
                    ErrorMessage = response.Message
                }).ToList()
            };
        }

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<SyncDownloadResultDto> DownloadAsync(
        string entityType,
        List<Guid> entityIds,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[SyncService] 下载数据 - EntityType={EntityType}, Count={Count}",
            entityType, entityIds.Count);

        var input = new SyncDownloadInputDto
        {
            EntityType = entityType,
            EntityIds = entityIds
        };

        var response = await _syncApi.DownloadAsync(input);
        if (!response.Success || response.Data == null)
        {
            _logger.LogWarning("[SyncService] 下载失败: {Message}", response.Message);
            return new SyncDownloadResultDto
            {
                EntityType = entityType,
                Count = 0
            };
        }

        // 保存到本地数据库
        await SaveDownloadedEntitiesAsync(entityType, response.Data.Entities, ct);

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<SyncDeleteResultDto> DeleteAsync(
        string entityType,
        List<Guid> entityIds,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[SyncService] 删除数据 - EntityType={EntityType}, Count={Count}",
            entityType, entityIds.Count);

        var input = new SyncDeleteInputDto
        {
            EntityType = entityType,
            EntityIds = entityIds
        };

        var response = await _syncApi.DeleteAsync(input);
        if (!response.Success || response.Data == null)
        {
            _logger.LogWarning("[SyncService] 删除失败: {Message}", response.Message);
            return new SyncDeleteResultDto
            {
                Success = [],
                Rejected = entityIds.Select(id => new SyncDeleteRejectedItem
                {
                    EntityId = id,
                    Reason = response.Message
                }).ToList()
            };
        }

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<SyncExecutionResult> ExecuteSyncAsync(
        string entityType,
        SyncResolution resolution,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[SyncService] 执行同步 - EntityType={EntityType}", entityType);

        var result = new SyncExecutionResult { EntityType = entityType };

        // 1. 处理上传（LocalOnly + 选择使用本地的冲突）
        var toUpload = resolution.ToUpload.ToList();
        foreach (var (entityId, useLocal) in resolution.ConflictResolutions)
        {
            if (useLocal)
                toUpload.Add(entityId);
        }

        if (toUpload.Count > 0)
        {
            var uploadResult = await UploadAsync(entityType, toUpload, ct);
            result.UploadedCount = uploadResult.SuccessCount;
            if (uploadResult.ErrorCount > 0)
            {
                result.FailedCount += uploadResult.ErrorCount;
                result.Errors.Add($"上传失败 {uploadResult.ErrorCount} 条");
            }
        }

        // 2. 处理下载（ServerOnly + 选择使用服务器的冲突）
        var toDownload = resolution.ToDownload.ToList();
        foreach (var (entityId, useLocal) in resolution.ConflictResolutions)
        {
            if (!useLocal)
                toDownload.Add(entityId);
        }

        if (toDownload.Count > 0)
        {
            var downloadResult = await DownloadAsync(entityType, toDownload, ct);
            result.DownloadedCount = downloadResult.Count;
        }

        // 3. 处理删除（本地已软删除，同步到服务器）
        // US-SYNC-006: 客户端删除同步
        if (resolution.ToDelete.Count > 0)
        {
            var deleteResult = await DeleteAsync(entityType, resolution.ToDelete, ct);
            result.DeletedCount = deleteResult.SuccessCount;
            result.DeleteRejections = deleteResult.Rejected;

            if (deleteResult.RejectedCount > 0)
            {
                _logger.LogWarning(
                    "[SyncService] 删除被拒绝 {RejectedCount} 条 (引用检查未通过)",
                    deleteResult.RejectedCount);
            }
        }

        // 4. 记录跳过的
        result.SkippedCount = resolution.Skipped.Count;

        _logger.LogInformation(
            "[SyncService] 同步完成 - Uploaded={Uploaded}, Downloaded={Downloaded}, Deleted={Deleted}, Skipped={Skipped}, Failed={Failed}",
            result.UploadedCount, result.DownloadedCount, result.DeletedCount, result.SkippedCount, result.FailedCount);

        // 同步完成后清理所有 Desktop 缓存
        _cacheManager?.InvalidateAll();

        return result;
    }

    #region Private Methods

    /// <summary>
    /// 获取本地元数据（用于比对）
    /// </summary>
    private async Task<List<LocalMetadata>> GetLocalMetadataAsync(string entityType, CancellationToken ct)
    {
        return entityType switch
        {
            "Herb" => await GetHerbMetadataAsync(ct),
            "Patient" => await GetPatientMetadataAsync(ct),
            "Formula" => await GetFormulaMetadataAsync(ct),
            "MedicalCase" => await GetMedicalCaseMetadataAsync(ct),
            _ => throw new ArgumentException($"不支持的实体类型: {entityType}")
        };
    }

    private async Task<List<LocalMetadata>> GetHerbMetadataAsync(CancellationToken ct)
    {
        return await _context.Herbs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(h => new LocalMetadata
            {
                EntityId = h.Id,
                EntityName = h.Name,
                Checksum = ChecksumHelper.ComputeHerbChecksum(h),
                LastModifiedAt = h.UpdatedAt ?? h.CreatedAt
            })
            .ToListAsync(ct);
    }

    private async Task<List<LocalMetadata>> GetPatientMetadataAsync(CancellationToken ct)
    {
        return await _context.Patients
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(p => new LocalMetadata
            {
                EntityId = p.Id,
                EntityName = p.Name,
                Checksum = ChecksumHelper.ComputePatientChecksum(p),
                LastModifiedAt = p.UpdatedAt ?? p.CreatedAt
            })
            .ToListAsync(ct);
    }

    private async Task<List<LocalMetadata>> GetFormulaMetadataAsync(CancellationToken ct)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(f => new LocalMetadata
            {
                EntityId = f.Id,
                EntityName = f.Name,
                Checksum = ChecksumHelper.ComputeFormulaChecksum(f),
                LastModifiedAt = f.UpdatedAt ?? f.CreatedAt
            })
            .ToListAsync(ct);
    }

    private async Task<List<LocalMetadata>> GetMedicalCaseMetadataAsync(CancellationToken ct)
    {
        return await _context.MedicalCases
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p.Items)
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(mc => new LocalMetadata
            {
                EntityId = mc.Id,
                EntityName = mc.CaseNumber ?? mc.Id.ToString(),
                Checksum = ChecksumHelper.ComputeMedicalCaseChecksum(mc),
                LastModifiedAt = mc.UpdatedAt ?? mc.CreatedAt
            })
            .ToListAsync(ct);
    }

    /// <summary>
    /// 获取本地实体并序列化为 JSON 字符串
    /// </summary>
    private async Task<List<string>> GetLocalEntitiesAsJsonAsync(
        string entityType,
        List<Guid> entityIds,
        CancellationToken ct)
    {
        return entityType switch
        {
            "Herb" => await GetHerbsAsJsonAsync(entityIds, ct),
            "Patient" => await GetPatientsAsJsonAsync(entityIds, ct),
            "Formula" => await GetFormulasAsJsonAsync(entityIds, ct),
            "MedicalCase" => await GetMedicalCasesAsJsonAsync(entityIds, ct),
            _ => throw new ArgumentException($"不支持的实体类型: {entityType}")
        };
    }

    private async Task<List<string>> GetHerbsAsJsonAsync(List<Guid> entityIds, CancellationToken ct)
    {
        var herbs = await _context.Herbs
            .IgnoreQueryFilters()
            .Where(h => entityIds.Contains(h.Id))
            .AsNoTracking()
            .ToListAsync(ct);

        return herbs.Select(h => JsonSerializer.Serialize(h, JsonOptions)).ToList();
    }

    private async Task<List<string>> GetPatientsAsJsonAsync(List<Guid> entityIds, CancellationToken ct)
    {
        var patients = await _context.Patients
            .IgnoreQueryFilters()
            .Where(p => entityIds.Contains(p.Id))
            .AsNoTracking()
            .ToListAsync(ct);

        return patients.Select(p => JsonSerializer.Serialize(p, JsonOptions)).ToList();
    }

    private async Task<List<string>> GetFormulasAsJsonAsync(List<Guid> entityIds, CancellationToken ct)
    {
        var formulas = await _context.Formulas
            .Include(f => f.Herbs)
            .IgnoreQueryFilters()
            .Where(f => entityIds.Contains(f.Id))
            .AsNoTracking()
            .ToListAsync(ct);

        return formulas.Select(f => JsonSerializer.Serialize(f, JsonOptions)).ToList();
    }

    private async Task<List<string>> GetMedicalCasesAsJsonAsync(List<Guid> entityIds, CancellationToken ct)
    {
        var cases = await _context.MedicalCases
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p.Items)
            .IgnoreQueryFilters()
            .Where(mc => entityIds.Contains(mc.Id))
            .AsNoTracking()
            .ToListAsync(ct);

        return cases.Select(mc => JsonSerializer.Serialize(mc, JsonOptions)).ToList();
    }

    /// <summary>
    /// 保存下载的实体到本地数据库
    /// </summary>
    private async Task SaveDownloadedEntitiesAsync(
        string entityType,
        List<string> entities,
        CancellationToken ct)
    {
        switch (entityType)
        {
            case "Herb":
                await SaveHerbsAsync(entities, ct);
                break;
            case "Patient":
                await SavePatientsAsync(entities, ct);
                break;
            case "Formula":
                await SaveFormulasAsync(entities, ct);
                break;
            case "MedicalCase":
                await SaveMedicalCasesAsync(entities, ct);
                break;
            default:
                throw new ArgumentException($"不支持的实体类型: {entityType}");
        }
    }

    private async Task SaveHerbsAsync(List<string> entities, CancellationToken ct)
    {
        foreach (var jsonString in entities)
        {
            var herb = JsonSerializer.Deserialize<Herb>(jsonString, JsonOptions);
            if (herb == null) continue;

            var existing = await _context.Herbs
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(h => h.Id == herb.Id, ct);

            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(herb);
            }
            else
            {
                _context.Herbs.Add(herb);
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SavePatientsAsync(List<string> entities, CancellationToken ct)
    {
        foreach (var jsonString in entities)
        {
            var patient = JsonSerializer.Deserialize<Patient>(jsonString, JsonOptions);
            if (patient == null) continue;

            var existing = await _context.Patients
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == patient.Id, ct);

            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(patient);
            }
            else
            {
                _context.Patients.Add(patient);
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SaveFormulasAsync(List<string> entities, CancellationToken ct)
    {
        foreach (var jsonString in entities)
        {
            var formula = JsonSerializer.Deserialize<Formula>(jsonString, JsonOptions);
            if (formula == null) continue;

            var existing = await _context.Formulas
                .Include(f => f.Herbs)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.Id == formula.Id, ct);

            if (existing != null)
            {
                // 更新基本属性
                _context.Entry(existing).CurrentValues.SetValues(formula);

                // 更新子项
                existing.Herbs.Clear();
                if (formula.Herbs != null)
                {
                    foreach (var herb in formula.Herbs)
                    {
                        existing.Herbs.Add(herb);
                    }
                }
            }
            else
            {
                _context.Formulas.Add(formula);
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SaveMedicalCasesAsync(List<string> entities, CancellationToken ct)
    {
        foreach (var jsonString in entities)
        {
            var medicalCase = JsonSerializer.Deserialize<MedicalCase>(jsonString, JsonOptions);
            if (medicalCase == null) continue;

            var existing = await _context.MedicalCases
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p.Items)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(mc => mc.Id == medicalCase.Id, ct);

            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(medicalCase);

                if (medicalCase.Consultation != null)
                {
                    if (existing.Consultation != null)
                        _context.Entry(existing.Consultation).CurrentValues.SetValues(medicalCase.Consultation);
                    else
                        existing.Consultation = medicalCase.Consultation;
                }

                if (medicalCase.Prescription != null)
                {
                    if (existing.Prescription != null)
                    {
                        _context.Entry(existing.Prescription).CurrentValues.SetValues(medicalCase.Prescription);
                        if (existing.Prescription.Items != null)
                            existing.Prescription.Items.Clear();
                        if (medicalCase.Prescription.Items != null)
                        {
                            foreach (var item in medicalCase.Prescription.Items)
                                existing.Prescription.Items.Add(item);
                        }
                    }
                    else
                    {
                        existing.Prescription = medicalCase.Prescription;
                    }
                }
            }
            else
            {
                _context.MedicalCases.Add(medicalCase);
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    #endregion

    /// <summary>
    /// 本地元数据（用于内部比对）
    /// </summary>
    private class LocalMetadata
    {
        public Guid EntityId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
    }
}
