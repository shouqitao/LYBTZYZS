using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Prescriptions;
using AutoMapper;
using LYBT.Shared.Interfaces.Services;
using SharedEnums = LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Mvvm;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;

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
        private readonly ILogger<PrescriptionEditorDialogViewModel> _logger;
        private readonly IMapper _mapper;

        #region Dialog Properties

        public string Title => IsViewMode ? "查看处方" : (IsEditMode ? "编辑处方" : "新建处方");
        // public event Action<IDialogResult>? RequestClose; // Removed for Prism 9 compatibility

        #endregion

        #region Properties

        private PrescriptionInfo _prescription = new();
        public PrescriptionInfo Prescription
        {
            get => _prescription;
            set => SetProperty(ref _prescription, value);
        }

        private ObservableCollection<PrescriptionItemInfo> _prescriptionItems = new();
        public ObservableCollection<PrescriptionItemInfo> PrescriptionItems
        {
            get => _prescriptionItems;
            set => SetProperty(ref _prescriptionItems, value);
        }

        private PrescriptionItemInfo? _selectedItem;
        public PrescriptionItemInfo? SelectedItem
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

        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand<PrescriptionItemInfo> RemoveHerbCommand { get; }
        public DelegateCommand<PrescriptionItemInfo> EditHerbCommand { get; }
        public DelegateCommand LoadFormulaTemplateCommand { get; }
        public DelegateCommand SelectPatientCommand { get; }
        public DelegateCommand PreviewCommand { get; }

        #endregion

        #region Constructor

        public PrescriptionEditorDialogViewModel(
            IPrescriptionService prescriptionService,
            IPatientService patientService,
            IHerbService herbService,
            ICustomDialogService dialogService,
            ILogger<PrescriptionEditorDialogViewModel> logger,
            IMapper mapper)
        {
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SavePrescriptionAsync(), CanSave)
                .ObservesProperty(() => IsViewMode);
            CancelCommand = new DelegateCommand(Cancel);
            AddHerbCommand = new DelegateCommand(AddHerb, () => !IsViewMode)
                .ObservesProperty(() => IsViewMode);
            RemoveHerbCommand = new DelegateCommand<PrescriptionItemInfo>(RemoveHerb, (item) => !IsViewMode && item != null)
                .ObservesProperty(() => IsViewMode);
            EditHerbCommand = new DelegateCommand<PrescriptionItemInfo>(EditHerb, (item) => !IsViewMode && item != null)
                .ObservesProperty(() => IsViewMode);
            LoadFormulaTemplateCommand = new DelegateCommand(LoadFormulaTemplate, () => !IsViewMode)
                .ObservesProperty(() => IsViewMode);
            SelectPatientCommand = new DelegateCommand(SelectPatient, () => !IsViewMode)
                .ObservesProperty(() => IsViewMode);
            PreviewCommand = new DelegateCommand(PreviewPrescription);

            // 监听处方项目变化
            PrescriptionItems.CollectionChanged += (s, e) => CalculateTotalAmount();
            
            // Initialize since we can't use OnDialogOpened
            Initialize();
        }

        #endregion

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

        #endregion

        #region Methods

        private void InitializeNewPrescription()
        {
            Prescription = new PrescriptionInfo
            {
                PrescriptionNumber = GeneratePrescriptionNo(),
                CreateTime = DateTime.Now,
                UserId = Guid.Empty, // TODO: 从当前登录用户获取
                Status = SharedEnums.PrescriptionStatus.Draft,
                DosageCount = 1
            };
            TotalDoses = 1;
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
                    // UltraThink四层架构：使用AutoMapper转换DTO → Info
                    Prescription = _mapper.Map<PrescriptionInfo>(result.Data);
                    
                    // 确保处方编号存在
                    if (string.IsNullOrEmpty(Prescription.PrescriptionNumber))
                    {
                        Prescription.PrescriptionNumber = GeneratePrescriptionNo();
                    }
                    
                    // 映射处方项目
                    if (result.Data.Items != null)
                    {
                        var items = _mapper.Map<List<PrescriptionItemInfo>>(result.Data.Items);
                        PrescriptionItems = new ObservableCollection<PrescriptionItemInfo>(items);
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
                Prescription.PrescriptionNumber = GeneratePrescriptionNo();
                Prescription.CreateTime = DateTime.Now;
                Prescription.Status = SharedEnums.PrescriptionStatus.Draft;
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
            // TotalAmount is read-only, update TotalPrice instead
            Prescription.TotalPrice = TotalAmount;
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
                Prescription.TotalPrice = TotalAmount;

                if (IsEditMode && Prescription.Id != Guid.Empty)
                {
                    // UltraThink四层架构：使用AutoMapper转换Info → DTO
                    var updateDto = _mapper.Map<PrescriptionEditDto>(Prescription);
                    updateDto.Items = _mapper.Map<List<PrescriptionItemCreateDto>>(PrescriptionItems);
                    
                    var result = await _prescriptionService.UpdateAsync(Prescription.Id, updateDto);
                    if (result.IsSuccess)
                    {
                        StatusMessage = "处方已更新";
                        await _dialogService.ShowSuccessAsync("处方更新成功", "操作完成");
                        // TODO: Close dialog with success
                    }
                    else
                    {
                        StatusMessage = "更新失败";
                        await _dialogService.ShowErrorAsync("处方更新失败", "错误");
                    }
                }
                else
                {
                    // UltraThink四层架构：使用AutoMapper转换Info → DTO
                    var createDto = _mapper.Map<PrescriptionCreateDto>(Prescription);
                    createDto.Items = _mapper.Map<List<PrescriptionItemCreateDto>>(PrescriptionItems);
                    createDto.TotalAmount = TotalAmount;
                    
                    var result = await _prescriptionService.CreateAsync(createDto);
                    if (result.IsSuccess)
                    {
                        StatusMessage = "处方已创建";
                        await _dialogService.ShowSuccessAsync("处方创建成功", "操作完成");
                        // TODO: Close dialog with success
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
            // TODO: Implement dialog cancel logic when Prism dialog support is added
            // RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private void AddHerb()
        {
            // TODO: Implement dialog logic when Prism dialog support is added
            // _dialogService.ShowDialog("HerbSelectionDialog", new DialogParameters(), (result) =>
            // {
            //     if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("SelectedHerb"))
            //     {
            //         var herb = result.Parameters.GetValue<HerbInfo>("SelectedHerb");
            //         var quantity = result.Parameters.GetValue<decimal>("Quantity");
                    
            //         var item = new PrescriptionItemInfo
            //         {
            //             HerbId = herb.Id,
            //             HerbName = herb.Name,
            //             Specification = herb.Specification,
            //             Unit = herb.Unit,
            //             Quantity = quantity,
            //             UnitPrice = herb.Price,
            //             Amount = quantity * herb.Price
            //         };

            //         PrescriptionItems.Add(item);
            //         CalculateTotalAmount();
            //     }
            // });
        }

        private void RemoveHerb(PrescriptionItemInfo? item)
        {
            if (item != null)
            {
                PrescriptionItems.Remove(item);
                CalculateTotalAmount();
            }
        }

        private void EditHerb(PrescriptionItemInfo? item)
        {
            if (item == null) return;

            // TODO: Implement dialog logic when Prism dialog support is added
            // var parameters = new DialogParameters
            // {
            //     { "HerbItem", item },
            //     { "EditMode", true }
            // };

            // _dialogService.ShowDialog("HerbSelectionDialog", parameters, (result) =>
            // {
            //     if (result.Result == ButtonResult.OK)
            //     {
            //         CalculateTotalAmount();
            //     }
            // });
        }

        private void LoadFormulaTemplate()
        {
            // TODO: Implement dialog logic when Prism dialog support is added
            // _dialogService.ShowDialog("FormulaTemplateDialog", new DialogParameters(), (result) =>
            // {
            //     if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("SelectedFormula"))
            //     {
            //         var formula = result.Parameters.GetValue<FormulaInfo>("SelectedFormula");
            //         // 加载验方模板中的药材
            //         if (formula.Items != null)
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
            // TODO: 实现患者选择对话框
            StatusMessage = "患者选择功能待实现";
        }

        private void PreviewPrescription()
        {
            // TODO: 实现处方预览功能
            StatusMessage = "处方预览功能待实现";
        }

        #endregion
    }
}