using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方高级查询对话框视图模型 (ENTRY-17)
    /// 提供处方历史的高级查询功能
    /// </summary>
    public class PrescriptionSearchDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        private readonly IPrescriptionRepository _prescriptionRepository;

        #endregion

        #region 数据属性

        private string _patientName = string.Empty;
        private string _symptomKeyword = string.Empty;
        private ObservableCollection<PrescriptionSearchResultDto> _searchResults = new();
        private PrescriptionSearchResultDto? _selectedPrescription;

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        /// <summary>
        /// 症状关键词
        /// </summary>
        public string SymptomKeyword
        {
            get => _symptomKeyword;
            set => SetProperty(ref _symptomKeyword, value);
        }

        /// <summary>
        /// 搜索结果列表
        /// </summary>
        public ObservableCollection<PrescriptionSearchResultDto> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        /// <summary>
        /// 选中的处方
        /// </summary>
        public PrescriptionSearchResultDto? SelectedPrescription
        {
            get => _selectedPrescription;
            set
            {
                if (SetProperty(ref _selectedPrescription, value))
                {
                    SelectCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region 对话框属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title { get; set; } = "高级查询历史处方";

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
        /// 选择命令
        /// </summary>
        public DelegateCommand SelectCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionSearchDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IPrescriptionRepository prescriptionRepository,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _prescriptionRepository = prescriptionRepository ?? throw new ArgumentNullException(nameof(prescriptionRepository));

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync(), CanSearch);
            SelectCommand = new DelegateCommand(ExecuteSelect, CanSelect);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PatientName) || e.PropertyName == nameof(SymptomKeyword))
                {
                    SearchCommand.RaiseCanExecuteChanged();
                }
            };
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
                if (parameters.ContainsKey("PatientName"))
                {
                    PatientName = parameters.GetValue<string>("PatientName");
                }

                if (parameters.ContainsKey("SymptomKeyword"))
                {
                    SymptomKeyword = parameters.GetValue<string>("SymptomKeyword");
                }

                // 如果有初始搜索条件，自动执行搜索
                if (!string.IsNullOrWhiteSpace(PatientName) || !string.IsNullOrWhiteSpace(SymptomKeyword))
                {
                    Task.Run(async () => await ExecuteSearchAsync());
                }

                Logger.LogInformation("处方高级查询对话框已打开");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开处方查询对话框时发生异常");
                ShowErrorMessage("初始化失败，请稍后重试");
            }
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
                SetIsBusy(true, "正在搜索处方...");

                // 清空之前的搜索结果
                SearchResults.Clear();

                // 调用搜索API
                var result = await _prescriptionRepository.SearchPrescriptionsAsync(
                    string.IsNullOrWhiteSpace(PatientName) ? null : PatientName,
                    string.IsNullOrWhiteSpace(SymptomKeyword) ? null : SymptomKeyword);

                if (result.IsSuccess && result.Data != null)
                {
                    foreach (var prescription in result.Data)
                    {
                        SearchResults.Add(prescription);
                    }

                    Logger.LogInformation("搜索完成，找到 {Count} 条处方记录", SearchResults.Count);

                    if (SearchResults.Count == 0)
                    {
                        ShowInfoMessage("未找到符合条件的处方记录");
                    }
                }
                else
                {
                    Logger.LogWarning("搜索处方失败：{Message}", result.Message);
                    ShowErrorMessage($"搜索失败：{result.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索处方时发生异常");
                ShowErrorMessage("搜索失败，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 执行选择
        /// </summary>
        private void ExecuteSelect()
        {
            if (SelectedPrescription != null)
            {
                var parameters = new DialogParameters
                {
                    { "SelectedPrescription", SelectedPrescription }
                };

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
                Logger.LogInformation("选择处方：PrescriptionId={PrescriptionId}, PatientName={PatientName}",
                    SelectedPrescription.PrescriptionId, SelectedPrescription.PatientName);
            }
        }

        /// <summary>
        /// 执行取消
        /// </summary>
        private void ExecuteCancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            Logger.LogInformation("取消处方查询");
        }

        #endregion

        #region 命令状态检查

        private bool CanSearch()
        {
            // 至少需要填写一个搜索条件
            return !IsBusy &&
                   (!string.IsNullOrWhiteSpace(PatientName) || !string.IsNullOrWhiteSpace(SymptomKeyword));
        }

        private bool CanSelect()
        {
            return SelectedPrescription != null;
        }

        #endregion
    }
}
