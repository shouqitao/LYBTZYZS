using System.Collections.ObjectModel;
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
    /// 患者选择对话框视图模型
    /// Issue #1457: 临床工作台患者选择功能
    /// Epic #1456: 看诊流程完整实现
    /// </summary>
    public class PatientSelectionDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        private readonly IPatientRepository _patientRepository;
        private readonly IDialogService _dialogService;

        #endregion

        #region 数据属性

        private string _searchKeyword = string.Empty;
        private ObservableCollection<PatientDto> _patients = new();
        private PatientDto? _selectedPatient;

        /// <summary>
        /// 搜索关键字 (支持姓名/拼音码/手机号)
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>
        /// 患者列表
        /// </summary>
        public ObservableCollection<PatientDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

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
                    UpdateCommandStates();
                }
            }
        }

        #endregion

        #region 对话框属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title { get; set; } = "选择患者";

        /// <summary>
        /// 对话框关闭事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

        #endregion

        #region 命令

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 确定命令
        /// </summary>
        public DelegateCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 新建患者命令
        /// </summary>
        public DelegateCommand NewPatientCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        /// <summary>
        /// 双击选择命令 (DataGrid行双击)
        /// </summary>
        public DelegateCommand<PatientDto> DoubleClickSelectCommand { get; }

        #endregion

        #region 构造函数

        public PatientSelectionDialogViewModel(
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
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            CancelCommand = new DelegateCommand(Cancel);
            NewPatientCommand = new DelegateCommand(NewPatient);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            DoubleClickSelectCommand = new DelegateCommand<PatientDto>(DoubleClickSelect, p => p != null);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => UpdateCommandStates();
        }

        #endregion

        #region IDialogAware 实现

        /// <summary>
        /// 是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog() => true;

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed() { }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                // 获取参数
                if (parameters.ContainsKey("Title"))
                {
                    Title = parameters.GetValue<string>("Title");
                }

                if (parameters.ContainsKey("DefaultKeyword"))
                {
                    SearchKeyword = parameters.GetValue<string>("DefaultKeyword");
                }

                // 加载最近就诊患者
                Task.Run(async () => await LoadRecentPatientsAsync());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开患者选择对话框时发生异常");
                ShowErrorMessage("初始化失败,请稍后重试");
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载最近就诊患者 (默认显示)
        /// </summary>
        private async Task LoadRecentPatientsAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载患者列表...");

                // 加载最近就诊的患者 (按最近就诊时间排序)
                var pagedData = await _patientRepository.GetPagedAsync(1, 20, null);
                Patients.Clear();
                foreach (var patient in pagedData.Items)
                {
                    Patients.Add(patient);
                }

                Logger.LogInformation("患者列表加载完成,共 {Count} 人", Patients.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表时发生异常");
                await ShowErrorMessageAsync("加载患者列表时发生系统错误,请稍后重试");
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
                SetIsBusy(true, "正在搜索...");

                // 如果搜索框为空,加载最近患者
                if (string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    await LoadRecentPatientsAsync();
                    return;
                }

                // 搜索患者 (支持姓名/拼音码/手机号)
                var pagedData = await _patientRepository.GetPagedAsync(1, 50, SearchKeyword);
                Patients.Clear();
                foreach (var patient in pagedData.Items)
                {
                    Patients.Add(patient);
                }

                Logger.LogInformation("搜索完成,找到 {Count} 个患者,关键字: {Keyword}", Patients.Count, SearchKeyword);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索患者时发生异常,关键字: {Keyword}", SearchKeyword);
                await ShowErrorMessageAsync("搜索失败,请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private async Task RefreshAsync()
        {
            SearchKeyword = string.Empty;
            await LoadRecentPatientsAsync();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 确定 - 返回选中患者
        /// </summary>
        private void Confirm()
        {
            if (SelectedPatient != null)
            {
                var parameters = new DialogParameters
                {
                    { "SelectedPatient", SelectedPatient }
                };

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
                Logger.LogInformation("选择患者: {PatientName} (ID: {PatientId})", SelectedPatient.Name, SelectedPatient.Id);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            Logger.LogInformation("取消选择患者");
        }

        /// <summary>
        /// 新建患者 (快速创建) - Issue #1487
        /// </summary>
        private void NewPatient()
        {
            try
            {
                Logger.LogInformation("打开快速创建患者对话框");

                // 打开QuickCreatePatientDialog
                var parameters = new DialogParameters();
                _dialogService.ShowDialog("QuickCreatePatientDialog", parameters, async result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        // 获取新创建的患者
                        var newPatient = result.Parameters.GetValue<PatientDto>("NewPatient");
                        if (newPatient != null)
                        {
                            Logger.LogInformation("患者创建成功: {PatientName} (ID: {PatientId})", newPatient.Name, newPatient.Id);

                            // 刷新患者列表
                            await SearchAsync();

                            // 自动选中新创建的患者
                            SelectedPatient = Patients.FirstOrDefault(p => p.Id == newPatient.Id);

                            // Issue #1487: 创建成功后自动关闭PatientSelectionDialog并返回选中患者
                            if (SelectedPatient != null)
                            {
                                var returnParameters = new DialogParameters
                                {
                                    { "SelectedPatient", SelectedPatient }
                                };
                                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, returnParameters));
                            }
                        }
                        else
                        {
                            Logger.LogWarning("QuickCreatePatientDialog返回OK但未提供患者信息");
                        }
                    }
                    else
                    {
                        Logger.LogInformation("取消快速创建患者");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开快速创建患者对话框时发生异常");
                ShowErrorMessage("打开对话框失败，请稍后重试");
            }
        }

        /// <summary>
        /// 双击选择患者 (DataGrid行双击)
        /// </summary>
        private void DoubleClickSelect(PatientDto patient)
        {
            if (patient != null)
            {
                SelectedPatient = patient;
                Confirm();
            }
        }

        #endregion

        #region 命令状态检查

        private bool CanConfirm() => SelectedPatient != null && !IsBusy;

        private void UpdateCommandStates()
        {
            ConfirmCommand.RaiseCanExecuteChanged();
            DoubleClickSelectCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
