using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace LYBT.Tests.Desktop._Infrastructure.Assertions;

/// <summary>
/// JWT Token 自定义断言
/// 提供 FluentAssertions 风格的 JWT Token 验证方法
/// </summary>
public static class JwtAssertions
{
    /// <summary>
    /// 验证 JWT Token 包含指定 Claim
    /// </summary>
    public static AndConstraint<StringAssertions> HaveClaim(
        this StringAssertions assertions,
        string claimType,
        string? expectedValue = null,
        string because = "",
        params object[] becauseArgs)
    {
        var token = ParseToken(assertions.Subject);
        var claim = token.Claims.FirstOrDefault(c => c.Type == claimType);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(claim != null)
            .FailWith($"Expected JWT token to contain claim '{claimType}'{{reason}}, but it was not found.");

        if (expectedValue != null)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(claim!.Value == expectedValue)
                .FailWith($"Expected claim '{claimType}' to have value '{{0}}'{{reason}}, but found '{{1}}'.",
                    expectedValue, claim.Value);
        }

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// 验证 JWT Token 包含指定角色
    /// </summary>
    public static AndConstraint<StringAssertions> HaveRole(
        this StringAssertions assertions,
        string expectedRole,
        string because = "",
        params object[] becauseArgs)
    {
        var token = ParseToken(assertions.Subject);
        var roles = token.Claims
            .Where(c => c.Type == "role" || c.Type == "roles" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(c => c.Value)
            .ToList();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(roles.Contains(expectedRole))
            .FailWith($"Expected JWT token to contain role '{{0}}'{{reason}}, but found roles: [{string.Join(", ", roles)}].",
                expectedRole);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// 验证 JWT Token 的用户名 (sub 或 name 或 unique_name claim)
    /// </summary>
    public static AndConstraint<StringAssertions> HaveUsername(
        this StringAssertions assertions,
        string expectedUsername,
        string because = "",
        params object[] becauseArgs)
    {
        var token = ParseToken(assertions.Subject);
        
        // 尝试多种可能的用户名 claim 类型
        var usernameClaims = new[] { "sub", "name", "unique_name", "username", "preferred_username" };
        var actualUsername = usernameClaims
            .Select(type => token.Claims.FirstOrDefault(c => c.Type == type)?.Value)
            .FirstOrDefault(value => !string.IsNullOrEmpty(value));

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actualUsername != null)
            .FailWith("Expected JWT token to contain a username claim{{reason}}, but none was found.");

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actualUsername == expectedUsername)
            .FailWith($"Expected JWT token username to be '{{0}}'{{reason}}, but found '{{1}}'.",
                expectedUsername, actualUsername);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// 验证 JWT Token 尚未过期
    /// </summary>
    public static AndConstraint<StringAssertions> NotBeExpired(
        this StringAssertions assertions,
        string because = "",
        params object[] becauseArgs)
    {
        var token = ParseToken(assertions.Subject);
        var expiration = token.ValidTo;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(expiration > DateTime.UtcNow)
            .FailWith($"Expected JWT token to not be expired{{reason}}, but it expired at {{0:yyyy-MM-dd HH:mm:ss}} UTC.",
                expiration);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// 验证 JWT Token 签发者
    /// </summary>
    public static AndConstraint<StringAssertions> HaveIssuer(
        this StringAssertions assertions,
        string expectedIssuer,
        string because = "",
        params object[] becauseArgs)
    {
        var token = ParseToken(assertions.Subject);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(token.Issuer == expectedIssuer)
            .FailWith($"Expected JWT token issuer to be '{{0}}'{{reason}}, but found '{{1}}'.",
                expectedIssuer, token.Issuer);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// 验证 JWT Token 受众
    /// </summary>
    public static AndConstraint<StringAssertions> HaveAudience(
        this StringAssertions assertions,
        string expectedAudience,
        string because = "",
        params object[] becauseArgs)
    {
        var token = ParseToken(assertions.Subject);
        var audiences = token.Audiences.ToList();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(audiences.Contains(expectedAudience))
            .FailWith($"Expected JWT token audience to contain '{{0}}'{{reason}}, but found: [{string.Join(", ", audiences)}].",
                expectedAudience);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// 解析 JWT Token
    /// </summary>
    private static JwtSecurityToken ParseToken(string? tokenString)
    {
        Execute.Assertion
            .ForCondition(!string.IsNullOrWhiteSpace(tokenString))
            .FailWith("JWT token should not be null or empty.");

        var handler = new JwtSecurityTokenHandler();
        
        Execute.Assertion
            .ForCondition(handler.CanReadToken(tokenString))
            .FailWith("String is not a valid JWT token.");

        return handler.ReadJwtToken(tokenString!);
    }
}

/// <summary>
/// JWT Token 断言扩展 - 用于 JwtSecurityToken 类型
/// </summary>
public static class JwtSecurityTokenAssertions
{
    /// <summary>
    /// 验证 Token 包含指定 Claim
    /// </summary>
    public static AndConstraint<ObjectAssertions> HaveClaim(
        this ObjectAssertions assertions,
        string claimType,
        string? expectedValue = null,
        string because = "",
        params object[] becauseArgs)
    {
        var token = assertions.Subject as JwtSecurityToken
            ?? throw new InvalidOperationException("Subject is not a JwtSecurityToken");

        var claim = token.Claims.FirstOrDefault(c => c.Type == claimType);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(claim != null)
            .FailWith($"Expected JWT token to contain claim '{claimType}'{{reason}}, but it was not found.");

        if (expectedValue != null)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(claim!.Value == expectedValue)
                .FailWith($"Expected claim '{claimType}' to have value '{{0}}'{{reason}}, but found '{{1}}'.",
                    expectedValue, claim.Value);
        }

        return new AndConstraint<ObjectAssertions>(assertions);
    }
}
