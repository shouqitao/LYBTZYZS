using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Catalog.Models;
using LYBT.Desktop.Catalog.ViewModels.Handlers;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using Microsoft.Win32;

namespace LYBT.Desktop.Catalog.ViewModels
{
    /// <summary>
    /// 验方Master-Detail视图模型（组合模式）
    ///
    /// 使用IFormulaService单依赖 + FormulaEditor子VM模式
    /// 所有编辑操作通过FormulaEditor封装
    /// </summary>
    public partial class FormulaMasterDetailViewModel : MasterDetailViewModelBase<FormulaListDto, FormulaDetailModel>
    {
        private readonly IFormulaService _formulaService;
        private readonly IFormulaStatusHandler _statusHandler;
        private readonly IHerbSearchProvider _herbSearchProvider;
        private readonly IDesktopCacheManager _cacheManager;

        /// <summary>验方编辑子 VM</summary>
        public FormulaEditorViewModel FormulaEditor { get; }

        // 所有药材列表（用于拼音码快速匹配）
        private readonly ObservableCollection<HerbListDto> _allHerbs = new();

        #region 扩展属性

        /// <inheritdoc/>
        protected override string EntityDisplayName => "验方";

        /// <inheritdoc/>
        protected override string NewEntityVerb => "新增";

        /// <inheritdoc/>
        protected override string? GetDetailDisplayName() => CurrentDetail?.Name;

        /// <summary>所有药材列表（用于拼音码快速匹配）</summary>
        public IEnumerable<HerbListDto> AllHerbs => _allHerbs;

        /// <summary>是否为验方校验模式（列表仅显示 Draft 待校验验方——US-FORM-007）</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNormalMode))]
        [NotifyPropertyChangedFor(nameof(DetailTitle))]
        private bool _isValidationMode;

        /// <summary>非校验模式（用于隐藏校验模式下不适用的搜索框等）</summary>
        public bool IsNormalMode => !IsValidationMode;

        /// <summary>当前选中详情是否已按校验视图加载（决定校验面板可见性）</summary>
        [ObservableProperty]
        private bool _isValidationDetail;

        /// <summary>校验面板当前验方名</summary>
        [ObservableProperty]
        private string _validationFormulaName = string.Empty;

        /// <summary>待绑定药材数（校验面板标题）</summary>
        [ObservableProperty]
        private int _validationPendingCount;

        /// <summary>校验面板药材行（含已绑定/待绑定）</summary>
        public ObservableCollection<FormulaValidationItemViewModel> ValidationRows { get; } = new();

        #endregion

        #region 详情标题（校验模式覆盖）

        /// <inheritdoc/>
        public override string DetailTitle
        {
            get
            {
                if (IsValidationDetail && SelectedItem != null)
                    return $"待校验 · {SelectedItem.Name}";
                return base.DetailTitle;
            }
        }

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public FormulaMasterDetailViewModel(
            IViewModelServices viewModelServices,
            IMasterDetailServices<FormulaListDto, FormulaDetailModel> masterDetailServices,
            IFormulaService formulaService,
            IFormulaStatusHandler statusHandler,
            IHerbSearchProvider herbSearchProvider,
            IDesktopCacheManager cacheManager,
            FormulaEditorViewModel formulaEditor)
            : base(viewModelServices, masterDetailServices)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
            _herbSearchProvider = herbSearchProvider ?? throw new ArgumentNullException(nameof(herbSearchProvider));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            FormulaEditor = formulaEditor ?? throw new ArgumentNullException(nameof(formulaEditor));

            PageTitle = "验方管理";
        }

        #region 基类抽象方法实现

