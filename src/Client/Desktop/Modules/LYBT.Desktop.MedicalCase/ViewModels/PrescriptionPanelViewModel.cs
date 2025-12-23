using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.MedicalCase.Events;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.Prescriptions.Models.Items;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 处方面板ViewModel
/// OpenSpec: refactor-oversized-viewmodels - 重构后 < 500行
/// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.3) - 移除ISaveable，使用IDataProvider
/// </summary>
public class PrescriptionPanelViewModel : UnifiedViewModelBase, IDataProvider
{
    #region 字段

    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IHerbRepository _herbRepository;
    private readonly IDialogService _dialogService;
    private readonly IEventAggregator _eventAggregator;
    private readonly PrescriptionCalculator _calculator;
    private readonly PrescriptionValidator _validator;
    private readonly PrescriptionItemHandler _itemHandler;
    private readonly PrescriptionSaveHandler _saveHandler;
    private readonly PrescriptionImportHandler _importHandler;
    private readonly PrescriptionDataLoader _dataLoader;
    private Guid _medicalCaseId;
    private Guid? _prescriptionId;
    private Guid _patientId;
    private string _patientName = string.Empty;
    private bool _isLoadingData;
    private bool _isInitialized;
    private ObservableCollection<HerbListDto> _allHerbs = new();

    #endregion

    #region 处方属性

    private string _treatmentMethod = string.Empty;
    /// <summary>
    /// 主治/适应症 (映射到 Indication)
    /// </summary>
    public string TreatmentMethod { get => _treatmentMethod; set => SetProperty(ref _treatmentMethod, value); }

    private string _treatmentPrinciple = string.Empty;
    /// <summary>
    /// 医嘱/用药建议 (映射到 Advice)
    /// </summary>
    public string TreatmentPrinciple { get => _treatmentPrinciple; set => SetProperty(ref _treatmentPrinciple, value); }

    private string _referencedFormulas = string.Empty;
    /// <summary>
    /// 引用的验方名称列表，逗号分隔
    /// OpenSpec: refactor-medicalcase-aggregate-crud
    /// </summary>
    public string ReferencedFormulas { get => _referencedFormulas; set => SetProperty(ref _referencedFormulas, value); }

