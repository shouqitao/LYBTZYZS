using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.ViewModels.Components
{
    /// <summary>
    /// 配方命令处理器 - 组件化架构实现
    /// Issue #1153: 负责配方的命令操作（保存、复制、打印等）
    /// </summary>
    public class FormulaCommandHandler
    {
        private readonly IFormulaRepository _repository;
        private readonly ILogger _logger;

        public FormulaCommandHandler(IFormulaRepository repository, ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 保存操作

        /// <summary>
        /// 保存配方
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> SaveFormulaAsync(
            FormulaDto currentFormula,
            string formulaName,
            string effect,
            string usage,
            string remark,
            bool isShared,
            IEnumerable<FormulaHerbItemDto> herbItems)
        {
            try
            {
                _logger.LogInformation("保存配方: {FormulaId}", currentFormula.Id);

                var updateDto = new FormulaUpdateDto
                {
                    Id = currentFormula.Id,
                    Name = formulaName.Trim(),
                    Effect = string.IsNullOrWhiteSpace(effect) ? null! : effect.Trim(),
                    Usage = string.IsNullOrWhiteSpace(usage) ? null! : usage.Trim(),
                    Remark = string.IsNullOrWhiteSpace(remark) ? null! : remark.Trim(),
                    IsShared = isShared,
                    Herbs = herbItems.Select(h => new FormulaHerbItemUpdateDto
                    {
                        Id = h.Id,
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        Preparation = h.Preparation,
                        Usage = h.Usage,
                        SortOrder = h.SortOrder
                    }).ToList()
                };

                var updatedFormula = await _repository.UpdateAsync(updateDto);
                return (true, updatedFormula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配方时发生异常: {FormulaId}", currentFormula.Id);
                return (false, null, "保存配方时发生系统错误，请稍后重试");
            }
        }

        #endregion

        #region 复制操作

        /// <summary>
        /// 复制配方
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> CopyFormulaAsync(FormulaDto sourceFormula)
        {
            try
            {
                _logger.LogInformation("复制配方: {FormulaId} ({FormulaName})", sourceFormula.Id, sourceFormula.Name);

                var createDto = new FormulaCreateDto
                {
                    Name = $"{sourceFormula.Name}_副本",
                    Effect = sourceFormula.Effect!,
                    Usage = sourceFormula.Usage!,
                    Remark = sourceFormula.Remark!,
                    IsShared = false, // 副本默认不共享
                    Herbs = sourceFormula.Herbs?.Select(h => new FormulaHerbItemCreateDto
                    {
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        Preparation = h.Preparation,
                        Usage = h.Usage,
                        SortOrder = h.SortOrder
                    }).ToList() ?? new List<FormulaHerbItemCreateDto>()
                };

                var newFormula = await _repository.CreateAsync(createDto);
                return (true, newFormula, $"配方复制成功！新配方名称：{newFormula.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制配方时发生异常: {FormulaId}", sourceFormula.Id);
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
                _logger.LogInformation("删除配方: {FormulaId}", formulaId);

                await _repository.DeleteAsync(formulaId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除配方时发生异常: {FormulaId}", formulaId);
                return (false, "删除配方时发生系统错误，请稍后重试");
            }
        }

        #endregion

        #region 其他操作

        /// <summary>
        /// 打印配方（占位实现）
        /// </summary>
        public Task<(bool success, string? errorMessage)> PrintFormulaAsync(FormulaDto formula)
        {
            _logger.LogInformation("打印配方: {FormulaId} ({FormulaName})", formula.Id, formula.Name);

            // TODO: 实现打印逻辑
            return Task.FromResult((true, "打印功能开发中"));
        }

        /// <summary>
        /// 查看使用历史（占位实现）
        /// </summary>
        public Task<(bool success, string? errorMessage)> ViewUsageHistoryAsync(Guid formulaId)
        {
            _logger.LogInformation("查看配方使用历史: {FormulaId}", formulaId);

            // TODO: 实现查看使用历史逻辑
            return Task.FromResult((true, "查看使用历史功能开发中"));
        }

        #endregion
    }
}