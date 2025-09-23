using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services.Navigation;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 病历列表视图模型 - 基于ModernManagementViewModel
/// 使用MedicalCaseItem作为UI模型，替代直接使用MedicalCaseDto
/// 保持原有XAML绑定兼容性，确保功能不变
/// </summary>
public class MedicalCaseListViewModel2 : ModernManagementViewModel<MedicalCaseItem>
{
    #region Fields

    private readonly IMedicalCaseService _medicalCaseService;
    private readonly ICustomDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IMapper _mapper;
    private readonly ILogger<MedicalCaseListViewModel2> _logger;

    private string _statusFilter = "All";

    #endregion

    #region Properties

    /// <summary>
    /// 选中的病历 - 兼容原有绑定
    /// </summary>
    public MedicalCaseItem? SelectedMedicalCase
    {
        get => SelectedItem;
        set => SelectedItem = value;
    }

    /// <summary>
    /// 状态筛选
    /// </summary>
    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    /// <summary>
    /// 开始问诊命令
    /// </summary>
    public DelegateCommand StartConsultationCommand { get; }

    /// <summary>
    /// 完成病历命令
    /// </summary>
    public DelegateCommand CompleteCommand { get; }

    /// <summary>
    /// 取消病历命令
    /// </summary>
    public DelegateCommand CancelCommand { get; }

    #endregion

    #region Constructor

    public MedicalCaseListViewModel2(
        IMedicalCaseService medicalCaseService,
        ICustomDialogService dialogService,
        INavigationService navigationService,
        IEventAggregator eventAggregator,
        IMapper mapper,
        ILogger<MedicalCaseListViewModel2> logger)
        : base(eventAggregator, dialogService)
    {
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化额外命令
        StartConsultationCommand = new DelegateCommand(
            async () => await StartConsultationAsync(),
            () => CanStartConsultation());

        CompleteCommand = new DelegateCommand(
            async () => await CompleteCaseAsync(),
            () => CanComplete());

        CancelCommand = new DelegateCommand(
            async () => await CancelCaseAsync(),
            () => CanCancel());
    }

    #endregion

    #region Command Methods Override

    /// <summary>
    /// 加载数据实现
    /// </summary>
    protected override async Task LoadDataAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var searchDto = new MedicalCaseSearchDto
            {
                Keyword = SearchKeyword,
                Status = ParseStatusFilter(),
                PageNumber = CurrentPage,
                PageSize = PageSize
            };

            var result = await _medicalCaseService.SearchCasesAsync(searchDto);

