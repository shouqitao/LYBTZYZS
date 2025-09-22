namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 凭证服务接口。
    /// </summary>
    public interface ICredentialService
    {
        void SaveCredentials(string username, string password, bool rememberMe);

        SavedCredentials? LoadCredentials();

        void DeleteCredentials();
    }
}

