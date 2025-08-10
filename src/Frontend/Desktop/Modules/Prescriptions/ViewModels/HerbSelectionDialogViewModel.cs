using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Core.Models.Common;
using Prism.Commands;
using Prism.Mvvm;
// using Prism.Dialogs; // Temporarily disabled due to Prism 9 compatibility
using Microsoft.Extensions.Logging;

namespace LYBT.WPF.Client.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 中药材选择对话框视图模型
    /// </summary>
    public class HerbSelectionDialogViewModel : BindableBase // Temporarily remove IDialogAware due to Prism 9 compatibility issues
    {
        private readonly IHerbService _herbService;
        private readonly ILogger<HerbSelectionDialogViewModel> _logger;

        #region Dialog Properties

        public string Title => IsEditMode ? "编辑药材用量" : "选择中药材";
        // public event Action<IDialogResult>? RequestClose; // Removed for Prism 9 compatibility

        #endregion

        #region Properties

        private ObservableCollection<HerbInfo> _herbs = new();
        public ObservableCollection<HerbInfo> Herbs
        {
            get => _herbs;
            set => SetProperty(ref _herbs, value);
        }

        private ObservableCollection<HerbInfo> _filteredHerbs = new();
        public ObservableCollection<HerbInfo> FilteredHerbs
        {
            get => _filteredHerbs;
            set => SetProperty(ref _filteredHerbs, value);
        }

        private HerbInfo? _selectedHerb;
        public HerbInfo? SelectedHerb
        {
            get => _selectedHerb;
            set => SetProperty(ref _selectedHerb, value);
        }

        private decimal _quantity = 10;
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterHerbs();
                }
            }
        }

        private string _selectedCategory = "全部";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    FilterHerbs();
                }
            }
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private PrescriptionItemInfo? _editingItem;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<string> Categories { get; } = new()
        {
            "全部", "解表药", "清热药", "泻下药", "灥湿药", 
            "温里药", "理气药", "消食药", "止血药", "活血化瘀药",
            "化痰止咳平喘药", "安神药", "平肝息风药", "开窍药",
            "补虚药", "收涩药", "涌吐药", "外用药", "其他"
        };

        #endregion

        #region Commands

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand RefreshCommand { get; }

        #endregion

        #region Constructor

        public HerbSelectionDialogViewModel(
            IHerbService herbService,
            ILogger<HerbSelectionDialogViewModel> logger)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化命令
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm)
                .ObservesProperty(() => SelectedHerb)
                .ObservesProperty(() => Quantity);
            CancelCommand = new DelegateCommand(Cancel);
            RefreshCommand = new DelegateCommand(async () => await LoadHerbsAsync());

            // 初始加载数据
            Task.Run(async () => await LoadHerbsAsync());
        }

        #endregion

        #region Dialog Methods (Temporarily disabled due to Prism 9 compatibility)

        // public bool CanCloseDialog() => !IsLoading;

        // public void OnDialogClosed()
        // {
        //     // 清理资源
        // }

        // public void OnDialogOpened(IDialogParameters parameters)
        // {
        //     if (parameters.ContainsKey("HerbItem"))
        //     {
        //         _editingItem = parameters.GetValue<PrescriptionItemInfo>("HerbItem");
        //         IsEditMode = true;
        //         Quantity = _editingItem.Quantity;
        //         // 在加载完成后选中对应的药材
        //     }
        // }

        #endregion

        #region Methods

        private async Task LoadHerbsAsync()
        {
            try
            {
                IsLoading = true;
                var herbs = await _herbService.GetHerbsAsync();
                if (herbs != null)
                {
                    Herbs = new ObservableCollection<HerbInfo>(herbs);
                    FilterHerbs();

                    // 如果是编辑模式，选中对应的药材
                    if (IsEditMode && _editingItem != null)
                    {
                        SelectedHerb = Herbs.FirstOrDefault(h => h.Id == _editingItem.HerbId);
                    }
                }
                else
                {
                    _logger.LogWarning("加载中药材失败: 未获取到数据");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载中药材时出错");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterHerbs()
        {
            var filtered = Herbs.AsEnumerable();

            // 按分类过滤
            if (SelectedCategory != "全部")
            {
                filtered = filtered.Where(h => h.Category == SelectedCategory);
            }

            // 按关键字过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(h =>
                    (h.Name?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            FilteredHerbs = new ObservableCollection<HerbInfo>(filtered);
        }

        private bool CanConfirm()
        {
            return (IsEditMode || SelectedHerb != null) && Quantity > 0;
        }

        private void Confirm()
        {
            // TODO: Implement dialog confirm logic when Prism dialog support is added
            // var parameters = new DialogParameters();

            if (IsEditMode && _editingItem != null)
            {
                // 编辑模式，更新数量
                _editingItem.Quantity = Quantity;
                // Amount 会自动计算，不需要手动设置
            }
            // else if (SelectedHerb != null)
            // {
            //     // 选择模式，返回选中的药材
            //     parameters.Add("SelectedHerb", SelectedHerb);
            //     parameters.Add("Quantity", Quantity);
            // }

            // RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        }

        private void Cancel()
        {
            // TODO: Implement dialog cancel logic when Prism dialog support is added
            // RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion
    }
}