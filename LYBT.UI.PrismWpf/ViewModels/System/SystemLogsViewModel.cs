using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.UI.PrismWpf.Models;

namespace LYBT.UI.PrismWpf.ViewModels.System
{
    /// <summary>
    /// 系统日志ViewModel
    /// </summary>
    public class SystemLogsViewModel : BindableBase
    {
        #region Fields
        private ObservableCollection<LogInfo> _logs = new();
        private LogInfo? _selectedLog;
        private string _searchText = string.Empty;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private string _selectedLogType = "全部";
        private string _selectedActionType = "全部";
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalCount = 0;
        #endregion

        #region Properties
        public ObservableCollection<LogInfo> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        public LogInfo? SelectedLog
        {
            get => _selectedLog;
            set => SetProperty(ref _selectedLog, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public string SelectedLogType
        {
            get => _selectedLogType;
            set => SetProperty(ref _selectedLogType, value);
        }

        public string SelectedActionType
        {
            get => _selectedActionType;
            set => SetProperty(ref _selectedActionType, value);
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

        public ObservableCollection<string> LogTypes { get; } = new() { "全部", "用户操作", "系统日志", "错误日志", "审计日志" };
        public ObservableCollection<string> ActionTypes { get; } = new() { "全部", "登录", "登出", "创建", "编辑", "删除", "查询" };
        #endregion

        #region Commands
        public ICommand SearchCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }
        public ICommand CleanupCommand { get; private set; }
        public ICommand ViewDetailsCommand { get; private set; }
        public ICommand PreviousPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        #endregion

        #region Constructor
        public SystemLogsViewModel()
        {
            InitializeCommands();
            LoadData();
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            SearchCommand = new DelegateCommand(OnSearch);
            RefreshCommand = new DelegateCommand(OnRefresh);
            ExportCommand = new DelegateCommand(OnExport);
            CleanupCommand = new DelegateCommand(OnCleanup);
            ViewDetailsCommand = new DelegateCommand<LogInfo>(OnViewDetails);
            PreviousPageCommand = new DelegateCommand(OnPreviousPage, () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(OnNextPage, () => CurrentPage < TotalPages);
        }

        private async void LoadData()
        {
            // TODO: 调用API加载日志数据
            await Task.Delay(100);
        }

        private void OnSearch() { CurrentPage = 1; LoadData(); }
        private void OnRefresh() { LoadData(); }
        private void OnExport() { /* TODO */ }
        private void OnCleanup() { /* TODO */ }
        private void OnViewDetails(LogInfo? log) { /* TODO */ }
        private void OnPreviousPage() { if (CurrentPage > 1) { CurrentPage--; LoadData(); } }
        private void OnNextPage() { if (CurrentPage < TotalPages) { CurrentPage++; LoadData(); } }
        #endregion
    }
}