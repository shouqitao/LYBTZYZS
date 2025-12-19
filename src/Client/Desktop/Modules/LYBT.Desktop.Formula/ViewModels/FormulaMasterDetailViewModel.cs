using System.Collections.ObjectModel;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Models.Items.Formulas;
using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;
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
    /// <summary>
    /// 验方Master-Detail视图模型
    /// OpenSpec: refactor-master-detail-layout
    /// OpenSpec: optimize-entity-data-flow - 使用FormulaListDto优化列表加载
    ///
    /// 合并FormulaManagementViewModel和FormulaDetailViewModel功能
    /// </summary>
    public class FormulaMasterDetailViewModel : MasterDetailViewModelBase<FormulaListDto, FormulaDetailModel>
    {
        private readonly IFormulaRepository _formulaRepository;
        private readonly IFormulaCommandHandler _commandHandler;
        private readonly IDialogService _prismDialogService;
        private readonly IHerbDataManager _herbDataManager;

        // 编辑模式下的药材列表
        private ObservableCollection<FormulaHerbItemViewModel> _editHerbItems = new();

        // 所有药材列表（用于拼音码快速匹配）
        private readonly ObservableCollection<HerbDetailDto> _allHerbs = new();

        public FormulaMasterDetailViewModel(
            IFormulaRepository formulaRepository,
            IFormulaCommandHandler commandHandler,
            IDialogService prismDialogService,
            IHerbDataManager herbDataManager,
            ICommonDialogService commonDialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, commonDialogService)
        {
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));
            _herbDataManager = herbDataManager ?? throw new ArgumentNullException(nameof(herbDataManager));

            PageTitle = "验方管理";

            // 初始化扩展命令
            ToggleStatusCommand = new DelegateCommand<FormulaListDto>(async f => await ToggleStatusAsync(f), f => f != null && !IsBusy);
            RestoreCommand = new DelegateCommand<FormulaListDto>(async f => await RestoreAsync(f), f => f != null && !IsBusy && IsAdmin);
            CopyFormulaCommand = new DelegateCommand(async () => await CopyFormulaAsync(), () => HasSelection && !IsBusy);
            ShowAuditLogCommand = new DelegateCommand(ExecuteShowAuditLog, () => HasSelection);

            // 药材操作命令
            AddHerbCommand = new DelegateCommand(ExecuteAddHerb, () => IsEditMode);
            DeleteHerbCommand = new DelegateCommand<FormulaHerbItemViewModel>(ExecuteDeleteHerb, h => h != null && IsEditMode);

            // 筛选命令
            ClearFiltersCommand = new DelegateCommand(
                async () => await ClearFiltersAsync(),
                () => !IsBusy && !string.IsNullOrWhiteSpace(SearchText));
            SearchByCategoryCommand = new DelegateCommand<string>(
                async (category) => await SearchByCategoryAsync(category),
                category => !IsBusy && !string.IsNullOrWhiteSpace(category));

            // 监听属性变化
            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(CurrentDetail):
                        RaisePropertyChanged(nameof(ViewFormulaDto));
                        break;
                    case nameof(IsEditMode):
                        if (IsEditMode)
                            PopulateEditHerbItems();
                        else
                            RaisePropertyChanged(nameof(ViewFormulaDto));
                        break;
                }
            };
        }

        #region 属性

        /// <summary>是否为管理员</summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        /// <summary>编辑模式下的药材列表</summary>
        public ObservableCollection<FormulaHerbItemViewModel> EditHerbItems
        {
            get => _editHerbItems;
            set => SetProperty(ref _editHerbItems, value);
        }

        /// <summary>药材数量</summary>
        public int HerbCount => EditHerbItems?.Count(h => h.HerbId != Guid.Empty) ?? 0;

        /// <summary>
        /// 查看模式下的FormulaDto（供FormulaViewControl使用）
        /// </summary>
        public FormulaDetailDto? ViewFormulaDto => CurrentDetail?.ToDto();

        /// <summary>
        /// 编辑模式下的详情模型（供FormulaEditControl绑定）
        /// </summary>
        public FormulaDetailModel? EditDetail => CurrentDetail;

        /// <summary>
        /// 详情标题
        /// </summary>
        public string DetailTitle => CurrentDetail == null ? "验方详情" :
            CurrentDetail.IsNew ? "新建验方" :
            IsEditMode ? $"编辑验方 - {CurrentDetail.Name}" :
            $"验方详情 - {CurrentDetail.Name}";

        #endregion

        #region 扩展命令

        public DelegateCommand<FormulaListDto> ToggleStatusCommand { get; }
        public DelegateCommand<FormulaListDto> RestoreCommand { get; }
        public DelegateCommand CopyFormulaCommand { get; }
        public DelegateCommand ShowAuditLogCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand<FormulaHerbItemViewModel> DeleteHerbCommand { get; }

        /// <summary>清除筛选命令</summary>
        public DelegateCommand ClearFiltersCommand { get; }

        /// <summary>按分类搜索命令</summary>
        public DelegateCommand<string> SearchByCategoryCommand { get; }

        #endregion

        #region 基类抽象方法实现

        protected override async Task<IEnumerable<FormulaListDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            // OpenSpec: optimize-entity-data-flow - 使用轻量级ListDto
            var result = await _formulaRepository.GetPagedListAsync(page, pageSize, searchText);

            TotalCount = result.TotalCount;
            CurrentPage = result.CurrentPage;
            PageSize = result.PageSize;

            return result.Items ?? Enumerable.Empty<FormulaListDto>();
        }

        protected override async Task<FormulaDetailModel?> LoadDetailAsync(FormulaListDto item)
        {
            try
            {
                // OpenSpec: optimize-entity-data-flow - 从ListDto加载完整详情
                var dto = await _formulaRepository.GetByIdAsync(item.Id);
                if (dto == null)
                {
                    await ShowErrorMessageAsync($"验方 '{item.Name}' 不存在或已被删除");
                    return null;
                }

                return FormulaDetailModel.FromDto(dto);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载验方详情失败: {FormulaId}", item.Id);
                await ShowErrorMessageAsync("加载验方详情失败");
                return null;
            }
        }

        protected override FormulaDetailModel CreateNewDetail()
        {
            return FormulaDetailModel.CreateNew();
        }

        protected override FormulaDetailModel CloneDetail(FormulaDetailModel detail)
        {
            return detail.Clone();
        }

        protected override object? GetDetailId(FormulaDetailModel detail)
        {
            return detail.Id;
        }

        protected override async Task<bool> SaveDetailAsync(FormulaDetailModel detail)
        {
            try
            {
                // 从编辑控件收集药材数据
                detail.Herbs.Clear();
                foreach (var herb in EditHerbItems.Where(h => h.HerbId != Guid.Empty || !string.IsNullOrWhiteSpace(h.HerbName)))
                {
                    detail.Herbs.Add(new FormulaHerbItemDto
                    {
                        HerbId = herb.HerbId == Guid.Empty ? null : herb.HerbId,
                        HerbName = herb.HerbName,
                        Dosage = herb.Dosage,
                        Unit = herb.Unit,
                        ProcessingMethod = herb.Remark,
                        DecocteMethod = herb.DecocteMethod
                    });
                }

                // OpenSpec: refactor-dto-simplification - Status字段已从FormulaInputDto移除
                var dto = detail.ToDto();
                var inputDto = new FormulaInputDto
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Effect = dto.Effect ?? string.Empty,
                    Usage = dto.Usage ?? string.Empty,
                    Property = dto.Property,
                    Remark = dto.Remark,
                    IsShared = dto.IsShared,
                    Category = dto.Category,
                    Herbs = detail.Herbs.Select(h => new FormulaHerbItemInputDto
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Dosage = h.Dosage,
                        Unit = h.Unit,
                        ProcessingMethod = h.ProcessingMethod,
                        DecocteMethod = h.DecocteMethod
                    }).ToList()
                };

                if (detail.IsNew)
                {
                    var result = await _formulaRepository.CreateAsync(inputDto);
                    detail.Id = result.Id;
                    detail.CreatedAt = result.CreatedAt;
                    Logger.LogInformation("验方创建成功: {FormulaId} - {FormulaName}", result.Id, result.Name);
                }
                else
                {
                    await _formulaRepository.UpdateAsync(inputDto);
                    detail.UpdatedAt = DateTime.Now;
                    Logger.LogInformation("验方更新成功: {FormulaId} - {FormulaName}", detail.Id, detail.Name);
                }

                await ShowSuccessMessageAsync($"验方 '{detail.Name}' 保存成功");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存验方失败: {FormulaName}", detail.Name);
                await ShowErrorMessageAsync($"保存验方失败: {ex.Message}");
                return false;
            }
        }

        protected override async Task<bool> DeleteDetailAsync(FormulaDetailModel detail)
        {
            try
            {
                var confirmed = await ShowConfirmationAsync($"确定要删除验方 '{detail.Name}' 吗？", "删除确认");
                if (!confirmed) return false;

                var success = await _commandHandler.DeleteAsync(detail.Id);
                if (success)
                {
                    Logger.LogInformation("验方删除成功: {FormulaId} - {FormulaName}", detail.Id, detail.Name);
                    await ShowSuccessMessageAsync($"验方 '{detail.Name}' 删除成功");
                    return true;
                }

                await ShowErrorMessageAsync($"删除验方 '{detail.Name}' 失败");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除验方失败: {FormulaId}", detail.Id);
                await ShowErrorMessageAsync("删除验方时发生系统错误");
                return false;
            }
        }

        protected override async Task OnExecuteBatchDeleteAsync(List<FormulaListDto> items)
        {
            if (items == null || items.Count == 0) return;

            var successCount = 0;
            var failureCount = 0;
            var failedItems = new List<string>();

            foreach (var item in items)
            {
                try
                {
                    if (await _commandHandler.DeleteAsync(item.Id))
                        successCount++;
                    else
                    {
                        failureCount++;
                        failedItems.Add(item.Name);
                    }
                }
                catch
                {
                    failureCount++;
                    failedItems.Add(item.Name);
                }
            }

            var message = $"批量删除完成！\n\n成功：{successCount}个\n失败：{failureCount}个";
            if (failureCount > 0 && failedItems.Count > 0)
            {
                message += $"\n\n失败的验方：\n{string.Join("、", failedItems.Take(5))}";
                if (failedItems.Count > 5) message += $"等{failedItems.Count}个";
            }

            if (failureCount > 0)
                await ShowWarningMessageAsync(message);
            else
                await ShowSuccessMessageAsync(message);

            if (successCount > 0)
                await LoadPageAsync();
        }

        #endregion

        #region 编辑模式辅助

        /// <summary>
        /// 填充编辑药材列表（进入编辑模式时调用）
        /// </summary>
        private void PopulateEditHerbItems()
        {
            // 从当前详情填充编辑药材列表
            EditHerbItems.Clear();
            if (CurrentDetail != null)
            {
                foreach (var herb in CurrentDetail.Herbs)
                {
                    EditHerbItems.Add(new FormulaHerbItemViewModel
                    {
                        HerbId = herb.HerbId ?? Guid.Empty,
                        HerbName = herb.HerbName ?? string.Empty,
                        Dosage = herb.Dosage,
                        Unit = herb.Unit ?? "g",
                        Remark = herb.ProcessingMethod,
                        DecocteMethod = herb.DecocteMethod,
                        AllHerbs = _allHerbs  // 注入药材列表用于快速匹配
                    });
                }
            }

            // 确保至少有一个空行用于输入
            if (EditHerbItems.Count == 0)
            {
                EditHerbItems.Add(new FormulaHerbItemViewModel { Unit = "g", AllHerbs = _allHerbs });
            }

            RaisePropertyChanged(nameof(HerbCount));
        }

        #endregion

        #region 扩展命令实现

        private async Task ToggleStatusAsync(FormulaListDto? formula)
        {
            if (formula == null) return;

            try
            {
                var newStatus = formula.Status == CommonStatus.Enabled ? "禁用" : "启用";
                var confirmed = await ShowConfirmationAsync($"确认{newStatus}验方 [{formula.Name}] 吗？", "状态切换确认");
                if (!confirmed) return;

                var result = await _formulaRepository.ToggleStatusAsync(formula.Id);
                if (result != null)
                {
                    Logger.LogInformation("验方状态已切换: {FormulaName} -> {NewStatus}", formula.Name, result.Status);
                    await ShowSuccessMessageAsync($"验方 '{formula.Name}' 已{(result.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
                    await LoadPageAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("切换验方状态失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "切换验方状态失败: {FormulaId}", formula.Id);
                await ShowErrorMessageAsync("切换验方状态失败");
            }
        }

        private async Task RestoreAsync(FormulaListDto? formula)
        {
            if (formula == null) return;

            try
            {
                var confirmed = await ShowConfirmationAsync($"确认恢复验方 [{formula.Name}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _formulaRepository.RestoreAsync(formula.Id);
                if (result != null)
                {
                    Logger.LogInformation("验方已恢复: {FormulaName}", formula.Name);
                    await ShowSuccessMessageAsync($"验方 '{formula.Name}' 已恢复");
                    await LoadPageAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("恢复验方失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "恢复验方失败: {FormulaId}", formula.Id);
                await ShowErrorMessageAsync("恢复验方失败");
            }
        }

        private async Task CopyFormulaAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                var confirmed = await ShowConfirmationAsync($"确认复制验方 [{SelectedItem.Name}] 吗？", "复制确认");
                if (!confirmed) return;

                var result = await _formulaRepository.CloneFormulaAsync(SelectedItem.Id);
                if (result != null)
                {
                    Logger.LogInformation("验方复制成功: {SourceName} -> {NewName}", SelectedItem.Name, result.Name);
                    await ShowSuccessMessageAsync($"验方已复制为 '{result.Name}'");
                    await LoadPageAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("复制验方失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "复制验方失败: {FormulaId}", SelectedItem?.Id);
                await ShowErrorMessageAsync("复制验方失败");
            }
        }

        private void ExecuteShowAuditLog()
        {
            if (SelectedItem == null) return;

            _prismDialogService.ShowDialog("EntityAuditLogDialog", new DialogParameters
            {
                { "EntityType", "formula" },
                { "EntityId", SelectedItem.Id },
                { "EntityDescription", $"验方：{SelectedItem.Name}" }
            }, _ => { });
        }

        private void ExecuteAddHerb()
        {
            EditHerbItems.Add(new FormulaHerbItemViewModel { Unit = "g", AllHerbs = _allHerbs });
            RaisePropertyChanged(nameof(HerbCount));
        }

        private void ExecuteDeleteHerb(FormulaHerbItemViewModel? herb)
        {
            if (herb == null) return;

            EditHerbItems.Remove(herb);
            RaisePropertyChanged(nameof(HerbCount));
        }

        /// <summary>
        /// 清除筛选条件
        /// </summary>
        private async Task ClearFiltersAsync()
        {
            Logger.LogInformation("清除验方筛选条件");
            SearchText = string.Empty;
            await RefreshAsync();
        }

        /// <summary>
        /// 按分类搜索验方
        /// </summary>
        private async Task SearchByCategoryAsync(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;

            Logger.LogInformation("按分类搜索验方: {Category}", category);
            SearchText = $"分类:{category}";
            await RefreshAsync();
        }

        #endregion

        #region 命令状态刷新

        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();
            ToggleStatusCommand?.RaiseCanExecuteChanged();
            RestoreCommand?.RaiseCanExecuteChanged();
            CopyFormulaCommand?.RaiseCanExecuteChanged();
            ShowAuditLogCommand?.RaiseCanExecuteChanged();
            AddHerbCommand?.RaiseCanExecuteChanged();
            DeleteHerbCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            SearchByCategoryCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        #region 药材列表加载

        /// <summary>
        /// 导航到页面时加载药材列表
        /// </summary>
        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            await LoadAllHerbsAsync();
        }

        /// <summary>
        /// 加载所有药材列表（用于拼音码快速匹配）
        /// </summary>
        private async Task LoadAllHerbsAsync()
        {
            try
            {
                Logger.LogDebug("开始加载所有药材列表");
                _allHerbs.Clear();

                const int pageSize = 100;
                int currentPage = 1;
                while (true)
                {
                    var pagedResult = await _herbDataManager.GetPagedAsync(currentPage, pageSize);
                    if (pagedResult?.Items == null || !pagedResult.Items.Any()) break;
                    foreach (var herb in pagedResult.Items) _allHerbs.Add(herb);
                    if (pagedResult.Items.Count < pageSize) break;
                    currentPage++;
                }
                Logger.LogInformation("成功分页加载 {Count} 个药材", _allHerbs.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材列表时发生异常");
            }
        }

        #endregion
    }
}
