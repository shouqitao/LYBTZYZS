using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Utilities.Security;

public sealed class BcryptPasswordService : IPasswordService
{
    public string HashPassword(string password, UserRole userType = UserRole.Doctor, ILogger? logger = null)
        => PasswordHelper.HashPassword(password, userType, logger);

    public PasswordHelper.PasswordVerificationResult VerifyPassword(
        string password,
        string hashedPassword,
        UserRole userType = UserRole.Doctor,
        ILogger? logger = null)
        => PasswordHelper.VerifyPassword(password, hashedPassword, userType, logger);

    public PasswordHelper.PasswordVerificationResult VerifyAndRehashIfNeeded(
        string password,
        string hashedPassword,
        UserRole userType = UserRole.Doctor,
        ILogger? logger = null)
        => PasswordHelper.VerifyAndRehashIfNeeded(password, hashedPassword, userType, logger);

    public PasswordHelper.PasswordValidationResult ValidatePassword(
        string password,
        int minLength = 8,
        bool requireUppercase = true,
        bool requireLowercase = true,
        bool requireDigits = true,
        bool requireSpecialChars = true)
        => PasswordHelper.ValidatePassword(password, minLength, requireUppercase, requireLowercase, requireDigits, requireSpecialChars);

    public string GenerateSecurePassword(
        int length = 20,
        bool includeUppercase = true,
        bool includeLowercase = true,
        bool includeDigits = true,
        bool includeSpecialChars = true)
        => PasswordHelper.GenerateSecurePassword(length, includeUppercase, includeLowercase, includeDigits, includeSpecialChars);

    public string GenerateTemporaryPassword()
        => PasswordHelper.GenerateTemporaryPassword();

    public bool SecureEquals(string? s1, string? s2)
        => PasswordHelper.SecureEquals(s1, s2);
}
