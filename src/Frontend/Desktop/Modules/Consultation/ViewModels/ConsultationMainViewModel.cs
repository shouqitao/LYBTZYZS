using LYBT.Shared.Models.Enums;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Formulas;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Common;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace LYBT.WPF.Client.Modules.Consultation.ViewModels
{
    /// <summary>
    /// 看诊主界面视图模型 - 连接真实API服务
    /// </summary>
    public class ConsultationMainViewModel : BindableBase
    {
        #region 配置常量
        
        private const int MAX_PATIENT_DISPLAY_COUNT = 50; // 最大患者显示数量
        private const int MAX_FORMULA_DISPLAY_COUNT = 20; // 最大验方显示数量
        
        // 处方验证常量
        private const decimal MIN_HERB_QUANTITY = 0.1m; // 最小药材用量
        private const decimal MAX_HERB_QUANTITY = 1000m; // 最大药材用量
        private const decimal DEFAULT_HERB_QUANTITY = 10m; // 默认药材用量
        private const int MAX_PRESCRIPTION_ITEMS = 50; // 最大处方项目数
        
        // 缓存配置常量
        private const int HERBS_CACHE_DURATION_MINUTES = 30; // 药材缓存30分钟
        private const int FORMULAS_CACHE_DURATION_MINUTES = 60; // 验方缓存60分钟
        private const int PATIENTS_CACHE_DURATION_MINUTES = 10; // 患者缓存10分钟
        
        #endregion
        
        #region 依赖服务
        
        private readonly IPatientsApiService _patientsApiService;
        private readonly IConsultationApiService _consultationApiService;
        private readonly IFormulaTemplateApiService _formulaApiService;
        private readonly IHerbService _herbService;
        private readonly IPrescriptionPrintService _prescriptionPrintService;
        private readonly IPrescriptionApiService _prescriptionApiService;
        private readonly ILogger<ConsultationMainViewModel> _logger;
        private readonly IMapper _mapper;

        #endregion
        #region 属性

        private string _title = "看诊工作台";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private ObservableCollection<PatientInfo> _patients = new();
        public ObservableCollection<PatientInfo> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientInfo? _selectedPatient;
        public PatientInfo? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    OnPatientSelected();
                }
            }
        }

        private ConsultationInfo? _currentConsultation;
        public ConsultationInfo? CurrentConsultation
        {
            get => _currentConsultation;
            set => SetProperty(ref _currentConsultation, value);
        }

        private ObservableCollection<PrescriptionItemInfo> _prescriptionItems = new();
        public ObservableCollection<PrescriptionItemInfo> PrescriptionItems
        {
            get => _prescriptionItems;
            set => SetProperty(ref _prescriptionItems, value);
        }

        private ObservableCollection<HerbInfo> _availableHerbs = new();
        public ObservableCollection<HerbInfo> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        private ObservableCollection<HerbInfo> _allHerbs = new(); // 保存所有药材数据
        private string _herbSearchKeyword = string.Empty;
        public string HerbSearchKeyword
        {
            get => _herbSearchKeyword;
            set
            {
                if (SetProperty(ref _herbSearchKeyword, value))
                {
                    FilterHerbs();
                }
            }
        }

        private ObservableCollection<FormulaInfo> _availableFormulas = new();
        public ObservableCollection<FormulaInfo> AvailableFormulas
        {
            get => _availableFormulas;
            set => SetProperty(ref _availableFormulas, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchPatients();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>当前处方ID（用于更新已存在的处方）</summary>
        private Guid _currentPrescriptionId = Guid.Empty;
        public Guid CurrentPrescriptionId
        {
            get => _currentPrescriptionId;
            set => SetProperty(ref _currentPrescriptionId, value);
        }

        // 缓存时间戳
        private DateTime _herbsCacheTime = DateTime.MinValue;
        private DateTime _formulasCacheTime = DateTime.MinValue;
        private DateTime _patientsCacheTime = DateTime.MinValue;

        #endregion

        #region 命令

        public ICommand RefreshCommand { get; }
        public ICommand NewConsultationCommand { get; }
        public ICommand SaveConsultationCommand { get; }
        public ICommand PrintPrescriptionCommand { get; }
        public ICommand RemovePrescriptionItemCommand { get; }
        public ICommand AddHerbCommand { get; }
        public ICommand ApplyFormulaCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }

        #endregion

        public ConsultationMainViewModel(
            IPatientsApiService patientsApiService,
            IConsultationApiService consultationApiService,
            IFormulaTemplateApiService formulaApiService,
            IHerbService herbService,
            IPrescriptionPrintService prescriptionPrintService,
            IPrescriptionApiService prescriptionApiService,
            ILogger<ConsultationMainViewModel> logger,
            IMapper mapper)
        {
            _patientsApiService = patientsApiService;
            _consultationApiService = consultationApiService;
            _formulaApiService = formulaApiService;
            _herbService = herbService;
            _prescriptionPrintService = prescriptionPrintService;
            _prescriptionApiService = prescriptionApiService;
            _logger = logger;
            _mapper = mapper;

            // 初始化命令
            RefreshCommand = new DelegateCommand(async () => await RefreshAllDataAsync());
            NewConsultationCommand = new DelegateCommand(StartNewConsultation, () => SelectedPatient != null);
            SaveConsultationCommand = new DelegateCommand(async () => await SaveConsultationAsync(), () => CurrentConsultation != null);
            PrintPrescriptionCommand = new DelegateCommand(PrintPrescription, () => PrescriptionItems?.Any() == true);
            RemovePrescriptionItemCommand = new DelegateCommand<PrescriptionItemInfo>(RemovePrescriptionItem);
            AddHerbCommand = new DelegateCommand<HerbInfo>(AddHerbToPrescription);
            ApplyFormulaCommand = new DelegateCommand<FormulaInfo>(ApplyFormula);
            IncreaseQuantityCommand = new DelegateCommand<PrescriptionItemInfo>(IncreaseQuantity);
            DecreaseQuantityCommand = new DelegateCommand<PrescriptionItemInfo>(DecreaseQuantity);

            // 加载初始数据
            _ = LoadInitialDataAsync();
        }

        #region 方法

        private async Task LoadInitialDataAsync()
        {
            await LoadPatientsAsync();
            await LoadAvailableHerbsAsync();
            await LoadAvailableFormulasAsync();
        }

        /// <summary>
        /// 刷新所有数据（强制清除缓存）
        /// </summary>
        private async Task RefreshAllDataAsync()
        {
            _logger.LogInformation("手动刷新所有数据，清除缓存");
            
            // 清除所有缓存
            ClearAllCache();
            
            // 重新加载所有数据
            await LoadInitialDataAsync();
        }

        private async Task LoadPatientsAsync()
        {
            // 检查缓存是否有效
            if (IsPatientsCacheValid())
            {
                _logger.LogInformation($"使用缓存的患者数据，共 {Patients.Count} 个患者");
                return;
            }

            try
            {
                IsLoading = true;
                
                // 获取启用的患者列表（最近就诊的患者优先）
                var response = await _patientsApiService.GetActivePatientsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    Patients.Clear();
                    
                    // 转换DTO为UI模型
                    foreach (var patientDto in response.Content.Take(MAX_PATIENT_DISPLAY_COUNT)) // 限制显示患者数量
                    {
                        var patientInfo = new PatientInfo
                        {
                            Id = patientDto.Id,
                            Name = patientDto.Name,
                            Gender = patientDto.Gender,
                            Age = patientDto.Age,
                            PhoneNumber = patientDto.PhoneNumber ?? "",
                            Address = patientDto.Address ?? "",
                        };
                        Patients.Add(patientInfo);
                    }
                    
                    // 更新缓存时间戳
                    _patientsCacheTime = DateTime.Now;
                    
                    _logger.LogInformation($"从API加载 {Patients.Count} 个患者，已缓存");
                }
                else
                {
                    _logger.LogWarning($"加载患者列表失败: {response.Error?.Content}");
                    ShowErrorMessage("加载患者列表失败，请检查网络连接");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "加载患者列表时权限不足");
                ShowErrorMessage("没有权限访问患者数据，请联系管理员");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogError(ex, "加载患者列表时网络请求失败");
                ShowErrorMessage("网络连接失败，请检查网络设置后重试");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "加载患者列表时请求超时");
                ShowErrorMessage("请求超时，服务器响应过慢，请稍后重试");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者列表时发生未知异常");
                ShowErrorMessage("加载患者列表时发生未知错误，请联系技术支持");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAvailableHerbsAsync()
        {
            // 检查缓存是否有效
            if (IsHerbsCacheValid())
            {
                _logger.LogInformation($"使用缓存的药材数据，共 {_allHerbs.Count} 种药材");
                // 刷新可见的药材列表
                FilterHerbs();
                return;
            }

            try
            {
                IsLoading = true;
                
                // 使用真实API获取可用药材列表
                var herbs = await _herbService.GetAvailableHerbsAsync();
                
                _allHerbs.Clear();
                AvailableHerbs.Clear();
                foreach (var herb in herbs)
                {
                    _allHerbs.Add(herb);
                    AvailableHerbs.Add(herb);
                }
                
                // 更新缓存时间戳
                _herbsCacheTime = DateTime.Now;
                
                _logger.LogInformation($"从API加载 {herbs.Count} 种可用药材，已缓存");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "加载药材数据时权限不足");
                ShowErrorMessage("没有权限访问药材数据，请联系管理员");
                _allHerbs.Clear();
                AvailableHerbs.Clear();
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogError(ex, "加载药材数据时网络请求失败");
                ShowErrorMessage("网络连接失败，无法获取药材数据");
                _allHerbs.Clear();
                AvailableHerbs.Clear();
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "加载药材数据时请求超时");
                ShowErrorMessage("请求超时，请稍后重试");
                _allHerbs.Clear();
                AvailableHerbs.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载可用药材时发生未知异常");
                ShowErrorMessage("加载药材数据失败，请联系技术支持");
                
                // 发生错误时清空列表，避免显示过期数据
                _allHerbs.Clear();
                AvailableHerbs.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载可用验方列表
        /// </summary>
        private async Task LoadAvailableFormulasAsync()
        {
            // 检查缓存是否有效
            if (IsFormulasCacheValid())
            {
                _logger.LogInformation($"使用缓存的验方数据，共 {AvailableFormulas.Count} 个验方");
                return;
            }

            try
            {
                IsLoading = true;
                
                // 获取验方列表
                var formulaResult = await _formulaApiService.GetListAsync();
                if (formulaResult.Success && formulaResult.Data != null)
                {
                    AvailableFormulas.Clear();
                    
                    // 限制显示数量，避免UI性能问题
                    var formulas = formulaResult.Data.Take(MAX_FORMULA_DISPLAY_COUNT);
                    foreach (var formula in formulas)
                    {
                        AvailableFormulas.Add(formula);
                    }
                    
                    // 更新缓存时间戳
                    _formulasCacheTime = DateTime.Now;
                    
                    _logger.LogInformation($"从API加载 {AvailableFormulas.Count} 个验方，已缓存");
                }
                else
                {
                    _logger.LogWarning("获取验方列表失败或返回数据为空");
                    AvailableFormulas.Clear();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "加载验方数据时权限不足");
                ShowErrorMessage("没有权限访问验方数据，请联系管理员");
                AvailableFormulas.Clear();
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogError(ex, "加载验方数据时网络请求失败");
                ShowErrorMessage("网络连接失败，无法获取验方数据");
                AvailableFormulas.Clear();
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "加载验方数据时请求超时");
                ShowErrorMessage("请求超时，请稍后重试");
                AvailableFormulas.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载可用验方时发生未知异常");
                ShowErrorMessage("加载验方数据失败，请联系技术支持");
                AvailableFormulas.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 根据搜索关键词过滤药材
        /// </summary>
        private void FilterHerbs()
        {
            List<HerbInfo> filteredHerbs;
            
            if (string.IsNullOrWhiteSpace(HerbSearchKeyword))
            {
                // 没有搜索关键词时显示所有药材
                filteredHerbs = _allHerbs.ToList();
            }
            else
            {
                // 根据关键词过滤 - 支持名称、拼音码、别名搜索
                var keyword = HerbSearchKeyword.Trim();
                filteredHerbs = _allHerbs.Where(h => 
                    h.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(h.PinyinCode) && h.PinyinCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(h.Alias) && h.Alias.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }
            
            // 使用更高效的集合更新方式
            AvailableHerbs.Clear();
            foreach (var herb in filteredHerbs)
            {
                AvailableHerbs.Add(herb);
            }
        }

        private async void SearchPatients()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // 搜索关键词为空时，重新加载所有患者
                await LoadPatientsAsync();
                return;
            }

            try
            {
                IsLoading = true;
                
                // 使用API搜索患者
                var response = await _patientsApiService.SearchAsync(SearchKeyword);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    Patients.Clear();
                    
                    // 转换搜索结果为UI模型
                    foreach (var patientDto in response.Content)
                    {
                        var patientInfo = new PatientInfo
                        {
                            Id = patientDto.Id,
                            Name = patientDto.Name,
                            Gender = patientDto.Gender,
                            Age = patientDto.Age,
                            PhoneNumber = patientDto.PhoneNumber ?? "",
                            Address = patientDto.Address ?? "",
                        };
                        Patients.Add(patientInfo);
                    }
                    
                    _logger.LogInformation($"搜索到 {Patients.Count} 个匹配的患者");
                }
                else
                {
                    _logger.LogWarning($"搜索患者失败: {response.Error?.Content}");
                    ShowErrorMessage("搜索患者失败");
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogError(ex, "搜索患者时网络请求失败");
                ShowErrorMessage("网络连接失败，搜索失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者时发生未知异常");
                ShowErrorMessage("搜索患者失败，请重试");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnPatientSelected()
        {
            if (SelectedPatient == null)
            {
                CurrentConsultation = null;
                return;
            }

            // 创建新的看诊记录
            CurrentConsultation = new ConsultationInfo
            {
                Id = Guid.NewGuid(),
                PatientId = SelectedPatient.Id,
                PatientName = SelectedPatient.Name,
                ConsultationTime = DateTime.Now,
                DoctorName = "当前医生"
            };
        }

        private void StartNewConsultation()
        {
            if (SelectedPatient == null) return;
            
            // 重用患者选择逻辑
            OnPatientSelected();
            
            // 清空处方项目（开始新的看诊）
            PrescriptionItems.Clear();
            
            // 重置处方ID（开始新处方）
            CurrentPrescriptionId = Guid.Empty;
        }

        private async Task SaveConsultationAsync()
        {
            if (CurrentConsultation == null) return;

            // 必填字段验证
            if (!ValidateConsultationData())
            {
                return;
            }

            try
            {
                IsLoading = true;
                
                // 准备看诊数据
                var startDto = new ConsultationStartDto
                {
                    PatientId = CurrentConsultation.PatientId,
                    MedicalCaseId = CurrentConsultation.MedicalCaseId != Guid.Empty ? CurrentConsultation.MedicalCaseId : Guid.NewGuid() // 如果没有医疗案例ID，创建新的
                };

                // 如果是新看诊记录，先开始看诊
                if (CurrentConsultation.Id == Guid.Empty || !await ConsultationExistsAsync(CurrentConsultation.Id))
                {
                    var startResponse = await _consultationApiService.StartConsultationAsync(startDto);
                    if (startResponse.IsSuccessStatusCode && startResponse.Content != null)
                    {
                        // 更新看诊ID
                        CurrentConsultation.Id = startResponse.Content.Id;
                        CurrentConsultation.MedicalCaseId = startResponse.Content.MedicalCaseId;
                        _logger.LogInformation($"成功创建看诊记录: {CurrentConsultation.Id}");
                    }
                    else
                    {
                        ShowErrorMessage("创建看诊记录失败");
                        return;
                    }
                }

                // 更新看诊信息
                var updateDto = new ConsultationUpdateDto
                {
                    // 中医四诊
                    Inspection = CurrentConsultation.Inspection,
                    AuscultationOlfaction = CurrentConsultation.AuscultationOlfaction,
                    Inquiry = CurrentConsultation.Inquiry,
                    Palpation = CurrentConsultation.Palpation,
                    TongueInspection = CurrentConsultation.TongueInspection,
                    PulseCondition = CurrentConsultation.PulseCondition,
                    
                    // 辨证论治
                    TCMDiagnosis = CurrentConsultation.TCMDiagnosis,
                    TreatmentPrinciple = CurrentConsultation.TreatmentPrinciple,
                    MedicalAdvice = CurrentConsultation.MedicalAdvice,
                    Diagnosis = CurrentConsultation.TCMDiagnosis, // 使用中医诊断作为主诊断
                    Remark = CurrentConsultation.Remark
                };

                var updateResponse = await _consultationApiService.UpdateConsultationAsync(CurrentConsultation.Id, updateDto);
                if (updateResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"成功保存看诊记录: {CurrentConsultation.Id}");
                    
                    // 保存处方（如果有处方项目）
                    bool prescriptionSaveResult = true;
                    if (PrescriptionItems.Any())
                    {
                        prescriptionSaveResult = await SavePrescriptionAsync();
                    }
                    
                    if (prescriptionSaveResult)
                    {
                        ShowSuccessMessage($"保存成功！看诊记录已保存{(PrescriptionItems.Any() ? "，处方已保存" : "")}");
                    }
                    else
                    {
                        ShowErrorMessage("看诊记录已保存，但处方保存失败");
                    }
                }
                else
                {
                    ShowErrorMessage("保存看诊记录失败");
                    _logger.LogWarning($"保存看诊记录失败: {updateResponse.Error?.Content}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "保存看诊记录时权限不足");
                ShowErrorMessage("没有权限保存看诊记录，请联系管理员");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "保存看诊记录时业务逻辑错误");
                ShowErrorMessage($"保存失败：{ex.Message}");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogError(ex, "保存看诊记录时网络请求失败");
                ShowErrorMessage("网络连接失败，保存失败，请检查网络后重试");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "保存看诊记录时请求超时");
                ShowErrorMessage("保存超时，请稍后重试");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存看诊记录时发生未知异常");
                ShowErrorMessage("保存看诊记录失败，请联系技术支持");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载可用验方列表
        /// </summary>
        private async Task LoadAvailableFormulasAsync()
        {
            try
            {
                IsLoading = true;
                
                // 获取验方列表，显示前20个最常用的
                var response = await _formulaApiService.GetFormulasAsync(null, null);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    AvailableFormulas.Clear();
                    
                    // 转换DTO为UI模型（取前20个）
                    foreach (var formulaDto in response.Content.Items.Take(MAX_FORMULA_DISPLAY_COUNT))
                    {
                        var formulaInfo = new FormulaInfo
                        {
                            Id = formulaDto.Id,
                            Name = formulaDto.Name,
                            // 使用来自 Formulas 命名空间的 FormulaDto，根据实际字段映射
                            Category = formulaDto.Category ?? "",
                            Indications = formulaDto.Indications,
                            CreateTime = formulaDto.CreateTime,
                            UpdateTime = formulaDto.UpdateTime
                        };
                        AvailableFormulas.Add(formulaInfo);
                    }
                    
                    _logger.LogInformation($"成功加载 {AvailableFormulas.Count} 个验方模板");
                }
                else
                {
                    _logger.LogWarning($"加载验方列表失败: {response.Error?.Content}");
                    ShowErrorMessage("加载验方列表失败，请检查网络连接");
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogError(ex, "加载验方列表时网络请求失败");
                ShowErrorMessage("网络连接失败，无法加载验方列表");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载验方列表时发生未知异常");
                ShowErrorMessage("加载验方列表失败，请重试");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 应用验方到处方
        /// </summary>
        private async void ApplyFormula(FormulaInfo formula)
        {
            if (formula == null) return;

            try
            {
                IsLoading = true;
                
                // 获取验方详情
                var response = await _formulaApiService.GetFormulaByIdAsync(formula.Id);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var formulaDetail = response.Content;
                    
                    // 清空当前处方
                    PrescriptionItems.Clear();
                    
                    // 添加验方中的药材到处方
                    foreach (var herb in formulaDetail.Herbs)
                    {
                        var prescriptionItem = new PrescriptionItemInfo
                        {
                            Id = Guid.NewGuid(),
                            HerbId = herb.HerbId,
                            HerbName = herb.HerbName,
                            Quantity = herb.Quantity,
                            Unit = herb.Unit
                        };
                        PrescriptionItems.Add(prescriptionItem);
                    }
                    
                    ShowSuccessMessage($"已应用验方：{formula.Name}，共 {formulaDetail.Herbs.Count} 味药材");
                    _logger.LogInformation($"成功应用验方: {formula.Name}");
                }
                else
                {
                    ShowErrorMessage("获取验方详情失败");
                    _logger.LogWarning($"获取验方详情失败: {response.Error?.Content}");
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogError(ex, "应用验方时网络请求失败");
                ShowErrorMessage("网络连接失败，应用验方失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用验方时发生未知异常");
                ShowErrorMessage("应用验方失败，请重试");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 检查看诊记录是否存在
        /// </summary>
        private async Task<bool> ConsultationExistsAsync(Guid consultationId)
        {
            try
            {
                var response = await _consultationApiService.GetByIdAsync(consultationId);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 打印处方
        /// </summary>
        private async void PrintPrescription()
        {
            if (!PrescriptionItems.Any())
            {
                ShowErrorMessage("处方为空，无法打印");
                return;
            }

            if (CurrentConsultation == null)
            {
                ShowErrorMessage("请先选择患者并完成看诊信息");
                return;
            }

            try
            {
                IsLoading = true;
                
                // 构建处方数据
                var prescriptionData = BuildPrescriptionData();
                
                // 先显示预览
                var previewResult = await _prescriptionPrintService.PreviewPrescriptionAsync(prescriptionData);
                if (previewResult.Success)
                {
                    // 显示打印预览对话框
                    var result = System.Windows.MessageBox.Show(
                        $"处方预览：\n\n{previewResult.Content}\n\n确定要打印吗？", 
                        "处方打印预览", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Question);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        // 执行打印
                        var printSuccess = await _prescriptionPrintService.PrintPrescriptionAsync(prescriptionData);
                        if (printSuccess)
                        {
                            System.Windows.MessageBox.Show("处方打印成功！", "提示", 
                                System.Windows.MessageBoxButton.OK, 
                                System.Windows.MessageBoxImage.Information);
                            
                            _logger.LogInformation($"处方打印成功：患者 {CurrentConsultation.PatientName}，共 {PrescriptionItems.Count} 味药材");
                        }
                        else
                        {
                            ShowErrorMessage("打印失败，请检查打印机设置");
                        }
                    }
                }
                else
                {
                    ShowErrorMessage($"预览生成失败：{previewResult.Message}");
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "打印处方时业务逻辑错误");
                ShowErrorMessage($"打印失败：{ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印处方时发生未知异常");
                ShowErrorMessage("打印处方失败，请重试");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 构建处方数据对象
        /// </summary>
        private object BuildPrescriptionData()
        {
            return new
            {
                ClinicName = "凌隐宝堂中医诊所",
                PatientName = CurrentConsultation?.PatientName ?? "",
                ConsultationTime = DateTime.Now,
                DoctorName = CurrentConsultation?.DoctorName ?? "当前医生",
                TCMDiagnosis = CurrentConsultation?.TCMDiagnosis ?? "",
                TreatmentPrinciple = CurrentConsultation?.TreatmentPrinciple ?? "",
                MedicalAdvice = CurrentConsultation?.MedicalAdvice ?? "",
                Remark = CurrentConsultation?.Remark ?? "",
                PrescriptionItems = PrescriptionItems.Select(item => new
                {
                    item.HerbName,
                    item.Quantity,
                    item.Unit,
                    item.UnitPrice,
                    Amount = item.Amount,
                    item.Usage,
                    item.Remark
                }).ToList(),
                TotalItems = PrescriptionItems.Count,
                TotalAmount = PrescriptionItems.Sum(i => i.Amount),
                PrintTime = DateTime.Now
            };
        }

        /// <summary>
        /// 保存处方到后端
        /// </summary>
        private async Task<bool> SavePrescriptionAsync()
        {
            try
            {
                if (CurrentConsultation == null || SelectedPatient == null)
                {
                    _logger.LogWarning("当前看诊信息或患者信息为空，无法保存处方");
                    return false;
                }

                // 获取当前医生ID (暂时使用固定值，实际应从当前用户会话获取)
                var currentDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // TODO: 从用户会话获取真实的医生ID
                var currentDoctorName = "当前医生"; // TODO: 从用户会话获取真实的医生姓名

                if (CurrentPrescriptionId == Guid.Empty)
                {
                    // 创建新处方
                    var createDto = new PrescriptionCreateDto
                    {
                        PatientId = SelectedPatient.Id,
                        DoctorId = currentDoctorId,
                        Diagnosis = CurrentConsultation.TCMDiagnosis ?? CurrentConsultation.Diagnosis ?? "中医诊断",
                        DosageCount = 7, // 默认7剂
                        Advice = CurrentConsultation.MedicalAdvice,
                        FormulaSource = "手动开方",
                        Remark = CurrentConsultation.Remark,
                        Items = PrescriptionItems.Select(item => new PrescriptionItemCreateDto
                        {
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            UnitPrice = item.UnitPrice,
                            Remark = item.Remark
                        }).ToList()
                    };

                    var createResponse = await _prescriptionApiService.CreatePrescriptionAsync(createDto);
                    if (createResponse.IsSuccessStatusCode && createResponse.Content != null)
                    {
                        CurrentPrescriptionId = createResponse.Content.Id;
                        _logger.LogInformation($"成功创建处方: {CurrentPrescriptionId}");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"创建处方失败: {createResponse.Error?.Content}");
                        return false;
                    }
                }
                else
                {
                    // 更新现有处方
                    var editDto = new PrescriptionEditDto
                    {
                        Id = CurrentPrescriptionId,
                        Diagnosis = CurrentConsultation.TCMDiagnosis ?? CurrentConsultation.Diagnosis ?? "中医诊断",
                        DosageCount = 7, // 默认7剂
                        Advice = CurrentConsultation.MedicalAdvice,
                        Remark = CurrentConsultation.Remark,
                        Items = PrescriptionItems.Select(item => new PrescriptionItemCreateDto
                        {
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            UnitPrice = item.UnitPrice,
                            Remark = item.Remark
                        }).ToList()
                    };

                    var updateResponse = await _prescriptionApiService.UpdatePrescriptionAsync(editDto);
                    if (updateResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"成功更新处方: {CurrentPrescriptionId}");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"更新处方失败: {updateResponse.Error?.Content}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方时发生异常");
                return false;
            }
        }

        private void AddHerbToPrescription(HerbInfo herb)
        {
            if (herb == null)
            {
                ShowErrorMessage("请选择要添加的药材");
                return;
            }

            // 验证处方项目数量限制
            if (PrescriptionItems.Count >= MAX_PRESCRIPTION_ITEMS)
            {
                ShowErrorMessage($"处方项目不能超过{MAX_PRESCRIPTION_ITEMS}项");
                return;
            }

            // 验证药材名称
            if (string.IsNullOrWhiteSpace(herb.Name))
            {
                ShowErrorMessage("药材名称不能为空");
                return;
            }

            var existingItem = PrescriptionItems.FirstOrDefault(p => p.HerbId == herb.Id);
            if (existingItem != null)
            {
                // 增加现有药材的用量，并验证最大值
                var newQuantity = existingItem.Quantity + DEFAULT_HERB_QUANTITY;
                if (newQuantity > MAX_HERB_QUANTITY)
                {
                    ShowErrorMessage($"单味药材用量不能超过{MAX_HERB_QUANTITY}{herb.Unit}");
                    return;
                }
                existingItem.Quantity = newQuantity;
                _logger.LogInformation($"增加药材用量: {herb.Name}, 新用量: {newQuantity}{herb.Unit}");
            }
            else
            {
                // 添加新的药材
                var newItem = new PrescriptionItemInfo
                {
                    Id = Guid.NewGuid(),
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = DEFAULT_HERB_QUANTITY,
                    Unit = !string.IsNullOrWhiteSpace(herb.Unit) ? herb.Unit : "g",
                    UnitPrice = herb.RetailPrice > 0 ? herb.RetailPrice : 0,
                    Origin = herb.Origin,
                    Specification = herb.Specification,
                    IsOutOfStock = herb.StockQuantity <= 0
                };

                PrescriptionItems.Add(newItem);
                _logger.LogInformation($"添加新药材: {herb.Name}, 用量: {DEFAULT_HERB_QUANTITY}{newItem.Unit}");
            }

            // 刷新命令状态
            (PrintPrescriptionCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private void RemovePrescriptionItem(PrescriptionItemInfo item)
        {
            if (item == null)
            {
                ShowErrorMessage("请选择要删除的药材");
                return;
            }

            PrescriptionItems.Remove(item);
            _logger.LogInformation($"移除药材: {item.HerbName}");
            
            // 刷新命令状态
            (PrintPrescriptionCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private void IncreaseQuantity(PrescriptionItemInfo item)
        {
            if (item == null)
            {
                ShowErrorMessage("请选择要调整的药材");
                return;
            }

            var newQuantity = item.Quantity + 1;
            if (newQuantity > MAX_HERB_QUANTITY)
            {
                ShowErrorMessage($"{item.HerbName}用量不能超过{MAX_HERB_QUANTITY}{item.Unit}");
                return;
            }

            item.Quantity = newQuantity;
            _logger.LogInformation($"增加药材用量: {item.HerbName}, 新用量: {newQuantity}{item.Unit}");
        }

        private void DecreaseQuantity(PrescriptionItemInfo item)
        {
            if (item == null)
            {
                ShowErrorMessage("请选择要调整的药材");
                return;
            }

            var newQuantity = item.Quantity - 1;
            if (newQuantity < MIN_HERB_QUANTITY)
            {
                ShowErrorMessage($"{item.HerbName}用量不能少于{MIN_HERB_QUANTITY}{item.Unit}");
                return;
            }

            item.Quantity = newQuantity;
            _logger.LogInformation($"减少药材用量: {item.HerbName}, 新用量: {newQuantity}{item.Unit}");
        }

        /// <summary>
        /// 验证看诊数据
        /// </summary>
        private bool ValidateConsultationData()
        {
            if (CurrentConsultation == null)
            {
                ShowErrorMessage("看诊信息不能为空");
                return false;
            }

            if (SelectedPatient == null)
            {
                ShowErrorMessage("请先选择患者");
                return false;
            }

            // 验证至少有一项中医四诊内容
            bool hasTCMDiagnosis = !string.IsNullOrWhiteSpace(CurrentConsultation.Inspection) ||
                                 !string.IsNullOrWhiteSpace(CurrentConsultation.AuscultationOlfaction) ||
                                 !string.IsNullOrWhiteSpace(CurrentConsultation.Inquiry) ||
                                 !string.IsNullOrWhiteSpace(CurrentConsultation.Palpation);

            if (!hasTCMDiagnosis)
            {
                ShowErrorMessage("请至少填写一项中医四诊内容（望、闻、问、切）");
                return false;
            }

            // 验证中医诊断
            if (string.IsNullOrWhiteSpace(CurrentConsultation.TCMDiagnosis))
            {
                ShowErrorMessage("中医诊断不能为空");
                return false;
            }

            // 如果有处方，验证处方数据
            if (PrescriptionItems.Any())
            {
                if (!ValidatePrescriptionData())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 验证处方数据
        /// </summary>
        private bool ValidatePrescriptionData()
        {
            foreach (var item in PrescriptionItems)
            {
                // 验证药材名称
                if (string.IsNullOrWhiteSpace(item.HerbName))
                {
                    ShowErrorMessage("处方中存在药材名称为空的项目，请检查");
                    return false;
                }

                // 验证用量范围
                if (item.Quantity < MIN_HERB_QUANTITY || item.Quantity > MAX_HERB_QUANTITY)
                {
                    ShowErrorMessage($"药材《{item.HerbName}》的用量应在{MIN_HERB_QUANTITY}-{MAX_HERB_QUANTITY}{item.Unit}之间");
                    return false;
                }

                // 验证单位
                if (string.IsNullOrWhiteSpace(item.Unit))
                {
                    ShowErrorMessage($"药材《{item.HerbName}》的单位不能为空");
                    return false;
                }

                // 验证药材ID
                if (item.HerbId == Guid.Empty)
                {
                    ShowErrorMessage($"药材《{item.HerbName}》的ID无效，请重新添加");
                    return false;
                }
            }

            // 验证处方项目数量
            if (PrescriptionItems.Count > MAX_PRESCRIPTION_ITEMS)
            {
                ShowErrorMessage($"处方项目数量不能超过{MAX_PRESCRIPTION_ITEMS}项");
                return false;
            }

            return true;
        }

        #region 缓存机制

        /// <summary>
        /// 检查药材缓存是否有效
        /// </summary>
        private bool IsHerbsCacheValid()
        {
            return DateTime.Now.Subtract(_herbsCacheTime).TotalMinutes < HERBS_CACHE_DURATION_MINUTES && _allHerbs.Any();
        }

        /// <summary>
        /// 检查验方缓存是否有效
        /// </summary>
        private bool IsFormulasCacheValid()
        {
            return DateTime.Now.Subtract(_formulasCacheTime).TotalMinutes < FORMULAS_CACHE_DURATION_MINUTES && AvailableFormulas.Any();
        }

        /// <summary>
        /// 检查患者缓存是否有效
        /// </summary>
        private bool IsPatientsCacheValid()
        {
            return DateTime.Now.Subtract(_patientsCacheTime).TotalMinutes < PATIENTS_CACHE_DURATION_MINUTES && Patients.Any();
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        private void ClearAllCache()
        {
            _herbsCacheTime = DateTime.MinValue;
            _formulasCacheTime = DateTime.MinValue;
            _patientsCacheTime = DateTime.MinValue;
            _logger.LogInformation("所有缓存已清除");
        }

        #endregion

        /// <summary>
        /// 显示错误消息
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            // 显示输入验证错误消息
            System.Windows.MessageBox.Show(message, "输入验证", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Warning);
        }

        /// <summary>
        /// 显示成功消息
        /// </summary>
        private void ShowSuccessMessage(string message)
        {
            // TODO: 实现成功消息显示
            System.Windows.MessageBox.Show(message, "成功", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
        }

        #endregion
    }
}