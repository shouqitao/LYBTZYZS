using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Navigation.Regions;
using Prism.Dialogs;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.WPF.Client.Core.Extensions;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels.Components
{
    /// <summary>
    /// 接待工作流协调器 - UltraThink专门化组件
    /// 职责单一：专注患者接待工作流程的协调和执行
    /// 代码干净：清晰的工作流步骤和状态管理
    /// 性能出色：优化的异步流程和资源管理
    /// </summary>
    public class ReceptionWorkflowCoordinator
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IDialogService _dialogService;
        private readonly IDialogService _prismDialogService;
        private readonly IRegionManager _regionManager;
        private readonly ILogger<ReceptionWorkflowCoordinator> _logger;

        // 关联的组件
        private PatientDataManager? _dataManager;
        private PatientSearchService? _searchService;
        private ReceptionValidationService? _validationService;

        public ReceptionWorkflowCoordinator(
            IMedicalCaseService medicalCaseService,
            IUserSessionManager userSessionManager,
            IDialogService dialogService,
            IDialogService prismDialogService,
            IRegionManager regionManager,
            ILogger<ReceptionWorkflowCoordinator> logger)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _userSessionManager = userSessionManager ?? throw new ArgumentNullException(nameof(userSessionManager));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 依赖注入

        /// <summary>
        /// 设置关联组件依赖
        /// </summary>
        public void SetDependencies(
            PatientDataManager dataManager,
            PatientSearchService searchService,
            ReceptionValidationService validationService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        #endregion

        #region 快速接待工作流

        /// <summary>
        /// 执行快速接待工作流
        /// </summary>
        public async Task<ReceptionResult> ExecuteQuickReceptionAsync()
        {
            if (_dataManager == null || _searchService == null || _validationService == null)
            {
                return new ReceptionResult 
                { 
                    IsSuccess = false, 
                    Message = "工作流组件未正确初始化" 
                };
            }

            var result = new ReceptionResult();

            try
            {
                _logger.LogInformation("开始快速接待工作流");

                // 步骤1：验证表单
                var validationResult = _validationService.ValidateQuickReceptionForm();
                if (!validationResult.IsValid)
                {
                    result.IsSuccess = false;
                    result.Message = validationResult.ErrorMessage;
                    return result;
                }

                _dataManager.SetLoadingState(true);

                // 步骤2：获取或创建患者
                var patient = await GetOrCreatePatientAsync();
                if (patient == null)
                {
                    result.IsSuccess = false;
                    result.Message = "获取或创建患者失败";
                    return result;
                }

                // 步骤3：创建医疗案例
                var medicalCaseResult = await CreateMedicalCaseAsync(patient.Id);
                if (medicalCaseResult == null || !medicalCaseResult.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.Message = medicalCaseResult?.Message ?? "创建医疗案例失败";
                    return result;
                }

                // 步骤4：显示成功消息
                await _dialogService.ShowSuccessAsync(
                    $"患者 {patient.Name} 接待成功，医疗案例已创建", 
                    "接待成功");

                // 步骤5：询问是否开始看诊
                var startConsultation = await _dialogService.ShowConfirmAsync(
                    "是否立即开始看诊？", 
                    "开始看诊");

                result.IsSuccess = true;
                result.Message = "快速接待完成";
                result.Patient = patient;
                result.MedicalCase = medicalCaseResult.MedicalCase;
                result.ShouldStartConsultation = startConsultation;

                _logger.LogInformation("快速接待工作流完成：患者 {PatientName}", patient.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速接待工作流失败");
                result.IsSuccess = false;
                result.Message = $"接待失败：{ex.Message}";
            }
            finally
            {
                _dataManager?.SetLoadingState(false);
            }

            return result;
        }

        /// <summary>
        /// 获取或创建患者
        /// </summary>
        private async Task<PatientDetailDto?> GetOrCreatePatientAsync()
        {
            if (_dataManager == null || _searchService == null)
                return null;

            try
            {
                // 如果选中了现有患者，使用现有患者
                if (_dataManager.SelectedPatient != null)
                {
                    _logger.LogDebug("使用选中的现有患者：{PatientName}", _dataManager.SelectedPatient.Name);
                    return _dataManager.PatientDetails ?? new PatientDetailDto 
                    { 
                        Id = _dataManager.SelectedPatient.Id,
                        Name = _dataManager.SelectedPatient.Name
                    };
                }

                // 否则创建新患者
                var newPatientDto = CreatePatientDtoFromForm();
                if (newPatientDto == null)
                    return null;

                var createResult = await _searchService.FindOrCreatePatientAsync(newPatientDto);
                
                if (createResult.IsSuccess && createResult.Patient != null)
                {
                    _dataManager.IsNewPatient = createResult.IsNewPatient;
                    _logger.LogInformation("患者处理成功：{PatientName}, 新患者：{IsNew}", 
                        createResult.Patient.Name, createResult.IsNewPatient);
                    return createResult.Patient;
                }

                await _dialogService.ShowErrorAsync(
                    createResult.Message ?? "创建患者失败", 
                    "错误");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取或创建患者失败");
                throw;
            }
        }

        /// <summary>
        /// 从表单创建患者DTO
        /// </summary>
        private PatientDetailDto? CreatePatientDtoFromForm()
        {
            if (_dataManager == null)
                return null;

            try
            {
                var dto = new PatientDetailDto
                {
                    Name = _dataManager.PatientName.Trim(),
                    Gender = Enum.TryParse<Gender>(_dataManager.PatientGender, out var gender) ? gender : Gender.Unknown,
                    PhoneNumber = _dataManager.PatientPhone.Trim(),
                    IDNumber = string.IsNullOrWhiteSpace(_dataManager.PatientIdCard) ? null : _dataManager.PatientIdCard.Trim()
                };

                // 计算出生日期（如果提供了年龄）
                if (!string.IsNullOrWhiteSpace(_dataManager.PatientAge) && 
                    int.TryParse(_dataManager.PatientAge, out var age))
                {
                    dto.BirthDate = DateTime.Today.AddYears(-age);
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从表单创建患者DTO失败");
                return null;
            }
        }

        #endregion

        #region 医疗案例管理

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        public async Task<MedicalCaseCreateResult> CreateMedicalCaseAsync(Guid patientId)
        {
            var result = new MedicalCaseCreateResult();

            try
            {
                _logger.LogDebug("开始创建医疗案例：患者ID {PatientId}", patientId);

                var createDto = new MedicalCaseCreateDto
                {
                    PatientId = patientId,
                    DoctorId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty,
                    Remark = "患者接待创建"
                };

                var serviceResult = await _medicalCaseService.CreateAsync(createDto);
                
                if (serviceResult.IsSuccess && serviceResult.Data != null)
                {
                    result.IsSuccess = true;
                    result.MedicalCase = serviceResult.Data as MedicalCaseInfo;
                    result.Message = "医疗案例创建成功";

                    _logger.LogInformation("医疗案例创建成功：{MedicalCaseId}", result.MedicalCase?.Id);
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = serviceResult.ErrorMessage ?? "创建医疗案例失败";
                    
                    await _dialogService.ShowErrorAsync(result.Message, "错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败：患者ID {PatientId}", patientId);
                result.IsSuccess = false;
                result.Message = $"创建失败：{ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 为选中患者创建医疗案例
        /// </summary>
        public async Task<MedicalCaseCreateResult> CreateMedicalCaseForSelectedPatientAsync()
        {
            if (_dataManager?.SelectedPatient == null)
            {
                return new MedicalCaseCreateResult
                {
                    IsSuccess = false,
                    Message = "未选中患者"
                };
            }

            var result = await CreateMedicalCaseAsync(_dataManager.SelectedPatient.Id);
            
            if (result.IsSuccess && result.MedicalCase != null)
            {
                await _dialogService.ShowSuccessAsync("医疗案例创建成功", "成功");
                
                // 刷新患者的医疗案例列表
                if (_searchService != null)
                {
                    var casesResult = await _searchService.LoadPatientMedicalCasesAsync(_dataManager.SelectedPatient.Id);
                    if (casesResult.IsSuccess)
                    {
                        _dataManager.SetRecentCases(casesResult.MedicalCases);
                    }
                }
            }

            return result;
        }

        #endregion

        #region 导航和界面操作

        /// <summary>
        /// 导航到看诊界面
        /// </summary>
        public void NavigateToConsultation(MedicalCaseInfo medicalCase)
        {
            try
            {
                _logger.LogInformation("导航到看诊界面：医疗案例 {MedicalCaseId}", medicalCase.Id);

                // 使用字符串参数方式导航 - Prism 9兼容
                _regionManager.RequestNavigate("MainContentRegion", 
                    $"ConsultationMainView?MedicalCaseId={medicalCase.Id}&PatientId={medicalCase.PatientId}&ConsultationMode=Start");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到看诊界面失败");
            }
        }

        /// <summary>
        /// 启动看诊流程
        /// </summary>
        public async Task StartConsultationAsync(MedicalCaseInfo medicalCase)
        {
            try
            {
                _logger.LogInformation("启动看诊流程：医疗案例 {MedicalCaseId}", medicalCase.Id);
                NavigateToConsultation(medicalCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动看诊失败：医疗案例 {MedicalCaseId}", medicalCase.Id);
                await _dialogService.ShowErrorAsync($"启动看诊失败：{ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 打开新患者注册对话框
        /// </summary>
        public void ShowNewPatientRegistrationDialog()
        {
            try
            {
                var parameters = new DialogParameters();
                
                _prismDialogService.ShowDialog("AddPatientDialog", parameters, async result =>
                {
                    if (result.Result == ButtonResult.OK && _searchService != null && _dataManager != null)
                    {
                        // 刷新搜索结果
                        var searchResult = await _searchService.SearchPatientsAsync(_dataManager.SearchKeyword);
                        if (searchResult.IsSuccess)
                        {
                            _dataManager.SetSearchResults(searchResult.Patients);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开新患者注册对话框失败");
            }
        }

        /// <summary>
        /// 查看患者详情
        /// </summary>
        public void ViewPatientDetails(Guid patientId)
        {
            try
            {
                _logger.LogDebug("查看患者详情：{PatientId}", patientId);
                
                // 使用字符串参数方式导航 - Prism 9兼容
                _regionManager.RequestNavigate("MainContentRegion", $"PatientDetailView?PatientId={patientId}&ViewMode=Detail");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查看患者详情失败：{PatientId}", patientId);
            }
        }

        #endregion

        #region 数据刷新

        /// <summary>
        /// 刷新接待数据
        /// </summary>
        public async Task RefreshReceptionDataAsync()
        {
            if (_searchService == null || _dataManager == null)
                return;

            try
            {
                _logger.LogDebug("开始刷新接待数据");

                // 加载今日患者数据
                var todayResult = await _searchService.LoadTodayPatientsAsync();
                if (todayResult.IsSuccess)
                {
                    _dataManager.SetRecentCases(todayResult.RecentCases);
                }

                // 如果有选中患者，刷新其医疗案例
                if (_dataManager.SelectedPatient != null)
                {
                    var casesResult = await _searchService.LoadPatientMedicalCasesAsync(_dataManager.SelectedPatient.Id);
                    if (casesResult.IsSuccess)
                    {
                        _dataManager.SetRecentCases(casesResult.MedicalCases);
                    }
                }

                _logger.LogDebug("接待数据刷新完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新接待数据失败");
            }
        }

        #endregion

        #region 结果类定义

        public class ReceptionResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public PatientDetailDto? Patient { get; set; }
            public MedicalCaseInfo? MedicalCase { get; set; }
            public bool ShouldStartConsultation { get; set; }
        }

        public class MedicalCaseCreateResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public MedicalCaseInfo? MedicalCase { get; set; }
        }

        #endregion
    }
}