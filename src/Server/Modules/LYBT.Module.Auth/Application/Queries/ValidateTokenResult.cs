namespace LYBT.Module.Auth.Application.Queries;

public record ValidateTokenResult(
    bool IsValid,
    string? UserId,
    string? UserName,
    string? Role);
