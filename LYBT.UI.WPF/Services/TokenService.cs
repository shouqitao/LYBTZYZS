using LYBT.Module.Users.Dtos;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 存储并提供当前登录用户及 JWT Token
    /// </summary>
    public class TokenService {
        private string? _token;
        private UserDto? _user;

        /// <summary>当前 JWT Token</summary>
        public string? Token => _token;

        /// <summary>当前登录用户信息</summary>
        public UserDto? CurrentUser => _user;

        /// <summary>设置登录后的凭据</summary>
        public void SetLoginInfo(string token, UserDto user) {
            _token = token;
            _user = user;
        }
    }
}
