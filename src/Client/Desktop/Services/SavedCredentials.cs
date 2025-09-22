using System;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 持久化的凭证信息。
    /// </summary>
    public class SavedCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
