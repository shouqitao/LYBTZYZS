using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// Token管理器实现
    /// </summary>
    public class TokenManager : ITokenManager
    {
        private string? _token;

        public string? GetToken() => _token;

        public void SetToken(string token)
        {
            _token = token;
        }

        public void ClearToken()
        {
            _token = null;
        }
    }
}