using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using Serilog.Events;

namespace LYBT.LocalWebAPI.Commands;

public record GetLoggingStatusQuery : IRequest<ApiResponse<object>>;

public record EnableDebugModeCommand(string? Level, int? DurationMinutes) : IRequest<ApiResponse<object>>;

public record DisableDebugModeCommand : IRequest<ApiResponse<object>>;

public record SetLoggingLevelCommand(string Level) : IRequest<ApiResponse<object>>;
