using LYBT.Shared.Models.Contracts.Registration;
using MediatR;

namespace LYBT.Module.Registrations.Application.Queries;

/// <summary>
/// 获取挂号详情请求。
/// </summary>
public sealed record GetRegistrationQuery(Guid Id) : IRequest<RegistrationDetailDto?>;


