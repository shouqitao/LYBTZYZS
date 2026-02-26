using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Remote;

/// <summary>
/// 远程医案数据源 - 通过 API 访问服务端
/// </summary>
public class RemoteMedicalCaseDataSource : IMedicalCaseDataSource
{
    private readonly IMedicalCaseApi _api;
    private readonly ILogger<RemoteMedicalCaseDataSource> _logger;
    private readonly MedicalCaseListToDetailMapper _listMapper = new();

    public RemoteMedicalCaseDataSource(IMedicalCaseApi api, ILogger<RemoteMedicalCaseDataSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.GetById - Id={Id}", id);

        try
        {
            var response = await _api.GetMedicalCaseByIdAsync(id);
            if (response.Data == null)
            {
                _logger.LogWarning("[RemoteDataSource] MedicalCase.GetById - NotFound: {Id}", id);
                return null;
            }
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Create - PatientId={PatientId}", input.PatientId);

        try
        {
            var response = await _api.CreateMedicalCaseAsync(input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "创建医案失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Create failed");
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Update - Id={Id}", input.Id);

        try
        {
            var response = await _api.SaveAsync(input.Id!.Value, input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "更新医案失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Update failed - Id={Id}", input.Id);
            throw;
        }
    }

    public async Task<(List<MedicalCaseDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.GetPaged - Page={Page}", page);

        try
        {
            var response = await _api.GetMedicalCasesAsync(page, pageSize, keyword);
            if (response.Data == null)
            {
                return (new List<MedicalCaseDetailDto>(), 0);
            }

            var items = response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.GetPaged failed");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Delete - Id={Id}", id);

        try
        {
            var response = await _api.DeleteMedicalCaseAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<MedicalCaseDetailDto> SaveAsync(MedicalCaseInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Save - Id={Id}", input.Id);

        try
        {
            var response = await _api.SaveAsync(input.Id!.Value, input);

            if (!response.Success || response.Data == null)
            {
                throw new InvalidOperationException(response.Message ?? "保存医案失败");
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Save failed - Id={Id}", input.Id);
            throw;
        }
    }

    public async Task<bool> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Complete - Id={Id}", id);

        try
        {
            var response = await _api.CloseCaseAsync(id);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Complete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<bool> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.Cancel - Id={Id}", id);

        try
        {
            var request = reason != null ? new CancelMedicalCaseRequestDto { Reason = reason } : null;
            var response = await _api.CancelMedicalCaseAsync(id, request);
            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Cancel failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<MedicalCaseDetailDto?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.GetWithDetails - Id={Id}", id);

        // GetMedicalCaseByIdAsync 已包含完整详情
        return await GetByIdAsync(id, ct);
    }

    public async Task<(List<MedicalCaseDetailDto> Items, int Total)> QueryAsync(
        Guid? patientId = null,
        Guid? userId = null,
        MedicalCaseStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.Query - PatientId={PatientId}, Page={Page}", patientId, page);

        try
        {
            var queryType = patientId.HasValue ? MedicalCaseQueryType.ByPatient : MedicalCaseQueryType.All;
            var response = await _api.QueryMedicalCasesAsync(
                queryType: queryType,
                patientId: patientId,
                doctorId: userId,
                pageIndex: page,
                pageSize: pageSize);

            if (response.Data == null)
            {
                return (new List<MedicalCaseDetailDto>(), 0);
            }

            var items = response.Data.Items.Select(_listMapper.ToDetailDto).ToList();

            // 客户端过滤状态和日期（如果服务端不支持）
            if (status.HasValue)
            {
                items = items.Where(m => m.CaseStatus == status.Value).ToList();
            }
            if (startDate.HasValue)
            {
                items = items.Where(m => m.CreatedAt >= startDate.Value).ToList();
            }
            if (endDate.HasValue)
            {
                items = items.Where(m => m.CreatedAt <= endDate.Value).ToList();
            }

            return (items, response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.Query failed");
            throw;
        }
    }

    public async Task<List<MedicalCaseDetailDto>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
    {
        _logger.LogDebug("[RemoteDataSource] MedicalCase.GetByPatientId - PatientId={PatientId}", patientId);

        try
        {
            var response = await _api.QueryMedicalCasesAsync(
                queryType: MedicalCaseQueryType.ByPatient,
                patientId: patientId,
                pageSize: 1000);

            if (response.Data == null)
            {
                return new List<MedicalCaseDetailDto>();
            }

            return response.Data.Items.Select(_listMapper.ToDetailDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.GetByPatientId failed");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> AddPrintLogAsync(
        Guid medicalCaseId,
        bool isSuccess,
        PrintType printType = PrintType.Prescription,
        string? printerName = null,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.AddPrintLog - MedicalCaseId={MedicalCaseId}, IsSuccess={IsSuccess}",
            medicalCaseId, isSuccess);

        try
        {
            var request = new PrintLogInputDto
            {
                PrintType = printType,
                IsSuccess = isSuccess,
                PrinterName = printerName,
                ErrorMessage = errorMessage
            };

            var response = await _api.AddPrintLogAsync(medicalCaseId, request);
            return response.Success;
        }
        catch (Exception ex)
        {
            // T4-S5-01: 打印日志记录失败不应阻塞打印操作
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.AddPrintLog failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return false;
        }
    }

    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[RemoteDataSource] MedicalCase.BatchDelete - Count={Count}", ids.Count);

        try
        {
            var response = await _api.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    FailureCount = ids.Count,
                    IsSuccess = false,
                    Message = response.Message ?? "批量删除失败"
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RemoteDataSource] MedicalCase.BatchDelete failed");
            return new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                FailureCount = ids.Count,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }
}

/// <summary>
/// MedicalCaseListDto -> MedicalCaseDetailDto 映射器 (仅限 DTO 间转换, 无 Entity 依赖)
/// </summary>
[Mapper]
internal partial class MedicalCaseListToDetailMapper
{
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.CreatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.ConsultationId))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PrescriptionId))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Remark))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PresentIllness))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PrintVersion))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.LastPrintedAt))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PrintCount))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.IsPrinted))]
    public partial MedicalCaseDetailDto ToDetailDto(MedicalCaseListDto listDto);
}
