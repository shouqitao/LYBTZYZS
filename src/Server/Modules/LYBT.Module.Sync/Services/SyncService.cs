using System.Text.Json;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Sync.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Sync.Services;

/// <summary>
/// 同步服务实现 - 处理 Herb/Patient/Formula 的双向同步
/// OpenSpec: implement-data-sync
/// </summary>
public class SyncService : ISyncService
{
    private readonly AppDbContext _dbContext;
    private readonly IHerbCrossModuleService _herbCrossModule;
    private readonly IPatientCrossModuleService _patientCrossModule;
    private readonly ILogger<SyncService> _logger;

    private static readonly IReadOnlyList<string> SupportedTypes = new[] { "Herb", "Patient", "Formula" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SyncService(
        AppDbContext dbContext,
        IHerbCrossModuleService herbCrossModule,
        IPatientCrossModuleService patientCrossModule,
        ILogger<SyncService> logger)
    {
        _dbContext = dbContext;
        _herbCrossModule = herbCrossModule;
        _patientCrossModule = patientCrossModule;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedEntityTypes() => SupportedTypes;

    /// <inheritdoc />
    public async Task<ServiceResult<List<SyncMetadataDto>>> GetMetadataAsync(string entityType)
    {
        if (!ValidateEntityType(entityType, out var errorMessage))
        {
            return ServiceResult<List<SyncMetadataDto>>.Failure(errorMessage!);
        }

        var metadata = entityType switch
        {
            "Herb" => await GetHerbMetadataAsync(),
            "Patient" => await GetPatientMetadataAsync(),
            "Formula" => await GetFormulaMetadataAsync(),
            _ => new List<SyncMetadataDto>()
        };

        _logger.LogInformation("获取 {EntityType} 元数据完成，共 {Count} 条", entityType, metadata.Count);
        return ServiceResult<List<SyncMetadataDto>>.Success(metadata);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<SyncCompareResultDto>> CompareAsync(SyncCompareInputDto input)
    {
        if (!ValidateEntityType(input.EntityType, out var errorMessage))
        {
            return ServiceResult<SyncCompareResultDto>.Failure(errorMessage!);
        }

        // 获取服务器端所有元数据
        var serverMetadataResult = await GetMetadataAsync(input.EntityType);
        if (!serverMetadataResult.IsSuccess)
        {
            return ServiceResult<SyncCompareResultDto>.Failure(serverMetadataResult.ErrorMessage!);
        }

        var serverMetadata = serverMetadataResult.Data!;
        var serverDict = serverMetadata.ToDictionary(m => m.EntityId);
        var localDict = input.LocalEntities.ToDictionary(e => e.EntityId);

        var diffs = new List<SyncDiffDto>();

        // 检查服务器端实体
        foreach (var server in serverMetadata)
        {
            if (localDict.TryGetValue(server.EntityId, out var local))
            {
                // 双方都有，比较 Checksum
                if (server.Checksum != local.Checksum)
                {
                    diffs.Add(new SyncDiffDto
                    {
                        EntityType = input.EntityType,
                        EntityId = server.EntityId,
                        DiffType = SyncDiffType.Modified,
                        LocalChecksum = local.Checksum,
                        ServerChecksum = server.Checksum,
                        LocalChangedAt = local.LastModifiedAt,
                        ServerChangedAt = server.LastModifiedAt
                    });
                }
                // Checksum 相同则不加入差异列表
            }
            else
            {
                // 仅服务器有
                diffs.Add(new SyncDiffDto
                {
                    EntityType = input.EntityType,
                    EntityId = server.EntityId,
                    DiffType = SyncDiffType.ServerOnly,
                    ServerChecksum = server.Checksum,
                    ServerChangedAt = server.LastModifiedAt
                });
            }
        }

        // 检查仅本地有的实体
        foreach (var local in input.LocalEntities)
        {
            if (!serverDict.ContainsKey(local.EntityId))
            {
                diffs.Add(new SyncDiffDto
                {
                    EntityType = input.EntityType,
                    EntityId = local.EntityId,
                    DiffType = SyncDiffType.LocalOnly,
                    LocalChecksum = local.Checksum,
                    LocalChangedAt = local.LastModifiedAt
                });
            }
        }

        var result = new SyncCompareResultDto
        {
            Diffs = diffs,
            ServerTotalCount = serverMetadata.Count,
            ComparedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "比对 {EntityType} 完成: LocalOnly={LocalOnly}, ServerOnly={ServerOnly}, Modified={Modified}",
            input.EntityType,
            diffs.Count(d => d.DiffType == SyncDiffType.LocalOnly),
            diffs.Count(d => d.DiffType == SyncDiffType.ServerOnly),
            diffs.Count(d => d.DiffType == SyncDiffType.Modified));

        return ServiceResult<SyncCompareResultDto>.Success(result);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<SyncUploadResultDto>> UploadAsync(SyncUploadInputDto input)
    {
        if (!ValidateEntityType(input.EntityType, out var errorMessage))
        {
            return ServiceResult<SyncUploadResultDto>.Failure(errorMessage!);
        }

        var results = new List<SyncUploadItemResult>();
        var successCount = 0;
        var conflictCount = 0;
        var errorCount = 0;

        foreach (var entityJsonString in input.Entities)
        {
            // Parse string to JsonElement for deserialization
            using var doc = JsonDocument.Parse(entityJsonString);
            var entityJson = doc.RootElement.Clone();

            var result = input.EntityType switch
            {
                "Herb" => await UploadHerbAsync(entityJson, input.OverwriteConflicts),
                "Patient" => await UploadPatientAsync(entityJson, input.OverwriteConflicts),
                "Formula" => await UploadFormulaAsync(entityJson, input.OverwriteConflicts),
                _ => new SyncUploadItemResult { Success = false, ErrorMessage = "不支持的实体类型" }
            };

            results.Add(result);
            if (result.Success) successCount++;
            else if (result.IsConflict) conflictCount++;
            else errorCount++;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "上传 {EntityType} 完成: Success={Success}, Conflict={Conflict}, Error={Error}",
            input.EntityType, successCount, conflictCount, errorCount);

        return ServiceResult<SyncUploadResultDto>.Success(new SyncUploadResultDto
        {
            SuccessCount = successCount,
            ConflictCount = conflictCount,
            ErrorCount = errorCount,
            Results = results
        });
    }

    /// <inheritdoc />
    public async Task<ServiceResult<SyncDownloadResultDto>> DownloadAsync(SyncDownloadInputDto input)
    {
        if (!ValidateEntityType(input.EntityType, out var errorMessage))
        {
            return ServiceResult<SyncDownloadResultDto>.Failure(errorMessage!);
        }

        var entities = new List<string>();

        foreach (var entityId in input.EntityIds)
        {
            var entityJson = input.EntityType switch
            {
                "Herb" => await GetHerbJsonStringAsync(entityId),
                "Patient" => await GetPatientJsonStringAsync(entityId),
                "Formula" => await GetFormulaJsonStringAsync(entityId),
                _ => null
            };

            if (!string.IsNullOrEmpty(entityJson))
            {
                entities.Add(entityJson);
            }
        }

        _logger.LogInformation("下载 {EntityType} 完成，共 {Count} 条", input.EntityType, entities.Count);

        return ServiceResult<SyncDownloadResultDto>.Success(new SyncDownloadResultDto
        {
            EntityType = input.EntityType,
            Entities = entities,
            Count = entities.Count
        });
    }

    /// <inheritdoc />
    public async Task<ServiceResult<SyncDeleteResultDto>> DeleteAsync(SyncDeleteInputDto input)
    {
        if (!ValidateEntityType(input.EntityType, out var errorMessage))
        {
            return ServiceResult<SyncDeleteResultDto>.Failure(errorMessage!);
        }

        var successIds = new List<Guid>();
        var rejected = new List<SyncDeleteRejectedItem>();

        foreach (var entityId in input.EntityIds)
        {
            var canDelete = input.EntityType switch
            {
                "Herb" => await CanDeleteHerbAsync(entityId),
                "Patient" => await CanDeletePatientAsync(entityId),
                "Formula" => (true, (string?)null), // Formula 无引用检查
                _ => (false, "不支持的实体类型")
            };

            if (canDelete.Item1)
            {
                // 执行软删除
                var deleted = await SoftDeleteEntityAsync(input.EntityType, entityId);
                if (deleted)
                {
                    successIds.Add(entityId);
                }
                else
                {
                    rejected.Add(new SyncDeleteRejectedItem
                    {
                        EntityId = entityId,
                        Reason = "实体不存在或已删除"
                    });
                }
            }
            else
            {
                rejected.Add(new SyncDeleteRejectedItem
                {
                    EntityId = entityId,
                    Reason = canDelete.Item2 ?? "有引用数据，无法删除"
                });
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "删除 {EntityType} 完成: Success={Success}, Rejected={Rejected}",
            input.EntityType, successIds.Count, rejected.Count);

        return ServiceResult<SyncDeleteResultDto>.Success(new SyncDeleteResultDto
        {
            Success = successIds,
            Rejected = rejected
        });
    }

    #region 私有方法 - 元数据获取

    private async Task<List<SyncMetadataDto>> GetHerbMetadataAsync()
    {
        // T5-P2-40: IgnoreQueryFilters 确保软删除记录参与 Checksum 比对
        var herbs = await _dbContext.Herbs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

        return herbs.Select(h => new SyncMetadataDto
        {
            EntityId = h.Id,
            Checksum = ChecksumHelper.ComputeHerbChecksum(h),
            LastModifiedAt = h.UpdatedAt ?? h.CreatedAt,
            IsDeleted = h.IsDeleted,
            EntityName = h.Name,
            EntityType = "Herb"
        }).ToList();
    }

    private async Task<List<SyncMetadataDto>> GetPatientMetadataAsync()
    {
        // T5-P2-40: IgnoreQueryFilters 确保软删除记录参与 Checksum 比对
        var patients = await _dbContext.Patients
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

        return patients.Select(p => new SyncMetadataDto
        {
            EntityId = p.Id,
            Checksum = ChecksumHelper.ComputePatientChecksum(p),
            LastModifiedAt = p.UpdatedAt ?? p.CreatedAt,
            IsDeleted = p.IsDeleted,
            EntityName = p.Name,
            EntityType = "Patient"
        }).ToList();
    }

    private async Task<List<SyncMetadataDto>> GetFormulaMetadataAsync()
    {
        // T5-P2-40: IgnoreQueryFilters 确保软删除记录参与 Checksum 比对
        var formulas = await _dbContext.Formulas
            .Include(f => f.Herbs)
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

        return formulas.Select(f => new SyncMetadataDto
        {
            EntityId = f.Id,
            Checksum = ChecksumHelper.ComputeFormulaChecksum(f),
            LastModifiedAt = f.UpdatedAt ?? f.CreatedAt,
            IsDeleted = f.IsDeleted,
            EntityName = f.Name,
            EntityType = "Formula"
        }).ToList();
    }

    #endregion

    #region 私有方法 - 上传处理

    private async Task<SyncUploadItemResult> UploadHerbAsync(JsonElement json, bool overwriteConflicts)
    {
        try
        {
            var herb = json.Deserialize<Herb>(JsonOptions);
            if (herb == null)
            {
                return new SyncUploadItemResult { Success = false, ErrorMessage = "JSON 反序列化失败" };
            }

            var existing = await _dbContext.Herbs.FindAsync(herb.Id);
            if (existing != null)
            {
                if (!overwriteConflicts)
                {
                    return new SyncUploadItemResult
                    {
                        EntityId = herb.Id,
                        Success = false,
                        IsConflict = true,
                        ErrorMessage = "服务器已存在该数据"
                    };
                }

                // 覆盖更新
                _dbContext.Entry(existing).CurrentValues.SetValues(herb);
            }
            else
            {
                _dbContext.Herbs.Add(herb);
            }

            return new SyncUploadItemResult { EntityId = herb.Id, Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Sync.UploadHerb -> Failed");
            return new SyncUploadItemResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<SyncUploadItemResult> UploadPatientAsync(JsonElement json, bool overwriteConflicts)
    {
        try
        {
            var patient = json.Deserialize<Patient>(JsonOptions);
            if (patient == null)
            {
                return new SyncUploadItemResult { Success = false, ErrorMessage = "JSON 反序列化失败" };
            }

            var existing = await _dbContext.Patients.FindAsync(patient.Id);
            if (existing != null)
            {
                if (!overwriteConflicts)
                {
                    return new SyncUploadItemResult
                    {
                        EntityId = patient.Id,
                        Success = false,
                        IsConflict = true,
                        ErrorMessage = "服务器已存在该数据"
                    };
                }

                _dbContext.Entry(existing).CurrentValues.SetValues(patient);
            }
            else
            {
                _dbContext.Patients.Add(patient);
            }

            return new SyncUploadItemResult { EntityId = patient.Id, Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Sync.UploadPatient -> Failed");
            return new SyncUploadItemResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<SyncUploadItemResult> UploadFormulaAsync(JsonElement json, bool overwriteConflicts)
    {
        try
        {
            var formula = json.Deserialize<Formula>(JsonOptions);
            if (formula == null)
            {
                return new SyncUploadItemResult { Success = false, ErrorMessage = "JSON 反序列化失败" };
            }

            var existing = await _dbContext.Formulas
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == formula.Id);

            if (existing != null)
            {
                if (!overwriteConflicts)
                {
                    return new SyncUploadItemResult
                    {
                        EntityId = formula.Id,
                        Success = false,
                        IsConflict = true,
                        ErrorMessage = "服务器已存在该数据"
                    };
                }

                // 删除旧的 Herbs 并添加新的
                _dbContext.RemoveRange(existing.Herbs);
                _dbContext.Entry(existing).CurrentValues.SetValues(formula);
                if (formula.Herbs != null)
                {
                    foreach (var herb in formula.Herbs)
                    {
                        herb.FormulaId = formula.Id;
                        _dbContext.Add(herb);
                    }
                }
            }
            else
            {
                _dbContext.Formulas.Add(formula);
            }

            return new SyncUploadItemResult { EntityId = formula.Id, Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Sync.UploadFormula -> Failed");
            return new SyncUploadItemResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    #endregion

    #region 私有方法 - 下载处理

    private async Task<string?> GetHerbJsonStringAsync(Guid id)
    {
        var herb = await _dbContext.Herbs.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
        if (herb == null) return null;

        return JsonSerializer.Serialize(herb, JsonOptions);
    }

    private async Task<string?> GetPatientJsonStringAsync(Guid id)
    {
        var patient = await _dbContext.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null) return null;

        return JsonSerializer.Serialize(patient, JsonOptions);
    }

    private async Task<string?> GetFormulaJsonStringAsync(Guid id)
    {
        var formula = await _dbContext.Formulas
            .Include(f => f.Herbs)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
        if (formula == null) return null;

        return JsonSerializer.Serialize(formula, JsonOptions);
    }

    #endregion

    #region 私有方法 - 删除检查

    private async Task<(bool canDelete, string? reason)> CanDeleteHerbAsync(Guid herbId)
    {
        var result = await _herbCrossModule.CheckHerbReferenceAsync(herbId);
        if (result.HasReferences)
        {
            return (false, $"药材被 {result.ReferenceCount} 个处方引用，请先禁用");
        }

        return (true, null);
    }

    private async Task<(bool canDelete, string? reason)> CanDeletePatientAsync(Guid patientId)
    {
        var result = await _patientCrossModule.CheckPatientReferenceAsync(patientId);
        if (result.HasReferences)
        {
            return (false, $"患者有 {result.ReferenceCount} 条医案记录，请先禁用");
        }

        return (true, null);
    }

    private async Task<bool> SoftDeleteEntityAsync(string entityType, Guid entityId)
    {
        switch (entityType)
        {
            case "Herb":
                var herb = await _dbContext.Herbs.FindAsync(entityId);
                if (herb == null || herb.IsDeleted) return false;
                herb.IsDeleted = true;
                return true;

            case "Patient":
                var patient = await _dbContext.Patients.FindAsync(entityId);
                if (patient == null || patient.IsDeleted) return false;
                patient.IsDeleted = true;
                return true;

            case "Formula":
                var formula = await _dbContext.Formulas.FindAsync(entityId);
                if (formula == null || formula.IsDeleted) return false;
                formula.IsDeleted = true;
                return true;

            default:
                return false;
        }
    }

    #endregion

    #region 辅助方法

    private bool ValidateEntityType(string entityType, out string? errorMessage)
    {
        if (!SupportedTypes.Contains(entityType))
        {
            _logger.LogWarning("不支持的实体类型: {EntityType}", entityType);
            errorMessage = $"不支持的实体类型: {entityType}，支持的类型: {string.Join(", ", SupportedTypes)}";
            return false;
        }

        errorMessage = null;
        return true;
    }

    #endregion
}
