using System.Text.Json;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Sync.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Sync;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Sync.Services;

/// <summary>
/// Sync service implementation - handles bidirectional sync for Herb/Patient/Formula/MedicalCase
/// </summary>
public class SyncService : ISyncService
{
    private readonly ISyncRepository _syncRepository;
    private readonly IHerbCrossModuleService _herbCrossModule;
    private readonly IPatientCrossModuleService _patientCrossModule;
    private readonly ILogger<SyncService> _logger;

    private static readonly IReadOnlyList<string> SupportedTypes = new[] { "Herb", "Patient", "Formula", "MedicalCase" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SyncService(
        ISyncRepository syncRepository,
        IHerbCrossModuleService herbCrossModule,
        IPatientCrossModuleService patientCrossModule,
        ILogger<SyncService> logger)
    {
        _syncRepository = syncRepository;
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
            "MedicalCase" => await GetMedicalCaseMetadataAsync(),
            _ => new List<SyncMetadataDto>()
        };

        _logger.LogInformation("Get {EntityType} metadata completed, {Count} records", entityType, metadata.Count);
        return ServiceResult<List<SyncMetadataDto>>.Success(metadata);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<SyncCompareResultDto>> CompareAsync(SyncCompareInputDto input)
    {
        if (!ValidateEntityType(input.EntityType, out var errorMessage))
        {
            return ServiceResult<SyncCompareResultDto>.Failure(errorMessage!);
        }

        var serverMetadataResult = await GetMetadataAsync(input.EntityType);
        if (!serverMetadataResult.IsSuccess)
        {
            return ServiceResult<SyncCompareResultDto>.Failure(serverMetadataResult.ErrorMessage!);
        }

        var serverMetadata = serverMetadataResult.Data;
        var serverDict = serverMetadata!.ToDictionary(m => m.EntityId);
        var localDict = input.LocalEntities.ToDictionary(e => e.EntityId);

        var diffs = new List<SyncDiffDto>();

        foreach (var server in serverMetadata!)
        {
            if (localDict.TryGetValue(server.EntityId, out var local))
            {
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
            }
            else
            {
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
            "Compare {EntityType} completed: LocalOnly={LocalOnly}, ServerOnly={ServerOnly}, Modified={Modified}",
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
            using var doc = JsonDocument.Parse(entityJsonString);
            var entityJson = doc.RootElement.Clone();

            var result = input.EntityType switch
            {
                "Herb" => await UploadHerbAsync(entityJson, input.OverwriteConflicts),
                "Patient" => await UploadPatientAsync(entityJson, input.OverwriteConflicts),
                "Formula" => await UploadFormulaAsync(entityJson, input.OverwriteConflicts),
                "MedicalCase" => await UploadMedicalCaseAsync(entityJson, input.OverwriteConflicts),
                _ => new SyncUploadItemResult { Success = false, ErrorMessage = "Unsupported entity type" }
            };

            results.Add(result);
            if (result.Success) successCount++;
            else if (result.IsConflict) conflictCount++;
            else errorCount++;
        }

        await _syncRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Upload {EntityType} completed: Success={Success}, Conflict={Conflict}, Error={Error}",
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
                "MedicalCase" => await GetMedicalCaseJsonStringAsync(entityId),
                _ => null
            };

            if (!string.IsNullOrEmpty(entityJson))
            {
                entities.Add(entityJson);
            }
        }

        _logger.LogInformation("Download {EntityType} completed, {Count} records", input.EntityType, entities.Count);

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
                "Formula" => (true, (string?)null),
                "MedicalCase" => (true, (string?)null),
                _ => (false, "Unsupported entity type")
            };

