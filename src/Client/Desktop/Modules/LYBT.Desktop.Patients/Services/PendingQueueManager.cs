using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Api; // IMedicalCaseApi
using LYBT.Desktop.Contracts.Services; // ISessionManager
using LYBT.Desktop.Patients.ViewModels.Components; // PatientService
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 待诊队列管理器 - 负责待诊队列加载和患者选择逻辑
/// Issue #1790: 从PatientSelectionViewModel提取待诊队列逻辑(~100行)
/// </summary>
public class PendingQueueManager
{
    private readonly IMedicalCaseApi _medicalCaseApi;
    private readonly PatientService _commandHandler;
    private readonly UnfinishedCaseHandler _unfinishedCaseHandler;
    private readonly ILogger<PendingQueueManager> _logger;
    private readonly ISessionManager _sessionManager;

    /// <summary>
    /// 待诊队列（未完成医案的患者列表）
    /// </summary>
    public ObservableCollection<PendingMedicalCaseDto> PendingQueue { get; } = new();

    /// <summary>
    /// 待诊队列加载完成事件
    /// </summary>
    public event EventHandler<PendingQueueLoadedEventArgs>? PendingQueueLoaded;

    /// <summary>
    /// 患者详情加载完成事件
    /// </summary>
    public event EventHandler<PatientLoadedEventArgs>? PatientLoaded;

    public PendingQueueManager(
        IMedicalCaseApi medicalCaseApi,
        PatientService commandHandler,
        UnfinishedCaseHandler unfinishedCaseHandler,
        ISessionManager sessionManager,
        ILogger<PendingQueueManager> logger)
    {
        _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _unfinishedCaseHandler = unfinishedCaseHandler ?? throw new ArgumentNullException(nameof(unfinishedCaseHandler));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 加载待看诊队列
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// </summary>
    public async Task LoadPendingCasesAsync()
    {
        try
        {
            _logger.LogInformation("开始加载待看诊队列");

            // Epic #2210 Phase 3: 获取当前医生ID并传递给API
            if (_sessionManager.CurrentUserId == null)
            {
                _logger.LogWarning("当前用户ID为空，无法加载待看诊队列");
                return;
            }

            // OpenSpec: unify-pending-query-api - 不传patientId，获取当前医生的所有待看诊医案
            // Server从JWT获取当前登录用户ID进行数据隔离
            var response = await _medicalCaseApi.GetPendingCasesAsync();

            if (response.Success && response.Data != null)
            {
                // Epic #2210 Phase 3: 使用Dispatcher在UI线程更新ObservableCollection
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    PendingQueue.Clear();
                    foreach (var item in response.Data)
                    {
                        PendingQueue.Add(item);
                    }
                });

                _logger.LogInformation("待看诊队列加载完成，共{Count}条记录", PendingQueue.Count);

                // 触发事件
                PendingQueueLoaded?.Invoke(this, new PendingQueueLoadedEventArgs
                {
                    QueueCount = PendingQueue.Count,
                    PendingCases = response.Data
                });
            }
            else
            {
                _logger.LogWarning("加载待看诊队列失败：{Message}", response.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载待看诊队列异常");
        }
    }

    /// <summary>
    /// 为待看诊队列选中的患者加载完整信息
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// RESTful重构: 移除currentPatients参数，PatientListDto不含完整详情，直接调用API
    /// </summary>
    public async Task<PatientDetailDto?> LoadPatientForPendingCaseAsync(Guid patientId)
    {
        try
        {
            _logger.LogInformation("加载患者详情：PatientId={PatientId}", patientId);

            // 通过CommandHandler加载完整详情
            var result = await _commandHandler.GetByIdAsync(patientId);
            if (result.IsSuccess && result.Data != null)
            {
                _logger.LogInformation("从API加载患者成功：{PatientName}", result.Data.Name);

                // 触发事件
                PatientLoaded?.Invoke(this, new PatientLoadedEventArgs
                {
                    Patient = result.Data,
                    Source = "API"
                });

                return result.Data;
            }
            else
            {
                _logger.LogWarning("加载患者详情失败：{ErrorMessage}", result.ErrorMessage);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载患者详情异常：PatientId={PatientId}", patientId);
            return null;
        }
    }

    /// <summary>
    /// 从待诊队列中移除患者
    /// </summary>
    public void RemoveFromQueue(Guid patientId)
    {
        var item = PendingQueue.FirstOrDefault(p => p.PatientId == patientId);
        if (item != null)
        {
            PendingQueue.Remove(item);
            _logger.LogInformation("已从待诊队列移除患者：PatientId={PatientId}", patientId);
        }
    }

    /// <summary>
    /// 清空待诊队列
    /// </summary>
    public void ClearQueue()
    {
        PendingQueue.Clear();
        _logger.LogInformation("待诊队列已清空");
    }
}

/// <summary>
/// 待诊队列加载完成事件参数
/// Issue #1790: 封装事件数据
/// </summary>
public class PendingQueueLoadedEventArgs : EventArgs
{
    public int QueueCount { get; set; }
    public List<PendingMedicalCaseDto> PendingCases { get; set; } = new();
}

/// <summary>
/// 患者详情加载完成事件参数
/// Issue #1790: 封装事件数据
/// </summary>
public class PatientLoadedEventArgs : EventArgs
{
    public PatientDetailDto Patient { get; set; } = null!;
    public string Source { get; set; } = string.Empty;
}
