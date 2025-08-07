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
        
        #endregion
        
        #region 依赖服务
        
        private readonly IPatientsApiService _patientsApiService;
        private readonly IConsultationApiService _consultationApiService;
        private readonly IFormulaTemplateApiService _formulaApiService;
        private readonly IHerbService _herbService;
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
            ILogger<ConsultationMainViewModel> logger,
            IMapper mapper)
        {
            _patientsApiService = patientsApiService;
            _consultationApiService = consultationApiService;
            _formulaApiService = formulaApiService;
            _herbService = herbService;
            _logger = logger;
            _mapper = mapper;

            // 初始化命令
            RefreshCommand = new DelegateCommand(async () => await LoadPatientsAsync());
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

        private async Task LoadPatientsAsync()
        {
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
                    
                    _logger.LogInformation($"成功加载 {Patients.Count} 个患者");
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
                
                _logger.LogInformation($"成功加载 {herbs.Count} 种可用药材");
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
        /// 根据搜索关键词过滤药材
        /// </summary>
        private void FilterHerbs()
        {
            AvailableHerbs.Clear();
            
            if (string.IsNullOrWhiteSpace(HerbSearchKeyword))
            {
                // 没有搜索关键词时显示所有药材
                foreach (var herb in _allHerbs)
                {
                    AvailableHerbs.Add(herb);
                }
            }
            else
            {
                // 根据关键词过滤
                var filteredHerbs = _allHerbs.Where(h => 
                    h.Name.Contains(HerbSearchKeyword, StringComparison.OrdinalIgnoreCase));
                
                foreach (var herb in filteredHerbs)
                {
                    AvailableHerbs.Add(herb);
                }
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
        }

        private async Task SaveConsultationAsync()
        {
            if (CurrentConsultation == null) return;

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
                    ShowSuccessMessage("看诊记录保存成功");
                    _logger.LogInformation($"成功保存看诊记录: {CurrentConsultation.Id}");
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
        private void PrintPrescription()
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
                // 构建处方内容
                var prescriptionContent = BuildPrescriptionContent();
                
                // 调用打印预览
                ShowPrintPreview(prescriptionContent);
                
                _logger.LogInformation($"处方打印预览：患者 {CurrentConsultation.PatientName}，共 {PrescriptionItems.Count} 味药材");
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
        }

        /// <summary>
        /// 构建处方打印内容
        /// </summary>
        private string BuildPrescriptionContent()
        {
            var content = new System.Text.StringBuilder();
            
            // 诊所抬头
            content.AppendLine("凌隐宝堂中医诊所");
            content.AppendLine("═════════════════════════════════════");
            content.AppendLine();
            
            // 患者信息
            content.AppendLine($"患者姓名：{CurrentConsultation?.PatientName}");
            content.AppendLine($"看诊时间：{DateTime.Now:yyyy年MM月dd日 HH:mm}");
            content.AppendLine($"医生：{CurrentConsultation?.DoctorName ?? "当前医生"}");
            content.AppendLine();
            
            // 中医诊断
            if (!string.IsNullOrWhiteSpace(CurrentConsultation?.TCMDiagnosis))
            {
                content.AppendLine($"中医诊断：{CurrentConsultation.TCMDiagnosis}");
                content.AppendLine();
            }
            
            // 治疗原则
            if (!string.IsNullOrWhiteSpace(CurrentConsultation?.TreatmentPrinciple))
            {
                content.AppendLine($"治疗原则：{CurrentConsultation.TreatmentPrinciple}");
                content.AppendLine();
            }
            
            // 处方内容
            content.AppendLine("处方：");
            content.AppendLine("─────────────────────────────────────");
            
            int index = 1;
            foreach (var item in PrescriptionItems)
            {
                content.AppendLine($"{index,2}. {item.HerbName,-15} {item.Quantity,6}{item.Unit}");
                index++;
            }
            
            content.AppendLine("─────────────────────────────────────");
            content.AppendLine($"共 {PrescriptionItems.Count} 味药材");
            content.AppendLine();
            
            // 用法用量
            content.AppendLine("用法：水煎服，一日一剂，分早晚两次温服。");
            content.AppendLine();
            
            // 医嘱
            if (!string.IsNullOrWhiteSpace(CurrentConsultation?.MedicalAdvice))
            {
                content.AppendLine($"医嘱：{CurrentConsultation.MedicalAdvice}");
                content.AppendLine();
            }
            
            // 备注
            if (!string.IsNullOrWhiteSpace(CurrentConsultation?.Remark))
            {
                content.AppendLine($"备注：{CurrentConsultation.Remark}");
                content.AppendLine();
            }
            
            content.AppendLine("═════════════════════════════════════");
            content.AppendLine($"打印时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            return content.ToString();
        }

        /// <summary>
        /// 显示打印预览
        /// </summary>
        private void ShowPrintPreview(string content)
        {
            // 使用简单的消息框显示打印内容（实际应用中可以使用更专业的打印预览控件）
            System.Windows.MessageBox.Show(content, "处方打印预览", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
            
            // TODO: 在实际部署中，这里可以集成真正的打印功能
            // 例如使用 PrintDocument 或第三方打印组件
        }
        private void AddHerbToPrescription(HerbInfo herb)
        {
            if (herb == null) return;

            var existingItem = PrescriptionItems.FirstOrDefault(p => p.HerbId == herb.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += 10; // 默认增加10单位
            }
            else
            {
                PrescriptionItems.Add(new PrescriptionItemInfo
                {
                    Id = Guid.NewGuid(),
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = 10, // 默认数量
                    Unit = herb.Unit
                });
            }
        }

        private void RemovePrescriptionItem(PrescriptionItemInfo item)
        {
            if (item == null) return;
            PrescriptionItems.Remove(item);
        }

        private void IncreaseQuantity(PrescriptionItemInfo item)
        {
            if (item == null) return;
            item.Quantity += 1;
        }

        private void DecreaseQuantity(PrescriptionItemInfo item)
        {
            if (item == null) return;
            if (item.Quantity > 1)
            {
                item.Quantity -= 1;
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            // TODO: 实现错误消息显示（可以使用MessageBox或更友好的通知方式）
            System.Windows.MessageBox.Show(message, "错误", 
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