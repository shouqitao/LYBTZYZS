using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Api; // IMedicalCaseApi
using LYBT.Desktop.Patients.ViewModels.Components; // PatientCommandHandler
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
    private readonly PatientCommandHandler _commandHandler;
    private readonly UnfinishedCaseHandler _unfinishedCaseHandler;
    private readonly ILogger<PendingQueueManager> _logger;

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
        PatientCommandHandler commandHandler,
        UnfinishedCaseHandler unfinishedCaseHandler,
        ILogger<PendingQueueManager> logger)
    {
        _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _unfinishedCaseHandler = unfinishedCaseHandler ?? throw new ArgumentNullException(nameof(unfinishedCaseHandler));
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

            var response = await _medicalCaseApi.GetPendingCasesAsync();

            if (response.Success && response.Data != null)
            {
                PendingQueue.Clear();
                foreach (var item in response.Data)
                {
                    PendingQueue.Add(item);
                }

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
    /// </summary>
    public async Task<PatientDto?> LoadPatientForPendingCaseAsync(
        Guid patientId,
        ObservableCollection<PatientDto> currentPatients)
    {
        try
        {
            _logger.LogInformation("加载患者详情：PatientId={PatientId}", patientId);

            // 先从当前患者列表中查找
            var patientInList = currentPatients.FirstOrDefault(p => p.Id == patientId);
            if (patientInList != null)
            {
                _logger.LogInformation("从当前列表中找到患者，直接返回");

                // 触发事件
                PatientLoaded?.Invoke(this, new PatientLoadedEventArgs
                {
                    Patient = patientInList,
                    Source = "CurrentList"
                });

                return patientInList;
            }

            // 列表中没有，通过CommandHandler加载
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
    public PatientDto Patient { get; set; } = null!;
    public string Source { get; set; } = string.Empty;
}
