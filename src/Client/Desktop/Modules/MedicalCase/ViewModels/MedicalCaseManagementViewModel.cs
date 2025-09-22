using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 医疗案例管理视图模型（兼容迁移至 ModernManagementViewModel 体系）。
/// 保持原有命令/属性命名，尽量不影响现有 XAML 绑定。
/// </summary>
public class MedicalCaseManagementViewModel : ModernManagementViewModel<MedicalCaseDto>
{
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly ICustomDialogService _dialogService;
    private readonly ILogger<MedicalCaseManagementViewModel> _logger;

    // 过滤条件
    private string _filterStatus = "全局状态";
    public string FilterStatus
    {
        get => _filterStatus;
        set => SetProperty(ref _filterStatus, value);
    }

    private DateTime? _startDate;
    public DateTime? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    private DateTime? _endDate;
    public DateTime? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    // 状态栏文本
    public string StatusText => $"共 {TotalCount} 条记录";

    // 自定义命令（基类已提供 Search/Add/Edit/Delete/ViewDetails/Refresh/PreviousPage/NextPage）
    public DelegateCommand<MedicalCaseDto> ViewConsultationCommand { get; private set; } = null!;
    public DelegateCommand<MedicalCaseDto> CreatePrescriptionCommand { get; private set; } = null!;
    public DelegateCommand<MedicalCaseDto> PrintCommand { get; private set; } = null!;

    // 兼容旧分页按钮（基类已有 Previous/Next）
    public DelegateCommand FirstPageCommand { get; private set; } = null!;
    public DelegateCommand LastPageCommand { get; private set; } = null!;

    public MedicalCaseManagementViewModel(
        IMedicalCaseService medicalCaseService,
        ICustomDialogService dialogService,
        IEventAggregator eventAggregator,
        IErrorHandlingService? errorHandlingService = null,
        ILogger<MedicalCaseManagementViewModel>? logger = null)
        : base(eventAggregator, errorHandlingService)
    {
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MedicalCaseManagementViewModel>.Instance;

        InitializeCommandsCompat();

        // 首次加载
        RefreshCommand.Execute();
    }

    private void InitializeCommandsCompat()
    {
        ViewConsultationCommand = new DelegateCommand<MedicalCaseDto>(async (item) => await ViewConsultationAsync(item), (item) => item != null);
        CreatePrescriptionCommand = new DelegateCommand<MedicalCaseDto>(async (item) => await CreatePrescriptionAsync(item), (item) => item != null);
        PrintCommand = new DelegateCommand<MedicalCaseDto>(async (item) => await PrintCaseAsync(item), (item) => item != null);

        FirstPageCommand = new DelegateCommand(() =>
        {
            if (CurrentPage != 1)
            {
                CurrentPage = 1;
                RefreshCommand.Execute();
            }
        });

        LastPageCommand = new DelegateCommand(() =>
        {
            if (CurrentPage != TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
                RefreshCommand.Execute();
            }
        });
    }

    // 基类数据加载回调
    protected override async Task<ServiceResult<PagedResult<MedicalCaseDto>>> LoadDataAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            _logger.LogInformation("加载医疗案例数据，页码: {Page}, 页大小: {Size}, 关键字: {Keyword}", page, pageSize, keyword);

            var request = new PagedQueryBaseDto
            {
                PageIndex = page,
                PageSize = pageSize,
                Keyword = keyword
            };

            if (!string.IsNullOrWhiteSpace(FilterStatus)) request.Extensions["Status"] = FilterStatus!;
            if (StartDate.HasValue) request.Extensions["StartDate"] = StartDate.Value;
            if (EndDate.HasValue) request.Extensions["EndDate"] = EndDate.Value;

