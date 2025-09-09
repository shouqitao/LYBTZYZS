using System.Collections.ObjectModel;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Interfaces.Services;

// UltraThink v2.0: 直接使用HerbDto，移除Info模型引用
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Prism.Commands;

namespace LYBT.Desktop.Prescriptions.ViewModels
{

    /// <summary>
    /// 中药材选择对话框视图模型
    /// </summary>
    /// <summary>
    /// 中药材选择对话框ViewModel - UltraThink架构统一
    /// </summary>
    public class HerbSelectionDialogViewModel : DialogViewModelBase
    {
        private readonly IHerbService _herbService;
        private ObservableCollection<HerbDto> _availableHerbs = new();
        private HerbDto? _selectedHerb;
        private string _searchText = string.Empty;
        private decimal _quantity = 1;
        private string _unit = "g";

        /// <summary>
        /// 可选择的中药材列表
        /// </summary>
        public ObservableCollection<HerbDto> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        /// <summary>
        /// 选中的中药材
        /// </summary>
        public HerbDto? SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                SetProperty(ref _selectedHerb, value);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
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
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; } = null!;

        /// <summary>
        /// 选中的中药材信息（用于返回结果）
        /// </summary>
        public PrescriptionItemDto? Result { get; private set; }

        public HerbSelectionDialogViewModel(IHerbService herbService) : base()
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            Title = "选择中药材";

            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());

            // 初始化加载中药材列表
            _ = LoadHerbsAsync();
        }

        /// <summary>
        /// 使用参数初始化（支持编辑模式）
        /// </summary>
        public async Task InitializeWithParametersAsync(Dictionary<string, object>? parameters = null)
        {
            if (parameters == null)
            {
                return;
            }

            try
            {
                // 检查编辑模式
                if (parameters.ContainsKey("EditMode") && parameters["EditMode"] is bool editMode && editMode)
                {
                    Title = "编辑药材";
                }

                // 设置初始数量和单位
                if (parameters.ContainsKey("Quantity") && parameters["Quantity"] is decimal quantity)
                {
                    Quantity = quantity;
                }

                if (parameters.ContainsKey("Unit") && parameters["Unit"] is string unit)
                {
                    Unit = unit;
                }

                // 如果提供了HerbId，预选中对应的药材
                if (parameters.ContainsKey("HerbId") && parameters["HerbId"] is Guid herbId && herbId != Guid.Empty)
                {
                    // 等待药材列表加载完成
                    await LoadHerbsAsync();

                    // 查找并选中指定的药材
                    var targetHerb = AvailableHerbs.FirstOrDefault(h => h.Id == herbId);
                    if (targetHerb != null)
                    {
                        SelectedHerb = targetHerb;

                        // 使用选中药材的单位作为默认值（如果参数没有提供单位）
                        if (!parameters.ContainsKey("Unit") && !string.IsNullOrEmpty(targetHerb.Unit))
                        {
                            Unit = targetHerb.Unit;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("初始化编辑参数", ex);
            }
        }

        /// <summary>
        /// 加载中药材列表
        /// </summary>
        private async Task LoadHerbsAsync()
        {
            try
            {
                IsLoading = true;
                var result = await _herbService.GetPagedAsync(new HerbPagedQueryDto { PageSize = 100 });
                if (result.IsSuccess && result.Data != null)
                {
                    AvailableHerbs = new ObservableCollection<HerbDto>(result.Data.Items);
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
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadHerbsAsync();
                return;
            }

            try
            {
                IsLoading = true;
                var result = await _herbService.GetPagedAsync(new HerbPagedQueryDto
                {
                    Name = SearchText,
                    PageSize = 100
                });

                if (result.IsSuccess && result.Data != null)
                {
                    AvailableHerbs = new ObservableCollection<HerbDto>(result.Data.Items);
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
        /// 执行确认逻辑
        /// </summary>
        protected override Task<bool> ExecuteConfirmAsync()
        {
            if (SelectedHerb == null)
            {
                return Task.FromResult(false);
            }

            Result = new PrescriptionItemDto
            {
                HerbId = SelectedHerb.Id,
                HerbName = SelectedHerb.Name,
                Quantity = Quantity,
                UnitPrice = SelectedHerb.Price,

                // UltraThink v2.0: 使用正确的属性名
                Usage = SelectedHerb.Spec ?? string.Empty,
                Unit = SelectedHerb.Unit
            };

            return Task.FromResult(true);
        }

        /// <summary>
        /// 检查是否可以确认
        /// </summary>
        protected override bool CanConfirm()
        {
            return !IsLoading && SelectedHerb != null && Quantity > 0;
        }
    }
}
