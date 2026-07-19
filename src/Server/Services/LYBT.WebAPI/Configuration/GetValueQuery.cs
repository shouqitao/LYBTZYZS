using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public record GetValueQuery(
    string Key
) : IRequest<Result<string?>>;


