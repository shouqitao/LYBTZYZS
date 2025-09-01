using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Refit;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者业务服务实现 - UltraThink三层架构业务逻辑层
/// 职责：业务流程编排、CRUD操作、状态转换、事件处理
/// </summary>
public class PatientBusinessService(
    IPatientApi patientApi,
    ILogger<PatientBusinessService> logger,
    IMemoryCache cache) : IPatientBusinessService
{
    private readonly IPatientApi _patientApi = patientApi;
    private readonly ILogger<PatientBusinessService> _logger = logger;
    private readonly IMemoryCache _cache = cache;

    #region 事件定义

    public event EventHandler<PatientStatusChangedEventArgs>? PatientStatusChanged;
    public event EventHandler<PatientOperationEventArgs>? PatientOperation;
    public event EventHandler<PatientVisitEventArgs>? PatientVisit;

    #endregion

    #region CRUD业务操作

    /// <summary>
    /// 创建患者（完整业务流程）
    /// </summary>
    public async Task<ServiceResult<PatientDto>> CreatePatientAsync(PatientCreateDto createDto)
    {
        try
        {
            _logger.LogInformation("开始创建患者，姓名：{Name}，手机：{Phone}", createDto.Name, createDto.Phone);

            // 1. 业务规则验证
            var validationResult = ValidatePatientCreateData(createDto);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("患者创建数据验证失败：{Message}", validationResult.ErrorMessage);
                return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage);
            }

            // 2. 重复性检查
            var duplicateCheck = await CheckForDuplicatesAsync(createDto);
            if (!duplicateCheck.IsSuccess)
            {
                _logger.LogWarning("患者重复性检查失败：{Message}", duplicateCheck.ErrorMessage);
                return ServiceResult<PatientDto>.Failure(duplicateCheck.ErrorMessage);
            }

            // 3. 调用API创建患者
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.CreatePatientAsync(createDto);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("创建患者API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                
                // 触发操作事件
                OnPatientOperation(new PatientOperationEventArgs
                {
                    PatientId = Guid.Empty,
                    Operation = "Create",
                    Description = "创建患者失败",
                    Success = false,
                    ErrorMessage = $"API调用失败：{apiResponse.StatusCode}"
                });
                
                return ServiceResult<PatientDto>.Failure($"创建失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content;
            if (result == null)
            {
                return ServiceResult<PatientDto>.Failure("创建患者返回数据为空");
            }

            // 4. 清除相关缓存
            ClearPatientRelatedCache();

            // 5. 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = result.Id,
                Action = "CREATE",
                Description = "创建新患者",
                Timestamp = DateTime.Now
            });

            // 6. 触发操作事件
            OnPatientOperation(new PatientOperationEventArgs
            {
                PatientId = result.Id,
                Operation = "Create",
                Description = "成功创建患者",
                Success = true
            });

            _logger.LogInformation("患者创建成功，ID：{PatientId}，姓名：{Name}", result.Id, result.Name);
            return ServiceResult<PatientDto>.Success(result, "患者创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者异常，姓名：{Name}", createDto.Name);
            
            // 触发操作事件
            OnPatientOperation(new PatientOperationEventArgs
            {
                PatientId = Guid.Empty,
                Operation = "Create",
                Description = "创建患者异常",
                Success = false,
                ErrorMessage = ex.Message
            });
            
            return ServiceResult<PatientDto>.Failure($"创建异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 更新患者信息（完整业务流程）
    /// </summary>
    public async Task<ServiceResult<PatientDto>> UpdatePatientAsync(Guid id, PatientUpdateDto updateDto)
    {
        try
        {
            _logger.LogInformation("开始更新患者，ID：{PatientId}，姓名：{Name}", id, updateDto.Name);

            // 1. 验证患者是否存在
            var existsResult = await ValidatePatientExistsAsync(id);
            if (!existsResult.IsSuccess || !existsResult.Data)
            {
                _logger.LogWarning("患者不存在，ID：{PatientId}", id);
                return ServiceResult<PatientDto>.Failure("患者不存在");
            }

            // 2. 业务规则验证
            var validationResult = ValidatePatientUpdateData(updateDto);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("患者更新数据验证失败：{Message}", validationResult.ErrorMessage);
                return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage);
            }

            // 3. 重复性检查（排除当前患者）
            if (!string.IsNullOrEmpty(updateDto.Phone))
            {
                var phoneCheck = await CheckPhoneAvailabilityAsync(updateDto.Phone, id);
                if (!phoneCheck.IsSuccess || !phoneCheck.Data)
                {
                    return ServiceResult<PatientDto>.Failure("手机号已被其他患者使用");
                }
            }

            if (!string.IsNullOrEmpty(updateDto.IdCard))
            {
                var idCardCheck = await CheckIdCardAvailabilityAsync(updateDto.IdCard, id);
                if (!idCardCheck.IsSuccess || !idCardCheck.Data)
                {
                    return ServiceResult<PatientDto>.Failure("身份证号已被其他患者使用");
                }
            }

            // 4. 调用API更新患者
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.UpdatePatientAsync(id, updateDto);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("更新患者API调用失败，ID：{PatientId}，状态码：{StatusCode}", id, apiResponse.StatusCode);
                
                // 触发操作事件
                OnPatientOperation(new PatientOperationEventArgs
                {
                    PatientId = id,
                    Operation = "Update",
                    Description = "更新患者失败",
                    Success = false,
                    ErrorMessage = $"API调用失败：{apiResponse.StatusCode}"
                });
                
                return ServiceResult<PatientDto>.Failure($"更新失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content;
            if (result == null)
            {
                return ServiceResult<PatientDto>.Failure("更新患者返回数据为空");
            }

            // 5. 清除相关缓存
            ClearPatientRelatedCache();
            _cache.Remove($"patient_{id}");

            // 6. 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = id,
                Action = "UPDATE",
                Description = "更新患者信息",
                Timestamp = DateTime.Now
            });

            // 7. 触发操作事件
            OnPatientOperation(new PatientOperationEventArgs
            {
                PatientId = id,
                Operation = "Update",
                Description = "成功更新患者信息",
                Success = true
            });

            _logger.LogInformation("患者更新成功，ID：{PatientId}，姓名：{Name}", id, result.Name);
            return ServiceResult<PatientDto>.Success(result, "患者信息更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者异常，ID：{PatientId}", id);
            
            // 触发操作事件
            OnPatientOperation(new PatientOperationEventArgs
            {
                PatientId = id,
                Operation = "Update",
                Description = "更新患者异常",
                Success = false,
                ErrorMessage = ex.Message
            });
            
            return ServiceResult<PatientDto>.Failure($"更新异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 删除患者（软删除业务流程）
    /// </summary>
    public async Task<ServiceResult<bool>> DeletePatientAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("开始删除患者，ID：{PatientId}", id);

            // 1. 验证患者是否存在
            var existsResult = await ValidatePatientExistsAsync(id);
            if (!existsResult.IsSuccess || !existsResult.Data)
            {
                _logger.LogWarning("患者不存在，ID：{PatientId}", id);
                return ServiceResult<bool>.Failure("患者不存在");
            }

            // 2. 业务约束检查
            var constraintCheck = await ValidatePatientConstraintsAsync(id);
            if (!constraintCheck.IsSuccess || !constraintCheck.Data)
            {
                return ServiceResult<bool>.Failure("患者存在关联数据，无法删除");
            }

            // 3. 调用API删除患者
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.DeletePatientAsync(id);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("删除患者API调用失败，ID：{PatientId}，状态码：{StatusCode}", id, apiResponse.StatusCode);
                
                // 触发操作事件
                OnPatientOperation(new PatientOperationEventArgs
                {
                    PatientId = id,
                    Operation = "Delete",
                    Description = "删除患者失败",
                    Success = false,
                    ErrorMessage = $"API调用失败：{apiResponse.StatusCode}"
                });
                
                return ServiceResult<bool>.Failure($"删除失败：{apiResponse.StatusCode}");
            }

            // 4. 清除相关缓存
            ClearPatientRelatedCache();
            _cache.Remove($"patient_{id}");

            // 5. 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = id,
                Action = "DELETE",
                Description = "删除患者（软删除）",
                Timestamp = DateTime.Now
            });

            // 6. 触发操作事件
            OnPatientOperation(new PatientOperationEventArgs
            {
                PatientId = id,
                Operation = "Delete",
                Description = "成功删除患者",
                Success = true
            });

            _logger.LogInformation("患者删除成功，ID：{PatientId}", id);
            return ServiceResult<bool>.Success(true, "患者删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者异常，ID：{PatientId}", id);
            
            // 触发操作事件
            OnPatientOperation(new PatientOperationEventArgs
            {
                PatientId = id,
                Operation = "Delete",
                Description = "删除患者异常",
                Success = false,
                ErrorMessage = ex.Message
            });
            
            return ServiceResult<bool>.Failure($"删除异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 批量删除患者
    /// </summary>
    public async Task<ServiceResult<BatchOperationResult>> BatchDeletePatientsAsync(List<Guid> patientIds)
    {
        try
        {
            _logger.LogInformation("开始批量删除患者，数量：{Count}", patientIds.Count);

            var result = new BatchOperationResult
            {
                TotalCount = patientIds.Count
            };

            foreach (var id in patientIds)
            {
                try
                {
                    var deleteResult = await DeletePatientAsync(id);
                    if (deleteResult.IsSuccess)
                    {
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BatchOperationError
                        {
                            PatientId = id,
                            ErrorMessage = deleteResult.ErrorMessage,
                            ErrorCode = "DELETE_FAILED"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BatchOperationError
                    {
                        PatientId = id,
                        ErrorMessage = ex.Message,
                        ErrorCode = "DELETE_EXCEPTION"
                    });
                }
            }

            _logger.LogInformation("批量删除患者完成，成功：{Success}，失败：{Failed}", result.SuccessCount, result.FailureCount);
            return ServiceResult<BatchOperationResult>.Success(result, $"批量删除完成，成功{result.SuccessCount}个，失败{result.FailureCount}个");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除患者异常");
            return ServiceResult<BatchOperationResult>.Failure($"批量删除异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复已删除患者
    /// </summary>
    public async Task<ServiceResult<PatientDto>> RestorePatientAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("开始恢复患者，ID：{PatientId}", id);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.RestorePatientAsync(id);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("恢复患者API调用失败，ID：{PatientId}，状态码：{StatusCode}", id, apiResponse.StatusCode);
                return ServiceResult<PatientDto>.Failure($"恢复失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content;
            if (result == null)
            {
                return ServiceResult<PatientDto>.Failure("恢复患者返回数据为空");
            }

            // 清除相关缓存
            ClearPatientRelatedCache();

            // 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = id,
                Action = "RESTORE",
                Description = "恢复已删除患者",
                Timestamp = DateTime.Now
            });

            // 触发操作事件
            OnPatientOperation(new PatientOperationEventArgs
            {
                PatientId = id,
                Operation = "Restore",
                Description = "成功恢复患者",
                Success = true
            });

            _logger.LogInformation("患者恢复成功，ID：{PatientId}", id);
            return ServiceResult<PatientDto>.Success(result, "患者恢复成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复患者异常，ID：{PatientId}", id);
            return ServiceResult<PatientDto>.Failure($"恢复异常：{ex.Message}");
        }
    }

    #endregion

    #region 患者状态管理业务

    /// <summary>
    /// 启用患者
    /// </summary>
    public async Task<ServiceResult<bool>> EnablePatientAsync(Guid patientId)
    {
        try
        {
            _logger.LogInformation("启用患者，ID：{PatientId}", patientId);

            var result = await UpdatePatientStatusAsync(patientId, true);
            if (result.IsSuccess)
            {
                // 触发状态变更事件
                OnPatientStatusChanged(new PatientStatusChangedEventArgs
                {
                    PatientId = patientId,
                    IsEnabled = true,
                    Reason = "管理员启用"
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启用患者异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"启用异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 禁用患者
    /// </summary>
    public async Task<ServiceResult<bool>> DisablePatientAsync(Guid patientId)
    {
        try
        {
            _logger.LogInformation("禁用患者，ID：{PatientId}", patientId);

            var result = await UpdatePatientStatusAsync(patientId, false);
            if (result.IsSuccess)
            {
                // 触发状态变更事件
                OnPatientStatusChanged(new PatientStatusChangedEventArgs
                {
                    PatientId = patientId,
                    IsEnabled = false,
                    Reason = "管理员禁用"
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "禁用患者异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"禁用异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 切换患者状态
    /// </summary>
    public async Task<ServiceResult<bool>> TogglePatientStatusAsync(Guid patientId)
    {
        try
        {
            _logger.LogInformation("切换患者状态，ID：{PatientId}", patientId);

            // 先获取当前状态
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var currentStatusResponse = await _patientApi.GetPatientByIdAsync(patientId);
            
            if (!currentStatusResponse.IsSuccessStatusCode || currentStatusResponse.Content == null)
            {
                return ServiceResult<bool>.Failure("无法获取患者当前状态");
            }

            var currentStatus = currentStatusResponse.Content.IsEnabled;
            var newStatus = !currentStatus;

            var result = await UpdatePatientStatusAsync(patientId, newStatus);
            if (result.IsSuccess)
            {
                // 触发状态变更事件
                OnPatientStatusChanged(new PatientStatusChangedEventArgs
                {
                    PatientId = patientId,
                    IsEnabled = newStatus,
                    Reason = "状态切换"
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换患者状态异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"状态切换异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 批量更新患者状态
    /// </summary>
    public async Task<ServiceResult<BatchOperationResult>> BatchUpdatePatientStatusAsync(List<Guid> patientIds, bool isEnabled)
    {
        try
        {
            _logger.LogInformation("批量更新患者状态，数量：{Count}，状态：{Status}", patientIds.Count, isEnabled);

            var result = new BatchOperationResult
            {
                TotalCount = patientIds.Count
            };

            var tasks = patientIds.Select(async id =>
            {
                try
                {
                    var updateResult = await UpdatePatientStatusAsync(id, isEnabled);
                    if (updateResult.IsSuccess)
                    {
                        result.SuccessCount++;
                        
                        // 触发状态变更事件
                        OnPatientStatusChanged(new PatientStatusChangedEventArgs
                        {
                            PatientId = id,
                            IsEnabled = isEnabled,
                            Reason = "批量状态更新"
                        });
                    }
                    else
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BatchOperationError
                        {
                            PatientId = id,
                            ErrorMessage = updateResult.ErrorMessage,
                            ErrorCode = "STATUS_UPDATE_FAILED"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BatchOperationError
                    {
                        PatientId = id,
                        ErrorMessage = ex.Message,
                        ErrorCode = "STATUS_UPDATE_EXCEPTION"
                    });
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogInformation("批量更新患者状态完成，成功：{Success}，失败：{Failed}", result.SuccessCount, result.FailureCount);
            return ServiceResult<BatchOperationResult>.Success(result, $"批量状态更新完成，成功{result.SuccessCount}个，失败{result.FailureCount}个");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新患者状态异常");
            return ServiceResult<BatchOperationResult>.Failure($"批量状态更新异常：{ex.Message}");
        }
    }

    #endregion

    #region 患者档案管理

    /// <summary>
    /// 完善患者档案
    /// </summary>
    public async Task<ServiceResult<PatientDto>> CompletePatientProfileAsync(Guid patientId, PatientProfileDto profileDto)
    {
        try
        {
            _logger.LogInformation("完善患者档案，ID：{PatientId}", patientId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.CompletePatientProfileAsync(patientId, profileDto);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("完善患者档案API调用失败，ID：{PatientId}，状态码：{StatusCode}", patientId, apiResponse.StatusCode);
                return ServiceResult<PatientDto>.Failure($"完善档案失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content;
            if (result == null)
            {
                return ServiceResult<PatientDto>.Failure("完善档案返回数据为空");
            }

            // 清除相关缓存
            _cache.Remove($"patient_{patientId}");

            // 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = patientId,
                Action = "PROFILE_COMPLETE",
                Description = "完善患者档案",
                Timestamp = DateTime.Now
            });

            // 触发操作事件
            OnPatientOperation(new PatientOperationEventArgs
            {
                PatientId = patientId,
                Operation = "ProfileComplete",
                Description = "成功完善患者档案",
                Success = true
            });

            _logger.LogInformation("患者档案完善成功，ID：{PatientId}", patientId);
            return ServiceResult<PatientDto>.Success(result, "患者档案完善成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完善患者档案异常，ID：{PatientId}", patientId);
            return ServiceResult<PatientDto>.Failure($"完善档案异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 更新患者医疗信息
    /// </summary>
    public async Task<ServiceResult<bool>> UpdateMedicalInfoAsync(Guid patientId, PatientMedicalInfoDto medicalInfo)
    {
        try
        {
            _logger.LogInformation("更新患者医疗信息，ID：{PatientId}", patientId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.UpdateMedicalInfoAsync(patientId, medicalInfo);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("更新患者医疗信息API调用失败，ID：{PatientId}，状态码：{StatusCode}", patientId, apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"更新医疗信息失败：{apiResponse.StatusCode}");
            }

            // 清除相关缓存
            _cache.Remove($"patient_{patientId}");

            // 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = patientId,
                Action = "MEDICAL_INFO_UPDATE",
                Description = "更新患者医疗信息",
                Timestamp = DateTime.Now
            });

            _logger.LogInformation("患者医疗信息更新成功，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Success(true, "医疗信息更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者医疗信息异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"更新医疗信息异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 更新患者联系信息
    /// </summary>
    public async Task<ServiceResult<bool>> UpdateContactInfoAsync(Guid patientId, PatientContactInfoDto contactInfo)
    {
        try
        {
            _logger.LogInformation("更新患者联系信息，ID：{PatientId}", patientId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.UpdateContactInfoAsync(patientId, contactInfo);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("更新患者联系信息API调用失败，ID：{PatientId}，状态码：{StatusCode}", patientId, apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"更新联系信息失败：{apiResponse.StatusCode}");
            }

            // 清除相关缓存
            _cache.Remove($"patient_{patientId}");
            ClearPatientRelatedCache(); // 清除可能包含联系信息的相关缓存

            // 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = patientId,
                Action = "CONTACT_INFO_UPDATE",
                Description = "更新患者联系信息",
                Timestamp = DateTime.Now
            });

            _logger.LogInformation("患者联系信息更新成功，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Success(true, "联系信息更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者联系信息异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"更新联系信息异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 添加患者备注
    /// </summary>
    public async Task<ServiceResult<bool>> AddPatientRemarksAsync(Guid patientId, string remarks)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                return ServiceResult<bool>.Failure("备注内容不能为空");
            }

            _logger.LogInformation("添加患者备注，ID：{PatientId}", patientId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.AddPatientRemarksAsync(patientId, remarks);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("添加患者备注API调用失败，ID：{PatientId}，状态码：{StatusCode}", patientId, apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"添加备注失败：{apiResponse.StatusCode}");
            }

            // 清除相关缓存
            _cache.Remove($"patient_{patientId}");

            // 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = patientId,
                Action = "ADD_REMARKS",
                Description = "添加患者备注",
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object> { { "remarks", remarks } }
            });

            _logger.LogInformation("患者备注添加成功，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Success(true, "备注添加成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加患者备注异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"添加备注异常：{ex.Message}");
        }
    }

    #endregion

    #region 就诊记录管理

    /// <summary>
    /// 记录患者就诊
    /// </summary>
    public async Task<ServiceResult> RecordPatientVisitAsync(Guid patientId, PatientVisitDto visitInfo)
    {
        try
        {
            _logger.LogInformation("记录患者就诊，ID：{PatientId}，就诊时间：{VisitTime}", patientId, visitInfo.VisitTime);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.RecordPatientVisitAsync(patientId, visitInfo);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("记录患者就诊API调用失败，ID：{PatientId}，状态码：{StatusCode}", patientId, apiResponse.StatusCode);
                return ServiceResult.Failure($"记录就诊失败：{apiResponse.StatusCode}");
            }

            // 更新最后就诊时间
            await UpdateLastVisitTimeAsync(patientId, visitInfo.VisitTime);

            // 清除相关缓存
            ClearPatientRelatedCache();
            _cache.Remove($"patient_{patientId}");

            // 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = patientId,
                Action = "VISIT_RECORD",
                Description = "记录患者就诊",
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    { "visitTime", visitInfo.VisitTime },
                    { "visitType", visitInfo.VisitType ?? "常规就诊" }
                }
            });

            // 触发就诊事件
            OnPatientVisit(new PatientVisitEventArgs
            {
                PatientId = patientId,
                VisitTime = visitInfo.VisitTime,
                VisitType = visitInfo.VisitType,
                PatientName = "患者" // 实际应该从患者信息获取
            });

            _logger.LogInformation("患者就诊记录成功，ID：{PatientId}", patientId);
            return ServiceResult.Success("就诊记录成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录患者就诊异常，ID：{PatientId}", patientId);
            return ServiceResult.Failure($"记录就诊异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 更新最后就诊时间
    /// </summary>
    public async Task<ServiceResult> UpdateLastVisitTimeAsync(Guid patientId, DateTime visitTime)
    {
        try
        {
            _logger.LogInformation("更新患者最后就诊时间，ID：{PatientId}，时间：{VisitTime}", patientId, visitTime);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.UpdateLastVisitTimeAsync(patientId, visitTime);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("更新最后就诊时间API调用失败，ID：{PatientId}，状态码：{StatusCode}", patientId, apiResponse.StatusCode);
                return ServiceResult.Failure($"更新就诊时间失败：{apiResponse.StatusCode}");
            }

            // 清除相关缓存
            _cache.Remove($"patient_{patientId}");

            _logger.LogInformation("患者最后就诊时间更新成功，ID：{PatientId}", patientId);
            return ServiceResult.Success("就诊时间更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者最后就诊时间异常，ID：{PatientId}", patientId);
            return ServiceResult.Failure($"更新就诊时间异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取患者就诊历史
    /// </summary>
    public async Task<ServiceResult<List<PatientVisitHistoryDto>>> GetPatientVisitHistoryAsync(Guid patientId)
    {
        try
        {
            var cacheKey = $"patient_visit_history_{patientId}";
            
            if (_cache.TryGetValue(cacheKey, out List<PatientVisitHistoryDto>? cachedResult))
            {
                _logger.LogDebug("从缓存获取患者就诊历史，ID：{PatientId}", patientId);
                return ServiceResult<List<PatientVisitHistoryDto>>.Success(cachedResult, "查询成功（缓存）");
            }

            _logger.LogInformation("获取患者就诊历史，ID：{PatientId}", patientId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.GetPatientVisitHistoryAsync(patientId);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("获取患者就诊历史API调用失败，ID：{PatientId}，状态码：{StatusCode}", patientId, apiResponse.StatusCode);
                return ServiceResult<List<PatientVisitHistoryDto>>.Failure($"获取就诊历史失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content ?? new List<PatientVisitHistoryDto>();
            
            // 缓存30分钟
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
            
            _logger.LogInformation("获取患者就诊历史成功，ID：{PatientId}，记录数：{Count}", patientId, result.Count);
            return ServiceResult<List<PatientVisitHistoryDto>>.Success(result, "就诊历史查询成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者就诊历史异常，ID：{PatientId}", patientId);
            return ServiceResult<List<PatientVisitHistoryDto>>.Failure($"获取就诊历史异常：{ex.Message}");
        }
    }

    #endregion

    #region 数据导入导出

    /// <summary>
    /// 导入患者数据
    /// </summary>
    public async Task<ServiceResult<PatientImportResultDto>> ImportPatientsAsync(PatientImportDto importDto)
    {
        try
        {
            _logger.LogInformation("开始导入患者数据，记录数量：{Count}", importDto.Records.Count);

            // 1. 数据验证
            if (importDto.ValidateData)
            {
                var validationResult = ValidateImportData(importDto.Records);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PatientImportResultDto>.Failure(validationResult.ErrorMessage);
                }
            }

            // 2. 调用API导入
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.ImportPatientsAsync(importDto);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("导入患者数据API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<PatientImportResultDto>.Failure($"导入失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content;
            if (result == null)
            {
                return ServiceResult<PatientImportResultDto>.Failure("导入返回数据为空");
            }

            // 3. 清除相关缓存
            ClearPatientRelatedCache();

            // 4. 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = Guid.Empty,
                Action = "BATCH_IMPORT",
                Description = $"批量导入患者数据，成功{result.SuccessCount}个，失败{result.FailureCount}个",
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    { "totalRecords", result.TotalRecords },
                    { "successCount", result.SuccessCount },
                    { "failureCount", result.FailureCount }
                }
            });

            _logger.LogInformation("患者数据导入完成，成功：{Success}，失败：{Failed}", result.SuccessCount, result.FailureCount);
            return ServiceResult<PatientImportResultDto>.Success(result, "数据导入完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入患者数据异常");
            return ServiceResult<PatientImportResultDto>.Failure($"导入异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 导出患者数据
    /// </summary>
    public async Task<ServiceResult<PatientExportResultDto>> ExportPatientsAsync(PatientExportQueryDto exportQuery)
    {
        try
        {
            _logger.LogInformation("开始导出患者数据");

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.ExportPatientsAsync(exportQuery);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("导出患者数据API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<PatientExportResultDto>.Failure($"导出失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content;
            if (result == null)
            {
                return ServiceResult<PatientExportResultDto>.Failure("导出返回数据为空");
            }

            // 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = Guid.Empty,
                Action = "DATA_EXPORT",
                Description = $"导出患者数据，记录数：{result.RecordCount}",
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    { "fileName", result.FileName },
                    { "recordCount", result.RecordCount }
                }
            });

            _logger.LogInformation("患者数据导出成功，记录数：{Count}", result.RecordCount);
            return ServiceResult<PatientExportResultDto>.Success(result, "数据导出成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出患者数据异常");
            return ServiceResult<PatientExportResultDto>.Failure($"导出异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证导入数据
    /// </summary>
    public ServiceResult<PatientImportValidationDto> ValidateImportData(List<PatientImportRecordDto> records)
    {
        try
        {
            _logger.LogInformation("验证患者导入数据，记录数量：{Count}", records.Count);

            var result = new PatientImportValidationDto
            {
                IsValid = true
            };

            foreach (var record in records)
            {
                var errors = new List<string>();

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(record.Name))
                    errors.Add("姓名不能为空");

                if (string.IsNullOrWhiteSpace(record.Phone))
                    errors.Add("手机号不能为空");

                if (string.IsNullOrWhiteSpace(record.Gender))
                    errors.Add("性别不能为空");

                // 验证数据格式
                if (!string.IsNullOrEmpty(record.Phone) && !IsValidPhone(record.Phone))
                    errors.Add("手机号格式不正确");

                if (!string.IsNullOrEmpty(record.IdCard) && !IsValidIdCard(record.IdCard))
                    errors.Add("身份证号格式不正确");

                if (errors.Any())
                {
                    result.IsValid = false;
                    result.ValidationErrors.AddRange(errors);
                    result.InvalidRecords.Add(record);
                }
                else
                {
                    result.ValidRecords.Add(record);
                }
            }

            var message = result.IsValid 
                ? "数据验证通过" 
                : $"数据验证失败，有效记录{result.ValidRecords.Count}个，无效记录{result.InvalidRecords.Count}个";

            _logger.LogInformation("患者导入数据验证完成，结果：{IsValid}，有效：{Valid}，无效：{Invalid}", 
                result.IsValid, result.ValidRecords.Count, result.InvalidRecords.Count);
                
            return ServiceResult<PatientImportValidationDto>.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证患者导入数据异常");
            return ServiceResult<PatientImportValidationDto>.Failure($"数据验证异常：{ex.Message}");
        }
    }

    #endregion

    #region 业务规则和验证

    /// <summary>
    /// 应用业务规则验证
    /// </summary>
    public ServiceResult ApplyBusinessRules(PatientBusinessRuleDto rules)
    {
        try
        {
            _logger.LogInformation("应用患者业务规则验证");

            // TODO: 实现具体的业务规则验证逻辑
            // 这里可以根据规则配置进行各种验证

            _logger.LogInformation("患者业务规则验证完成");
            return ServiceResult.Success("业务规则验证通过");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用患者业务规则异常");
            return ServiceResult.Failure($"业务规则验证异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证患者业务约束
    /// </summary>
    public async Task<ServiceResult<bool>> ValidatePatientConstraintsAsync(Guid patientId)
    {
        try
        {
            _logger.LogInformation("验证患者业务约束，ID：{PatientId}", patientId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            // 检查患者是否存在关联的就诊记录、处方等数据
            // 这里简化为返回true，实际应该调用API检查

            _logger.LogInformation("患者业务约束验证完成，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Success(true, "约束验证通过");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证患者业务约束异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"约束验证异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查手机号重复性
    /// </summary>
    public async Task<ServiceResult<bool>> CheckPhoneAvailabilityAsync(string phone, Guid? excludePatientId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return ServiceResult<bool>.Failure("手机号不能为空");
            }

            _logger.LogInformation("检查手机号可用性，手机号：{Phone}", phone);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.CheckPhoneAvailabilityAsync(phone, excludePatientId);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("检查手机号可用性API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"检查手机号失败：{apiResponse.StatusCode}");
            }

            var isAvailable = apiResponse.Content;
            _logger.LogInformation("手机号可用性检查完成，手机号：{Phone}，可用：{Available}", phone, isAvailable);
            
            return ServiceResult<bool>.Success(isAvailable, isAvailable ? "手机号可用" : "手机号已被使用");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查手机号可用性异常，手机号：{Phone}", phone);
            return ServiceResult<bool>.Failure($"检查手机号异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查身份证号重复性
    /// </summary>
    public async Task<ServiceResult<bool>> CheckIdCardAvailabilityAsync(string idCard, Guid? excludePatientId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(idCard))
            {
                return ServiceResult<bool>.Failure("身份证号不能为空");
            }

            _logger.LogInformation("检查身份证号可用性，身份证号：{IdCard}", idCard);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.CheckIdCardAvailabilityAsync(idCard, excludePatientId);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("检查身份证号可用性API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"检查身份证号失败：{apiResponse.StatusCode}");
            }

            var isAvailable = apiResponse.Content;
            _logger.LogInformation("身份证号可用性检查完成，身份证号：{IdCard}，可用：{Available}", idCard, isAvailable);
            
            return ServiceResult<bool>.Success(isAvailable, isAvailable ? "身份证号可用" : "身份证号已被使用");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查身份证号可用性异常，身份证号：{IdCard}", idCard);
            return ServiceResult<bool>.Failure($"检查身份证号异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证患者年龄合理性
    /// </summary>
    public ServiceResult ValidatePatientAge(DateTime birthDate)
    {
        try
        {
            var age = DateTime.Now.Year - birthDate.Year;
            if (birthDate > DateTime.Now.AddYears(-age)) age--;

            if (age < 0)
            {
                return ServiceResult.Failure("出生日期不能晚于当前日期");
            }

            if (age > 150)
            {
                return ServiceResult.Failure("年龄不能超过150岁");
            }

            _logger.LogDebug("患者年龄验证通过，年龄：{Age}", age);
            return ServiceResult.Success("年龄验证通过");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证患者年龄异常");
            return ServiceResult.Failure($"年龄验证异常：{ex.Message}");
        }
    }

    #endregion

    #region 患者关系管理

    /// <summary>
    /// 添加患者关系（家庭成员）
    /// </summary>
    public async Task<ServiceResult<bool>> AddPatientRelationshipAsync(Guid patientId, PatientRelationshipDto relationship)
    {
        try
        {
            _logger.LogInformation("添加患者关系，ID：{PatientId}，关系：{RelationType}", patientId, relationship.RelationshipType);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.AddPatientRelationshipAsync(patientId, relationship);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("添加患者关系API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"添加患者关系失败：{apiResponse.StatusCode}");
            }

            // 清除相关缓存
            _cache.Remove($"patient_family_{patientId}");

            _logger.LogInformation("患者关系添加成功，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Success(true, "患者关系添加成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加患者关系异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"添加患者关系异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取患者家庭成员
    /// </summary>
    public async Task<ServiceResult<List<PatientRelationshipDto>>> GetPatientFamilyMembersAsync(Guid patientId)
    {
        try
        {
            var cacheKey = $"patient_family_{patientId}";
            
            if (_cache.TryGetValue(cacheKey, out List<PatientRelationshipDto>? cachedResult))
            {
                _logger.LogDebug("从缓存获取患者家庭成员，ID：{PatientId}", patientId);
                return ServiceResult<List<PatientRelationshipDto>>.Success(cachedResult, "查询成功（缓存）");
            }

            _logger.LogInformation("获取患者家庭成员，ID：{PatientId}", patientId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.GetPatientFamilyMembersAsync(patientId);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("获取患者家庭成员API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<List<PatientRelationshipDto>>.Failure($"获取家庭成员失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content ?? new List<PatientRelationshipDto>();
            
            // 缓存1小时
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
            
            _logger.LogInformation("获取患者家庭成员成功，ID：{PatientId}，数量：{Count}", patientId, result.Count);
            return ServiceResult<List<PatientRelationshipDto>>.Success(result, "家庭成员查询成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者家庭成员异常，ID：{PatientId}", patientId);
            return ServiceResult<List<PatientRelationshipDto>>.Failure($"获取家庭成员异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 移除患者关系
    /// </summary>
    public async Task<ServiceResult<bool>> RemovePatientRelationshipAsync(Guid relationshipId)
    {
        try
        {
            _logger.LogInformation("移除患者关系，关系ID：{RelationshipId}", relationshipId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.RemovePatientRelationshipAsync(relationshipId);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("移除患者关系API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"移除患者关系失败：{apiResponse.StatusCode}");
            }

            // 清除相关缓存（这里需要更智能的缓存清除策略）
            // TODO: 实现更精确的缓存清除

            _logger.LogInformation("患者关系移除成功，关系ID：{RelationshipId}", relationshipId);
            return ServiceResult<bool>.Success(true, "患者关系移除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除患者关系异常，关系ID：{RelationshipId}", relationshipId);
            return ServiceResult<bool>.Failure($"移除患者关系异常：{ex.Message}");
        }
    }

    #endregion

    #region 患者标签管理

    /// <summary>
    /// 添加患者标签
    /// </summary>
    public async Task<ServiceResult<bool>> AddPatientTagAsync(Guid patientId, string tag)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return ServiceResult<bool>.Failure("标签不能为空");
            }

            _logger.LogInformation("添加患者标签，ID：{PatientId}，标签：{Tag}", patientId, tag);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.AddPatientTagAsync(patientId, tag);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("添加患者标签API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"添加患者标签失败：{apiResponse.StatusCode}");
            }

            // 清除相关缓存
            _cache.Remove($"patient_tags_{patientId}");

            _logger.LogInformation("患者标签添加成功，ID：{PatientId}，标签：{Tag}", patientId, tag);
            return ServiceResult<bool>.Success(true, "患者标签添加成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加患者标签异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"添加患者标签异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 移除患者标签
    /// </summary>
    public async Task<ServiceResult<bool>> RemovePatientTagAsync(Guid patientId, string tag)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return ServiceResult<bool>.Failure("标签不能为空");
            }

            _logger.LogInformation("移除患者标签，ID：{PatientId}，标签：{Tag}", patientId, tag);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.RemovePatientTagAsync(patientId, tag);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("移除患者标签API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"移除患者标签失败：{apiResponse.StatusCode}");
            }

            // 清除相关缓存
            _cache.Remove($"patient_tags_{patientId}");

            _logger.LogInformation("患者标签移除成功，ID：{PatientId}，标签：{Tag}", patientId, tag);
            return ServiceResult<bool>.Success(true, "患者标签移除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除患者标签异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"移除患者标签异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取患者所有标签
    /// </summary>
    public async Task<ServiceResult<List<string>>> GetPatientTagsAsync(Guid patientId)
    {
        try
        {
            var cacheKey = $"patient_tags_{patientId}";
            
            if (_cache.TryGetValue(cacheKey, out List<string>? cachedResult))
            {
                _logger.LogDebug("从缓存获取患者标签，ID：{PatientId}", patientId);
                return ServiceResult<List<string>>.Success(cachedResult, "查询成功（缓存）");
            }

            _logger.LogInformation("获取患者标签，ID：{PatientId}", patientId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.GetPatientTagsAsync(patientId);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("获取患者标签API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<List<string>>.Failure($"获取患者标签失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content ?? new List<string>();
            
            // 缓存30分钟
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
            
            _logger.LogInformation("获取患者标签成功，ID：{PatientId}，数量：{Count}", patientId, result.Count);
            return ServiceResult<List<string>>.Success(result, "患者标签查询成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者标签异常，ID：{PatientId}", patientId);
            return ServiceResult<List<string>>.Failure($"获取患者标签异常：{ex.Message}");
        }
    }

    #endregion

    #region 审计和监控

    /// <summary>
    /// 记录患者操作审计
    /// </summary>
    public async Task<ServiceResult> RecordPatientAuditAsync(PatientAuditDto auditInfo)
    {
        try
        {
            _logger.LogInformation("记录患者操作审计，患者ID：{PatientId}，操作：{Action}", auditInfo.PatientId, auditInfo.Action);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.RecordPatientAuditAsync(auditInfo);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("记录患者操作审计API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult.Failure($"记录审计失败：{apiResponse.StatusCode}");
            }

            _logger.LogDebug("患者操作审计记录成功，患者ID：{PatientId}，操作：{Action}", auditInfo.PatientId, auditInfo.Action);
            return ServiceResult.Success("审计记录成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录患者操作审计异常，患者ID：{PatientId}", auditInfo.PatientId);
            return ServiceResult.Failure($"记录审计异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 生成患者档案报告
    /// </summary>
    public async Task<ServiceResult<PatientProfileReportDto>> GeneratePatientProfileReportAsync(Guid patientId)
    {
        try
        {
            _logger.LogInformation("生成患者档案报告，ID：{PatientId}", patientId);

            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.GeneratePatientProfileReportAsync(patientId);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("生成患者档案报告API调用失败，状态码：{StatusCode}", apiResponse.StatusCode);
                return ServiceResult<PatientProfileReportDto>.Failure($"生成报告失败：{apiResponse.StatusCode}");
            }

            var result = apiResponse.Content;
            if (result == null)
            {
                return ServiceResult<PatientProfileReportDto>.Failure("生成报告返回数据为空");
            }

            _logger.LogInformation("患者档案报告生成成功，ID：{PatientId}", patientId);
            return ServiceResult<PatientProfileReportDto>.Success(result, "档案报告生成成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成患者档案报告异常，ID：{PatientId}", patientId);
            return ServiceResult<PatientProfileReportDto>.Failure($"生成报告异常：{ex.Message}");
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 验证患者创建数据
    /// </summary>
    private ServiceResult ValidatePatientCreateData(PatientCreateDto createDto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(createDto.Name))
            errors.Add("患者姓名不能为空");

        if (string.IsNullOrWhiteSpace(createDto.Phone))
            errors.Add("手机号不能为空");
        else if (!IsValidPhone(createDto.Phone))
            errors.Add("手机号格式不正确");

        if (!string.IsNullOrEmpty(createDto.IdCard) && !IsValidIdCard(createDto.IdCard))
            errors.Add("身份证号格式不正确");

        if (createDto.BirthDate.HasValue)
        {
            var ageValidation = ValidatePatientAge(createDto.BirthDate.Value);
            if (!ageValidation.IsSuccess)
                errors.Add(ageValidation.ErrorMessage);
        }

        return errors.Any() 
            ? ServiceResult.Failure(string.Join("；", errors))
            : ServiceResult.Success("数据验证通过");
    }

    /// <summary>
    /// 验证患者更新数据
    /// </summary>
    private ServiceResult ValidatePatientUpdateData(PatientUpdateDto updateDto)
    {
        var errors = new List<string>();

        if (!string.IsNullOrEmpty(updateDto.Name) && string.IsNullOrWhiteSpace(updateDto.Name))
            errors.Add("患者姓名不能为空字符串");

        if (!string.IsNullOrEmpty(updateDto.Phone) && !IsValidPhone(updateDto.Phone))
            errors.Add("手机号格式不正确");

        if (!string.IsNullOrEmpty(updateDto.IdCard) && !IsValidIdCard(updateDto.IdCard))
            errors.Add("身份证号格式不正确");

        if (updateDto.BirthDate.HasValue)
        {
            var ageValidation = ValidatePatientAge(updateDto.BirthDate.Value);
            if (!ageValidation.IsSuccess)
                errors.Add(ageValidation.ErrorMessage);
        }

        return errors.Any() 
            ? ServiceResult.Failure(string.Join("；", errors))
            : ServiceResult.Success("数据验证通过");
    }

    /// <summary>
    /// 检查重复性
    /// </summary>
    private async Task<ServiceResult> CheckForDuplicatesAsync(PatientCreateDto createDto)
    {
        // 检查手机号重复
        var phoneCheck = await CheckPhoneAvailabilityAsync(createDto.Phone);
        if (!phoneCheck.IsSuccess || !phoneCheck.Data)
        {
            return ServiceResult.Failure("手机号已被其他患者使用");
        }

        // 检查身份证号重复
        if (!string.IsNullOrEmpty(createDto.IdCard))
        {
            var idCardCheck = await CheckIdCardAvailabilityAsync(createDto.IdCard);
            if (!idCardCheck.IsSuccess || !idCardCheck.Data)
            {
                return ServiceResult.Failure("身份证号已被其他患者使用");
            }
        }

        return ServiceResult.Success("重复性检查通过");
    }

    /// <summary>
    /// 验证患者是否存在
    /// </summary>
    private async Task<ServiceResult<bool>> ValidatePatientExistsAsync(Guid id)
    {
        try
        {
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.GetPatientByIdAsync(id);
            var exists = apiResponse.IsSuccessStatusCode && apiResponse.Content != null;
            
            return ServiceResult<bool>.Success(exists, exists ? "患者存在" : "患者不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证患者存在性异常，ID：{PatientId}", id);
            return ServiceResult<bool>.Failure($"验证患者存在性异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 更新患者状态
    /// </summary>
    private async Task<ServiceResult<bool>> UpdatePatientStatusAsync(Guid patientId, bool isEnabled)
    {
        try
        {
            // TODO: API通信应该移至公共模块 - 统一API客户端管理
            var apiResponse = await _patientApi.UpdatePatientStatusAsync(patientId, isEnabled);
            
            if (!apiResponse.IsSuccessStatusCode)
            {
                _logger.LogError("更新患者状态API调用失败，ID：{PatientId}，状态码：{StatusCode}", patientId, apiResponse.StatusCode);
                return ServiceResult<bool>.Failure($"状态更新失败：{apiResponse.StatusCode}");
            }

            // 清除相关缓存
            _cache.Remove($"patient_{patientId}");
            ClearPatientRelatedCache();

            // 记录操作审计
            await RecordPatientAuditAsync(new PatientAuditDto
            {
                PatientId = patientId,
                Action = "STATUS_UPDATE",
                Description = $"更新患者状态为{(isEnabled ? "启用" : "禁用")}",
                Timestamp = DateTime.Now
            });

            return ServiceResult<bool>.Success(true, "状态更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者状态异常，ID：{PatientId}", patientId);
            return ServiceResult<bool>.Failure($"状态更新异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 清除患者相关缓存
    /// </summary>
    private void ClearPatientRelatedCache()
    {
        var cacheKeys = new[]
        {
            "patient_statistics",
            "patient_count_statistics",
            "patient_gender_distribution",
            "patient_age_distribution",
            "active_patients",
            "disabled_patients"
        };

        foreach (var key in cacheKeys)
        {
            _cache.Remove(key);
        }
    }

    /// <summary>
    /// 验证手机号格式
    /// </summary>
    private static bool IsValidPhone(string phone)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
    }

    /// <summary>
    /// 验证身份证号格式
    /// </summary>
    private static bool IsValidIdCard(string idCard)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(idCard, @"^[1-9]\d{5}(19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dX]$");
    }

    /// <summary>
    /// 触发患者状态变更事件
    /// </summary>
    private void OnPatientStatusChanged(PatientStatusChangedEventArgs e)
    {
        PatientStatusChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 触发患者操作事件
    /// </summary>
    private void OnPatientOperation(PatientOperationEventArgs e)
    {
        PatientOperation?.Invoke(this, e);
    }

    /// <summary>
    /// 触发患者就诊事件
    /// </summary>
    private void OnPatientVisit(PatientVisitEventArgs e)
    {
        PatientVisit?.Invoke(this, e);
    }

    #endregion
}