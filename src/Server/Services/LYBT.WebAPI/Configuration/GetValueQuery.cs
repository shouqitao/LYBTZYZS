using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public record GetValueQuery(
    string Key
) : IRequest<Result<string?>>;


