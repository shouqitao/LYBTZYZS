using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Helpers
{
    /// <summary>
    /// HerbService业务助手类 - UltraThink Helper模式
    /// 负责复杂业务流程、批量操作和特殊业务逻辑
    /// </summary>
    public class HerbBusinessHelper
    {
        private readonly AppDbContext _context;
        private readonly IHerbRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbBusinessHelper> _logger;
        private readonly HerbValidationHelper _validationHelper;

        public HerbBusinessHelper(
            AppDbContext context, 
            IHerbRepository repository,
            IMapper mapper, 
            ILogger<HerbBusinessHelper> logger,
            HerbValidationHelper validationHelper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            try
            {
                // 验证导入数据
                var validationResult = await _validationHelper.ValidateImportAsync(herbs);
                if (!validationResult.IsSuccess || (validationResult.Data?.Any() == true))
                {
                    var errors = validationResult.Data ?? new List<string> { "验证失败" };
                    return ServiceResult<int>.Failure($"导入验证失败: {string.Join("; ", errors)}");
                }

                // 开始事务
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var models = new List<Herb>();
                    foreach (var dto in herbs)
                    {
                        var model = _mapper.Map<Herb>(dto);
                        model.Id = Guid.NewGuid();
                        model.PinYinCode = _validationHelper.GenerateSimplePinyinCode(model.Name);
                        model.Status = CommonStatus.Enabled;
                        models.Add(model);
                    }

                    var result = await _repository.AddRangeAsync(models);
                    if (!result)
                        return ServiceResult<int>.Failure("批量保存药材失败");

                    await transaction.CommitAsync();
                    _logger.LogInformation("批量导入药材成功: {Count}条", models.Count);
                    return ServiceResult<int>.Success(models.Count);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入药材失败");
                return ServiceResult<int>.Failure("导入药材失败", ex);
            }
        }

        /// <summary>
        /// 批量更新状态（优化版 - 使用ExecuteUpdate）
        /// </summary>
        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        {
            try
            {
                // 验证请求
                var validationResult = await _validationHelper.ValidateBatchStatusUpdateAsync(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<int>.Failure(validationResult.ErrorMessage!);

                // 使用EF Core的ExecuteUpdate进行批量更新，避免加载到内存
                var newStatus = dto.Status ? CommonStatus.Enabled : CommonStatus.Disabled;
                var affectedCount = await _context.Herbs
                    .Where(h => dto.Ids.Contains(h.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(h => h.Status, newStatus));

                var operation = dto.Status ? "启用" : "禁用";
                _logger.LogInformation("批量{Operation}药材成功: 影响{Count}条记录", operation, affectedCount);
                return ServiceResult<int>.Success(affectedCount);
            }
            catch (Exception ex)
            {
                var operation = dto.Status ? "启用" : "禁用";
                _logger.LogError(ex, "批量{Operation}药材失败", operation);
                return ServiceResult<int>.Failure($"批量{operation}药材失败", ex);
            }
        }

        /// <summary>
        /// 批量更新价格
        /// </summary>
        public async Task<ServiceResult<int>> BatchUpdatePriceAsync(List<HerbPriceUpdateDto> updates)
        {
            try
            {
                var successCount = 0;
                var errors = new List<string>();

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var update in updates)
                    {
                        // 验证每个价格更新
                        var validation = await _validationHelper.ValidatePriceUpdateAsync(update.Id, update);
                        if (!validation.IsSuccess)
                        {
                            errors.Add($"ID {update.Id}: {validation.ErrorMessage}");
                            continue;
                        }

                        var herb = await _repository.GetByIdAsync(update.Id);
                        if (herb == null)
                        {
                            errors.Add($"ID {update.Id}: 药材不存在");
                            continue;
                        }

                        // 更新价格
                        if (update.CostPrice.HasValue)
                            herb.CostPrice = update.CostPrice.Value;
                        if (update.Price.HasValue)
                            herb.Price = update.Price.Value;

                        var result = await _repository.UpdateAsync(herb);
                        if (result != null)
                            successCount++;
                        else
                            errors.Add($"ID {update.Id}: 更新失败");
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation("批量更新价格完成: 成功{SuccessCount}条, 失败{ErrorCount}条", 
                        successCount, errors.Count);

                    if (errors.Any())
                    {
                        return ServiceResult<int>.Failure($"批量更新完成，但有{errors.Count}条失败: {string.Join("; ", errors.Take(3))}");
                    }

                    return ServiceResult<int>.Success(successCount);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新价格失败");
                return ServiceResult<int>.Failure("批量更新价格失败", ex);
            }
        }

        /// <summary>
        /// 创建药材并自动生成拼音码
        /// </summary>
        public async Task<ServiceResult<HerbDto>> CreateHerbWithAutoCodeAsync(HerbCreateDto dto)
        {
            try
            {
                // 验证创建请求
                var validation = await _validationHelper.ValidateCreateAsync(dto);
                if (!validation.IsSuccess)
                    return ServiceResult<HerbDto>.Failure(validation.ErrorMessage!);

                var model = _mapper.Map<Herb>(dto);
                model.Id = Guid.NewGuid();
                model.PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode)
                    ? _validationHelper.GenerateSimplePinyinCode(model.Name)
                    : dto.PinYinCode;
                model.Status = CommonStatus.Enabled;

                var result = await _repository.AddAsync(model);
                if (result == null)
                    return ServiceResult<HerbDto>.Failure("新增药材失败");

                var herbDto = _mapper.Map<HerbDto>(model);
                _logger.LogInformation("创建药材成功: {HerbName} (ID: {HerbId})", model.Name, model.Id);
                return ServiceResult<HerbDto>.Success(herbDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建药材失败");
                return ServiceResult<HerbDto>.Failure("新增药材失败", ex);
            }
        }

        /// <summary>
        /// 更新药材价格并记录历史（简化版）
        /// </summary>
        public async Task<ServiceResult<bool>> UpdatePriceWithHistoryAsync(Guid id, HerbPriceUpdateDto dto)
        {
            try
            {
                // 验证价格更新
                var validation = await _validationHelper.ValidatePriceUpdateAsync(id, dto);
                if (!validation.IsSuccess)
                    return ServiceResult<bool>.Failure(validation.ErrorMessage!);

                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                    return ServiceResult<bool>.Failure("药材不存在");

                // 记录原价格（这里简化处理，实际应该保存到价格历史表）
                var oldCostPrice = herb.CostPrice;
                var oldPrice = herb.Price;

                // 更新价格
                if (dto.CostPrice.HasValue)
                    herb.CostPrice = dto.CostPrice.Value;
                if (dto.Price.HasValue)
                    herb.Price = dto.Price.Value;

                var result = await _repository.UpdateAsync(herb);
                if (result == null)
                    return ServiceResult<bool>.Failure("更新价格失败");

                _logger.LogInformation("更新药材价格成功: {HerbName} 成本价 {OldCost}->{NewCost} 销售价 {OldPrice}->{NewPrice}",
                    herb.Name, oldCostPrice, herb.CostPrice, oldPrice, herb.Price);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新价格失败: {Id}", id);
                return ServiceResult<bool>.Failure("更新价格失败", ex);
            }
        }

        /// <summary>
        /// 软删除药材（设置状态为禁用）
        /// </summary>
        public async Task<ServiceResult<bool>> SoftDeleteAsync(Guid id)
        {
            try
            {
                // 验证删除请求
                var validation = await _validationHelper.ValidateDeleteAsync(id);
                if (!validation.IsSuccess)
                    return ServiceResult<bool>.Failure(validation.ErrorMessage!);

                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<bool>.Failure("药材不存在");

                model.Status = CommonStatus.Disabled;

                var result = await _repository.UpdateAsync(model);
                if (result == null)
                    return ServiceResult<bool>.Failure("删除药材失败");

                _logger.LogInformation("软删除药材成功: {HerbName} (ID: {HerbId})", model.Name, id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除药材失败: {Id}", id);
                return ServiceResult<bool>.Failure("删除药材失败", ex);
            }
        }

        /// <summary>
        /// 设置药材启用/禁用状态
        /// </summary>
        public async Task<ServiceResult<bool>> SetStatusAsync(Guid id, bool isActive)
        {
            try
            {
                if (!_validationHelper.IsValidGuid(id))
                    return ServiceResult<bool>.Failure("无效的药材ID");

                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<bool>.Failure("药材不存在");

                var newStatus = isActive ? CommonStatus.Enabled : CommonStatus.Disabled;
                model.Status = newStatus;

                var result = await _repository.UpdateAsync(model);
                if (result == null)
                    return ServiceResult<bool>.Failure("更新药材状态失败");

                var operation = isActive ? "启用" : "禁用";
                _logger.LogInformation("{Operation}药材成功: {HerbName} (ID: {HerbId})", operation, model.Name, id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                var operation = isActive ? "启用" : "禁用";
                _logger.LogError(ex, "{Operation}药材失败: {Id}", operation, id);
                return ServiceResult<bool>.Failure($"{operation}药材失败", ex);
            }
        }

        /// <summary>
        /// 导出药材数据（包含格式化处理）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        {
            try
            {
                var list = await _repository.GetAllAsync();
                var dtos = _mapper.Map<List<HerbDto>>(list);

                // 格式化导出数据（可以添加额外处理逻辑）
                foreach (var dto in dtos)
                {
                    // 确保拼音码不为空
                    if (string.IsNullOrEmpty(dto.PinYinCode))
                    {
                        dto.PinYinCode = _validationHelper.GenerateSimplePinyinCode(dto.Name);
                    }
                }

                _logger.LogInformation("导出药材数据成功: {Count}条", dtos.Count);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据失败");
                return ServiceResult<List<HerbDto>>.Failure("导出药材数据失败", ex);
            }
        }

        /// <summary>
        /// 获取药材业务规则验证结果
        /// </summary>
        public async Task<ServiceResult<Dictionary<string, object>>> GetBusinessRulesAsync(Guid id)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                    return ServiceResult<Dictionary<string, object>>.Failure("药材不存在");

                var rules = new Dictionary<string, object>
                {
                    ["CanDelete"] = herb.Status == CommonStatus.Disabled, // 只能删除已禁用的药材
                    ["CanUpdatePrice"] = herb.Status == CommonStatus.Enabled, // 只能更新启用药材的价格
                    ["IsExpensive"] = herb.Price > 100, // 标记高价药材
                    ["HasValidPinyin"] = !string.IsNullOrEmpty(herb.PinYinCode), // 拼音码完整性
                    ["PriceMargin"] = herb.Price - herb.CostPrice // 价格利润
                };

                return ServiceResult<Dictionary<string, object>>.Success(rules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取业务规则失败: {Id}", id);
                return ServiceResult<Dictionary<string, object>>.Failure("获取业务规则失败", ex);
            }
        }
    }
}