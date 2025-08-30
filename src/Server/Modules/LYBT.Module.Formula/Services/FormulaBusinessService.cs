using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Formula;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 验方业务服务 - 专注业务规则和复杂操作 (UltraThink重构: <250行)
    /// 职责：复制、分析、分享等业务逻辑
    /// </summary>
    public class FormulaBusinessService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaBusinessService> _logger;

        public FormulaBusinessService(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<FormulaBusinessService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        #region 验方复制

        public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return ServiceResult<FormulaDto>.Failure("新验方名称不能为空");
                }

                var originalFormula = await _dbContext.Formulas
                    .Include(f => f.Herbs)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (originalFormula == null)
                {
                    return ServiceResult<FormulaDto>.Failure("原验方不存在");
                }

                // 检查新名称是否已存在
                var nameExists = await _dbContext.Formulas
                    .AnyAsync(f => f.Name == newName);

                if (nameExists)
                {
                    return ServiceResult<FormulaDto>.Failure($"验方名称'{newName}'已存在");
                }

                // 创建副本
                var copyFormula = new LYBT.Entities.Formula.Formula
                {
                    Id = Guid.NewGuid(),
                    Name = newName,
                    Effect = originalFormula.Effect,
                    Usage = originalFormula.Usage,
                    Remark = originalFormula.Remark,
                    Property = originalFormula.Property,
                    Status = CommonStatus.Enabled,
                    IsShared = false
                };

                // 复制药材组成
                foreach (var originalHerb in originalFormula.Herbs)
                {
                    copyFormula.Herbs.Add(new LYBT.Entities.Formula.FormulaHerbItem
                    {
                        HerbId = originalHerb.HerbId,
                        HerbName = originalHerb.HerbName,
                        Quantity = originalHerb.Quantity,
                        Unit = originalHerb.Unit,
                        Usage = originalHerb.Usage,
                        Remark = originalHerb.Remark
                    });
                }

                _dbContext.Formulas.Add(copyFormula);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("复制验方成功: {OriginalName} -> {NewName}", originalFormula.Name, newName);
                var dto = _mapper.Map<FormulaDto>(copyFormula);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制验方失败, ID: {FormulaId}, 新名称: {NewName}", id, newName);
                return ServiceResult<FormulaDto>.Failure($"复制验方失败: {ex.Message}");
            }
        }

        #endregion

        #region 验方分享

        public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                {
                    return ServiceResult<bool>.Failure("验方不存在");
                }

                // 设置为公开分享
                formula.IsShared = true;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("分享验方成功: {FormulaName}", formula.Name);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分享验方失败, ID: {FormulaId}", id);
                return ServiceResult<bool>.Failure($"分享失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                {
                    return ServiceResult<bool>.Failure("验方不存在");
                }

                // 取消分享
                formula.IsShared = false;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("取消分享验方成功: {FormulaName}", formula.Name);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消分享验方失败, ID: {FormulaId}", id);
                return ServiceResult<bool>.Failure($"取消分享失败: {ex.Message}");
            }
        }

        #endregion

        #region 验方分析（简化版）

        public async Task<ServiceResult<object>> AnalyzeFormulaAsync(Guid formulaId)
        {
            try
            {
                var formula = await _dbContext.Formulas
                    .Include(f => f.Herbs)
                    .FirstOrDefaultAsync(f => f.Id == formulaId);

                if (formula == null)
                {
                    return ServiceResult<object>.Failure("验方不存在");
                }

                var analysis = new
                {
                    FormulaId = formulaId,
                    FormulaName = formula.Name,
                    HerbCount = formula.Herbs.Count,
                    TotalQuantity = formula.Herbs.Sum(fh => fh.Quantity),
                    EstimatedCost = formula.Herbs.Sum(fh => fh.Quantity * 10), // 简化估算
                    Complexity = DetermineComplexity(formula.Herbs.Count),
                    SafetyLevel = AssessSafetyLevel(formula.Herbs),
                    Recommendations = GenerateRecommendations(formula)
                };

                return ServiceResult<object>.Success(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析验方失败, ID: {FormulaId}", formulaId);
                return ServiceResult<object>.Failure($"分析失败: {ex.Message}");
            }
        }

        #endregion

        #region 私有分析方法

        private string DetermineComplexity(int herbCount)
        {
            return herbCount switch
            {
                <= 5 => "简单",
                <= 10 => "中等",
                <= 15 => "复杂",
                _ => "非常复杂"
            };
        }

        private string AssessSafetyLevel(ICollection<FormulaHerbItem> herbs)
        {
            // 简化的安全性评估
            var hasHighRiskHerbs = herbs.Any(fh => 
                fh.HerbName?.Contains("附子") == true ||
                fh.HerbName?.Contains("乌头") == true);

            return hasHighRiskHerbs ? "需要注意" : "相对安全";
        }

        private List<string> GenerateRecommendations(LYBT.Entities.Formula.Formula formula)
        {
            var recommendations = new List<string>();

            if (formula.Herbs.Count > 15)
                recommendations.Add("药味较多，建议简化");

            if (string.IsNullOrWhiteSpace(formula.Usage))
                recommendations.Add("建议补充服用方法");

            recommendations.Add("建议记录使用效果");

            return recommendations;
        }

        #endregion
    }
}