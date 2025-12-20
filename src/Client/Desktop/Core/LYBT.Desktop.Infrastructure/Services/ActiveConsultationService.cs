using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 活跃医案服务实现
/// OpenSpec: clarify-cancel-consultation-logic
/// 跟踪当前活跃的医案会话，并在退出登录时协调确认逻辑
/// </summary>
public class ActiveConsultationService : IActiveConsultationService
{
    private readonly ILogger<ActiveConsultationService> _logger;
    private readonly object _lock = new();

    private Guid? _activeMedicalCaseId;
    private Func<Task<LeaveConsultationResult>>? _leaveHandler;

    public ActiveConsultationService(ILogger<ActiveConsultationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool HasActiveConsultation
    {
        get
        {
            lock (_lock)
            {
                return _activeMedicalCaseId.HasValue && _leaveHandler != null;
            }
        }
    }

    /// <inheritdoc />
    public Guid? ActiveMedicalCaseId
    {
        get
        {
            lock (_lock)
            {
                return _activeMedicalCaseId;
            }
        }
    }

    /// <inheritdoc />
    public void Register(Guid medicalCaseId, Func<Task<LeaveConsultationResult>> leaveHandler)
    {
        lock (_lock)
        {
            _activeMedicalCaseId = medicalCaseId;
            _leaveHandler = leaveHandler ?? throw new ArgumentNullException(nameof(leaveHandler));
        }

        _logger.LogDebug("已注册活跃医案: {MedicalCaseId}", medicalCaseId);
    }

    /// <inheritdoc />
    public void Unregister()
    {
        Guid? previousId;
        lock (_lock)
        {
            previousId = _activeMedicalCaseId;
            _activeMedicalCaseId = null;
            _leaveHandler = null;
        }

        if (previousId.HasValue)
        {
            _logger.LogDebug("已注销活跃医案: {MedicalCaseId}", previousId);
        }
    }

    /// <inheritdoc />
    public async Task<LeaveConsultationResult> RequestLeaveAsync()
    {
        Func<Task<LeaveConsultationResult>>? handler;
        Guid? medicalCaseId;

        lock (_lock)
        {
            handler = _leaveHandler;
            medicalCaseId = _activeMedicalCaseId;
        }

        // 没有活跃医案，直接允许离开
        if (handler == null || !medicalCaseId.HasValue)
        {
            _logger.LogDebug("无活跃医案，允许直接离开");
            return LeaveConsultationResult.AllowLeave();
        }

        _logger.LogDebug("有活跃医案 {MedicalCaseId}，调用离开处理器", medicalCaseId);

        try
        {
            var result = await handler();
            _logger.LogDebug("离开处理器返回: CanLeave={CanLeave}, Choice={Choice}",
                result.CanLeave, result.Choice);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "离开处理器执行失败");
            // 出错时不允许离开，避免数据丢失
            return LeaveConsultationResult.CancelLeave();
        }
    }
}
