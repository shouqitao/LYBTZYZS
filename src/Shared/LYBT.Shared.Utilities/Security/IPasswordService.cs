using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Utilities.Security;

/// <summary>
/// 密码服务接口 - 提供可注入的密码哈希、验证和生成能力
/// 封装 PasswordHelper 静态类，便于单元测试和依赖注入
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// 哈希密码
    /// </summary>
    string HashPassword(string password, UserRole userType = UserRole.Doctor, ILogger? logger = null);

    /// <summary>
    /// 验证密码
    /// </summary>
    PasswordHelper.PasswordVerificationResult VerifyPassword(
        string password,
        string hashedPassword,
        UserRole userType = UserRole.Doctor,
        ILogger? logger = null);

    /// <summary>
    /// 验证密码，如果工作因子已升级则自动重新哈希
    /// </summary>
    PasswordHelper.PasswordVerificationResult VerifyAndRehashIfNeeded(
        string password,
        string hashedPassword,
        UserRole userType = UserRole.Doctor,
        ILogger? logger = null);

    /// <summary>
    /// 验证密码合规性（长度、复杂度）
    /// </summary>
    PasswordHelper.PasswordValidationResult ValidatePassword(
        string password,
        int minLength = 8,
        bool requireUppercase = true,
        bool requireLowercase = true,
        bool requireDigits = true,
        bool requireSpecialChars = true);

    /// <summary>
    /// 生成安全随机密码
    /// </summary>
    string GenerateSecurePassword(
        int length = 20,
        bool includeUppercase = true,
        bool includeLowercase = true,
        bool includeDigits = true,
        bool includeSpecialChars = true);

    /// <summary>
    /// 生成临时密码（8位，1大写+4小写+3数字）
    /// </summary>
    string GenerateTemporaryPassword();

    /// <summary>
    /// 防时间攻击的字符串比较
    /// </summary>
    bool SecureEquals(string? s1, string? s2);
}
