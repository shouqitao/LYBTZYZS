using LYBT.Shared.Configuration.Options.Server;

namespace LYBT.Infrastructure.Configuration.Services;

public interface IDefaultPasswordService
{
    bool IsDefaultPasswordAllowed();

    bool ValidateSetupToken(string? providedToken);

    string GetOrGeneratePassword();

    bool ShouldForcePasswordChange();
}
