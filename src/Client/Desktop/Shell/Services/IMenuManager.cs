namespace LYBT.Desktop.Shell.Services;

using System.Windows.Input;
using Prism.Commands;

/// <summary>
/// 菜单命令管理器接口
/// </summary>
public interface IMenuManager
{
    ICommand QuickAddPatientCommand { get; }
    ICommand QuickStartMedicalCaseCommand { get; }
    ICommand ShowHelpCommand { get; }
    ICommand ShowSettingsCommand { get; }
    ICommand ToggleThemeCommand { get; }
    ICommand SaveAllCommand { get; }
    ICommand RefreshAllCommand { get; }
    ICommand PrintCommand { get; }
    ICommand ExportCommand { get; }
    ICommand UndoCommand { get; }
    ICommand RedoCommand { get; }
    ICommand EditProfileCommand { get; }
    ICommand NavigateToHomeCommand { get; }
    ICommand NavigateToSystemSettingsCommand { get; }
    DelegateCommand NavigateBackCommand { get; }
    DelegateCommand NavigateForwardCommand { get; }
}
