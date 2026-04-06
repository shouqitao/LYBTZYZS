using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.Formula.Services
{
    /// <summary>
    /// 配方Service实现
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// OpenSpec: cleanup-formula-dead-code - 清理未使用的占位方法和FormulaValidation方法
    /// 使用 CommandResult&lt;T&gt; 统一返回类型，遵循 RemoteUserService 金标准模式
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

        #region 查询操作

        public async Task<CommandResult<FormulaDetailDto>> GetByIdAsync(Guid formulaId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] Formula.GetById - FormulaId={FormulaId}", formulaId);

                var formula = await _repository.GetByIdAsync(formulaId);
                if (formula == null)
                    return CommandResult<FormulaDetailDto>.NotFound("验方不存在");

                return CommandResult<FormulaDetailDto>.Succeeded(formula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.GetById failed - FormulaId={FormulaId}", formulaId);
                return CommandResult<FormulaDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取验方", ex));
            }
        }

        public async Task<CommandResult<PagedResult<FormulaListDto>>> GetPagedAsync(
            int page, int pageSize, string? keyword = null, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] Formula.GetPaged - Page={Page}, PageSize={PageSize}, Keyword={Keyword}",
                    page, pageSize, keyword);

                var result = await _repository.GetPagedAsync(page, pageSize, keyword);
                return CommandResult<PagedResult<FormulaListDto>>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.GetPaged failed - Page={Page}, Keyword={Keyword}", page, keyword);
                return CommandResult<PagedResult<FormulaListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("分页查询验方", ex));
            }
        }

        #endregion

        #region 保存操作

        public async Task<CommandResult<FormulaDetailDto>> SaveFormulaAsync(
            FormulaDetailDto currentFormula,
            string formulaName,
            string effect,
            string usage,
            string property,
            string category,
            string remark,
            bool isShared,
            List<FormulaHerbItemInputDto> herbInputDtos,
            CancellationToken ct = default)
        {
            try
            {
                var isNewFormula = currentFormula.Id == Guid.Empty;
                _logger.LogInformation("[SVC] Formula.Save started - FormulaId={FormulaId} IsNew={IsNew}", currentFormula.Id, isNewFormula);

                if (herbInputDtos == null || herbInputDtos.Count == 0)
                {
                    return CommandResult<FormulaDetailDto>.Failed("验方必须包含至少一味中药材");
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

                return CommandResult<FormulaDetailDto>.Succeeded(resultFormula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Save failed - FormulaId={FormulaId}", currentFormula.Id);
                return CommandResult<FormulaDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存配方", ex));
            }
        }

        #endregion

        #region 复制操作

        public async Task<CommandResult<FormulaDetailDto>> CopyFormulaAsync(FormulaDetailDto sourceFormula, CancellationToken ct = default)
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
                    IsShared = false,
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
                return CommandResult<FormulaDetailDto>.Succeeded(newFormula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Copy failed - SourceId={FormulaId}", sourceFormula.Id);
                return CommandResult<FormulaDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("复制配方", ex));
            }
        }

        #endregion

        #region 删除操作

        public async Task<CommandResult<bool>> DeleteFormulaAsync(Guid formulaId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.Delete started - FormulaId={FormulaId}", formulaId);

                await _repository.DeleteAsync(formulaId);
                _logger.LogInformation("[SVC] Formula.Delete completed - FormulaId={FormulaId}", formulaId);
                return CommandResult<bool>.Succeeded(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Delete failed - FormulaId={FormulaId}", formulaId);
                return CommandResult<bool>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除配方", ex));
            }
        }

        #endregion

        #region 状态管理

        public async Task<CommandResult<FormulaDetailDto>> ToggleStatusAsync(Guid formulaId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.ToggleStatus started - FormulaId={FormulaId}", formulaId);

                var formula = await _repository.ToggleStatusAsync(formulaId);
                if (formula == null)
                    return CommandResult<FormulaDetailDto>.NotFound("验方不存在");

                _logger.LogInformation("[SVC] Formula.ToggleStatus completed - FormulaId={FormulaId}, Status={Status}",
                    formulaId, formula.Status);
                return CommandResult<FormulaDetailDto>.Succeeded(formula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.ToggleStatus failed - FormulaId={FormulaId}", formulaId);
                return CommandResult<FormulaDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("切换验方状态", ex));
            }
        }

        public async Task<CommandResult<FormulaDetailDto>> RestoreAsync(Guid formulaId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.Restore started - FormulaId={FormulaId}", formulaId);

                var formula = await _repository.RestoreAsync(formulaId);
                if (formula == null)
                    return CommandResult<FormulaDetailDto>.NotFound("验方不存在或未被删除");

                _logger.LogInformation("[SVC] Formula.Restore completed - FormulaId={FormulaId}", formulaId);
                return CommandResult<FormulaDetailDto>.Succeeded(formula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.Restore failed - FormulaId={FormulaId}", formulaId);
                return CommandResult<FormulaDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("恢复验方", ex));
            }
        }

        #endregion

        #region 批量操作

        public async Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> formulaIds, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.BatchDelete started - Count={Count}", formulaIds.Count);

                var result = await _repository.BatchDeleteAsync(formulaIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量删除验方返回空结果");

                _logger.LogInformation("[SVC] Formula.BatchDelete completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.BatchDelete failed - Count={Count}", formulaIds.Count);
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量删除验方", ex));
            }
        }

        public async Task<CommandResult<BatchOperationResultDto>> BatchEnableAsync(List<Guid> formulaIds, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.BatchEnable started - Count={Count}", formulaIds.Count);

                var result = await _repository.BatchEnableAsync(formulaIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量启用验方返回空结果");

                _logger.LogInformation("[SVC] Formula.BatchEnable completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.BatchEnable failed - Count={Count}", formulaIds.Count);
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量启用验方", ex));
            }
        }

        public async Task<CommandResult<BatchOperationResultDto>> BatchDisableAsync(List<Guid> formulaIds, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.BatchDisable started - Count={Count}", formulaIds.Count);

                var result = await _repository.BatchDisableAsync(formulaIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量禁用验方返回空结果");

                _logger.LogInformation("[SVC] Formula.BatchDisable completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.BatchDisable failed - Count={Count}", formulaIds.Count);
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量禁用验方", ex));
            }
        }

        #endregion

        #region 批量导入/导出

        public async Task<CommandResult<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.BatchImport started");

                var result = await _repository.BatchImportAsync(request, ct);
                if (result == null)
                    return CommandResult<FormulaBatchImportResultDto>.Failed("批量导入操作失败");

                _logger.LogInformation("[SVC] Formula.BatchImport completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<FormulaBatchImportResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.BatchImport failed");
                return CommandResult<FormulaBatchImportResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量导入验方", ex));
            }
        }

        public async Task<CommandResult<byte[]>> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.ExportFormulas started - Category={Category}", category);

                var data = await _repository.ExportFormulasAsync(category, ct);
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出验方数据操作失败");

                _logger.LogInformation("[SVC] Formula.ExportFormulas completed - Size={Size} bytes", data.Length);
                return CommandResult<byte[]>.Succeeded(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.ExportFormulas failed - Category={Category}", category);
                return CommandResult<byte[]>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导出验方数据", ex));
            }
        }

        public async Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] Formula.ExportTemplate started");

                var data = await _repository.ExportTemplateAsync(ct);
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出模板操作失败");

                _logger.LogInformation("[SVC] Formula.ExportTemplate completed - Size={Size} bytes", data.Length);
                return CommandResult<byte[]>.Succeeded(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Formula.ExportTemplate failed");
                return CommandResult<byte[]>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导出验方模板", ex));
            }
        }

        #endregion
    }
}
