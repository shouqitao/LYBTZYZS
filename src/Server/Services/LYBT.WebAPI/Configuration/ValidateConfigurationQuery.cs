using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public record ValidateConfigurationQuery() : IRequest<Result>;


