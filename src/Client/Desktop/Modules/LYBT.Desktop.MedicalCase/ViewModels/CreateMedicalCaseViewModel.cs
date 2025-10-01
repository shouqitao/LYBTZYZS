using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 创建医疗案例视图模型 - UltraThink简化架构
    /// </summary>
    public class CreateMedicalCaseViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IMedicalCaseService _medicalCaseService;

        #endregion

        #region 属性

        private string _caseNumber = string.Empty;
        private Guid? _patientId;
        private string _patientName = string.Empty;
        private string _chiefComplaint = string.Empty;
        private string _presentIllnessHistory = string.Empty;
        private string _pastMedicalHistory = string.Empty;
        private CaseStatus _status = CaseStatus.Active;

        /// <summary>
        /// 案例编号
        /// </summary>
        [Required(ErrorMessage = "案例编号不能为空")]
        [StringLength(50, ErrorMessage = "案例编号长度不能超过50个字符")]
        public string CaseNumber
        {
            get => _caseNumber;
            set
            {
                if (SetProperty(ref _caseNumber, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid? PatientId
        {
            get => _patientId;
            set => SetProperty(ref _patientId, value);
        }

        /// <summary>
        /// 患者姓名
        /// </summary>
        [Required(ErrorMessage = "患者姓名不能为空")]
        public string PatientName
        {
            get => _patientName;
            set
            {
                if (SetProperty(ref _patientName, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 主诉
        /// </summary>
        [Required(ErrorMessage = "主诉不能为空")]
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set
            {
                if (SetProperty(ref _chiefComplaint, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 现病史
        /// </summary>
        [StringLength(2000, ErrorMessage = "现病史长度不能超过2000个字符")]
        public string PresentIllnessHistory
        {
            get => _presentIllnessHistory;
            set
            {
                if (SetProperty(ref _presentIllnessHistory, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 既往史
        /// </summary>
        [StringLength(1000, ErrorMessage = "既往史长度不能超过1000个字符")]
        public string PastMedicalHistory
        {
            get => _pastMedicalHistory;
            set
            {
                if (SetProperty(ref _pastMedicalHistory, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 案例状态
        /// </summary>
        public CaseStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 创建命令
        /// </summary>
        public DelegateCommand CreateCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 重置表单命令
        /// </summary>
        public DelegateCommand ResetFormCommand { get; }

        #endregion

        #region 构造函数

        public CreateMedicalCaseViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IMedicalCaseService medicalCaseService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));

            // 初始化命令
            CreateCommand = new DelegateCommand(async () => await CreateAsync(), CanCreate);
            CancelCommand = new DelegateCommand(Cancel);
            ResetFormCommand = new DelegateCommand(ResetForm);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => CreateCommand.RaiseCanExecuteChanged();

            // 生成默认案例编号
            GenerateDefaultCaseNumber();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        private async Task CreateAsync()
        {
            try
            {
                SetIsBusy(true, "正在创建医疗案例...");

                var createDto = new MedicalCaseCreateDto
                {
                    CaseNumber = CaseNumber.Trim(),
                    PatientId = PatientId ?? Guid.Empty,
                    DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                    ChiefComplaint = ChiefComplaint.Trim(),
                    PresentIllnessHistory = string.IsNullOrWhiteSpace(PresentIllnessHistory) ? null : PresentIllnessHistory.Trim(),
                    PastMedicalHistory = string.IsNullOrWhiteSpace(PastMedicalHistory) ? null : PastMedicalHistory.Trim(),
                    Status = (MedicalCaseStatus)Status
                };

                var result = await _medicalCaseService.CreateAsync(createDto);
                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync("医疗案例创建成功");
                    NavigateToMedicalCaseManagement();
                }
                else
                {
                    await ShowErrorMessageAsync($"创建医疗案例失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建医疗案例时发生异常");
                await ShowErrorMessageAsync("创建医疗案例时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 检查是否可以创建
        /// </summary>
        private bool CanCreate()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(CaseNumber) &&
                   !string.IsNullOrWhiteSpace(PatientName) &&
                   !string.IsNullOrWhiteSpace(ChiefComplaint) &&
                   !HasErrors;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            NavigateToMedicalCaseManagement();
        }

        /// <summary>
        /// 重置表单
        /// </summary>
        private void ResetForm()
        {
            CaseNumber = string.Empty;
            PatientId = null;
            PatientName = string.Empty;
            ChiefComplaint = string.Empty;
            PresentIllnessHistory = string.Empty;
            PastMedicalHistory = string.Empty;
            Status = CaseStatus.Active;

            // 清除验证错误
            ClearAllErrors();

            // 重新生成案例编号
            GenerateDefaultCaseNumber();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成默认案例编号
        /// </summary>
        private void GenerateDefaultCaseNumber()
        {
            CaseNumber = $"MC{DateTime.Now:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// 导航到医疗案例管理页面
        /// </summary>
        private void NavigateToMedicalCaseManagement()
        {
            NavigateTo("MainRegion", "MedicalCaseManagementView");
        }

        #endregion
    }
}
