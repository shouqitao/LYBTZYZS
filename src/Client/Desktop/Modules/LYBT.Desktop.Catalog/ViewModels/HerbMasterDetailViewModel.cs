// P2-14-3 Herb/Formula BatchImport 命令抽取评估：已 via MasterDetailCommandGroup 抽公共命令，CanExecute 仍各VM重复已评估参数化收益<独立演进成本
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Catalog.Mappers;
using LYBT.Desktop.Catalog.Models;
using LYBT.Desktop.Catalog.ViewModels.Handlers;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Utilities.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace LYBT.Desktop.Catalog.ViewModels
{
    /// <summary>
    /// 药材Master-Detail视图模型（组合模式）
    ///
    /// 使用IMasterDetailServices实现组合模式
    /// HerbEditorViewModel子VM封装编辑逻辑
    /// </summary>
    public partial class HerbMasterDetailViewModel : MasterDetailViewModelBase<HerbListDto, HerbDetailModel>
    {
        private readonly IHerbService _herbService;
        private readonly IHerbStatusHandler _statusHandler;
        private readonly IDesktopCacheManager _cacheManager;
        private readonly HerbDetailModelMapper _herbMapper;

        /// <summary>药材编辑子 VM</summary>
        public HerbEditorViewModel HerbEditor { get; }

        #region 扩展属性

        /// <inheritdoc/>
        protected override string EntityDisplayName => "药材";

        /// <inheritdoc/>
        protected override string? GetDetailDisplayName() => CurrentDetail?.Name;

        /// <summary>是否允许编辑名称（新建时允许，编辑时不允许）</summary>
        public bool IsNameEditable => IsNew;

        /// <summary>状态选项</summary>
        public ObservableCollection<CommonStatus> StatusOptions { get; }

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public HerbMasterDetailViewModel(
            IViewModelServices viewModelServices,
            IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServices,
            IHerbService herbService,
            IHerbStatusHandler statusHandler,
            IDesktopCacheManager cacheManager,
            HerbDetailModelMapper herbMapper,
            HerbEditorViewModel herbEditor)
            : base(viewModelServices, masterDetailServices)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _herbMapper = herbMapper ?? throw new ArgumentNullException(nameof(herbMapper));
            HerbEditor = herbEditor ?? throw new ArgumentNullException(nameof(herbEditor));

            PageTitle = "药材管理";
            StatusOptions = new ObservableCollection<CommonStatus>(CommonOptions.StatusOptions);
        }

        #region 基类抽象方法实现

        /// <summary>加载列表数据</summary>
        protected override async Task LoadListAsync()
        {
            Logger.LogInformation("药材搜索: 第{Page}页, 每页{PageSize}条, 关键词: '{SearchText}'",
                CurrentPage, PageSize, SearchText);

            try
            {
                await MasterDetailServices.Loading.ExecuteWithLoadingAsync(async () =>
                {
                    var result = await _herbService.GetPagedAsync(CurrentPage, PageSize, SearchText);
                    if (!result.Success || result.Data == null)
                    {
                        MasterDetailServices.ErrorHandler.SetError("LoadList", result.Error ?? "获取药材列表失败");
                        return;
                    }

                    MasterDetailServices.Pagination.TotalCount = result.Data.TotalCount;

                    Items.Clear();
                    foreach (var item in result.Data.Items ?? Enumerable.Empty<HerbListDto>())
                    {
                        Items.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取药材列表时发生异常");
                MasterDetailServices.ErrorHandler.HandleException(ex, "获取药材列表");
            }
        }

        /// <summary>加载详情数据</summary>
        protected override async Task LoadDetailAsync(HerbListDto item)
        {
            var result = await _herbService.GetByIdAsync(item.Id);
            if (!result.Success || result.Data == null)
            {
                MasterDetailServices.ErrorHandler.SetError("LoadDetail", result.Error ?? "加载药材详情失败");
                return;
            }

            // D1: 改用 Mapperly HerbDetailModelMapper，替代手写 new HerbDetailModel + new HerbDetailDto 双重映射
            var detail = _herbMapper.ToItem(result.Data);

            HerbEditor.InitializeFromDto(result.Data);
            OnPropertyChanged(nameof(IsNameEditable));
        }

        /// <summary>创建新详情实例</summary>
        protected override HerbDetailModel CreateNewDetail()
        {
            HerbEditor.InitializeForNewCase();
            var detail = HerbDetailModel.CreateNew();
            OnPropertyChanged(nameof(IsNameEditable));
            return detail;
        }

        /// <summary>保存详情</summary>
        protected override async Task<bool> SaveDetailAsync(HerbDetailModel detail)
        {
            if (!HerbEditor.Validate())
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("请修正验证错误后重试", "验证失败");
                return false;
            }

            var input = HerbEditor.GetHerbData();
            var result = HerbEditor.Herb.Id == Guid.Empty
                ? await _herbService.CreateAsync(input)
                : await _herbService.UpdateAsync(input);

            if (!result.Success)
            {
                MasterDetailServices.ErrorHandler.SetError("Save", result.Error ?? "保存药材失败");
                return false;
            }

            if (result.Data != null)
            {
                detail.Id = result.Data.Id;
                detail.Name = result.Data.Name;
                detail.PinYinCode = result.Data.PinYinCode ?? detail.PinYinCode ?? string.Empty;
                detail.Origin = result.Data.Origin;
                detail.Spec = result.Data.Spec;
                detail.Unit = result.Data.Unit;
                detail.Price = result.Data.Price;
                detail.CostPrice = result.Data.CostPrice;
                detail.Effect = result.Data.Effect;
                detail.Usage = result.Data.Usage;
                detail.Remark = result.Data.Remark;
                detail.Status = result.Data.Status;
            }

            _cacheManager.InvalidateHerbCaches();
            return true;
        }

        /// <summary>删除项</summary>
        protected override async Task<bool> DeleteItemAsync(HerbListDto item)
        {
            var result = await _herbService.DeleteAsync(item.Id);
            if (!result.Success)
            {
                MasterDetailServices.ErrorHandler.SetError("Delete", result.Error ?? "删除药材失败");
                return false;
            }

            _cacheManager.InvalidateHerbCaches();
            return true;
        }

        #endregion

        #region 扩展命令

        /// <summary>切换药材状态</summary>
        [RelayCommand(CanExecute = nameof(CanToggleStatus))]
        private async Task ToggleStatusAsync()
        {
            if (SelectedItem == null) return;
            if (await _statusHandler.ToggleStatusAsync(SelectedItem))
            {
                _cacheManager.InvalidateHerbCaches();
                await RefreshAsync();
            }
        }

        private bool CanToggleStatus() => HasSelection && !IsBusy;

        /// <summary>复制药材</summary>
        [RelayCommand(CanExecute = nameof(CanCopyHerb))]
        private void CopyHerb()
        {
            if (CurrentDetail == null) return;

            var copy = CurrentDetail.Clone();
            copy.Id = Guid.Empty;
            copy.Name = $"{copy.Name}_副本";
            copy.PinYinCode = PinYinHelper.GetPinYinCode(copy.Name);
            copy.Status = CommonStatus.Enabled;

            MasterDetailServices.DetailEditor.CreateNew(() => copy);

            Logger.LogInformation("复制药材: {SourceName} -> {CopyName}", SelectedItem?.Name, copy.Name);
        }

        private bool CanCopyHerb() => HasSelection && !IsBusy && IsAdmin;

        /// <inheritdoc/>
        protected override async Task InvalidateCachesAsync()
        {
            _cacheManager.InvalidateHerbCaches();
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override async Task RestoreItemAsync(HerbListDto item)
        {
            await _statusHandler.RestoreAsync(item);
        }

        /// <summary>批量删除（单次 batch-delete 调用，替代逐条删除）</summary>
        protected override async Task DeleteBatchAsync(List<HerbListDto> items)
        {
            var result = await _herbService.BatchDeleteAsync(items.Select(h => h.Id).ToList());
            if (result.Success && result.Data != null)
            {
                Logger.LogInformation("药材批量删除完成: 成功 {Success}, 失败 {Failure}",
                    result.Data.SuccessCount, result.Data.FailureCount);
                _cacheManager.InvalidateHerbCaches();

                // P1-C：部分失败需提示用户
                if (result.Data.FailureCount > 0)
                {
                    await MasterDetailServices.Dialog.ShowWarningAsync(
                        $"批量删除完成：成功 {result.Data.SuccessCount} 条，失败 {result.Data.FailureCount} 条", "部分失败");
                }
            }
            else
            {
                MasterDetailServices.ErrorHandler.SetError("BatchDelete", result.Error ?? "批量删除药材失败");
            }
        }

        /// <summary>批量启用（单次 batch-enable 调用）</summary>
        protected override async Task EnableBatchAsync(List<HerbListDto> items)
            => await BatchSetStatusAsync(items, CommonStatus.Enabled, "批量启用");

        /// <summary>批量禁用（单次 batch-disable 调用）</summary>
        protected override async Task DisableBatchAsync(List<HerbListDto> items)
            => await BatchSetStatusAsync(items, CommonStatus.Disabled, "批量禁用");

        private async Task BatchSetStatusAsync(List<HerbListDto> items, CommonStatus status, string operationName)
        {
            var result = await _herbService.BatchSetStatusAsync(items.Select(h => h.Id).ToList(), status);
            if (result.Success && result.Data != null)
            {
                Logger.LogInformation("{Operation}完成: 成功 {Success}, 失败 {Failure}",
                    operationName, result.Data.SuccessCount, result.Data.FailureCount);
                _cacheManager.InvalidateHerbCaches();

                // P1-C：部分失败需提示用户
                if (result.Data.FailureCount > 0)
                {
                    await MasterDetailServices.Dialog.ShowWarningAsync(
                        $"{operationName}完成：成功 {result.Data.SuccessCount} 条，失败 {result.Data.FailureCount} 条", "部分失败");
                }
            }
            else
            {
                MasterDetailServices.ErrorHandler.SetError(operationName, result.Error ?? $"{operationName}药材失败");
            }
        }

        /// <summary>按分类搜索</summary>
        [RelayCommand]
        private async Task SearchByCategoryAsync(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;

            Logger.LogInformation("按分类搜索药材: {Category}", category);
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

        /// <summary>下载导入模板（US-HERB-013）</summary>
        [RelayCommand]
        private async Task DownloadImportTemplateAsync()
        {
            try
            {
                var result = await _herbService.ExportTemplateAsync();
                if (!result.Success || result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "下载导入模板失败", "操作失败");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON 文件|*.json",
                    FileName = "药材导入模板.json",
                    Title = "保存导入模板"
                };
                if (dialog.ShowDialog() != true) return;

                await File.WriteAllBytesAsync(dialog.FileName, result.Data);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"模板已保存到：{dialog.FileName}", "下载成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "下载药材导入模板失败");
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("下载模板", ex), "操作失败");
            }
        }

        /// <summary>批量导入药材（US-HERB-006）</summary>
        [RelayCommand]
        private async Task ImportHerbsAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON 文件|*.json",
                Title = "选择药材导入文件"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var request = JsonSerializer.Deserialize<HerbBatchImportInputDto>(json, ImportJsonOptions);
                if (request?.Herbs == null || request.Herbs.Count == 0)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync("文件中没有药材数据，请检查格式", "导入失败");
                    return;
                }

                var confirmed = await MasterDetailServices.Dialog.ShowConfirmAsync(
                    $"将导入 {request.Herbs.Count} 条药材记录（重复策略：{request.Strategy}），是否继续？", "确认导入");
                if (!confirmed) return;

                var result = await _herbService.BatchImportAsync(request);
                if (!result.Success || result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "批量导入失败", "操作失败");
                    return;
                }

                var data = result.Data;
                var msg = $"导入完成：成功 {data.SuccessCount} 条，失败 {data.FailureCount} 条，跳过 {data.SkippedCount} 条";
                await MasterDetailServices.Dialog.ShowSuccessAsync(msg, "导入结果");
                _cacheManager.InvalidateHerbCaches();
                await RefreshAsync();
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex, "解析药材导入文件失败: {File}", dialog.FileName);
                await MasterDetailServices.Dialog.ShowErrorAsync("文件格式错误，请使用下载的 JSON 模板", "导入失败");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量导入药材失败: {File}", dialog.FileName);
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入药材", ex), "操作失败");
            }
        }

        /// <summary>导出药材数据（US-HERB-007/013）</summary>
        [RelayCommand]
        private async Task ExportHerbsAsync()
        {
            try
            {
                var result = await _herbService.ExportHerbsAsync(SearchText);
                if (!result.Success || result.Data == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync(result.Error ?? "导出药材数据失败", "操作失败");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON 文件|*.json",
                    FileName = $"药材导出_{DateTime.Now:yyyyMMddHHmmss}.json",
                    Title = "保存导出文件"
                };
                if (dialog.ShowDialog() != true) return;

                await File.WriteAllBytesAsync(dialog.FileName, result.Data);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"已导出 {result.Data.Length} 字节到：{dialog.FileName}", "导出成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出药材数据失败");
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导出药材", ex), "操作失败");
            }
        }

        #endregion
    }
}
