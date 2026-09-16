using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Infrastructure.SharedKernel.Events;

/// <summary>
/// 领域事件基础设施 DI 注册（ADR-0018）。
/// Server（WebAPI）与 LocalWebAPI 双宿主均须调用。
/// </summary>
public static class DomainEventServiceCollectionExtensions
{
    /// <summary>
    /// 注册 <see cref="IDomainEventDispatcher"/>。
    /// 前置条件：宿主已通过 <c>AddMediatR</c> 注册 <c>IPublisher</c>。
    /// </summary>
    public static IServiceCollection AddDomainEventDispatcher(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }
}
