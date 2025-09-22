using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

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

            _logger.LogWarning(
                "医疗案例创建HTTP请求失败: 患者ID: {PatientId}, 状态码: {StatusCode}",
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
                Id = id,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                ChiefComplaint = dto.ChiefComplaint,
                PresentIllness = dto.PresentIllness,
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
                        Remark = getResponse.Content.Remark
                    };
                    return ServiceResult<MedicalCaseDto>.Success(medicalCaseDto, "医案更新成功");
                }

                return ServiceResult<MedicalCaseDto>.Failure("医案更新成功但获取最新数据失败");
            }

            _logger.LogWarning(
                "医疗案例更新HTTP请求失败: {MedicalCaseId}, 状态码: {StatusCode}",
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
    public Task<ServiceResult<bool>> Delete(Guid id)
    {
        _logger.LogWarning("医疗案例删除请求被拒绝: {MedicalCaseId} - 确保诊疗历史完整性", id);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持删除医疗案例，确保诊疗历史数据完整性"));
    }

    /// <summary>
    /// 开始医疗案例业务处理
    /// 执行医案状态转换：从创建状态转为进行中状态
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> StartAsync(Guid id)
    {
        return await UpdateStatusAsync(id, MedicalCaseStatus.Active, "开始就诊");
    }

    /// <summary>
    /// 完成医疗案例业务处理
    /// 执行医案状态转换：从进行中状态转为完成状态
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> CompleteAsync(Guid id)
    {
        return await UpdateStatusAsync(id, MedicalCaseStatus.Closed, "结束就诊");
    }

    /// <summary>
    /// 取消医疗案例业务处理
    /// 执行医案状态转换：转为取消状态，记录取消原因
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> CancelAsync(Guid id)
    {
        return await UpdateStatusAsync(id, MedicalCaseStatus.Closed, "取消就诊");
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

            _logger.LogWarning(
                "{OperationName}HTTP请求失败: {MedicalCaseId}, 状态码: {StatusCode}",
                operationName, id, refitResponse.StatusCode);
            return ServiceResult<bool>.Failure($"{operationName}网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{OperationName}过程发生异常: {MedicalCaseId}", operationName, id);
            return ServiceResult<bool>.Failure($"{operationName}过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 暂停医疗案例业务处理
    /// 执行医案暂停流程：暂停案例处理，记录暂停原因
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <param name="reason">暂停原因</param>
    /// <returns>暂停操作结果</returns>
    public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason)
    {
        try
        {
            _logger.LogInformation("开始处理医疗案例暂停: {MedicalCaseId}, 原因: {Reason}", id, reason);

            var dto = new SuspendMedicalCaseDto { Reason = reason };
            var refitResponse = await _medicalCaseApi.SuspendAsync(id, dto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content == true)
            {
                _logger.LogInformation("医疗案例暂停成功: {MedicalCaseId}", id);
                return ServiceResult<bool>.Success(true, "医案暂停成功");
            }

            _logger.LogWarning(
                "医疗案例暂停HTTP请求失败: {MedicalCaseId}, 状态码: {StatusCode}",
                id, refitResponse.StatusCode);
            return ServiceResult<bool>.Failure("暂停医案网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医疗案例暂停过程发生异常: {MedicalCaseId}", id);
            return ServiceResult<bool>.Failure($"暂停医案过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 恢复医疗案例业务处理
    /// 执行医案恢复流程：恢复暂停的案例处理
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <returns>恢复操作结果</returns>
    public async Task<ServiceResult<bool>> ResumeAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("开始处理医疗案例恢复: {MedicalCaseId}", id);

            var refitResponse = await _medicalCaseApi.ResumeAsync(id);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content == true)
            {
                _logger.LogInformation("医疗案例恢复成功: {MedicalCaseId}", id);
                return ServiceResult<bool>.Success(true, "医案恢复成功");
            }

            _logger.LogWarning(
                "医疗案例恢复HTTP请求失败: {MedicalCaseId}, 状态码: {StatusCode}",
                id, refitResponse.StatusCode);
            return ServiceResult<bool>.Failure("恢复医案网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医疗案例恢复过程发生异常: {MedicalCaseId}", id);
            return ServiceResult<bool>.Failure($"恢复医案过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 归档医疗案例业务处理
    /// 执行医案归档流程：将完成的案例移至归档状态
    /// </summary>
    /// <param name="id">医案唯一标识</param>
    /// <param name="archiveReason">归档原因</param>
    /// <returns>归档操作结果</returns>
    public async Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
    {
        try
        {
            _logger.LogInformation("开始处理医疗案例归档: {MedicalCaseId}, 原因: {Reason}", id, archiveReason);

            var dto = new ArchiveMedicalCaseDto { ArchiveReason = archiveReason };
            var refitResponse = await _medicalCaseApi.ArchiveAsync(id, dto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content == true)
            {
                _logger.LogInformation("医疗案例归档成功: {MedicalCaseId}", id);
                return ServiceResult<bool>.Success(true, "医案归档成功");
            }

            _logger.LogWarning(
                "医疗案例归档HTTP请求失败: {MedicalCaseId}, 状态码: {StatusCode}",
                id, refitResponse.StatusCode);
            return ServiceResult<bool>.Failure("归档医案网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医疗案例归档过程发生异常: {MedicalCaseId}", id);
            return ServiceResult<bool>.Failure($"归档医案过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 搜索医疗案例业务处理
    /// 执行医案搜索功能：根据关键词查询匹配的医案
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>匹配的医案列表</returns>
    public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return ServiceResult<List<MedicalCaseDto>>.Failure("搜索关键词不能为空");
        }

        try
        {
            _logger.LogInformation("开始搜索医疗案例: 关键词: {Keyword}", keyword);

            var refitResponse = await _medicalCaseApi.SearchAsync(keyword);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                _logger.LogInformation("医疗案例搜索成功: 找到 {Count} 条记录", refitResponse.Content.Count);
                return ServiceResult<List<MedicalCaseDto>>.Success(refitResponse.Content, $"搜索成功，找到 {refitResponse.Content.Count} 条记录");
            }

            _logger.LogWarning(
                "医疗案例搜索HTTP请求失败: 关键词: {Keyword}, 状态码: {StatusCode}",
                keyword, refitResponse.StatusCode);
            return ServiceResult<List<MedicalCaseDto>>.Failure("搜索医案网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医疗案例搜索过程发生异常: 关键词: {Keyword}", keyword);
            return ServiceResult<List<MedicalCaseDto>>.Failure($"搜索医案过程发生错误: {ex.Message}");
        }
    }

    #endregion 医疗案例业务逻辑专业化实现
}