            return await _medicalCaseService.GetPagedAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载医疗案例数据异常");
            return ServiceResult<PagedResult<MedicalCaseDto>>.Failure($"加载失败: {ex.Message}");
        }
    }

    // 映射到统一命令
    protected override Task OnAddAsync() => AddCaseAsync();
    protected override Task OnEditAsync(MedicalCaseDto item) => EditCaseAsync(item);
    protected override Task OnDeleteAsync(MedicalCaseDto item) => DeleteCaseAsync(item);
    protected override Task OnViewDetailsAsync(MedicalCaseDto item) => ViewDetailsAsync(item);

    protected override void RaiseCanExecuteChanged()
    {
        base.RaiseCanExecuteChanged();
        ViewConsultationCommand.RaiseCanExecuteChanged();
        CreatePrescriptionCommand.RaiseCanExecuteChanged();
        PrintCommand.RaiseCanExecuteChanged();
    }

    #region 业务方法（复制自原实现，保持行为）

    private async Task AddCaseAsync()
    {
        try
        {
            _logger.LogInformation("打开新建医疗案例对话框");

            var parameters = new Dictionary<string, object>();
            var result = await _dialogService.ShowDialogAsync("CreateMedicalCaseDialog", parameters);

            if (result.Result == true)
            {
                _logger.LogInformation("医疗案例创建成功，刷新数据列表");
                RefreshCommand.Execute();
                await _dialogService.ShowSuccessAsync("医疗案例创建成功", "成功");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建医疗案例时发生异常");
            await _dialogService.ShowErrorAsync($"创建医疗案例失败: {ex.Message}", "错误");
        }
    }

    private async Task ViewDetailsAsync(MedicalCaseDto medicalCase)
    {
        if (medicalCase == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("查看医疗案例详情: {CaseId}", medicalCase.Id);

            var result = await _medicalCaseService.GetByIdAsync(medicalCase.Id);
            if (result.IsSuccess && result.Data != null)
            {
                var detailInfo = $"案例ID: {result.Data.Id}\n" +
                                 $"患者: {result.Data.PatientName}\n" +
                                 $"医生: {result.Data.DoctorName}\n" +
                                 $"创建时间: {result.Data.CreateTime:yyyy-MM-dd HH:mm}\n" +
                                 $"状态: {result.Data.Status}\n" +
                                 $"诊疗结果: {result.Data.DiagnosisResult ?? "暂无"}\n" +
                                 $"备注: {result.Data.Remark ?? "暂无"}";

                await _dialogService.ShowInformationAsync(detailInfo, $"医疗案例详情 - {result.Data.PatientName}");
            }
            else
            {
                await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "获取医疗案例详情失败", "错误");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查看医疗案例详情时发生异常");
            await _dialogService.ShowErrorAsync($"查看详情失败: {ex.Message}", "错误");
        }
    }

    private async Task EditCaseAsync(MedicalCaseDto medicalCase)
    {
        if (medicalCase == null)
        {
            return;
        }

        _logger.LogInformation("编辑医疗案例: {CaseId}", medicalCase.Id);
        // TODO: 实现编辑逻辑
        await Task.CompletedTask;
    }

    private async Task ViewConsultationAsync(MedicalCaseDto medicalCase)
    {
        if (medicalCase == null)
        {
            return;
        }

        _logger.LogInformation("查看诊疗记录: {CaseId}", medicalCase.Id);
        // TODO: 实现查看诊疗记录逻辑
        await Task.CompletedTask;
    }

    private async Task CreatePrescriptionAsync(MedicalCaseDto medicalCase)
    {
        if (medicalCase == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("为案例 {CaseId} 创建处方: {PatientName}", medicalCase.Id, medicalCase.PatientName);

            var parameters = new Dictionary<string, object>
            {
                ["IsEditMode"] = false,
                ["MedicalCaseId"] = medicalCase.Id,
                ["PatientId"] = medicalCase.PatientId,
                ["PatientName"] = medicalCase.PatientName,
                ["ContextMode"] = "MedicalCase"
            };

            var result = await _dialogService.ShowDialogAsync("PrescriptionEditorDialog", parameters);

            if (result.Result == true)
            {
                _logger.LogInformation("处方创建成功: {CaseId}", medicalCase.Id);
                await _dialogService.ShowSuccessAsync($"为患者 {medicalCase.PatientName} 创建的处方已保存", "操作成功");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建处方时发生异常: {CaseId}", medicalCase.Id);
            await _dialogService.ShowErrorAsync($"创建处方失败: {ex.Message}", "错误");
        }
    }

    private async Task PrintCaseAsync(MedicalCaseDto medicalCase)
    {
        if (medicalCase == null)
        {
            return;
        }

        _logger.LogInformation("打印医疗案例: {CaseId}", medicalCase.Id);
        // TODO: 实现打印逻辑
        await Task.CompletedTask;
    }

    private async Task DeleteCaseAsync(MedicalCaseDto medicalCase)
    {
        if (medicalCase == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("删除医疗案例: {CaseId}", medicalCase.Id);

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要删除医疗案例？\n" +
                $"患者: {medicalCase.PatientName}\n" +
                $"创建时间: {medicalCase.CreateTime:yyyy-MM-dd HH:mm}\n" +
                $"此操作不可恢复",
                "确认删除");

            if (confirm)
            {
                var result = await _medicalCaseService.DeleteAsync(medicalCase.Id);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("医疗案例删除成功: {CaseId}", medicalCase.Id);
                    RefreshCommand.Execute();
                    await _dialogService.ShowInformationAsync("医疗案例删除成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "删除失败", "错误");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除医疗案例时发生异常");
            await _dialogService.ShowErrorAsync($"删除失败: {ex.Message}", "错误");
        }
    }

    #endregion 业务方法
}
