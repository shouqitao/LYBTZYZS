using System.Collections.ObjectModel;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Core.ViewModels.Dialogs
{

    /// <summary>
    /// 中药材选择对话框ViewModel - UltraThink统一架构
    /// </summary>
    public class HerbSelectionDialogViewModel : DialogViewModelBase
    {
        private readonly IHerbService _herbService;
        private string _searchKeyword = string.Empty;
        private HerbDto? _selectedHerb;
        private decimal _quantity = 10;
        private string _unit = "g";

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchCommand?.Execute();
                }
            }
        }

        /// <summary>
        /// 选中的中药材
        /// </summary>
        public HerbDto? SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                if (SetProperty(ref _selectedHerb, value))
                {
                    if (_selectedHerb != null)
                    {
                        Unit = _selectedHerb.Unit ?? "g";
                    }

                    ConfirmCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 用量
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        /// <summary>
        /// 中药材列表
        /// </summary>
        public ObservableCollection<HerbDto> Herbs { get; } = new();

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; } = null!;

        /// <summary>
        /// 排序命令 - UltraThink Command绑定优化
        /// </summary>
        public DelegateCommand<string> SortCommand { get; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="HerbSelectionDialogViewModel"/> class.
        /// 构造函数
        /// </summary>
        public HerbSelectionDialogViewModel(
            IHerbService herbService,
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            Title = "选择中药材";

            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            SortCommand = new DelegateCommand<string>(ExecuteSort);

            // 初始化加载中药材列表
            _ = LoadHerbsAsync();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HerbSelectionDialogViewModel"/> class.
        /// 简化构造函数（使用ContainerLocator）
        /// </summary>
        public HerbSelectionDialogViewModel(IHerbService herbService)
            : base()
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            Title = "选择中药材";

            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            SortCommand = new DelegateCommand<string>(ExecuteSort);

            // 初始化加载中药材列表
            _ = LoadHerbsAsync();
        }

        /// <summary>
        /// 加载中药材列表
        /// </summary>
        private async Task LoadHerbsAsync()
        {
            try
            {
                IsLoading = true;

                var query = new HerbPagedQueryDto { PageIndex = 1, PageSize = 1000 };
                var result = await _herbService.GetPagedAsync(query);
                if (result.IsSuccess && result.Data?.Items != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Herbs.Clear();
                        foreach (var herbDto in result.Data.Items)
                        {
                            Herbs.Add(herbDto);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("加载中药材列表", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行搜索
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            try
            {
                IsLoading = true;

                var query = new HerbPagedQueryDto { PageIndex = 1, PageSize = 1000 };
                var result = await _herbService.GetPagedAsync(query);
                if (result.IsSuccess && result.Data?.Items != null)
                {
                    var filteredHerbs = string.IsNullOrWhiteSpace(SearchKeyword)
                        ? result.Data.Items
                        : result.Data.Items.Where(h => h.Name.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase));

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Herbs.Clear();
                        foreach (var herbDto in filteredHerbs)
                        {
                            Herbs.Add(herbDto);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("搜索中药材", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行排序 - UltraThink Command绑定优化
        /// </summary>
        /// <param name="columnName">列名</param>
        private void ExecuteSort(string? columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return;
            }

            try
            {
                var sortedHerbs = columnName.ToLower() switch
                {
                    "名称" => Herbs.OrderBy(h => h.Name),
                    "规格" => Herbs.OrderBy(h => h.Spec),
                    "单位" => Herbs.OrderBy(h => h.Unit),
                    "单价" => Herbs.OrderBy(h => h.Price),
                    _ => Herbs.OrderBy(h => h.Name)
                };

                // 更新集合
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Herbs.Clear();
                    foreach (var herb in sortedHerbs)
                    {
                        Herbs.Add(herb);
                    }
                });
            }
            catch (Exception)
            {
                // 排序失败时记录错误但不中断用户操作
                StatusMessage = "排序失败，请重试";
            }
        }

        /// <summary>
        /// 执行确认逻辑
        /// </summary>
        protected override Task<bool> ExecuteConfirmAsync()
        {
            return Task.FromResult(SelectedHerb != null && Quantity > 0);
        }

        /// <summary>
        /// 检查是否可以确认
        /// </summary>
        protected override bool CanConfirm()
        {
            return !IsLoading && SelectedHerb != null && Quantity > 0;
        }

        /// <summary>
        /// 获取选择结果
        /// </summary>
        public (HerbDto? Herb, decimal Quantity, string Unit) GetResult()
        {
            return (SelectedHerb, Quantity, Unit);
        }
    }
}
