using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// Step 1 - 患者选择ViewModel（Epic #1494 - Task #1497）
    /// 支持搜索、新建患者、选择患者
    /// </summary>
    public class PatientSelectionViewModel : UnifiedViewModelBase, IValidatable
    {
        #region 字段

        private readonly IPatientRepository _patientRepository;
        private readonly ICommonDialogService _dialogService;

        #endregion

        #region 属性

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
        /// 当前选中的患者
        /// </summary>
        public PatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    SelectPatientCommand.RaiseCanExecuteChanged();
                    RaisePropertyChanged(nameof(ValidationMessage));
                }
            }
        }

        private string _searchKeyword = string.Empty;
        /// <summary>
        /// 搜索关键字（姓名/拼音码/手机号）
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // 实时搜索
                    _ = SearchPatientsAsync();
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

        private const int PageSize = 50; // 每页显示50条记录（Issue #1497要求）

        #endregion

        #region IValidatable实现

        /// <summary>
        /// 验证是否已选择患者
        /// </summary>
        public bool Validate()
        {
            return SelectedPatient != null;
        }

        /// <summary>
        /// 验证错误消息
        /// </summary>
        public string ValidationMessage => SelectedPatient == null ? "请选择一位患者" : string.Empty;

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand NewPatientCommand { get; }
        public DelegateCommand<PatientDto> SelectPatientCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 患者选择事件（通知父ViewModel）
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
            SearchCommand = new DelegateCommand(async () => await SearchPatientsAsync());
            NewPatientCommand = new DelegateCommand(ExecuteNewPatient);
            SelectPatientCommand = new DelegateCommand<PatientDto>(ExecuteSelectPatient, CanExecuteSelectPatient);
            PreviousPageCommand = new DelegateCommand(async () => await ExecutePreviousPageAsync(), CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(async () => await ExecuteNextPageAsync(), CanExecuteNextPage);

            Logger.LogInformation("PatientSelectionViewModel已初始化");

            // 自动加载第一页数据
            _ = LoadPatientsAsync();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 搜索患者
        /// </summary>
        private async Task SearchPatientsAsync()
        {
            try
            {
                CurrentPage = 1; // 重置到第一页
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索患者失败，关键字：{SearchKeyword}", SearchKeyword);
                await ShowErrorMessageAsync($"搜索失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 加载患者列表（分页）
        /// </summary>
        private async Task LoadPatientsAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载患者列表...");

                Logger.LogInformation("加载患者列表，页码：{CurrentPage}，每页：{PageSize}，关键字：{SearchKeyword}",
                    CurrentPage, PageSize, SearchKeyword);

                var result = await _patientRepository.GetPagedAsync(CurrentPage, PageSize, string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword);

                Patients.Clear();
                if (result.Items != null)
                {
                    foreach (var patient in result.Items)
                    {
                        Patients.Add(patient);
                    }
                }

                TotalCount = result.TotalCount;
                TotalPages = result.TotalPages;
                CurrentPage = result.CurrentPage;

                Logger.LogInformation("加载患者列表成功，共{TotalCount}条记录，当前第{CurrentPage}页/{TotalPages}页",
                    TotalCount, CurrentPage, TotalPages);

                // 更新分页命令状态
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表失败");
                await ShowErrorMessageAsync($"加载失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 新建患者
        /// </summary>
        private async void ExecuteNewPatient()
        {
            try
            {
                Logger.LogInformation("打开新建患者对话框");

                // TODO: Task #1497实现后，打开快速新建患者对话框
                // var result = _dialogService.ShowDialog("PatientQuickCreateDialog");
                // if (result.Success)
                // {
                //     var newPatient = result.Data as PatientDto;
                //     Patients.Insert(0, newPatient); // 添加到列表顶部
                //     SelectedPatient = newPatient; // 自动选中
                // }

                await _dialogService.ShowWarningAsync("新建患者功能待实现（需要创建快速新建对话框）");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "新建患者失败");
            }
        }

        /// <summary>
        /// 选择患者
        /// </summary>
        private void ExecuteSelectPatient(PatientDto? patient)
        {
            if (patient == null)
            {
                Logger.LogWarning("选择患者失败：患者为空");
                return;
            }

            try
            {
                Logger.LogInformation("选择患者：{PatientName}（Id: {PatientId}）", patient.Name, patient.Id);

                SelectedPatient = patient;

                // 触发事件，通知父ViewModel
                PatientSelected?.Invoke(this, patient);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "选择患者失败");
            }
        }

        private bool CanExecuteSelectPatient(PatientDto? patient)
        {
            return patient != null;
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
    }
}
