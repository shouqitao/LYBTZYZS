using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Models.ViewModels
{
    /// <summary>
    /// 列表ViewModel基类 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则，提供基本列表管理功能
    /// </summary>
    public abstract class ListViewModelBase<T> : ViewModelBase where T : class
    {
        private ObservableCollection<T> _items = new();
        private T? _selectedItem;
        private string _searchText = string.Empty;

        protected ListViewModelBase(ILogger logger, IEventAggregator? eventAggregator = null) 
            : base(logger, eventAggregator)
        {
            InitializeCommands();
        }

        /// <summary>
        /// 数据项集合
        /// </summary>
        public ObservableCollection<T> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// 选中的项
        /// </summary>
        public T? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnSelectedItemChanged(value);
                    RefreshCommands();
                }
            }
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = SearchAsync();
                }
            }
        }

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelection => SelectedItem != null;

        #region 命令

        public DelegateCommand LoadCommand { get; private set; } = null!;
        public DelegateCommand RefreshCommand { get; private set; } = null!;
        public DelegateCommand AddCommand { get; private set; } = null!;
        public DelegateCommand EditCommand { get; private set; } = null!;
        public DelegateCommand DeleteCommand { get; private set; } = null!;
        public DelegateCommand ClearSearchCommand { get; private set; } = null!;

        #endregion

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            LoadCommand = new DelegateCommand(async () => await LoadDataAsync(), CanLoad);
            RefreshCommand = new DelegateCommand(async () => await RefreshDataAsync(), CanRefresh);
            AddCommand = new DelegateCommand(async () => await AddItemAsync(), CanAdd);
            EditCommand = new DelegateCommand(async () => await EditItemAsync(), CanEdit);
            DeleteCommand = new DelegateCommand(async () => await DeleteItemAsync(), CanDelete);
            ClearSearchCommand = new DelegateCommand(ClearSearch, CanClearSearch);
        }

        /// <summary>
        /// 刷新所有命令的可执行状态
        /// </summary>
        private void RefreshCommands()
        {
            LoadCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
            AddCommand.RaiseCanExecuteChanged();
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            ClearSearchCommand.RaiseCanExecuteChanged();
        }

        #region 抽象方法 - 子类必须实现

        /// <summary>
        /// 加载数据
        /// </summary>
        protected abstract Task LoadDataAsync();

        /// <summary>
        /// 搜索数据
        /// </summary>
        protected abstract Task SearchAsync();

        /// <summary>
        /// 添加新项
        /// </summary>
        protected abstract Task AddItemAsync();

        /// <summary>
        /// 编辑选中项
        /// </summary>
        protected abstract Task EditItemAsync();

        /// <summary>
        /// 删除选中项
        /// </summary>
        protected abstract Task DeleteItemAsync();

        #endregion

        #region 虚方法 - 子类可重写

        /// <summary>
        /// 选中项变化时触发
        /// </summary>
        protected virtual void OnSelectedItemChanged(T? selectedItem)
        {
            // 子类可重写
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        protected virtual async Task RefreshDataAsync()
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// 清除搜索
        /// </summary>
        protected virtual void ClearSearch()
        {
            SearchText = string.Empty;
        }

        #endregion

        #region 命令可执行条件

        protected virtual bool CanLoad() => !IsBusy;
        protected virtual bool CanRefresh() => !IsBusy;
        protected virtual bool CanAdd() => !IsBusy;
        protected virtual bool CanEdit() => !IsBusy && HasSelection;
        protected virtual bool CanDelete() => !IsBusy && HasSelection;
        protected virtual bool CanClearSearch() => !string.IsNullOrWhiteSpace(SearchText);

        #endregion

        #region 辅助方法

        /// <summary>
        /// 清空列表
        /// </summary>
        protected void ClearItems()
        {
            Items.Clear();
            SelectedItem = null;
        }

        /// <summary>
        /// 添加项到列表
        /// </summary>
        protected void AddItem(T item)
        {
            Items.Add(item);
        }

        /// <summary>
        /// 从列表移除项
        /// </summary>
        protected void RemoveItem(T item)
        {
            Items.Remove(item);
            if (SelectedItem == item)
            {
                SelectedItem = null;
            }
        }

        /// <summary>
        /// 选择指定项
        /// </summary>
        protected void SelectItem(T item)
        {
            SelectedItem = item;
        }

        #endregion
    }
}