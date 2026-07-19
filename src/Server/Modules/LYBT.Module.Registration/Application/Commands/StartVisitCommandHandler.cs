using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Registration.Application.Commands;

/// <summary>
/// 接诊处理器。
/// </summary>
public sealed class StartVisitCommandHandler
    : IRequestHandler<StartVisitCommand, Result<Guid>>
{
    private readonly IRegistrationRepository _repository;

    public StartVisitCommandHandler(IRegistrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(
        StartVisitCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (entity is null)
            return Result<Guid>.Failure(ErrorCode.RegistrationNotFound, ErrorMessages.Get(ErrorCode.RegistrationNotFound));

        try
        {
            entity.StartVisit();
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure(ErrorCode.RegistrationInvalidStatusTransition, ex.Message);
        }

        await _repository.UpdateAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}


