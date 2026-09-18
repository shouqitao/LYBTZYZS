using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Shell.Services;
using System.Windows.Input;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// HeaderViewModel — 顶部应用栏 (h48)
/// 按 desktop-layout-framework §顶部应用栏 7 子元素，VM 委托 IShellServices.LoginState + Menu.EditProfile
/// N4: 增加后退按钮绑定（NavigateBackCommand → MenuManager → INavigationCoordinator）
/// </summary>
public partial class HeaderViewModel : ObservableObject, IDisposable
{
    private readonly IShellServices _shell;

    public string CurrentUserDisplayName => _shell.LoginState.CurrentUserDisplayName;
    public string CurrentUserRoleDisplay => _shell.LoginState.CurrentUserRoleDisplay;
    public string CurrentUserInitial => _shell.LoginState.CurrentUserInitial;

    public ICommand EditProfileCommand => _shell.Menu.EditProfileCommand;

    /// <summary>后退命令 — 与快捷键 Alt+Left / MenuManager.NavigateBackCommand 同源</summary>
    public ICommand NavigateBackCommand => _shell.Menu.NavigateBackCommand;

    public HeaderViewModel(IShellServices shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _shell.LoginState.LoginStateChanged += OnLoginStateChanged;
    }

    private void OnLoginStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentUserDisplayName));
        OnPropertyChanged(nameof(CurrentUserRoleDisplay));
        OnPropertyChanged(nameof(CurrentUserInitial));
    }

    public void Dispose()
    {
        _shell.LoginState.LoginStateChanged -= OnLoginStateChanged;
    }
}
