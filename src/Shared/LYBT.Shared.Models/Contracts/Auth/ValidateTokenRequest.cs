using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Auth;

/// <summary>
/// Token验证请求
/// </summary>
public class ValidateTokenRequest
{
    /// <summary>
    /// 要验证的JWT Token
    /// </summary>
    [Required(ErrorMessage = "Token不能为空")]
    public string Token { get; set; } = string.Empty;
}