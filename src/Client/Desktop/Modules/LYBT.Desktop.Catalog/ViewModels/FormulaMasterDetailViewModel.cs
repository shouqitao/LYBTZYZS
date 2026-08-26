using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            Logger.LogInformation("验方搜索: 第{Page}页, 每页{PageSize}条, 关键词: '{SearchText}'",
                CurrentPage, PageSize, SearchText);

            try
            {
                await MasterDetailServices.Loading.ExecuteWithLoadingAsync(async () =>
                {
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

        /// <summary>加载详情数据</summary>
        protected override async Task LoadDetailAsync(FormulaListDto item)
        {
            try
            {
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
