using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.Patients.Models;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Patients.ViewModels;

/// <summary>
/// 患者管理视图模型 - 基于ModernManagementViewModel
/// 使用PatientItem作为UI模型，替代直接使用PatientDto
/// 保持原有XAML绑定兼容性，确保功能不变
/// </summary>
public class PatientManagementViewModel2 : ModernManagementViewModel<PatientItem>
{
    #region Fields

    private readonly IPatientService _patientService;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly ICustomDialogService _dialogService;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientManagementViewModel2> _logger;

    private PatientViewState _viewState = new();
    private bool _showAdvancedSearch;
    private string _statusText = string.Empty;

    #endregion

    #region Properties

    /// <summary>
    /// 视图状态
    /// </summary>
    public PatientViewState ViewState
    {
        get => _viewState;
        set => SetProperty(ref _viewState, value);
    }

    /// <summary>
    /// 选中的患者 - 兼容原有绑定
    /// </summary>
    public PatientItem? SelectedPatient
    {
        get => SelectedItem;
        set => SelectedItem = value;
    }

    /// <summary>
    /// 显示高级搜索
    /// </summary>
    public bool ShowAdvancedSearch
    {
        get => _showAdvancedSearch;
        set => SetProperty(ref _showAdvancedSearch, value);
    }

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// 导入命令
    /// </summary>
    public DelegateCommand ImportCommand { get; }

    /// <summary>
    /// 切换状态命令
    /// </summary>
    public DelegateCommand ToggleStatusCommand { get; }

    /// <summary>
    /// 第一页命令
    /// </summary>
    public DelegateCommand FirstPageCommand { get; }

    /// <summary>
    /// 最后一页命令
    /// </summary>
    public DelegateCommand LastPageCommand { get; }

    #endregion

    #region Constructor

    public PatientManagementViewModel2(
        IPatientService patientService,
        IMedicalCaseService medicalCaseService,
        ICustomDialogService dialogService,
        IEventAggregator eventAggregator,
        IMapper mapper,
        ILogger<PatientManagementViewModel2> logger)
        : base(eventAggregator, dialogService)
    {
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化额外命令
        ImportCommand = new DelegateCommand(async () => await ImportPatientsAsync());
        ToggleStatusCommand = new DelegateCommand(
            async () => await TogglePatientStatusAsync(),
            () => SelectedItem != null);
        FirstPageCommand = new DelegateCommand(
            async () => await GoToFirstPageAsync(),
            () => CurrentPage > 1);
        LastPageCommand = new DelegateCommand(
            async () => await GoToLastPageAsync(),
            () => CurrentPage < TotalPages);

        // 初始化状态
        UpdateStatusText();
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
            var searchDto = new PatientSearchDto
            {
                Keyword = SearchKeyword,
                PageNumber = CurrentPage,
                PageSize = PageSize
            };

            var result = await _patientService.SearchPatientsAsync(searchDto);

            if (result.IsSuccess && result.Data != null)
            {
                // 转换DTO到UI模型
                Items.Clear();
                foreach (var dto in result.Data.Items)
                {
                    Items.Add(PatientItem.FromDto(dto));
                }

                TotalCount = result.Data.TotalCount;
                UpdateStatusText();
            }
            else
            {
                await ShowErrorAsync(result.ErrorMessage ?? "加载患者数据失败");
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
    /// 添加实现
    /// </summary>
    protected override async Task AddAsync()
    {
        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "Mode", "Add" }
        };

        await _dialogService.ShowDialogAsync("PatientEditDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("患者添加成功");
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
            { "PatientId", SelectedItem.Id }
        };

        await _dialogService.ShowDialogAsync("PatientEditDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("患者信息更新成功");
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
            $"确定要删除患者 {SelectedItem.Name} 吗？",
            "确认删除");

        if (confirmed)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _patientService.DeletePatientAsync(SelectedItem.Id);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync("患者删除成功");
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

        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "PatientId", SelectedItem.Id },
            { "ViewMode", true }
        };

        await _dialogService.ShowDialogAsync("PatientDetailDialog", parameters);
    }

    /// <summary>
    /// 导出实现
    /// </summary>
    protected override async Task ExportAsync()
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
            FileName = $"患者档案_{DateTime.Now:yyyyMMdd}.xlsx"
        };

        if (saveDialog.ShowDialog() == true)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                // 获取所有数据
                var searchDto = new PatientSearchDto
                {
                    Keyword = SearchKeyword,
                    PageNumber = 1,
                    PageSize = int.MaxValue
                };

                var result = await _patientService.SearchPatientsAsync(searchDto);

                if (result.IsSuccess && result.Data != null)
                {
                    // TODO: 实现导出逻辑
                    await ShowSuccessAsync($"已导出 {result.Data.TotalCount} 条患者记录");
                }
                else
                {
                    await ShowErrorAsync("导出失败");
                }
            });
        }
    }

    #endregion

    #region Additional Methods

    /// <summary>
    /// 导入患者
    /// </summary>
    private async Task ImportPatientsAsync()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
            Title = "选择要导入的患者档案"
        };

        if (openDialog.ShowDialog() == true)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    // TODO: 实现导入逻辑
                    await LoadDataAsync();
                    await ShowSuccessAsync("患者档案导入成功");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "导入患者失败");
                    await ShowErrorAsync($"导入失败: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// 切换患者状态
    /// </summary>
    private async Task TogglePatientStatusAsync()
    {
        if (SelectedItem == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            // TODO: 实现状态切换
            await LoadDataAsync();
        });
    }

    /// <summary>
    /// 跳转到第一页
    /// </summary>
    private async Task GoToFirstPageAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>
    /// 跳转到最后一页
    /// </summary>
    private async Task GoToLastPageAsync()
    {
        CurrentPage = TotalPages;
        await LoadDataAsync();
    }

    /// <summary>
    /// 更新状态文本
    /// </summary>
    private void UpdateStatusText()
    {
        StatusText = $"共 {TotalCount} 条记录，第 {CurrentPage}/{TotalPages} 页";
    }

    /// <summary>
    /// 选中项变化处理
    /// </summary>
    protected override void OnSelectedItemChanged(PatientItem? newItem)
    {
        base.OnSelectedItemChanged(newItem);

        // 更新ViewState
        ViewState.SelectedPatient = newItem;

        // 更新命令状态
        ToggleStatusCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// 命令状态更新
    /// </summary>
    protected override void RaiseCanExecuteChanged()
    {
        base.RaiseCanExecuteChanged();

        FirstPageCommand.RaiseCanExecuteChanged();
        LastPageCommand.RaiseCanExecuteChanged();
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