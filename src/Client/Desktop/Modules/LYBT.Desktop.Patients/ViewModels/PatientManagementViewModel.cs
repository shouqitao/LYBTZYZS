using Prism.Mvvm;
using Prism.Commands;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型 - 占位实现
    /// </summary>
    public class PatientManagementViewModel : BindableBase, INavigationAware
    {
        private readonly IRegionManager _regionManager;

        #region 属性

        private ObservableCollection<PatientItemPlaceholder> _items = new();
        public ObservableCollection<PatientItemPlaceholder> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private PatientItemPlaceholder? _selectedItem;
        public PatientItemPlaceholder? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private string _statusMessage = "共 0 条记录";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand<PatientItemPlaceholder> EditCommand { get; }
        public DelegateCommand<PatientItemPlaceholder> DeleteCommand { get; }
        public DelegateCommand<PatientItemPlaceholder> ViewDetailsCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        #endregion

        #region 构造函数

        public PatientManagementViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearch);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            AddCommand = new DelegateCommand(ExecuteAdd);
            EditCommand = new DelegateCommand<PatientItemPlaceholder>(ExecuteEdit);
            DeleteCommand = new DelegateCommand<PatientItemPlaceholder>(ExecuteDelete);
            ViewDetailsCommand = new DelegateCommand<PatientItemPlaceholder>(ExecuteViewDetails);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage);

            // 加载占位数据
            LoadPlaceholderData();
        }

        #endregion

        #region 命令实现

        private void ExecuteSearch()
        {
            StatusMessage = $"搜索: {SearchText}（功能开发中）";
        }

        private void ExecuteRefresh()
        {
            LoadPlaceholderData();
            StatusMessage = "已刷新";
        }

        private void ExecuteAdd()
        {
            StatusMessage = "添加患者（功能开发中）";
        }

        private void ExecuteEdit(PatientItemPlaceholder? patient)
        {
            if (patient != null)
                StatusMessage = $"编辑患者: {patient.Name}（功能开发中）";
        }

        private void ExecuteDelete(PatientItemPlaceholder? patient)
        {
            if (patient != null)
                StatusMessage = $"删除患者: {patient.Name}（功能开发中）";
        }

        private void ExecuteViewDetails(PatientItemPlaceholder? patient)
        {
            if (patient != null)
                StatusMessage = $"查看患者: {patient.Name}（功能开发中）";
        }

        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
            StatusMessage = "第 1 页";
        }

        private void ExecutePreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                StatusMessage = $"第 {CurrentPage} 页";
            }
        }

        private void ExecuteNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                StatusMessage = $"第 {CurrentPage} 页";
            }
        }

        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
            StatusMessage = $"第 {TotalPages} 页";
        }

        #endregion

        #region 数据加载

        private void LoadPlaceholderData()
        {
            Items.Clear();

            // 占位数据
            Items.Add(new PatientItemPlaceholder
            {
                Name = "张三",
                Gender = "男",
                Age = "45",
                PhoneNumber = "138****1234",
                IdNumber = "3101********1234",
                VisitCount = "5"
            });

            Items.Add(new PatientItemPlaceholder
            {
                Name = "李四",
                Gender = "女",
                Age = "32",
                PhoneNumber = "139****5678",
                IdNumber = "3102********5678",
                VisitCount = "3"
            });

            Items.Add(new PatientItemPlaceholder
            {
                Name = "王五",
                Gender = "男",
                Age = "58",
                PhoneNumber = "136****9012",
                IdNumber = "3103********9012",
                VisitCount = "12"
            });

            StatusMessage = $"共 {Items.Count} 条记录";
            TotalPages = 1;
            CurrentPage = 1;
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            LoadPlaceholderData();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        #endregion
    }

    /// <summary>
    /// 患者列表项占位模型
    /// </summary>
    public class PatientItemPlaceholder
    {
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string VisitCount { get; set; } = string.Empty;
    }
}
