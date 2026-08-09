namespace LYBT.Module.Identity.Application.Queries;

public record ValidateTokenResult(
    bool IsValid,
    string? UserId,
    string? UserName,
    string? Role);
