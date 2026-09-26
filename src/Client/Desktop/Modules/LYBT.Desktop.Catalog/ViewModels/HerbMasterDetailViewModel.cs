// P2-14-3 Herb/Formula BatchImport 命令抽取评估：已 via MasterDetailCommandGroup 抽公共命令，CanExecute 仍各VM重复已评估参数化收益<独立演进成本
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Catalog.Mappers;
using LYBT.Desktop.Catalog.Models;
using LYBT.Desktop.Catalog.Services;
using LYBT.Desktop.Catalog.ViewModels.Handlers;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Infrastructure.Models;
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
        private readonly IFileDialogService _fileDialogService;
        private readonly IHerbExcelService _herbExcelService;

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

        /// <summary>批量导入重复处理策略选项（AC②：跳过/更新/报错）</summary>
        public ReadOnlyCollection<DuplicateStrategyOption> ImportDuplicateStrategyOptions => DuplicateStrategyOptions.All;

        /// <summary>批量导入重复处理策略（AC②，随请求 DTO 传给服务端）</summary>
        [ObservableProperty]
        private DuplicateStrategy _importDuplicateStrategy = DuplicateStrategy.Skip;

        /// <summary>当前/最近一次批量导入进度（AC③：如「已导入 1000/2500 行」）</summary>
        public ImportProgressInfo ImportProgress { get; } = new();

        /// <summary>最近一次批量导入报告（AC⑤）；null 表示无可显示报告</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasImportReport))]
        private ImportReport? _importReport;

        /// <summary>是否存在可显示的导入报告（控制报告面板可见性）</summary>
        public bool HasImportReport => ImportReport != null;

        /// <summary>关闭导入报告面板</summary>
        [RelayCommand]
        private void CloseImportReport() => ImportReport = null;

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
            IFileDialogService fileDialogService,
            IHerbExcelService herbExcelService,
            HerbEditorViewModel herbEditor)
            : base(viewModelServices, masterDetailServices)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _herbMapper = herbMapper ?? throw new ArgumentNullException(nameof(herbMapper));
            _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
            _herbExcelService = herbExcelService ?? throw new ArgumentNullException(nameof(herbExcelService));
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

        /// <summary>
        /// I-1 修复：本模块自定义命令的 CanExecute 依赖 HasSelection/IsBusy/IsAdmin，
        /// 基类在 选中/忙碌/CRUD 状态变化时回调此钩子（基类命令无法覆盖子类命令）
        /// </summary>
        protected override void OnCrudCommandStateChanged()
        {
            ToggleStatusCommand.NotifyCanExecuteChanged();
            CopyHerbCommand.NotifyCanExecuteChanged();
        }

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

        /// <summary>
        /// 下载导入模板（US-HERB-013 / US-SHELL-021 AC①）：服务端 JSON 字段定义 → 本地 Excel 模板（.xlsx）。
        /// </summary>
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
                    Filter = "Excel 文件|*.xlsx",
                    FileName = "药材导入模板.xlsx",
                    Title = "保存导入模板"
                };
                if (dialog.ShowDialog() != true) return;

                // AC①：列定义以服务端 import-template 为权威，仅表现层渲染为 .xlsx（后端契约不变）
                var workbook = _herbExcelService.GenerateTemplate(result.Data);
                await File.WriteAllBytesAsync(dialog.FileName, workbook);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"模板已保存到：{dialog.FileName}", "下载成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "下载药材导入模板失败");
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("下载模板", ex), "操作失败");
            }
        }

        /// <summary>
        /// 批量导入药材（US-HERB-006 / US-SHELL-021 AC③⑤）：
        /// .xlsx → 批量导入 DTO（重复策略取自界面选择）→ 每批 ≤1000 行顺序提交 → 汇总导入报告。
        /// </summary>
        [RelayCommand]
        private async Task ImportHerbsAsync()
        {
            // P2-4: 经 IFileDialogService 抽象弹出打开对话框（原 Microsoft.Win32.OpenFileDialog，标题「选择药材导入文件」）
            var filePath = _fileDialogService.ShowOpenFileDialog("Excel 文件|*.xlsx", ".xlsx");
            if (filePath == null) return;

            try
            {
                HerbBatchImportInputDto request;
                await using (var stream = File.OpenRead(filePath))
                {
                    // AC①：Excel 列定义以服务端 import-template 为准；解析失败消息含行号+列名（AC②）
                    request = _herbExcelService.ParseImportFile(stream);
                }

                // AC②：解析器不决定策略，由界面选择（Skip/Update/Error）
                request.Strategy = ImportDuplicateStrategy;

                if (request.Herbs.Count == 0)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync("文件中没有药材数据，请检查格式", "导入失败");
                    return;
                }

                var confirmed = await MasterDetailServices.Dialog.ShowConfirmAsync(
                    $"将导入 {request.Herbs.Count} 条药材记录（{ImportBatchRunner.CountBatches(request.Herbs.Count)} 批，重复策略：{DuplicateStrategyOptions.GetDisplay(ImportDuplicateStrategy)}），是否继续？",
                    "确认导入");
                if (!confirmed) return;

                var report = await ImportHerbsInBatchesAsync(request.Herbs);
                ImportReport = report;

                Logger.LogInformation("药材批量导入完成: 总 {Total} 行, 成功 {Success}, 失败 {Failure}, 跳过 {Skipped}, 批 {Batches}, 中止 {Aborted}",
                    report.TotalCount, report.SuccessCount, report.FailureCount, report.SkippedCount, report.BatchCount, report.IsAborted);

                if (report.HasFailures)
                {
                    await MasterDetailServices.Dialog.ShowWarningAsync(report.Summary, "导入报告");
                }
                else
                {
                    await MasterDetailServices.Dialog.ShowSuccessAsync(report.Summary, "导入报告");
                }

                _cacheManager.InvalidateHerbCaches();
                await RefreshAsync();
            }
            catch (InvalidDataException ex)
            {
                Logger.LogError(ex, "解析药材导入文件失败: {File}", filePath);
                await MasterDetailServices.Dialog.ShowErrorAsync($"文件格式错误：{ex.Message}", "导入失败");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量导入药材失败: {File}", filePath);
                await MasterDetailServices.Dialog.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入药材", ex), "操作失败");
            }
        }

        /// <summary>
        /// 按 ≤1000 行分批提交并汇总为一份报告（AC③ 进度、AC⑤ 报告）。
        /// 服务端失败行号是**批内相对行号**（首数据行 = 2），此处加批次偏移还原为文件行号（AC②）。
        /// </summary>
        private async Task<ImportReport> ImportHerbsInBatchesAsync(IReadOnlyList<HerbInputDto> rows)
        {
            var report = new ImportReport();
            var totalBatches = ImportBatchRunner.CountBatches(rows.Count);
            var completedBatches = 0;

            ImportProgress.Reset(rows.Count);
            MasterDetailServices.Loading.BeginLoading(ImportProgress.Message);
            try
            {
                await ImportBatchRunner.RunAsync(
                    rows,
                    async (batch, offset) =>
                    {
                        var result = await _herbService.BatchImportAsync(new HerbBatchImportInputDto
                        {
                            Herbs = batch,
                            Strategy = ImportDuplicateStrategy
                        });

                        if (!result.Success || result.Data == null)
                        {
                            // 整批失败（服务端已回滚该批——AC④）→ 记入报告并停止后续批次
                            report.Abort(batch.Count, offset + 2, result.Error ?? "批量导入失败");
                            return false;
                        }

                        var data = result.Data;
                        report.AddBatch(batch.Count, data.SuccessCount, data.FailureCount, data.SkippedCount);
                        foreach (var failure in data.Failures)
                        {
                            var details = failure.ErrorDetails.Count > 0
                                ? $"（{string.Join("；", failure.ErrorDetails)}）"
                                : string.Empty;
                            report.AddFailure(offset + failure.RowNumber, failure.HerbName, $"{failure.Reason}{details}");
                        }

                        return true;
                    },
                    (processed, _) =>
                    {
                        completedBatches++;
                        ImportProgress.Report(processed, $"第 {completedBatches}/{totalBatches} 批");
                        MasterDetailServices.Loading.BusyMessage = ImportProgress.Message;
                    });
            }
            finally
            {
                MasterDetailServices.Loading.EndLoading();
            }

            return report;
        }

        /// <summary>导出药材数据（US-HERB-007/013 / US-SHELL-021 AC①）：服务端 JSON → 本地 Excel（.xlsx）。</summary>
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
                    Filter = "Excel 文件|*.xlsx",
                    FileName = $"药材导出_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                    Title = "保存导出文件"
                };
                if (dialog.ShowDialog() != true) return;

                // AC①：列定义以服务端 export 为权威，仅表现层渲染为 .xlsx（后端契约不变）
                var workbook = _herbExcelService.GenerateExportFile(result.Data);
                await File.WriteAllBytesAsync(dialog.FileName, workbook);
                await MasterDetailServices.Dialog.ShowSuccessAsync($"药材数据已导出到：{dialog.FileName}", "导出成功");
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
