using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base;

/// <summary>
/// 基础数据管理视图模型基类 - Phase 2统一架构
/// 封装分页、搜索、命令等通用逻辑，为Users/Patients/Herbs模块提供统一基础
/// Issue #1994: 创建泛型基类，实现500ms搜索防抖
/// </summary>
/// <typeparam name="TDto">数据传输对象类型（UserDto/PatientDto/HerbDto）</typeparam>
public abstract class BaseManagementViewModel<TDto> : UnifiedViewModelBase where TDto : class
{
    #region 字段

    private CancellationTokenSource? _searchCancellation;
    private const int SearchDebounceMilliseconds = 500; // 500ms防抖
    private bool _isInitializing = true; // Issue #2011: 跟踪初始化状态

    #endregion

    #region 分页属性

    private int _pageIndex = 1;
    private int _pageSize = 20;
    private int _totalCount;

    /// <summary>
    /// 当前页码（从1开始）
    /// </summary>
    public int PageIndex
    {
        get => _pageIndex;
        set
        {
            if (SetProperty(ref _pageIndex, value))
            {
                // Issue #2011: 防止构造期间触发数据加载，避免 StackOverflow
                if (!_isInitializing)
                {
                    _ = LoadDataAsync(); // 页码变化时重新加载数据
                }
                RaisePropertyChanged(nameof(TotalPages));
                RaisePropertyChanged(nameof(HasPreviousPage));
                RaisePropertyChanged(nameof(HasNextPage));
                RefreshCommands();
            }
        }
    }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                PageIndex = 1; // 重置到第一页
                RaisePropertyChanged(nameof(TotalPages));
            }
        }
    }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        protected set
        {
            if (SetProperty(ref _totalCount, value))
            {
                RaisePropertyChanged(nameof(TotalPages));
                RaisePropertyChanged(nameof(HasPreviousPage));
                RaisePropertyChanged(nameof(HasNextPage));
            }
        }
    }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;

    #endregion

    #region 搜索属性

    private string _searchText = string.Empty;

    /// <summary>
    /// 搜索文本（带500ms防抖）
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = TriggerSearchWithDebounceAsync(); // 触发防抖搜索
            }
        }
    }

    #endregion

    #region 忙碌状态

    private string _busyMessage = "正在加载...";

    /// <summary>
    /// 忙碌提示消息
    /// </summary>
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    #endregion

    #region 数据集合

    private ObservableCollection<TDto> _items = new();
    private TDto? _selectedItem;

    /// <summary>
    /// 数据项集合
    /// </summary>
    public ObservableCollection<TDto> Items
    {
        get => _items;
        protected set => SetProperty(ref _items, value);
    }

    /// <summary>
    /// 当前选中项
    /// </summary>
    public TDto? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                RefreshCommands();
            }
        }
    }

    #endregion

    #region 命令

    /// <summary>
    /// 刷新命令
    /// </summary>
    public DelegateCommand RefreshCommand { get; private set; } = null!;

    /// <summary>
    /// 上一页命令
    /// </summary>
    public DelegateCommand PreviousPageCommand { get; private set; } = null!;

    /// <summary>
    /// 下一页命令
    /// </summary>
    public DelegateCommand NextPageCommand { get; private set; } = null!;

    /// <summary>
    /// 删除命令
    /// </summary>
    public DelegateCommand<TDto> DeleteCommand { get; private set; } = null!;

    #endregion

    #region 构造函数

    protected BaseManagementViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        // Issue #2011: 移除重复的 InitializeManagementCommands() 调用
        // InitializeCommands() 已经在 base 构造函数中被调用，会自动调用 InitializeManagementCommands()
    }

    #endregion

    #region 命令初始化

    protected override void InitializeCommands()
    {
        base.InitializeCommands();
        InitializeManagementCommands();
    }

    private void InitializeManagementCommands()
    {
        RefreshCommand = new DelegateCommand(
            async () => await RefreshAsync(),
            () => !IsBusy && !IsLoading)
            .ObservesProperty(() => IsBusy)
            .ObservesProperty(() => IsLoading);

        PreviousPageCommand = new DelegateCommand(
            ExecutePreviousPage,
            () => HasPreviousPage && !IsBusy && !IsLoading)
            .ObservesProperty(() => PageIndex)
            .ObservesProperty(() => IsBusy)
            .ObservesProperty(() => IsLoading);

        NextPageCommand = new DelegateCommand(
            ExecuteNextPage,
            () => HasNextPage && !IsBusy && !IsLoading)
            .ObservesProperty(() => PageIndex)
            .ObservesProperty(() => IsBusy)
            .ObservesProperty(() => IsLoading);

        DeleteCommand = new DelegateCommand<TDto>(
            async item => await ExecuteDeleteAsync(item),
            CanExecuteDelete)
            .ObservesProperty(() => IsBusy)
            .ObservesProperty(() => IsLoading);
    }

    #endregion

    #region 抽象方法（子类必须实现）

    /// <summary>
    /// 加载数据（子类必须实现）
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="searchText">搜索关键词</param>
    /// <returns>分页结果</returns>
    protected abstract Task<PagedResult<TDto>> LoadDataAsync(int pageIndex, int pageSize, string? searchText);

    /// <summary>
    /// 删除数据项（子类必须实现）
    /// </summary>
    /// <param name="item">要删除的项</param>
    /// <returns>是否成功</returns>
    protected abstract Task<bool> DeleteItemAsync(TDto item);

    #endregion

    #region 数据加载

    /// <summary>
    /// 加载数据（内部实现）
    /// </summary>
    private async Task LoadDataAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            IsLoading = true;

            try
            {
                var result = await LoadDataAsync(PageIndex, PageSize, SearchText);

                RunOnUIThread(() =>
                {
                    Items.Clear();
                    if (result.Items != null)
                    {
                        foreach (var item in result.Items)
                        {
                            Items.Add(item);
                        }
                    }

                    TotalCount = result.TotalCount;
                });

                Logger.LogDebug("加载数据成功: 第{Page}页, 共{TotalCount}条记录", PageIndex, TotalCount);
            }
            finally
            {
                IsLoading = false;
            }

        }, "加载数据");
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    public async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    #endregion

    #region 搜索防抖

    /// <summary>
    /// 触发带防抖的搜索（500ms延迟）
    /// Issue #1994: 核心防抖逻辑实现
    /// </summary>
    private async Task TriggerSearchWithDebounceAsync()
    {
        // 取消之前的搜索任务
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();

        var currentCancellation = _searchCancellation;

        try
        {
            // 等待500ms
            await Task.Delay(SearchDebounceMilliseconds, currentCancellation.Token);

            // 如果没有被取消，执行搜索
            if (!currentCancellation.Token.IsCancellationRequested)
            {
                Logger.LogDebug("搜索防抖触发: {SearchText}", SearchText);
                PageIndex = 1; // 重置到第一页
                // PageIndex变化会自动触发 LoadDataAsync
            }
        }
        catch (TaskCanceledException)
        {
            Logger.LogDebug("搜索防抖已取消（输入仍在继续）");
        }
    }

    #endregion

    #region 命令执行

    /// <summary>
    /// 上一页
    /// </summary>
    private void ExecutePreviousPage()
    {
        if (HasPreviousPage)
        {
            PageIndex--;
        }
    }

    /// <summary>
    /// 下一页
    /// </summary>
    private void ExecuteNextPage()
    {
        if (HasNextPage)
        {
            PageIndex++;
        }
    }

    /// <summary>
    /// 执行删除
    /// </summary>
    private async Task ExecuteDeleteAsync(TDto item)
    {
        if (item == null) return;

        await ExecuteSafelyAsync(async () =>
        {
            var success = await DeleteItemAsync(item);
            if (success)
            {
                await RefreshAsync();
                Logger.LogInformation("删除成功");
            }
            else
            {
                Logger.LogWarning("删除失败");
            }
        }, "删除数据");
    }

    /// <summary>
    /// 是否可以删除
    /// </summary>
    private bool CanExecuteDelete(TDto item)
    {
        return item != null && !IsBusy && !IsLoading;
    }

    #endregion

    #region 命令刷新

    protected override void RefreshCommands()
    {
        base.RefreshCommands();
        RefreshManagementCommands();
    }

    private void RefreshManagementCommands()
    {
        RefreshCommand?.RaiseCanExecuteChanged();
        PreviousPageCommand?.RaiseCanExecuteChanged();
        NextPageCommand?.RaiseCanExecuteChanged();
        DeleteCommand?.RaiseCanExecuteChanged();
    }

    #endregion

    #region 生命周期

    /// <summary>
    /// 页面初始化时自动加载数据
    /// </summary>
    protected override async Task InitializeAsync(NavigationParameters parameters)
    {
        await base.InitializeAsync(parameters);

        // Issue #2011: 初始化完成，允许数据加载
        _isInitializing = false;

        await LoadDataAsync();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    protected override void OnDisposing()
    {
        base.OnDisposing();
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
    }

    #endregion
}
