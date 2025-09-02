using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 看诊主界面视图模型 - 简化版纯数据记录
    /// 只负责简单的四诊数据录入，不包含流程监管和智能处理
    /// </summary>
    public class ConsultationMainViewModel : SessionAwareViewModel, INavigationAware
    {
        #region 服务依赖

        private readonly IConsultationService _consultationService;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IPatientService _patientService;

        #endregion

        #region 基本属性

        private string _title = "看诊记录";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private ObservableCollection<PatientDto> _patients = new();
        public ObservableCollection<PatientDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientDto? _selectedPatient;
        public PatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        private ConsultationDto _consultation = new();
        public ConsultationDto Consultation
        {
            get => _consultation;
            set => SetProperty(ref _consultation, value);
        }

        private Guid? _medicalCaseId;
        public Guid? MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 命令

        public ICommand LoadPatientsCommand { get; }
        public ICommand SaveConsultationCommand { get; }
        public ICommand ClearDataCommand { get; }

        #endregion

        #region 构造函数

        public ConsultationMainViewModel(
            IConsultationService consultationService,
            IMedicalCaseService medicalCaseService,
            IPatientService patientService,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<ConsultationMainViewModel> logger)
            : base(sessionManager, notificationService, logger)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

            LoadPatientsCommand = new DelegateCommand(async () => await LoadPatientsAsync());
            SaveConsultationCommand = new DelegateCommand(async () => await SaveConsultationAsync());
            ClearDataCommand = new DelegateCommand(ClearData);

            // ✅ 修复: 使用Task.Run等待初始化，防止fire-and-forget
            _ = Task.Run(async () => await InitializeAsync());
        }

        #endregion

        #region 初始化

        private async Task InitializeAsync()
        {
            try
            {
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                LogError(ex, "初始化失败");
                // 可以考虑显示用户友好的错误消息
                ShowError("系统初始化失败，请稍后重试");
            }
        }

        #endregion

        #region 数据加载

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                // 使用分页查询获取患者列表
                var query = new LYBT.Shared.Models.Contracts.Patients.PatientPagedQueryDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = ""
                };
                
                var result = await _patientService.GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    Patients.Clear();
                    foreach (var patient in result.Data.Items)
                    {
                        Patients.Add(patient);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region 数据保存

        private async Task SaveConsultationAsync()
        {
            try
            {
                if (SelectedPatient == null)
                {
                    ShowWarning("请先选择患者");
                    return;
                }

                IsLoading = true;

                // 设置基本信息
                Consultation.PatientId = SelectedPatient.Id;
                Consultation.DoctorId = CurrentUser?.Id ?? Guid.Empty;
                Consultation.MedicalCaseId = MedicalCaseId ?? Guid.NewGuid();
                Consultation.ConsultationTime = DateTime.Now;
                Consultation.DoctorName = CurrentUser?.RealName ?? "";

                var createDto = new ConsultationStartDto
                {
                    PatientId = SelectedPatient.Id,
                    DoctorId = CurrentUser?.Id ?? Guid.Empty,
                    MedicalCaseId = MedicalCaseId ?? Guid.NewGuid(),
                    EstimatedDuration = 30,
                    ConsultationType = "门诊",
                    Remark = $"患者：{SelectedPatient.Name}，医生：{CurrentUser?.RealName ?? ""}"
                };

                var result = await _consultationService.StartAsync(createDto);
                if (result.IsSuccess && result.Data != null)
                {
                    Consultation = result.Data;
                    ShowSuccess("看诊记录保存成功");
                }
                else
                {
                    ShowError($"保存失败: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "保存看诊记录失败");
                ShowError("保存失败，请重试");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region 数据清理

        private void ClearData()
        {
            Consultation = new ConsultationDto();
            SelectedPatient = null;
            MedicalCaseId = null;
        }

        #endregion

        #region 导航接口实现

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters["MedicalCaseId"] is Guid caseId)
            {
                MedicalCaseId = caseId;
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        #endregion
    }
}