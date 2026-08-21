using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.Catalog.Application.Queries;

public class CheckHerbReferenceQueryHandler
    : IRequestHandler<CheckHerbReferenceQuery, Result<HerbReferenceCheckDto>>,
      IRequestHandler<BatchCheckHerbReferenceQuery, Result<List<HerbReferenceCheckDto>>>
{
    private readonly IHerbRepository _herbRepository;
    private readonly IHerbReferenceRepository _referenceRepository;

    public CheckHerbReferenceQueryHandler(
        IHerbRepository herbRepository,
        IHerbReferenceRepository referenceRepository)
    {
        _herbRepository = herbRepository;
        _referenceRepository = referenceRepository;
    }

    public async Task<Result<HerbReferenceCheckDto>> Handle(
        CheckHerbReferenceQuery request, CancellationToken cancellationToken)
    {
        var herb = await _herbRepository.GetByIdAsync(request.HerbId, cancellationToken);
        if (herb == null)
            return Result<HerbReferenceCheckDto>.Failure(ErrorCode.HerbNotFound, ErrorMessages.Get(ErrorCode.HerbNotFound));

        var prescriptionCount = await _referenceRepository
            .GetPrescriptionReferenceCountAsync(request.HerbId, cancellationToken);
        var formulaCount = await _referenceRepository
            .GetFormulaReferenceCountAsync(request.HerbId, cancellationToken);
        var totalCount = prescriptionCount + formulaCount;

        var recentReferences = prescriptionCount > 0
            ? await _referenceRepository.GetRecentPrescriptionReferencesAsync(request.HerbId, 5, cancellationToken)
            : null;

        string? warning = totalCount > 0
            ? $"药材被 {prescriptionCount} 个处方和 {formulaCount} 个验方引用"
            : null;

        return Result<HerbReferenceCheckDto>.Success(new HerbReferenceCheckDto
        {
            HerbId = herb.Id,
            HerbName = herb.Name,
            HasReferences = totalCount > 0,
            ReferenceCount = totalCount,
            CanDelete = true,
            DeleteWarning = warning,
            RecentReferences = recentReferences
        });
    }

    public async Task<Result<List<HerbReferenceCheckDto>>> Handle(
        BatchCheckHerbReferenceQuery request, CancellationToken cancellationToken)
    {
        var herbIds = request.HerbIds;
        if (herbIds.Count == 0)
            return Result<List<HerbReferenceCheckDto>>.Success(new List<HerbReferenceCheckDto>());

        // P1-11: 批量名称查询（单次 IN），原逐 ID GetByIdAsync 造成 N+1
        var herbs = await _herbRepository.GetNamesByIdsAsync(herbIds, cancellationToken);

        var prescriptionCounts = await _referenceRepository
            .GetBatchPrescriptionReferenceCountsAsync(herbIds, cancellationToken);
        var formulaCounts = await _referenceRepository
            .GetBatchFormulaReferenceCountsAsync(herbIds, cancellationToken);

        var results = new List<HerbReferenceCheckDto>();
        foreach (var herbId in herbIds)
        {
            if (!herbs.TryGetValue(herbId, out var herbName))
                continue;

            var prescriptionCount = prescriptionCounts.GetValueOrDefault(herbId, 0);
            var formulaCount = formulaCounts.GetValueOrDefault(herbId, 0);
            var totalCount = prescriptionCount + formulaCount;

            string? warning = totalCount > 0
                ? $"药材被 {prescriptionCount} 个处方和 {formulaCount} 个验方引用"
                : null;

            results.Add(new HerbReferenceCheckDto
            {
                HerbId = herbId,
                HerbName = herbName,
                HasReferences = totalCount > 0,
                ReferenceCount = totalCount,
                CanDelete = true,
                DeleteWarning = warning
            });
        }

        return Result<List<HerbReferenceCheckDto>>.Success(results);
    }
}
