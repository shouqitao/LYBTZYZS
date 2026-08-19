using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Reports;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.MedicalCase.Reports.Services;

/// <summary>
/// 报表Service实现 - 封装报表域（对齐 VM→Service→Repository→IApiClient 分层，P0-2）
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _repository;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IReportRepository repository, ILogger<ReportService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandResult<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        try
        {
            var result = await _repository.GetDailyIncomeAsync(startDate, endDate);
            return result.Success && result.Data != null
                ? CommandResult<DailyIncomeDto>.Succeeded(result.Data)
                : CommandResult<DailyIncomeDto>.Failed(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Report.GetDailyIncome failed - Start={Start}, End={End}", startDate, endDate);
            return CommandResult<DailyIncomeDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载收入报表", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<CommandResult<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        try
        {
            var result = await _repository.GetDailyConsultationsAsync(startDate, endDate);
            return result.Success && result.Data != null
                ? CommandResult<DailyConsultationDto>.Succeeded(result.Data)
                : CommandResult<DailyConsultationDto>.Failed(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Report.GetDailyConsultations failed - Start={Start}, End={End}", startDate, endDate);
            return CommandResult<DailyConsultationDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载问诊报表", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<CommandResult<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        try
        {
            var result = await _repository.GetDailyHerbUsageAsync(startDate, endDate);
            return result.Success && result.Data != null
                ? CommandResult<DailyHerbUsageDto>.Succeeded(result.Data)
                : CommandResult<DailyHerbUsageDto>.Failed(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Report.GetDailyHerbUsage failed - Start={Start}, End={End}", startDate, endDate);
            return CommandResult<DailyHerbUsageDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载药材用量报表", ex));
        }
    }
}
