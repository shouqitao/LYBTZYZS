using AutoMapper;
using LYBT.Entities.Formula;
using LYBT.Infrastructure.Data;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Services
{

    /// <summary>
    /// 验方业务服务 - 专注业务规则和复杂操作 (UltraThink重构: <250行)
    /// 职责：复制、分析、分享等业务逻辑
    /// </summary>
    public class FormulaBusinessService : IFormulaBusinessService
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

        #region 从处方创建验方

        /// <summary>
        /// 从处方创建验方
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return ServiceResult<FormulaDto>.Failure("验方名称不能为空");
                }

                // 检查处方是否存在
                var prescription = await _dbContext.Prescriptions
                    .Include(p => p.Items)

                    .FirstOrDefaultAsync(p => p.Id == prescriptionId);

                if (prescription == null)
                {
                    return ServiceResult<FormulaDto>.Failure("处方不存在");
                }

                if (prescription.Items == null || !prescription.Items.Any())
                {
                    return ServiceResult<FormulaDto>.Failure("处方中没有药材信息");
                }

                // 检查验方名称是否重复
                var existingFormula = await _dbContext.Formulas
                    .FirstOrDefaultAsync(f => f.Name == name);
                if (existingFormula != null)
                {
                    return ServiceResult<FormulaDto>.Failure("验方名称已存在");
                }

                // 创建新验方
                var newFormula = new LYBT.Entities.Formula.Formula
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Effect = prescription.Indication ?? "根据处方创建",
                    Usage = prescription.Advice ?? "遵医嘱服用",
                    Property = string.Empty,
                    IsShared = false,
                    Status = CommonStatus.Enabled,
                    Remark = $"基于处方【{prescription.Id.ToString()}】创建",

                    // CreateTime = DateTime.Now, // 实体中无此字段
                    Herbs = new List<LYBT.Entities.Formula.FormulaHerbItem>()
                };

                // 复制处方药材到验方
                foreach (var item in prescription.Items)
                {
                    newFormula.Herbs.Add(new LYBT.Entities.Formula.FormulaHerbItem
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        Usage = item.Usage,

                        // SortOrder属性在FormulaHerbItem中不存在，已移除
                    });
                }

                _dbContext.Formulas.Add(newFormula);
                await _dbContext.SaveChangesAsync();

                // 重新查询以获取完整的验方信息（包含导航属性）
                var createdFormula = await _dbContext.Formulas
                    .Include(f => f.Herbs)

                    .FirstOrDefaultAsync(f => f.Id == newFormula.Id);

                var dto = _mapper.Map<FormulaDto>(createdFormula);

                _logger.LogInformation("从处方创建验方成功: {FormulaName}, 处方ID: {PrescriptionId}", name, prescriptionId);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从处方创建验方失败, 处方ID: {PrescriptionId}, 验方名称: {Name}", prescriptionId, name);
                return ServiceResult<FormulaDto>.Failure($"创建失败: {ex.Message}");
            }
        }

        #endregion 从处方创建验方

        #endregion 验方复制

        #region 验方分享

        public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName)
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

                _logger.LogInformation(
                    "分享验方成功: {FormulaName}, 操作者: {OperatorName} ({OperatorId})",
                    formula.Name, operatorName, operatorId);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分享验方失败, ID: {FormulaId}, 操作者: {OperatorName}", id, operatorName);
                return ServiceResult<bool>.Failure($"分享失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName)
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

                _logger.LogInformation(
                    "取消分享验方成功: {FormulaName}, 操作者: {OperatorName} ({OperatorId})",
                    formula.Name, operatorName, operatorId);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消分享验方失败, ID: {FormulaId}, 操作者: {OperatorName}", id, operatorName);
                return ServiceResult<bool>.Failure($"取消分享失败: {ex.Message}");
            }
        }

        #endregion 验方分享

        #region 验方分析（简化版）

        public async Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
        {
            try
            {
                var formula = await _dbContext.Formulas
                    .Include(f => f.Herbs)

                    .FirstOrDefaultAsync(f => f.Id == formulaId);

                if (formula == null)
                {
                    return ServiceResult<FormulaAnalysisResult>.Failure("验方不存在");
                }

                var analysis = new FormulaAnalysisResult
                {
                    Summary = $"验方【{formula.Name}】共含{formula.Herbs.Count}味药材，复方配伍{DetermineComplexity(formula.Herbs.Count)}",
                    Effects = new List<string>
                    {
                        formula.Effect ?? "功效待完善",
                        $"药材数量: {formula.Herbs.Count}味",
                        $"安全等级: {AssessSafetyLevel(formula.Herbs)}"
                    },
                    Contraindications = new List<string>
                    {
                        "请遵医嘱使用",
                        "孕妇慎用",
                        "如有过敏史请告知医生"
                    },
                    Warnings = new List<HerbCompatibilityWarning>()
                };

                // 检查基本配伍禁忌（简化版）
                var herbNames = formula.Herbs.Select(h => h.HerbName).ToList();
                if (herbNames.Contains("甘草") && herbNames.Contains("甘遂"))
                {
                    analysis.Warnings.Add(new HerbCompatibilityWarning
                    {
                        HerbName1 = "甘草",
                        HerbName2 = "甘遂",
                        WarningLevel = "严重",
                        Description = "甘草与甘遂相反，不宜同用"
                    });
                }

                return ServiceResult<FormulaAnalysisResult>.Success(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析验方失败, ID: {FormulaId}", formulaId);
                return ServiceResult<FormulaAnalysisResult>.Failure($"分析失败: {ex.Message}");
            }
        }

        #endregion 验方分析（简化版）

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
            {
                recommendations.Add("药味较多，建议简化");
            }

            if (string.IsNullOrWhiteSpace(formula.Usage))
            {
                recommendations.Add("建议补充服用方法");
            }

            recommendations.Add("建议记录使用效果");

            return recommendations;
        }

        #endregion 私有分析方法

        #region 基础CRUD操作

        /// <summary>
        /// 创建验方
        /// </summary>
        /// <param name="dto">验方创建数据传输对象</param>
        /// <returns>包含创建的验方的服务结果，失败时返回错误消息</returns>
        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return ServiceResult<FormulaDto>.Failure("验方名称不能为空");
                }

                // 检查名称是否重复
                var existingFormula = await _dbContext.Formulas
                    .FirstOrDefaultAsync(f => f.Name == dto.Name);
                if (existingFormula != null)
                {
                    return ServiceResult<FormulaDto>.Failure("验方名称已存在");
                }

                var formula = _mapper.Map<LYBT.Entities.Formula.Formula>(dto);
                formula.Id = Guid.NewGuid();
                formula.Status = CommonStatus.Enabled;
                formula.IsShared = false;

                _dbContext.Formulas.Add(formula);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("创建验方成功: {FormulaName}", dto.Name);
                var resultDto = _mapper.Map<FormulaDto>(formula);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方失败: {FormulaName}", dto.Name);
                return ServiceResult<FormulaDto>.Failure($"创建验方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新验方
        /// </summary>
        /// <param name="id">验方ID</param>
        /// <param name="dto">验方更新数据传输对象</param>
        /// <returns>包含更新后验方的服务结果，失败时返回错误消息</returns>
        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<FormulaDto>.Failure("验方ID不能为空");
                }

                var formula = await _dbContext.Formulas
                    .Include(f => f.Herbs)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (formula == null)
                {
                    return ServiceResult<FormulaDto>.Failure("验方不存在");
                }

                // 检查名称重复
                if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != formula.Name)
                {
                    var nameExists = await _dbContext.Formulas
                        .AnyAsync(f => f.Name == dto.Name && f.Id != id);
                    if (nameExists)
                    {
                        return ServiceResult<FormulaDto>.Failure("验方名称已存在");
                    }
                    formula.Name = dto.Name;
                }

                // 更新基本信息
                if (!string.IsNullOrWhiteSpace(dto.Effect))
                    formula.Effect = dto.Effect;
                if (!string.IsNullOrWhiteSpace(dto.Usage))
                    formula.Usage = dto.Usage;
                if (!string.IsNullOrWhiteSpace(dto.Remark))
                    formula.Remark = dto.Remark;
                if (!string.IsNullOrWhiteSpace(dto.Instructions))
                    formula.Property = dto.Instructions; // 使用Instructions字段映射到Property

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("更新验方成功: {FormulaName}", formula.Name);
                var resultDto = _mapper.Map<FormulaDto>(formula);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新验方失败，ID: {FormulaId}", id);
                return ServiceResult<FormulaDto>.Failure($"更新验方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除验方
        /// </summary>
        /// <param name="id">验方ID</param>
        /// <returns>表示删除操作成功或失败的服务结果</returns>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("验方ID不能为空");
                }

                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                {
                    return ServiceResult<bool>.Failure("验方不存在");
                }

                // 软删除：设置状态为已删除
                formula.Status = CommonStatus.Disabled;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("删除验方成功: {FormulaName}", formula.Name);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除验方失败，ID: {FormulaId}", id);
                return ServiceResult<bool>.Failure($"删除验方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启用验方
        /// </summary>
        /// <param name="id">验方ID</param>
        /// <returns>表示启用操作成功或失败的服务结果</returns>
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("验方ID不能为空");
                }

                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                {
                    return ServiceResult.Failure("验方不存在");
                }

                formula.Status = CommonStatus.Enabled;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("启用验方成功: {FormulaName}", formula.Name);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用验方失败，ID: {FormulaId}", id);
                return ServiceResult.Failure($"启用验方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 禁用验方
        /// </summary>
        /// <param name="id">验方ID</param>
        /// <returns>表示禁用操作成功或失败的服务结果</returns>
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("验方ID不能为空");
                }

                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                {
                    return ServiceResult.Failure("验方不存在");
                }

                formula.Status = CommonStatus.Disabled;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("禁用验方成功: {FormulaName}", formula.Name);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用验方失败，ID: {FormulaId}", id);
                return ServiceResult.Failure($"禁用验方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换验方状态
        /// </summary>
        /// <param name="id">验方ID</param>
        /// <returns>表示切换操作成功或失败的服务结果</returns>
        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("验方ID不能为空");
                }

                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                {
                    return ServiceResult<bool>.Failure("验方不存在");
                }

                // 切换状态
                formula.Status = formula.Status == CommonStatus.Enabled 
                    ? CommonStatus.Disabled 
                    : CommonStatus.Enabled;
                
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("切换验方状态成功: {FormulaName} -> {Status}", formula.Name, formula.Status);
                return ServiceResult<bool>.Success(formula.Status == CommonStatus.Enabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换验方状态失败，ID: {FormulaId}", id);
                return ServiceResult<bool>.Failure($"切换验方状态失败: {ex.Message}");
            }
        }

        #endregion 基础CRUD操作
    }
}
