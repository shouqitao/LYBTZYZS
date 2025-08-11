using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Desktop.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;

using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 药材选择对话框视图模型
    /// </summary>
    public class SelectHerbDialogViewModel : BindableBase
    {
        private readonly IHerbService _herbService;
        private readonly IDialogService _dialogService;
        
        private bool _isLoading;
        private string _searchKeyword = string.Empty;
        private HerbInfo? _selectedHerb;
        private ObservableCollection<HerbInfo> _herbs = new ObservableCollection<HerbInfo>();
        private ObservableCollection<HerbSelectionItem> _selectedItems = new ObservableCollection<HerbSelectionItem>();
        private decimal _defaultQuantity = 10;
        private string _quantityUnit = "g";
        
        #region 属性

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 搜索关键词（支持拼音码）
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // 实时搜索
                    SearchCommand.Execute();
                }
            }
        }

        /// <summary>
        /// 选中的药材
        /// </summary>
        public HerbInfo? SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                if (SetProperty(ref _selectedHerb, value))
                {
                    AddToSelectionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 药材列表
        /// </summary>
        public ObservableCollection<HerbInfo> Herbs
        {
            get => _herbs;
            set => SetProperty(ref _herbs, value);
        }

        /// <summary>
        /// 已选择的药材列表
        /// </summary>
        public ObservableCollection<HerbSelectionItem> SelectedItems
        {
            get => _selectedItems;
            set => SetProperty(ref _selectedItems, value);
        }

        /// <summary>
        /// 默认用量
        /// </summary>
        public decimal DefaultQuantity
        {
            get => _defaultQuantity;
            set => SetProperty(ref _defaultQuantity, value);
        }

        /// <summary>
        /// 用量单位
        /// </summary>
        public string QuantityUnit
        {
            get => _quantityUnit;
            set => SetProperty(ref _quantityUnit, value);
        }

        /// <summary>
        /// 是否显示空状态
        /// </summary>
        public bool ShowEmptyState => !IsLoading && !_herbs.Any();

        /// <summary>
        /// 空状态消息
        /// </summary>
        public string EmptyStateMessage
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                    return $"未找到包含 \"{SearchKeyword}\" 的药材";
                return "暂无药材数据";
            }
        }

        /// <summary>
        /// 选择总价
        /// </summary>
        public decimal TotalPrice => SelectedItems.Sum(i => i.Subtotal);

        /// <summary>
        /// 选择数量
        /// </summary>
        public int SelectionCount => SelectedItems.Count;

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddToSelectionCommand { get; }
        public DelegateCommand<HerbSelectionItem> RemoveFromSelectionCommand { get; }
        public DelegateCommand<HerbSelectionItem> UpdateQuantityCommand { get; }
        public DelegateCommand ClearSelectionCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand<HerbInfo> QuickAddCommand { get; }

        #endregion

        #region 回调

        public Action<List<HerbSelectionItem>>? OnHerbsSelected { get; set; }
        public Action? OnCancelled { get; set; }

        #endregion

        private List<HerbInfo> _allHerbs = new();

        public SelectHerbDialogViewModel(
            IHerbService herbService,
            IDialogService dialogService)
        {
            _herbService = herbService;
            _dialogService = dialogService;

            // 监听选择项变化
            _selectedItems.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(TotalPrice));
                RaisePropertyChanged(nameof(SelectionCount));
                ConfirmCommand?.RaiseCanExecuteChanged();
            };

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            RefreshCommand = new DelegateCommand(async () => await LoadHerbsAsync());
            AddToSelectionCommand = new DelegateCommand(ExecuteAddToSelection, CanAddToSelection);
            RemoveFromSelectionCommand = new DelegateCommand<HerbSelectionItem>(ExecuteRemoveFromSelection);
            UpdateQuantityCommand = new DelegateCommand<HerbSelectionItem>(ExecuteUpdateQuantity);
            ClearSelectionCommand = new DelegateCommand(ExecuteClearSelection, () => SelectedItems.Any());
            ConfirmCommand = new DelegateCommand(ExecuteConfirm, () => SelectedItems.Any());
            CancelCommand = new DelegateCommand(ExecuteCancel);
            QuickAddCommand = new DelegateCommand<HerbInfo>(ExecuteQuickAdd);

            // 初始加载
            Task.Run(async () => await LoadHerbsAsync());
        }

        private async Task LoadHerbsAsync()
        {
            try
            {
                IsLoading = true;

                // 加载药材列表
                var result = await _herbService.GetListAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    _allHerbs = result.Data.Select(dto => new HerbInfo
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        Category = string.Empty, // 默认值
                        Price = dto.Price,
                        Unit = dto.Unit,
                        Stock = 0, // 默认值
                        IsActive = true // 默认值
                    }).ToList();
                    Herbs = new ObservableCollection<HerbInfo>(_allHerbs);
                    RaisePropertyChanged(nameof(ShowEmptyState));
                    RaisePropertyChanged(nameof(EmptyStateMessage));
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "加载药材列表失败",
                        "加载失败");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(
                    $"加载药材列表时发生错误：{ex.Message}",
                    "系统错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task ExecuteSearchAsync()
        {
            try
            {
                IsLoading = true;
                
                if (string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    // 如果搜索词为空，显示所有药材
                    Herbs = new ObservableCollection<HerbInfo>(_allHerbs);
                }
                else
                {
                    // 搜索药材（支持名称和拼音码）
                    var keyword = SearchKeyword.ToLower();
                    var filteredHerbs = _allHerbs.Where(h =>
                        h.Name.ToLower().Contains(keyword) ||
                        (!string.IsNullOrWhiteSpace(h.PinYinCode) && h.PinYinCode.ToLower().Contains(keyword))
                    ).ToList();
                    
                    Herbs = new ObservableCollection<HerbInfo>(filteredHerbs);
                }
                
                RaisePropertyChanged(nameof(ShowEmptyState));
                RaisePropertyChanged(nameof(EmptyStateMessage));
            }
            finally
            {
                IsLoading = false;
            }
            
            return Task.CompletedTask;
        }

        private bool CanAddToSelection()
        {
            return SelectedHerb != null && 
                   !SelectedItems.Any(i => i.HerbId == SelectedHerb.Id);
        }

        private void ExecuteAddToSelection()
        {
            if (SelectedHerb == null) return;
            
            var selectionItem = new HerbSelectionItem
            {
                HerbId = SelectedHerb.Id,
                HerbName = SelectedHerb.Name,
                Quantity = DefaultQuantity,
                Unit = SelectedHerb.Unit ?? "g",
                UnitPrice = SelectedHerb.Price,
                Subtotal = DefaultQuantity * SelectedHerb.Price
            };
            
            // 监听数量变化
            selectionItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HerbSelectionItem.Quantity))
                {
                    selectionItem.Subtotal = selectionItem.Quantity * selectionItem.UnitPrice;
                    RaisePropertyChanged(nameof(TotalPrice));
                }
            };
            
            SelectedItems.Add(selectionItem);
            
            // 清除选择
            SelectedHerb = null;
            
            // 更新命令状态
            ClearSelectionCommand.RaiseCanExecuteChanged();
        }

        private void ExecuteQuickAdd(HerbInfo herb)
        {
            if (herb == null) return;
            
            // 检查是否已存在
            var existing = SelectedItems.FirstOrDefault(i => i.HerbId == herb.Id);
            if (existing != null)
            {
                // 增加数量
                existing.Quantity += DefaultQuantity;
                _dialogService.ShowInformationAsync(
                    $"药材\"{herb.Name}\"已存在，数量增加到{existing.Quantity}{existing.Unit}",
                    "提示");
            }
            else
            {
                // 添加新项
                var selectionItem = new HerbSelectionItem
                {
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = DefaultQuantity,
                    Unit = herb.Unit ?? "g",
                    UnitPrice = herb.Price,
                    Subtotal = DefaultQuantity * herb.Price
                };
                
                // 监听数量变化
                selectionItem.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(HerbSelectionItem.Quantity))
                    {
                        selectionItem.Subtotal = selectionItem.Quantity * selectionItem.UnitPrice;
                        RaisePropertyChanged(nameof(TotalPrice));
                    }
                };
                
                SelectedItems.Add(selectionItem);
            }
            
            ClearSelectionCommand.RaiseCanExecuteChanged();
        }

        private void ExecuteRemoveFromSelection(HerbSelectionItem item)
        {
            if (item == null) return;
            
            SelectedItems.Remove(item);
            ClearSelectionCommand.RaiseCanExecuteChanged();
        }

        private void ExecuteUpdateQuantity(HerbSelectionItem item)
        {
            if (item == null) return;
            
            // 这里可以打开数量编辑对话框
            // 暂时通过绑定直接编辑
        }

        private void ExecuteClearSelection()
        {
            var confirm = _dialogService.ShowConfirmationAsync(
                "确定要清空所有已选药材吗？",
                "清空确认").Result;
                
            if (confirm)
            {
                SelectedItems.Clear();
                ClearSelectionCommand.RaiseCanExecuteChanged();
            }
        }

        private void ExecuteConfirm()
        {
            if (SelectedItems.Any())
            {
                OnHerbsSelected?.Invoke(SelectedItems.ToList());
            }
        }

        private void ExecuteCancel()
        {
            OnCancelled?.Invoke();
        }
    }

    /// <summary>
    /// 药材选择项
    /// </summary>
    public class HerbSelectionItem : BindableBase
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        
        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }
        
        public string Unit { get; set; } = "g";
        public decimal UnitPrice { get; set; }
        
        private decimal _subtotal;
        public decimal Subtotal
        {
            get => _subtotal;
            set => SetProperty(ref _subtotal, value);
        }
        
        // 显示文本
        public string DisplayText => $"{HerbName} {Quantity}{Unit}";
        public string PriceText => $"￥{Subtotal:F2}";
    }
}