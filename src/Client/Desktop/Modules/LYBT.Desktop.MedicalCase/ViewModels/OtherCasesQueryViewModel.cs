using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 其他病案查询ViewModel
    /// Issue #1592 - Phase 3
    /// </summary>
    public class OtherCasesQueryViewModel : UnifiedViewModelBase
    {
        #region 字段

        private readonly IMedicalCaseRepository _medicalCaseRepository;

        #endregion

        #region 属性

        private string? _patientName;
        /// <summary>
        /// 患者姓名关键字
        /// </summary>
        public string? PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private DateTime? _startDate;
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime? _endDate;
        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        private string? _diagnosisKeyword;
        /// <summary>
        /// 诊断关键词
        /// </summary>
        public string? DiagnosisKeyword
        {
            get => _diagnosisKeyword;
            set => SetProperty(ref _diagnosisKeyword, value);
        }

        private ObservableCollection<MedicalCaseDto> _queryResults = new();
        /// <summary>
        /// 查询结果列表
        /// </summary>
        public ObservableCollection<MedicalCaseDto> QueryResults
        {
            get => _queryResults;
            set => SetProperty(ref _queryResults, value);
        }

        private MedicalCaseDto? _selectedCase;
        /// <summary>
        /// 当前选中的病案
        /// </summary>
        public MedicalCaseDto? SelectedCase
        {
            get => _selectedCase;
            set
            {
                if (SetProperty(ref _selectedCase, value))
                {
                    ViewDetailCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isQuerying;
        /// <summary>
        /// 是否正在查询
        /// </summary>
        public bool IsQuerying
        {
            get => _isQuerying;
            set => SetProperty(ref _isQuerying, value);
        }

        private bool _hasNoResults;
        /// <summary>
        /// 是否无查询结果
        /// </summary>
        public bool HasNoResults
        {
            get => _hasNoResults;
            set => SetProperty(ref _hasNoResults, value);
        }

        #endregion

        #region 命令

        public DelegateCommand QueryCommand { get; }
        public DelegateCommand ClearCommand { get; }
        public DelegateCommand ViewDetailCommand { get; }

        #endregion

        #region 构造函数

        public OtherCasesQueryViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));

            // 初始化命令
            QueryCommand = new DelegateCommand(ExecuteQueryAsync, CanExecuteQuery);
            ClearCommand = new DelegateCommand(ExecuteClear);
            ViewDetailCommand = new DelegateCommand(ExecuteViewDetail, () => SelectedCase != null);

            // 监听属性变化更新命令状态
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PatientName) ||
                    e.PropertyName == nameof(StartDate) ||
                    e.PropertyName == nameof(EndDate) ||
                    e.PropertyName == nameof(DiagnosisKeyword))
                {
                    QueryCommand.RaiseCanExecuteChanged();
                }
            };
        }

        #endregion

        #region 命令实现

        private bool CanExecuteQuery()
        {
            // 至少需要一个查询条件
            return !string.IsNullOrWhiteSpace(PatientName) ||
                   StartDate.HasValue ||
                   EndDate.HasValue ||
                   !string.IsNullOrWhiteSpace(DiagnosisKeyword);
        }

        private async void ExecuteQueryAsync()
        {
            if (IsQuerying) return;

            // 验证日期范围
            if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
            {
                ShowError("开始日期不能晚于结束日期");
                return;
            }

            IsQuerying = true;
            HasNoResults = false;

            try
            {
                Logger.LogInformation("开始查询病案，条件：患者={PatientName}, 日期={StartDate}~{EndDate}, 诊断={DiagnosisKeyword}",
                    PatientName ?? "无", StartDate, EndDate, DiagnosisKeyword ?? "无");

                var results = await _medicalCaseRepository.QueryAsync(
                    PatientName,
                    StartDate,
                    EndDate,
                    DiagnosisKeyword);

                QueryResults.Clear();
                foreach (var item in results)
                {
                    QueryResults.Add(item);
                }

                HasNoResults = QueryResults.Count == 0;
                Logger.LogInformation("查询完成，共{Count}条结果", QueryResults.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查询病案失败");
                ShowError($"查询失败：{ex.Message}");
            }
            finally
            {
                IsQuerying = false;
            }
        }

        private void ExecuteClear()
        {
            PatientName = null;
            StartDate = null;
            EndDate = null;
            DiagnosisKeyword = null;
            QueryResults.Clear();
            SelectedCase = null;
            HasNoResults = false;
        }

        private void ExecuteViewDetail()
        {
            if (SelectedCase == null) return;

            try
            {
                // 导航到医案详情页
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", SelectedCase.Id }
                };

                RegionManager.RequestNavigate("ContentRegion", "MedicalCaseDetailView", parameters);
                Logger.LogInformation("导航到病案详情页，ID: {Id}", SelectedCase.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航失败");
                ShowError($"打开详情失败：{ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        private void ShowError(string message)
        {
            // TODO: 集成全局消息提示服务
            Logger.LogWarning("错误提示：{Message}", message);
        }

        #endregion
    }
}
