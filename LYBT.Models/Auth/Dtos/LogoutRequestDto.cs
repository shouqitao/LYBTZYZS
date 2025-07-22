using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Auth.Dtos {

    /// <summary>
    /// 用户登出请求 DTO
    /// </summary>
    public class LogoutRequestDto {

        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        [DisplayName("用户名")]
/// <summary>
/// Username 属性。
/// </summary>
        public string Username { get; set; } = string.Empty;
    }
}
