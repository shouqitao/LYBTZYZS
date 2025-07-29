using LYBT.Models.Users;
using System.ComponentModel;

namespace LYBT.Models.Auth {

    /// <summary>
    /// 登录成功返回 DTO
    /// </summary>
    public class LoginResponseDto {

        /// <summary>JWT令牌</summary>
        [DisplayName("JWT Token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>用户信息</summary>
        [DisplayName("用户信息")]
        public UserDto User { get; set; } = new();
    }
}