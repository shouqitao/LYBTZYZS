using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Coordinators
{
    /// <summary>
    /// 患者协调器 - UltraThink架构的患者业务协调层
    /// 协调患者相关的所有业务操作，提供统一的业务接口
    /// </summary>
    public class PatientCoordinator : IDataCoordinator<PatientDto, PatientCreateDto, PatientUpdateDto>
    {
        #region Fields

        private readonly IPatientService _patientService;
        private readonly ILogger<PatientCoordinator> _logger;
        private readonly Dictionary<Guid, PatientDto> _cache = new();

        #endregion

        #region Events

        public event EventHandler<DataChangedEventArgs<PatientDto>>? DataChanged;
        public event EventHandler<OperationProgressEventArgs>? OperationProgress;

        #endregion

        #region Constructor

        public PatientCoordinator(IPatientService patientService, ILogger<PatientCoordinator> logger)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Query Operations

        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                _logger.LogInformation("开始分页查询患者，页码: {Page}, 关键字: {Keyword}", query.CurrentPage, query.SearchKeyword);

                // 转换为患者查询DTO
                var patientQuery = new PatientPagedQueryDto
                {
                    PageIndex = query.CurrentPage,
                    PageSize = query.PageSize,
                    Keyword = query.SearchKeyword
                };

                var result = await _patientService.GetPagedAsync(patientQuery);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新缓存
                    foreach (var patient in result.Data.Items)
                    {
                        _cache[patient.Id] = patient;
                    }

                    _logger.LogInformation("患者分页查询成功，返回 {Count} 条记录", result.Data.Items.Count);
                }
                else
                {
                    _logger.LogWarning("患者分页查询失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "患者分页查询异常");
                return ServiceResult<PagedResult<PatientDto>>.Failure($"查询患者失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 先检查缓存
                if (_cache.TryGetValue(id, out var cachedPatient))
                {
                    return ServiceResult<PatientDto>.Success(cachedPatient);
                }

                _logger.LogInformation("根据ID查询患者: {PatientId}", id);

                var result = await _patientService.GetByIdAsync(id);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新缓存
                    _cache[id] = result.Data;
                    _logger.LogInformation("患者查询成功: {PatientName}", result.Data.Name);
                }
                else
                {
                    _logger.LogWarning("患者查询失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID查询患者异常: {PatientId}", id);
                return ServiceResult<PatientDto>.Failure($"查询患者详情失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索患者，关键字: {Keyword}", keyword);

                var query = new PatientPagedQueryDto
                {
                    Keyword = keyword,
                    PageIndex = 1,
                    PageSize = 100 // 搜索返回前100条
                };

                var result = await _patientService.GetPagedAsync(query);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("患者搜索成功，找到 {Count} 条记录", result.Data.Items.Count);
                    return ServiceResult<List<PatientDto>>.Success(result.Data.Items);
                }

                return ServiceResult<List<PatientDto>>.Failure(result.ErrorMessage ?? "搜索患者失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "患者搜索异常");
                return ServiceResult<List<PatientDto>>.Failure($"搜索患者失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<List<PatientDto>>> GetActiveAsync()
        {
            try
            {
                _logger.LogInformation("获取活跃患者列表");

                var query = new PatientPagedQueryDto
                {
                    Status = CommonStatus.Enabled,
                    PageIndex = 1,
                    PageSize = 1000
                };

                var result = await _patientService.GetPagedAsync(query);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("活跃患者查询成功，返回 {Count} 条记录", result.Data.Items.Count);
                    return ServiceResult<List<PatientDto>>.Success(result.Data.Items);
                }

                return ServiceResult<List<PatientDto>>.Failure(result.ErrorMessage ?? "获取活跃患者失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活跃患者异常");
                return ServiceResult<List<PatientDto>>.Failure($"获取活跃患者失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region CRUD Operations

        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto)
        {
            try
            {
                _logger.LogInformation("创建患者: {PatientName}", createDto.Name);

                // 验证数据
                var validationResult = await ValidateAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                var result = await _patientService.CreateAsync(createDto);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新缓存
                    _cache[result.Data.Id] = result.Data;

                    // 触发数据变化事件
                    DataChanged?.Invoke(this, new DataChangedEventArgs<PatientDto>(DataChangeType.Created, result.Data));

                    _logger.LogInformation("患者创建成功: {PatientId}", result.Data.Id);
                }
                else
                {
                    _logger.LogWarning("患者创建失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者异常");
                return ServiceResult<PatientDto>.Failure($"创建患者失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation("更新患者: {PatientId}", id);

                // 验证数据
                var validationResult = await ValidateUpdateAsync(id, updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                var result = await _patientService.UpdateAsync(id, updateDto);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新缓存
                    _cache[id] = result.Data;

                    // 触发数据变化事件
                    DataChanged?.Invoke(this, new DataChangedEventArgs<PatientDto>(DataChangeType.Updated, result.Data));

                    _logger.LogInformation("患者更新成功: {PatientId}", id);
                }
                else
                {
                    _logger.LogWarning("患者更新失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者异常: {PatientId}", id);
                return ServiceResult<PatientDto>.Failure($"更新患者失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("删除患者: {PatientId}", id);

                // 获取患者信息用于事件
                var patient = await GetByIdAsync(id);

                var result = await _patientService.DeleteAsync(id);

                if (result.IsSuccess)
                {
                    // 从缓存中移除
                    _cache.Remove(id);

                    // 触发数据变化事件
                    if (patient.IsSuccess && patient.Data != null)
                    {
                        DataChanged?.Invoke(this, new DataChangedEventArgs<PatientDto>(DataChangeType.Deleted, patient.Data));
                    }

                    _logger.LogInformation("患者删除成功: {PatientId}", id);
                }
                else
                {
                    _logger.LogWarning("患者删除失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者异常: {PatientId}", id);
                return ServiceResult<bool>.Failure($"删除患者失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Status Operations

        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("启用患者: {PatientId}", id);

                var result = await _patientService.EnableAsync(id);

                if (result.IsSuccess)
                {
                    // 清除缓存以强制重新加载
                    _cache.Remove(id);

                    // 重新获取患者信息
                    var updatedPatient = await GetByIdAsync(id);
                    if (updatedPatient.IsSuccess && updatedPatient.Data != null)
                    {
                        DataChanged?.Invoke(this, new DataChangedEventArgs<PatientDto>(DataChangeType.StatusChanged, updatedPatient.Data));
                    }

                    _logger.LogInformation("患者启用成功: {PatientId}", id);
                }
                else
                {
                    _logger.LogWarning("患者启用失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用患者异常: {PatientId}", id);
                return ServiceResult<bool>.Failure($"启用患者失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("禁用患者: {PatientId}", id);

                var result = await _patientService.DisableAsync(id);

                if (result.IsSuccess)
                {
                    // 清除缓存以强制重新加载
                    _cache.Remove(id);

                    // 重新获取患者信息
                    var updatedPatient = await GetByIdAsync(id);
                    if (updatedPatient.IsSuccess && updatedPatient.Data != null)
                    {
                        DataChanged?.Invoke(this, new DataChangedEventArgs<PatientDto>(DataChangeType.StatusChanged, updatedPatient.Data));
                    }

                    _logger.LogInformation("患者禁用成功: {PatientId}", id);
                }
                else
                {
                    _logger.LogWarning("患者禁用失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用患者异常: {PatientId}", id);
                return ServiceResult<bool>.Failure($"禁用患者失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量启用患者，数量: {Count}", ids.Count);

                var result = await _patientService.BatchEnableAsync(ids);

                if (result.IsSuccess)
                {
                    // 清除相关缓存
                    foreach (var id in ids)
                    {
                        _cache.Remove(id);
                    }

                    // 报告进度
                    OperationProgress?.Invoke(this, new OperationProgressEventArgs("批量启用", result.Data, ids.Count, "启用完成"));

                    _logger.LogInformation("批量启用患者成功，处理数量: {Count}", result.Data);
                }
                else
                {
                    _logger.LogWarning("批量启用患者失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用患者异常");
                return ServiceResult<int>.Failure($"批量启用患者失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量禁用患者，数量: {Count}", ids.Count);

                var result = await _patientService.BatchDisableAsync(ids);

                if (result.IsSuccess)
                {
                    // 清除相关缓存
                    foreach (var id in ids)
                    {
                        _cache.Remove(id);
                    }

                    // 报告进度
                    OperationProgress?.Invoke(this, new OperationProgressEventArgs("批量禁用", result.Data, ids.Count, "禁用完成"));

                    _logger.LogInformation("批量禁用患者成功，处理数量: {Count}", result.Data);
                }
                else
                {
                    _logger.LogWarning("批量禁用患者失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用患者异常");
                return ServiceResult<int>.Failure($"批量禁用患者失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Validation

        public async Task<ServiceResult<bool>> ValidateAsync(PatientCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                    return ServiceResult<bool>.Failure("创建数据不能为空");

                if (string.IsNullOrWhiteSpace(createDto.Name))
                    return ServiceResult<bool>.Failure("患者姓名不能为空");

                if (string.IsNullOrWhiteSpace(createDto.IdNumber))
                    return ServiceResult<bool>.Failure("身份证号不能为空");

                // 检查身份证号是否重复
                var existingPatients = await SearchAsync(createDto.IdNumber);
                if (existingPatients.IsSuccess && existingPatients.Data != null && existingPatients.Data.Any())
                {
                    return ServiceResult<bool>.Failure("该身份证号已存在");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者创建数据异常");
                return ServiceResult<bool>.Failure($"数据验证失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<bool>> ValidateUpdateAsync(Guid id, PatientUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    return ServiceResult<bool>.Failure("更新数据不能为空");

                if (string.IsNullOrWhiteSpace(updateDto.Name))
                    return ServiceResult<bool>.Failure("患者姓名不能为空");

                // 检查患者是否存在
                var existingPatient = await GetByIdAsync(id);
                if (!existingPatient.IsSuccess || existingPatient.Data == null)
                {
                    return ServiceResult<bool>.Failure("患者不存在");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者更新数据异常: {PatientId}", id);
                return ServiceResult<bool>.Failure($"数据验证失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region Cache Management

        public async Task RefreshCacheAsync()
        {
            try
            {
                _logger.LogInformation("刷新患者缓存");

                // 获取所有活跃患者并更新缓存
                var activePatients = await GetActiveAsync();
                if (activePatients.IsSuccess && activePatients.Data != null)
                {
                    _cache.Clear();
                    foreach (var patient in activePatients.Data)
                    {
                        _cache[patient.Id] = patient;
                    }

                    _logger.LogInformation("患者缓存刷新完成，缓存 {Count} 条记录", activePatients.Data.Count);

                    // 触发数据刷新事件
                    DataChanged?.Invoke(this, new DataChangedEventArgs<PatientDto>(DataChangeType.Refreshed, activePatients.Data));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新患者缓存异常");
            }
        }

        public void ClearCache()
        {
            _logger.LogInformation("清除患者缓存");
            _cache.Clear();
        }

        #endregion
    }
}