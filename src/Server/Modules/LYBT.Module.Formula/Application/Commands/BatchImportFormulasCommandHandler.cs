using MediatR;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.Module.Formulas.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formulas.Application.Commands;

public class BatchImportFormulasCommandHandler(
    IFormulaRepository repository,
    IHerbCrossModuleService herbCrossModule,
    ILogger<BatchImportFormulasCommandHandler> logger
) : IRequestHandler<BatchImportFormulasCommand, Result<FormulaBatchImportResultDto>>
{
    public async Task<Result<FormulaBatchImportResultDto>> Handle(
        BatchImportFormulasCommand request, CancellationToken cancellationToken)
    {
        var result = new FormulaBatchImportResultDto
        {
            FileName = request.FileName,
            ImportTime = DateTime.UtcNow,
            StartTime = DateTime.UtcNow,
            TotalCount = request.Formulas.Count
        };

        int index = 0;
        foreach (var item in request.Formulas)
        {
            index++;
            try
            {
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

                var formula = LYBT.Module.Formulas.Domain.Formula.Create(
                    name: item.Name,
                    effect: item.Effect,
                    usage: item.Usage,
                    property: item.Property,
                    isShared: item.IsShared,
                    remark: item.Remark);

                foreach (var herbDto in item.Herbs)
                {
                    var matchedHerb = await herbCrossModule.GetHerbByNameOrPinyinAsync(herbDto.HerbName, cancellationToken);
                    var herbItem = LYBT.Module.Formulas.Domain.FormulaHerbItem.Create(
                        formulaId: formula.Id,
                        herbName: herbDto.HerbName,
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

        result.EndTime = DateTime.UtcNow;
        result.IsSuccess = true;
        result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条，药材匹配 {result.MatchedHerbsCount} 个，未匹配 {result.UnmatchedHerbsCount} 个";

        return Result<FormulaBatchImportResultDto>.Success(result);
    }
}


