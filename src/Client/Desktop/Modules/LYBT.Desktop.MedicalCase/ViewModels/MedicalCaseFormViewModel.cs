using System.Collections.ObjectModel;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医案表单ViewModel
    /// Epic #2175 BF-002 Phase 3 - 一体化病案编辑界面
    /// Task 3.2: 实现状态管理（IsConsultationCompleted/CanEditPrescription）
    /// Task 3.3: 实现SaveDraftCommand自动化流程
    /// </summary>
    public class MedicalCaseFormViewModel : UnifiedViewModelBase
    {
        private readonly MedicalCaseDataManager _dataManager;
        private readonly IContainerProvider _containerProvider;
        private readonly ObservableCollection<HerbDto> _allHerbs = new();

        #region 构造函数

        public MedicalCaseFormViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            MedicalCaseDataManager dataManager,
            IContainerProvider containerProvider,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
            PageTitle = "病案编辑";

            // 初始化集合
            PrescriptionItems = new ObservableCollection<PrescriptionItemViewModel>();

            // 初始化Commands
            SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft, CanExecuteSaveDraft);
            SaveAndCompleteCommand = new DelegateCommand(ExecuteSaveAndComplete, CanExecuteSaveAndComplete);
            AddHerbCommand = new DelegateCommand(ExecuteAddHerb, CanExecuteAddHerb);
            RemoveItemCommand = new DelegateCommand<PrescriptionItemViewModel>(ExecuteRemoveItem);
            ImportFormulaCommand = new DelegateCommand(ExecuteImportFormula, CanExecuteImportFormula);
            ImportHistoryCommand = new DelegateCommand(ExecuteImportHistory, CanExecuteImportHistory);
            DosageCompletedCommand = new DelegateCommand<PrescriptionItemViewModel>(ExecuteDosageCompleted);
        }

        #endregion

        #region 医案基础属性

        private Guid _medicalCaseId;
        /// <summary>
        /// 医案ID
        /// </summary>
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private Guid _patientId;
        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId
        {
            get => _patientId;
            set => SetProperty(ref _patientId, value);
        }

        private Guid _doctorId;
        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid DoctorId
        {
            get => _doctorId;
            set => SetProperty(ref _doctorId, value);
        }

        private DateTime? _step1CompletedAt;
        /// <summary>
        /// Step1完成时间
        /// </summary>
        public DateTime? Step1CompletedAt
        {
            get => _step1CompletedAt;
            set => SetProperty(ref _step1CompletedAt, value);
        }

        private DateTime? _step2CompletedAt;
        /// <summary>
        /// Step2完成时间
        /// </summary>
        public DateTime? Step2CompletedAt
        {
            get => _step2CompletedAt;
            set => SetProperty(ref _step2CompletedAt, value);
        }

        #endregion

        #region 诊断区属性 (Step 1: 辨证)

        private string _chiefComplaint = string.Empty;
        /// <summary>
        /// 主诉
        /// </summary>
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set
            {
                if (SetProperty(ref _chiefComplaint, value))
                {
                    RaisePropertyChanged(nameof(IsConsultationCompleted));
                    RaisePropertyChanged(nameof(CanEditPrescription));
                    SaveDraftCommand.RaiseCanExecuteChanged();
                    SaveAndCompleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _tcmDiagnosis = string.Empty;
        /// <summary>
        /// 中医诊断
        /// </summary>
        public string TCMDiagnosis
        {
            get => _tcmDiagnosis;
            set
            {
                if (SetProperty(ref _tcmDiagnosis, value))
                {
                    RaisePropertyChanged(nameof(IsConsultationCompleted));
                    RaisePropertyChanged(nameof(CanEditPrescription));
                    SaveDraftCommand.RaiseCanExecuteChanged();
                    SaveAndCompleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _inspection = string.Empty;
        /// <summary>
        /// 望诊
        /// </summary>
        public string Inspection
        {
            get => _inspection;
            set => SetProperty(ref _inspection, value);
        }

        private string _auscultation = string.Empty;
        /// <summary>
        /// 闻诊
        /// </summary>
        public string Auscultation
        {
            get => _auscultation;
            set => SetProperty(ref _auscultation, value);
        }

        private string _inquiry = string.Empty;
        /// <summary>
        /// 问诊
        /// </summary>
        public string Inquiry
        {
            get => _inquiry;
            set => SetProperty(ref _inquiry, value);
        }

        private string _pulseDiagnosis = string.Empty;
        /// <summary>
        /// 切诊（脉象）
        /// </summary>
        public string PulseDiagnosis
        {
            get => _pulseDiagnosis;
            set => SetProperty(ref _pulseDiagnosis, value);
        }

        #endregion

        #region 处方需求标记 (Step 2: 标记处方需求)

        private bool? _needsPrescription;
        /// <summary>
        /// 是否需要开处方
        /// Epic #2175 BF-002: 三态语义
        /// null: 未标记
        /// true: 需要开处方
        /// false: 不需要开处方
        /// </summary>
        public bool? NeedsPrescription
        {
            get => _needsPrescription;
            set
            {
                if (SetProperty(ref _needsPrescription, value))
                {
                    RaisePropertyChanged(nameof(NeedsPrescriptionTrue));
                    RaisePropertyChanged(nameof(NeedsPrescriptionFalse));
                    RaisePropertyChanged(nameof(IsPrescriptionFlagSet));
                    RaisePropertyChanged(nameof(CanEditPrescription));
                    SaveAndCompleteCommand.RaiseCanExecuteChanged();
                    AddHerbCommand.RaiseCanExecuteChanged();
                    ImportFormulaCommand.RaiseCanExecuteChanged();
                    ImportHistoryCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// RadioButton辅助属性 - "需要开处方"
        /// </summary>
        public bool NeedsPrescriptionTrue
        {
            get => NeedsPrescription == true;
            set
            {
                if (value)
                {
                    NeedsPrescription = true;
                }
            }
        }

        /// <summary>
        /// RadioButton辅助属性 - "不需要开处方"
        /// </summary>
        public bool NeedsPrescriptionFalse
        {
            get => NeedsPrescription == false;
            set
            {
                if (value)
                {
                    NeedsPrescription = false;
                }
            }
        }

        #endregion

        #region 处方区属性 (Step 3: 开具处方)

        /// <summary>
        /// 处方药材列表
        /// Epic #2175 BF-002 Task 3.5 - 使用PrescriptionItemViewModel
        /// </summary>
        public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems { get; }

        /// <summary>
        /// 所有药材列表 - Epic #2175 BF-002 Task 3.6: 用于拼音过滤
        /// 在OnNavigatedTo时加载
        /// </summary>
        public ObservableCollection<HerbDto> AllHerbs => _allHerbs;

        private int _dosageCount = 1;
        /// <summary>
        /// 剂数
        /// </summary>
        public int DosageCount
        {
            get => _dosageCount;
            set
            {
                if (SetProperty(ref _dosageCount, value))
                {
                    RaisePropertyChanged(nameof(SubTotal));
                    RaisePropertyChanged(nameof(TotalAmount));
                }
            }
        }

        private decimal _discount = 1.0m;
        /// <summary>
        /// 折扣（0.00-1.00）
        /// </summary>
        public decimal Discount
        {
            get => _discount;
            set
            {
                if (SetProperty(ref _discount, value))
                {
                    RaisePropertyChanged(nameof(TotalAmount));
                }
            }
        }

        /// <summary>
        /// 小计（单剂总价）
        /// Epic #2175 BF-002 Task 3.5 - 自动计算处方药材总价
        /// </summary>
        public decimal SubTotal
        {
            get
            {
                return PrescriptionItems.Sum(item => item.ItemAmount);
            }
        }

        /// <summary>
        /// 总计（小计 × 剂数 × 折扣）
        /// </summary>
        public decimal TotalAmount
        {
            get
            {
                return SubTotal * DosageCount * Discount;
            }
        }

        #endregion

        #region 状态计算属性 (Epic #2175 BF-002)

        /// <summary>
        /// Step 1是否完成：辨证信息是否已填写
        /// 判断标准：主诉和中医诊断均不为空
        /// </summary>
        public bool IsConsultationCompleted
        {
            get => !string.IsNullOrWhiteSpace(ChiefComplaint) &&
                   !string.IsNullOrWhiteSpace(TCMDiagnosis);
        }

        /// <summary>
        /// Step 2是否完成：处方需求标记是否已设置
        /// 判断标准：NeedsPrescription不为null
        /// </summary>
        public bool IsPrescriptionFlagSet
        {
            get => NeedsPrescription.HasValue;
        }

        /// <summary>
        /// 是否可以编辑处方（Epic #2175 BF-002核心业务规则）
        /// 条件：
        /// 1. Step 1完成（辨证信息已填写）
        /// 2. Step 2完成（处方需求已标记）
        /// 3. NeedsPrescription == true（明确需要开处方）
        /// </summary>
        public bool CanEditPrescription
        {
            get => IsConsultationCompleted &&
                   IsPrescriptionFlagSet &&
                   NeedsPrescription == true;
        }

        #endregion

        #region Commands

        /// <summary>
        /// 保存草稿Command
        /// Task 3.3: 实现自动化流程（自动完成Step1/Step2）
        /// </summary>
        public DelegateCommand SaveDraftCommand { get; }

        /// <summary>
        /// 保存并完成Command
        /// </summary>
        public DelegateCommand SaveAndCompleteCommand { get; }

        /// <summary>
        /// 添加药材Command
        /// </summary>
        public DelegateCommand AddHerbCommand { get; }

        /// <summary>
        /// 删除药材Command
        /// </summary>
        public DelegateCommand<PrescriptionItemViewModel> RemoveItemCommand { get; }

        /// <summary>
        /// 导入经验方Command
        /// TODO: Task 3.8 - 实现经验方导入对话框
        /// </summary>
        public DelegateCommand ImportFormulaCommand { get; }

        /// <summary>
        /// 导入历史处方Command
        /// TODO: Task 3.9 - 实现历史处方导入对话框
        /// </summary>
        public DelegateCommand ImportHistoryCommand { get; }

        /// <summary>
        /// 剂量完成Command - Epic #2175 BF-002 Task 3.7
        /// 在HerbCardControl中输入剂量后按Enter触发
        /// 处理重复药材检测等逻辑
        /// </summary>
        public DelegateCommand<PrescriptionItemViewModel> DosageCompletedCommand { get; }

        #endregion

        #region Command实现

        private bool CanExecuteSaveDraft()
        {
            // Task 3.3: 至少需要填写主诉或诊断
            return !string.IsNullOrWhiteSpace(ChiefComplaint) ||
                   !string.IsNullOrWhiteSpace(TCMDiagnosis);
        }

        private async void ExecuteSaveDraft()
        {
            try
            {
                Logger.LogInformation("开始执行保存草稿: MedicalCaseId={MedicalCaseId}", MedicalCaseId);

                // Epic #2175 BF-002 自动化流程
                // Step 1: 自动完成Step1（如果辨证信息完整且Step1未完成）
                if (IsConsultationCompleted && Step1CompletedAt == null)
                {
                    Logger.LogInformation("自动完成Step1: 辨证信息已完整");
                    var step1Request = new CompleteStep1Request
                    {
                        PrescriptionEnabled = true
                    };
                    var step1Response = await _dataManager.CompleteStep1Async(MedicalCaseId, step1Request);

                    if (step1Response.Success && step1Response.Data != null)
                    {
                        Step1CompletedAt = step1Response.Data.Step1CompletedAt;
                        Logger.LogInformation("Step1完成: CompletedAt={CompletedAt}", Step1CompletedAt);
                    }
                    else
                    {
                        Logger.LogWarning("Step1完成失败: {Message}", step1Response.Message);
                    }
                }

                // Step 2: 自动完成Step2（如果处方需求已标记且Step2未完成）
                if (IsPrescriptionFlagSet && Step2CompletedAt == null)
                {
                    Logger.LogInformation("自动完成Step2: 处方需求已标记 NeedsPrescription={NeedsPrescription}", NeedsPrescription);
                    var step2Request = new SetPrescriptionFlagRequest
                    {
                        NeedsPrescription = NeedsPrescription!.Value
                    };
                    var step2Response = await _dataManager.SetPrescriptionFlagAsync(MedicalCaseId, step2Request);

                    if (step2Response.Success)
                    {
                        // 重新加载以获取Step2CompletedAt
                        await _dataManager.InitializeAsync(MedicalCaseId);
                        Step2CompletedAt = _dataManager.CurrentConsultation?.Step2CompletedAt;
                        Logger.LogInformation("Step2完成: CompletedAt={CompletedAt}", Step2CompletedAt);
                    }
                    else
                    {
                        Logger.LogWarning("Step2完成失败: {Message}", step2Response.Message);
                    }
                }

                // Step 3: 保存病案数据
                var saveRequest = new MedicalCaseInputDto
                {
                    Id = MedicalCaseId,
                    PatientId = PatientId,
                    DoctorId = DoctorId,
                    ChiefComplaint = ChiefComplaint,
                    TCMDiagnosis = TCMDiagnosis,
                    Inspection = Inspection,
                    Auscultation = Auscultation,
                    Inquiry = Inquiry,
                    Palpation = PulseDiagnosis
                };

                var saveResponse = await _dataManager.SaveAsDraftAsync(MedicalCaseId, saveRequest);

                if (saveResponse.Success)
                {
                    Logger.LogInformation("保存草稿成功");
                    await UserNotificationService?.ShowSuccessAsync("保存成功")!;
                }
                else
                {
                    Logger.LogWarning("保存草稿失败: {Message}", saveResponse.Message);
                    await UserNotificationService?.ShowWarningAsync($"保存失败: {saveResponse.Message}")!;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存草稿异常");
                await UserNotificationService?.ShowErrorAsync($"保存失败: {ex.Message}")!;
            }
        }

        private bool CanExecuteSaveAndComplete()
        {
            // 必须完成Step1和Step2
            return IsConsultationCompleted && IsPrescriptionFlagSet;
        }

        private void ExecuteSaveAndComplete()
        {
            // TODO: Task 3.3 实现
            Logger.LogInformation("执行保存并完成");
        }

        private bool CanExecuteAddHerb()
        {
            // 必须能编辑处方
            return CanEditPrescription;
        }

        private void ExecuteAddHerb()
        {
            // Epic #2175 BF-002 Task 3.7: 添加新的空药材项
            Logger.LogInformation("执行添加药材");

            var newItem = new PrescriptionItemViewModel(EventAggregator, LoggerFactory)
            {
                AllHerbs = AllHerbs, // 注入药材列表以支持拼音过滤
                Dosage = 10m // 默认剂量10g
            };

            PrescriptionItems.Add(newItem);

            // 触发价格重新计算
            RaisePropertyChanged(nameof(SubTotal));
            RaisePropertyChanged(nameof(TotalAmount));

            Logger.LogInformation("已添加新药材项，当前药材数量: {Count}", PrescriptionItems.Count);
        }

        private void ExecuteRemoveItem(PrescriptionItemViewModel? item)
        {
            if (item != null && PrescriptionItems.Contains(item))
            {
                PrescriptionItems.Remove(item);
                // 触发价格重新计算
                RaisePropertyChanged(nameof(SubTotal));
                RaisePropertyChanged(nameof(TotalAmount));
            }
        }

        private bool CanExecuteImportFormula()
        {
            // 必须能编辑处方
            return CanEditPrescription;
        }

        private void ExecuteImportFormula()
        {
            try
            {
                Logger.LogDebug("打开经验方选择对话框");

                // Task 3.8: 使用IContainerProvider延迟解析IDialogService
                var dialogService = _containerProvider.Resolve<Prism.Services.Dialogs.IDialogService>();

                // 打开经验方选择对话框
                dialogService.ShowDialog(
                    "FormulaSelectionDialog",
                    null,
                    async result =>
                    {
                        if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
                        {
                            // 获取选中的经验方
                            var selectedFormula = result.Parameters.GetValue<Shared.Models.Contracts.Formula.FormulaDto>("SelectedFormula");
                            if (selectedFormula != null)
                            {
                                Logger.LogInformation("选择经验方: {FormulaName} ({FormulaId})",
                                    selectedFormula.Name, selectedFormula.Id);

                                // 调用API导入经验方到处方
                                await ImportFormulaAsync(selectedFormula.Id);
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开经验方选择对话框失败");
            }
        }

        /// <summary>
        /// 导入经验方到处方 - Epic #2175 BF-002 Task 3.8 + Task 3.10
        /// </summary>
        private async Task ImportFormulaAsync(Guid formulaId)
        {
            try
            {
                Logger.LogInformation("开始导入经验方: {FormulaId}", formulaId);

                // 调用DataManager的ImportFormulaIntoPrescriptionAsync API
                var response = await _dataManager.ImportFormulaIntoPrescriptionAsync(MedicalCaseId, formulaId);

                if (response?.Success == true && response.Data?.Items != null)
                {
                    var prescription = response.Data;

                    // Task 3.10: 检测重复药材
                    var duplicates = CheckDuplicateHerbs(prescription.Items);

                    if (duplicates.Count > 0)
                    {
                        // 显示重复药材提醒对话框
                        var dialogService = _containerProvider.Resolve<Prism.Services.Dialogs.IDialogService>();
                        var parameters = new Prism.Services.Dialogs.DialogParameters
                        {
                            { "DuplicateHerbs", duplicates }
                        };

                        dialogService.ShowDialog("DuplicateHerbAlertDialog", parameters, result =>
                        {
                            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
                            {
                                // 用户确认合并，执行合并逻辑
                                MergeDuplicateHerbs(prescription.Items, duplicates);
                                Logger.LogInformation("用户确认合并 {Count} 个重复药材", duplicates.Count);
                            }
                            else
                            {
                                Logger.LogInformation("用户取消导入经验方");
                            }
                        });
                    }
                    else
                    {
                        // 没有重复药材，直接导入
                        ImportPrescriptionItems(prescription);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入经验方失败");
            }
        }

        /// <summary>
        /// 检测重复药材 - Epic #2175 BF-002 Task 3.10
        /// </summary>
        private List<DuplicateHerbInfo> CheckDuplicateHerbs(List<Shared.Models.Contracts.Prescriptions.PrescriptionItemDto> importedItems)
        {
            var duplicates = new List<DuplicateHerbInfo>();

            foreach (var importedItem in importedItems)
            {
                var existingItem = PrescriptionItems.FirstOrDefault(p => p.HerbId == importedItem.HerbId);
                if (existingItem != null)
                {
                    duplicates.Add(new DuplicateHerbInfo
                    {
                        HerbId = importedItem.HerbId,
                        HerbName = importedItem.HerbName ?? string.Empty,
                        CurrentDosage = existingItem.Dosage,
                        ImportedDosage = importedItem.Dosage
                    });
                }
            }

            return duplicates;
        }

        /// <summary>
        /// 合并重复药材 - Epic #2175 BF-002 Task 3.10
        /// 合并规则: Math.Max(currentDosage, importedDosage)
        /// </summary>
        private void MergeDuplicateHerbs(List<Shared.Models.Contracts.Prescriptions.PrescriptionItemDto> importedItems, List<DuplicateHerbInfo> duplicates)
        {
            // 1. 合并重复药材（更新剂量为最大值）
            foreach (var duplicate in duplicates)
            {
                var existingItem = PrescriptionItems.First(p => p.HerbId == duplicate.HerbId);
                existingItem.Dosage = duplicate.MergedDosage;
            }

            // 2. 添加非重复药材
            var duplicateHerbIds = duplicates.Select(d => d.HerbId).ToList();
            foreach (var importedItem in importedItems.Where(i => !duplicateHerbIds.Contains(i.HerbId)))
            {
                var itemViewModel = _containerProvider.Resolve<PrescriptionItemViewModel>();
                itemViewModel.HerbId = importedItem.HerbId;
                itemViewModel.HerbName = importedItem.HerbName ?? string.Empty;
                itemViewModel.Dosage = importedItem.Dosage;
                itemViewModel.UnitPrice = importedItem.UnitPrice;
                itemViewModel.AllHerbs = _allHerbs;

                PrescriptionItems.Add(itemViewModel);
            }

            // 3. 触发价格重新计算
            RaisePropertyChanged(nameof(SubTotal));
            RaisePropertyChanged(nameof(TotalAmount));
        }

        /// <summary>
        /// 导入处方药材（无重复时直接导入）- Epic #2175 BF-002 Task 3.10
        /// </summary>
        private void ImportPrescriptionItems(Shared.Models.Contracts.Prescriptions.PrescriptionDto prescription)
        {
            // 清空当前处方药材列表
            PrescriptionItems.Clear();

            // 重新加载处方药材列表
            foreach (var herbItem in prescription.Items)
            {
                var itemViewModel = _containerProvider.Resolve<PrescriptionItemViewModel>();
                itemViewModel.HerbId = herbItem.HerbId;
                itemViewModel.HerbName = herbItem.HerbName ?? string.Empty;
                itemViewModel.Dosage = herbItem.Dosage;
                itemViewModel.UnitPrice = herbItem.UnitPrice;
                itemViewModel.AllHerbs = _allHerbs;

                PrescriptionItems.Add(itemViewModel);
            }

            // 更新剂数和折扣
            DosageCount = prescription.DosageCount;
            Discount = prescription.Discount;

            // 触发价格重新计算
            RaisePropertyChanged(nameof(SubTotal));
            RaisePropertyChanged(nameof(TotalAmount));

            Logger.LogInformation("成功导入处方，共 {Count} 味药材", prescription.Items.Count);
        }

        private bool CanExecuteImportHistory()
        {
            // 必须能编辑处方
            return CanEditPrescription;
        }

        private void ExecuteImportHistory()
        {
            try
            {
                Logger.LogDebug("打开历史处方选择对话框");

                // Task 3.9: 使用IContainerProvider延迟解析IDialogService
                var dialogService = _containerProvider.Resolve<Prism.Services.Dialogs.IDialogService>();

                // 打开历史处方选择对话框，传入PatientId
                var parameters = new Prism.Services.Dialogs.DialogParameters
                {
                    { "PatientId", PatientId }
                };

                dialogService.ShowDialog(
                    "HistoryPrescriptionSelectionDialog",
                    parameters,
                    result =>
                    {
                        if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
                        {
                            // 获取选中的历史处方
                            var selectedPrescription = result.Parameters.GetValue<Shared.Models.Contracts.Prescriptions.PrescriptionDto>("SelectedPrescription");
                            if (selectedPrescription != null)
                            {
                                Logger.LogInformation("选择历史处方: {PrescriptionNumber} ({PrescriptionId})",
                                    selectedPrescription.PrescriptionNumber, selectedPrescription.Id);

                                // 直接导入历史处方数据
                                ImportHistoryPrescription(selectedPrescription);
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开历史处方选择对话框失败");
            }
        }


        /// <summary>
        /// 导入历史处方到当前处方 - Epic #2175 BF-002 Task 3.9 + Task 3.10
        /// </summary>
        private void ImportHistoryPrescription(Shared.Models.Contracts.Prescriptions.PrescriptionDto historyPrescription)
        {
            try
            {
                Logger.LogInformation("开始导入历史处方: {PrescriptionId}", historyPrescription.Id);

                if (historyPrescription.Items == null || historyPrescription.Items.Count == 0)
                {
                    Logger.LogWarning("历史处方药材列表为空");
                    return;
                }

                // Task 3.10: 检测重复药材
                var duplicates = CheckDuplicateHerbs(historyPrescription.Items);

                if (duplicates.Count > 0)
                {
                    // 显示重复药材提醒对话框
                    var dialogService = _containerProvider.Resolve<Prism.Services.Dialogs.IDialogService>();
                    var parameters = new Prism.Services.Dialogs.DialogParameters
                    {
                        { "DuplicateHerbs", duplicates }
                    };

                    dialogService.ShowDialog("DuplicateHerbAlertDialog", parameters, result =>
                    {
                        if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
                        {
                            // 用户确认合并，执行合并逻辑
                            MergeDuplicateHerbs(historyPrescription.Items, duplicates);
                            Logger.LogInformation("用户确认合并 {Count} 个重复药材", duplicates.Count);
                        }
                        else
                        {
                            Logger.LogInformation("用户取消导入历史处方");
                        }
                    });
                }
                else
                {
                    // 没有重复药材，直接导入
                    ImportPrescriptionItems(historyPrescription);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入历史处方失败");
            }
        }

        /// <summary>
        /// 剂量完成处理 - Epic #2175 BF-002 Task 3.7
        /// 在HerbCardControl中输入剂量后按Enter时触发
        /// Task 3.10将扩展此方法以实现重复药材聚合提醒
        /// </summary>
        private void ExecuteDosageCompleted(PrescriptionItemViewModel? item)
        {
            if (item == null)
            {
                Logger.LogWarning("DosageCompleted: item is null");
                return;
            }

            // Task 3.7: 基本验证
            if (!item.IsDosageValid)
            {
                Logger.LogWarning("剂量无效: {HerbName} {Dosage}g - {Message}",
                    item.HerbName, item.Dosage, item.DosageValidationMessage);
                return;
            }

            Logger.LogDebug("剂量输入完成: {HerbName} {Dosage}g",
                item.HerbName, item.Dosage);

            // Task 3.10: 检测重复药材（简单版本 - 仅日志记录）
            var duplicates = PrescriptionItems
                .Where(p => p != item && p.HerbId == item.HerbId && p.HerbId != Guid.Empty)
                .ToList();

            if (duplicates.Any())
            {
                Logger.LogInformation("检测到重复药材: {HerbName} (共{Count}次)",
                    item.HerbName, duplicates.Count + 1);
                // TODO: Task 3.10 - 实现重复药材聚合提醒对话框
            }
        }

        #endregion

        #region INavigationAware实现

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            try
            {
                // Task 3.3: 从导航参数加载MedicalCase数据
                if (navigationContext.Parameters.TryGetValue<Guid>("MedicalCaseId", out var medicalCaseId))
                {
                    MedicalCaseId = medicalCaseId;
                    Logger.LogInformation("导航到病案编辑页面: MedicalCaseId={MedicalCaseId}", MedicalCaseId);

                    // 加载医案数据
                    _ = LoadMedicalCaseDataAsync();
                }
                else
                {
                    Logger.LogWarning("缺少MedicalCaseId导航参数");
                    _ = UserNotificationService?.ShowWarningAsync("缺少医案ID参数");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到病案编辑页面失败");
                _ = UserNotificationService?.ShowErrorAsync($"加载失败: {ex.Message}");
            }
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 每次导航都创建新实例
            return false;
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            Logger.LogInformation("离开病案编辑页面");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载医案数据
        /// </summary>
        private async Task LoadMedicalCaseDataAsync()
        {
            try
            {
                // Epic #2175 BF-002 Task 3.7: 加载所有药材列表，供拼音码过滤使用
                await LoadAllHerbsAsync();

                await _dataManager.InitializeAsync(MedicalCaseId);

                if (_dataManager.Current != null)
                {
                    PatientId = _dataManager.Current.PatientId;
                    DoctorId = _dataManager.Current.DoctorId;
                    // TODO: NeedsPrescription 需要从API单独获取，当前MedicalCaseDto中不包含此字段
                    // NeedsPrescription = _dataManager.Current.NeedsPrescription;
                }

                if (_dataManager.CurrentConsultation != null)
                {
                    ChiefComplaint = _dataManager.CurrentConsultation.ChiefComplaint ?? string.Empty;
                    TCMDiagnosis = _dataManager.CurrentConsultation.TCMDiagnosis ?? string.Empty;
                    Inspection = _dataManager.CurrentConsultation.Inspection ?? string.Empty;
                    Auscultation = _dataManager.CurrentConsultation.AuscultationOlfaction ?? string.Empty;
                    Inquiry = _dataManager.CurrentConsultation.Inquiry ?? string.Empty;
                    PulseDiagnosis = _dataManager.CurrentConsultation.Palpation ?? string.Empty;
                    Step1CompletedAt = _dataManager.CurrentConsultation.Step1CompletedAt;
                    Step2CompletedAt = _dataManager.CurrentConsultation.Step2CompletedAt;
                }

                Logger.LogInformation("医案数据加载完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载医案数据失败");
                await UserNotificationService?.ShowErrorAsync($"加载数据失败: {ex.Message}")!;
            }
        }

        /// <summary>
        /// 加载所有药材列表 - Epic #2175 BF-002 Task 3.7: 通过IContainerProvider延迟解析跨模块依赖
        /// </summary>
        private async Task LoadAllHerbsAsync()
        {
            try
            {
                Logger.LogDebug("开始加载所有药材列表");

                // Task 3.7: 使用IContainerProvider延迟解析IHerbDataManager（避免构造函数强依赖）
                var herbDataManager = _containerProvider.Resolve<IHerbDataManager>();

                _allHerbs.Clear();

                // 分页加载所有药材（Server端限制pageSize最大100）
                const int pageSize = 100;
                int currentPage = 1;
                int totalLoaded = 0;

                while (true)
                {
                    var pagedResult = await herbDataManager.GetPagedAsync(currentPage, pageSize);

                    if (pagedResult?.Items == null || !pagedResult.Items.Any())
                    {
                        break; // 没有更多数据
                    }

                    foreach (var herb in pagedResult.Items)
                    {
                        _allHerbs.Add(herb);
                    }

                    totalLoaded += pagedResult.Items.Count;

                    // 如果当前页数据不足pageSize，说明已经是最后一页
                    if (pagedResult.Items.Count < pageSize)
                    {
                        break;
                    }

                    currentPage++;
                }

                Logger.LogInformation("成功分页加载 {Count} 个药材", totalLoaded);

                // 调试日志：输出前5个药材的Name和PinYinCode
                if (_allHerbs.Any())
                {
                    Logger.LogInformation("=== 前5个药材数据 ===");
                    foreach (var herb in _allHerbs.Take(5))
                    {
                        Logger.LogInformation("Name: {Name}, PinYinCode: {PinYinCode}",
                            herb.Name, herb.PinYinCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材列表失败");
                // 不阻止主流程，只记录错误
            }
        }

        #endregion
    }
}
