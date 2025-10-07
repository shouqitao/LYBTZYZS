using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LYBT.Desktop.{Module}.ViewModels;

/// <summary>
/// {Entity} 详情查看/编辑 ViewModel
/// 职责：单项数据查看、编辑、保存
/// </summary>
public class {Entity}DetailViewModel : UnifiedViewModelBase
{
    #region Fields

    private readonly I{Entity}Service _{entity}Service;
    private readonly IRegionManager _navigationService;

    private {Entity}Dto? _current{Entity};
    private bool _isEditMode;

    #endregion

    #region Properties

    /// <summary>
    /// 当前 {Entity} 数据
    /// </summary>
    public {Entity}Dto? Current{Entity}
    {
        get => _current{Entity};
        set => SetProperty(ref _current{Entity}, value);
    }

    /// <summary>
    /// 是否处于编辑模式
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                RaisePropertyChanged(nameof(IsReadOnly));
            }
        }
    }

    /// <summary>
    /// 是否只读（非编辑模式）
    /// </summary>
    public bool IsReadOnly => !IsEditMode;

    #endregion

    #region Constructor

    /// <summary>
    /// 构造函数
    /// 依赖注入顺序：业务服务 → 基类依赖 → 可选依赖
    /// </summary>
    public {Entity}DetailViewModel(
        I{Entity}Service {entity}Service,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _{entity}Service = {entity}Service ?? throw new ArgumentNullException(nameof({entity}Service));
        _navigationService = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        // 设置页面标题
        PageTitle = "{Entity} 详情";

        // 初始化命令
        EditCommand = new DelegateCommand(OnEdit, CanEdit);
        SaveCommand = new DelegateCommand(OnSave, CanSave);
        CancelCommand = new DelegateCommand(OnCancel);
        BackCommand = new DelegateCommand(OnBack);
    }

    #endregion

    #region Commands

    public ICommand EditCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BackCommand { get; }

    #endregion

    #region Command Handlers

    private bool CanEdit() => !IsEditMode && Current{Entity} != null;

    private void OnEdit()
    {
        IsEditMode = true;
    }

    private bool CanSave() => IsEditMode && Current{Entity} != null;

    private async void OnSave()
    {
        if (Current{Entity} == null) return;

        try
        {
            IsLoading = true;

            var updateDto = new Update{Entity}Dto
            {
                // 映射字段
                // TODO: 根据实际 DTO 结构填充
            };

            var result = await _{entity}Service.UpdateAsync(Current{Entity}.Id, updateDto);

            if (result.IsSuccess)
            {
                await ShowSuccessMessageAsync("保存成功");
                IsEditMode = false;

                // 重新加载数据
                await LoadDataAsync(Current{Entity}.Id);
            }
            else
            {
                await ShowErrorMessageAsync(result.Message ?? "保存失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存 {Entity} 时发生异常");
            await ShowErrorMessageAsync("保存失败，请重试");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnCancel()
    {
        IsEditMode = false;

        // 重新加载原始数据
        if (Current{Entity} != null)
        {
            _ = LoadDataAsync(Current{Entity}.Id);
        }
    }

    private void OnBack()
    {
        _navigationService.RequestNavigate("MainRegion", "{Entity}ManagementView");
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// 加载 {Entity} 详情数据
    /// </summary>
    private async Task LoadDataAsync(Guid {entity}Id)
    {
        try
        {
            IsLoading = true;

            var result = await _{entity}Service.GetByIdAsync({entity}Id);

            if (result.IsSuccess && result.Data != null)
            {
                Current{Entity} = result.Data;
            }
            else
            {
                await ShowErrorMessageAsync(result.Message ?? "加载 {Entity} 详情失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载 {Entity} 详情时发生异常");
            await ShowErrorMessageAsync("加载失败，请重试");
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

        // 从导航参数获取 ID
        if (navigationContext.Parameters.TryGetValue<Guid>("id", out var id))
        {
            _ = LoadDataAsync(id);
        }
        else
        {
            Logger.LogWarning("导航到 {Entity}DetailView 时缺少 id 参数");
        }
    }

    #endregion
}
