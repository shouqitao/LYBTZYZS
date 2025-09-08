using System.Collections.ObjectModel;
using AutoMapper;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Prescriptions.ViewModels
{

    /// <summary>
    /// 处方编辑对话框视图模型
    /// </summary>
    public class PrescriptionEditorDialogViewModel : BindableBase // Temporarily remove IDialogAware due to Prism 9 compatibility issues
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly IPatientService _patientService;
        private readonly IHerbService _herbService;
        private readonly ICustomDialogService _dialogService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly ILogger<PrescriptionEditorDialogViewModel> _logger;
        private readonly IMapper _mapper;

        #region Dialog Properties

        public string Title => IsViewMode ? "查看处方" : (IsEditMode ? "编辑处方" : "新建处方");
        // public event Action<IDialogResult>? RequestClose; // Removed for Prism 9 compatibility
        #endregion Dialog Properties

        #region Properties

        private PrescriptionDto _prescription = new();

        public PrescriptionDto Prescription
        {
            get => _prescription;
            set => SetProperty(ref _prescription, value);
        }

        private ObservableCollection<PrescriptionItemDto> _prescriptionItems = new();

        public ObservableCollection<PrescriptionItemDto> PrescriptionItems
        {
            get => _prescriptionItems;
            set => SetProperty(ref _prescriptionItems, value);
        }

        private PrescriptionItemDto? _selectedItem;

        public PrescriptionItemDto? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private bool _isEditMode;

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private bool _isViewMode;

        public bool IsViewMode
        {
            get => _isViewMode;
            set => SetProperty(ref _isViewMode, value);
        }

        private bool _isCopyMode;

        public bool IsCopyMode
        {
            get => _isCopyMode;
            set => SetProperty(ref _isCopyMode, value);
        }

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // 患者选择相关属性
        private ObservableCollection<PatientDto> _availablePatients = new();

        public ObservableCollection<PatientDto> AvailablePatients
        {
            get => _availablePatients;
            set => SetProperty(ref _availablePatients, value);
        }

        private PatientDto? _selectedPatientFromList;

        public PatientDto? SelectedPatientFromList
        {
            get => _selectedPatientFromList;
            set
            {
                if (SetProperty(ref _selectedPatientFromList, value) && value != null)
                {
                    // 选择患者后自动填充处方信息
                    Prescription.PatientId = value.Id;
                    Prescription.PatientName = value.Name;
                    SaveCommand.RaiseCanExecuteChanged();
                    _logger.LogInformation("从列表选择患者: {PatientName} (ID: {PatientId})", value.Name, value.Id);
                }
            }
        }

        private string _patientSearchKeyword = string.Empty;

        public string PatientSearchKeyword
        {
            get => _patientSearchKeyword;
            set => SetProperty(ref _patientSearchKeyword, value);
        }

        // 上下文模式相关属性
        private bool _isContextMode;

        public bool IsContextMode
        {
            get => _isContextMode;
            set => SetProperty(ref _isContextMode, value);
        }

        private Guid _medicalCaseId = Guid.Empty;

        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private decimal _totalAmount;

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        private int _totalDoses = 1;

        public int TotalDoses
        {
            get => _totalDoses;
            set
            {
                if (SetProperty(ref _totalDoses, value))
                {
                    CalculateTotalAmount();
                }
            }
        }

        #endregion Properties

        #region Commands

        public DelegateCommand SaveCommand { get; } = null!;
        public DelegateCommand CancelCommand { get; } = null!;
        public DelegateCommand AddHerbCommand { get; } = null!;
        public DelegateCommand<PrescriptionItemDto> RemoveHerbCommand { get; } = null!;
        public DelegateCommand<PrescriptionItemDto> EditHerbCommand { get; } = null!;
        public DelegateCommand LoadFormulaTemplateCommand { get; } = null!;
        public DelegateCommand SelectPatientCommand { get; } = null!;
        public DelegateCommand SearchPatientsCommand { get; } = null!;
        public DelegateCommand CreateNewPatientCommand { get; } = null!;
        public DelegateCommand PreviewCommand { get; } = null!;

        #endregion Commands

        #region Constructor

        public PrescriptionEditorDialogViewModel(
            IPrescriptionService prescriptionService,
            IPatientService patientService,
            IHerbService herbService,
            ICustomDialogService dialogService,
            IUserSessionManager userSessionManager,
            ILogger<PrescriptionEditorDialogViewModel> logger,
            IMapper mapper)
        {
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _userSessionManager = userSessionManager ?? throw new ArgumentNullException(nameof(userSessionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SavePrescriptionAsync(), CanSave)
                .ObservesProperty(() => IsViewMode);
            CancelCommand = new DelegateCommand(Cancel);
            AddHerbCommand = new DelegateCommand(async () => await AddHerbAsync(), () => !IsViewMode)
                .ObservesProperty(() => IsViewMode);
            RemoveHerbCommand = new DelegateCommand<PrescriptionItemDto>(RemoveHerb, (item) => !IsViewMode && item != null)
                .ObservesProperty(() => IsViewMode);
            EditHerbCommand = new DelegateCommand<PrescriptionItemDto>(async (item) => await EditHerbAsync(item), (item) => !IsViewMode && item != null)
                .ObservesProperty(() => IsViewMode);
            LoadFormulaTemplateCommand = new DelegateCommand(LoadFormulaTemplate, () => !IsViewMode)
                .ObservesProperty(() => IsViewMode);
            SelectPatientCommand = new DelegateCommand(SelectPatient, () => !IsViewMode)
                .ObservesProperty(() => IsViewMode);
            SearchPatientsCommand = new DelegateCommand(async () => await SearchPatientsAsync());
            CreateNewPatientCommand = new DelegateCommand(async () => await CreateNewPatientAsync(), () => !IsViewMode)
                .ObservesProperty(() => IsViewMode);
            PreviewCommand = new DelegateCommand(async () => await PreviewPrescriptionAsync());

            // 监听处方项目变化
            PrescriptionItems.CollectionChanged += (s, e) => CalculateTotalAmount();

            // Initialize since we can't use OnDialogOpened
            Initialize();

            // 加载患者列表
            _ = Task.Run(async () => await LoadPatientsAsync());
        }

        #endregion Constructor

        #region Dialog Methods (Temporarily disabled due to Prism 9 compatibility)

        // public bool CanCloseDialog() => !IsLoading;

        // public void OnDialogClosed()
        // {
        //     // 清理资源
        // }

        // public void OnDialogOpened(IDialogParameters parameters)
        // {
        //     // 解析参数
        //     if (parameters.ContainsKey("PrescriptionId"))
        //     {
        //         var prescriptionId = parameters.GetValue<Guid>("PrescriptionId");
        //         IsEditMode = parameters.ContainsKey("EditMode") && parameters.GetValue<bool>("EditMode");
        //         IsViewMode = parameters.ContainsKey("ViewMode") && parameters.GetValue<bool>("ViewMode");
        //         Task.Run(async () => await LoadPrescriptionAsync(prescriptionId));
        //     }
        //     else if (parameters.ContainsKey("SourcePrescriptionId"))
        //     {
        //         var sourcePrescriptionId = parameters.GetValue<Guid>("SourcePrescriptionId");
        //         IsCopyMode = true;
        //         Task.Run(async () => await CopyPrescriptionAsync(sourcePrescriptionId));
        //     }
        //     else if (parameters.ContainsKey("PatientId"))
        //     {
        //         var patientId = parameters.GetValue<Guid>("PatientId");
        //         Prescription.PatientId = patientId;
        //         Task.Run(async () => await LoadPatientInfoAsync(patientId));
        //     }
        //     else
        //     {
        //         // 新建模式
        //         InitializeNewPrescription();
        //     }
        // }

        // Initialize on construction for now
        private void Initialize()
        {
            InitializeNewPrescription();
        }

        /// <summary>
        /// 使用上下文参数初始化（支持从医案跳转）
        /// </summary>
        public async Task InitializeWithContextAsync(Dictionary<string, object>? parameters = null)
        {
            if (parameters == null)
            {
                InitializeNewPrescription();
                return;
            }

            try
            {
                // 检查是否是上下文模式（从医案跳转）
                if (parameters.ContainsKey("ContextMode") && parameters["ContextMode"].ToString() == "MedicalCase")
                {
                    IsContextMode = true;

                    // 获取医案ID
                    if (parameters.ContainsKey("MedicalCaseId") && parameters["MedicalCaseId"] is Guid medicalCaseId)
                    {
                        MedicalCaseId = medicalCaseId;
                    }

                    // 获取患者信息
                    if (parameters.ContainsKey("PatientId") && parameters["PatientId"] is Guid patientId)
                    {
                        // 初始化新处方并设置患者信息
                        InitializeNewPrescription();

                        // 加载患者详细信息
                        await LoadPatientInfoAsync(patientId);

                        // 在上下文模式下，不需要加载患者列表
                        StatusMessage = $"正在为患者 {Prescription.PatientName} 开具处方（来自医案 {MedicalCaseId}）";

                        _logger.LogInformation(
                            "上下文模式初始化完成，医案ID: {MedicalCaseId}，患者: {PatientName}",
                            MedicalCaseId, Prescription.PatientName);
                    }
                }
                else
                {
                    // 常规模式
                    InitializeNewPrescription();
                    _ = Task.Run(async () => await LoadPatientsAsync());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化处方编辑器上下文时发生错误");
                StatusMessage = $"初始化失败: {ex.Message}";

                // 回退到常规模式
                InitializeNewPrescription();
            }
        }

        #endregion Dialog Methods (Temporarily disabled due to Prism 9 compatibility)

        #region Methods

        private void InitializeNewPrescription()
        {
            Prescription = new PrescriptionDto
            {
                UserId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty,
                Status = CommonStatus.Enabled, // UltraThink v2.0: 使用CommonStatus，通过业务逻辑映射到处方状态
                DosageCount = 1,
                PrescriptionNo = GeneratePrescriptionNo(),
                CreateTime = DateTime.Now
            };
            TotalDoses = 1;

            _logger.LogInformation("初始化新处方，医生ID: {DoctorId}", Prescription.UserId);
        }

        private string GeneratePrescriptionNo()
        {
            return $"RX{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        private async Task LoadPrescriptionAsync(Guid prescriptionId)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载处方...";

                var result = await _prescriptionService.GetByIdAsync(prescriptionId);
                if (result.IsSuccess && result.Data != null)
                {
                    // 直接使用DTO
                    Prescription = result.Data;

                    // 处方数据已加载

                    // 映射处方项目
                    if (result.Data.Items != null)
                    {
                        var items = result.Data.Items ?? new List<PrescriptionItemDto>();
                        PrescriptionItems = new ObservableCollection<PrescriptionItemDto>(items);
                    }

                    TotalDoses = Prescription.DosageCount;
                    CalculateTotalAmount();
                    StatusMessage = string.Empty;
                }
                else
                {
                    StatusMessage = "加载处方失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                _logger.LogError(ex, "加载处方时出错");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CopyPrescriptionAsync(Guid sourcePrescriptionId)
        {
            await LoadPrescriptionAsync(sourcePrescriptionId);
            if (Prescription != null)
            {
                // 复制处方时重置一些字段
                Prescription.Id = Guid.Empty;
                Prescription.Status = CommonStatus.Enabled; // UltraThink v2.0: 使用CommonStatus
                StatusMessage = "已复制处方内容，请修改后保存";
            }
        }

        private async Task LoadPatientInfoAsync(Guid patientId)
        {
            try
            {
                var result = await _patientService.GetByIdAsync(patientId);
                if (result.IsSuccess && result.Data != null)
                {
                    Prescription.PatientId = patientId;
                    Prescription.PatientName = result.Data.Name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者信息失败");
            }
        }

        private void CalculateTotalAmount()
        {
            TotalAmount = PrescriptionItems.Sum(item => item.Quantity * item.UnitPrice) * TotalDoses;
            // UltraThink v2.0: TotalPrice是计算属性，无需手动赋值
            // 总价会根据Items和DosageCount自动计算
        }

        private bool CanSave()
        {
            return !IsViewMode && !IsLoading &&
                   Prescription.PatientId != Guid.Empty &&
                   PrescriptionItems.Count > 0;
        }

        private async Task SavePrescriptionAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在保存...";

                // 更新处方信息
                Prescription.Items = PrescriptionItems.ToList();
                Prescription.DosageCount = TotalDoses;
                // UltraThink v2.0: TotalPrice是计算属性，无需手动赋值
                if (IsEditMode && Prescription.Id != Guid.Empty)
                {
                    // 直接使用DTO
                    var updateDto = new PrescriptionEditDto
                    {
                        Id = Prescription.Id,
                        PatientId = Prescription.PatientId,
                        DosageCount = Prescription.DosageCount,
                        TotalPrice = Prescription.TotalPrice,
                        Items = PrescriptionItems.Select(item => new PrescriptionItemCreateDto
                        {
                            HerbId = item.HerbId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        }).ToList()
                    };

                    var result = await _prescriptionService.UpdateAsync(Prescription.Id, updateDto);
                    if (result.IsSuccess)
                    {
                        StatusMessage = "处方已更新";
                        await _dialogService.ShowSuccessAsync("处方更新成功", "操作完成");
                        // Note: 对话框通过ShowSuccessAsync自动关闭
                    }
                    else
                    {
                        StatusMessage = "更新失败";
                        await _dialogService.ShowErrorAsync("处方更新失败", "错误");
                    }
                }
                else
                {
                    // 直接使用DTO
                    var createDto = new PrescriptionCreateDto
                    {
                        PatientId = Prescription.PatientId,
                        DoctorId = Prescription.UserId, // UltraThink v2.0: 使用正确的属性名
                        DosageCount = Prescription.DosageCount,
                        TotalAmount = TotalAmount,
                        Items = PrescriptionItems.Select(item => new PrescriptionItemCreateDto
                        {
                            HerbId = item.HerbId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        }).ToList()
                    };

                    // UltraThink v2.0: 在上下文模式下关联医案（如果支持）
                    if (IsContextMode && MedicalCaseId != Guid.Empty)
                    {
                        _logger.LogInformation("处方将关联医案: {MedicalCaseId}", MedicalCaseId);
                        // 注意：需要检查PrescriptionCreateDto是否有MedicalCaseId属性
                        // 如果没有，可以在处方备注中记录医案关联
                        // createDto.MedicalCaseId = MedicalCaseId; // 待DTO支持
                    }

                    var result = await _prescriptionService.CreateAsync(createDto);
                    if (result.IsSuccess)
                    {
                        StatusMessage = "处方已创建";
                        await _dialogService.ShowSuccessAsync("处方创建成功", "操作完成");
                        // Note: 对话框通过ShowSuccessAsync自动关闭
                    }
                    else
                    {
                        StatusMessage = "创建失败";
                        await _dialogService.ShowErrorAsync("处方创建失败", "错误");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
                _logger.LogError(ex, "保存处方时出错");
                await _dialogService.ShowErrorAsync($"保存失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            // Note: 取消逻辑通过ViewModel事件处理，无需Prism对话框支持
        }

        private async Task AddHerbAsync()
        {
            try
            {
                _logger.LogInformation("打开药材选择对话框");

                var parameters = new Dictionary<string, object>
                {
                    ["Title"] = "选择药材"
                };

                var result = await _dialogService.ShowDialogAsync("HerbSelectionDialog", parameters);

                if (result.Result == true && result.Data is Dictionary<string, object> data)
                {
                    if (data.ContainsKey("SelectedItem") && data["SelectedItem"] is PrescriptionItemDto selectedItem)
                    {
                        // 生成新的ID和计算金额
                        selectedItem.Id = Guid.NewGuid();

                        // 添加到处方项目列表
                        PrescriptionItems.Add(selectedItem);
                        CalculateTotalAmount();

                        StatusMessage = $"已添加药材: {selectedItem.HerbName}";
                        _logger.LogInformation(
                            "添加药材成功: {HerbName}, 数量: {Quantity}",
                            selectedItem.HerbName, selectedItem.Quantity);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材时发生错误");
                StatusMessage = $"添加药材失败: {ex.Message}";
                await _dialogService.ShowErrorAsync($"添加药材失败: {ex.Message}", "错误");
            }
        }

        private void RemoveHerb(PrescriptionItemDto? item)
        {
            if (item != null)
            {
                PrescriptionItems.Remove(item);
                CalculateTotalAmount();
            }
        }

        private async Task EditHerbAsync(PrescriptionItemDto? item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("编辑药材: {HerbName}", item.HerbName);

                var parameters = new Dictionary<string, object>
                {
                    ["Title"] = "编辑药材",
                    ["EditMode"] = true,
                    ["HerbId"] = item.HerbId,
                    ["Quantity"] = item.Quantity,
                    ["Unit"] = item.Unit
                };

                var result = await _dialogService.ShowDialogAsync("HerbSelectionDialog", parameters);

                if (result.Result == true && result.Data is Dictionary<string, object> data)
                {
                    if (data.ContainsKey("SelectedItem") && data["SelectedItem"] is PrescriptionItemDto editedItem)
                    {
                        // 保持原有的ID
                        editedItem.Id = item.Id;

                        // 找到并替换原项目
                        var index = PrescriptionItems.IndexOf(item);
                        if (index >= 0)
                        {
                            PrescriptionItems[index] = editedItem;
                            CalculateTotalAmount();

                            StatusMessage = $"已更新药材: {editedItem.HerbName}";
                            _logger.LogInformation(
                                "编辑药材成功: {HerbName}, 数量: {Quantity}",
                                editedItem.HerbName, editedItem.Quantity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑药材时发生错误");
                StatusMessage = $"编辑药材失败: {ex.Message}";
                await _dialogService.ShowErrorAsync($"编辑药材失败: {ex.Message}", "错误");
            }
        }

        private void LoadFormulaTemplate()
        {
            // Note: 验方模板选择功能已通过FormulaSelectionDialog实现
            // 验方模板加载逻辑通过导航和事件处理实现
            //         {
            //             foreach (var formulaItem in formula.Items)
            //             {
            //                 var item = new PrescriptionItemInfo
            //                 {
            //                     HerbId = formulaItem.HerbId,
            //                     HerbName = formulaItem.HerbName,
            //                     Specification = formulaItem.Specification,
            //                     Unit = formulaItem.Unit,
            //                     Quantity = formulaItem.Quantity,
            //                     UnitPrice = formulaItem.UnitPrice,
            //                     Amount = formulaItem.Quantity * formulaItem.UnitPrice
            //                 };
            //                 PrescriptionItems.Add(item);
            //             }
            //             CalculateTotalAmount();
            //             StatusMessage = $"已加载验方模板: {formula.Name}";
            //         }
            //     }
            // });
        }

        private void SelectPatient()
        {
            // 这个方法用于UI绑定，实际选择通过SelectedPatientFromList属性处理
            StatusMessage = "请从患者列表中选择，或搜索患者";
        }

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "加载患者列表...";

                // 获取活跃患者列表
                var result = await _patientService.SearchAsync(string.Empty); // 获取所有活跃患者
                if (result.IsSuccess && result.Data != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AvailablePatients.Clear();
                        foreach (var patientDto in result.Data)
                        {
                            AvailablePatients.Add(patientDto);
                        }
                    });

                    _logger.LogInformation("患者列表加载完成，共 {Count} 个患者", result.Data.Count);
                }
                else
                {
                    StatusMessage = $"加载患者列表失败: {result.ErrorMessage}";
                    _logger.LogError("加载患者列表失败: {ErrorMessage}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载患者列表时发生错误: {ex.Message}";
                _logger.LogError(ex, "加载患者列表时发生错误");
            }
            finally
            {
                IsLoading = false;
                if (StatusMessage.Contains("加载患者列表"))
                {
                    StatusMessage = string.Empty;
                }
            }
        }

        private async Task SearchPatientsAsync()
        {
            if (string.IsNullOrWhiteSpace(PatientSearchKeyword))
            {
                await LoadPatientsAsync();
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "搜索患者...";

                var result = await _patientService.SearchAsync(PatientSearchKeyword);
                if (result.IsSuccess && result.Data != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AvailablePatients.Clear();
                        foreach (var patientDto in result.Data)
                        {
                            AvailablePatients.Add(patientDto);
                        }
                    });

                    StatusMessage = $"找到 {result.Data.Count} 个匹配的患者";
                    _logger.LogInformation(
                        "患者搜索完成，关键词: {Keyword}，结果: {Count}",
                        PatientSearchKeyword, result.Data.Count);
                }
                else
                {
                    StatusMessage = $"搜索患者失败: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"搜索患者时发生错误: {ex.Message}";
                _logger.LogError(ex, "搜索患者时发生错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CreateNewPatientAsync()
        {
            try
            {
                _logger.LogInformation("打开新建患者对话框");

                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = false
                };

                var result = await _dialogService.ShowDialogAsync("PatientAddEditDialog", parameters);

                if (result.Result == true)
                {
                    _logger.LogInformation("患者创建成功，刷新患者列表");

                    // 刷新患者列表
                    await LoadPatientsAsync();

                    // 如果有返回的患者数据，自动选择该患者
                    if (result.Data is Dictionary<string, object> data && data.ContainsKey("Patient") && data["Patient"] is PatientDto newPatient)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SelectedPatientFromList = newPatient;
                        });

                        _logger.LogInformation("已自动选择新创建的患者: {PatientName}", newPatient.Name);
                    }

                    await _dialogService.ShowSuccessAsync("患者创建成功", "成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者时发生错误");
                await _dialogService.ShowErrorAsync($"创建患者失败: {ex.Message}", "错误");
            }
        }

        private async Task PreviewPrescriptionAsync()
        {
            try
            {
                if (Prescription.PatientId == Guid.Empty || PrescriptionItems.Count == 0)
                {
                    await _dialogService.ShowWarningAsync("请先选择患者并添加药材后再进行预览", "预览提醒");
                    return;
                }

                StatusMessage = "正在生成处方预览...";

                // 创建打印数据
                var printData = CreatePrintData();

                // 显示预览对话框
                var parameters = new Dictionary<string, object>
                {
                    ["PrintData"] = printData,
                    ["Title"] = "处方预览"
                };

                // 简单的文本预览实现
                await _dialogService.ShowInformationAsync(printData, "处方预览");

                StatusMessage = "预览完成";
                _logger.LogInformation("处方预览成功，处方号: {PrescriptionNo}", Prescription.PrescriptionNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成处方预览时发生错误");
                StatusMessage = $"预览失败: {ex.Message}";
                await _dialogService.ShowErrorAsync($"预览失败: {ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 创建打印数据
        /// </summary>
        private string CreatePrintData()
        {
            var sb = new System.Text.StringBuilder();

            // 处方头部信息
            sb.AppendLine("=============================================");
            sb.AppendLine("                中医处方                    ");
            sb.AppendLine("=============================================");
            sb.AppendLine();

            // 基础信息
            sb.AppendLine($"处方编号：{Prescription.PrescriptionNo}");
            sb.AppendLine($"患者姓名：{Prescription.PatientName}");
            sb.AppendLine($"开方日期：{Prescription.CreateTime:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"医生：{_userSessionManager.CurrentUser?.RealName ?? "未知"}");
            sb.AppendLine();

            // 药材列表
            sb.AppendLine("药材明细：");
            sb.AppendLine("---------------------------------------------");
            sb.AppendLine("药材名称\t\t规格\t\t数量\t\t单价\t\t金额");
            sb.AppendLine("---------------------------------------------");

            decimal totalItemAmount = 0;
            foreach (var item in PrescriptionItems)
            {
                var amount = item.Quantity * item.UnitPrice;
                totalItemAmount += amount;

                sb.AppendLine($"{item.HerbName?.PadRight(12) ?? "未知".PadRight(12)}\t" +
                             $"{(item.Usage ?? string.Empty).PadRight(8)}\t" +
                             $"{item.Quantity}\t\t" +
                             $"¥{item.UnitPrice:F2}\t\t" +
                             $"¥{amount:F2}");
            }

            sb.AppendLine("---------------------------------------------");
            sb.AppendLine($"单次金额：¥{totalItemAmount:F2}");
            sb.AppendLine($"药付数：{TotalDoses} 付");
            sb.AppendLine($"总金额：¥{TotalAmount:F2}");
            sb.AppendLine();

            // 用法用量（如果有）
            if (!string.IsNullOrEmpty(Prescription.Usage))
            {
                sb.AppendLine("用法用量：");
                sb.AppendLine(Prescription.Usage);
                sb.AppendLine();
            }

            // 医嘱（如果有）
            if (!string.IsNullOrEmpty(Prescription.Advice))
            {
                sb.AppendLine("医嘱：");
                sb.AppendLine(Prescription.Advice);
                sb.AppendLine();
            }

            // 备注（如果有）
            if (!string.IsNullOrEmpty(Prescription.Remark))
            {
                sb.AppendLine("备注：");
                sb.AppendLine(Prescription.Remark);
                sb.AppendLine();
            }

            sb.AppendLine("=============================================");
            sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return sb.ToString();
        }

        #endregion Methods
    }
}
