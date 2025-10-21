using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者选择ViewModel - Issue #1557 Step 1
    /// 看诊流程模块化迁移 - DDD聚合根对齐
    ///
    /// 功能：
    /// - 搜索患者（姓名/拼音码/手机号）
    /// - 分页加载（每页50条）
    /// - 选择患者后发布PatientSelectedEvent事件
    /// - 支持新建患者快速对话框
    /// - 通过EventAggregator与MedicalCaseFlowViewModel解耦通信
    /// </summary>
    public class PatientSelectionViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IPatientRepository _patientRepository;
        private readonly IDialogService _dialogService;

        #endregion

        #region 流程上下文属性

        private Guid _medicalCaseFlowId;
        /// <summary>
        /// 医案流程ID（从MedicalCaseFlowViewModel通过NavigationParameters传入）
        /// </summary>
        public Guid MedicalCaseFlowId
        {
            get => _medicalCaseFlowId;
            set => SetProperty(ref _medicalCaseFlowId, value);
        }

        #endregion

        #region 数据属性

        /// <summary>
        /// 患者列表（搜索结果或分页数据）
        /// </summary>
        public ObservableCollection<PatientDto> Patients { get; } = new();

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
                }
            }
        }

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
                    SearchCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region 分页属性

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

        private const int PageSize = 50; // 每页50条记录

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand NewPatientCommand { get; }
        public DelegateCommand SelectPatientCommand { get; }
        public DelegateCommand<PatientDto> DoubleClickPatientCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }

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
            NewPatientCommand = new DelegateCommand(ExecuteNewPatient);
            SelectPatientCommand = new DelegateCommand(ExecuteSelectPatient, CanExecuteSelectPatient);
            DoubleClickPatientCommand = new DelegateCommand<PatientDto>(ExecuteDoubleClickPatient);
            PreviousPageCommand = new DelegateCommand(async () => await ExecutePreviousPageAsync(), CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(async () => await ExecuteNextPageAsync(), CanExecuteNextPage);

            Logger.LogInformation("PatientSelectionViewModel已初始化");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 搜索患者
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            try
            {
                SetIsBusy(true, "正在搜索患者...");

                Logger.LogInformation("搜索患者，关键字：{Keyword}", SearchKeyword);

                // 重置到第1页
                CurrentPage = 1;

                // 调用分页API（传入搜索关键字）
                var result = await _patientRepository.GetPagedAsync(CurrentPage, PageSize, SearchKeyword);

                // 更新患者列表
                Patients.Clear();
                foreach (var patient in result.Items)
                {
                    Patients.Add(patient);
                }

                // 更新分页信息
                TotalPages = result.TotalPages;
                TotalCount = result.TotalCount;

                Logger.LogInformation("搜索成功，找到{TotalCount}条记录，当前显示第{CurrentPage}页", TotalCount, CurrentPage);

                // 触发分页命令更新
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索患者失败");
                await ShowErrorMessageAsync($"搜索失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanExecuteSearch()
        {
            return !string.IsNullOrWhiteSpace(SearchKeyword);
        }

        /// <summary>
        /// 新建患者
        /// Issue #1543: 集成QuickCreatePatientDialog
        /// </summary>
        private void ExecuteNewPatient()
        {
            try
            {
                Logger.LogInformation("打开快速新建患者对话框");

                _dialogService.ShowDialog("QuickCreatePatientDialog", new DialogParameters(), result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        var newPatient = result.Parameters.GetValue<PatientDto>("NewPatient");
                        if (newPatient != null)
                        {
                            Logger.LogInformation("新建患者成功：{PatientName}（ID: {PatientId}）",
                                newPatient.Name, newPatient.Id);

                            // 1. 将新患者添加到列表顶部
                            Patients.Insert(0, newPatient);

                            // 2. 自动选中新患者
                            SelectedPatient = newPatient;

                            // 3. 发布患者选择事件（使用EventAggregator）
                            PublishPatientSelectedEvent(newPatient);
                        }
                        else
                        {
                            Logger.LogWarning("对话框返回的患者数据为空");
                        }
                    }
                    else
                    {
                        Logger.LogInformation("用户取消了快速新建患者");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开新建患者对话框失败");
            }
        }

        /// <summary>
        /// 选择患者（点击【选择】按钮）
        /// </summary>
        private void ExecuteSelectPatient()
        {
            if (SelectedPatient == null)
            {
                Logger.LogWarning("未选择患者");
                return;
            }

            try
            {
                Logger.LogInformation("选择患者：{PatientName}（ID: {PatientId}）", SelectedPatient.Name, SelectedPatient.Id);

                // 发布患者选择事件（使用EventAggregator）
                PublishPatientSelectedEvent(SelectedPatient);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "选择患者失败");
            }
        }

        private bool CanExecuteSelectPatient()
        {
            return SelectedPatient != null;
        }

        /// <summary>
        /// 双击患者行（快捷选择）
        /// </summary>
        private void ExecuteDoubleClickPatient(PatientDto? patient)
        {
            if (patient == null)
            {
                Logger.LogWarning("双击的患者为空");
                return;
            }

            try
            {
                Logger.LogInformation("双击选择患者：{PatientName}（ID: {PatientId}）", patient.Name, patient.Id);

                SelectedPatient = patient;

                // 发布患者选择事件（使用EventAggregator）
                PublishPatientSelectedEvent(patient);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "双击选择患者失败");
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private async Task ExecutePreviousPageAsync()
        {
            if (!CanExecutePreviousPage())
                return;

            try
            {
                SetIsBusy(true, "正在加载上一页...");

                CurrentPage--;
                await LoadCurrentPageAsync();

                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载上一页失败");
                await ShowErrorMessageAsync($"加载失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
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
            if (!CanExecuteNextPage())
                return;

            try
            {
                SetIsBusy(true, "正在加载下一页...");

                CurrentPage++;
                await LoadCurrentPageAsync();

                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载下一页失败");
                await ShowErrorMessageAsync($"加载失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanExecuteNextPage()
        {
            return CurrentPage < TotalPages;
        }

        #endregion

        #region 事件发布辅助方法

        /// <summary>
        /// 发布患者选择事件
        /// Issue #1557 - 使用EventAggregator替代.NET Event，实现模块间解耦
        /// </summary>
        /// <param name="patient">选中的患者</param>
        private void PublishPatientSelectedEvent(PatientDto patient)
        {
            try
            {
                var payload = new PatientSelectedPayload
                {
                    PatientId = patient.Id,
                    PatientName = patient.Name,
                    Gender = patient.Gender.ToString(),  // Gender枚举转换为string
                    Age = patient.Age ?? 0,  // 处理可空int
                    PhoneNumber = patient.PhoneNumber ?? string.Empty,
                    LastVisitDate = patient.LastVisitTime,  // 属性名修正：LastVisitTime
                    VisitCount = patient.VisitCount,
                    AllergyHistory = patient.AllergyHistory ?? string.Empty,
                    MedicalCaseFlowId = this.MedicalCaseFlowId,
                    SelectedAt = DateTime.Now
                };

                EventAggregator.GetEvent<PatientSelectedEvent>()
                    .Publish(payload);

                Logger.LogInformation("已发布PatientSelectedEvent，患者：{PatientName}，流程ID：{FlowId}",
                    patient.Name, MedicalCaseFlowId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "发布PatientSelectedEvent失败");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 加载当前页数据
        /// </summary>
        private async Task LoadCurrentPageAsync()
        {
            Logger.LogInformation("加载第{CurrentPage}页患者数据", CurrentPage);

            var result = await _patientRepository.GetPagedAsync(CurrentPage, PageSize, SearchKeyword);

            Patients.Clear();
            foreach (var patient in result.Items)
            {
                Patients.Add(patient);
            }

            TotalPages = result.TotalPages;
            TotalCount = result.TotalCount;

            Logger.LogInformation("加载成功，当前第{CurrentPage}/{TotalPages}页，共{TotalCount}条记录", CurrentPage, TotalPages, TotalCount);
        }

        /// <summary>
        /// 加载初始患者列表（第1页）
        /// </summary>
        private async Task LoadInitialPatientsAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载患者列表...");

                Logger.LogInformation("加载初始患者列表（第1页）");

                CurrentPage = 1;
                await LoadCurrentPageAsync();

                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表失败");
                await ShowErrorMessageAsync($"加载患者列表失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            try
            {
                // Issue #1557 - 接收MedicalCaseFlowViewModel传来的流程ID
                var flowId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseFlowId");
                if (flowId != Guid.Empty)
                {
                    Logger.LogInformation("接收到医案流程ID：{FlowId}", flowId);
                    MedicalCaseFlowId = flowId;
                }
                else
                {
                    Logger.LogWarning("未接收到有效的医案流程ID，将生成新的流程ID");
                    MedicalCaseFlowId = Guid.NewGuid();
                }

                // 接收HomeView传来的搜索关键字（保留原有功能）
                var searchKeyword = navigationContext.Parameters.GetValue<string>("SearchKeyword");
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    Logger.LogInformation("接收到搜索关键字：{SearchKeyword}", searchKeyword);
                    SearchKeyword = searchKeyword;
                    // 自动触发搜索
                    _ = ExecuteSearchAsync();
                }
                else
                {
                    // 无搜索关键字，加载第1页数据
                    _ = LoadInitialPatientsAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到患者选择视图时发生异常");
            }
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 允许重复导航（每次进入Step 1都重新加载数据）
            return false;
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            Logger.LogInformation("离开患者选择视图，当前选择：{PatientName}，流程ID：{FlowId}",
                SelectedPatient?.Name ?? "未选择", MedicalCaseFlowId);
        }

        #endregion
    }
}
