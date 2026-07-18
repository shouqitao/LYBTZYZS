using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Auth.ViewModels;

/// <summary>
/// 登录凭证 UI 状态 — 用户名/密码输入、记住密码
/// 从 LoginViewModel 提取，单一职责：凭证输入状态管理
/// </summary>
public partial class LoginCredentialsViewModel : CoreViewModelBase
{
    private readonly IUsernameStorageService? _usernameStorage;
    private readonly ICredentialVault? _credentialVault;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberUsername;

    [ObservableProperty]
    private bool _rememberPassword;

    [ObservableProperty]
    private bool _hasSavedPassword;

    [ObservableProperty]
    private string? _savedUsername;

    public LoginCredentialsViewModel(
        IViewModelServices services,
        IUsernameStorageService? usernameStorage,
        ICredentialVault? credentialVault)
        : base(services)
    {
        _usernameStorage = usernameStorage;
        _credentialVault = credentialVault;
    }

    /// <summary>
    /// 加载已保存的凭证
    /// </summary>
    public async Task LoadSavedCredentialsAsync()
    {
        try
        {
            if (_usernameStorage != null)
            {
                var savedUsername = await _usernameStorage.GetSavedUsernameAsync();
                var isRememberMeEnabled = await _usernameStorage.IsRememberMeEnabledAsync();
                if (!string.IsNullOrEmpty(savedUsername))
                {
                    string? savedPassword = null;
                    bool hasSavedPassword = false;
                    if (_credentialVault != null)
                    {
                        hasSavedPassword = await _credentialVault.HasSavedPasswordAsync(savedUsername);
                        if (hasSavedPassword)
                        {
                            savedPassword = await _credentialVault.GetPasswordAsync(savedUsername);
                        }
                    }

                    await Services.UiThreadDispatcher.InvokeAsync(() =>
                    {
                        SavedUsername = savedUsername;
                        Username = savedUsername;
                        RememberUsername = isRememberMeEnabled;
                        HasSavedPassword = hasSavedPassword;
                        if (!string.IsNullOrEmpty(savedPassword))
                        {
                            Password = savedPassword;
                            RememberPassword = true;
                        }
                        else
                        {
                            RememberPassword = false;
                        }
                    });
                }
            }
        }
        catch (Exception ex) { Logger.LogError(ex, "[VM] Login.LoadCredentials failed"); }
    }

    /// <summary>
    /// 登录成功后保存凭证
    /// </summary>
    public async Task SaveCredentialsAsync(bool rememberPassword)
    {
        try
        {
            if (_usernameStorage != null)
            {
                if (RememberUsername)
                {
                    await _usernameStorage.SaveUsernameAsync(Username, rememberMe: true);
                }
                else
                {
                    await _usernameStorage.ClearUsernameAsync();
                }
            }
            if (_credentialVault != null)
            {
                if (!string.IsNullOrEmpty(rememberPassword ? Password : null))
                {
                    await _credentialVault.SavePasswordAsync(Username, Password);
                }
                else
                {
                    await _credentialVault.ClearPasswordAsync(Username);
                }
            }
        }
        catch (Exception ex) { Logger.LogError(ex, "[VM] Login.SaveCredentials failed"); }
    }

    /// <summary>
    /// RememberUsername 变更时清除已保存的用户名
    /// </summary>
    partial void OnRememberUsernameChanged(bool value)
    {
        if (!value)
        {
            _ = ClearSavedUsernameAsync();
        }
    }

    /// <summary>
    /// RememberPassword 变更时自动勾选 RememberUsername，并清除已保存密码
    /// </summary>
    partial void OnRememberPasswordChanged(bool value)
    {
        if (value && !RememberUsername)
        {
            RememberUsername = true;
        }
        if (!value)
        {
            _ = ClearSavedPasswordAsync();
        }
    }

    /// <summary>
    /// Username 变更时，如果改为非保存用户名则清除密码
    /// </summary>
    partial void OnUsernameChanged(string value)
    {
        if (SavedUsername != null && !string.IsNullOrEmpty(SavedUsername)
            && !string.IsNullOrEmpty(value) && value != SavedUsername
            && !string.IsNullOrEmpty(Password))
        {
            Password = string.Empty;
            HasSavedPassword = false;
        }
    }

    private async Task ClearSavedUsernameAsync()
    {
        try
        {
            if (_usernameStorage != null)
            {
                await _usernameStorage.ClearUsernameAsync();
                Logger.LogInformation("[VM] Login.ClearSavedUsername - 已清除保存的用户名");
            }
        }
        catch (Exception ex) { Logger.LogError(ex, "[VM] Login.ClearSavedUsername failed"); }
    }

    private async Task ClearSavedPasswordAsync()
    {
        try
        {
            if (_credentialVault != null && !string.IsNullOrEmpty(Username))
            {
                await _credentialVault.ClearPasswordAsync(Username);
                Logger.LogInformation("[VM] Login.ClearSavedPassword - 已清除用户 {Username} 的保存密码", Username);
            }
        }
        catch (Exception ex) { Logger.LogError(ex, "[VM] Login.ClearSavedPassword failed"); }
    }
}
