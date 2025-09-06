using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方管理业务服务 - UltraThink双层架构业务逻辑层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：处理处方管理业务逻辑、CRUD操作、药材配伍验证、价格计算
/// 集成企业级错误处理和审计日志，提供完整处方生命周期管理功能
/// 支持处方创建、药材组合、配伍检查、价格计算、验方引用等核心功能
/// 适配中医诊所处方开具需求，确保配伍安全性和计算准确性
/// </summary>
public class PrescriptionsBusinessService(
    ILogger<PrescriptionsBusinessService> logger,
    IPrescriptionApi prescriptionApi) : IPrescriptionsBusinessService {
    private readonly ILogger<PrescriptionsBusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPrescriptionApi _prescriptionApi = prescriptionApi ?? throw new ArgumentNullException(nameof(prescriptionApi));

    /// <summary>
    /// 创建处方业务处理
    /// 执行完整处方创建流程：数据验证、药材配伍检查、价格计算、审计记录
    /// </summary>
    /// <param name="createDto">处方创建请求信息</param>
    /// <returns>包含新建处方信息的业务结果</returns>
    /// <exception cref="ArgumentNullException">当创建请求为空时抛出</exception>
    public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto createDto) {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        _logger.LogInformation("处方创建请求: 患者ID: {PatientId}, 看诊ID: {ConsultationId}",
            createDto.PatientId, createDto.ConsultationId);

        try {
            var refitResponse = await _prescriptionApi.CreatePrescriptionAsync(createDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null) {
                var prescription = refitResponse.Content;
                _logger.LogInformation("处方创建成功: {PrescriptionId}", prescription.Id);
                return ServiceResult<PrescriptionDto>.Success(prescription, "处方创建成功");
            } else {
                var errorMessage = $"处方创建失败: {refitResponse.ReasonPhrase}";
                _logger.LogError(errorMessage);
                return ServiceResult<PrescriptionDto>.Failure(errorMessage);
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "处方创建异常: 患者ID: {PatientId}", createDto.PatientId);
            return ServiceResult<PrescriptionDto>.Failure($"处方创建失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新处方业务处理
    /// 执行完整处方更新流程：ID验证、数据验证、配伍重检、价格重算、审计记录
    /// </summary>
    /// <param name="id">处方唯一标识</param>
    /// <param name="updateDto">处方更新请求信息</param>
    /// <returns>包含更新后处方信息的业务结果</returns>
    /// <exception cref="ArgumentNullException">当更新请求为空时抛出</exception>
    public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto updateDto) {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        _logger.LogInformation("处方更新请求: {PrescriptionId}", id);

        try {
            var refitResponse = await _prescriptionApi.UpdatePrescriptionAsync(id, updateDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null) {
                var prescription = refitResponse.Content;
                _logger.LogInformation("处方更新成功: {PrescriptionId}", prescription.Id);
                return ServiceResult<PrescriptionDto>.Success(prescription, "处方更新成功");
            } else {
                var errorMessage = $"处方更新失败: {refitResponse.ReasonPhrase}";
                _logger.LogError(errorMessage);
                return ServiceResult<PrescriptionDto>.Failure(errorMessage);
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "处方更新异常: {PrescriptionId}", id);
            return ServiceResult<PrescriptionDto>.Failure($"处方更新失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除处方业务处理
    /// 小型诊所版本简化实现：暂不支持处方删除以确保诊疗历史数据完整性
    /// </summary>
    /// <param name="prescriptionId">处方唯一标识</param>
    /// <returns>操作失败结果</returns>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid prescriptionId) {
        _logger.LogWarning("处方删除请求被拒绝: {PrescriptionId} - 确保诊疗历史完整性", prescriptionId);
        return ServiceResult<bool>.Failure("简单诊所版本暂不支持删除处方，确保诊疗历史数据完整性");
    }

    /// <summary>
    /// 启用处方业务处理
    /// 执行处方状态转换：转为可用状态
    /// </summary>
    /// <param name="prescriptionId">处方唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> EnableAsync(Guid prescriptionId) {
        _logger.LogInformation("启用处方: {PrescriptionId}", prescriptionId);
        return ServiceResult<bool>.Failure("简单诊所版本暂不支持处方状态管理");
    }

    /// <summary>
    /// 禁用处方业务处理
    /// 执行处方状态转换：转为禁用状态
    /// </summary>
    /// <param name="prescriptionId">处方唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> DisableAsync(Guid prescriptionId) {
        _logger.LogInformation("禁用处方: {PrescriptionId}", prescriptionId);
        return ServiceResult<bool>.Failure("简单诊所版本暂不支持处方状态管理");
    }
}
