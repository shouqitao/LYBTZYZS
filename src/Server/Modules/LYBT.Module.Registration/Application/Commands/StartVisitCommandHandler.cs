using LYBT.Module.Registration.Interfaces;
using MediatR;

namespace LYBT.Module.Registration.Application.Commands;

/// <summary>
/// 接诊处理器。
/// </summary>
public sealed class StartVisitCommandHandler
    : IRequestHandler<StartVisitCommand, Guid>
{
    private readonly IRegistrationRepository _repository;

    public StartVisitCommandHandler(IRegistrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(
        StartVisitCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException("挂号记录不存在");

        entity.StartVisit();
        await _repository.UpdateAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}