            if (canDelete.Item1)
            {
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
                        Reason = "Entity not found or already deleted"
                    });
                }
            }
            else
            {
                rejected.Add(new SyncDeleteRejectedItem
                {
                    EntityId = entityId,
                    Reason = canDelete.Item2 ?? "Has reference data, cannot delete"
                });
            }
        }

        await _syncRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Delete {EntityType} completed: Success={Success}, Rejected={Rejected}",
            input.EntityType, successIds.Count, rejected.Count);

        return ServiceResult<SyncDeleteResultDto>.Success(new SyncDeleteResultDto
        {
            Success = successIds,
            Rejected = rejected
        });
    }

    #region Private - Metadata

    private async Task<List<SyncMetadataDto>> GetHerbMetadataAsync()
    {
        var herbs = await _syncRepository.GetAllHerbsIncludingDeletedAsync();
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
        var patients = await _syncRepository.GetAllPatientsIncludingDeletedAsync();
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
        var formulas = await _syncRepository.GetAllFormulasWithHerbsIncludingDeletedAsync();
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

    private async Task<List<SyncMetadataDto>> GetMedicalCaseMetadataAsync()
    {
        var cases = await _syncRepository.GetAllMedicalCasesIncludingDeletedAsync();
        return cases.Select(mc => new SyncMetadataDto
        {
            EntityId = mc.Id,
            Checksum = ChecksumHelper.ComputeMedicalCaseChecksum(mc),
            LastModifiedAt = mc.UpdatedAt ?? mc.CreatedAt,
            IsDeleted = mc.IsDeleted,
            EntityName = mc.CaseNumber ?? mc.Id.ToString(),
            EntityType = "MedicalCase"
        }).ToList();
    }

    #endregion

    #region Private - Upload

    private async Task<SyncUploadItemResult> UploadHerbAsync(JsonElement json, bool overwriteConflicts)
    {
        try
        {
            var herb = json.Deserialize<Herb>(JsonOptions);
            if (herb == null)
                return new SyncUploadItemResult { Success = false, ErrorMessage = "JSON deserialization failed" };

            var existing = await _syncRepository.FindHerbAsync(herb.Id);
            if (existing != null)
            {
                if (!overwriteConflicts)
                    return new SyncUploadItemResult { EntityId = herb.Id, Success = false, IsConflict = true, ErrorMessage = "Server already has this data" };
                _syncRepository.UpdateHerbValues(existing, herb);
            }
            else
            {
                _syncRepository.AddHerb(herb);
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
                return new SyncUploadItemResult { Success = false, ErrorMessage = "JSON deserialization failed" };

            var existing = await _syncRepository.FindPatientAsync(patient.Id);
            if (existing != null)
            {
                if (!overwriteConflicts)
                    return new SyncUploadItemResult { EntityId = patient.Id, Success = false, IsConflict = true, ErrorMessage = "Server already has this data" };
                _syncRepository.UpdatePatientValues(existing, patient);
            }
            else
            {
                _syncRepository.AddPatient(patient);
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
                return new SyncUploadItemResult { Success = false, ErrorMessage = "JSON deserialization failed" };

            var existing = await _syncRepository.FindFormulaWithHerbsAsync(formula.Id);
            if (existing != null)
            {
                if (!overwriteConflicts)
                    return new SyncUploadItemResult { EntityId = formula.Id, Success = false, IsConflict = true, ErrorMessage = "Server already has this data" };
                _syncRepository.RemoveFormulaHerbs(existing.Herbs);
                _syncRepository.UpdateFormulaValues(existing, formula);
                if (formula.Herbs != null)
                {
                    foreach (var herb in formula.Herbs)
                    {
                        herb.FormulaId = formula.Id;
                        _syncRepository.AddFormulaHerbItem(herb);
                    }
                }
            }
            else
            {
                _syncRepository.AddFormula(formula);
            }
            return new SyncUploadItemResult { EntityId = formula.Id, Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Sync.UploadFormula -> Failed");
            return new SyncUploadItemResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<SyncUploadItemResult> UploadMedicalCaseAsync(JsonElement json, bool overwriteConflicts)
    {
        try
        {
            var medicalCase = json.Deserialize<MedicalCase>(JsonOptions);
            if (medicalCase == null)
                return new SyncUploadItemResult { Success = false, ErrorMessage = "JSON deserialization failed" };

            var existing = await _syncRepository.FindMedicalCaseWithIncludesAsync(medicalCase.Id);
            if (existing != null)
            {
                if (!overwriteConflicts)
                    return new SyncUploadItemResult { EntityId = medicalCase.Id, Success = false, IsConflict = true, ErrorMessage = "Server already has this data" };
                _syncRepository.UpdateMedicalCaseValues(existing, medicalCase);
            }
            else
            {
                _syncRepository.AddMedicalCase(medicalCase);
            }
            return new SyncUploadItemResult { EntityId = medicalCase.Id, Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Sync.UploadMedicalCase -> Failed");
            return new SyncUploadItemResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    #endregion

    #region Private - Download

    private async Task<string?> GetHerbJsonStringAsync(Guid id)
    {
        var herb = await _syncRepository.GetHerbByIdNoTrackingAsync(id);
        return herb == null ? null : JsonSerializer.Serialize(herb, JsonOptions);
    }

    private async Task<string?> GetPatientJsonStringAsync(Guid id)
    {
        var patient = await _syncRepository.GetPatientByIdNoTrackingAsync(id);
        return patient == null ? null : JsonSerializer.Serialize(patient, JsonOptions);
    }

    private async Task<string?> GetFormulaJsonStringAsync(Guid id)
    {
        var formula = await _syncRepository.GetFormulaWithHerbsByIdNoTrackingAsync(id);
        return formula == null ? null : JsonSerializer.Serialize(formula, JsonOptions);
    }

    private async Task<string?> GetMedicalCaseJsonStringAsync(Guid id)
    {
        var medicalCase = await _syncRepository.GetMedicalCaseWithIncludesByIdNoTrackingAsync(id);
        return medicalCase == null ? null : JsonSerializer.Serialize(medicalCase, JsonOptions);
    }

    #endregion

    #region Private - Delete checks

    private async Task<(bool canDelete, string? reason)> CanDeleteHerbAsync(Guid herbId)
    {
        var result = await _herbCrossModule.CheckHerbReferenceAsync(herbId);
        if (result.HasReferences)
            return (false, $"Herb referenced by {result.ReferenceCount} prescriptions, disable instead");
        return (true, null);
    }

    private async Task<(bool canDelete, string? reason)> CanDeletePatientAsync(Guid patientId)
    {
        var result = await _patientCrossModule.CheckPatientReferenceAsync(patientId);
        if (result.HasReferences)
            return (false, $"Patient has {result.ReferenceCount} medical case records, disable instead");
        return (true, null);
    }

    private async Task<bool> SoftDeleteEntityAsync(string entityType, Guid entityId)
    {
        return entityType switch
        {
            "Herb" => await _syncRepository.SoftDeleteHerbAsync(entityId),
            "Patient" => await _syncRepository.SoftDeletePatientAsync(entityId),
            "Formula" => await _syncRepository.SoftDeleteFormulaAsync(entityId),
            "MedicalCase" => await _syncRepository.SoftDeleteMedicalCaseAsync(entityId),
            _ => false
        };
    }

    #endregion

    #region Helpers

    private bool ValidateEntityType(string entityType, out string? errorMessage)
    {
        if (!SupportedTypes.Contains(entityType))
        {
            _logger.LogWarning("Unsupported entity type: {EntityType}", entityType);
            errorMessage = $"Unsupported entity type: {entityType}, supported: {string.Join(", ", SupportedTypes)}";
            return false;
        }
        errorMessage = null;
        return true;
    }

    #endregion
}
