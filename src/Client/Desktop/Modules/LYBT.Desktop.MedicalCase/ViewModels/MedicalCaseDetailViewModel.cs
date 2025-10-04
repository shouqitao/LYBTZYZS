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

        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand BackCommand { get; }

        /// <summary>
        /// 关闭命令（别名）
        /// </summary>
        public DelegateCommand CloseCommand { get; }

        /// <summary>
        /// 编辑命令
        /// </summary>
        public DelegateCommand EditCommand { get; }

        /// <summary>
        /// 打印命令
        /// </summary>
        public DelegateCommand PrintCommand { get; }

        /// <summary>
        /// 打印处方命令
        /// </summary>
        public DelegateCommand PrintPrescriptionCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        /// <summary>
        /// 开始诊疗命令
        /// </summary>
        public DelegateCommand StartConsultationCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseDetailViewModel(
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
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelCommand = new DelegateCommand(Cancel);

            // 初始化新增命令
            BackCommand = new DelegateCommand(ExecuteBack);
            CloseCommand = BackCommand; // 别名
            EditCommand = new DelegateCommand(ExecuteEdit, CanEdit);
            PrintCommand = new DelegateCommand(ExecutePrint, CanPrint);
            PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription, CanPrintPrescription);
            RefreshCommand = new DelegateCommand(async () => await ExecuteRefreshAsync());
            StartConsultationCommand = new DelegateCommand(ExecuteStartConsultation, CanStartConsultation);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => UpdateCommandStates();
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

        /// <summary>
        /// 返回列表
        /// </summary>
        private void ExecuteBack()
        {
            NavigateTo("MainRegion", "MedicalCaseListView");
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        private void ExecuteEdit()
        {
            try
            {
                Logger.LogInformation("编辑病历功能开发中");
                ShowInfoMessage("编辑病历功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "编辑病历时发生异常");
                ShowErrorMessage("编辑病历失败");
            }
        }

        /// <summary>
        /// 检查是否可以编辑
        /// </summary>
        private bool CanEdit()
        {
            return !IsBusy && MedicalCase != null;
        }

        /// <summary>
        /// 打印病历
        /// </summary>
        private void ExecutePrint()
        {
            try
            {
                Logger.LogInformation("打印病历: {MedicalCaseId}", MedicalCase?.Id);
                ShowInfoMessage("打印病历功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打印病历时发生异常");
                ShowErrorMessage("打印病历失败");
            }
        }

        /// <summary>
        /// 检查是否可以打印
        /// </summary>
        private bool CanPrint()
        {
            return !IsBusy && MedicalCase != null;
        }

        /// <summary>
        /// 打印处方
        /// </summary>
        private void ExecutePrintPrescription()
        {
            try
            {
                Logger.LogInformation("打印处方: {MedicalCaseId}", MedicalCase?.Id);
                ShowInfoMessage("打印处方功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打印处方时发生异常");
                ShowErrorMessage("打印处方失败");
            }
        }

        /// <summary>
        /// 检查是否可以打印处方
        /// </summary>
        private bool CanPrintPrescription()
        {
            return !IsBusy && MedicalCase != null;
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        private async Task ExecuteRefreshAsync()
        {
            try
            {
                SetIsBusy(true, "正在刷新数据...");

                if (MedicalCase == null)
                {
                    await ShowErrorMessageAsync("无法刷新：病历数据无效");
                    return;
                }

                // 重新加载病历数据
                var result = await _medicalCaseService.GetByIdAsync(MedicalCase.Id);
                if (result.IsSuccess && result.Data != null)
                {
                    LoadMedicalCase(result.Data);
                    await ShowSuccessMessageAsync("数据刷新成功");
                }
                else
                {
                    await ShowErrorMessageAsync($"刷新失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "刷新病历数据时发生异常");
                await ShowErrorMessageAsync("刷新数据时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 开始诊疗
        /// </summary>
        private void ExecuteStartConsultation()
        {
            try
            {
                if (MedicalCase == null)
                {
                    ShowErrorMessage("无法开始诊疗：病历数据无效");
                    return;
                }

                Logger.LogInformation("开始诊疗: {MedicalCaseId}", MedicalCase.Id);

                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", MedicalCase.Id }
                };

                NavigateTo("MainRegion", "ConsultationMainView", parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始诊疗时发生异常");
                ShowErrorMessage("开始诊疗失败，请稍后重试");
            }
        }

        /// <summary>
        /// 检查是否可以开始诊疗
        /// </summary>
        private bool CanStartConsultation()
        {
            return !IsBusy && MedicalCase != null;
        }

        /// <summary>
        /// 更新命令状态
        /// </summary>
        private void UpdateCommandStates()
        {
            SaveCommand.RaiseCanExecuteChanged();
            EditCommand.RaiseCanExecuteChanged();
            PrintCommand.RaiseCanExecuteChanged();
            PrintPrescriptionCommand.RaiseCanExecuteChanged();
            StartConsultationCommand.RaiseCanExecuteChanged();
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
