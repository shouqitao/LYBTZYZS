using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Services
{
    /// <summary>
    /// 配方Service实现
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// OpenSpec: cleanup-formula-dead-code - 清理未使用的占位方法和FormulaValidation方法
    /// 提供配方CRUD和业务操作的统一处理
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly IFormulaRepository _repository;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(IFormulaRepository repository, ILogger<FormulaService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 保存操作

        /// <summary>
        /// 保存配方
        /// Issue #2149: 优化双重映射，直接接收InputDto以提升性能
        /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[SVC]前缀
        /// </summary>
        public async Task<(bool success, FormulaDetailDto? formula, string? errorMessage)> SaveFormulaAsync(
            FormulaDetailDto currentFormula,
            string formulaName,
            string effect,
            string usage,
            string property,
            string category,
            string remark,
            bool isShared,
            List<FormulaHerbItemInputDto> herbInputDtos)
        {
            try
            {
                var isNewFormula = currentFormula.Id == Guid.Empty;
                _logger.LogInformation("[SVC] Formula.Save started - FormulaId={FormulaId} IsNew={IsNew}", currentFormula.Id, isNewFormula);

                // 验证至少有一味药材
                if (herbInputDtos == null || herbInputDtos.Count == 0)
                {
                    return (false, null, "验方必须包含至少一味中药材");
                }

                var inputDto = new FormulaInputDto
                {
                    Id = currentFormula.Id,
                    Name = formulaName.Trim(),
                    Effect = string.IsNullOrWhiteSpace(effect) ? null! : effect.Trim(),
                    Usage = string.IsNullOrWhiteSpace(usage) ? null! : usage.Trim(),
                    Property = string.IsNullOrWhiteSpace(property) ? null : property.Trim(),
                    Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
                    Remark = string.IsNullOrWhiteSpace(remark) ? null! : remark.Trim(),
                    IsShared = isShared,
                    Herbs = herbInputDtos
                };

                // OpenSpec: implement-formula-copy-flow - 根据Id判断新建或更新
                FormulaDetailDto resultFormula;
                if (isNewFormula)
                {
                    _logger.LogInformation("[SVC] Formula.Create started - Name={Name}", formulaName);
                    resultFormula = await _repository.CreateAsync(inputDto);
                    _logger.LogInformation("[SVC] Formula.Create completed - FormulaId={FormulaId}", resultFormula.Id);
                }
                else
                {
                    _logger.LogInformation("[SVC] Formula.Update started - FormulaId={FormulaId}", currentFormula.Id);
                    resultFormula = await _repository.UpdateAsync(inputDto);
                    _logger.LogInformation("[SVC] Formula.Update completed - FormulaId={FormulaId}", resultFormula.Id);
                }

                return (true, resultFormula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Save failed - FormulaId={FormulaId}", currentFormula.Id);
                return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存配方", ex));
            }
        }

        #endregion

        #region 复制操作

        /// <summary>
        /// 复制配方
        /// </summary>
        public async Task<(bool success, FormulaDetailDto? formula, string? errorMessage)> CopyFormulaAsync(FormulaDetailDto sourceFormula)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.Copy started - SourceId={FormulaId} Name={FormulaName}", sourceFormula.Id, sourceFormula.Name);

                var createDto = new FormulaInputDto
                {
                    Name = $"{sourceFormula.Name}_副本",
                    Effect = sourceFormula.Effect!,
                    Usage = sourceFormula.Usage!,
                    Remark = sourceFormula.Remark!,
                    IsShared = false, // 副本默认不共享
                    Herbs = sourceFormula.Herbs?.Select(h => new FormulaHerbItemInputDto
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Dosage = h.Dosage,
                        Unit = h.Unit,
                        Preparation = h.Preparation,
                        ProcessingMethod = h.ProcessingMethod,
                        Usage = h.Usage,
                        SortOrder = h.SortOrder,
                        DecocteMethod = h.DecocteMethod
                    }).ToList() ?? new List<FormulaHerbItemInputDto>()
                };

                var newFormula = await _repository.CreateAsync(createDto);
                _logger.LogInformation("[SVC] Formula.Copy completed - NewId={FormulaId} Name={FormulaName}", newFormula.Id, newFormula.Name);
                return (true, newFormula, $"配方复制成功！新配方名称：{newFormula.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Copy failed - SourceId={FormulaId}", sourceFormula.Id);
                return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("复制配方", ex));
            }
        }

        #endregion

        #region 删除操作

        /// <summary>
        /// 删除配方
        /// </summary>
        public async Task<(bool success, string? errorMessage)> DeleteFormulaAsync(Guid formulaId)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.Delete started - FormulaId={FormulaId}", formulaId);

                await _repository.DeleteAsync(formulaId);
                _logger.LogInformation("[SVC] Formula.Delete completed - FormulaId={FormulaId}", formulaId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Delete failed - FormulaId={FormulaId}", formulaId);
                return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除配方", ex));
            }
        }

        #endregion

        // OpenSpec: simplify-desktop-data-layer - 已删除基本CRUD操作(CreateAsync/UpdateAsync)，ViewModel直接使用Repository
        // OpenSpec: cleanup-formula-dead-code - 已删除PrintFormulaAsync/ViewUsageHistoryAsync占位方法
        // OpenSpec: cleanup-formula-dead-code - 已删除GetPendingValidationFormulasAsync/ValidateFormulaHerbAsync（FormulaValidationViewModel已删除）
    }
}
