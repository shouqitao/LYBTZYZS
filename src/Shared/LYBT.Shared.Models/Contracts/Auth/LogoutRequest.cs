using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Auth
{

    /// <summary>
    /// 用户登出请求 - 前后端共享API契约
    /// </summary>
    public class LogoutRequest
    {

        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>刷新令牌（可选，用于撤销）</summary>
        [DisplayName("刷新令牌")]
        public string? RefreshToken { get; set; }

        /// <summary>设备ID（可选，用于撤销特定设备）</summary>
        [DisplayName("设备ID")]
        public string? DeviceId { get; set; }
    }
}
