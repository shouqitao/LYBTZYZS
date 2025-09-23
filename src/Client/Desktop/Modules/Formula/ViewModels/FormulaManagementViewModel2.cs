using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services.Navigation;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.Formula.Models;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels;

/// <summary>
/// 验方管理视图模型 - 基于ModernManagementViewModel
/// 使用FormulaItem作为UI模型，替代直接使用FormulaDto
/// 保持原有XAML绑定兼容性，确保功能不变
/// </summary>
public class FormulaManagementViewModel2 : ModernManagementViewModel<FormulaItem>
{
    #region Fields

    private readonly IFormulaService _formulaService;
    private readonly ICustomDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IMapper _mapper;
    private readonly ILogger<FormulaManagementViewModel2> _logger;

    private string _typeFilter = "All";
    private string _categoryFilter = "All";
    private bool _showFavoritesOnly;

    #endregion

    #region Properties

    /// <summary>
    /// 选中的验方 - 兼容原有绑定
    /// </summary>
    public FormulaItem? SelectedFormula
    {
        get => SelectedItem;
        set => SelectedItem = value;
    }

    /// <summary>
    /// 类型筛选
    /// </summary>
    public string TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (SetProperty(ref _typeFilter, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    /// <summary>
    /// 类别筛选
    /// </summary>
    public string CategoryFilter
    {
        get => _categoryFilter;
        set
        {
            if (SetProperty(ref _categoryFilter, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    /// <summary>
    /// 仅显示收藏
    /// </summary>
    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set
        {
            if (SetProperty(ref _showFavoritesOnly, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    /// <summary>
    /// 应用验方命令
    /// </summary>
    public DelegateCommand ApplyFormulaCommand { get; }

    /// <summary>
    /// 切换收藏命令
    /// </summary>
    public DelegateCommand ToggleFavoriteCommand { get; }

    /// <summary>
    /// 复制验方命令
    /// </summary>
    public DelegateCommand CopyFormulaCommand { get; }

    /// <summary>
    /// 分享验方命令
    /// </summary>
    public DelegateCommand ShareFormulaCommand { get; }

    #endregion

    #region Constructor

    public FormulaManagementViewModel2(
        IFormulaService formulaService,
        ICustomDialogService dialogService,
        INavigationService navigationService,
        IEventAggregator eventAggregator,
        IMapper mapper,
        ILogger<FormulaManagementViewModel2> logger)
        : base(eventAggregator, dialogService)
    {
        _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化额外命令
        ApplyFormulaCommand = new DelegateCommand(
            async () => await ApplyFormulaAsync(),
            () => CanApplyFormula());

        ToggleFavoriteCommand = new DelegateCommand(
            async () => await ToggleFavoriteAsync(),
            () => CanToggleFavorite());

        CopyFormulaCommand = new DelegateCommand(
            async () => await CopyFormulaAsync(),
            () => CanCopyFormula());

        ShareFormulaCommand = new DelegateCommand(
            async () => await ShareFormulaAsync(),
            () => CanShareFormula());
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
            var searchDto = new FormulaSearchDto
            {
                Keyword = SearchKeyword,
                Category = CategoryFilter == "All" ? null : CategoryFilter,
                IsClassic = TypeFilter == "Classic" ? true : null,
                IsPersonal = TypeFilter == "Personal" ? true : null,
                PageNumber = CurrentPage,
                PageSize = PageSize
            };

            var result = await _formulaService.GetPagedAsync(searchDto);

            if (result.IsSuccess && result.Data != null)
            {
                // 转换DTO到UI模型
                Items.Clear();
                foreach (var dto in result.Data.Items)
                {
                    var item = FormulaItem.FromDto(dto);
                    
                    // 应用收藏筛选
                    if (ShowFavoritesOnly && !item.IsFavorite)
                        continue;

                    Items.Add(item);
                }

                TotalCount = result.Data.TotalCount;
            }
            else
            {
                await ShowErrorAsync(result.ErrorMessage ?? "加载验方数据失败");
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
    /// 添加实现 - 创建新验方
    /// </summary>
    protected override async Task AddAsync()
    {
        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "Mode", "Create" }
        };

        await _dialogService.ShowDialogAsync("FormulaCreateDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("验方创建成功");
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
            { "FormulaId", SelectedItem.Id }
        };

        await _dialogService.ShowDialogAsync("FormulaEditDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("验方更新成功");
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
            $"确定要删除验方 {SelectedItem.Name} 吗？\n此操作不可恢复。",
            "确认删除");

        if (confirmed)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _formulaService.DeleteAsync(SelectedItem.Id);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync("验方删除成功");
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

        // 使用NavigationService导航到详情页
        var parameters = new NavigationParameters
        {
            { "FormulaId", SelectedItem.Id }
        };

        await _navigationService.NavigateToAsync(
            RegionNames.MedicineWorkbenchContentRegion,
            "FormulaDetailView",
            parameters);
    }

    #endregion

    #region Additional Methods

    /// <summary>
    /// 应用验方（创建处方）
    /// </summary>
    private async Task ApplyFormulaAsync()
    {
        if (SelectedItem == null) return;

        var parameters = new NavigationParameters
        {
            { "FormulaId", SelectedItem.Id },
            { "FormulaName", SelectedItem.Name },
            { "Herbs", SelectedItem.Herbs }
        };

        await _navigationService.NavigateToAsync(
            RegionNames.PrescriptionWorkbenchContentRegion,
            "PrescriptionCreateView",
            parameters);

        await ShowSuccessAsync($"已应用验方 {SelectedItem.Name} 到新处方");
    }

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    private async Task ToggleFavoriteAsync()
    {
        if (SelectedItem == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            SelectedItem.IsFavorite = !SelectedItem.IsFavorite;
            
            // TODO: 调用后端API保存收藏状态
            // var result = await _formulaService.ToggleFavoriteAsync(SelectedItem.Id);

            var action = SelectedItem.IsFavorite ? "收藏" : "取消收藏";
            await ShowSuccessAsync($"已{action}验方 {SelectedItem.Name}");
        });
    }

    /// <summary>
    /// 复制验方
    /// </summary>
    private async Task CopyFormulaAsync()
    {
        if (SelectedItem == null) return;

        var newName = await _dialogService.ShowInputAsync(
            $"请输入新验方名称:",
            "复制验方",
            $"{SelectedItem.Name}_副本");

        if (!string.IsNullOrEmpty(newName))
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var createDto = new FormulaCreateDto
                {
                    Name = newName,
                    Category = SelectedItem.Category,
                    Source = SelectedItem.Source,
                    Composition = SelectedItem.Composition,
                    Effect = SelectedItem.Effect,
                    Indication = SelectedItem.Indication,
                    Usage = SelectedItem.Usage,
                    IsPersonal = true,
                    IsClassic = false,
                    Herbs = SelectedItem.Herbs.Select(h => h.ToDto()).ToList()
                };

                var result = await _formulaService.CreateAsync(createDto);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync($"验方 {newName} 复制成功");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? "复制失败");
                }
            });
        }
    }

    /// <summary>
    /// 分享验方
    /// </summary>
    private async Task ShareFormulaAsync()
    {
        if (SelectedItem == null) return;

        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "Formula", SelectedItem }
        };

        await _dialogService.ShowDialogAsync("FormulaShareDialog", parameters, result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                ShowSuccessAsync($"验方 {SelectedItem.Name} 分享成功").Wait();
            }
        });
    }

    /// <summary>
    /// 是否可以应用验方
    /// </summary>
    private bool CanApplyFormula()
    {
        return SelectedItem != null && SelectedItem.IsAvailable;
    }

    /// <summary>
    /// 是否可以切换收藏
    /// </summary>
    private bool CanToggleFavorite()
    {
        return SelectedItem != null;
    }

    /// <summary>
    /// 是否可以复制验方
    /// </summary>
    private bool CanCopyFormula()
    {
        return SelectedItem != null;
    }

    /// <summary>
    /// 是否可以分享验方
    /// </summary>
    private bool CanShareFormula()
    {
        return SelectedItem != null && !SelectedItem.IsPersonal;
    }

    /// <summary>
    /// 选中项变化处理
    /// </summary>
    protected override void OnSelectedItemChanged(FormulaItem? newItem)
    {
        base.OnSelectedItemChanged(newItem);

        // 更新命令状态
        ApplyFormulaCommand.RaiseCanExecuteChanged();
        ToggleFavoriteCommand.RaiseCanExecuteChanged();
        CopyFormulaCommand.RaiseCanExecuteChanged();
        ShareFormulaCommand.RaiseCanExecuteChanged();
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