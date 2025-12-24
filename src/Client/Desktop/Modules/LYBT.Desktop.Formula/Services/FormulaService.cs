using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.Services
{
    /// <summary>
    /// 配方Service实现
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
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
                return (false, null, "保存配方时发生系统错误，请稍后重试");
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
                return (false, null, "复制配方时发生系统错误，请稍后重试");
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
                return (false, "删除配方时发生系统错误，请稍后重试");
            }
        }

        /// <summary>
        /// 删除配方（简化版，Issue #1787: 兼容返回bool的调用）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid formulaId)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.Delete started - FormulaId={FormulaId}", formulaId);
                await _repository.DeleteAsync(formulaId);
                _logger.LogInformation("[SVC] Formula.Delete completed - FormulaId={FormulaId}", formulaId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Delete failed - FormulaId={FormulaId}", formulaId);
                return false;
            }
        }

        #endregion

        #region 基本CRUD操作

        /// <summary>
        /// 创建配方（Issue #1787: 支持基本创建操作）
        /// </summary>
        public async Task<(bool success, FormulaDetailDto? formula, string? errorMessage)> CreateAsync(FormulaInputDto createDto)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.Create started - Name={FormulaName}", createDto.Name);

                var createdFormula = await _repository.CreateAsync(createDto);
                _logger.LogInformation("[SVC] Formula.Create completed - FormulaId={FormulaId}", createdFormula.Id);

                return (true, createdFormula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Create failed - Name={FormulaName}", createDto.Name);
                return (false, null, "创建配方时发生系统错误");
            }
        }

        /// <summary>
        /// 更新配方（Issue #1787: 支持基本更新操作）
        /// </summary>
        public async Task<(bool success, FormulaDetailDto? formula, string? errorMessage)> UpdateAsync(FormulaInputDto updateDto)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.Update started - FormulaId={FormulaId}", updateDto.Id);

                var updatedFormula = await _repository.UpdateAsync(updateDto);
                _logger.LogInformation("[SVC] Formula.Update completed - Name={FormulaName}", updatedFormula.Name);

                return (true, updatedFormula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Update failed - FormulaId={FormulaId}", updateDto.Id);
                return (false, null, "更新配方时发生系统错误");
            }
        }

        #endregion

        #region 其他操作

        /// <summary>
        /// 打印配方（占位实现）
        /// </summary>
        public Task<(bool success, string? errorMessage)> PrintFormulaAsync(FormulaDetailDto formula)
        {
            _logger.LogDebug("[SVC] Formula.Print - FormulaId={FormulaId} Name={FormulaName}", formula.Id, formula.Name);

            // TODO: 实现打印逻辑
            return Task.FromResult<(bool, string?)>((true, "打印功能开发中"));
        }

        /// <summary>
        /// 分页查询配方（Issue #1787: 支持分页查询）
        /// </summary>
        public async Task<(bool success, PagedResult<FormulaListDto>? data, string? errorMessage)> GetPagedAsync(
            int page, int pageSize, string? searchText = null)
        {
            try
            {
                _logger.LogDebug("[SVC] Formula.GetPaged started - Page={Page} PageSize={PageSize} SearchText={SearchText}",
                    page, pageSize, searchText);

                var result = await _repository.GetPagedAsync(page, pageSize, searchText);

                _logger.LogDebug("[SVC] Formula.GetPaged completed - TotalCount={TotalCount}", result.TotalCount);
                return (true, result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.GetPaged failed");
                return (false, null, "查询配方时发生系统错误");
            }
        }

        /// <summary>
        /// 根据ID获取配方（Issue #1787: 支持单个配方查询）
        /// </summary>
        public async Task<(bool success, FormulaDetailDto? formula, string? errorMessage)> GetByIdAsync(Guid formulaId)
        {
            try
            {
                _logger.LogDebug("[SVC] Formula.GetById started - FormulaId={FormulaId}", formulaId);

                var formula = await _repository.GetByIdAsync(formulaId);

                if (formula == null)
                {
                    _logger.LogWarning("[SVC] Formula.GetById → NotFound - FormulaId={FormulaId}", formulaId);
                    return (false, null, "配方不存在");
                }

                _logger.LogDebug("[SVC] Formula.GetById completed - Name={FormulaName}", formula.Name);
                return (true, formula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.GetById failed - FormulaId={FormulaId}", formulaId);
                return (false, null, "查询配方时发生系统错误");
            }
        }

        /// <summary>
        /// 查看使用历史（占位实现）
        /// </summary>
        public Task<(bool success, string? errorMessage)> ViewUsageHistoryAsync(Guid formulaId)
        {
            _logger.LogDebug("[SVC] Formula.ViewUsageHistory - FormulaId={FormulaId}", formulaId);

            // TODO: 实现查看使用历史逻辑
            return Task.FromResult<(bool, string?)>((true, "查看使用历史功能开发中"));
        }

        /// <summary>
        /// 获取待校验的验方列表（Issue #1787: 支持FormulaValidationViewModel）
        /// </summary>
        public async Task<(bool success, List<FormulaDetailDto>? data, string? errorMessage)> GetPendingValidationFormulasAsync()
        {
            try
            {
                _logger.LogDebug("[SVC] Formula.GetPendingValidation started");

                var formulas = await _repository.GetPendingValidationFormulasAsync();

                _logger.LogDebug("[SVC] Formula.GetPendingValidation completed - Count={Count}", formulas.Count);
                return (true, formulas, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.GetPendingValidation failed");
                return (false, null, "查询待校验验方列表时发生系统错误");
            }
        }

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库（Issue #1787: 支持FormulaValidationViewModel）
        /// </summary>
        public async Task<(bool success, string? errorMessage)> ValidateFormulaHerbAsync(
            Guid formulaId, Guid herbItemId, Guid selectedHerbId)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.ValidateHerb started - FormulaId={FormulaId} HerbItemId={HerbItemId} SelectedHerbId={SelectedHerbId}",
                    formulaId, herbItemId, selectedHerbId);

                var result = await _repository.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

                if (result)
                {
                    _logger.LogInformation("[SVC] Formula.ValidateHerb completed");
                    return (true, null);
                }
                else
                {
                    _logger.LogWarning("[SVC] Formula.ValidateHerb failed");
                    return (false, "配方药材验证失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.ValidateHerb failed");
                return (false, "验证配方药材时发生系统错误");
            }
        }

        #endregion
    }
}
