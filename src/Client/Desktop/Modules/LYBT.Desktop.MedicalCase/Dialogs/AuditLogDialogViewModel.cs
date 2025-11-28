using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// 审计日志对话框ViewModel
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
    /// 用于显示医案的变更历史记录
    /// </summary>
    public class AuditLogDialogViewModel : BindableBase, IDialogAware
    {
        #region 服务依赖

        private readonly IMedicalCaseApi _medicalCaseApi;
        private readonly ILogger<AuditLogDialogViewModel> _logger;
        private Guid _medicalCaseId;

        #endregion

        #region 属性

        private string _caseNumber = string.Empty;
        /// <summary>
        /// 医案编号
        /// </summary>
        public string CaseNumber
        {
            get => _caseNumber;
            set => SetProperty(ref _caseNumber, value);
        }

        private string _patientName = string.Empty;
        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private ObservableCollection<MedicalCaseAuditLogDto> _auditLogs = new();
        /// <summary>
        /// 审计日志列表
        /// </summary>
        public ObservableCollection<MedicalCaseAuditLogDto> AuditLogs
        {
            get => _auditLogs;
            set => SetProperty(ref _auditLogs, value);
        }

        private MedicalCaseAuditLogDto? _selectedLog;
        /// <summary>
        /// 选中的日志
        /// </summary>
        public MedicalCaseAuditLogDto? SelectedLog
        {
            get => _selectedLog;
            set => SetProperty(ref _selectedLog, value);
        }

        private bool _isLoading;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
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

        private int _totalCount;
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        #endregion

        #region IDialogAware

        public string Title => $"变更记录 - {CaseNumber}";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue("MedicalCaseId", out Guid medicalCaseId))
            {
                _medicalCaseId = medicalCaseId;
            }

            if (parameters.TryGetValue("CaseNumber", out string? caseNumber))
            {
                CaseNumber = caseNumber ?? string.Empty;
            }

            if (parameters.TryGetValue("PatientName", out string? patientName))
            {
                PatientName = patientName ?? string.Empty;
            }

            LoadAuditLogsAsync();
        }

        #endregion

        #region 命令

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand RefreshCommand { get; }

        #endregion

        #region 构造函数

        public AuditLogDialogViewModel(
            IMedicalCaseApi medicalCaseApi,
            ILogger<AuditLogDialogViewModel> logger)
        {
            _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            CloseCommand = new DelegateCommand(ExecuteClose);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, CanPreviousPage)
                .ObservesProperty(() => CurrentPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage, CanNextPage)
                .ObservesProperty(() => CurrentPage)
                .ObservesProperty(() => TotalPages);
            RefreshCommand = new DelegateCommand(() => LoadAuditLogsAsync());

            _logger.LogInformation("AuditLogDialogViewModel已初始化");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载审计日志
        /// </summary>
        private async void LoadAuditLogsAsync()
        {
            if (_medicalCaseId == Guid.Empty)
            {
                StatusMessage = "未指定医案";
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "正在加载变更记录...";

                var response = await _medicalCaseApi.GetAuditLogsAsync(_medicalCaseId, CurrentPage, 20);

                if (response.Success && response.Data != null)
                {
                    AuditLogs = new ObservableCollection<MedicalCaseAuditLogDto>(response.Data.Logs);
                    TotalCount = response.Data.TotalCount;
                    TotalPages = response.Data.TotalPages;
                    CurrentPage = response.Data.CurrentPage;

                    StatusMessage = TotalCount > 0
                        ? $"共 {TotalCount} 条变更记录"
                        : "暂无变更记录";

                    _logger.LogInformation("加载了医案 {MedicalCaseId} 的 {Count} 条审计日志",
                        _medicalCaseId, AuditLogs.Count);
                }
                else
                {
                    StatusMessage = response.Message ?? "加载变更记录失败";
                    _logger.LogWarning("加载审计日志失败: {Message}", response.Message);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "加载变更记录失败";
                _logger.LogError(ex, "加载医案审计日志失败，医案ID: {MedicalCaseId}", _medicalCaseId);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanPreviousPage() => CurrentPage > 1;

        private void ExecutePreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadAuditLogsAsync();
            }
        }

        private bool CanNextPage() => CurrentPage < TotalPages;

        private void ExecuteNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadAuditLogsAsync();
            }
        }

        private void ExecuteClose()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        #endregion
    }
}
