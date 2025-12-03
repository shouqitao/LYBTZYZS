using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Events;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 处方面板ViewModel
/// OpenSpec: refactor-oversized-viewmodels - 重构后 < 500行
/// </summary>
public class PrescriptionPanelViewModel : UnifiedViewModelBase, ISaveable
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
    private ObservableCollection<HerbDto> _allHerbs = new();

    #endregion

    #region 处方属性

    private string _treatmentMethod = string.Empty;
    public string TreatmentMethod { get => _treatmentMethod; set => SetProperty(ref _treatmentMethod, value); }

    private string _treatmentPrinciple = string.Empty;
    public string TreatmentPrinciple { get => _treatmentPrinciple; set => SetProperty(ref _treatmentPrinciple, value); }

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

    public ObservableCollection<PrescriptionHerbItemViewModel> HerbItems { get; } = new();

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
    public DelegateCommand<PrescriptionHerbItemViewModel> DeleteHerbCommand { get; }
    public DelegateCommand<PrescriptionHerbItemViewModel> DosageCompletedCommand { get; }
    public DelegateCommand AddNewRowCommand { get; }
    public DelegateCommand OpenFormulaImportDialogCommand { get; }
    public DelegateCommand OpenHistoryCopyDialogCommand { get; }

    #endregion

    #region 构造函数

    public PrescriptionPanelViewModel(
        IMedicalCaseRepository medicalCaseRepository, IHerbRepository herbRepository,
        IDialogService dialogService, IEventAggregator eventAggregator, ILoggerFactory loggerFactory,
        IRegionManager regionManager, PrescriptionCalculator calculator,
        PrescriptionValidator validator, PrescriptionItemHandler itemHandler,
        PrescriptionSaveHandler saveHandler, PrescriptionImportHandler importHandler,
        PrescriptionDataLoader dataLoader, ISessionManager? sessionManager = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager)
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
        DeleteHerbCommand = new DelegateCommand<PrescriptionHerbItemViewModel>(ExecuteDeleteHerb);
        DosageCompletedCommand = new DelegateCommand<PrescriptionHerbItemViewModel>(ExecuteDosageCompleted);
        AddNewRowCommand = new DelegateCommand(() => _itemHandler.EnsureMinimumBlankRows(HerbItems, _allHerbs, OnHerbItemChanged));
        OpenFormulaImportDialogCommand = new DelegateCommand(ExecuteOpenFormulaImportDialog);
        OpenHistoryCopyDialogCommand = new DelegateCommand(ExecuteOpenHistoryCopyDialog);

        _itemHandler.AddDefaultHerbItems(HerbItems, _allHerbs, OnHerbItemChanged);
        EventAggregator.GetEvent<SaveAllRequestedEvent>().Subscribe(OnSaveAllRequested, ThreadOption.UIThread);
    }

    #endregion

    #region 初始化

    public async Task InitializeAsync(Guid medicalCaseId, Guid patientId, string patientName = "", PrescriptionDto? existingPrescription = null)
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

    private void LoadFromDto(PrescriptionDto dto)
    {
        _isLoadingData = true;
        try
        {
            _prescriptionId = dto.Id;
            TreatmentMethod = dto.Indication ?? string.Empty;
            TreatmentPrinciple = dto.Advice ?? string.Empty;
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

    private void OnHerbItemChanged(PrescriptionHerbItemViewModel item, string propertyName)
    {
        if (_isLoadingData) return;
        CalculatePrices();
        UpdateItemCount();
        CheckDuplicateHerbs();
        if (propertyName == nameof(PrescriptionHerbItemViewModel.HerbId))
            _itemHandler.EnsureMinimumBlankRows(HerbItems, _allHerbs, OnHerbItemChanged);
        NotifyDataChanged();
    }

    #endregion

    #region ISaveable

    public async Task<bool> SaveAsync()
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
            await ShowErrorMessageAsync($"保存失败：{ex.Message}");
            return false;
        }
    }

    public async Task<bool> SaveSilentlyAsync()
    {
        var context = CreateSaveContext();
        var result = await _saveHandler.SaveSilentlyAsync(context);
        if (result.IsEmpty) return true;
        if (result.IsSuccess)
        {
            _prescriptionId = result.PrescriptionId;
            _itemHandler.CompactHerbItems(HerbItems, _allHerbs, OnHerbItemChanged);
            HasUnsavedChanges = false;
            return true;
        }
        return false;
    }

    private PrescriptionSaveContext CreateSaveContext() => new()
    {
        MedicalCaseId = _medicalCaseId,
        PrescriptionId = _prescriptionId,
        PatientId = _patientId,
        DoctorId = SessionManager?.CurrentUserId ?? Guid.Empty,
        DosageCount = DosageCount,
        Usage = Usage,
        Items = _itemHandler.CollectPrescriptionItems(HerbItems),
        TotalPrice = TotalPrice
    };

    #endregion

    #region 命令实现

    private void ExecuteAddRow() => _itemHandler.AddNewRow(HerbItems, _allHerbs, OnHerbItemChanged);

    private void ExecuteDeleteHerb(PrescriptionHerbItemViewModel? item)
    {
        if (item == null) return;
        _itemHandler.DeleteHerbItem(HerbItems, item, _allHerbs, OnHerbItemChanged);
        UpdateItemCount();
        CalculatePrices();
        CheckDuplicateHerbs();
    }

    private void ExecuteDosageCompleted(PrescriptionHerbItemViewModel? item)
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
            await ShowErrorMessageAsync($"保存失败：{ex.Message}");
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
            await _medicalCaseRepository.DeletePrescriptionAsync(_medicalCaseId);
            ResetPrescription();
            await ShowSuccessMessageAsync("处方已删除");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除处方失败");
            await ShowErrorMessageAsync($"删除失败：{ex.Message}");
        }
        finally { SetIsBusy(false); }
    }

    private void ResetPrescription()
    {
        _prescriptionId = null;
        HerbItems.Clear();
        _itemHandler.AddDefaultHerbItems(HerbItems, _allHerbs, OnHerbItemChanged);
        TreatmentMethod = TreatmentPrinciple = string.Empty;
        SingleDosagePrice = TotalPrice = 0;
        ItemCount = 0;
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
            if (!parameters.TryGetValue<FormulaDto>("SelectedFormula", out var formula) || formula == null) return;
            if (!parameters.TryGetValue<List<FormulaHerbItemDto>>("SelectedHerbs", out var herbs) || herbs?.Any() != true)
            { await ShowErrorMessageAsync("验方无药材信息"); return; }

            var importResult = _importHandler.ProcessFormulaImport(formula, herbs, HerbItems, _allHerbs);
            if (!importResult.IsSuccess) { await ShowErrorMessageAsync(importResult.ErrorMessage ?? "导入失败"); return; }
            if (importResult.HasDuplicates) { DuplicateHerbsWarningText = importResult.DuplicateWarningText; IsDuplicateHerbsWarningVisible = true; }

            var addedCount = _importHandler.AddHerbItemsToCollection(HerbItems, importResult.ItemsToAdd, () => _itemHandler.CreateHerbItem(_allHerbs, OnHerbItemChanged));
            _itemHandler.EnsureMinimumBlankRows(HerbItems, _allHerbs, OnHerbItemChanged);
            UpdateItemCount();
            CalculatePrices();
            await ShowSuccessMessageAsync($"已导入验方「{importResult.FormulaName}」，添加 {addedCount} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理验方导入结果异常");
            await ShowErrorMessageAsync($"导入失败：{ex.Message}");
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
            if (copyResult.HasDuplicates) { DuplicateHerbsWarningText = copyResult.DuplicateWarningText; IsDuplicateHerbsWarningVisible = true; }

            var addedCount = _importHandler.AddHerbItemsToCollection(HerbItems, copyResult.ItemsToAdd, () => _itemHandler.CreateHerbItem(_allHerbs, OnHerbItemChanged));
            _itemHandler.EnsureMinimumBlankRows(HerbItems, _allHerbs, OnHerbItemChanged);
            UpdateItemCount();
            CalculatePrices();
            await ShowSuccessMessageAsync($"已复制 {addedCount} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理历史复制结果异常");
            await ShowErrorMessageAsync($"复制失败：{ex.Message}");
        }
        finally { SetIsBusy(false); }
    }

    #endregion

    #region 事件处理

    private async void OnSaveAllRequested(Guid medicalCaseId)
    { if (medicalCaseId == _medicalCaseId) await SaveSilentlyAsync(); }

    private void NotifyDataChanged()
    {
        if (!_isInitialized || HasUnsavedChanges) return;
        HasUnsavedChanges = true;
        EventAggregator.GetEvent<PrescriptionDataChangedEvent>().Publish(_medicalCaseId);
    }

    public void Cleanup() => EventAggregator.GetEvent<SaveAllRequestedEvent>().Unsubscribe(OnSaveAllRequested);

    #endregion
}
