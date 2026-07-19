using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Herbs.Domain;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Application.Mappers;
using LYBT.Shared.Utilities.Text;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 批量导入药材命令处理器。
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

        for (int i = 0; i < request.Herbs.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dto = request.Herbs[i];
            var rowNumber = i + 2;

            try
            {
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
                            var existingHerbs = await _herbRepository.GetPagedAsync(1, 1, dto.Name, null, cancellationToken);
                            var existingHerb = existingHerbs.Items.FirstOrDefault();
                            if (existingHerb != null)
                            {
                                existingHerb.UpdateProfile(
                                    dto.Name, dto.Unit, dto.Price, dto.PinYinCode,
                                    dto.Category, dto.Properties, dto.Origin, dto.Spec,
                                    dto.CostPrice, dto.Effect, dto.Usage, dto.Remark,
                                    request.CurrentUserId);
                                await _herbRepository.UpdateAsync(existingHerb, cancellationToken);
                                result.SuccessCount++;
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

                var entity = HerbDtoMapper.ToEntity(dto, request.CurrentUserId);

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

        return Result<HerbBatchImportResultDto>.Success(result);
    }
}