        /// <summary>加载列表数据</summary>
        protected override async Task LoadListAsync()
        {
            Logger.LogInformation("验方搜索: 第{Page}页, 每页{PageSize}条, 关键词: '{SearchText}', 校验模式: {IsValidationMode}",
                CurrentPage, PageSize, SearchText, IsValidationMode);

            try
            {
                await MasterDetailServices.Loading.ExecuteWithLoadingAsync(async () =>
                {
                    // US-FORM-007：校验模式走待校验端点（Draft），普通模式走全量分页
                    if (IsValidationMode)
                    {
                        var pending = await _formulaService.GetPendingValidationAsync(CurrentPage, PageSize);
                        if (!pending.Success || pending.Data == null)
                        {
                            MasterDetailServices.ErrorHandler.SetError("Load", pending.Error ?? "加载待校验验方失败");
                            return;
                        }

                        var pendingData = pending.Data;
                        MasterDetailServices.Pagination.TotalCount = pendingData.TotalCount;
                        Items.Clear();
                        foreach (var detail in pendingData.Items ?? Enumerable.Empty<FormulaDetailDto>())
                        {
                            Items.Add(ToListDto(detail));
                        }
                        return;
                    }

                    var result = await _formulaService.GetPagedAsync(CurrentPage, PageSize, SearchText);
                    if (!result)
                    {
                        MasterDetailServices.ErrorHandler.SetError("Load", result.Error ?? "加载验方列表失败");
                        return;
                    }

                    var pagedData = result.Data!;
                    MasterDetailServices.Pagination.TotalCount = pagedData.TotalCount;

                    Items.Clear();
                    foreach (var item in pagedData.Items ?? Enumerable.Empty<FormulaListDto>())
                    {
                        Items.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取验方列表时发生异常");
                MasterDetailServices.ErrorHandler.HandleException(ex, "获取验方列表");
            }
        }

        /// <summary>待校验详情 DTO → 列表 DTO（校验模式下待校验端点返回 DetailDto，转轻量列表项）</summary>
        private static FormulaListDto ToListDto(FormulaDetailDto d)
        {
            return new FormulaListDto
            {
                Id = d.Id,
                Name = d.Name,
                Effect = d.Effect,
                Indication = d.Indication,
                Category = d.Category,
                IsShared = d.IsShared,
                Status = d.Status,
                ValidationStatus = d.ValidationStatus,
                HerbCount = d.HerbCount,
                TotalPrice = d.TotalPrice,
                CreatedAt = d.CreatedAt
            };
        }

        /// <summary>加载详情数据</summary>
        protected override async Task LoadDetailAsync(FormulaListDto item)
        {
            try
            {
                // 校验模式：详情区切换为校验面板（US-FORM-008 绑定流程）
                if (IsValidationMode)
                {
                    await LoadValidationDetailAsync(item.Id);
                    return;
                }

                var result = await _formulaService.GetByIdAsync(item.Id);
                if (!result)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(
                        result.Error ?? $"验方 '{item.Name}' 不存在或已被删除", "加载失败");
                    return;
                }

                FormulaEditor.InitializeFromDto(result.Data!);
                FormulaEditor.SetAllHerbs(_allHerbs);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载验方详情失败: {FormulaId}", item.Id);
                MasterDetailServices.ErrorHandler.HandleException(ex, "加载验方详情");
            }
        }

        /// <summary>加载校验详情（含每味药材的绑定状态）</summary>
        private async Task LoadValidationDetailAsync(Guid formulaId)
        {
            ValidationRows.Clear();
            var result = await _formulaService.GetByIdAsync(formulaId);
            if (!result || result.Data == null)
            {
                IsValidationDetail = false;
                await MasterDetailServices.Dialog.ShowErrorAsync(
                    result.Error ?? "加载验方详情失败", "加载失败");
                return;
            }

            var dto = result.Data;
            ValidationFormulaName = dto.Name;

            var pendingCount = 0;
            foreach (var herb in dto.Herbs ?? Enumerable.Empty<FormulaHerbItemDto>())
            {
                if (!herb.IsValidated) pendingCount++;
                ValidationRows.Add(new FormulaValidationItemViewModel
                {
                    HerbItemId = herb.Id,
                    OriginalHerbName = herb.OriginalHerbName,
                    BoundHerbName = herb.HerbName,
                    Dosage = herb.Dosage,
                    Unit = herb.Unit ?? string.Empty,
                    IsValidated = herb.IsValidated
                });
            }

            ValidationPendingCount = pendingCount;
            IsValidationDetail = true;
            OnPropertyChanged(nameof(DetailTitle));
            OnPropertyChanged(nameof(IsValidationDetail));
        }

        #region 验方校验命令（US-FORM-007/008——Desktop 校验 UI）

        /// <summary>切换「待校验」模式</summary>
        [RelayCommand]
        private async Task ToggleValidationModeAsync()
        {
            IsValidationMode = !IsValidationMode;
            ValidationRows.Clear();
            IsValidationDetail = false;
            SearchText = string.Empty;
            MasterDetailServices.Pagination.CurrentPage = 1;
            Logger.LogInformation("切换验方校验模式: {IsValidationMode}", IsValidationMode);
            await RefreshAsync();
        }

        /// <summary>校验绑定某味药材到系统药材库</summary>
        [RelayCommand]
        private async Task ValidateHerbAsync(FormulaValidationItemViewModel? row)
        {
            if (row == null || row.IsBinding) return;
            if (row.SelectedHerbId == null || row.SelectedHerbId == Guid.Empty)
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("请先从列表选择要绑定的系统药材", "校验失败");
                return;
            }

            var formulaId = SelectedItem?.Id ?? Guid.Empty;
            if (formulaId == Guid.Empty) return;

            row.IsBinding = true;
            try
            {
                var selectedHerbId = row.SelectedHerbId.Value;
                var result = await _formulaService.ValidateHerbAsync(formulaId, row.HerbItemId, selectedHerbId);
                if (!result.Success)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "药材校验失败", "校验失败");
                    return;
                }

                _cacheManager.InvalidateFormulaCaches();
                await MasterDetailServices.Dialog.ShowSuccessAsync($"「{row.DisplayName}」已绑定为系统药材", "校验成功");

                // 重载详情刷新绑定状态；若最后一味绑定完成服务端已自动晋升 Validated，列表将不再出现该验方
                await LoadValidationDetailAsync(formulaId);
                if (ValidationPendingCount == 0)
                {
                    await MasterDetailServices.Dialog.ShowSuccessAsync($"验方「{ValidationFormulaName}」已全部校验完成", "校验完成");
                }
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "校验验方药材失败: FormulaId={FormulaId}, HerbItemId={HerbItemId}", formulaId, row.HerbItemId);
                await MasterDetailServices.Dialog.ShowErrorAsync(
                    ClientErrorMessageMapper.GetSafeOperationFailureMessage("校验药材", ex), "校验失败");
            }
            finally
            {
                row.IsBinding = false;
            }
        }

        #endregion

        /// <summary>创建新详情实例</summary>
        protected override FormulaDetailModel CreateNewDetail()
        {
            FormulaEditor.InitializeForNewCase();
            FormulaEditor.SetAllHerbs(_allHerbs);
            return FormulaDetailModel.CreateNew();
        }

        /// <summary>保存详情</summary>
        protected override async Task<bool> SaveDetailAsync(FormulaDetailModel detail)
        {
            if (!FormulaEditor.Validate())
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("请修正验证错误后重试", "验证失败");
                return false;
            }

            // P1-B：逐行校验药材选择与剂量（Dosage 数据注解不自动执行——ObservableObject 非 ValidatableModelBase）
            foreach (var herbItem in FormulaEditor.EditHerbItems)
            {
                if (herbItem.HerbId == Guid.Empty || string.IsNullOrWhiteSpace(herbItem.HerbName))
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync("存在未选择药材的行，请补全或删除", "验证失败");
                    return false;
                }
                if (herbItem.Dosage is < 1 or > 500)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync($"药材「{herbItem.HerbName}」剂量需在 1-500 之间", "验证失败");
                    return false;
                }
            }

            try
            {
                var herbInputDtos = FormulaEditor.GetHerbInputDtos();
                var formula = FormulaEditor.Formula;

                var input = new FormulaInputDto
                {
                    Id = formula.Id.OrNullIfEmpty(),
                    Name = formula.Name,
                    Effect = formula.Effect ?? string.Empty,
                    Usage = formula.Usage ?? string.Empty,
                    Property = formula.Property ?? string.Empty,
                    Category = formula.Category ?? string.Empty,
                    Remark = formula.Remark ?? string.Empty,
                    IsShared = formula.IsShared,
                    Herbs = herbInputDtos
                };

                var result = formula.Id == Guid.Empty
                    ? await _formulaService.CreateAsync(input)
                    : await _formulaService.UpdateAsync(input);

                if (!result)
                {
                    MasterDetailServices.ErrorHandler.SetError("Save", result.Error ?? "保存验方失败");
                    return false;
                }

                var savedFormula = result.Data!;
                detail.Id = savedFormula.Id;
                detail.CreatedAt = savedFormula.CreatedAt;
                detail.UpdatedAt = savedFormula.UpdatedAt;
                Logger.LogInformation("验方保存成功: {FormulaId} - {FormulaName}", savedFormula.Id, savedFormula.Name);

                _cacheManager.InvalidateFormulaCaches();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存验方失败: {FormulaName}", detail.Name);
                MasterDetailServices.ErrorHandler.SetError("Save", "保存验方时发生异常，请重试");
                return false;
            }
        }

        /// <summary>删除项</summary>
        protected override async Task<bool> DeleteItemAsync(FormulaListDto item)
        {
            var result = await _formulaService.DeleteAsync(item.Id);
            if (!result)
            {
                MasterDetailServices.ErrorHandler.SetError("Delete", result.Error ?? $"删除验方 '{item.Name}' 失败");
                return false;
            }

            Logger.LogInformation("验方删除成功: {FormulaId} - {FormulaName}", item.Id, item.Name);
            _cacheManager.InvalidateFormulaCaches();
            return true;
        }

        #endregion

        #region 扩展命令

        /// <summary>切换验方状态</summary>
        [RelayCommand(CanExecute = nameof(CanToggleStatus))]
        private async Task ToggleStatusAsync()
        {
            if (SelectedItem == null) return;
            if (await _statusHandler.ToggleStatusAsync(SelectedItem))
            {
                _cacheManager.InvalidateFormulaCaches();
                await RefreshAsync();
            }
        }

        private bool CanToggleStatus() => HasSelection && !IsBusy;

        /// <summary>复制验方</summary>
        [RelayCommand(CanExecute = nameof(CanCopyFormula))]
        private async Task CopyFormulaAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                var confirmed = await MasterDetailServices.Dialog.ShowConfirmAsync($"确认复制验方 [{SelectedItem.Name}] 吗？", "复制确认");
                if (!confirmed) return;

                var detailResult = await _formulaService.GetByIdAsync(SelectedItem.Id);
                if (!detailResult)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(detailResult.Error ?? "获取验方详情失败", "操作失败");
                    return;
                }

                var copyResult = await _formulaService.CopyFormulaAsync(detailResult.Data!);
                if (copyResult)
                {
                    Logger.LogInformation("验方复制成功: {SourceName} -> {NewName}", SelectedItem.Name, copyResult.Data!.Name);
                    await MasterDetailServices.Dialog.ShowSuccessAsync($"验方已复制为 '{copyResult.Data!.Name}'", "操作成功");
                    _cacheManager.InvalidateFormulaCaches();
                    await RefreshAsync();
                }
                else
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(copyResult.Error ?? "复制验方失败", "操作失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "复制验方失败");
                await MasterDetailServices.Dialog.ShowErrorAsync("复制验方失败", "操作失败");
            }
        }

        private bool CanCopyFormula() => HasSelection && !IsBusy;

        /// <inheritdoc/>
        protected override async Task InvalidateCachesAsync()
        {
            _cacheManager.InvalidateFormulaCaches();
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override async Task RestoreItemAsync(FormulaListDto item)
        {
            await _statusHandler.RestoreAsync(item);
        }

        /// <summary>批量删除（单次 batch-delete 调用，替代逐条删除）</summary>
        protected override async Task DeleteBatchAsync(List<FormulaListDto> items)
        {
            var result = await _formulaService.BatchDeleteAsync(items.Select(f => f.Id).ToList());
            if (result.Success && result.Data != null)
            {
                Logger.LogInformation("验方批量删除完成: 成功 {Success}, 失败 {Failure}",
                    result.Data.SuccessCount, result.Data.FailureCount);
                _cacheManager.InvalidateFormulaCaches();

                // P1-C：部分失败需提示用户
                if (result.Data.FailureCount > 0)
                {
                    await MasterDetailServices.Dialog.ShowWarningAsync(
                        $"批量删除完成：成功 {result.Data.SuccessCount} 条，失败 {result.Data.FailureCount} 条", "部分失败");
                }
            }
            else
            {
                MasterDetailServices.ErrorHandler.SetError("BatchDelete", result.Error ?? "批量删除验方失败");
            }
        }

        /// <summary>批量启用（单次 batch-enable 调用）</summary>
        protected override async Task EnableBatchAsync(List<FormulaListDto> items)
            => await BatchSetStatusAsync(items, CommonStatus.Enabled, "批量启用");

        /// <summary>批量禁用（单次 batch-disable 调用）</summary>
        protected override async Task DisableBatchAsync(List<FormulaListDto> items)
            => await BatchSetStatusAsync(items, CommonStatus.Disabled, "批量禁用");

        private async Task BatchSetStatusAsync(List<FormulaListDto> items, CommonStatus status, string operationName)
        {
            var result = await _formulaService.BatchSetStatusAsync(items.Select(f => f.Id).ToList(), status);
            if (result.Success && result.Data != null)
            {
                Logger.LogInformation("{Operation}完成: 成功 {Success}, 失败 {Failure}",
                    operationName, result.Data.SuccessCount, result.Data.FailureCount);
                _cacheManager.InvalidateFormulaCaches();

                // P1-C：部分失败需提示用户
                if (result.Data.FailureCount > 0)
                {
                    await MasterDetailServices.Dialog.ShowWarningAsync(
                        $"{operationName}完成：成功 {result.Data.SuccessCount} 条，失败 {result.Data.FailureCount} 条", "部分失败");
                }
            }
            else
            {
                MasterDetailServices.ErrorHandler.SetError(operationName, result.Error ?? $"{operationName}验方失败");
            }
        }

        /// <summary>添加药材行</summary>
        [RelayCommand(CanExecute = nameof(CanAddHerb))]
        private void AddHerb()
        {
            FormulaEditor.AddHerb(_allHerbs);
        }

        private bool CanAddHerb() => IsEditMode;

        /// <summary>删除药材行</summary>
        [RelayCommand(CanExecute = nameof(CanDeleteHerb))]
        private void DeleteHerb(FormulaHerbItemViewModel? herb)
        {
            if (herb == null) return;
            FormulaEditor.DeleteHerb(herb);
        }

        private bool CanDeleteHerb(FormulaHerbItemViewModel? herb) => herb != null && IsEditMode;

        /// <summary>按分类搜索</summary>
        [RelayCommand]
        private async Task SearchByCategoryAsync(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;

            Logger.LogInformation("按分类搜索验方: {Category}", category);
            SearchText = $"分类:{category}";
            await RefreshAsync();
        }

        #endregion

        #region 批量导入/导出命令

        /// <summary>导入 JSON 文件反序列化选项（camelCase + 枚举字符串，ADR-0022 对齐）</summary>
        private static readonly JsonSerializerOptions ImportJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>下载导入模板（US-FORM-013）</summary>
        [RelayCommand]
        private async Task DownloadImportTemplateAsync()
        {
            try
            {
                var result = await _formulaService.ExportTemplateAsync();
                if (!result.Success || result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "下载导入模板失败", "操作失败");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON 文件|*.json",
                    FileName = "验方导入模板.json",
                    Title = "保存导入模板"
                };
                if (dialog.ShowDialog() != true) return;

                await File.WriteAllBytesAsync(dialog.FileName, result.Data);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"模板已保存到：{dialog.FileName}", "下载成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "下载验方导入模板失败");
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("下载模板", ex), "操作失败");
            }
        }

        /// <summary>批量导入验方（US-FORM-006）</summary>
        [RelayCommand]
        private async Task ImportFormulasAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON 文件|*.json",
                Title = "选择验方导入文件"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var request = JsonSerializer.Deserialize<FormulaBatchImportInputDto>(json, ImportJsonOptions);
                if (request?.Formulas == null || request.Formulas.Count == 0)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync("文件中没有验方数据，请检查格式", "导入失败");
                    return;
                }

                var confirmed = await MasterDetailServices.Dialog.ShowConfirmAsync(
                    $"将导入 {request.Formulas.Count} 条验方记录，是否继续？", "确认导入");
                if (!confirmed) return;

                var result = await _formulaService.BatchImportAsync(request);
                if (!result.Success || result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "批量导入失败", "操作失败");
                    return;
                }

                var data = result.Data;
                var msg = $"导入完成：成功 {data.SuccessCount} 条，失败 {data.FailureCount} 条，匹配药材 {data.MatchedHerbsCount} 味";
                await MasterDetailServices.Dialog.ShowSuccessAsync(msg, "导入结果");
                _cacheManager.InvalidateFormulaCaches();
                await RefreshAsync();
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex, "解析验方导入文件失败: {File}", dialog.FileName);
                await MasterDetailServices.Dialog.ShowErrorAsync("文件格式错误，请使用下载的 JSON 模板", "导入失败");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量导入验方失败: {File}", dialog.FileName);
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入验方", ex), "操作失败");
            }
        }

        /// <summary>导出验方数据（US-FORM-013）</summary>
        [RelayCommand]
        private async Task ExportFormulasAsync()
        {
            try
            {
                var result = await _formulaService.ExportFormulasAsync();
                if (!result.Success || result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "导出验方数据失败", "操作失败");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON 文件|*.json",
                    FileName = $"验方导出_{DateTime.Now:yyyyMMddHHmmss}.json",
                    Title = "保存导出文件"
                };
                if (dialog.ShowDialog() != true) return;

                await File.WriteAllBytesAsync(dialog.FileName, result.Data);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"已导出 {result.Data.Length} 字节到：{dialog.FileName}", "导出成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出验方数据失败");
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导出验方", ex), "操作失败");
            }
        }

        #endregion

        #region 导航

        protected override async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            await base.OnNavigatedToAsync(navigationContext);
            await LoadAllHerbsAsync();
        }

        /// <summary>加载所有药材列表</summary>
        private async Task LoadAllHerbsAsync()
        {
            try
            {
                Logger.LogDebug("开始加载所有药材列表");
                _allHerbs.Clear();
                var herbs = await _herbSearchProvider.GetAllHerbsAsync();
                foreach (var herb in herbs) _allHerbs.Add(herb);
                Logger.LogInformation("成功加载 {Count} 个药材", _allHerbs.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材列表时发生异常");
            }
        }

        #endregion

        #region Disposal

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
