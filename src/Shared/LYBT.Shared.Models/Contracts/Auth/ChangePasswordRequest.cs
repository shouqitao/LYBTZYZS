using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Auth;

/// <summary>
/// 修改密码请求 - 前后端共享API契约
/// </summary>
public class ChangePasswordRequest {

    /// <summary>
    /// 旧密码
    /// </summary>
    [Required(ErrorMessage = "旧密码不能为空")]
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "新密码长度必须在6-50个字符之间")]
    public string NewPassword { get; set; } = string.Empty;
}
