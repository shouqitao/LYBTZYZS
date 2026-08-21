using FluentValidation;
using LYBT.Module.Catalog.Application.Mappers;
using LYBT.Module.Catalog.Application.Validators;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Utilities.Text;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量导入药材命令处理器。P2-12-2 批量策略与单条一致：需 AdminOrSuperAdmin（HerbsController.Create/Update 同策略），校验含 <> 禁用（与单条 HerbCommandHandler 一致），软删同名走新增不复活。
/// </summary>
public class BatchImportHerbsCommandHandler : IRequestHandler<BatchImportHerbsCommand, Result<HerbBatchImportResultDto>>
{
    private readonly IHerbRepository _herbRepository;

    public BatchImportHerbsCommandHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<HerbBatchImportResultDto>> Handle(
        BatchImportHerbsCommand request, CancellationToken cancellationToken)
    {
        const int MAX_IMPORT_SIZE = 10000;

        var result = new HerbBatchImportResultDto
        {
            ImportTime = DateTime.UtcNow
        };

        if (request.Herbs.Count > MAX_IMPORT_SIZE)
        {
            return Result<HerbBatchImportResultDto>.Failure(ErrorCode.ValidationFailed, $"批量导入最多支持{MAX_IMPORT_SIZE}条记录");
        }

        // T4.2: 分批 500 条 per 事务，避免 10000 ChangeTracker 超时，partial 成功/失败已通过 result 透出
        const int BatchSize = 500;
        for (int batchStart = 0; batchStart < request.Herbs.Count; batchStart += BatchSize)
        {
            var batch = request.Herbs.Skip(batchStart).Take(BatchSize).ToList();
            for (int j = 0; j < batch.Count; j++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dto = batch[j];
                var rowNumber = batchStart + j + 2;

            try
            {
                // P1-5：批量路径补单体一致的校验（含 <> 非法字符）
                var itemValidator = new HerbImportItemDtoValidator();
                var validation = await itemValidator.ValidateAsync(dto, cancellationToken);
                if (!validation.IsValid)
                {
                    result.FailureCount++;
                    result.Failures.Add(new HerbImportFailureDto
                    {
                        RowNumber = rowNumber,
                        HerbName = dto.Name,
                        Reason = "校验失败",
                        ErrorDetails = validation.Errors.Select(e => e.ErrorMessage).ToList()
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dto.PinYinCode))
                {
                    dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);
                }

                var exists = await _herbRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken);

                if (exists)
                {
                    switch (request.Strategy)
                    {
                        case DuplicateStrategy.Skip:
                            result.SkippedCount++;
                            continue;

                        case DuplicateStrategy.Update:
                            var existingHerb = await _herbRepository.GetByNameAsync(dto.Name, cancellationToken);
                            if (existingHerb != null)
                            {
                                // P1-15：已软删的同名记录视为不存在，不复活旧审计链，直接新建（过滤唯一索引允许）
                                if (existingHerb.IsDeleted)
                                {
                                    var newEntity = CatalogDtoMapper.ToEntity(dto, request.CurrentUserId);
                                    await _herbRepository.AddAsync(newEntity, cancellationToken);
                                    result.SuccessCount++;
                                }
                                else
                                {
                                    existingHerb.UpdateProfile(
                                        dto.Name, dto.Unit, dto.Price, dto.PinYinCode,
                                        dto.Category, dto.Properties, dto.Origin, dto.Spec,
                                        dto.CostPrice, dto.Effect, dto.Usage, dto.Remark,
                                        request.CurrentUserId);
                                    await _herbRepository.UpdateAsync(existingHerb, cancellationToken);
                                    result.SuccessCount++;
                                }
                            }
                            continue;

                        case DuplicateStrategy.Error:
                            result.FailureCount++;
                            result.Failures.Add(new HerbImportFailureDto
                            {
                                RowNumber = rowNumber,
                                HerbName = dto.Name,
                                Reason = "药材名称重复",
                                ErrorDetails = new List<string> { "已存在同名药材，导入策略设置为报错" }
                            });
                            continue;
                    }
                }

                var entity = CatalogDtoMapper.ToEntity(dto, request.CurrentUserId);

                await _herbRepository.AddAsync(entity, cancellationToken);
                result.SuccessCount++;
            }
            catch
            {
                result.FailureCount++;
                result.Failures.Add(new HerbImportFailureDto
                {
                    RowNumber = rowNumber,
                    HerbName = dto.Name,
                    Reason = "导入失败",
                    ErrorDetails = new List<string> { "数据处理异常" }
                });
            }
        }
        }

        return Result<HerbBatchImportResultDto>.Success(result);
    }
}
