using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LYBT.Desktop.{Module}.ViewModels;

/// <summary>
/// {Entity} 列表管理 ViewModel
/// 职责：列表查询、分页、搜索、CRUD操作导航
/// </summary>
public class {Entity}ManagementViewModel : UnifiedListViewModelBase<{Entity}Dto>
{
    #region Fields

    private readonly I{Entity}Service _{entity}Service;
    private readonly IRegionManager _navigationService;

    #endregion

    #region Constructor

    /// <summary>
    /// 构造函数
    /// 依赖注入顺序：业务服务 → 基类依赖 → 可选依赖
    /// </summary>
    public {Entity}ManagementViewModel(
        I{Entity}Service {entity}Service,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _{entity}Service = {entity}Service ?? throw new ArgumentNullException(nameof({entity}Service));
        _navigationService = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        // 设置页面标题
        PageTitle = "{Entity} 管理";

        // 初始化命令
        AddCommand = new DelegateCommand(OnAdd);
        EditCommand = new DelegateCommand<{Entity}Dto>(OnEdit, CanEdit);
        DeleteCommand = new DelegateCommand<{Entity}Dto>(OnDelete, CanDelete);
    }

    #endregion

    #region Commands

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    #endregion

    #region Abstract Method Implementation

    /// <summary>
    /// 获取分页数据
    /// 基类会自动调用此方法，并处理 IsLoading 状态
    /// </summary>
    protected override async Task<IEnumerable<{Entity}Dto>> GetItemsAsync(int page, int pageSize, string? searchText)
    {
        var result = await _{entity}Service.GetPagedAsync(page, pageSize, searchText);

        if (result.IsSuccess && result.Data != null)
        {
            var pagedData = result.Data;
            TotalCount = pagedData.TotalCount;
            return pagedData.Items ?? Enumerable.Empty<{Entity}Dto>();
        }

        await ShowErrorMessageAsync(result.Message ?? "获取 {Entity} 列表失败");
        return Enumerable.Empty<{Entity}Dto>();
    }

    #endregion

    #region Command Handlers

    private void OnAdd()
    {
        // 导航到创建页面
        _navigationService.RequestNavigate("MainRegion", "{Entity}CreateView");
    }

    private bool CanEdit({Entity}Dto? item) => item != null;

    private void OnEdit({Entity}Dto? item)
    {
        if (item == null) return;

        var parameters = new NavigationParameters
        {
            { "id", item.Id }
        };
        _navigationService.RequestNavigate("MainRegion", "{Entity}DetailView", parameters);
    }

    private bool CanDelete({Entity}Dto? item) => item != null;

    private async void OnDelete({Entity}Dto? item)
    {
        if (item == null) return;

        var confirmed = await ShowConfirmationAsync($"确定要删除 {Entity} \"{item.Name}\" 吗？");
        if (!confirmed) return;

        try
        {
            IsLoading = true;
            var result = await _{entity}Service.DeleteAsync(item.Id);

            if (result.IsSuccess)
            {
                await ShowSuccessMessageAsync("删除成功");
                await RefreshAsync(); // 刷新列表
            }
            else
            {
                await ShowErrorMessageAsync(result.Message ?? "删除失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除 {Entity} 时发生异常");
            await ShowErrorMessageAsync("删除失败，请重试");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Navigation

    /// <summary>
    /// 导航进入时触发
    /// </summary>
    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        // 加载初始数据
        _ = LoadDataAsync();
    }

    #endregion
}
