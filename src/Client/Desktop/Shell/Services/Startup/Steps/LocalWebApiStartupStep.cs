using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// 启动嵌入式 LocalWebAPI 服务器（本地模式用）。
/// 非必需步骤——失败只记录警告，不阻塞启动。
/// </summary>
public class LocalWebApiStartupStep : IStartupStep
{
    private readonly IEmbeddedLocalWebApiService _localWebApi;
    private readonly ILogger<LocalWebApiStartupStep> _logger;

    public LocalWebApiStartupStep(
        IEmbeddedLocalWebApiService localWebApi,
        ILogger<LocalWebApiStartupStep> logger)
    {
        _localWebApi = localWebApi;
        _logger = logger;
    }

    public string Name => "本地 API 服务";
    public int Order => 250;
    public bool IsRequired => false;
    public string? ParallelGroup => null;

    public async Task<StartupStepResult> ExecuteAsync(
        IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report("正在启动本地 API 服务...");
            await _localWebApi.StartAsync(cancellationToken);
            _logger.LogInformation("本地 API 服务已启动于 {Url}", _localWebApi.BaseUrl);
            return StartupStepResult.Succeeded(TimeSpan.Zero);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "本地 API 服务启动失败");
            return StartupStepResult.Failed(
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("启动本地API", ex),
                ex);
        }
    }
}
