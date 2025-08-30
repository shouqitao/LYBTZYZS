using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Formula;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Services.Core
{
    /// <summary>
    /// 验方服务核心 - 纯粹CRUD操作 (UltraThink扩展友好设计: <300行)
    /// 职责：最基本的增删改查，为扩展提供稳定基础
    /// 设计原则：单一职责、开放封闭、便于测试
    /// </summary>
    public class FormulaServiceCore
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaServiceCore> _logger;

        public FormulaServiceCore(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<FormulaServiceCore> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基础CRUD - 稳定不变的核心功能

        /// <summary>
        /// 根据ID获取验方 - 核心查询功能，不易变化
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<FormulaDto>.Failure("验方ID不能为空");
                }

                var formula = await _dbContext.Formulas
                    .Include(f => f.Herbs)
                    .FirstOrDefaultAsync(f => f.Id == id && f.Status != CommonStatus.Disabled);

                if (formula == null)
                {
                    return ServiceResult<FormulaDto>.Failure("验方不存在");
                }

                var dto = _mapper.Map<FormulaDto>(formula);
                _logger.LogInformation("获取验方成功: {FormulaName} (ID: {FormulaId})", formula.Name, id);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方失败, ID: {FormulaId}", id);
                return ServiceResult<FormulaDto>.Failure($"获取验方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建验方 - 基础创建功能，支持扩展验证
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            try
            {
                // 基础验证
                var validationResult = ValidateForCreate(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<FormulaDto>.Failure(validationResult.ErrorMessage ?? "验证失败");
                }

                var formula = _mapper.Map<LYBT.Entities.Formula.Formula>(dto);
                formula.Id = Guid.NewGuid();
                formula.Status = CommonStatus.Enabled;

                _dbContext.Formulas.Add(formula);
                await _dbContext.SaveChangesAsync();

                var resultDto = _mapper.Map<FormulaDto>(formula);
                _logger.LogInformation("创建验方成功: {FormulaName} (ID: {FormulaId})", formula.Name, formula.Id);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方失败: {FormulaName}", dto.Name);
                return ServiceResult<FormulaDto>.Failure($"创建验方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新验方 - 基础更新功能，支持扩展验证
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            try
            {
                if (id != dto.Id)
                {
                    return ServiceResult<FormulaDto>.Failure("ID不匹配");
                }

                var formula = await _dbContext.Formulas
                    .FirstOrDefaultAsync(f => f.Id == id && f.Status != CommonStatus.Disabled);

                if (formula == null)
                {
                    return ServiceResult<FormulaDto>.Failure("验方不存在");
                }

                // 基础验证
                var validationResult = ValidateForUpdate(id, dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<FormulaDto>.Failure(validationResult.ErrorMessage ?? "验证失败");
                }

                _mapper.Map(dto, formula);
                await _dbContext.SaveChangesAsync();

                var resultDto = _mapper.Map<FormulaDto>(formula);
                _logger.LogInformation("更新验方成功: {FormulaName} (ID: {FormulaId})", formula.Name, id);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新验方失败, ID: {FormulaId}", id);
                return ServiceResult<FormulaDto>.Failure($"更新验方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除验方 - 软删除，基础功能
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                {
                    return ServiceResult<bool>.Failure("验方不存在");
                }

                // 基础约束检查 - 可扩展
                var constraintResult = CheckDeleteConstraints(id);
                if (!constraintResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(constraintResult.ErrorMessage ?? "删除约束检查失败");
                }

                formula.Status = CommonStatus.Disabled;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("删除验方成功: {FormulaName} (ID: {FormulaId})", formula.Name, id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除验方失败, ID: {FormulaId}", id);
                return ServiceResult<bool>.Failure($"删除验方失败: {ex.Message}");
            }
        }

        #endregion

        #region 状态管理 - 基础功能，便于扩展

        /// <summary>
        /// 启用验方
        /// </summary>
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            return await UpdateStatusAsync(id, CommonStatus.Enabled, "启用");
        }

        /// <summary>
        /// 禁用验方
        /// </summary>
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            return await UpdateStatusAsync(id, CommonStatus.Disabled, "禁用");
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                {
                    return ServiceResult<bool>.Failure("验方不存在");
                }

                var newStatus = formula.Status == CommonStatus.Enabled 
                    ? CommonStatus.Disabled 
                    : CommonStatus.Enabled;

                var result = await UpdateStatusAsync(id, newStatus, "切换");
                return result.IsSuccess 
                    ? ServiceResult<bool>.Success(true)
                    : ServiceResult<bool>.Failure(result.ErrorMessage ?? "切换状态失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换验方状态失败, ID: {FormulaId}", id);
                return ServiceResult<bool>.Failure($"切换状态失败: {ex.Message}");
            }
        }

        #endregion

        #region 基础查询支持 - 为扩展查询提供基础

        /// <summary>
        /// 构建基础查询 - 供扩展查询服务使用
        /// </summary>
        public IQueryable<LYBT.Entities.Formula.Formula> BuildBaseQuery()
        {
            return _dbContext.Formulas
                .Include(f => f.Herbs)
                .Where(f => f.Status != CommonStatus.Disabled);
        }

        /// <summary>
        /// 检查验方是否存在 - 基础功能
        /// </summary>
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbContext.Formulas
                .AnyAsync(f => f.Id == id && f.Status != CommonStatus.Disabled);
        }

        /// <summary>
        /// 检查名称是否重复 - 基础验证功能
        /// </summary>
        public async Task<bool> IsNameDuplicatedAsync(string name, Guid? excludeId = null)
        {
            var query = _dbContext.Formulas
                .Where(f => f.Name == name && f.Status != CommonStatus.Disabled);
            
            if (excludeId.HasValue)
            {
                query = query.Where(f => f.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        #endregion

        #region 私有验证方法 - 基础验证，可被扩展

        private ServiceResult ValidateForCreate(FormulaCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return ServiceResult.Failure("验方名称不能为空");
            }

            if (dto.Name.Length > 100)
            {
                return ServiceResult.Failure("验方名称长度不能超过100个字符");
            }

            return ServiceResult.Success();
        }

        private ServiceResult ValidateForUpdate(Guid id, FormulaUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return ServiceResult.Failure("验方名称不能为空");
            }

            if (dto.Name.Length > 100)
            {
                return ServiceResult.Failure("验方名称长度不能超过100个字符");
            }

            return ServiceResult.Success();
        }

        private ServiceResult CheckDeleteConstraints(Guid id)
        {
            // 简化约束检查，实际项目中可以扩展
            return ServiceResult.Success();
        }

        private async Task<ServiceResult> UpdateStatusAsync(Guid id, CommonStatus status, string operation)
        {
            try
            {
                var updated = await _dbContext.Formulas
                    .Where(f => f.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(f => f.Status, status));

                if (updated == 0)
                {
                    return ServiceResult.Failure("验方不存在");
                }

                _logger.LogInformation("{Operation}验方成功, ID: {FormulaId}", operation, id);
                return ServiceResult.Success($"{operation}验方成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Operation}验方失败, ID: {FormulaId}", operation, id);
                return ServiceResult.Failure($"{operation}验方失败: {ex.Message}");
            }
        }

        #endregion
    }
}