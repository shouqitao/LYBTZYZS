namespace LYBT.Desktop.Infrastructure.Navigation;

/// <summary>
/// Region 集合监控接口
/// </summary>
public interface IRegionMonitor : IDisposable
{
    /// <summary>开始监控</summary>
    void StartMonitoring();

    /// <summary>停止监控</summary>
    void StopMonitoring();
}
