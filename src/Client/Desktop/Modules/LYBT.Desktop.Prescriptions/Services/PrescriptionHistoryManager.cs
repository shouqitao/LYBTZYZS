using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels; // Issue #1790: PrescriptionItemViewModel
using LYBT.Desktop.Modules.Prescriptions.ViewModels.Components;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Modules.Prescriptions.Services;

/// <summary>
/// 处方历史管理器 - 负责加载和复制历史处方
/// Issue #1790: 从PrescriptionViewModel提取历史处方管理逻辑(~120行)
/// Issue #1374: ENTRY-16 历史处方复制功能
/// </summary>
public class PrescriptionHistoryManager
{
    private readonly PrescriptionDataManager _dataManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IRegionManager _regionManager;
    private readonly ISessionManager? _sessionManager;
    private readonly IUserNotificationService? _userNotificationService;
    private readonly ILogger<PrescriptionHistoryManager> _logger;

    private ObservableCollection<PrescriptionSearchResultDto> _recentPrescriptions = new();
    private PrescriptionSearchResultDto? _selectedRecentPrescription;

    /// <summary>
    /// 患者最近处方列表 (Issue #1374 ENTRY-16)
    /// </summary>
    public ObservableCollection<PrescriptionSearchResultDto> RecentPrescriptions => _recentPrescriptions;

    /// <summary>
    /// 选中的历史处方 (Issue #1374 ENTRY-16)
    /// </summary>
    public PrescriptionSearchResultDto? SelectedRecentPrescription
    {
        get => _selectedRecentPrescription;
        set => _selectedRecentPrescription = value;
    }

    /// <summary>
    /// 历史处方复制完成事件
    /// </summary>
    public event EventHandler<HistoryCopiedEventArgs>? HistoryCopied;

    public PrescriptionHistoryManager(
        PrescriptionDataManager dataManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ILogger<PrescriptionHistoryManager> logger,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager;
        _userNotificationService = userNotificationService;
    }

    /// <summary>
    /// 加载患者最近处方列表 (Issue #1374 ENTRY-16)
    /// </summary>
    public async Task LoadRecentPrescriptionsAsync(MedicalCaseDto? currentMedicalCase)
    {
        try
        {
            if (currentMedicalCase?.PatientId == null || currentMedicalCase.PatientId == Guid.Empty)
            {
                _logger.LogWarning("无法加载历史处方：患者ID无效");
                return;
            }

            // Issue #1786: 使用DataManager包装Api方法
            var response = await _dataManager.GetPatientRecentPrescriptionsAsync(
                currentMedicalCase.PatientId,
                count: 5);
            var recentPrescriptions = response.Data ?? new List<PrescriptionSearchResultDto>();

            _recentPrescriptions.Clear();
            foreach (var prescription in recentPrescriptions)
            {
                _recentPrescriptions.Add(prescription);
            }

            _logger.LogInformation("已加载患者最近处方，共 {Count} 条", recentPrescriptions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载患者最近处方失败");
            // 不抛出异常，避免影响主流程
        }
    }

    /// <summary>
    /// 从历史处方复制 (Issue #1374 ENTRY-16)
    /// </summary>
    public void CopyFromHistory(PrescriptionSearchResultDto prescription)
    {
        if (prescription == null) return;

        try
        {
            _logger.LogInformation("从历史处方复制，处方ID: {PrescriptionId}, 患者: {PatientName}",
                prescription.Id, prescription.PatientName);

            // 清空当前处方项
            _dataManager.Clear();

            // 复制处方项
            var copiedItems = new List<PrescriptionItemViewModel>();
            foreach (var item in prescription.Items)
            {
                var newItem = new PrescriptionItemViewModel(
                    _eventAggregator,
                    _loggerFactory,
                    _regionManager,
                    _sessionManager,
                    _userNotificationService)
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Dosage = item.Dosage,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Remark = item.Remark
                };
                _dataManager.PrescriptionItems.Add(newItem);
                copiedItems.Add(newItem);
            }

            // 清空选择（避免重复触发）
            SelectedRecentPrescription = null;

            // 触发事件
            HistoryCopied?.Invoke(this, new HistoryCopiedEventArgs
            {
                PrescriptionId = prescription.Id,
                ItemCount = prescription.Items.Count,
                CopiedItems = copiedItems
            });

            _logger.LogInformation("历史处方复制完成，共 {Count} 味药材", prescription.Items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从历史处方复制时发生异常");
            throw;
        }
    }
}

/// <summary>
/// 历史处方复制完成事件参数
/// Issue #1790: 封装事件数据
/// </summary>
public class HistoryCopiedEventArgs : EventArgs
{
    public Guid PrescriptionId { get; set; }
    public int ItemCount { get; set; }
    public List<PrescriptionItemViewModel> CopiedItems { get; set; } = new();
}
