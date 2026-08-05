namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 管理 WPF 进程内嵌入式 LocalWebAPI Kestrel 服务器的生命周期。
/// </summary>
public interface IEmbeddedLocalWebApiService
{
    /// <summary>指示服务器当前是否正在运行。</summary>
    bool IsRunning { get; }

    /// <summary>服务器正在监听的基地址 URL。</summary>
    string BaseUrl { get; }

    /// <summary>启动嵌入式服务器。幂等——已运行时不做任何操作。</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>优雅地停止嵌入式服务器。</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
