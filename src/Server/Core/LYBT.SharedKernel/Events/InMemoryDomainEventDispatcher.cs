using MediatR;

namespace LYBT.SharedKernel.Events;

public class InMemoryDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public InMemoryDomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}


