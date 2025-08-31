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
using LYBT.Shared.Models.Base;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Helpers
{
    /// <summary>
    /// HerbService验证助手类 - UltraThink Helper模式
    /// 负责所有业务验证、规则检查和数据一致性逻辑
    /// </summary>
    public class HerbValidationHelper : BaseValidationHelper
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbValidationHelper> _logger;

        public HerbValidationHelper(AppDbContext context, IMapper mapper, ILogger<HerbValidationHelper> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 验证创建药材请求
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateCreateAsync(HerbCreateDto dto)
        {
            try
            {
                // 基础字段验证
                var nameValidation = ValidateRequiredString(dto.Name, "药材名称");
                if (!nameValidation.IsSuccess) return nameValidation;
                
                var priceValidation = ValidatePositiveNumber(dto.Price, "药材价格", allowZero: true);
                if (!priceValidation.IsSuccess) return priceValidation;
                
                var unitValidation = ValidateRequiredString(dto.Unit, "药材单位");
                if (!unitValidation.IsSuccess) return unitValidation;
                
                // 检查药材名称是否重复
                var exists = await _context.Herbs
                    .AnyAsync(h => h.Name == dto.Name && h.Status == CommonStatus.Enabled);
                
                if (exists)
                    return ServiceResult<bool>.Failure($"药材'{dto.Name}'已存在");
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证创建药材请求失败");
                return ServiceResult<bool>.Failure("验证创建请求失败");
            }
        }

        /// <summary>
        /// 验证更新药材请求
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUpdateAsync(Guid id, HerbUpdateDto dto)
        {
            try
            {
                // 基础字段验证
                var nameValidation = ValidateRequiredString(dto.Name, "药材名称");
                if (!nameValidation.IsSuccess) return nameValidation;
                
                var priceValidation = ValidatePositiveNumber(dto.Price, "药材价格", allowZero: true);
                if (!priceValidation.IsSuccess) return priceValidation;
                
                var unitValidation = ValidateRequiredString(dto.Unit, "药材单位");
                if (!unitValidation.IsSuccess) return unitValidation;
                
                // 检查药材是否存在
                var exists = await _context.Herbs.AnyAsync(h => h.Id == id);
                if (!exists)
                    return ServiceResult<bool>.Failure("要更新的药材不存在");
                
                // 检查名称是否与其他药材重复（排除自身）
                var nameExists = await _context.Herbs
                    .AnyAsync(h => h.Name == dto.Name && h.Id != id && h.Status == CommonStatus.Enabled);
                
                if (nameExists)
                    return ServiceResult<bool>.Failure($"药材名称'{dto.Name}'已被其他药材使用");
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证更新药材请求失败: {Id}", id);
                return ServiceResult<bool>.Failure("验证更新请求失败");
            }
        }

        /// <summary>
        /// 验证删除药材请求
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateDeleteAsync(Guid id)
        {
            try
            {
                // 检查药材是否存在
                var herb = await _context.Herbs.FirstOrDefaultAsync(h => h.Id == id);
                if (herb == null)
                    return ServiceResult<bool>.Failure("要删除的药材不存在");

                // 检查是否已被处方引用（这里简化处理，实际应检查处方表）
                // 注意：由于Prescription.Herbs导航属性不存在，暂时跳过此检查
                // var isUsedInPrescription = await _context.Prescriptions
                //     .AnyAsync(p => p.Herbs.Any(h => h.HerbId == id));
                // 
                // if (isUsedInPrescription)
                //     return ServiceResult<bool>.Failure("该药材已被处方引用，无法删除");

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证删除药材请求失败: {Id}", id);
                return ServiceResult<bool>.Failure("验证删除请求失败");
            }
        }

        /// <summary>
        /// 验证批量导入药材
        /// </summary>
        public async Task<ServiceResult<List<string>>> ValidateImportAsync(List<HerbImportDto> herbs)
        {
            try
            {
                var errors = new List<string>();

                if (herbs == null || herbs.Count == 0)
                {
                    errors.Add("导入的药材列表为空");
                    return ServiceResult<List<string>>.Success(errors);
                }

                // 获取现有药材名称列表
                var existingNames = await _context.Herbs
                    .Where(h => h.Status == CommonStatus.Enabled)
                    .Select(h => h.Name)
                    .ToListAsync();

                // 验证每个药材
                for (int i = 0; i < herbs.Count; i++)
                {
                    var herb = herbs[i];
                    var rowNumber = i + 1;

                    // 基础验证
                    if (string.IsNullOrWhiteSpace(herb.Name))
                        errors.Add($"第{rowNumber}行：药材名称不能为空");
                    
                    if (herb.Price < 0)
                        errors.Add($"第{rowNumber}行：药材价格不能为负数");
                    
                    if (string.IsNullOrWhiteSpace(herb.Unit))
                        errors.Add($"第{rowNumber}行：药材单位不能为空");
                    
                    // 重复性检查
                    if (existingNames.Contains(herb.Name))
                        errors.Add($"第{rowNumber}行：药材'{herb.Name}'已存在");
                    
                    // 导入列表内部重复检查
                    var duplicateInBatch = herbs.Take(i).Any(h => h.Name == herb.Name);
                    if (duplicateInBatch)
                        errors.Add($"第{rowNumber}行：药材'{herb.Name}'在导入列表中重复");
                }

                return ServiceResult<List<string>>.Success(errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证导入药材失败");
                return ServiceResult<List<string>>.Failure("验证导入失败", ex);
            }
        }

        /// <summary>
        /// 验证价格更新
        /// </summary>
        public async Task<ServiceResult<bool>> ValidatePriceUpdateAsync(Guid id, HerbPriceUpdateDto dto)
        {
            try
            {
                // 检查药材是否存在
                var exists = await _context.Herbs.AnyAsync(h => h.Id == id);
                if (!exists)
                    return ServiceResult<bool>.Failure("要更新价格的药材不存在");
                
                // 价格验证
                if (dto.CostPrice.HasValue)
                {
                    var costPriceValidation = ValidatePositiveNumber(dto.CostPrice.Value, "成本价", allowZero: true);
                    if (!costPriceValidation.IsSuccess) return costPriceValidation;
                }
                
                if (dto.Price.HasValue)
                {
                    var priceValidation = ValidatePositiveNumber(dto.Price.Value, "销售价", allowZero: true);
                    if (!priceValidation.IsSuccess) return priceValidation;
                }
                
                // 成本价不应高于销售价的业务规则验证
                if (dto.CostPrice.HasValue && dto.Price.HasValue && dto.CostPrice.Value > dto.Price.Value)
                    return ServiceResult<bool>.Failure("成本价不能高于销售价");
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证价格更新失败: {Id}", id);
                return ServiceResult<bool>.Failure("验证价格更新失败");
            }
        }

        /// <summary>
        /// 验证批量状态更新
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateBatchStatusUpdateAsync(BatchStatusUpdateDto dto)
        {
            try
            {
                if (dto.Ids == null || dto.Ids.Count == 0)
                    return ServiceResult<bool>.Failure("要更新状态的药材ID列表为空");
                
                // 检查所有ID是否有效
                var existingIds = await _context.Herbs
                    .Where(h => dto.Ids.Contains(h.Id))
                    .Select(h => h.Id)
                    .ToListAsync();

                var invalidIds = dto.Ids.Except(existingIds).ToList();
                if (invalidIds.Any())
                {
                    return ServiceResult<bool>.Failure($"以下药材ID不存在: {string.Join(", ", invalidIds)}");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证批量状态更新失败");
                return ServiceResult<bool>.Failure("验证批量状态更新失败");
            }
        }

        /// <summary>
        /// 生成简单拼音码
        /// </summary>
        public string GenerateSimplePinyinCode(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // 简化实现：只取第一个字符的大写
            return name.Substring(0, Math.Min(name.Length, 1)).ToUpperInvariant();
        }

        /// <summary>
        /// 验证分页查询参数
        /// </summary>
        public ServiceResult<bool> ValidatePagedQuery(HerbPagedQueryDto query)
        {
            try
            {
                if (query.PageIndex < 1)
                    return ServiceResult<bool>.Failure("页码必须大于0");
                
                if (query.PageSize < 1)
                    return ServiceResult<bool>.Failure("页大小必须大于0");
                
                if (query.PageSize > 100)
                    return ServiceResult<bool>.Failure("页大小不能超过100");
                
                if (query.MinPrice.HasValue && query.MinPrice.Value < 0)
                    return ServiceResult<bool>.Failure("最低价格不能为负数");
                
                if (query.MaxPrice.HasValue && query.MaxPrice.Value < 0)
                    return ServiceResult<bool>.Failure("最高价格不能为负数");
                
                if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice.Value > query.MaxPrice.Value)
                    return ServiceResult<bool>.Failure("最低价格不能高于最高价格");
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证分页查询参数失败");
                return ServiceResult<bool>.Failure("验证查询参数失败");
            }
        }

        // 基类已提供ValidateGuid方法，此处删除重复定义

        /// <summary>
        /// 验证字符串是否为有效搜索关键词
        /// </summary>
        public bool IsValidSearchKeyword(string? keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) && keyword.Trim().Length >= 1;
        }
    }
}