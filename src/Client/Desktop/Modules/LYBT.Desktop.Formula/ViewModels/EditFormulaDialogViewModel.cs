using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Herbs.Contracts;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>配方编辑对话框视图模型</summary>
    public class EditFormulaDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        private readonly FormulaCommandHandler _commandHandler;
        private readonly IHerbCommandHandler _herbCommandHandler;
        private readonly ObservableCollection<HerbListDto> _allHerbs = new();

        private Guid? _formulaId;
        private string _formulaName = string.Empty;
        private string _description = string.Empty;
        private CommonStatus _status = CommonStatus.Enabled;

        public ObservableCollection<FormulaHerbItemViewModel> HerbItems { get; } = new();
        public int HerbCount => HerbItems.Count(h => h.HerbId != Guid.Empty);

        public Guid? FormulaId { get => _formulaId; set => SetProperty(ref _formulaId, value); }

        [Required(ErrorMessage = "配方名称不能为空")]
        [StringLength(100, ErrorMessage = "配方名称长度不能超过100个字符")]
        public string FormulaName { get => _formulaName; set { if (SetProperty(ref _formulaName, value)) ValidateProperty(); } }

        [StringLength(500, ErrorMessage = "配方描述长度不能超过500个字符")]
        public string Description { get => _description; set { if (SetProperty(ref _description, value)) ValidateProperty(); } }

        public CommonStatus Status { get => _status; set => SetProperty(ref _status, value); }
        public CommonStatus[] StatusOptions { get; }
        public string Title => FormulaId.HasValue ? "编辑验方模板" : "新建验方模板";
        public event Action<IDialogResult>? RequestClose;

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand<FormulaHerbItemViewModel> DeleteHerbCommand { get; }
        public DelegateCommand<FormulaHerbItemViewModel> DosageCompletedCommand { get; }
        public DelegateCommand<HerbDetailDto> HerbSelectedCommand { get; }
        public DelegateCommand AddNewRowCommand { get; }

        public EditFormulaDialogViewModel(
            FormulaCommandHandler commandHandler,
            IHerbCommandHandler herbCommandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _herbCommandHandler = herbCommandHandler ?? throw new ArgumentNullException(nameof(herbCommandHandler));
            StatusOptions = Enum.GetValues<CommonStatus>();

            SaveCommand = new DelegateCommand(async () => await SaveFormulaAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(FormulaName) && !HasErrors);
            CancelCommand = new DelegateCommand(() => RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel)));
            DeleteHerbCommand = new DelegateCommand<FormulaHerbItemViewModel>(DeleteHerb);
            DosageCompletedCommand = new DelegateCommand<FormulaHerbItemViewModel>(OnDosageCompleted);
            HerbSelectedCommand = new DelegateCommand<HerbDetailDto>(OnHerbSelected);
            AddNewRowCommand = new DelegateCommand(AddNewRow);

            PropertyChanged += (s, e) => SaveCommand.RaiseCanExecuteChanged();
        }

        public bool CanCloseDialog() => true;
        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            try { _ = InitializeAsync(parameters.TryGetValue("FormulaId", out Guid id) ? id : null); }
            catch (Exception ex) { Logger.LogError(ex, "打开对话框时发生异常"); }
        }

        public async Task InitializeAsync(Guid? formulaId = null)
        {
            try
            {
                FormulaId = formulaId;
                if (formulaId.HasValue)
                {
                    SetIsBusy(true, "正在加载配方信息...");
                    var result = await _commandHandler.GetByIdAsync(formulaId.Value);
                    if (!result.success || result.formula == null) { await ShowErrorMessageAsync(result.errorMessage ?? "配方不存在"); return; }
                    FormulaName = result.formula.Name ?? string.Empty;
                    Description = result.formula.Remark ?? string.Empty;
                    Status = result.formula.Status;
                }
                else { FormulaName = string.Empty; Description = string.Empty; Status = CommonStatus.Enabled; }

                await LoadAllHerbsAsync();
                HerbItems.Clear();
                EnsureMinimumBlankRows();
                ClearAllErrors();
            }
            catch (Exception ex) { Logger.LogError(ex, "初始化配方编辑数据时发生异常"); await ShowErrorMessageAsync("初始化配方数据时发生系统错误"); }
            finally { SetIsBusy(false); }
        }

        private async Task SaveFormulaAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存配方...");
                var dto = new FormulaInputDto { Id = FormulaId, Name = FormulaName.Trim(), Remark = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim() };

                if (FormulaId.HasValue)
                {
                    var result = await _commandHandler.UpdateAsync(dto);
                    if (!result.success || result.formula == null) { await ShowErrorMessageAsync(result.errorMessage ?? "更新配方失败"); return; }
                    RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters { { "Formula", result.formula } }));
                }
                else
                {
                    var result = await _commandHandler.CreateAsync(dto);
                    if (!result.success || result.formula == null) { await ShowErrorMessageAsync(result.errorMessage ?? "创建配方失败"); return; }
                    RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters { { "Formula", result.formula } }));
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "保存配方时发生异常"); await ShowErrorMessageAsync("保存配方时发生系统错误"); }
            finally { SetIsBusy(false); }
        }

        private void DeleteHerb(FormulaHerbItemViewModel? herbItem)
        {
            if (herbItem == null) return;
            try { HerbItems.Remove(herbItem); RaisePropertyChanged(nameof(HerbCount)); EnsureMinimumBlankRows(); }
            catch (Exception ex) { Logger.LogError(ex, "删除药材时发生异常"); _ = ShowErrorMessageAsync("删除药材失败"); }
        }

        private void OnDosageCompleted(FormulaHerbItemViewModel? herbItem)
        {
            if (herbItem == null) return;
            try
            {
                if (herbItem.HerbId != Guid.Empty)
                {
                    var duplicates = HerbItems.Where(h => h.HerbId == herbItem.HerbId && h != herbItem).ToList();
                    if (duplicates.Any())
                    {
                        var maxQty = Math.Max(herbItem.Dosage, duplicates.Max(d => d.Dosage));
                        herbItem.Dosage = maxQty;
                        foreach (var dup in duplicates) HerbItems.Remove(dup);
                        _ = ShowWarningMessageAsync($"{herbItem.HerbName}有重复，剂量改为{maxQty}g");
                    }
                }
                RaisePropertyChanged(nameof(HerbCount));
            }
            catch (Exception ex) { Logger.LogError(ex, "剂量输入完成处理时发生异常"); _ = ShowErrorMessageAsync("处理剂量输入失败"); }
        }

        private void AddNewRow()
        {
            try { for (int i = 0; i < 4; i++) HerbItems.Add(CreateBlankHerbItem()); }
            catch (Exception ex) { Logger.LogError(ex, "添加新行时发生异常"); }
        }

        private void OnHerbSelected(HerbDetailDto? selectedHerb)
        {
            if (selectedHerb == null) return;
            try
            {
                var currentItem = HerbItems.FirstOrDefault(h => h.HerbId == selectedHerb.Id || (string.IsNullOrEmpty(h.HerbName) && h.HerbId == Guid.Empty));
                if (currentItem != null)
                {
                    currentItem.HerbId = selectedHerb.Id;
                    currentItem.HerbName = selectedHerb.Name ?? string.Empty;
                    currentItem.Unit = selectedHerb.Unit ?? string.Empty;
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "药材选择处理时发生异常"); _ = ShowErrorMessageAsync("处理药材选择失败"); }
        }

        private void EnsureMinimumBlankRows()
        {
            var blankSlots = HerbItems.Count(h => h.HerbId == Guid.Empty);
            while (blankSlots < 4) { HerbItems.Add(CreateBlankHerbItem()); blankSlots++; }
        }

        private FormulaHerbItemViewModel CreateBlankHerbItem() => new() { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = string.Empty, AllHerbs = _allHerbs };

        private async Task LoadAllHerbsAsync()
        {
            try
            {
                _allHerbs.Clear();
                const int pageSize = 100;
                int currentPage = 1;
                while (true)
                {
                    var pagedResult = await _herbCommandHandler.GetPagedAsync(currentPage, pageSize);
                    if (pagedResult?.Items == null || !pagedResult.Items.Any()) break;
                    foreach (var herb in pagedResult.Items) _allHerbs.Add(herb);
                    if (pagedResult.Items.Count < pageSize) break;
                    currentPage++;
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "加载药材列表时发生异常"); }
        }
    }
}
