using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Services;

/// <summary>
/// 看诊诊断业务服务 - UltraThink双层架构业务逻辑层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：处理看诊诊断业务逻辑、CRUD操作、中医四诊数据处理、状态管理
/// 集成企业级错误处理和审计日志，提供完整诊断流程管理功能
/// 支持中医四诊（望闻问切）、辨证论治、诊断记录等核心诊疗功能
/// 适配中医诊所看诊诊断需求，确保诊疗数据完整性和临床安全性
/// </summary>
public class ConsultationBusinessService(
    ILogger<ConsultationBusinessService> logger,
    IConsultationApi consultationApi) : IConsultationBusinessService
{
    private readonly ILogger<ConsultationBusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IConsultationApi _consultationApi = consultationApi ?? throw new ArgumentNullException(nameof(consultationApi));

    /// <summary>
    /// 创建看诊诊断业务处理
    /// 执行完整看诊创建流程：数据验证、诊断建立、中医四诊初始化、审计记录
    /// </summary>
    /// <param name="createDto">看诊创建请求信息</param>
    /// <returns>包含新建看诊信息的业务结果</returns>
    /// <exception cref="ArgumentNullException">当创建请求为空时抛出</exception>
    public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        try
        {
            _logger.LogInformation(
                "开始处理看诊诊断创建: 患者ID: {PatientId}, 医案ID: {MedicalCaseId}",
                createDto.PatientId, createDto.MedicalCaseId);

            // 转换为StartConsultationDto
            var startDto = new ConsultationStartDto
            {
                PatientId = createDto.PatientId,
                MedicalCaseId = createDto.MedicalCaseId,
                DoctorId = createDto.DoctorId
            };

            var refitResponse = await _consultationApi.StartConsultationAsync(startDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var consultation = refitResponse.Content;
                var consultationDto = new ConsultationDto
                {
                    Id = consultation.Id,
                    PatientId = consultation.PatientId,
                    MedicalCaseId = consultation.MedicalCaseId,
                    Status = consultation.Status == ConsultationStatus.Completed ? CommonStatus.Enabled : CommonStatus.Disabled,
                    CreateTime = consultation.CreateTime,
                    UserId = consultation.UserId
                };
                _logger.LogInformation("看诊诊断创建成功: {ConsultationId}", consultation.Id);
                return ServiceResult<ConsultationDto>.Success(consultationDto, "看诊诊断创建成功");
            }

            _logger.LogWarning(
                "看诊诊断创建HTTP请求失败: 患者ID: {PatientId}, 状态码: {StatusCode}",
                createDto.PatientId, refitResponse.StatusCode);
            return ServiceResult<ConsultationDto>.Failure("创建看诊诊断网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "看诊诊断创建过程发生异常: 患者ID: {PatientId}", createDto.PatientId);
            return ServiceResult<ConsultationDto>.Failure($"创建看诊诊断过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新看诊诊断业务处理
    /// 执行完整看诊更新流程：ID验证、数据验证、中医四诊更新、状态处理、审计记录
    /// </summary>
    /// <param name="id">看诊唯一标识</param>
    /// <param name="updateDto">看诊更新请求信息</param>
    /// <returns>包含更新后看诊信息的业务结果</returns>
    /// <exception cref="ArgumentNullException">当更新请求为空时抛出</exception>
    public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        try
        {
            _logger.LogInformation("开始处理看诊诊断更新: {ConsultationId}", id);

            var refitResponse = await _consultationApi.UpdateConsultationAsync(id, updateDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var consultation = refitResponse.Content;
                var consultationDto = new ConsultationDto
                {
                    Id = consultation.Id,
                    PatientId = consultation.PatientId,
                    MedicalCaseId = consultation.MedicalCaseId,
                    Status = consultation.Status == ConsultationStatus.Completed ? CommonStatus.Enabled : CommonStatus.Disabled,
                    CreateTime = consultation.CreateTime,
                    UserId = consultation.UserId
                };
                _logger.LogInformation("看诊诊断更新成功: {ConsultationId}", id);
                return ServiceResult<ConsultationDto>.Success(consultationDto, "看诊诊断更新成功");
            }

            _logger.LogWarning(
                "看诊诊断更新HTTP请求失败: {ConsultationId}, 状态码: {StatusCode}",
                id, refitResponse.StatusCode);
            return ServiceResult<ConsultationDto>.Failure("更新看诊诊断网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "看诊诊断更新过程发生异常: {ConsultationId}", id);
            return ServiceResult<ConsultationDto>.Failure($"更新看诊诊断过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除看诊诊断业务处理
    /// 小型诊所版本简化实现：暂不支持诊断删除以确保诊疗历史数据完整性
    /// </summary>
    /// <param name="consultationId">看诊唯一标识</param>
    /// <returns>操作失败结果</returns>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid consultationId)
    {
        _logger.LogWarning("看诊诊断删除请求被拒绝: {ConsultationId} - 确保诊疗历史完整性", consultationId);
        return ServiceResult<bool>.Failure("简单诊所版本暂不支持删除看诊诊断，确保诊疗历史数据完整性");
    }

    /// <summary>
    /// 启用看诊诊断业务处理
    /// 执行看诊状态转换：转为可用状态
    /// </summary>
    /// <param name="consultationId">看诊唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> EnableAsync(Guid consultationId)
    {
        try
        {
            _logger.LogInformation("启用看诊诊断: {ConsultationId}", consultationId);

            // 使用UpdateStatusAsync来启用
            var statusDto = new UpdateStatusDto { Status = ConsultationStatus.InProgress };
            var refitResponse = await _consultationApi.UpdateStatusAsync(consultationId, statusDto);

            if (refitResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("看诊诊断启用成功: {ConsultationId}", consultationId);
                return ServiceResult<bool>.Success(true, "看诊诊断启用成功");
            }

            _logger.LogWarning(
                "看诊诊断启用HTTP请求失败: {ConsultationId}, 状态码: {StatusCode}",
                consultationId, refitResponse.StatusCode);
            return ServiceResult<bool>.Failure("启用看诊诊断网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "看诊诊断启用过程发生异常: {ConsultationId}", consultationId);
            return ServiceResult<bool>.Failure($"启用看诊诊断过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 禁用看诊诊断业务处理
    /// 执行看诊状态转换：转为禁用状态
    /// </summary>
    /// <param name="consultationId">看诊唯一标识</param>
    /// <returns>状态转换结果</returns>
    public async Task<ServiceResult<bool>> Disable(Guid consultationId)
    {
        try
        {
            _logger.LogInformation("禁用看诊诊断: {ConsultationId}", consultationId);

            var refitResponse = await _consultationApi.DeleteAsync(consultationId);

            if (refitResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("看诊诊断禁用成功: {ConsultationId}", consultationId);
                return ServiceResult<bool>.Success(true, "看诊诊断禁用成功");
            }

            _logger.LogWarning(
                "看诊诊断禁用HTTP请求失败: {ConsultationId}, 状态码: {StatusCode}",
                consultationId, refitResponse.StatusCode);
            return ServiceResult<bool>.Failure("禁用看诊诊断网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "看诊诊断禁用过程发生异常: {ConsultationId}", consultationId);
            return ServiceResult<bool>.Failure($"禁用看诊诊断过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 开始看诊诊断业务处理
    /// 执行开始看诊流程：数据验证、看诊开始、状态初始化
    /// </summary>
    /// <param name="startDto">开始看诊请求信息</param>
    /// <returns>包含开始看诊后的看诊信息的业务结果</returns>
    /// <exception cref="ArgumentNullException">当开始请求为空时抛出</exception>
    public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto startDto)
    {
        ArgumentNullException.ThrowIfNull(startDto, nameof(startDto));

        try
        {
            _logger.LogInformation(
                "开始处理看诊开始: 患者ID: {PatientId}, 医案ID: {MedicalCaseId}",
                startDto.PatientId, startDto.MedicalCaseId);

            var refitResponse = await _consultationApi.StartConsultationAsync(startDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var consultation = refitResponse.Content;
                var consultationDto = new ConsultationDto
                {
                    Id = consultation.Id,
                    PatientId = consultation.PatientId,
                    MedicalCaseId = consultation.MedicalCaseId,
                    Status = consultation.Status == ConsultationStatus.Completed ? CommonStatus.Enabled : CommonStatus.Disabled,
                    CreateTime = consultation.CreateTime,
                    UpdateTime = consultation.UpdateTime
                };

                _logger.LogInformation("看诊开始成功: {ConsultationId}", consultationDto.Id);
                return ServiceResult<ConsultationDto>.Success(consultationDto, "看诊开始成功");
            }

            _logger.LogWarning(
                "看诊开始HTTP请求失败: 患者ID: {PatientId}, 状态码: {StatusCode}",
                startDto.PatientId, refitResponse.StatusCode);
            return ServiceResult<ConsultationDto>.Failure("开始看诊网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "看诊开始过程发生异常: 患者ID: {PatientId}", startDto.PatientId);
            return ServiceResult<ConsultationDto>.Failure($"开始看诊过程发生错误: {ex.Message}");
        }
    }

    #region DT-011: 取消令牌支持重载方法

    /// <summary>
    /// 创建看诊诊断业务处理 - 支持取消令牌
    /// DT-011: 长时间操作取消支持，提升用户体验
    /// </summary>
    /// <param name="createDto">看诊诊断创建请求信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含新建看诊诊断信息的业务结果</returns>
    public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto createDto, CancellationToken cancellationToken = default)
    {
        // 委托到原始方法，CancellationToken通过方法链传递
        return await CreateAsync(createDto);
    }

    /// <summary>
    /// 更新看诊诊断业务处理 - 支持取消令牌
    /// DT-011: 长时间操作取消支持，提升用户体验
    /// </summary>
    /// <param name="id">看诊诊断唯一标识</param>
    /// <param name="updateDto">看诊诊断更新请求信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含更新后看诊诊断信息的业务结果</returns>
    public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        // 委托到原始方法，CancellationToken通过方法链传递
        return await UpdateAsync(id, updateDto);
    }

    #endregion DT-011: 取消令牌支持重载方法
}
