using System.Collections.ObjectModel;
using System.IO;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Contracts;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 药材Master-Detail视图模型
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 合并HerbManagementViewModel和HerbDetailViewModel的功能
    /// 左侧40%显示药材列表，右侧60%显示详情/编辑
    /// </summary>
    /// OpenSpec: optimize-entity-data-flow - 使用HerbListDto优化列表加载
    public class HerbMasterDetailViewModel : MasterDetailViewModelBase<HerbListDto, HerbDetailModel>
    {
        private readonly IHerbService _herbService;
        private readonly IHerbRepository _herbRepository;
        private readonly ICommonDialogService _dialogService;
        private readonly IDialogService _prismDialogService;

        #region 编辑属性

        private string _editName = string.Empty;
        private string _editPinYinCode = string.Empty;
        private string? _editOrigin;
        private string? _editSpec;
        private string _editUnit = "克";
        private decimal _editPrice;
        private decimal _editCostPrice;
        private string? _editEffect;
        private string? _editUsage;
        private string? _editRemark;
        private CommonStatus _editStatus = CommonStatus.Enabled;

        /// <summary>编辑-药材名称</summary>
        public string EditName
        {
            get => _editName;
            set
            {
                if (SetProperty(ref _editName, value))
                {
                    EditPinYinCode = PinYinHelper.GetPinYinCode(value);
                    MarkAsModified();
                }
            }
        }

        /// <summary>编辑-拼音码（自动生成，可手动修正多音字错误）</summary>
        public string EditPinYinCode
        {
            get => _editPinYinCode;
            set { if (SetProperty(ref _editPinYinCode, value)) MarkAsModified(); }
        }

        /// <summary>编辑-产地</summary>
        public string? EditOrigin
        {
            get => _editOrigin;
            set { if (SetProperty(ref _editOrigin, value)) MarkAsModified(); }
        }

        /// <summary>编辑-规格</summary>
        public string? EditSpec
        {
            get => _editSpec;
            set { if (SetProperty(ref _editSpec, value)) MarkAsModified(); }
        }

        /// <summary>编辑-单位</summary>
        public string EditUnit
        {
            get => _editUnit;
            set { if (SetProperty(ref _editUnit, value)) MarkAsModified(); }
        }

        /// <summary>编辑-零售价</summary>
        public decimal EditPrice
        {
            get => _editPrice;
            set { if (SetProperty(ref _editPrice, value)) MarkAsModified(); }
        }

        /// <summary>编辑-成本价</summary>
        public decimal EditCostPrice
        {
            get => _editCostPrice;
            set { if (SetProperty(ref _editCostPrice, value)) MarkAsModified(); }
        }

        /// <summary>编辑-功效</summary>
        public string? EditEffect
        {
            get => _editEffect;
            set { if (SetProperty(ref _editEffect, value)) MarkAsModified(); }
        }

        /// <summary>编辑-用法用量</summary>
        public string? EditUsage
        {
            get => _editUsage;
            set { if (SetProperty(ref _editUsage, value)) MarkAsModified(); }
        }

        /// <summary>编辑-备注</summary>
        public string? EditRemark
        {
            get => _editRemark;
            set { if (SetProperty(ref _editRemark, value)) MarkAsModified(); }
        }

        /// <summary>编辑-状态</summary>
        public CommonStatus EditStatus
        {
            get => _editStatus;
            set { if (SetProperty(ref _editStatus, value)) MarkAsModified(); }
        }

        /// <summary>是否允许编辑名称（新建时允许，编辑时不允许）</summary>
        public bool IsNameEditable => CurrentDetail?.IsNew ?? false;

        #endregion

        #region 扩展命令

        /// <summary>切换药材状态命令</summary>
        public DelegateCommand ToggleStatusCommand { get; private set; } = null!;

        /// <summary>复制药材命令</summary>
        public DelegateCommand CopyHerbCommand { get; private set; } = null!;

        /// <summary>恢复软删除命令</summary>
        public DelegateCommand RestoreCommand { get; private set; } = null!;

        /// <summary>导入药材命令</summary>
        public DelegateCommand ImportCommand { get; private set; } = null!;

        /// <summary>导出药材命令</summary>
        public DelegateCommand ExportCommand { get; private set; } = null!;

        /// <summary>下载模板命令</summary>
        public DelegateCommand DownloadTemplateCommand { get; private set; } = null!;

        /// <summary>查看审计日志命令</summary>
        public DelegateCommand ShowAuditLogCommand { get; private set; } = null!;

        /// <summary>按分类搜索命令</summary>
        public DelegateCommand<string> SearchByCategoryCommand { get; private set; } = null!;

        /// <summary>是否为管理员</summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        /// <summary>状态选项</summary>
        public ObservableCollection<CommonStatus> StatusOptions { get; }

        /// <summary>详情标题</summary>
        public string DetailTitle
        {
            get
            {
                if (CurrentDetail == null) return "药材详情";
                if (CurrentDetail.IsNew) return "新建药材";
                return IsEditMode ? $"编辑药材 - {CurrentDetail.Name}" : $"药材详情 - {CurrentDetail.Name}";
            }
        }

        #endregion

        public HerbMasterDetailViewModel(
            IHerbService herbService,
            IHerbRepository herbRepository,
            ICommonDialogService dialogService,
            IDialogService prismDialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, dialogService)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));

            PageTitle = "药材管理";
            StatusOptions = new ObservableCollection<CommonStatus>(Enum.GetValues<CommonStatus>());

            InitializeExtendedCommands();
        }

        private void InitializeExtendedCommands()
        {
            ToggleStatusCommand = new DelegateCommand(
                async () => await ToggleStatusAsync(),
                () => HasSelection && !IsBusy)
                .ObservesProperty(() => HasSelection)
                .ObservesProperty(() => IsBusy);

            CopyHerbCommand = new DelegateCommand(
                ExecuteCopyHerb,
                () => HasSelection && !IsBusy && IsAdmin)
                .ObservesProperty(() => HasSelection)
                .ObservesProperty(() => IsBusy);

            RestoreCommand = new DelegateCommand(
                async () => await RestoreAsync(),
                () => HasSelection && !IsBusy && IsAdmin)
                .ObservesProperty(() => HasSelection)
                .ObservesProperty(() => IsBusy);

            ImportCommand = new DelegateCommand(
                async () => await ImportHerbsAsync(),
                () => !IsBusy && !IsLoading)
                .ObservesProperty(() => IsBusy)
                .ObservesProperty(() => IsLoading);

            ExportCommand = new DelegateCommand(
                async () => await ExportHerbsAsync(),
                () => !IsBusy && !IsLoading && Items.Count > 0)
                .ObservesProperty(() => IsBusy)
                .ObservesProperty(() => IsLoading);

            DownloadTemplateCommand = new DelegateCommand(
                async () => await DownloadTemplateAsync(),
                () => !IsBusy && !IsLoading)
                .ObservesProperty(() => IsBusy)
                .ObservesProperty(() => IsLoading);

            ShowAuditLogCommand = new DelegateCommand(
                ExecuteShowAuditLog,
                () => HasSelection)
                .ObservesProperty(() => HasSelection);

            SearchByCategoryCommand = new DelegateCommand<string>(
                async (category) => await SearchByCategoryAsync(category),
                category => !IsBusy && !string.IsNullOrWhiteSpace(category))
                .ObservesProperty(() => IsBusy);
        }

        #region 基类重写 - 列表操作

        protected override async Task<IEnumerable<HerbListDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            // OpenSpec: optimize-entity-data-flow - 使用轻量级ListDto
            Logger.LogInformation("药材搜索: 第{Page}页, 每页{PageSize}条, 关键词: '{SearchText}'", page, pageSize, searchText);
            try
            {
                var pagedData = await _herbRepository.GetPagedAsync(page, pageSize, searchText);
                TotalCount = pagedData.TotalCount;
                return pagedData.Items ?? Enumerable.Empty<HerbListDto>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取药材列表时发生异常");
                await UserNotificationService!.HandleExceptionAsync(ex, $"获取药材列表 - 模块:{nameof(HerbMasterDetailViewModel)}");
                TotalCount = 0;
                return Enumerable.Empty<HerbListDto>();
            }
        }

        protected override async Task OnExecuteBatchDeleteAsync(List<HerbListDto> items)
        {
            if (items == null || items.Count == 0) return;

            var successCount = 0;
            var failureCount = 0;

            foreach (var item in items)
            {
                var result = await _herbService.DeleteAsync(item.Id);
                if (result.success)
                    successCount++;
                else
                    failureCount++;
            }

            var message = $"批量删除完成！成功：{successCount}个，失败：{failureCount}个";
            if (failureCount > 0)
                await ShowWarningMessageAsync(message);
            else
                await ShowSuccessMessageAsync(message);

            if (successCount > 0)
                await RefreshAsync();
        }

        #endregion

        #region 基类重写 - 详情操作

        protected override async Task<HerbDetailModel?> LoadDetailAsync(HerbListDto item)
        {
            var result = await _herbService.GetByIdAsync(item.Id);
            if (!result.success || result.data == null)
            {
                ErrorMessage = result.error;
                return null;
            }

            var herb = result.data;
            var detail = new HerbDetailModel
            {
                Id = herb.Id,
                Name = herb.Name,
                PinYinCode = herb.PinYinCode ?? PinYinHelper.GetPinYinCode(herb.Name),
                Origin = herb.Origin,
                Spec = herb.Spec,
                Unit = herb.Unit,
                Price = herb.Price,
                CostPrice = herb.CostPrice,
                Effect = herb.Effect,
                Usage = herb.Usage,
                Remark = herb.Remark,
                Status = herb.Status
            };

            RaisePropertyChanged(nameof(DetailTitle));
            return detail;
        }

        protected override HerbDetailModel CreateNewDetail()
        {
            var detail = HerbDetailModel.CreateNew();

            // 清空编辑属性
            ClearEditProperties();
            RaisePropertyChanged(nameof(IsNameEditable));
            RaisePropertyChanged(nameof(DetailTitle));

            return detail;
        }

        private void ClearEditProperties()
        {
            EditName = string.Empty;
            EditPinYinCode = string.Empty;
            EditOrigin = null;
            EditSpec = null;
            EditUnit = "克";
            EditPrice = 0;
            EditCostPrice = 0;
            EditEffect = null;
            EditUsage = null;
            EditRemark = null;
            EditStatus = CommonStatus.Enabled;
        }

        protected override HerbDetailModel CloneDetail(HerbDetailModel detail)
        {
            // 复制到编辑属性
            EditName = detail.Name;
            EditPinYinCode = detail.PinYinCode;
            EditOrigin = detail.Origin;
            EditSpec = detail.Spec;
            EditUnit = detail.Unit;
            EditPrice = detail.Price;
            EditCostPrice = detail.CostPrice ?? 0;
            EditEffect = detail.Effect;
            EditUsage = detail.Usage;
            EditRemark = detail.Remark;
            EditStatus = detail.Status;

            RaisePropertyChanged(nameof(IsNameEditable));

            return detail.Clone();
        }

        protected override object? GetDetailId(HerbDetailModel detail)
        {
            return detail.Id;
        }

        protected override async Task<bool> SaveDetailAsync(HerbDetailModel detail)
        {
            if (string.IsNullOrWhiteSpace(EditName))
            {
                await ShowErrorMessageAsync("药材名称不能为空");
                return false;
            }

            if (string.IsNullOrWhiteSpace(EditUnit))
            {
                await ShowErrorMessageAsync("单位不能为空");
                return false;
            }

            // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除，由服务端管理
            var input = new HerbInputDto
            {
                Id = detail.Id,
                Name = EditName.Trim(),
                PinYinCode = EditPinYinCode?.Trim(),
                Origin = EditOrigin?.Trim(),
                Spec = EditSpec?.Trim(),
                Unit = EditUnit.Trim(),
                Price = EditPrice,
                CostPrice = EditCostPrice > 0 ? EditCostPrice : null,
                Effect = EditEffect?.Trim(),
                Usage = EditUsage?.Trim(),
                Remark = EditRemark?.Trim()
            };

            var result = detail.IsNew
                ? await _herbService.CreateAsync(input)
                : await _herbService.UpdateAsync(detail.Id, input);

            if (!result.success)
            {
                ErrorMessage = result.error;
                return false;
            }

            if (result.data != null)
            {
                // 更新详情模型
                detail.Id = result.data.Id;
                detail.Name = result.data.Name;
                detail.PinYinCode = result.data.PinYinCode ?? detail.PinYinCode ?? string.Empty;
                detail.Origin = result.data.Origin;
                detail.Spec = result.data.Spec;
                detail.Unit = result.data.Unit;
                detail.Price = result.data.Price;
                detail.CostPrice = result.data.CostPrice;
                detail.Effect = result.data.Effect;
                detail.Usage = result.data.Usage;
                detail.Remark = result.data.Remark;
                detail.Status = result.data.Status;

                RaisePropertyChanged(nameof(DetailTitle));
            }

            return true;
        }

        protected override async Task<bool> DeleteDetailAsync(HerbDetailModel detail)
        {
            var result = await _herbService.DeleteAsync(detail.Id);
            if (!result.success)
                ErrorMessage = result.error;
            return result.success;
        }

        #endregion

        #region 扩展功能

        private async Task ToggleStatusAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                var herb = SelectedItem;
                var newStatus = herb.Status == CommonStatus.Enabled ? "禁用" : "启用";
                var confirmed = await ShowConfirmationAsync($"确认{newStatus}药材 [{herb.Name}] 吗？", "状态切换确认");
                if (!confirmed) return;

                var result = await _herbRepository.ToggleStatusAsync(herb.Id);
                if (result != null)
                {
                    Logger.LogInformation("药材状态已切换: {HerbName} -> {NewStatus}", herb.Name, result.Status);
                    await ShowSuccessMessageAsync($"药材 '{herb.Name}' 已{(result.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
                    await RefreshAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("切换药材状态失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "切换药材状态失败");
                await ShowErrorMessageAsync("切换药材状态失败");
            }
        }

        private void ExecuteCopyHerb()
        {
            if (CurrentDetail == null) return;

            // 创建复制的详情
            var copy = CurrentDetail.Clone();
            copy.Id = Guid.Empty;
            copy.Name = $"{copy.Name}_副本";
            copy.PinYinCode = PinYinHelper.GetPinYinCode(copy.Name);
            copy.Status = CommonStatus.Enabled;

            // 设置为新建模式
            CurrentDetail = copy;
            IsEditMode = true;
            RaisePropertyChanged(nameof(DetailTitle));

            Logger.LogInformation("复制药材: {SourceName} -> {CopyName}", SelectedItem?.Name, copy.Name);
        }

        private async Task RestoreAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                var herb = SelectedItem;
                var confirmed = await ShowConfirmationAsync($"确认恢复药材 [{herb.Name}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _herbRepository.RestoreAsync(herb.Id);
                if (result != null)
                {
                    Logger.LogInformation("药材已恢复: {HerbName}", herb.Name);
                    await ShowSuccessMessageAsync($"药材 '{herb.Name}' 已恢复");
                    await RefreshAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("恢复药材失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "恢复药材失败");
                await ShowErrorMessageAsync("恢复药材失败");
            }
        }

        private async Task ImportHerbsAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _dialogService.ShowOpenFileDialogAsync(filter: "Excel文件|*.xlsx", title: "选择药材导入文件");
                if (string.IsNullOrEmpty(filePath)) return;

                using var fileStream = File.OpenRead(filePath);
                Logger.LogInformation("开始导入药材文件：{FileName}", Path.GetFileName(filePath));
                var result = await _herbRepository.BatchImportAsync(fileStream, Path.GetFileName(filePath));

                if (result == null)
                {
                    await _dialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入药材");
                    return;
                }

                var message = $"导入完成！\n\n成功：{result.SuccessCount}条\n失败：{result.FailureCount}条\n跳过：{result.SkippedCount}条\n\n成功率：{result.SuccessRate:F1}%";
                if (result.FailureCount > 0)
                {
                    message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：\n";
                    foreach (var failure in result.Failures.Take(3))
                        message += $"\n第{failure.RowNumber}行（{failure.HerbName}）：{failure.Reason}";
                }

                await _dialogService.ShowInfoAsync(message, "导入结果");
                if (result.SuccessCount > 0) await RefreshAsync();
            }, "导入药材");
        }

        private async Task ExportHerbsAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _dialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "导出药材数据",
                    defaultFileName: $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                if (string.IsNullOrEmpty(filePath)) return;

                Logger.LogInformation("导出药材数据，关键词：{Keyword}", SearchText);
                var bytes = await _herbRepository.ExportHerbsAsync(SearchText);

                if (bytes == null || bytes.Length == 0)
                {
                    await _dialogService.ShowErrorAsync("导出失败，请稍后重试", "导出药材");
                    return;
                }

                await File.WriteAllBytesAsync(filePath, bytes);
                await _dialogService.ShowInfoAsync($"成功导出药材数据到：\n{filePath}", "导出成功");
            }, "导出药材");
        }

        private async Task DownloadTemplateAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _dialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "保存药材导入模板",
                    defaultFileName: $"药材导入模板_{DateTime.Now:yyyyMMdd}.xlsx");

                if (string.IsNullOrEmpty(filePath)) return;

                Logger.LogInformation("下载药材导入模板");
                var bytes = await _herbRepository.ExportTemplateAsync();

                if (bytes == null || bytes.Length == 0)
                {
                    await _dialogService.ShowErrorAsync("下载模板失败，请稍后重试", "下载模板");
                    return;
                }

                await File.WriteAllBytesAsync(filePath, bytes);
                await _dialogService.ShowInfoAsync($"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入药材」功能导入。", "下载成功");
            }, "下载模板");
        }

        private void ExecuteShowAuditLog()
        {
            if (SelectedItem == null) return;

            Logger.LogInformation("查看药材审计日志：{HerbId}", SelectedItem.Id);
            _prismDialogService.ShowDialog("EntityAuditLogDialog",
                new DialogParameters
                {
                    { "EntityType", "herb" },
                    { "EntityId", SelectedItem.Id },
                    { "EntityDescription", $"药材：{SelectedItem.Name}" }
                },
                _ => { });
        }

        /// <summary>
        /// 按分类搜索药材
        /// </summary>
        private async Task SearchByCategoryAsync(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;

            Logger.LogInformation("按分类搜索药材: {Category}", category);
            SearchText = $"分类:{category}";
            await RefreshAsync();
        }

        #endregion
    }
}
