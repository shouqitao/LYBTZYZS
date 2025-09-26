using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者业务服务 - 简化版，只包含基础CRUD
/// </summary>
public class PatientBusinessService : IPatientBusinessService
{
    private readonly ILogger<PatientBusinessService> _logger;
    private readonly IPatientApi _patientApi;
    private readonly IExceptionHandler _exceptionHandler;

    public PatientBusinessService(
        ILogger<PatientBusinessService> logger,
        IPatientApi patientApi,
        IExceptionHandler exceptionHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _patientApi = patientApi ?? throw new ArgumentNullException(nameof(patientApi));
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
    }

    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        return await _exceptionHandler.HandleException<PatientDto>(
            async (ct) =>
            {
                _logger.LogInformation("创建患者档案: {PatientName}", createDto.Name);

                var response = await _patientApi.CreatePatientAsync(createDto).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    _logger.LogInformation("患者档案创建成功: {PatientName}", response.Content.Name);
                    return ServiceResult<PatientDto>.Success(response.Content, "患者档案创建成功");
                }

                return ServiceResult<PatientDto>.Failure("创建患者档案失败");
            },
            nameof(CreateAsync), $"创建患者档案: {createDto.Name}", cancellationToken);
    }

    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        return await _exceptionHandler.HandleException<PatientDto>(
            async (ct) =>
            {
                _logger.LogInformation("更新患者档案: {PatientId}", id);

                var response = await _patientApi.UpdatePatientAsync(id, updateDto).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    _logger.LogInformation("患者档案更新成功: {PatientName}", response.Content.Name);
                    return ServiceResult<PatientDto>.Success(response.Content, "患者档案更新成功");
                }

                return ServiceResult<PatientDto>.Failure("更新患者档案失败");
            },
            nameof(UpdateAsync), $"更新患者档案: {id}", cancellationToken);
    }

    public async Task<ServiceResult<bool>> EnableAsync(Guid patientId)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("启用患者档案: {PatientId}", patientId);
                return ServiceResult<bool>.Success(true, "患者档案启用成功");
            },
            nameof(EnableAsync), $"启用患者档案: {patientId}", CancellationToken.None);
    }

    public async Task<ServiceResult<bool>> DisableAsync(Guid patientId)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("禁用患者档案: {PatientId}", patientId);
                return ServiceResult<bool>.Success(true, "患者档案禁用成功");
            },
            nameof(DisableAsync), $"禁用患者档案: {patientId}", CancellationToken.None);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid patientId)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("删除患者档案: {PatientId}", patientId);

                var response = await _patientApi.DeletePatientAsync(patientId).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("患者档案删除成功: {PatientId}", patientId);
                    return ServiceResult<bool>.Success(true, "患者档案删除成功");
                }

                return ServiceResult<bool>.Failure("删除患者档案失败");
            },
            nameof(DeleteAsync), $"删除患者档案: {patientId}", CancellationToken.None);
    }
}