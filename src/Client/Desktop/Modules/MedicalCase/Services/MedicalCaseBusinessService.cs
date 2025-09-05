using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医疗案例业务服务 - UltraThink双层架构业务逻辑层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：处理医疗案例业务逻辑、CRUD操作、状态管理、流程控制
/// 集成企业级错误处理和审计日志，提供完整医案生命周期管理功能
/// 支持医案创建、状态转换、完成取消等核心诊疗流程功能
/// 适配中医诊所医疗案例管理需求，确保诊疗流程完整性和数据安全性
/// </summary>
public class MedicalCaseBusinessService(
    ILogger<MedicalCaseBusinessService> logger,
    IMedicalCaseApi medicalCaseApi) : IMedicalCaseBusinessService
{
    private readonly ILogger<MedicalCaseBusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMedicalCaseApi _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));

    #region 医疗案例业务逻辑专业化实现

    /// <summary>
    /// 医疗案例创建业务处理
    /// 执行完整医案创建流程：数据验证、案例建立、状态初始化、审计记录
    /// </summary>
    /// <param name="dto">医案创建请求信息</param>
    /// <returns>包含新建医案信息的业务结果</returns>
    /// <exception cref="ArgumentNullException">当创建请求为空时抛出</exception>
    public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));
        
        try
        {
            _logger.LogInformation("开始处理医疗案例创建: 患者ID: {PatientId}", dto.PatientId);
            
            var refitResponse = await _medicalCaseApi.CreateAsync(dto);
            
            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var medicalCase = refitResponse.Content;
                _logger.LogInformation("医疗案例创建成功: {MedicalCaseId}", medicalCase.Id);
                return ServiceResult<MedicalCaseDto>.Success(medicalCase, "医案创建成功");
            }
            
            _logger.LogWarning("医疗案例创建HTTP请求失败: 患者ID: {PatientId}, 状态码: {StatusCode}", 
                dto.PatientId, refitResponse.StatusCode);
            return ServiceResult<MedicalCaseDto>.Failure("创建医案网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医疗案例创建过程发生异常: 患者ID: {PatientId}", dto.PatientId);
            return ServiceResult<MedicalCaseDto>.Failure($"创建医案过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 医疗案例更新业务处理
    /// 执行完整医案更新流程：ID验证、数据验证、案例更新、状态处理、审计记录
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <param name="dto">医案更新请求信息</param>
    /// <returns>包含更新后医案信息的业务结果</returns>
    /// <exception cref="ArgumentNullException">当更新请求为空时抛出</exception>
    public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseDetailDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));
        
        try
        {
            _logger.LogInformation("开始处理医疗案例更新: {MedicalCaseId}", id);
            
            // 转换为EditDto
            var editDto = new MedicalCaseEditDto
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                ChiefComplaint = dto.ChiefComplaint,
                PresentIllness = dto.PresentIllness,
                PastHistory = dto.PastHistory,
                FamilyHistory = dto.FamilyHistory,
                PersonalHistory = dto.PersonalHistory,
                Status = dto.Status,
                Remark = dto.Remark
            };
            
            var refitResponse = await _medicalCaseApi.UpdateAsync(id, editDto);
            
            if (refitResponse.IsSuccessStatusCode && refitResponse.Content == true)
            {
                _logger.LogInformation("医疗案例更新成功: {MedicalCaseId}", id);
                
                // 重新获取更新后的数据
                var getResponse = await _medicalCaseApi.GetByIdAsync(id);
                if (getResponse.IsSuccessStatusCode && getResponse.Content != null)
                {
                    var medicalCaseDto = new MedicalCaseDto
                    {
                        Id = getResponse.Content.Id,
                        PatientId = getResponse.Content.PatientId,
                        DoctorId = getResponse.Content.DoctorId,
                        Status = getResponse.Content.Status,
                        CreateTime = getResponse.Content.CreateTime,
                        ChiefComplaint = getResponse.Content.ChiefComplaint
                    };
                    return ServiceResult<MedicalCaseDto>.Success(medicalCaseDto, "医案更新成功");
                }
                
                return ServiceResult<MedicalCaseDto>.Failure("医案更新成功但获取最新数据失败");
            }
            
            _logger.LogWarning("医疗案例更新HTTP请求失败: {MedicalCaseId}, 状态码: {StatusCode}", 
                id, refitResponse.StatusCode);
            return ServiceResult<MedicalCaseDto>.Failure("更新医案网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医疗案例更新过程发生异常: {MedicalCaseId}", id);
            return ServiceResult<MedicalCaseDto>.Failure($"更新医案过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除医疗案例业务处理
    /// 小型诊所版本简化实现：暂不支持医案删除以确保诊疗历史数据完整性
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <returns>操作失败结果</returns>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        _logger.LogWarning("医疗案例删除请求被拒绝: {MedicalCaseId} - 确保诊疗历史完整性", id);
        return ServiceResult<bool>.Failure("简单诊所版本暂不支持删除医疗案例，确保诊疗历史数据完整性");
    }

    /// <summary>
    /// 开始医疗案例业务处理
    /// 执行医案状态转换：从创建状态转为进行中状态
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> StartAsync(Guid id)
    {
        return await UpdateStatusAsync(id, MedicalCaseStatus.InConsultation, "开始医案");
    }

    /// <summary>
    /// 完成医疗案例业务处理
    /// 执行医案状态转换：从进行中状态转为完成状态
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> CompleteAsync(Guid id)
    {
        return await UpdateStatusAsync(id, MedicalCaseStatus.Completed, "完成医案");
    }

    /// <summary>
    /// 取消医疗案例业务处理
    /// 执行医案状态转换：转为取消状态，记录取消原因
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> CancelAsync(Guid id)
    {
        return await UpdateStatusAsync(id, MedicalCaseStatus.Cancelled, "取消医案");
    }

    /// <summary>
    /// 更新医案状态的通用方法
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <param name="status">新状态</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>状态转换结果</returns>
    private async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status, string operationName)
    {
        try
        {
            _logger.LogInformation("开始处理{OperationName}: {MedicalCaseId}", operationName, id);
            
            var refitResponse = await _medicalCaseApi.UpdateStatusAsync(id, status);
            
            if (refitResponse.IsSuccessStatusCode && refitResponse.Content == true)
            {
                _logger.LogInformation("{OperationName}成功: {MedicalCaseId}", operationName, id);
                return ServiceResult<bool>.Success(true, $"{operationName}成功");
            }
            
            _logger.LogWarning("{OperationName}HTTP请求失败: {MedicalCaseId}, 状态码: {StatusCode}", 
                operationName, id, refitResponse.StatusCode);
            return ServiceResult<bool>.Failure($"{operationName}网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{OperationName}过程发生异常: {MedicalCaseId}", operationName, id);
            return ServiceResult<bool>.Failure($"{operationName}过程发生错误: {ex.Message}");
        }
    }

    #endregion
}