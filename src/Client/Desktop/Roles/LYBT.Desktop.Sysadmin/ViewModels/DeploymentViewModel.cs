using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Net.Http;

namespace LYBT.Desktop.Sysadmin.ViewModels;

/// <summary>
/// 部署管理视图模型 - 上传更新包、重启服务
/// </summary>
public partial class DeploymentViewModel : NavigableViewModelBase
{
    private readonly IDeployApi _deployApi;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isUploading;
    [ObservableProperty] private bool _isRestarting;
    [ObservableProperty] private double _uploadProgress;
    [ObservableProperty] private string? _selectedFileName;

    public DeploymentViewModel(IViewModelServices services, IDeployApi deployApi)
        : base(services)
    {
        _deployApi = deployApi;
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

            var response = await _deployApi.UploadAsync(content);
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
            StatusMessage = $"上传失败: {ex.Message}";
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
            var response = await _deployApi.RestartAsync();
            StatusMessage = response.Success ? "重启指令已发送" : $"重启失败: {response.Message}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "重启服务失败");
            StatusMessage = $"重启失败: {ex.Message}";
        }
        finally
        {
            IsRestarting = false;
        }
    }

    private bool CanControl => !IsUploading && !IsRestarting;
}
