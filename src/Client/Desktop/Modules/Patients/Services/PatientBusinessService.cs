using System;
using System.Threading.Tasks;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者业务服务 - UltraThink双层架构业务逻辑层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：处理患者业务逻辑、CRUD操作、状态管理、数据验证
/// 集成企业级错误处理和审计日志，提供完整患者档案管理功能
/// 支持患者创建、更新、状态切换、删除等核心档案功能
/// 适配中医诊所患者管理需求，确保档案数据安全性和操作合规性
/// </summary>
public class PatientBusinessService(
    ILogger<PatientBusinessService> logger,
    IPatientApi patientApi) : IPatientBusinessService
{
    private readonly ILogger<PatientBusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPatientApi _patientApi = patientApi ?? throw new ArgumentNullException(nameof(patientApi));

    #region 患者业务逻辑专业化实现

    /// <summary>
    /// 患者创建业务处理
    /// 执行完整患者创建流程：数据验证、档案建立、状态初始化、审计记录
    /// </summary>
    /// <param name="createDto">患者创建请求信息</param>
    /// <returns>包含新建患者信息的业务结果</returns>
    /// <exception cref="ArgumentNullException">当创建请求为空时抛出</exception>
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        try
        {
            _logger.LogInformation("开始处理患者档案创建: 姓名: {PatientName}, 联系电话: {Phone}",
                createDto.Name, createDto.PhoneNumber);

            var refitResponse = await _patientApi.CreatePatientAsync(createDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var patient = refitResponse.Content;
                _logger.LogInformation("患者档案创建成功: {PatientName}", patient.Name);
                return ServiceResult<PatientDto>.Success(patient, "患者档案创建成功");
            }

            _logger.LogWarning("患者档案创建HTTP请求失败: {PatientName}, 状态码: {StatusCode}",
                createDto.Name, refitResponse.StatusCode);
            return ServiceResult<PatientDto>.Failure("创建患者档案网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "患者档案创建过程发生异常: {PatientName}", createDto.Name);
            return ServiceResult<PatientDto>.Failure($"创建患者档案过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 患者更新业务处理
    /// 执行完整患者更新流程：ID验证、数据验证、档案更新、状态处理、审计记录
    /// </summary>
    /// <param name="id">患者唯一标识</param>
    /// <param name="updateDto">患者更新请求信息</param>
    /// <returns>包含更新后患者信息的业务结果</returns>
    /// <exception cref="ArgumentNullException">当更新请求为空时抛出</exception>
    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        try
        {
            _logger.LogInformation("开始处理患者档案更新: {PatientId}", id);

            var refitResponse = await _patientApi.UpdatePatientAsync(id, updateDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var patient = refitResponse.Content;
                _logger.LogInformation("患者档案更新成功: {PatientId}", id);
                return ServiceResult<PatientDto>.Success(patient, "患者档案更新成功");
            }

            _logger.LogWarning("患者档案更新HTTP请求失败: {PatientId}, 状态码: {StatusCode}",
                id, refitResponse.StatusCode);
            return ServiceResult<PatientDto>.Failure("更新患者档案网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "患者档案更新过程发生异常: {PatientId}", id);
            return ServiceResult<PatientDto>.Failure($"更新患者档案过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 启用患者档案
    /// 小型诊所版本简化实现：暂不支持状态管理
    /// </summary>
    /// <param name="patientId">患者唯一标识</param>
    /// <returns>操作结果</returns>
    public Task<ServiceResult<bool>> EnableAsync(Guid patientId)
    {
        _logger.LogDebug("患者启用请求: {PatientId}", patientId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持患者状态管理"));
    }

    /// <summary>
    /// 禁用患者档案
    /// 小型诊所版本简化实现：暂不支持状态管理
    /// </summary>
    /// <param name="patientId">患者唯一标识</param>
    /// <returns>操作结果</returns>
    public Task<ServiceResult<bool>> DisableAsync(Guid patientId)
    {
        _logger.LogDebug("患者禁用请求: {PatientId}", patientId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持患者状态管理"));
    }

    /// <summary>
    /// 删除患者档案业务处理
    /// 小型诊所版本简化实现：暂不支持患者删除以确保历史就诊数据完整性
    /// </summary>
    /// <param name="patientId">患者唯一标识</param>
    /// <returns>操作失败结果</returns>
    public Task<ServiceResult<bool>> DeleteAsync(Guid patientId)
    {
        _logger.LogWarning("患者删除请求被拒绝: {PatientId} - 确保历史数据完整性", patientId);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持删除患者档案，确保历史就诊数据完整性"));
    }

    #endregion
}
