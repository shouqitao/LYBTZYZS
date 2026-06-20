using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// 临床工作台 ViewModel
/// 一体化布局：左侧患者列表（嵌入 PatientSelectionControl）+ 右侧看诊工作区
/// 直接作为嵌入 PatientSelectionControl 的 DataContext
/// </summary>
public partial class ClinicalWorkspaceViewModel : NavigableViewModelBase
{
    #region 依赖服务

    private readonly IPatientService _patientService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    #endregion 依赖服务

    #region 缓存（5 分钟过期，避免 OnNavigatedTo 重复全量拉取）

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private List<PatientListDto>? _patientListCache;
    private DateTime _patientListCachedAt;
    private string? _patientListCacheKeyword;

    private readonly Dictionary<Guid, (List<HistoryItem> Items, DateTime CachedAt)> _patientHistoryCache = new();

    #endregion 缓存（5 分钟过期，避免 OnNavigatedTo 重复全量拉取）

    #region 可观察属性

    /// <summary>患者列表</summary>
    [ObservableProperty]
    private ObservableCollection<PatientListDto> _patients = new();

    /// <summary>当前选中的患者</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyCanExecuteChangedFor(nameof(StartConsultationCommand))]
    private PatientListDto? _selectedPatient;

    /// <summary>患者详情（用于右侧 PatientViewControl）</summary>
    [ObservableProperty]
    private PatientDetailDto? _patientDetail;

    /// <summary>搜索关键词</summary>
    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    /// <summary>状态消息</summary>
    [ObservableProperty]
    private string _pageStatusMessage = string.Empty;

    /// <summary>选中患者的最近就诊记录（最多 5 条）</summary>
    [ObservableProperty]
    private ObservableCollection<HistoryItem> _patientHistory = new();

    #endregion 可观察属性

    #region 计算属性

    /// <summary>是否有选中患者</summary>
    public bool HasSelection => SelectedPatient != null;

    #endregion 计算属性

    #region 构造函数

