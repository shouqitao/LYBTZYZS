using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Net.Http;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 部署管理视图模型 - 上传更新包、重启服务
/// </summary>
public partial class DeploymentViewModel : NavigableViewModelBase
{
    private readonly IDeploymentService _deploymentService;
    private readonly INavigationCoordinator _navigationCoordinator;

    [ObservableProperty] private string _statusMessage = string.Empty;

    // I-3 修复：UploadCommand/RestartCommand 的 CanExecute 依赖以下三态，此前无任何通知 →
    // 选文件后上传按钮永不启用（上传功能不可达）、上传/重启中按钮状态陈旧
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UploadCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    private bool _isUploading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UploadCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    private bool _isRestarting;

    [ObservableProperty] private double _uploadProgress;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UploadCommand))]
    private string? _selectedFileName;

    public DeploymentViewModel(IViewModelServices services, IDeploymentService deploymentService, INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _deploymentService = deploymentService;
        _navigationCoordinator = navigationCoordinator;
        PageTitle = "部署管理";
    }

    [RelayCommand]
    private void SelectFile()
    {
        var dialog = new OpenFileDialog { Filter = "ZIP 文件|*.zip", Title = "选择更新包" };
        if (dialog.ShowDialog() == true)
        {
            SelectedFileName = dialog.FileName;
            StatusMessage = $"已选择: {Path.GetFileName(dialog.FileName)}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanUpload))]
    private async Task UploadAsync()
    {
        if (string.IsNullOrEmpty(SelectedFileName)) return;

        try
        {
            IsUploading = true;
            StatusMessage = "正在上传...";
            UploadProgress = 0;

            using var stream = File.OpenRead(SelectedFileName);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(stream), "file", Path.GetFileName(SelectedFileName));

            var response = await _deploymentService.UploadAsync(content);
            if (response.Success)
            {
                StatusMessage = "上传成功！";
                UploadProgress = 100;
            }
            else
            {
                StatusMessage = $"上传失败: {response.Message}";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "上传更新包失败");
            StatusMessage = "上传失败，请检查文件和网络连接";
        }
        finally
        {
            IsUploading = false;
        }
    }

    private bool CanUpload => !IsUploading && !IsRestarting && !string.IsNullOrEmpty(SelectedFileName);

    [RelayCommand(CanExecute = nameof(CanControl))]
    private async Task RestartAsync()
    {
        try
        {
            IsRestarting = true;
            StatusMessage = "正在重启服务...";
            var response = await _deploymentService.RestartAsync();
            StatusMessage = response.Success ? "重启指令已发送" : $"重启失败: {response.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "重启服务失败");
            StatusMessage = "重启失败，请稍后重试";
        }
        finally
        {
            IsRestarting = false;
        }
    }

    private bool CanControl => !IsUploading && !IsRestarting;

    [RelayCommand]
    private void GoBack() => _navigationCoordinator.NavigateBack();
}
