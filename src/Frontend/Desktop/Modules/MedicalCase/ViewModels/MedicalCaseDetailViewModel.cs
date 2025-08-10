using System;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.WPF.Client.Core.ViewModels;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Navigation.Regions;
using Prism.Events;
using System.Collections.ObjectModel;

using Prism.Dialogs;
using LYBT.WPF.Client.Core.Extensions;
namespace LYBT.WPF.Client.Modules.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例详情视图模型
    /// </summary>
    public class MedicalCaseDetailViewModel : BaseViewModel, INavigationAware
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IDialogService _dialogService;
        private readonly IRegionManager _regionManager;

        #region 属性

        private Guid _medicalCaseId;
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private MedicalCaseDetailDto? _medicalCase;
        public MedicalCaseDetailDto? MedicalCase
        {
            get => _medicalCase;
            set => SetProperty(ref _medicalCase, value);
        }

        private bool _isLoading;
        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isReadOnly = true;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        // 患者信息
        private string _patientName = string.Empty;
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private string _patientPhone = string.Empty;
        public string PatientPhone
        {
            get => _patientPhone;
            set => SetProperty(ref _patientPhone, value);
        }

        // 案例基本信息
        private string _caseNumber = string.Empty;
        public string CaseNumber
        {
            get => _caseNumber;
            set => SetProperty(ref _caseNumber, value);
        }

        private string _chiefComplaint = string.Empty;
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }

        private string _currentIllnessHistory = string.Empty;
        public string CurrentIllnessHistory
        {
            get => _currentIllnessHistory;
            set => SetProperty(ref _currentIllnessHistory, value);
        }

        private string _pastMedicalHistory = string.Empty;
        public string PastMedicalHistory
        {
            get => _pastMedicalHistory;
            set => SetProperty(ref _pastMedicalHistory, value);
        }

        private string _physicalExamination = string.Empty;
        public string PhysicalExamination
        {
            get => _physicalExamination;
            set => SetProperty(ref _physicalExamination, value);
        }

        private string _auxiliaryExamination = string.Empty;
        public string AuxiliaryExamination
        {
            get => _auxiliaryExamination;
            set => SetProperty(ref _auxiliaryExamination, value);
        }

        private string _diagnosisSummary = string.Empty;
        public string DiagnosisSummary
        {
            get => _diagnosisSummary;
            set => SetProperty(ref _diagnosisSummary, value);
        }

        private string _treatmentPlan = string.Empty;
        public string TreatmentPlan
        {
            get => _treatmentPlan;
            set => SetProperty(ref _treatmentPlan, value);
        }

        private string _clinicalNotes = string.Empty;
        public string ClinicalNotes
        {
            get => _clinicalNotes;
            set => SetProperty(ref _clinicalNotes, value);
        }

        private MedicalCaseStatus _status;
        public MedicalCaseStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private DateTime _createTime;
        public DateTime CreateTime
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        private DateTime? _completeTime;
        public DateTime? CompleteTime
        {
            get => _completeTime;
            set => SetProperty(ref _completeTime, value);
        }

        private string _doctorName = string.Empty;
        public string DoctorName
        {
            get => _doctorName;
            set => SetProperty(ref _doctorName, value);
        }

        #endregion

        #region 命令

        public DelegateCommand LoadDataCommand { get; }
        public DelegateCommand BackCommand { get; }
        public DelegateCommand StartConsultationCommand { get; }
        public DelegateCommand CompleteCaseCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand CancelEditCommand { get; }
        public DelegateCommand PrintCommand { get; }

        #endregion

        public MedicalCaseDetailViewModel(
            IMedicalCaseService medicalCaseService,
            IDialogService dialogService,
            IRegionManager regionManager,
            IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            _medicalCaseService = medicalCaseService;
            _dialogService = dialogService;
            _regionManager = regionManager;

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            BackCommand = new DelegateCommand(NavigateBack);
            StartConsultationCommand = new DelegateCommand(async () => await StartConsultationAsync(), CanStartConsultation);
            CompleteCaseCommand = new DelegateCommand(async () => await CompleteCaseAsync(), CanCompleteCase);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsReadOnly);
            EditCommand = new DelegateCommand(EnableEdit, () => IsReadOnly);
            CancelEditCommand = new DelegateCommand(CancelEdit, () => !IsReadOnly);
            PrintCommand = new DelegateCommand(async () => await PrintCaseAsync());

            // 监听属性变化以更新命令状态
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsReadOnly) || e.PropertyName == nameof(Status))
                {
                    StartConsultationCommand.RaiseCanExecuteChanged();
                    CompleteCaseCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();
                    EditCommand.RaiseCanExecuteChanged();
                    CancelEditCommand.RaiseCanExecuteChanged();
                }
            };
        }

        #region 导航实现

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
            {
                MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                IsReadOnly = !navigationContext.Parameters.GetValue<bool>("EditMode");
                LoadDataCommand.Execute();
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
            {
                var id = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                return MedicalCaseId == id;
            }
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 清理资源
        }

        #endregion

        #region 私有方法

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // 加载医疗案例详情
                var result = await _medicalCaseService.GetByIdAsync(MedicalCaseId);
                if (result.IsSuccess && result.Data != null)
                {
                    MedicalCase = result.Data;
                    
                    // 映射到UI属性
                    PatientName = result.Data.PatientName ?? "";
                    CaseNumber = $"MC{result.Data.Id.ToString().Substring(0, 8).ToUpper()}";
                    // 以下属性在后端DTO中不存在，暂时设为空值
                    ChiefComplaint = "";
                    CurrentIllnessHistory = "";
                    PastMedicalHistory = "";
                    PhysicalExamination = "";
                    AuxiliaryExamination = "";
                    DiagnosisSummary = "";
                    TreatmentPlan = "";
                    ClinicalNotes = "";
                    DoctorName = "";
                    
                    // 解析状态
                    Status = ParseStatus(result.Data.Status);
                    StatusText = GetStatusText(Status);
                    
                    CreateTime = result.Data.CreateTime;
                    CompleteTime = result.Data.CompleteTime;
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"加载医疗案例失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"加载数据时发生错误: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateBack()
        {
            _regionManager.RequestNavigate("MainContentRegion", "MedicalCaseListView");
        }

        private async Task StartConsultationAsync()
        {
            if (MedicalCase == null) return;

            try
            {
                // 更新状态为看诊中
                var result = await _medicalCaseService.UpdateStatusAsync(MedicalCase.Id, MedicalCaseStatus.InConsultation);
                
                if (result.IsSuccess)
                {
                    // 导航到看诊界面 - 使用字符串参数方式
                    _regionManager.RequestNavigate("MainContentRegion", $"ConsultationMainView?MedicalCaseId={MedicalCase.Id}&PatientId={MedicalCase.PatientId}&ConsultationMode=Start");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "无法开始看诊", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"开始看诊失败: {ex.Message}", "错误");
            }
        }

        private async Task CompleteCaseAsync()
        {
            if (MedicalCase == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                "确定要完成该医疗案例吗？完成后将无法继续编辑。",
                "确认完成");

            if (!confirm) return;

            try
            {
                IsLoading = true;
                var result = await _medicalCaseService.UpdateStatusAsync(MedicalCase.Id, MedicalCaseStatus.Completed);
                
                if (result.IsSuccess)
                {
                    await _dialogService.ShowSuccessAsync("医疗案例已完成", "操作成功");
                    await LoadDataAsync();
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"完成失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"操作失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveAsync()
        {
            if (MedicalCase == null) return;

            try
            {
                IsLoading = true;

                var editDto = new MedicalCaseEditDto
                {
                    Id = MedicalCase.Id,
                    // 只包含后端DTO实际支持的属性
                    Remark = string.IsNullOrWhiteSpace(MedicalCase.Remark) ? null : MedicalCase.Remark.Trim(),
                    Status = Status.ToString()
                };

                var result = await _medicalCaseService.UpdateAsync(editDto);
                
                if (result.IsSuccess)
                {
                    await _dialogService.ShowSuccessAsync("保存成功", "操作完成");
                    IsReadOnly = true;
                    await LoadDataAsync();
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"保存失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"保存失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void EnableEdit()
        {
            IsReadOnly = false;
        }

        private void CancelEdit()
        {
            IsReadOnly = true;
            LoadDataCommand.Execute();
        }

        private async Task PrintCaseAsync()
        {
            // TODO: 实现打印功能
            await _dialogService.ShowInformationAsync("打印功能开发中", "提示");
        }

        private bool CanStartConsultation()
        {
            return Status == MedicalCaseStatus.Registered;
        }

        private bool CanCompleteCase()
        {
            return Status == MedicalCaseStatus.InConsultation;
        }

        private static MedicalCaseStatus ParseStatus(string? status)
        {
            return status?.ToLower() switch
            {
                "registered" or "已挂号" => MedicalCaseStatus.Registered,
                "inconsultation" or "看诊中" => MedicalCaseStatus.InConsultation,
                "completed" or "已完成" => MedicalCaseStatus.Completed,
                "cancelled" or "已取消" => MedicalCaseStatus.Cancelled,
                _ => MedicalCaseStatus.Registered
            };
        }

        private static string GetStatusText(MedicalCaseStatus status)
        {
            return status switch
            {
                MedicalCaseStatus.Registered => "已挂号",
                MedicalCaseStatus.InConsultation => "看诊中",
                MedicalCaseStatus.Completed => "已完成",
                MedicalCaseStatus.Cancelled => "已取消",
                _ => "未知"
            };
        }

        #endregion
    }
}