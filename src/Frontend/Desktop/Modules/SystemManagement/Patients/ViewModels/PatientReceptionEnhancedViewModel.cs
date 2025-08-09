using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;
using LYBT.WPF.Client.Core.Extensions;
using LYBT.WPF.Client.Core.Models;
using Prism.Navigation.Regions;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Interfaces;
using LYBT.WPF.Client.Services;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels
{
    /// <summary>
    /// 增强版患者接待视图模型
    /// 支持智能搜索、身份证读卡、查无此人提示等功能
    /// </summary>
    public class PatientReceptionEnhancedViewModel : BindableBase, INavigationAware
    {
        #region 依赖服务

        private readonly IPatientService _patientService;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IDialogService _dialogService;
        private readonly IDialogService _prismDialogService;
        private readonly IRegionManager _regionManager;
        private readonly ILogger<PatientReceptionEnhancedViewModel> _logger;
        private readonly IIDCardReaderService? _idCardReaderService;

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
                    // 实时搜索（输入2个字符后触发）
                    if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                    {
                        _ = SmartSearchAsync();
                    }
                    else if (string.IsNullOrWhiteSpace(value))
                    {
                        SearchResults.Clear();
                        SearchStatusMessage = "";
                    }
                }
            }
        }

        private string _searchStatusMessage = "";
        public string SearchStatusMessage
        {
            get => _searchStatusMessage;
            set => SetProperty(ref _searchStatusMessage, value);
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
                    // 选中患者后加载详细信息和历史记录
                    if (value != null)
                    {
                        _ = LoadPatientDetailsAsync(value.Id);
                        AutoFillPatientForm(value);
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

        private ObservableCollection<MedicalCaseInfo> _patientHistory = new();
        public ObservableCollection<MedicalCaseInfo> PatientHistory
        {
            get => _patientHistory;
            set => SetProperty(ref _patientHistory, value);
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

        private bool _hasIdCardReader;
        public bool HasIdCardReader
        {
            get => _hasIdCardReader;
            set => SetProperty(ref _hasIdCardReader, value);
        }

        // 患者信息表单字段
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

        private string _patientAddress = "";
        public string PatientAddress
        {
            get => _patientAddress;
            set => SetProperty(ref _patientAddress, value);
        }

        private string _allergyHistory = "";
        public string AllergyHistory
        {
            get => _allergyHistory;
            set => SetProperty(ref _allergyHistory, value);
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
        public ICommand ReadIdCardCommand { get; }
        public ICommand QuickReceptionCommand { get; }
        public ICommand SaveAndStartCommand { get; }
        public ICommand CreateMedicalCaseCommand { get; }
        public ICommand StartConsultationCommand { get; }
        public ICommand ViewPatientDetailsCommand { get; }
        public ICommand RegisterNewPatientCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region 构造函数

        public PatientReceptionEnhancedViewModel(
            IPatientService patientService,
            IMedicalCaseService medicalCaseService,
            IUserSessionManager userSessionManager,
            IDialogService dialogService,
            IDialogService prismDialogService,
            IRegionManager regionManager,
            ILogger<PatientReceptionEnhancedViewModel> logger,
            IIDCardReaderService? idCardReaderService = null)
        {
            _patientService = patientService;
            _medicalCaseService = medicalCaseService;
            _userSessionManager = userSessionManager;
            _dialogService = dialogService;
            _prismDialogService = prismDialogService;
            _regionManager = regionManager;
            _logger = logger;
            _idCardReaderService = idCardReaderService;

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SmartSearchAsync());
            ReadIdCardCommand = new DelegateCommand(async () => await ReadIdCardAsync());
            QuickReceptionCommand = new DelegateCommand(async () => await QuickReceptionAsync(), CanQuickReception);
            SaveAndStartCommand = new DelegateCommand(async () => await SaveAndStartConsultationAsync());
            CreateMedicalCaseCommand = new DelegateCommand(async () => await CreateMedicalCaseAsync(), () => SelectedPatient != null);
            StartConsultationCommand = new DelegateCommand<MedicalCaseInfo>(async mc => await StartConsultationAsync(mc));
            ViewPatientDetailsCommand = new DelegateCommand<PatientInfo>(ViewPatientDetails);
            RegisterNewPatientCommand = new DelegateCommand(async () => await RegisterNewPatientAsync());
            ClearFormCommand = new DelegateCommand(ClearForm);
            RefreshCommand = new DelegateCommand(async () => await RefreshDataAsync());

            // 初始化
            _ = InitializeAsync();
        }

        #endregion

        #region 初始化

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;

                // 检查身份证读卡器
                CheckIdCardReader();

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

        private void CheckIdCardReader()
        {
            // 检测身份证读卡器服务
            HasIdCardReader = _idCardReaderService != null;
            
            // 如果有读卡器服务，订阅事件
            if (_idCardReaderService != null)
            {
                _idCardReaderService.StatusChanged += OnIdCardReaderStatusChanged;
                _idCardReaderService.CardRead += OnIdCardRead;
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

        #region 智能搜索

        private async Task SmartSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                SearchResults.Clear();
                SearchStatusMessage = "";
                return;
            }

            try
            {
                IsLoading = true;
                SearchStatusMessage = "搜索中...";

                // 识别搜索类型
                var searchType = IdentifySearchType(SearchKeyword);
                
                // 根据类型执行不同的搜索策略
                var result = await ExecuteSearchAsync(searchType, SearchKeyword);

                if (result.IsSuccess && result.Data != null)
                {
                    var patients = result.Data.Select(dto => new PatientInfo
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        Gender = dto.Gender,
                        Age = CalculateAge(dto.BirthDate),
                        Phone = dto.PhoneNumber,
                        // IdCard = dto.IDNumber,  // 删除PatientInfo中不存在的IdCard属性
                        Status = dto.Status  // 直接使用后端Status属性
                    }).ToList();

                    SearchResults = new ObservableCollection<PatientInfo>(patients);

                    // 根据结果数量显示不同提示
                    if (patients.Count == 0)
                    {
                        SearchStatusMessage = "查无此人";
                        // 自动提示创建新患者
                        await PromptCreateNewPatientAsync();
                    }
                    else if (patients.Count == 1)
                    {
                        SearchStatusMessage = "找到1位患者";
                        // 自动选中唯一结果
                        SelectedPatient = SearchResults.First();
                    }
                    else
                    {
                        SearchStatusMessage = $"找到{patients.Count}位患者";
                    }
                }
                else
                {
                    SearchResults.Clear();
                    SearchStatusMessage = "查无此人";
                    await PromptCreateNewPatientAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "智能搜索失败");
                SearchStatusMessage = "搜索失败";
                await _dialogService.ShowErrorAsync("搜索失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private SearchType IdentifySearchType(string keyword)
        {
            // 去除空格
            keyword = keyword.Trim();

            // 纯数字判断
            if (Regex.IsMatch(keyword, @"^\d+$"))
            {
                if (keyword.Length <= 4)
                {
                    return SearchType.PhoneSuffix; // 电话后4位
                }
                else if (keyword.Length <= 6)
                {
                    return SearchType.IdCardSuffix; // 身份证后6位
                }
                else if (keyword.Length == 11)
                {
                    return SearchType.Phone; // 完整手机号
                }
                else if (keyword.Length == 18)
                {
                    return SearchType.IdCard; // 完整身份证号
                }
            }

            // 包含X的可能是身份证
            if (Regex.IsMatch(keyword, @"^\d+[xX]?$") && keyword.Length >= 6)
            {
                return SearchType.IdCardSuffix;
            }

            // 纯字母可能是拼音
            if (Regex.IsMatch(keyword, @"^[a-zA-Z]+$"))
            {
                return SearchType.PinYin;
            }

            // 包含中文的是姓名
            if (Regex.IsMatch(keyword, @"[\u4e00-\u9fa5]"))
            {
                return SearchType.Name;
            }

            // 默认按姓名搜索
            return SearchType.Name;
        }

        private async Task<ServiceResult<List<PatientDetailDto>>> ExecuteSearchAsync(SearchType searchType, string keyword)
        {
            // 根据搜索类型调用不同的服务方法
            switch (searchType)
            {
                case SearchType.Name:
                case SearchType.PinYin:
                    return await _patientService.SearchByNameOrPinYinAsync(keyword);
                    
                case SearchType.Phone:
                case SearchType.PhoneSuffix:
                    return await _patientService.SearchByPhoneAsync(keyword);
                    
                case SearchType.IdCard:
                case SearchType.IdCardSuffix:
                    return await _patientService.SearchByIdCardAsync(keyword);
                    
                default:
                    return await _patientService.QuickSearchAsync(keyword);
            }
        }

        private async Task PromptCreateNewPatientAsync()
        {
            // 延迟一秒后提示，避免过于频繁
            await Task.Delay(1000);

            if (SearchResults.Count == 0 && !string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var createNew = await _dialogService.ShowConfirmAsync(
                    $"未找到患者 '{SearchKeyword}'，是否创建新患者档案？",
                    "查无此人");

                if (createNew)
                {
                    // 尝试解析搜索关键词
                    ParseSearchKeywordToForm(SearchKeyword);
                    
                    // 聚焦到姓名输入框
                    IsNewPatient = true;
                }
            }
        }

        private void ParseSearchKeywordToForm(string keyword)
        {
            // 如果是中文，可能是姓名
            if (Regex.IsMatch(keyword, @"[\u4e00-\u9fa5]"))
            {
                PatientName = keyword;
            }
            // 如果是11位数字，可能是手机号
            else if (Regex.IsMatch(keyword, @"^\d{11}$"))
            {
                PatientPhone = keyword;
            }
            // 如果是18位，可能是身份证
            else if (Regex.IsMatch(keyword, @"^\d{17}[\dxX]$"))
            {
                PatientIdCard = keyword;
                // 从身份证解析性别和年龄
                ParseIdCardInfo(keyword);
            }
        }

        #endregion

        #region 身份证读卡

        private async Task ReadIdCardAsync()
        {
            try
            {
                // 检查是否有读卡器服务
                if (_idCardReaderService == null)
                {
                    await _dialogService.ShowWarningAsync(
                        "身份证读卡器功能未启用\n请联系管理员配置读卡器", 
                        "提示");
                    return;
                }

                // 检查读卡器连接状态
                if (!await _idCardReaderService.IsConnectedAsync())
                {
                    IsLoading = true;
                    SearchStatusMessage = "正在连接读卡器...";
                    
                    var connected = await _idCardReaderService.ConnectAsync();
                    if (!connected)
                    {
                        IsLoading = false;
                        await _dialogService.ShowErrorAsync(
                            "连接身份证读卡器失败\n请检查设备是否正确连接", 
                            "连接失败");
                        return;
                    }
                }

                IsLoading = true;
                SearchStatusMessage = "正在读取身份证信息...";

                // 读取身份证
                var idCardInfo = await _idCardReaderService.ReadCardAsync();

                if (idCardInfo != null)
                {
                    // 填充表单
                    FillFormFromIdCard(idCardInfo);

                    // 自动查询是否已存在
                    await SearchByIdCardAsync(idCardInfo.IDNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取身份证失败");
                await _dialogService.ShowErrorAsync("读取身份证失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnIdCardReaderStatusChanged(object? sender, IDCardReaderStatusChangedEventArgs e)
        {
            // 更新UI状态
            _ = _dialogService.ShowInformationAsync(
                $"读卡器状态: {e.NewStatus}",
                "状态更新");
            
            _logger.LogInformation($"读卡器状态变化: {e.OldStatus} -> {e.NewStatus}");
        }

        private void OnIdCardRead(object? sender, IDCardReadEventArgs e)
        {
            if (e.Success && e.CardInfo != null)
            {
                // 自动填充表单
                FillFormFromIdCard(e.CardInfo);
                
                // 自动搜索
                _ = SearchByIdCardAsync(e.CardInfo.IDNumber);
            }
            else
            {
                _ = _dialogService.ShowErrorAsync(
                    e.ErrorMessage ?? "读取身份证失败",
                    "读卡失败");
            }
        }

        // 模拟身份证信息（已废弃，改用IDCardReaderService）
        [Obsolete("使用 IIDCardReaderService 替代")]
        private class IdCardInfo
        {
            public string Name { get; set; } = "";
            public string Gender { get; set; } = "";
            public string Nation { get; set; } = "";
            public DateTime BirthDate { get; set; } = new DateTime(1990, 1, 1);
            public string IdNumber { get; set; } = "320123199001011234";
            public string Address { get; set; } = "江苏省苏州市姑苏区人民路1号";
            public string IssuingAuthority { get; set; } = "苏州市公安局";
            public DateTime ValidFrom { get; set; } = new DateTime(2020, 1, 1);
            public DateTime ValidTo { get; set; } = new DateTime(2030, 1, 1);
        }

        private void FillFormFromIdCard(IDCardInfo idCardInfo)
        {
            PatientName = idCardInfo.Name;
            PatientGender = idCardInfo.Gender.ToString();
            PatientAge = idCardInfo.Age.ToString();
            PatientIdCard = idCardInfo.IDNumber;
            PatientAddress = idCardInfo.Address;
            
            // 记录读卡来源
            SearchStatusMessage = $"已从身份证读取信息：{idCardInfo.Name}";
            _logger.LogInformation($"身份证读取成功: {idCardInfo.Name}, {idCardInfo.IDNumber}");
        }

        private async Task SearchByIdCardAsync(string idCard)
        {
            var result = await _patientService.SearchByIdCardAsync(idCard);
            
            if (result.IsSuccess && result.Data?.Count > 0)
            {
                // 找到患者
                var patient = result.Data.First();
                SearchResults = new ObservableCollection<PatientInfo>
                {
                    new PatientInfo
                    {
                        Id = patient.Id,
                        Name = patient.Name,
                        Gender = patient.Gender,
                        Age = CalculateAge(patient.BirthDate),
                        Phone = patient.PhoneNumber,
                        // IdCard = patient.IdNumber,  // 删除PatientInfo中不存在的IdCard属性
                    }
                };
                SelectedPatient = SearchResults.First();
                SearchStatusMessage = "已找到患者信息";
            }
            else
            {
                // 未找到，提示保存
                SearchStatusMessage = "未找到患者信息";
                var save = await _dialogService.ShowConfirmAsync(
                    "该身份证未在系统中登记，是否保存为新患者？",
                    "新患者");
                    
                if (save)
                {
                    IsNewPatient = true;
                }
            }
        }

        private void ParseIdCardInfo(string idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard) || idCard.Length != 18)
                return;

            try
            {
                // 解析性别（第17位）
                int genderCode = int.Parse(idCard.Substring(16, 1));
                PatientGender = genderCode % 2 == 0 ? "女" : "男";

                // 解析出生日期
                string birthDateStr = idCard.Substring(6, 8);
                if (DateTime.TryParseExact(birthDateStr, "yyyyMMdd", null, 
                    System.Globalization.DateTimeStyles.None, out DateTime birthDate))
                {
                    PatientAge = CalculateAge(birthDate).ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析身份证信息失败");
            }
        }

        #endregion

        #region 快速接待

        private bool CanQuickReception()
        {
            // 要么选中了现有患者，要么填写了新患者必要信息
            return SelectedPatient != null ||
                   (!string.IsNullOrWhiteSpace(PatientName) && !string.IsNullOrWhiteSpace(PatientPhone));
        }

        private async Task QuickReceptionAsync()
        {
            try
            {
                IsLoading = true;

                PatientDetailDto patient;

                if (SelectedPatient != null && !IsNewPatient)
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
                    patient = await CreateOrUpdatePatientAsync();
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

        private async Task SaveAndStartConsultationAsync()
        {
            await QuickReceptionAsync();
        }

        private async Task<PatientDetailDto?> CreateOrUpdatePatientAsync()
        {
            try
            {
                // 验证必填项
                if (string.IsNullOrWhiteSpace(PatientName))
                {
                    await _dialogService.ShowWarningAsync("请输入患者姓名", "提示");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(PatientPhone))
                {
                    await _dialogService.ShowWarningAsync("请输入联系电话", "提示");
                    return null;
                }

                var dto = new PatientDetailDto
                {
                    Name = PatientName.Trim(),
                    Gender = Enum.TryParse<Gender>(PatientGender, out var gender) ? gender : Gender.Unknown,
                    PhoneNumber = PatientPhone.Trim(),
                    IDNumber = string.IsNullOrWhiteSpace(PatientIdCard) ? null : PatientIdCard.Trim(),
                    Address = string.IsNullOrWhiteSpace(PatientAddress) ? null : PatientAddress.Trim(),
                    AllergyHistory = string.IsNullOrWhiteSpace(AllergyHistory) ? null : AllergyHistory.Trim(),
                    // IsEnabled属性在后端DTO中不存在，删除
                };

                // 计算出生日期（如果提供了年龄）
                if (!string.IsNullOrWhiteSpace(PatientAge) && int.TryParse(PatientAge, out var age))
                {
                    dto.BirthDate = DateTime.Today.AddYears(-age);
                }

                // 创建或更新患者
                if (SelectedPatient != null && !IsNewPatient)
                {
                    // 更新现有患者
                    dto.Id = SelectedPatient.Id;
                    var updateResult = await _patientService.UpdateAsync(dto);
                    
                    if (updateResult.IsSuccess)
                    {
                        // 更新成功，重新获取患者数据
                        var getResult = await _patientService.GetByIdAsync(dto.Id);
                        if (getResult.IsSuccess && getResult.Data != null)
                        {
                            return getResult.Data;
                        }
                        else
                        {
                            await _dialogService.ShowErrorAsync("获取更新后的患者信息失败", "错误");
                            return null;
                        }
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(updateResult.ErrorMessage ?? "更新患者失败", "错误");
                        return null;
                    }
                }
                else
                {
                    // 创建新患者
                    var createResult = await _patientService.CreateAsync(dto);
                    if (createResult.IsSuccess && createResult.Data != null)
                    {
                        return createResult.Data;
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(createResult.ErrorMessage ?? "创建患者失败", "错误");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建或更新患者失败");
                await _dialogService.ShowErrorAsync($"保存患者信息失败: {ex.Message}", "错误");
                return null;
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
            _regionManager.RequestNavigate("ContentRegion", $"ConsultationMainView?MedicalCaseId={medicalCase.Id}&PatientId={medicalCase.PatientId}&PatientName={medicalCase.PatientName}&ConsultationMode=Start");
        }

        #endregion

        #region 辅助方法

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
                    PatientHistory = new ObservableCollection<MedicalCaseInfo>(
                        cases.OrderByDescending(c => c.CreateTime).Take(5)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者医疗案例失败");
            }
        }

        private void AutoFillPatientForm(PatientInfo patient)
        {
            PatientName = patient.Name;
            PatientGender = patient.Gender.ToString();
            PatientAge = patient.Age.ToString();
            PatientPhone = patient.Phone ?? "";
            // PatientIdCard = patient.IdNumber;  // 使用IdNumber属性
            
            if (PatientDetails != null)
            {
                PatientAddress = PatientDetails.Address;
                AllergyHistory = PatientDetails.AllergyHistory;
            }
        }

        private async Task RegisterNewPatientAsync()
        {
            // 打开新建患者对话框
            var parameters = new DialogParameters
            {
                { "InitialName", PatientName },
                { "InitialPhone", PatientPhone }
            };

            _prismDialogService.ShowDialog("AddPatientDialog", parameters, async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    // 刷新搜索结果
                    await SmartSearchAsync();
                }
            });
        }

        private void ViewPatientDetails(PatientInfo? patient)
        {
            if (patient == null) return;

            // 使用字符串参数方式导航 - Prism 9兼容
            _regionManager.RequestNavigate("ContentRegion", $"PatientDetailView?PatientId={patient.Id}&ViewMode=Detail");
        }

        private void ClearForm()
        {
            PatientName = "";
            PatientGender = "男";
            PatientAge = "";
            PatientPhone = "";
            PatientIdCard = "";
            PatientAddress = "";
            AllergyHistory = "";
            ChiefComplaint = "";
            SearchKeyword = "";
            SearchStatusMessage = "";
            SelectedPatient = null;
            PatientDetails = null;
            IsNewPatient = false;
            SearchResults.Clear();
            PatientHistory.Clear();
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

        #region 内部类型

        private enum SearchType
        {
            Name,           // 姓名
            PinYin,         // 拼音
            Phone,          // 完整手机号
            PhoneSuffix,    // 手机号后几位
            IdCard,         // 完整身份证
            IdCardSuffix    // 身份证后几位
        }


        #endregion
    }
}