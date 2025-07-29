using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.UI.PrismWpf.Models;

namespace LYBT.UI.PrismWpf.ViewModels.Herb
{
    /// <summary>
    /// 药材列表ViewModel
    /// </summary>
    public class HerbListViewModel : BindableBase
    {
        #region Fields
        private ObservableCollection<HerbInfo> _herbs = new();
        private HerbInfo? _selectedHerb;
        private string _searchText = string.Empty;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalCount = 0;
        #endregion

        #region Properties
        public ObservableCollection<HerbInfo> Herbs
        {
            get => _herbs;
            set => SetProperty(ref _herbs, value);
        }

        public HerbInfo? SelectedHerb
        {
            get => _selectedHerb;
            set => SetProperty(ref _selectedHerb, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }
        #endregion

        #region Commands
        public ICommand AddHerbCommand { get; private set; }
        public ICommand EditHerbCommand { get; private set; }
        public ICommand StockInCommand { get; private set; }
        public ICommand StockOutCommand { get; private set; }
        public ICommand StockAlertCommand { get; private set; }
        public ICommand BatchImportCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand PreviousPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        #endregion

        #region Constructor
        public HerbListViewModel()
        {
            InitializeCommands();
            LoadData();
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            AddHerbCommand = new DelegateCommand(OnAddHerb);
            EditHerbCommand = new DelegateCommand<HerbInfo>(OnEditHerb);
            StockInCommand = new DelegateCommand<HerbInfo>(OnStockIn);
            StockOutCommand = new DelegateCommand<HerbInfo>(OnStockOut);
            StockAlertCommand = new DelegateCommand(OnStockAlert);
            BatchImportCommand = new DelegateCommand(OnBatchImport);
            SearchCommand = new DelegateCommand(OnSearch);
            PreviousPageCommand = new DelegateCommand(OnPreviousPage, () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(OnNextPage, () => CurrentPage < TotalPages);
        }

        private async void LoadData()
        {
            // TODO: 调用API加载药材数据
            await Task.Delay(100);
        }

        private void OnAddHerb() { /* TODO */ }
        private void OnEditHerb(HerbInfo? herb) { /* TODO */ }
        private void OnStockIn(HerbInfo? herb) { /* TODO */ }
        private void OnStockOut(HerbInfo? herb) { /* TODO */ }
        private void OnStockAlert() { /* TODO */ }
        private void OnBatchImport() { /* TODO */ }
        private void OnSearch() { /* TODO */ }
        private void OnPreviousPage() { /* TODO */ }
        private void OnNextPage() { /* TODO */ }
        #endregion
    }
}