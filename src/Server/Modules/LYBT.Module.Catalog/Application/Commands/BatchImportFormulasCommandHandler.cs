using FluentValidation;
using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Application.Validators;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
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

        // 药材组成构建（新增与 Update 复用：名称/拼音码匹配药材库，未匹配计入 UnmatchedHerbsCount 待人工校验）
        List<FormulaHerbItem> BuildHerbItems(FormulaImportItemDto item, Guid formulaId)
        {
            var items = new List<FormulaHerbItem>(item.Herbs.Count);
            foreach (var herbDto in item.Herbs)
            {
                LYBT.Entities.Herbs.Herb? matchedHerb = null;
                if (herbDto.HerbName != null)
                {
                    if (!herbByName.TryGetValue(herbDto.HerbName, out matchedHerb))
                        herbByPinyin.TryGetValue(herbDto.HerbName, out matchedHerb);
                }

                items.Add(FormulaHerbItem.Create(
                    formulaId: formulaId,
                    herbName: herbDto.HerbName ?? string.Empty,
                    dosage: herbDto.Dosage,
                    unit: herbDto.Unit ?? "g",
                    herbId: matchedHerb?.Id,
                    originalHerbName: herbDto.HerbName,
                    usage: herbDto.Usage,
                    processingMethod: herbDto.Preparation));

                if (matchedHerb != null)
                    result.MatchedHerbsCount++;
                else
                    result.UnmatchedHerbsCount++;
            }

            return items;
        }

        // AC④（US-SHELL-021）：整次请求一个显式事务（ADR-0030：同 CatalogDbContext 事务）。
        // 逐行的 Skip/Update/Error 与校验失败仍按行写入 result.Failures（既有部分成功语义不变）；
        // 事务保证：中途取消（循环顶部 ThrowIfCancellationRequested）或提交失败 → 本次请求已写入的行整体回滚，不留半批数据。
        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
        try
        {
            // T4.2: 分批 500 条限制单批 ChangeTracker 规模（避免 10000 条一次性跟踪过重）；
            // 事务边界不在批次上，而是整次请求（见上）——批次内失败由 result 透出。
            const int BatchSize = 500;
            for (int batchStart = 0; batchStart < request.Formulas.Count; batchStart += BatchSize)
            {
                var batch = request.Formulas.Skip(batchStart).Take(BatchSize).ToList();
                for (int j = 0; j < batch.Count; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = batch[j];
                    var rowNumber = batchStart + j + 2;

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
                                RowIndex = rowNumber,
                                FormulaName = item.Name,
                                ErrorMessage = string.Join("; ", fv.Errors.Select(e => e.ErrorMessage))
                            });
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(item.Name))
                        {
                            result.FailureCount++;
                            result.Failures.Add(new FormulaImportFailureDto
                            {
                                RowIndex = rowNumber,
                                FormulaName = item.Name,
                                ErrorMessage = "验方名称不能为空"
                            });
                            continue;
                        }

                        // P1-15/P1-8：ExistsByNameAsync 带 [IsDeleted]=0 过滤，已软删同名视为不存在（过滤唯一索引允许重建）
                        // AC②（US-SHELL-021）：重复键 = 验方名称；重复按 request.Strategy 处理（Skip/Update/Error）
                        if (await repository.ExistsByNameAsync(item.Name, null, cancellationToken))
                        {
                            switch (request.Strategy)
                            {
                                case DuplicateStrategy.Skip:
                                    result.SkippedCount++;
                                    continue;

                                case DuplicateStrategy.Update:
                                    var existingFormula = await repository.GetByNameAsync(item.Name, cancellationToken);
                                    if (existingFormula != null)
                                    {
                                        existingFormula.UpdateProfile(
                                            name: item.Name,
                                            effect: item.Effect,
                                            indication: item.Indication,
                                            usage: item.Usage,
                                            remark: item.Remark,
                                            property: item.Property,
                                            category: item.Category,
                                            isShared: item.IsShared,
                                            updatedBy: request.CurrentUserId);
                                        var updatedHerbs = BuildHerbItems(item, existingFormula.Id);
                                        existingFormula.ReplaceHerbs(updatedHerbs);
                                        if (updatedHerbs.Count > 0 && updatedHerbs.All(h => h.IsValidated))
                                            existingFormula.Validate();
                                        existingFormula.DegradeToDraftIfAnyHerbUnvalidated();
                                        await repository.UpdateAsync(existingFormula, cancellationToken);
                                        result.SuccessCount++;
                                        result.SuccessfulIds.Add(existingFormula.Id);
                                    }
                                    continue;

                                case DuplicateStrategy.Error:
                                    result.FailureCount++;
                                    result.Failures.Add(new FormulaImportFailureDto
                                    {
                                        RowIndex = rowNumber,
                                        FormulaName = item.Name,
                                        ErrorMessage = $"验方名称 '{item.Name}' 已存在"
                                    });
                                    continue;
                            }
                        }

                        var formula = Formula.Create(
                            name: item.Name,
                            category: item.Category,
                            effect: item.Effect,
                            usage: item.Usage,
                            property: item.Property,
                            isShared: item.IsShared,
                            remark: item.Remark);

                        foreach (var herbItem in BuildHerbItems(item, formula.Id))
                            formula.AddHerb(herbItem);

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
                            RowIndex = rowNumber,
                            FormulaName = item.Name,
                            ErrorMessage = "数据处理异常"
                        });
                        logger.LogError(ex, "[CMD] FormulaImport → ItemError - FormulaName={FormulaName}", item.Name);
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // 请求级失败（取消/提交失败等）：整批回滚——回滚不依赖可能已取消的令牌
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        result.EndTime = DateTime.UtcNow;
        result.IsSuccess = true;
        result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条，跳过 {result.SkippedCount} 条，药材匹配 {result.MatchedHerbsCount} 个，未匹配 {result.UnmatchedHerbsCount} 个";

        return Result<FormulaBatchImportResultDto>.Success(result);
    }
}
