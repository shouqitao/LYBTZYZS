using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Coordinators
{
    /// <summary>
    /// 患者协调器 - UltraThink v2.0简化版本
    /// 协调患者相关的业务操作协调
    /// </summary>
    public class PatientCoordinator
    {
        #region Fields

        private readonly PatientModule _patientService;
        private readonly ILogger<PatientCoordinator> _logger;
        private readonly Dictionary<Guid, PatientDto> _cache = new();

        #endregion

        #region Events

        // UltraThink v2.0: 简化事件系统
        public event EventHandler<PatientDto>? PatientChanged;
        public event EventHandler<string>? OperationProgress;

        #endregion

        #region Constructor

        public PatientCoordinator(PatientModule patientService, ILogger<PatientCoordinator> logger)
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

                // 转换为PatientPagedQueryDto
                var patientQuery = new PatientPagedQueryDto
                {
                    Keyword = query.Keyword,
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    SortField = query.SortField,
                    IsDescending = query.IsDescending
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
                return ServiceResult<PagedResult<PatientDto>>.Failure($"查询患者失败: {ex.Message}");
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
                    // UltraThink v2.0: 直接使用统一的PatientDto，无需转换
                    // 更新缓存
                    _cache[id] = result.Data;
                    _logger.LogInformation("患者查询成功: {PatientName}", result.Data.Name);
                    
                    return ServiceResult<PatientDto>.Success(result.Data);
                }
                else
                {
                    _logger.LogWarning("患者查询失败: {Error}", result.ErrorMessage);
                }

                return ServiceResult<PatientDto>.Failure(result.ErrorMessage ?? "获取患者信息失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID查询患者异常: {PatientId}", id);
                return ServiceResult<PatientDto>.Failure($"查询患者详情失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<PatientDto>>> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索患者，关键字: {Keyword}", keyword);

                var result = await _patientService.SearchByKeywordAsync(keyword);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("患者搜索成功，找到 {Count} 条记录", result.Data.Count());
                    return result;
                }

                return ServiceResult<IEnumerable<PatientDto>>.Failure(result.ErrorMessage ?? "搜索患者失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "患者搜索异常");
                return ServiceResult<IEnumerable<PatientDto>>.Failure($"搜索患者失败: {ex.Message}");
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

                    // UltraThink v2.0: 简化事件通知
                    PatientChanged?.Invoke(this, result.Data);

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
                return ServiceResult<PatientDto>.Failure($"创建患者失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PatientDto>> UpdateAsync(PatientUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation("更新患者: {PatientId}", updateDto.Id);

                // 验证数据
                var validationResult = await ValidateUpdateAsync(updateDto.Id, updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                var result = await _patientService.UpdateAsync(updateDto.Id, updateDto);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新缓存
                    _cache[updateDto.Id] = result.Data;

                    // UltraThink v2.0: 简化事件通知
                    PatientChanged?.Invoke(this, result.Data);

                    _logger.LogInformation("患者更新成功: {PatientId}", updateDto.Id);
                }
                else
                {
                    _logger.LogWarning("患者更新失败: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者异常: {PatientId}", updateDto.Id);
                return ServiceResult<PatientDto>.Failure($"更新患者失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
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

                    // UltraThink v2.0: 简化事件通知
                    if (patient.IsSuccess && patient.Data != null)
                    {
                        PatientChanged?.Invoke(this, patient.Data);
                    }

                    _logger.LogInformation("患者删除成功: {PatientId}", id);
                    return ServiceResult.Success();
                }
                else
                {
                    _logger.LogWarning("患者删除失败: {Error}", result.ErrorMessage);
                    return ServiceResult.Failure(result.ErrorMessage ?? "删除患者失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者异常: {PatientId}", id);
                return ServiceResult.Failure($"删除患者失败: {ex.Message}");
            }
        }

        #endregion

        #region Status Operations

        public async Task<ServiceResult> EnableAsync(Guid id)
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
                        PatientChanged?.Invoke(this, updatedPatient.Data);
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
                return ServiceResult.Failure($"启用患者失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DisableAsync(Guid id)
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
                        PatientChanged?.Invoke(this, updatedPatient.Data);
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
                return ServiceResult.Failure($"禁用患者失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量启用患者，数量: {Count}", ids.Count);

                // UltraThink v2.0: 使用循环调用单个操作替代批量操作（简化原则）
                int successCount = 0;
                foreach (var id in ids)
                {
                    var result = await _patientService.EnableAsync(id);
                    if (result.IsSuccess)
                    {
                        successCount++;
                        _cache.Remove(id); // 清除缓存
                    }
                }

                // 报告进度
                OperationProgress?.Invoke(this, $"已启用 {successCount}/{ids.Count} 个患者");

                _logger.LogInformation("批量启用患者完成，成功数量: {Count}", successCount);
                return ServiceResult<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用患者异常");
                return ServiceResult<int>.Failure($"批量启用患者失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量禁用患者，数量: {Count}", ids.Count);

                // UltraThink v2.0: 使用循环调用单个操作替代批量操作（简化原则）
                int successCount = 0;
                foreach (var id in ids)
                {
                    var result = await _patientService.DisableAsync(id);
                    if (result.IsSuccess)
                    {
                        successCount++;
                        _cache.Remove(id); // 清除缓存
                    }
                }

                // 报告进度
                OperationProgress?.Invoke(this, $"已禁用 {successCount}/{ids.Count} 个患者");

                _logger.LogInformation("批量禁用患者完成，成功数量: {Count}", successCount);
                return ServiceResult<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用患者异常");
                return ServiceResult<int>.Failure($"批量禁用患者失败: {ex.Message}");
            }
        }

        #endregion

        #region Validation

        public async Task<ServiceResult> ValidateAsync(PatientCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                    return ServiceResult.Failure("创建数据不能为空");

                if (string.IsNullOrWhiteSpace(createDto.Name))
                    return ServiceResult.Failure("患者姓名不能为空");

                if (string.IsNullOrWhiteSpace(createDto.IdNumber))
                    return ServiceResult.Failure("身份证号不能为空");

                // 检查身份证号是否重复
                var existingPatients = await SearchAsync(createDto.IdNumber);
                if (existingPatients.IsSuccess && existingPatients.Data != null && existingPatients.Data.Any())
                {
                    return ServiceResult.Failure("该身份证号已存在");
                }

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者创建数据异常");
                return ServiceResult.Failure($"数据验证失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> ValidateUpdateAsync(Guid id, PatientUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    return ServiceResult.Failure("更新数据不能为空");

                if (string.IsNullOrWhiteSpace(updateDto.Name))
                    return ServiceResult.Failure("患者姓名不能为空");

                // 检查患者是否存在
                var existingPatient = await GetByIdAsync(id);
                if (!existingPatient.IsSuccess || existingPatient.Data == null)
                {
                    return ServiceResult.Failure("患者不存在");
                }

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者更新数据异常: {PatientId}", id);
                return ServiceResult.Failure($"数据验证失败: {ex.Message}");
            }
        }

        #endregion

        #region Cache Management

        public void ClearCache()
        {
            _logger.LogInformation("清除患者缓存");
            _cache.Clear();
        }

        #endregion
    }
}