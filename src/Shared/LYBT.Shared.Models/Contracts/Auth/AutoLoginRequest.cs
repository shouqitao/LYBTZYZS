using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Auth
{
    /// <summary>
    /// 自动登录请求 - 使用AutoLoginToken代替密码
    /// OpenSpec: refactor-login-authentication (CVT-001)
    /// </summary>
    /// <remarks>
    /// <para>功能: 用于"记住密码"场景的自动登录</para>
    /// <para>流程: 客户端提供存储的AutoLoginToken → 服务端验证 → 返回新的AccessToken/RefreshToken</para>
    /// <para>安全: AutoLoginToken可被服务端随时撤销，不暴露用户密码</para>
    /// </remarks>
    public class AutoLoginRequest
    {
        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        [DisplayName("用户名")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>自动登录令牌</summary>
        [Required(ErrorMessage = "自动登录令牌不能为空")]
        [DisplayName("自动登录令牌")]
        public string AutoLoginToken { get; set; } = string.Empty;

        /// <summary>客户端IP</summary>
        [DisplayName("客户端IP")]
        public string? ClientIp { get; set; }

        /// <summary>UserAgent</summary>
        [DisplayName("UserAgent")]
        public string? UserAgent { get; set; }

        /// <summary>设备ID（用于多设备管理）</summary>
        [DisplayName("设备ID")]
        public string? DeviceId { get; set; }

        /// <summary>设备名称（用于多设备管理）</summary>
        [DisplayName("设备名称")]
        public string? DeviceName { get; set; }
    }
}
