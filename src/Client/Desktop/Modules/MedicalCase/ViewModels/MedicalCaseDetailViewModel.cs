using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例详情视图模型 - UltraThink简化架构
    /// </summary>
    public class MedicalCaseDetailViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IMedicalCaseService _medicalCaseService;

        #endregion

        #region 属性

        private MedicalCaseDto? _medicalCase;
        private string _caseNumber = string.Empty;
        private string _patientName = string.Empty;
        private string _chiefComplaint = string.Empty;
        private CaseStatus _status = CaseStatus.Active;

        /// <summary>
        /// 医疗案例
        /// </summary>
        public MedicalCaseDto? MedicalCase
        {
            get => _medicalCase;
            set => SetProperty(ref _medicalCase, value);
        }

        /// <summary>
        /// 案例编号
        /// </summary>
        [Required(ErrorMessage = "案例编号不能为空")]
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
        /// 保存命令
        /// </summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseDetailViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IMedicalCaseService medicalCaseService,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelCommand = new DelegateCommand(Cancel);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => SaveCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 保存
        /// </summary>
        /// <summary>
        /// 保存
        /// </summary>
        /// <summary>
        /// 保存
        /// </summary>
        private async Task SaveAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存医疗案例...");

                if (MedicalCase == null)
                {
                    await ShowErrorMessageAsync("医疗案例数据无效");
                    return;
                }

                // 更新案例信息
                MedicalCase.CaseNumber = CaseNumber.Trim();
                MedicalCase.ChiefComplaint = ChiefComplaint.Trim();

                // 创建MedicalCaseUpdateDto对象 - 使用正确的属性结构
                var updateDto = new MedicalCaseUpdateDto
                {
                    Id = MedicalCase.Id,
                    PatientId = MedicalCase.PatientId,
                    DoctorId = MedicalCase.DoctorId,
                    Remark = MedicalCase.Remark,
                    ChiefComplaint = MedicalCase.ChiefComplaint,
                    PresentIllness = MedicalCase.ChiefComplaint, // 使用主诉作为现病史
                    PhysicalExamination = "",
                    AuxiliaryExamination = "",
                    PrescriptionInfo = "",
                    FollowUpPlan = ""
                };

                var result = await _medicalCaseService.UpdateAsync(MedicalCase.Id, updateDto);
                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync("医疗案例保存成功");
                    // 修复NavigateBack调用 - 提供区域名称
                    RegionManager.RequestNavigate("ContentRegion", "MedicalCaseListView");
                }
                else
                {
                    await ShowErrorMessageAsync($"保存失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存医疗案例时发生异常");
                await ShowErrorMessageAsync("保存医疗案例时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 检查是否可以保存
        /// </summary>
        private bool CanSave()
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
            NavigateBack("ContentRegion");
        }

        #endregion

        #region 导航方法

        /// <summary>
        /// 导航参数处理
        /// </summary>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            if (navigationContext.Parameters.TryGetValue("MedicalCase", out MedicalCaseDto medicalCase))
            {
                LoadMedicalCase(medicalCase);
            }
        }

        /// <summary>
        /// 加载医疗案例
        /// </summary>
        private void LoadMedicalCase(MedicalCaseDto medicalCase)
        {
            MedicalCase = medicalCase;
            CaseNumber = medicalCase.CaseNumber ?? string.Empty;
            PatientName = medicalCase.PatientName ?? string.Empty;
            ChiefComplaint = medicalCase.ChiefComplaint ?? string.Empty;
            Status = (CaseStatus)medicalCase.CaseStatus;
        }

        #endregion
    }
}