            if (result.IsSuccess && result.Data != null)
            {
                // 转换DTO到UI模型
                Items.Clear();
                foreach (var dto in result.Data.Items)
                {
                    Items.Add(MedicalCaseItem.FromDto(dto));
                }

                TotalCount = result.Data.TotalCount;
            }
            else
            {
                await ShowErrorAsync(result.ErrorMessage ?? "加载病历数据失败");
            }
        });
    }

    /// <summary>
    /// 搜索实现
    /// </summary>
    protected override async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>
    /// 添加实现 - 创建新病历
    /// </summary>
    protected override async Task AddAsync()
    {
        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "Mode", "Create" }
        };

        await _dialogService.ShowDialogAsync("MedicalCaseCreateDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("病历创建成功");
            }
        });
    }

    /// <summary>
    /// 编辑实现
    /// </summary>
    protected override async Task EditAsync()
    {
        if (SelectedItem == null) return;

        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "Mode", "Edit" },
            { "MedicalCaseId", SelectedItem.Id }
        };

        await _dialogService.ShowDialogAsync("MedicalCaseEditDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("病历更新成功");
            }
        });
    }

    /// <summary>
    /// 删除实现
    /// </summary>
    protected override async Task DeleteAsync()
    {
        if (SelectedItem == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"确定要删除病历 {SelectedItem.CaseNumber} 吗？\n此操作不可恢复。",
            "确认删除");

        if (confirmed)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _medicalCaseService.DeleteAsync(SelectedItem.Id);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync("病历删除成功");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? "删除失败");
                }
            });
        }
    }

    /// <summary>
    /// 查看详情实现
    /// </summary>
    protected override async Task ViewDetailsAsync()
    {
        if (SelectedItem == null) return;

        // 使用NavigationService导航
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", SelectedItem.Id }
        };

        await _navigationService.NavigateToAsync(
            RegionNames.SystemWorkbenchContentRegion,
            "MedicalCaseDetailView",
            parameters);
    }

    #endregion

    #region Additional Methods

    /// <summary>
    /// 开始问诊
    /// </summary>
    private async Task StartConsultationAsync()
    {
        if (SelectedItem == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _medicalCaseService.UpdateStatusAsync(
                SelectedItem.Id,
                MedicalCaseStatus.Active);

            if (result.IsSuccess)
            {
                // 导航到问诊界面
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", SelectedItem.Id },
                    { "PatientId", SelectedItem.PatientId }
                };

                await _navigationService.NavigateToAsync(
                    RegionNames.ConsultationWorkbenchContentRegion,
                    "ConsultationMainView",
                    parameters);

                await ShowSuccessAsync("已开始问诊");
            }
            else
            {
                await ShowErrorAsync(result.ErrorMessage ?? "开始问诊失败");
            }
        });
    }

    /// <summary>
    /// 完成病历
    /// </summary>
    private async Task CompleteCaseAsync()
    {
        if (SelectedItem == null) return;

        var reason = await _dialogService.ShowInputAsync(
            "请输入完成原因",
            "完成病历",
            "治疗完成");

        if (!string.IsNullOrEmpty(reason))
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _medicalCaseService.CompleteAsync(
                    SelectedItem.Id,
                    reason);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync("病历已完成");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? "完成病历失败");
                }
            });
        }
    }

    /// <summary>
    /// 取消病历
    /// </summary>
    private async Task CancelCaseAsync()
    {
        if (SelectedItem == null) return;

        var reason = await _dialogService.ShowInputAsync(
            "请输入取消原因",
            "取消病历",
            "患者取消");

        if (!string.IsNullOrEmpty(reason))
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var updateDto = new MedicalCaseUpdateDto
                {
                    Status = MedicalCaseStatus.Cancelled,
                    CompletionReason = reason
                };

                var result = await _medicalCaseService.UpdateAsync(
                    SelectedItem.Id,
                    updateDto);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync("病历已取消");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? "取消病历失败");
                }
            });
        }
    }

    /// <summary>
    /// 解析状态筛选
    /// </summary>
    private MedicalCaseStatus? ParseStatusFilter()
    {
        return StatusFilter switch
        {
            "Active" => MedicalCaseStatus.Active,
            "Closed" => MedicalCaseStatus.Closed,
            "Cancelled" => MedicalCaseStatus.Cancelled,
            _ => null
        };
    }

    /// <summary>
    /// 是否可以开始问诊
    /// </summary>
    private bool CanStartConsultation()
    {
        return SelectedItem != null && SelectedItem.CanStartConsultation;
    }

    /// <summary>
    /// 是否可以完成
    /// </summary>
    private bool CanComplete()
    {
        return SelectedItem != null && SelectedItem.IsActive;
    }

    /// <summary>
    /// 是否可以取消
    /// </summary>
    private bool CanCancel()
    {
        return SelectedItem != null && SelectedItem.IsActive;
    }

    /// <summary>
    /// 选中项变化处理
    /// </summary>
    protected override void OnSelectedItemChanged(MedicalCaseItem? newItem)
    {
        base.OnSelectedItemChanged(newItem);

        // 更新命令状态
        StartConsultationCommand.RaiseCanExecuteChanged();
        CompleteCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// 初始化
    /// </summary>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await LoadDataAsync();
    }

    #endregion
}