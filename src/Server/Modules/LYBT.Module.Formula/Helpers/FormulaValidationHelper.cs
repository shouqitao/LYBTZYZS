using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Formula;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LYBT.Module.Formula.Helpers
{
    /// <summary>
    /// 验方验证辅助类
    /// 负责所有验方相关的数据验证、业务规则校验
    /// </summary>
    public class FormulaValidationHelper
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<FormulaValidationHelper> _logger;

        public FormulaValidationHelper(
            AppDbContext dbContext, 
            ILogger<FormulaValidationHelper> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 验证验方创建数据
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateCreateAsync(FormulaCreateDto dto)
        {
            try
            {
                var errors = new List<string>();

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(dto.Name))
                    errors.Add("验方名称不能为空");                if (dto.Name?.Length > 100)                    errors.Add("验方名称长度不能超过100个字符");                // 检查名称重复
                if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                    var exists = await _dbContext.Formulas
                        .AnyAsync(f => f.Name == dto.Name && f.Status == CommonStatus.Enabled);
                    
                    if (exists)                        errors.Add("验方名称已存在");                }

                if (errors.Any())                    return ServiceResult<bool>.Failure(string.Join("; ", errors));                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证验方创建数据失败");                return ServiceResult<bool>.Failure("验证失败");            }
        }

        /// <summary>
        /// 验证验方更新数据
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            try
            {
                var errors = new List<string>();

                // 检查验方是否存在
                var exists = await _dbContext.Formulas
                    .AnyAsync(f => f.Id == id && f.Status == CommonStatus.Enabled);
                
                if (!exists)                    errors.Add("验方不存在");                // 验证更新字段
                if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                    if (dto.Name.Length > 100)                        errors.Add("验方名称长度不能超过100个字符");                    // 检查名称重复（排除自己）
                    var nameExists = await _dbContext.Formulas
                        .AnyAsync(f => f.Name == dto.Name && f.Id != id && f.Status == CommonStatus.Enabled);
                    
                    if (nameExists)                        errors.Add("验方名称已存在");                }

                if (dto.Effect?.Length > 200)                    errors.Add("功效描述长度不能超过200个字符");                if (dto.Usage?.Length > 200)                    errors.Add("用法描述长度不能超过200个字符");                if (errors.Any())                    return ServiceResult<bool>.Failure(string.Join("; ", errors));                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证验方更新数据失败: {Id}", id);                return ServiceResult<bool>.Failure("验证失败");            }
        }

        /// <summary>
        /// 验证导入数据
        /// </summary>
        public async Task<ServiceResult<FormulaImportResultDto>> ValidateImportDataAsync(
            List<FormulaImportDto> formulas,
            FormulaImportOptionsDto options)
        {
            try
            {
                var result = new FormulaImportResultDto
                {                    ImportBatch = options.ImportBatch ?? "验证批次",                    TotalCount = formulas.Count,
                    StartTime = DateTime.Now
                };

                var failedItems = new List<FormulaImportErrorDto>();

                for (int i = 0; i < formulas.Count; i++)
                {
                    var importDto = formulas[i];
                    var errors = await ValidateSingleImportDto(importDto, options);

                    if (errors.Any())
                    {
                        failedItems.Add(new FormulaImportErrorDto
                        {
                            RowIndex = i + 1,                            FormulaName = importDto.Name ?? $"第{i + 1}行",                            ErrorMessage = string.Join("; ", errors),                            OriginalData = JsonSerializer.Serialize(importDto)
                        });
                        result.FailedCount++;
                    }
                    else
                    {
                        result.SuccessCount++;
                    }
                }

                result.EndTime = DateTime.Now;
                result.FailedItems = failedItems;

                return ServiceResult<FormulaImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证导入数据异常");                return ServiceResult<FormulaImportResultDto>.Failure($"验证导入数据异常: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 验证单个导入DTO
        /// </summary>
        private async Task<List<string>> ValidateSingleImportDto(FormulaImportDto importDto, FormulaImportOptionsDto options)
        {
            var errors = new List<string>();

            // 验证必填字段
            if (string.IsNullOrWhiteSpace(importDto.Name))                errors.Add("验方名称不能为空");            
            if (importDto.Name?.Length > 100)                errors.Add("验方名称长度不能超过100个字符");            if (importDto.Effect?.Length > 200)                errors.Add("功效描述长度不能超过200个字符");            if (importDto.Usage?.Length > 200)                errors.Add("用法描述长度不能超过200个字符");            // 验证药材信息
            if (importDto.Herbs?.Any() != true)
            {                errors.Add("必须包含至少一味中药材");            }
            else
            {
                foreach (var herb in importDto.Herbs)
                {
                    if (string.IsNullOrWhiteSpace(herb.HerbName))                        errors.Add($"中药材名称不能为空");                    
                    if (herb.Quantity <= 0 || herb.Quantity > 1000)                        errors.Add($"用量必须在0.1-1000之间");                    
                    if (string.IsNullOrWhiteSpace(herb.Unit))                        errors.Add($"用量单位不能为空");                }
            }

            // 检查重复名称
            if (!string.IsNullOrWhiteSpace(importDto.Name))
            {
                var existingFormula = await _dbContext.Formulas
                    .AnyAsync(f => f.Name == importDto.Name && f.Status == CommonStatus.Enabled);
                
                if (existingFormula && !options.SkipDuplicates && !options.UpdateExisting)
                {                    errors.Add("验方名称已存在");                }
            }

            return errors;
        }

        /// <summary>
        /// 验证验方ID是否有效
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateFormulaIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)                    return ServiceResult<bool>.Failure("验方ID不能为空");                var exists = await _dbContext.Formulas
                    .AnyAsync(f => f.Id == id && f.Status == CommonStatus.Enabled);

                if (!exists)                    return ServiceResult<bool>.Failure("验方不存在");                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证验方ID失败: {Id}", id);                return ServiceResult<bool>.Failure("验证失败");            }
        }

        /// <summary>
        /// 验证验方分享权限
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateSharePermissionAsync(Guid formulaId, Guid operatorId)
        {
            try
            {
                var formula = await _dbContext.Formulas
                    .FirstOrDefaultAsync(f => f.Id == formulaId && f.Status == CommonStatus.Enabled);

                if (formula == null)                    return ServiceResult<bool>.Failure("验方不存在");                // TODO: 添加权限验证逻辑
                // 检查操作员是否有权限分享此验方
                // 这里可以根据具体业务规则进行权限判断

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证分享权限失败: FormulaId={FormulaId}, OperatorId={OperatorId}", formulaId, operatorId);                return ServiceResult<bool>.Failure("验证失败");            }
        }

        /// <summary>
        /// 验证复制操作
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateCopyAsync(Guid originalId, string newName)
        {
            try
            {
                var errors = new List<string>();

                // 检查原验方是否存在
                var originalExists = await _dbContext.Formulas
                    .AnyAsync(f => f.Id == originalId && f.Status == CommonStatus.Enabled);
                
                if (!originalExists)                    errors.Add("原验方不存在");                // 验证新名称
                if (string.IsNullOrWhiteSpace(newName))                    errors.Add("新验方名称不能为空");                
                if (newName?.Length > 100)                    errors.Add("新验方名称长度不能超过100个字符");                // 检查新名称是否重复
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    var nameExists = await _dbContext.Formulas
                        .AnyAsync(f => f.Name == newName && f.Status == CommonStatus.Enabled);
                    
                    if (nameExists)                        errors.Add("新验方名称已存在");                }

                if (errors.Any())                    return ServiceResult<bool>.Failure(string.Join("; ", errors));                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证复制操作失败: OriginalId={OriginalId}, NewName={NewName}", originalId, newName);                return ServiceResult<bool>.Failure("验证失败");            }
        }

        /// <summary>
        /// 验证批量操作的ID列表
        /// </summary>
        public async Task<ServiceResult<List<Guid>>> ValidateBatchIdsAsync(List<Guid> ids)
        {
            try
            {
                if (ids?.Any() != true)                    return ServiceResult<List<Guid>>.Failure("ID列表不能为空");                if (ids.Count > 100)                    return ServiceResult<List<Guid>>.Failure("批量操作最多支持100个验方");                // 检查所有ID是否有效
                var validIds = await _dbContext.Formulas
                    .Where(f => ids.Contains(f.Id) && f.Status == CommonStatus.Enabled)
                    .Select(f => f.Id)
                    .ToListAsync();

                var invalidIds = ids.Except(validIds).ToList();
                if (invalidIds.Any())
                {                    _logger.LogWarning("发现无效的验方ID: {InvalidIds}", string.Join(", ", invalidIds));                }

                return ServiceResult<List<Guid>>.Success(validIds);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证批量ID列表失败");                return ServiceResult<List<Guid>>.Failure("验证失败", ex);
            }
        }
    }
}


