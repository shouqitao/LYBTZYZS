using FluentValidation;
using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Application.Validators;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Formula;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量导入验方命令处理器。
/// </summary>
public class BatchImportFormulasCommandHandler(
    IFormulaRepository repository,
    IHerbRepository herbRepository,
    ILogger<BatchImportFormulasCommandHandler> logger
) : IRequestHandler<BatchImportFormulasCommand, Result<FormulaBatchImportResultDto>>
{
    public async Task<Result<FormulaBatchImportResultDto>> Handle(
        BatchImportFormulasCommand request, CancellationToken cancellationToken)
    {
        // P2 (US-FORM-006): 单次导入上限 10000（对齐药材导入——原无上限）
        const int MAX_IMPORT_SIZE = 10000;
        if (request.Formulas.Count > MAX_IMPORT_SIZE)
        {
            return Result<FormulaBatchImportResultDto>.Failure(
                ErrorCode.InvalidRequest,
                $"单次导入数量不能超过 {MAX_IMPORT_SIZE} 条，当前 {request.Formulas.Count} 条");
        }

        var result = new FormulaBatchImportResultDto
        {
            FileName = request.FileName,
            ImportTime = DateTime.UtcNow,
            StartTime = DateTime.UtcNow,
            TotalCount = request.Formulas.Count
        };

        // T3.1: 改用模块内 IHerbRepository.GetAllActiveAsync（不再绕跨模块接口）
        var allHerbs = await herbRepository.GetAllActiveAsync(cancellationToken);
        var herbByName = allHerbs.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);
        var herbByPinyin = allHerbs
            .Where(h => !string.IsNullOrEmpty(h.PinYinCode))
            .GroupBy(h => h.PinYinCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First());

        // T4.2: 分批 500 条 per 事务，避免 10000 ChangeTracker 超时
        const int BatchSize = 500;
        int index = 0;
        for (int batchStart = 0; batchStart < request.Formulas.Count; batchStart += BatchSize)
        {
            var batch = request.Formulas.Skip(batchStart).Take(BatchSize).ToList();
            foreach (var item in batch)
        {
            index++;
            try
            {
                // P1-5：批量路径补 <> 校验（与单体一致）
                var formulaItemValidator = new FormulaImportItemDtoValidator();
                var fv = await formulaItemValidator.ValidateAsync(item, cancellationToken);
                if (!fv.IsValid)
                {
                    result.FailureCount++;
                    result.Failures.Add(new FormulaImportFailureDto
                    {
                        RowIndex = index,
                        FormulaName = item.Name ?? string.Empty,
                        ErrorMessage = string.Join("; ", fv.Errors.Select(e => e.ErrorMessage))
                    });
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    result.FailureCount++;
                    result.Failures.Add(new FormulaImportFailureDto
                    {
                        RowIndex = index,
                        FormulaName = item.Name ?? string.Empty,
                        ErrorMessage = "验方名称不能为空"
                    });
                    continue;
                }

                // P1-15/P1-8：ExistsByNameAsync 带 [IsDeleted]=0 过滤，已软删同名视为不存在（过滤唯一索引允许重建）
                if (await repository.ExistsByNameAsync(item.Name, null, cancellationToken))
                {
                    result.FailureCount++;
                    result.Failures.Add(new FormulaImportFailureDto
                    {
                        RowIndex = index,
                        FormulaName = item.Name,
                        ErrorMessage = $"验方名称 '{item.Name}' 已存在"
                    });
                    continue;
                }

                var formula = Formula.Create(
                    name: item.Name,
                    category: item.Category,
                    effect: item.Effect,
                    usage: item.Usage,
                    property: item.Property,
                    isShared: item.IsShared,
                    remark: item.Remark);

                foreach (var herbDto in item.Herbs)
                {
                    LYBT.Entities.Herbs.Herb? matchedHerb = null;
                    if (herbDto.HerbName != null)
                    {
                        if (!herbByName.TryGetValue(herbDto.HerbName, out matchedHerb))
                            herbByPinyin.TryGetValue(herbDto.HerbName, out matchedHerb);
                    }
                    var herbItem = FormulaHerbItem.Create(
                        formulaId: formula.Id,
                        herbName: herbDto.HerbName ?? string.Empty,
                        dosage: herbDto.Dosage,
                        unit: herbDto.Unit ?? "g",
                        herbId: matchedHerb?.Id,
                        originalHerbName: herbDto.HerbName,
                        usage: herbDto.Usage,
                        processingMethod: herbDto.Preparation);

                    formula.AddHerb(herbItem);

                    if (matchedHerb != null)
                        result.MatchedHerbsCount++;
                    else
                        result.UnmatchedHerbsCount++;
                }

                if (formula.Herbs.Any() && formula.Herbs.All(h => h.IsValidated))
                    formula.Validate();

                await repository.AddAsync(formula, cancellationToken);

                result.SuccessCount++;
                result.SuccessfulIds.Add(formula.Id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Failures.Add(new FormulaImportFailureDto
                {
                    RowIndex = index,
                    FormulaName = item.Name ?? string.Empty,
                    ErrorMessage = "数据处理异常"
                });
                logger.LogError(ex, "[CMD] FormulaImport → ItemError - FormulaName={FormulaName}", item.Name);
            }
        }
        }

        result.EndTime = DateTime.UtcNow;
        result.IsSuccess = true;
        result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条，药材匹配 {result.MatchedHerbsCount} 个，未匹配 {result.UnmatchedHerbsCount} 个";

        return Result<FormulaBatchImportResultDto>.Success(result);
    }
}