    public ClinicalWorkspaceViewModel(
        IViewModelServices services,
        IPatientService patientService,
        INavigationCoordinator navigationCoordinator,
        IMedicalCaseRepository medicalCaseRepository)
        : base(services)
    {
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));

        PageTitle = "看诊工作台";
    }

    #endregion 构造函数

    #region 属性变更处理

    /// <summary>SelectedPatient 变更：加载详情并记录日志</summary>
    partial void OnSelectedPatientChanged(PatientListDto? value)
    {
        Logger.LogInformation("选中患者变更: {Name}", value?.Name ?? "(无)");
        _ = LoadPatientDetailAsync();
        _ = LoadPatientHistoryAsync();
    }

    #endregion 属性变更处理

    #region 命令

    /// <summary>开始看诊：导航到医案工作区（临床模式）</summary>
    [RelayCommand(CanExecute = nameof(CanStartConsultation))]
    private void StartConsultation()
    {
        if (SelectedPatient == null) return;

        try
        {
            Logger.LogInformation("开始看诊 - 患者: {Name} ({Id})", SelectedPatient.Name, SelectedPatient.Id);
            var navParams = MedicalCaseNavigationParameters.ForClinical(SelectedPatient.Id);
            _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, new Dictionary<string, object>(navParams));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导航到医案工作区失败");
        }
    }

    private bool CanStartConsultation() => SelectedPatient != null;

    /// <summary>新建患者：导航到患者管理（带 AddNew 参数）</summary>
    [RelayCommand]
    private void NewPatient()
    {
        try
        {
            Logger.LogInformation("导航到患者管理（新建）");
            var parameters = new Dictionary<string, object> { { "Action", "AddNew" } };
            _navigationCoordinator.NavigateTo(ViewNames.PatientManagement, parameters);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导航到患者管理失败");
        }
    }

    /// <summary>刷新患者列表</summary>
    [RelayCommand]
    private async Task RefreshAsync() => await LoadPatientsAsync();

    /// <summary>搜索患者</summary>
    [RelayCommand]
    private async Task SearchAsync() => await LoadPatientsAsync();

    #endregion 命令

    #region 私有方法

    /// <summary>加载患者列表</summary>
    private async Task LoadPatientsAsync()
    {
        // 命中缓存且未过期 → 直接使用，避免重复全量拉取
        var keyword = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword;
        if (_patientListCache != null
            && (DateTime.UtcNow - _patientListCachedAt) < CacheTtl
            && string.Equals(_patientListCacheKeyword, keyword, StringComparison.Ordinal))
        {
            Patients = new ObservableCollection<PatientListDto>(_patientListCache);
            PageStatusMessage = $"共 {_patientListCache.Count} 位患者（缓存）";
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _patientService.GetPatientsPagedAsync(page: 1, pageSize: 100, keyword: keyword);

            if (result.Success && result.Data != null)
            {
                var items = result.Data.Items.ToList();
                Patients = new ObservableCollection<PatientListDto>(items);
                PageStatusMessage = $"共 {result.Data.TotalCount} 位患者";
                Logger.LogInformation("加载患者列表成功，共 {Count} 条", result.Data.TotalCount);

                // 写入缓存
                _patientListCache = items;
                _patientListCachedAt = DateTime.UtcNow;
                _patientListCacheKeyword = keyword;
            }
            else
            {
                PageStatusMessage = result.Error ?? "加载患者列表失败";
                Logger.LogWarning("加载患者列表失败：{Message}", result.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者列表异常");
            PageStatusMessage = "加载患者列表异常";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>加载患者详情</summary>
    private async Task LoadPatientDetailAsync()
    {
        if (SelectedPatient == null)
        {
            PatientDetail = null;
            return;
        }

        try
        {
            var result = await _patientService.GetByIdAsync(SelectedPatient.Id);
            if (result.Success && result.Data != null)
            {
                PatientDetail = result.Data;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者详情失败");
        }
    }

    /// <summary>
    /// 加载选中患者的最近 5 条就诊记录
    /// 通过 IMedicalCaseRepository.QueryAsync + MedicalCaseQueryDto.PatientId 过滤
    /// </summary>
    private async Task LoadPatientHistoryAsync()
    {
        if (SelectedPatient == null)
        {
            PatientHistory.Clear();
            return;
        }

        // 命中缓存且未过期 → 直接使用
        var patientId = SelectedPatient.Id;
        if (_patientHistoryCache.TryGetValue(patientId, out var entry)
            && (DateTime.UtcNow - entry.CachedAt) < CacheTtl)
        {
            PatientHistory = new ObservableCollection<HistoryItem>(entry.Items);
            return;
        }

        try
        {
            var query = new MedicalCaseQueryDto
            {
                PatientId = patientId,
                PageIndex = 1,
                PageSize = 5,
            };
            var paged = await _medicalCaseRepository.QueryAsync(query);

            var items = paged?.Items ?? Enumerable.Empty<MedicalCaseListDto>();
            var history = items
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new HistoryItem
                {
                    Date = c.CreatedAt,
                    Diagnosis = c.Diagnosis ?? "（未填写诊断）",
                    Summary = BuildSummary(c),
                })
                .ToList();

            PatientHistory = new ObservableCollection<HistoryItem>(history);

            // 写入缓存
            _patientHistoryCache[patientId] = (history, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者历史就诊记录失败");
            PatientHistory.Clear();
        }
    }

    /// <summary>根据医案状态/处方情况构造简短摘要</summary>
    private static string BuildSummary(MedicalCaseListDto c)
    {
        var parts = new List<string>();
        if (c.HasConsultation) parts.Add("诊疗");
        if (c.HasPrescription) parts.Add("处方");
        if (!parts.Any()) parts.Add("无记录");
        return string.Join(" · ", parts);
    }

    #endregion 私有方法

    #region INavigationAware

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        _ = LoadPatientsAsync();
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        base.OnNavigatedFrom(navigationContext);
    }

    #endregion INavigationAware
}

/// <summary>历史就诊记录显示模型（用于 ClinicalWorkspace 历史面板）</summary>
public partial class HistoryItem
{
    /// <summary>就诊日期（取医案创建时间）</summary>
    public DateTime Date { get; set; }

    /// <summary>诊断摘要</summary>
    public string Diagnosis { get; set; } = string.Empty;

    /// <summary>备注（含诊疗/处方情况）</summary>
    public string Summary { get; set; } = string.Empty;
}
