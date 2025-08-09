using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;
using LYBT.WPF.Client.Core.Extensions;
using Prism.Navigation.Regions;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels
{
    /// <summary>
    /// 患者接待视图模型 - 整合原Registration（挂号）功能
    /// 提供快速接待、患者搜索、创建医疗案例等功能
    /// </summary>
    public class PatientReceptionViewModel : BindableBase, INavigationAware
    {
        #region 依赖服务

        private readonly IPatientService _patientService;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IDialogService _dialogService;
        private readonly IDialogService _prismDialogService;
        private readonly IRegionManager _regionManager;
        private readonly ILogger<PatientReceptionViewModel> _logger;

        #endregion

        #region 属性

        private string _title = "患者接待";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _searchKeyword = "";
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // 实时搜索
                    if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                    {
                        _ = SearchPatientsAsync();
                    }
                }
            }
        }

        private ObservableCollection<PatientInfo> _searchResults = new();
        public ObservableCollection<PatientInfo> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        private PatientInfo? _selectedPatient;
        public PatientInfo? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    // 选中患者后加载详细信息
                    if (value != null)
                    {
                        _ = LoadPatientDetailsAsync(value.Id);
                    }
                }
            }
        }

        private PatientDetailDto? _patientDetails;
        public PatientDetailDto? PatientDetails
        {
            get => _patientDetails;
            set => SetProperty(ref _patientDetails, value);
        }

        private ObservableCollection<MedicalCaseInfo> _recentCases = new();
        public ObservableCollection<MedicalCaseInfo> RecentCases
        {
            get => _recentCases;
            set => SetProperty(ref _recentCases, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isNewPatient;
        public bool IsNewPatient
        {
            get => _isNewPatient;
            set => SetProperty(ref _isNewPatient, value);
        }

        // 快速接待表单字段
        private string _patientName = "";
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private string _patientGender = "男";
        public string PatientGender
        {
            get => _patientGender;
            set => SetProperty(ref _patientGender, value);
        }

        private string _patientAge = "";
        public string PatientAge
        {
            get => _patientAge;
            set => SetProperty(ref _patientAge, value);
        }

        private string _patientPhone = "";
        public string PatientPhone
        {
            get => _patientPhone;
            set => SetProperty(ref _patientPhone, value);
        }

        private string _patientIdCard = "";
        public string PatientIdCard
        {
            get => _patientIdCard;
            set => SetProperty(ref _patientIdCard, value);
        }

        private string _chiefComplaint = "";
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }

        #endregion

        #region 命令

        public ICommand SearchCommand { get; }
        public ICommand QuickReceptionCommand { get; }
        public ICommand CreateMedicalCaseCommand { get; }
        public ICommand StartConsultationCommand { get; }
        public ICommand ViewPatientDetailsCommand { get; }
        public ICommand RegisterNewPatientCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region 构造函数

        public PatientReceptionViewModel(
            IPatientService patientService,
            IMedicalCaseService medicalCaseService,
            IUserSessionManager userSessionManager,
            IDialogService dialogService,
            IDialogService prismDialogService,
            IRegionManager regionManager,
            ILogger<PatientReceptionViewModel> logger)
        {
            _patientService = patientService;
            _medicalCaseService = medicalCaseService;
            _userSessionManager = userSessionManager;
            _dialogService = dialogService;
            _prismDialogService = prismDialogService;
            _regionManager = regionManager;
            _logger = logger;

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchPatientsAsync());
            QuickReceptionCommand = new DelegateCommand(async () => await QuickReceptionAsync(), CanQuickReception);
            CreateMedicalCaseCommand = new DelegateCommand(async () => await CreateMedicalCaseAsync(), () => SelectedPatient != null);
            StartConsultationCommand = new DelegateCommand<MedicalCaseInfo>(async mc => await StartConsultationAsync(mc));
            ViewPatientDetailsCommand = new DelegateCommand<PatientInfo>(ViewPatientDetails);
            RegisterNewPatientCommand = new DelegateCommand(async () => await RegisterNewPatientAsync());
            ClearFormCommand = new DelegateCommand(ClearForm);
            RefreshCommand = new DelegateCommand(async () => await RefreshDataAsync());

            // 初始化数据
            _ = InitializeAsync();
        }

        #endregion

        #region 初始化

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                
                // 加载今日接待的患者
                await LoadTodayPatientsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化患者接待模块失败");
                await _dialogService.ShowErrorAsync("初始化失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadTodayPatientsAsync()
        {
            try
            {
                // 获取今日的医疗案例
                var result = await _medicalCaseService.GetPagedAsync(1, 20);

                if (result != null && result.Items != null && string.IsNullOrEmpty(result.ErrorMessage))
                {
                    RecentCases = new ObservableCollection<MedicalCaseInfo>(
                        result.Items.OrderByDescending(c => c.CreateTime)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载今日患者失败");
            }
        }

        #endregion

        #region 患者搜索

        private async Task SearchPatientsAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                SearchResults.Clear();
                return;
            }

            try
            {
                IsLoading = true;

                var result = await _patientService.QuickSearchAsync(SearchKeyword);
                
                if (result.IsSuccess && result.Data != null)
                {
                    SearchResults = new ObservableCollection<PatientInfo>(
                        result.Data.Select(dto => new PatientInfo
                        {
                            Id = dto.Id,
                            Name = dto.Name,
                            Gender = dto.Gender,
                            Age = CalculateAge(dto.BirthDate),
                            Phone = dto.PhoneNumber,
                            // IdCard = dto.IdNumber,  // 删除不存在的IdCard属性
                            Status = dto.Status  // 直接使用后端Status属性
                        })
                    );

                    // 如果只有一个结果，自动选中
                    if (SearchResults.Count == 1)
                    {
                        SelectedPatient = SearchResults.First();
                    }
                }
                else
                {
                    SearchResults.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者失败");
                await _dialogService.ShowErrorAsync("搜索失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadPatientDetailsAsync(Guid patientId)
        {
            try
            {
                var result = await _patientService.GetByIdAsync(patientId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    PatientDetails = result.Data;
                    
                    // 加载该患者的医疗案例历史
                    await LoadPatientMedicalCasesAsync(patientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者详情失败");
            }
        }

        private async Task LoadPatientMedicalCasesAsync(Guid patientId)
        {
            try
            {
                var result = await _medicalCaseService.GetByPatientIdAsync(patientId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var cases = result.Data as List<MedicalCaseInfo> ?? new List<MedicalCaseInfo>();
                    RecentCases = new ObservableCollection<MedicalCaseInfo>(
                        cases.OrderByDescending(c => c.CreateTime).Take(10)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者医疗案例失败");
            }
        }

        #endregion

        #region 快速接待

        private bool CanQuickReception()
        {
            // 要么选中了现有患者，要么填写了新患者信息
            return SelectedPatient != null || 
                   (!string.IsNullOrWhiteSpace(PatientName) && !string.IsNullOrWhiteSpace(PatientPhone));
        }

        private async Task QuickReceptionAsync()
        {
            try
            {
                IsLoading = true;

                PatientDetailDto patient;
                
                if (SelectedPatient != null)
                {
                    // 使用选中的患者
                    patient = PatientDetails ?? new PatientDetailDto 
                    { 
                        Id = SelectedPatient.Id,
                        Name = SelectedPatient.Name
                    };
                }
                else
                {
                    // 创建新患者
                    patient = await CreateOrFindPatientAsync();
                    if (patient == null) return;
                }

                // 创建医疗案例
                var medicalCase = await CreateMedicalCaseForPatientAsync(patient.Id);
                
                if (medicalCase != null)
                {
                    // 提示成功
                    await _dialogService.ShowSuccessAsync(
                        $"患者 {patient.Name} 接待成功，医疗案例已创建", 
                        "接待成功");

                    // 询问是否立即开始看诊
                    var startConsultation = await _dialogService.ShowConfirmAsync(
                        "是否立即开始看诊？", 
                        "开始看诊");

                    if (startConsultation)
                    {
                        NavigateToConsultation(medicalCase);
                    }
                    else
                    {
                        // 刷新列表
                        await RefreshDataAsync();
                        ClearForm();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速接待失败");
                await _dialogService.ShowErrorAsync("接待失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<PatientDetailDto?> CreateOrFindPatientAsync()
        {
            try
            {
                var dto = new PatientDetailDto
                {
                    Name = PatientName.Trim(),
                    Gender = Enum.TryParse<Gender>(PatientGender, out var gender) ? gender : Gender.Unknown,
                    PhoneNumber = PatientPhone.Trim(),
                    IDNumber = string.IsNullOrWhiteSpace(PatientIdCard) ? null : PatientIdCard.Trim(),
                    // IsEnabled属性在后端DTO中不存在，删除
                };

                // 计算出生日期（如果提供了年龄）
                if (!string.IsNullOrWhiteSpace(PatientAge) && int.TryParse(PatientAge, out var age))
                {
                    dto.BirthDate = DateTime.Today.AddYears(-age);
                }

                // 查询或创建患者
                var result = await _patientService.FindOrCreateAsync(dto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    IsNewPatient = result.Data.Id == Guid.Empty; // 判断是否是新创建的
                    return result.Data;
                }
                
                await _dialogService.ShowErrorAsync(
                    result.ErrorMessage ?? "创建患者失败", 
                    "错误");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建或查找患者失败");
                throw;
            }
        }

        #endregion

        #region 医疗案例管理

        private async Task CreateMedicalCaseAsync()
        {
            if (SelectedPatient == null) return;

            try
            {
                var medicalCase = await CreateMedicalCaseForPatientAsync(SelectedPatient.Id);
                
                if (medicalCase != null)
                {
                    await _dialogService.ShowSuccessAsync(
                        "医疗案例创建成功", 
                        "成功");
                    
                    // 刷新案例列表
                    await LoadPatientMedicalCasesAsync(SelectedPatient.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
                await _dialogService.ShowErrorAsync("创建失败: " + ex.Message, "错误");
            }
        }

        private async Task<MedicalCaseInfo?> CreateMedicalCaseForPatientAsync(Guid patientId)
        {
            try
            {
                var createDto = new MedicalCaseCreateDto
                {
                    PatientId = patientId,
                    DoctorId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty,
                    // ChiefComplaint属性在后端MedicalCaseCreateDto中不存在，删除
                    Remark = "患者接待创建"
                };

                var result = await _medicalCaseService.CreateAsync(createDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    return result.Data as MedicalCaseInfo;
                }

                await _dialogService.ShowErrorAsync(
                    result.ErrorMessage ?? "创建医疗案例失败", 
                    "错误");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
                throw;
            }
        }

        private async Task StartConsultationAsync(MedicalCaseInfo? medicalCase)
        {
            if (medicalCase == null) return;

            try
            {
                NavigateToConsultation(medicalCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动看诊失败");
                await _dialogService.ShowErrorAsync("启动看诊失败: " + ex.Message, "错误");
            }
        }

        private void NavigateToConsultation(MedicalCaseInfo medicalCase)
        {
            // 使用字符串参数方式导航 - Prism 9兼容
            _regionManager.RequestNavigate("MainContentRegion", $"ConsultationMainView?MedicalCaseId={medicalCase.Id}&PatientId={medicalCase.PatientId}&ConsultationMode=Start");
        }

        #endregion

        #region 辅助方法

        private async Task RegisterNewPatientAsync()
        {
            // 打开新建患者对话框
            var parameters = new DialogParameters();
            
            _prismDialogService.ShowDialog("AddPatientDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    // 刷新搜索结果
                    _ = SearchPatientsAsync();
                }
            });
        }

        private void ViewPatientDetails(PatientInfo? patient)
        {
            if (patient == null) return;

            // 使用字符串参数方式导航 - Prism 9兼容
            _regionManager.RequestNavigate("MainContentRegion", $"PatientDetailView?PatientId={patient.Id}&ViewMode=Detail");
        }

        private void ClearForm()
        {
            PatientName = "";
            PatientGender = "男";
            PatientAge = "";
            PatientPhone = "";
            PatientIdCard = "";
            ChiefComplaint = "";
            SearchKeyword = "";
            SelectedPatient = null;
            PatientDetails = null;
            IsNewPatient = false;
        }

        private async Task RefreshDataAsync()
        {
            await LoadTodayPatientsAsync();
            if (SelectedPatient != null)
            {
                await LoadPatientMedicalCasesAsync(SelectedPatient.Id);
            }
        }

        private int CalculateAge(DateTime? birthDate)
        {
            if (!birthDate.HasValue) return 0;
            
            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;
            
            return age;
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 如果有患者ID参数，直接加载该患者
            if (navigationContext.Parameters.ContainsKey("PatientId"))
            {
                var patientId = navigationContext.Parameters.GetValue<Guid>("PatientId");
                _ = LoadPatientDetailsAsync(patientId);
            }

            // 刷新数据
            _ = RefreshDataAsync();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 清理表单
            ClearForm();
        }

        #endregion
    }
}