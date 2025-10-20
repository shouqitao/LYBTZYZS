using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 患者选择ViewModel - Task #1497 Step 1实现
    /// 支持搜索、选择患者，选择后通知父ViewModel自动创建MedicalCase
    /// Epic #1494: 医案流程UI重构
    /// </summary>
    public class PatientSelectionViewModel : UnifiedViewModelBase, IValidatable, ISaveable
    {
        #region 服务依赖

        private readonly IPatientRepository _patientRepository;
        private readonly IDialogService _dialogService;

        #endregion

        #region 数据属性

        // 患者列表
        private ObservableCollection<PatientDto> _patients = new();
        public ObservableCollection<PatientDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        // 已选患者
        private PatientDto? _selectedPatient;
        public PatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    SelectPatientCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // 搜索关键字
        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // 分页属性
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalCount = 0;
        private int _totalPages = 0;

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        // IValidatable实现
        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 选择患者命令
        /// </summary>
        public DelegateCommand SelectPatientCommand { get; }

        /// <summary>
        /// 新建患者命令
        /// </summary>
        public DelegateCommand NewPatientCommand { get; }

        /// <summary>
        /// 双击行命令（DataGrid双击）
        /// </summary>
        public DelegateCommand<PatientDto> RowDoubleClickCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 患者已选择事件（通知父ViewModel）
        /// </summary>
        public event EventHandler<PatientDto>? PatientSelected;

        #endregion

        #region 构造函数

        public PatientSelectionViewModel(
            IPatientRepository patientRepository,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync(), CanExecuteSearch);
            SelectPatientCommand = new DelegateCommand(ExecuteSelectPatient, CanExecuteSelectPatient);
            NewPatientCommand = new DelegateCommand(ExecuteNewPatient);
            RowDoubleClickCommand = new DelegateCommand<PatientDto>(ExecuteRowDoubleClick);
            PreviousPageCommand = new DelegateCommand(async () => await ExecutePreviousPageAsync(), CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(async () => await ExecuteNextPageAsync(), CanExecuteNextPage);

            Logger.LogInformation("PatientSelectionViewModel已初始化");
        }

        #endregion

        #region IValidatable实现

        /// <summary>
        /// 验证是否已选择患者
        /// </summary>
        public bool Validate()
        {
            if (SelectedPatient == null)
            {
                ValidationMessage = "请先选择患者";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        #endregion

        #region ISaveable实现

        /// <summary>
        /// 保存操作（对于患者选择，不需要保存，直接返回true）
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            // 患者选择步骤不需要保存操作
            // 选择患者后，父ViewModel会自动创建MedicalCase
            await Task.CompletedTask;
            return true;
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 执行搜索
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            try
            {
                SetIsBusy(true, "正在搜索患者...");

                CurrentPage = 1; // 重置到第一页
                await LoadPatientsAsync();

                Logger.LogInformation("搜索患者，关键字：{Keyword}，结果数量：{Count}", SearchKeyword, Patients.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索患者时发生异常");
                await ShowErrorMessageAsync($"搜索失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanExecuteSearch()
        {
            return !IsBusy;
        }

        /// <summary>
        /// 选择患者
        /// </summary>
        private void ExecuteSelectPatient()
        {
            if (SelectedPatient == null)
            {
                ShowErrorMessage("请先选择患者");
                return;
            }

            try
            {
                Logger.LogInformation("选择患者：{PatientName} (ID: {PatientId})",
                    SelectedPatient.Name, SelectedPatient.Id);

                // 触发事件，通知父ViewModel
                PatientSelected?.Invoke(this, SelectedPatient);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "选择患者时发生异常");
                ShowErrorMessage($"选择患者失败：{ex.Message}");
            }
        }

        private bool CanExecuteSelectPatient()
        {
            return SelectedPatient != null;
        }

        /// <summary>
        /// 新建患者
        /// </summary>
        private void ExecuteNewPatient()
        {
            try
            {
                // TODO: Task #1502 - 打开快速新建患者对话框
                Logger.LogInformation("打开快速新建患者对话框（功能开发中）");
                ShowInfoMessage("快速新建患者功能开发中...");

                // 新建成功后，刷新列表
                // await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "新建患者时发生异常");
                ShowErrorMessage($"新建患者失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 双击行选择患者
        /// </summary>
        private void ExecuteRowDoubleClick(PatientDto? patient)
        {
            if (patient != null)
            {
                SelectedPatient = patient;
                ExecuteSelectPatient();
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private async Task ExecutePreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadPatientsAsync();
            }
        }

        private bool CanExecutePreviousPage()
        {
            return CurrentPage > 1;
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private async Task ExecuteNextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadPatientsAsync();
            }
        }

        private bool CanExecuteNextPage()
        {
            return CurrentPage < TotalPages;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 加载患者列表（分页）
        /// </summary>
        private async Task LoadPatientsAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载患者列表...");

                var result = await _patientRepository.GetPagedAsync(
                    CurrentPage,
                    _pageSize,
                    string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword);

                Patients.Clear();
                foreach (var patient in result.Items)
                {
                    Patients.Add(patient);
                }

                TotalCount = result.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / _pageSize);

                Logger.LogInformation("加载患者列表成功，当前页：{Page}/{TotalPages}，总数：{TotalCount}",
                    CurrentPage, TotalPages, TotalCount);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表时发生异常");
                await ShowErrorMessageAsync($"加载失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region INavigationAware

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            try
            {
                // 获取搜索关键字（从HomeView传来）
                var searchKeyword = navigationContext.Parameters.GetValue<string>("SearchKeyword");
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    SearchKeyword = searchKeyword;
                    Logger.LogInformation("接收到搜索关键字：{SearchKeyword}", searchKeyword);
                }

                // 加载患者列表
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到患者选择界面时发生异常");
            }
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public override void OnNavigatedFrom(NavigationContext navigationContext) { }

        #endregion
    }
}
