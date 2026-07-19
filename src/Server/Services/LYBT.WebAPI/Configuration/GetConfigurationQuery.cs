using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public record GetConfigurationQuery() : IRequest<Result<Dictionary<string, string>>>;


