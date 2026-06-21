using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Sysadmin.Models;

/// <summary>
/// 单个状态卡片模型 - 用于仪表盘 2x2 网格中的每一个卡片
/// </summary>
public partial class StatusCard : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private string _status = "正常";

    [ObservableProperty]
    private bool _isHealthy = true;
}

/// <summary>
/// 仪表盘聚合状态 - 聚合 4 个状态卡片供视图绑定
/// </summary>
public partial class DashboardStatus : ObservableObject
{
    [ObservableProperty]
    private StatusCard _apiStatus = new() { Title = "API 状态", Value = "检测中..." };

    [ObservableProperty]
    private StatusCard _dbStatus = new() { Title = "数据库", Value = "检测中..." };

    [ObservableProperty]
    private StatusCard _loginCount = new() { Title = "今日登录", Value = "--" };

    [ObservableProperty]
    private StatusCard _systemInfo = new() { Title = "系统信息", Value = "加载中..." };

    [ObservableProperty]
    private bool _isLoading;
}