    private string _remark = string.Empty;
    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get => _remark; set => SetProperty(ref _remark, value); }

    private int _dosageCount = 7;
    public int DosageCount
    {
        get => _dosageCount;
        set { if (SetProperty(ref _dosageCount, value)) CalculatePrices(); }
    }

    private string _usage = "水煎服，一日一剂，分早晚两次温服";
    public string Usage { get => _usage; set => SetProperty(ref _usage, value); }

    private decimal _singleDosagePrice;
    public decimal SingleDosagePrice { get => _singleDosagePrice; set => SetProperty(ref _singleDosagePrice, value); }

    private decimal _totalPrice;
    public decimal TotalPrice { get => _totalPrice; set => SetProperty(ref _totalPrice, value); }

    private int _itemCount;
    public int ItemCount { get => _itemCount; set => SetProperty(ref _itemCount, value); }

    #endregion

    #region 药材和警告属性

    public ObservableCollection<PrescriptionHerbItem> HerbItems { get; } = new();

    private bool _isDuplicateHerbsWarningVisible;
    public bool IsDuplicateHerbsWarningVisible { get => _isDuplicateHerbsWarningVisible; set => SetProperty(ref _isDuplicateHerbsWarningVisible, value); }

    private string _duplicateHerbsWarningText = string.Empty;
    public string DuplicateHerbsWarningText { get => _duplicateHerbsWarningText; set => SetProperty(ref _duplicateHerbsWarningText, value); }

    #endregion

    #region 控件状态

    private bool _hasUnsavedChanges;
    public bool HasUnsavedChanges { get => _hasUnsavedChanges; private set => SetProperty(ref _hasUnsavedChanges, value); }

    private bool _isReadOnly;
    public bool IsReadOnly { get => _isReadOnly; set => SetProperty(ref _isReadOnly, value); }

    #endregion

    #region 命令

    public DelegateCommand AddRowCommand { get; }
    public DelegateCommand SaveDraftCommand { get; }
    public DelegateCommand DeletePrescriptionCommand { get; }
    public DelegateCommand<PrescriptionHerbItem> DeleteHerbCommand { get; }
    public DelegateCommand<PrescriptionHerbItem> DosageCompletedCommand { get; }
    public DelegateCommand AddNewRowCommand { get; }
    public DelegateCommand OpenFormulaImportDialogCommand { get; }
    public DelegateCommand OpenHistoryCopyDialogCommand { get; }
    public DelegateCommand ClearHerbItemsCommand { get; }

    #endregion

    #region 构造函数

    public PrescriptionPanelViewModel(
        IMedicalCaseRepository medicalCaseRepository, IHerbRepository herbRepository,
        IDialogService dialogService, IEventAggregator eventAggregator, ILoggerFactory loggerFactory,
        IRegionManager regionManager, PrescriptionCalculator calculator,
        PrescriptionValidator validator, PrescriptionItemHandler itemHandler,
        PrescriptionSaveHandler saveHandler, PrescriptionImportHandler importHandler,
        PrescriptionDataLoader dataLoader, ISessionManager? sessionManager = null,
        ICommonDialogService? commonDialogService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, null, commonDialogService)
    {
        _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventAggregator = eventAggregator;
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _itemHandler = itemHandler ?? throw new ArgumentNullException(nameof(itemHandler));
        _saveHandler = saveHandler ?? throw new ArgumentNullException(nameof(saveHandler));
        _importHandler = importHandler ?? throw new ArgumentNullException(nameof(importHandler));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));

        AddRowCommand = new DelegateCommand(ExecuteAddRow);
        SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft);
        DeletePrescriptionCommand = new DelegateCommand(ExecuteDeletePrescription);
        DeleteHerbCommand = new DelegateCommand<PrescriptionHerbItem>(ExecuteDeleteHerb);
        DosageCompletedCommand = new DelegateCommand<PrescriptionHerbItem>(ExecuteDosageCompleted);
        AddNewRowCommand = new DelegateCommand(() => _itemHandler.EnsureMinimumBlankRows(HerbItems, _allHerbs, OnHerbItemChanged));
        OpenFormulaImportDialogCommand = new DelegateCommand(ExecuteOpenFormulaImportDialog);
        OpenHistoryCopyDialogCommand = new DelegateCommand(ExecuteOpenHistoryCopyDialog);
        ClearHerbItemsCommand = new DelegateCommand(ExecuteClearHerbItems);

        _itemHandler.AddDefaultHerbItems(HerbItems, _allHerbs, OnHerbItemChanged);
        EventAggregator.GetEvent<SaveAllRequestedEvent>().Subscribe(OnSaveAllRequested, ThreadOption.UIThread);
    }

    #endregion

    #region 初始化

    public async Task InitializeAsync(Guid medicalCaseId, Guid patientId, string patientName = "", PrescriptionDetailDto? existingPrescription = null)
    {
        _medicalCaseId = medicalCaseId;
        _patientId = patientId;
        _patientName = patientName;
        HerbItems.Clear();
        _prescriptionId = null;

        await _dataLoader.LoadAllHerbsAsync(_allHerbs);
        _dataLoader.InjectHerbsToItems(HerbItems, _allHerbs);

        if (existingPrescription != null) LoadFromDto(existingPrescription);
        else { _itemHandler.AddDefaultHerbItems(HerbItems, _allHerbs, OnHerbItemChanged); UpdateItemCount(); }

        _isInitialized = true;
        HasUnsavedChanges = false;
    }

    private void LoadFromDto(PrescriptionDetailDto dto)
    {
        _isLoadingData = true;
        try
        {
            _prescriptionId = dto.Id;
            TreatmentMethod = string.Empty; // Indication已删除，打印时从Consultation.TCMDiagnosis获取
            TreatmentPrinciple = dto.Advice ?? string.Empty;
            ReferencedFormulas = dto.ReferencedFormulas ?? string.Empty;
            Remark = dto.Remark ?? string.Empty;
            DosageCount = dto.DosageCount;
            Usage = dto.Usage ?? string.Empty;
            SingleDosagePrice = dto.SingleDosePrice;
            TotalPrice = dto.TotalPrice;

            HerbItems.Clear();
            if (dto.Items?.Any() == true)
            {
                foreach (var item in dto.Items)
                {
                    var herbItem = _itemHandler.CreateHerbItem(_allHerbs, OnHerbItemChanged);
                    herbItem.HerbId = item.HerbId;
                    herbItem.HerbName = item.HerbName ?? string.Empty;
                    herbItem.Dosage = item.Dosage;
                    herbItem.DecocteMethod = item.DecocteMethod;
                    herbItem.SetLoadedUnitPrice(item.UnitPrice);
                    HerbItems.Add(herbItem);
                }
            }
            _itemHandler.EnsureMinimumBlankRows(HerbItems, _allHerbs, OnHerbItemChanged);
            UpdateItemCount();
            CalculatePrices();
        }
        finally { _isLoadingData = false; }
    }

    private void OnHerbItemChanged(PrescriptionHerbItem item, string propertyName)
    {
        if (_isLoadingData) return;
        CalculatePrices();
        UpdateItemCount();
        CheckDuplicateHerbs();
        if (propertyName == nameof(PrescriptionHerbItem.HerbId))
            _itemHandler.EnsureMinimumBlankRows(HerbItems, _allHerbs, OnHerbItemChanged);
        NotifyDataChanged();
    }

    #endregion

    #region 内部保存（供命令使用）

    /// <summary>
    /// 保存处方数据到服务器
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.3) - 内部方法，供命令使用
    /// 注意：主要的保存流程已迁移到聚合保存模式，此方法仅供内部命令使用
    /// </summary>
    private async Task<bool> SaveAsync()
    {
        try
        {
            var context = CreateSaveContext();
            var result = await _saveHandler.SaveAsync(context);
            if (result.IsEmpty) return true;
            if (result.IsSuccess)
            {
                _prescriptionId = result.PrescriptionId;
                _itemHandler.CompactHerbItems(HerbItems, _allHerbs, OnHerbItemChanged);
                HasUnsavedChanges = false;
                EventAggregator.GetEvent<PrescriptionSavedEvent>().Publish(new PrescriptionSavedPayload
                {
                    MedicalCaseId = _medicalCaseId,
                    PrescriptionId = result.PrescriptionId ?? Guid.Empty,
                    IsAutoSave = false
                });
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存处方数据异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
            return false;
        }
    }

    private PrescriptionSaveContext CreateSaveContext() => new()
    {
        MedicalCaseId = _medicalCaseId,
        PrescriptionId = _prescriptionId,
        PatientId = _patientId,
        UserId = SessionManager?.CurrentUserId ?? Guid.Empty,
        DosageCount = DosageCount,
        Usage = Usage,
        Items = _itemHandler.CollectPrescriptionItems(HerbItems),
        TotalPrice = TotalPrice
    };

    #endregion

    #region IDataProvider

    /// <summary>
    /// 获取诊断数据
    /// PrescriptionPanel不提供诊断数据，返回null
    /// </summary>
    /// <returns>null（诊断数据由ConsultationPanel提供）</returns>
    public ConsultationInputDto? GetConsultationData() => null;

    /// <summary>
    /// 获取处方数据
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.3)
    /// </summary>
    /// <returns>处方聚合输入DTO</returns>
    public PrescriptionInputDto? GetPrescriptionData()
    {
        var items = _itemHandler.CollectPrescriptionItems(HerbItems);

        // 如果没有有效药材项，返回表示不需要处方的DTO
        if (items.Count == 0)
        {
            return new PrescriptionInputDto
            {
                NeedsPrescription = false,
                DosageCount = DosageCount,
                Usage = Usage,
                Advice = TreatmentPrinciple,
                ReferencedFormulas = ReferencedFormulas,
                Remark = Remark,
                Id = _prescriptionId
            };
        }

        return new PrescriptionInputDto
        {
            NeedsPrescription = true,
            DosageCount = DosageCount,
            Usage = Usage,
            Advice = TreatmentPrinciple,
            ReferencedFormulas = ReferencedFormulas,
            Remark = Remark,
            Items = items,
            Id = _prescriptionId
        };
    }

    #endregion

    #region 命令实现

    private void ExecuteAddRow() => _itemHandler.AddNewRow(HerbItems, _allHerbs, OnHerbItemChanged);

    private void ExecuteDeleteHerb(PrescriptionHerbItem? item)
    {
        if (item == null) return;
        _itemHandler.DeleteHerbItem(HerbItems, item, _allHerbs, OnHerbItemChanged);
        UpdateItemCount();
        CalculatePrices();
        CheckDuplicateHerbs();
    }

    private void ExecuteDosageCompleted(PrescriptionHerbItem? item)
    {
        if (item == null) return;
        CheckDuplicateHerbs();
        CalculatePrices();
    }

    private async void ExecuteSaveDraft()
    {
        try
        {
            SetIsBusy(true, "正在保存...");
            if (await SaveAsync()) await ShowSuccessMessageAsync("处方草稿已保存");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存草稿失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
        finally { SetIsBusy(false); }
    }

    private async void ExecuteDeletePrescription()
    {
        try
        {
            if (!_prescriptionId.HasValue) { await ShowErrorMessageAsync("当前没有处方可删除"); return; }
            if (!await ShowConfirmationAsync("确定要删除当前处方吗？此操作不可恢复！", "删除处方")) return;
            SetIsBusy(true, "正在删除...");

            // OpenSpec: simplify-medicalcase-api - 通过聚合保存设置NeedsPrescription=false触发删除
            var inputDto = new MedicalCaseInputDto
            {
                Id = _medicalCaseId,
                NeedsPrescription = false,
                Prescription = null
            };
            await _medicalCaseRepository.SaveAsync(_medicalCaseId, inputDto);

            ResetPrescription();
            await ShowSuccessMessageAsync("处方已删除");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除处方失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除", ex));
        }
        finally { SetIsBusy(false); }
    }

    private void ResetPrescription()
    {
        _prescriptionId = null;
        HerbItems.Clear();
        _itemHandler.AddDefaultHerbItems(HerbItems, _allHerbs, OnHerbItemChanged);
        TreatmentMethod = TreatmentPrinciple = string.Empty;
        ReferencedFormulas = Remark = string.Empty;
        SingleDosagePrice = TotalPrice = 0;
        ItemCount = 0;
    }

    /// <summary>
    /// 清空当前处方药材
    /// </summary>
    private async void ExecuteClearHerbItems()
    {
        // 检查是否有有效药材
        var validItemCount = HerbItems.Count(i => i.HerbId != Guid.Empty && i.Dosage > 0);
        if (validItemCount == 0)
        {
            await ShowSuccessMessageAsync("当前没有可清空的药材");
            return;
        }

        // 确认对话框
        if (!await ShowConfirmationAsync($"确定要清空当前所有药材（共{validItemCount}项）吗？", "清空药材"))
            return;

        // 清空药材项
        HerbItems.Clear();
        _itemHandler.AddDefaultHerbItems(HerbItems, _allHerbs, OnHerbItemChanged);

        // 更新统计
        UpdateItemCount();
        CalculatePrices();
        CheckDuplicateHerbs();
        NotifyDataChanged();

        Logger.LogInformation("已清空处方药材，共{Count}项", validItemCount);
        await ShowSuccessMessageAsync($"已清空{validItemCount}项药材");
    }

    #endregion

    #region 辅助方法

    private void UpdateItemCount() => ItemCount = _calculator.CalculateItemCount(HerbItems);

    private void CalculatePrices()
    {
        var result = _calculator.CalculatePrices(HerbItems, DosageCount);
        SingleDosagePrice = result.SingleDosagePrice;
        TotalPrice = result.TotalPrice;
    }

    private void CheckDuplicateHerbs()
    {
        var result = _validator.CheckDuplicateHerbs(HerbItems);
        IsDuplicateHerbsWarningVisible = result.HasDuplicates;
        DuplicateHerbsWarningText = result.WarningText;
    }

    #endregion

    #region 弹窗处理

    private void ExecuteOpenFormulaImportDialog() =>
        _dialogService.ShowDialog(nameof(Dialogs.FormulaImportDialog), null, async r =>
        { if (r.Result == ButtonResult.OK) await HandleFormulaImportResultAsync(r.Parameters); });

    private void ExecuteOpenHistoryCopyDialog() =>
        _dialogService.ShowDialog(nameof(Dialogs.HistoryCopyDialog),
            new DialogParameters { { "PatientId", _patientId }, { "PatientName", _patientName } },
            async r => { if (r.Result == ButtonResult.OK) await HandleHistoryCopyResultAsync(r.Parameters); });

    private async Task HandleFormulaImportResultAsync(IDialogParameters parameters)
    {
        try
        {
            SetIsBusy(true, "正在导入验方...");
            if (!parameters.TryGetValue<FormulaDetailDto>("SelectedFormula", out var formula) || formula == null) return;
            if (!parameters.TryGetValue<List<FormulaHerbItemDto>>("SelectedHerbs", out var herbs) || herbs?.Any() != true)
            { await ShowErrorMessageAsync("验方无药材信息"); return; }

            var importResult = _importHandler.ProcessFormulaImport(formula, herbs, HerbItems, _allHerbs);
            if (!importResult.IsSuccess) { await ShowErrorMessageAsync(importResult.ErrorMessage ?? "导入失败"); return; }

            // OpenSpec: enhance-duplicate-herb-dialog - 使用逐个弹窗确认重复药材
            Logger.LogInformation("验方导入处理: HasDuplicates={HasDuplicates}, DuplicateCount={Count}",
                importResult.HasDuplicates, importResult.DuplicateInfos.Count);
            if (importResult.HasDuplicates)
            {
                Logger.LogInformation("开始逐个弹窗确认重复药材...");
                await ShowDuplicateHerbDialogsAsync(importResult.DuplicateInfos);
                Logger.LogInformation("重复药材确认完成");
            }

            var addedCount = _importHandler.AddHerbItemsToCollection(HerbItems, importResult.ItemsToAdd, () => _itemHandler.CreateHerbItem(_allHerbs, OnHerbItemChanged));
            _itemHandler.EnsureMinimumBlankRows(HerbItems, _allHerbs, OnHerbItemChanged);
            UpdateItemCount();
            CalculatePrices();

            // 记录引用的验方名称
            if (!string.IsNullOrEmpty(importResult.FormulaName))
            {
                if (string.IsNullOrEmpty(ReferencedFormulas))
                    ReferencedFormulas = importResult.FormulaName;
                else if (!ReferencedFormulas.Contains(importResult.FormulaName))
                    ReferencedFormulas = $"{ReferencedFormulas}, {importResult.FormulaName}";
            }

            await ShowSuccessMessageAsync($"已导入验方「{importResult.FormulaName}」，添加 {addedCount} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理验方导入结果异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入", ex));
        }
        finally { SetIsBusy(false); }
    }

    private async Task HandleHistoryCopyResultAsync(IDialogParameters parameters)
    {
        try
        {
            SetIsBusy(true, "正在复制处方...");
            if (!parameters.TryGetValue<List<PrescriptionItemDto>>("SelectedItems", out var items) || items?.Any() != true)
            { await ShowErrorMessageAsync("历史处方无药材记录"); return; }

            var copyResult = _importHandler.ProcessHistoryCopy(items, HerbItems);
            if (!copyResult.IsSuccess) { await ShowErrorMessageAsync(copyResult.ErrorMessage ?? "复制失败"); return; }

            // OpenSpec: enhance-duplicate-herb-dialog - 使用逐个弹窗确认重复药材
            Logger.LogInformation("历史复制处理: HasDuplicates={HasDuplicates}, DuplicateCount={Count}",
                copyResult.HasDuplicates, copyResult.DuplicateInfos.Count);
            if (copyResult.HasDuplicates)
            {
                Logger.LogInformation("开始逐个弹窗确认重复药材...");
                await ShowDuplicateHerbDialogsAsync(copyResult.DuplicateInfos);
                Logger.LogInformation("重复药材确认完成");
            }

            var addedCount = _importHandler.AddHerbItemsToCollection(HerbItems, copyResult.ItemsToAdd, () => _itemHandler.CreateHerbItem(_allHerbs, OnHerbItemChanged));
            _itemHandler.EnsureMinimumBlankRows(HerbItems, _allHerbs, OnHerbItemChanged);
            UpdateItemCount();
            CalculatePrices();
            await ShowSuccessMessageAsync($"已复制 {addedCount} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理历史复制结果异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("复制", ex));
        }
        finally { SetIsBusy(false); }
    }

    /// <summary>
    /// 逐个显示重复药材确认对话框
    /// OpenSpec: enhance-duplicate-herb-dialog
    /// </summary>
    private async Task ShowDuplicateHerbDialogsAsync(List<DuplicateHerbInfo> duplicates)
    {
        Logger.LogInformation("ShowDuplicateHerbDialogsAsync: 开始处理 {Count} 个重复药材", duplicates.Count);
        foreach (var duplicate in duplicates)
        {
            Logger.LogInformation("显示重复药材对话框: {HerbName}", duplicate.HerbName);
            var parameters = new DialogParameters
            {
                { "HerbName", duplicate.HerbName }
            };

            var tcs = new TaskCompletionSource<bool>();
            _dialogService.ShowDialog("DuplicateHerbAlertDialog", parameters, result =>
            {
                Logger.LogInformation("对话框已关闭: {HerbName}, Result={Result}", duplicate.HerbName, result.Result);
                tcs.SetResult(true);
            });

            await tcs.Task;
        }

        Logger.LogInformation("所有对话框已确认，开始合并剂量");
        // 所有确认完成后执行剂量合并
        MergeDuplicateHerbs(duplicates);
    }

    /// <summary>
    /// 合并重复药材的剂量（取最大值）
    /// OpenSpec: enhance-duplicate-herb-dialog
    /// </summary>
    private void MergeDuplicateHerbs(List<DuplicateHerbInfo> duplicates)
    {
        foreach (var duplicate in duplicates)
        {
            var existingItem = HerbItems.FirstOrDefault(h => h.HerbId == duplicate.HerbId);
            if (existingItem != null)
            {
                existingItem.Dosage = duplicate.MergedDosage;
                Logger.LogDebug("合并重复药材: {HerbName}, 剂量: {CurrentDosage}g -> {MergedDosage}g",
                    duplicate.HerbName, duplicate.CurrentDosage, duplicate.MergedDosage);
            }
        }

        CalculatePrices();
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理全局保存请求事件
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4) - 使用SaveAsync替代SaveSilentlyAsync
    /// 注意：此事件处理逻辑将在聚合保存模式下逐步废弃
    /// </summary>
    private async void OnSaveAllRequested(Guid medicalCaseId)
    {
        if (medicalCaseId == _medicalCaseId)
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "SaveAllRequested保存失败，静默处理");
            }
        }
    }

    private void NotifyDataChanged()
    {
        if (!_isInitialized || HasUnsavedChanges) return;
        HasUnsavedChanges = true;
        EventAggregator.GetEvent<PrescriptionDataChangedEvent>().Publish(_medicalCaseId);
    }

    public void Cleanup() => EventAggregator.GetEvent<SaveAllRequestedEvent>().Unsubscribe(OnSaveAllRequested);

    #endregion
}
