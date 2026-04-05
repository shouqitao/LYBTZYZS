using System.Security.Cryptography;
using System.Text;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Utilities.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Configuration.Services;

public sealed class DefaultPasswordService : IDefaultPasswordService
{
    private readonly IHostEnvironment _environment;
    private readonly SystemAdminOptions _systemAdminOptions;
    private readonly DefaultPasswordOptions _defaultPasswordOptions;
    private readonly ILogger<DefaultPasswordService> _logger;

    public DefaultPasswordService(
        IHostEnvironment environment,
        SystemAdminOptions systemAdminOptions,
        DefaultPasswordOptions defaultPasswordOptions,
        ILogger<DefaultPasswordService> logger)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _systemAdminOptions = systemAdminOptions ?? throw new ArgumentNullException(nameof(systemAdminOptions));
        _defaultPasswordOptions = defaultPasswordOptions ?? throw new ArgumentNullException(nameof(defaultPasswordOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsDefaultPasswordAllowed()
        => !_environment.IsProduction() || _systemAdminOptions.AllowAutoCreateInProduction;

    public bool ValidateSetupToken(string? providedToken)
    {
        if (!_environment.IsProduction())
        {
            return true;
        }

        var expectedToken = _systemAdminOptions.InitialSetupToken;
        if (string.IsNullOrWhiteSpace(expectedToken) || string.IsNullOrWhiteSpace(providedToken))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
        var providedBytes = Encoding.UTF8.GetBytes(providedToken);

        return expectedBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    public string GetOrGeneratePassword()
    {
        if (!_environment.IsProduction())
        {
            return _defaultPasswordOptions.SysAdminPassword;
        }

        _logger.LogWarning("生产环境使用随机生成的系统管理员默认密码");
        return PasswordHelper.GenerateSecurePassword();
    }

    public bool ShouldForcePasswordChange()
        => _defaultPasswordOptions.ForceChangeOnFirstLogin;
}
