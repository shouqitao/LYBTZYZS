using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services.Navigation;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.Herbs.Models;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Herbs.ViewModels;

/// <summary>
/// 中药材管理视图模型 - 基于ModernManagementViewModel
/// 使用HerbItem作为UI模型，替代直接使用HerbDto
/// 保持原有XAML绑定兼容性，确保功能不变
/// </summary>
public class HerbManagementViewModel2 : ModernManagementViewModel<HerbItem>
{
    #region Fields

    private readonly IHerbService _herbService;
    private readonly ICustomDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IMapper _mapper;
    private readonly ILogger<HerbManagementViewModel2> _logger;

    private string _categoryFilter = "All";
    private string _stockFilter = "All";

    #endregion

    #region Properties

    /// <summary>
    /// 选中的药材 - 兼容原有绑定
    /// </summary>
    public HerbItem? SelectedHerb
    {
        get => SelectedItem;
        set => SelectedItem = value;
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
    /// 库存筛选
    /// </summary>
    public string StockFilter
    {
        get => _stockFilter;
        set
        {
            if (SetProperty(ref _stockFilter, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    /// <summary>
    /// 切换状态命令
    /// </summary>
    public DelegateCommand ToggleStatusCommand { get; }

    /// <summary>
    /// 调整库存命令
    /// </summary>
    public DelegateCommand AdjustStockCommand { get; }

    /// <summary>
    /// 导入药材命令
    /// </summary>
    public DelegateCommand ImportHerbsCommand { get; }

    /// <summary>
    /// 导出药材命令
    /// </summary>
    public DelegateCommand ExportHerbsCommand { get; }

    #endregion

    #region Constructor

    public HerbManagementViewModel2(
        IHerbService herbService,
        ICustomDialogService dialogService,
        INavigationService navigationService,
        IEventAggregator eventAggregator,
        IMapper mapper,
        ILogger<HerbManagementViewModel2> logger)
        : base(eventAggregator, dialogService)
    {
        _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 初始化额外命令
        ToggleStatusCommand = new DelegateCommand(
            async () => await ToggleStatusAsync(),
            () => CanToggleStatus());

        AdjustStockCommand = new DelegateCommand(
            async () => await AdjustStockAsync(),
            () => CanAdjustStock());

        ImportHerbsCommand = new DelegateCommand(
            async () => await ImportHerbsAsync(),
            () => !IsLoading);

        ExportHerbsCommand = new DelegateCommand(
            async () => await ExportHerbsAsync(),
            () => !IsLoading && Items.Count > 0);
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
            var searchDto = new HerbSearchDto
            {
                Keyword = SearchKeyword,
                Category = CategoryFilter == "All" ? null : CategoryFilter,
                PageNumber = CurrentPage,
                PageSize = PageSize
            };

            var result = await _herbService.GetPagedAsync(searchDto);

            if (result.IsSuccess && result.Data != null)
            {
                // 转换DTO到UI模型
                Items.Clear();
                foreach (var dto in result.Data.Items)
                {
                    var item = HerbItem.FromDto(dto);
                    
                    // 应用库存筛选
                    if (StockFilter != "All")
                    {
                        var shouldInclude = StockFilter switch
                        {
                            "InStock" => item.HasStock,
                            "LowStock" => item.Stock > 0 && item.Stock < 50,
                            "OutOfStock" => item.Stock <= 0,
                            _ => true
                        };

                        if (!shouldInclude) continue;
                    }

                    Items.Add(item);
                }

                TotalCount = result.Data.TotalCount;
            }
            else
            {
                await ShowErrorAsync(result.ErrorMessage ?? "加载药材数据失败");
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
    /// 添加实现 - 创建新药材
    /// </summary>
    protected override async Task AddAsync()
    {
        var parameters = new Prism.Services.Dialogs.DialogParameters
        {
            { "Mode", "Create" }
        };

        await _dialogService.ShowDialogAsync("HerbCreateDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("药材创建成功");
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
            { "HerbId", SelectedItem.Id }
        };

        await _dialogService.ShowDialogAsync("HerbEditDialog", parameters, async result =>
        {
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                await LoadDataAsync();
                await ShowSuccessAsync("药材更新成功");
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
            $"确定要删除药材 {SelectedItem.Name} 吗？\n此操作不可恢复。",
            "确认删除");

        if (confirmed)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _herbService.DeleteAsync(SelectedItem.Id);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync("药材删除成功");
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
            { "HerbId", SelectedItem.Id }
        };

        await _navigationService.NavigateToAsync(
            RegionNames.MedicineWorkbenchContentRegion,
            "HerbDetailView",
            parameters);
    }

    #endregion

    #region Additional Methods

    /// <summary>
    /// 切换状态
    /// </summary>
    private async Task ToggleStatusAsync()
    {
        if (SelectedItem == null) return;

        var action = SelectedItem.IsActive ? "禁用" : "启用";
        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"确定要{action}药材 {SelectedItem.Name} 吗？",
            $"确认{action}");

        if (confirmed)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var updateDto = new HerbUpdateDto
                {
                    Id = SelectedItem.Id,
                    IsActive = !SelectedItem.IsActive
                };

                var result = await _herbService.UpdateAsync(SelectedItem.Id, updateDto);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync($"药材{action}成功");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? $"{action}失败");
                }
            });
        }
    }

    /// <summary>
    /// 调整库存
    /// </summary>
    private async Task AdjustStockAsync()
    {
        if (SelectedItem == null) return;

        var stockStr = await _dialogService.ShowInputAsync(
            $"当前库存: {SelectedItem.Stock}\n请输入新的库存数量:",
            "调整库存",
            SelectedItem.Stock.ToString());

        if (!string.IsNullOrEmpty(stockStr) && int.TryParse(stockStr, out var newStock))
        {
            if (newStock < 0)
            {
                await ShowErrorAsync("库存数量不能为负数");
                return;
            }

            await ExecuteWithLoadingAsync(async () =>
            {
                var updateDto = new HerbUpdateDto
                {
                    Id = SelectedItem.Id,
                    Stock = newStock
                };

                var result = await _herbService.UpdateAsync(SelectedItem.Id, updateDto);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync("库存调整成功");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? "库存调整失败");
                }
            });
        }
    }

    /// <summary>
    /// 导入药材
    /// </summary>
    private async Task ImportHerbsAsync()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv",
            Title = "选择药材数据文件"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _herbService.ImportAsync(openFileDialog.FileName);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    await ShowSuccessAsync($"成功导入 {result.Data} 条药材数据");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? "导入失败");
                }
            });
        }
    }

    /// <summary>
    /// 导出药材
    /// </summary>
    private async Task ExportHerbsAsync()
    {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv",
            Title = "保存药材数据",
            FileName = $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _herbService.ExportAsync(saveFileDialog.FileName);

                if (result.IsSuccess)
                {
                    await ShowSuccessAsync($"成功导出 {result.Data} 条药材数据");
                }
                else
                {
                    await ShowErrorAsync(result.ErrorMessage ?? "导出失败");
                }
            });
        }
    }

    /// <summary>
    /// 是否可以切换状态
    /// </summary>
    private bool CanToggleStatus()
    {
        return SelectedItem != null;
    }

    /// <summary>
    /// 是否可以调整库存
    /// </summary>
    private bool CanAdjustStock()
    {
        return SelectedItem != null && SelectedItem.IsActive;
    }

    /// <summary>
    /// 选中项变化处理
    /// </summary>
    protected override void OnSelectedItemChanged(HerbItem? newItem)
    {
        base.OnSelectedItemChanged(newItem);

        // 更新命令状态
        ToggleStatusCommand.RaiseCanExecuteChanged();
        AdjustStockCommand.RaiseCanExecuteChanged();
        ExportHerbsCommand.RaiseCanExecuteChanged();
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