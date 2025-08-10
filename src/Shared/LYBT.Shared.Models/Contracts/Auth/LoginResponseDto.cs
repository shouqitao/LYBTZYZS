using LYBT.Shared.Models.Core;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Auth
{

    /// <summary>
    /// 登录成功返回 DTO
    /// </summary>
    public class LoginResponseDto
    {

        /// <summary>JWT令牌</summary>
        [DisplayName("JWT Token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>用户信息</summary>
        [DisplayName("用户信息")]
        public BaseUser User { get; set; } = new();
    }
}