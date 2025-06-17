namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 存储并提供 JWT Token
    /// </summary>
    public class TokenService {
        private string? _token;
        public string? Token => _token;
        public void SetToken(string token) => _token = token;
    }
}
