using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 患者选择视图模型 - Epic #1494 Task #1497
    /// 用于医案流程Step 1：患者选择
    /// 支持搜索、新建患者、选择患者后通知父ViewModel
    /// </summary>
    public class PatientSelectionViewModel : UnifiedViewModelBase
    {
        #region 字段

        private readonly IPatientRepository _patientRepository;
        private readonly ICommonDialogService _dialogService;

        #endregion

        #region 属性

        private string _searchKeyword = string.Empty;
        /// <summary>
        /// 搜索关键字（支持姓名/拼音码/手机号）
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // 实时搜索
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        _ = LoadPatientsAsync();
                    }
                }
            }
        }

        private ObservableCollection<PatientDto> _patients = new();
        /// <summary>
        /// 患者列表
        /// </summary>
        public ObservableCollection<PatientDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientDto? _selectedPatient;
        /// <summary>
        /// 选中的患者
        /// </summary>
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

        private int _currentPage = 1;
        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _totalPages = 1;
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        private int _totalCount = 0;
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        /// <summary>
        /// 每页显示记录数
        /// </summary>
        public int PageSize { get; } = 50;

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand NewPatientCommand { get; }
        public DelegateCommand SelectPatientCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<PatientDto> DoubleClickSelectCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 患者选择事件 - 通知父ViewModel（MedicalCaseFlowViewModel）
        /// </summary>
        public event EventHandler<PatientDto>? PatientSelected;

        #endregion

        #region 构造函数

        public PatientSelectionViewModel(
            IPatientRepository patientRepository,
            ICommonDialogService dialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            NewPatientCommand = new DelegateCommand(ExecuteNewPatient);
            SelectPatientCommand = new DelegateCommand(ExecuteSelectPatient, CanSelectPatient);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            DoubleClickSelectCommand = new DelegateCommand<PatientDto>(ExecuteDoubleClickSelect, p => p != null);
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), CanNextPage);
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), CanPreviousPage);

            Logger.LogInformation("PatientSelectionViewModel已初始化");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化加载数据 - 由MedicalCaseFlowViewModel调用
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                Logger.LogInformation("初始化患者选择视图");
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "初始化患者选择视图失败");
                await ShowErrorMessageAsync("初始化失败，请稍后重试");
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载患者列表（分页）
        /// </summary>
        private async Task LoadPatientsAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载患者列表...");

                var pagedResult = await _patientRepository.GetPagedAsync(
                    CurrentPage,
                    PageSize,
                    string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword);

                Patients.Clear();
                foreach (var patient in pagedResult.Items)
                {
                    Patients.Add(patient);
                }

                TotalCount = pagedResult.TotalCount;
                TotalPages = pagedResult.TotalPages;

                Logger.LogInformation("患者列表加载完成，共 {TotalCount} 人，当前第 {CurrentPage}/{TotalPages} 页",
                    TotalCount, CurrentPage, TotalPages);

                // 更新分页命令状态
                NextPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表失败");
                await ShowErrorMessageAsync("加载患者列表失败，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 搜索患者
        /// </summary>
        private async Task SearchAsync()
        {
            try
            {
                Logger.LogInformation("搜索患者，关键字: {SearchKeyword}", SearchKeyword);

                // 重置到第一页
                CurrentPage = 1;
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索患者失败，关键字: {SearchKeyword}", SearchKeyword);
                await ShowErrorMessageAsync("搜索失败，请稍后重试");
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private async Task RefreshAsync()
        {
            try
            {
                Logger.LogInformation("刷新患者列表");
                SearchKeyword = string.Empty;
                CurrentPage = 1;
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "刷新患者列表失败");
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadPatientsAsync();
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadPatientsAsync();
            }
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 新建患者
        /// </summary>
        private void ExecuteNewPatient()
        {
            try
            {
                Logger.LogInformation("打开新建患者对话框");
                // TODO: Task #1497+ - 实现快速新建患者对话框
                _dialogService.ShowInfoAsync("快速新建患者功能开发中...", "新建患者");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开新建患者对话框失败");
            }
        }

        /// <summary>
        /// 选择患者
        /// </summary>
        private void ExecuteSelectPatient()
        {
            if (SelectedPatient != null)
            {
                Logger.LogInformation("选择患者: {PatientName} (ID: {PatientId})",
                    SelectedPatient.Name, SelectedPatient.Id);

                // 触发事件，通知父ViewModel（MedicalCaseFlowViewModel）
                PatientSelected?.Invoke(this, SelectedPatient);
            }
        }

        /// <summary>
        /// 双击选择患者
        /// </summary>
        private void ExecuteDoubleClickSelect(PatientDto patient)
        {
            if (patient != null)
            {
                SelectedPatient = patient;
                ExecuteSelectPatient();
            }
        }

        #endregion

        #region 命令状态检查

        private bool CanSelectPatient() => SelectedPatient != null && !IsBusy;
        private bool CanNextPage() => CurrentPage < TotalPages && !IsBusy;
        private bool CanPreviousPage() => CurrentPage > 1 && !IsBusy;

        #endregion
    }
}
