using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 验方Master-Detail视图模型（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 使用IMasterDetailServices实现组合模式
    /// </summary>
    public partial class FormulaMasterDetailViewModel : MasterDetailViewModelBase<FormulaListDto, FormulaDetailModel>
    {
        private readonly IFormulaRepository _formulaRepository;
        private readonly IFormulaService _formulaService;
        private readonly IDialogService _prismDialogService;
        private readonly IHerbService _herbService;
        private readonly ISessionManager? _sessionManager;

        // 编辑模式下的药材列表
        private ObservableCollection<FormulaHerbItemViewModel> _editHerbItems = new();

        // 所有药材列表（用于拼音码快速匹配）
        private readonly ObservableCollection<HerbListDto> _allHerbs = new();

        #region 扩展属性

        /// <summary>是否为管理员</summary>
        public bool IsAdmin => _sessionManager?.HasPermission(UserRole.Admin) == true;

        /// <summary>编辑模式下的药材列表</summary>
        public ObservableCollection<FormulaHerbItemViewModel> EditHerbItems
        {
            get => _editHerbItems;
            set => SetProperty(ref _editHerbItems, value);
        }

        /// <summary>药材数量</summary>
        public int HerbCount => EditHerbItems?.Count(h => h.HerbId != Guid.Empty) ?? 0;

        /// <summary>查看模式下的FormulaDto</summary>
        public FormulaDetailDto? ViewFormulaDto => CurrentDetail?.ToDto();

        /// <summary>编辑模式下的详情模型</summary>
        public FormulaDetailModel? EditDetail => CurrentDetail;

        /// <summary>详情标题</summary>
        public string DetailTitle
        {
            get
            {
                if (CurrentDetail == null) return "验方详情";
                if (IsNew) return "新建验方";
                return IsEditMode ? $"编辑验方 - {CurrentDetail.Name}" : $"验方详情 - {CurrentDetail.Name}";
            }
        }

        #endregion

        public FormulaMasterDetailViewModel(
            IMasterDetailServices<FormulaListDto, FormulaDetailModel> services,
            IFormulaRepository formulaRepository,
            IFormulaService formulaService,
            IDialogService prismDialogService,
            IHerbService herbService,
            ILoggerFactory loggerFactory,
            ISessionManager? sessionManager = null)
            : base(services, loggerFactory)
        {
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _sessionManager = sessionManager;

            PageTitle = "验方管理";

            // 监听属性变化
            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(CurrentDetail):
                        OnPropertyChanged(nameof(ViewFormulaDto));
                        OnPropertyChanged(nameof(EditDetail));
                        break;
                    case nameof(IsEditMode):
                        if (IsEditMode)
                            PopulateEditHerbItems();
                        else
                            OnPropertyChanged(nameof(ViewFormulaDto));
                        OnPropertyChanged(nameof(DetailTitle));
                        break;
                }
            };
        }

        #region 基类抽象方法实现

        /// <summary>加载列表数据</summary>
        protected override async Task LoadListAsync()
        {
            Logger.LogInformation("验方搜索: 第{Page}页, 每页{PageSize}条, 关键词: '{SearchText}'",
                CurrentPage, PageSize, SearchText);

            try
            {
                await Services.Loading.ExecuteWithLoadingAsync(async () =>
                {
                    var pagedData = await _formulaRepository.GetPagedAsync(CurrentPage, PageSize, SearchText);
                    Services.Pagination.TotalCount = pagedData.TotalCount;

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
                Services.ErrorHandler.HandleException(ex, "获取验方列表");
            }
        }

        /// <summary>加载详情数据</summary>
        protected override async Task LoadDetailAsync(FormulaListDto item)
        {
            try
            {
                var dto = await _formulaRepository.GetByIdAsync(item.Id);
                if (dto == null)
                {
                    await Services.Dialog.ShowErrorAsync($"验方 '{item.Name}' 不存在或已被删除", "加载失败");
                    return;
                }

                var detail = FormulaDetailModel.FromDto(dto);
                Services.DetailEditor.LoadDetail(detail);
                OnPropertyChanged(nameof(DetailTitle));
                OnPropertyChanged(nameof(ViewFormulaDto));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载验方详情失败: {FormulaId}", item.Id);
                Services.ErrorHandler.HandleException(ex, "加载验方详情");
            }
        }

        /// <summary>创建新详情实例</summary>
        protected override FormulaDetailModel CreateNewDetail()
        {
            var detail = FormulaDetailModel.CreateNew();
            OnPropertyChanged(nameof(DetailTitle));
            return detail;
        }

        /// <summary>保存详情</summary>
        protected override async Task<bool> SaveDetailAsync(FormulaDetailModel detail)
        {
            if (string.IsNullOrWhiteSpace(detail.Name))
            {
                await Services.Dialog.ShowErrorAsync("验方名称不能为空", "验证失败");
                return false;
            }

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

                if (IsNew)
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

                OnPropertyChanged(nameof(DetailTitle));
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存验方失败: {FormulaName}", detail.Name);
                var errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage(
                    IsNew ? "创建验方" : "更新验方", ex);
                Services.ErrorHandler.SetError("Save", errorMessage);
                return false;
            }
        }

        /// <summary>删除项</summary>
        protected override async Task<bool> DeleteItemAsync(FormulaListDto item)
        {
            var success = await _formulaService.DeleteAsync(item.Id);
            if (!success)
            {
                Services.ErrorHandler.SetError("Delete", $"删除验方 '{item.Name}' 失败");
            }
            else
            {
                Logger.LogInformation("验方删除成功: {FormulaId} - {FormulaName}", item.Id, item.Name);
            }
            return success;
        }

        #endregion

        #region 编辑模式辅助

        /// <summary>填充编辑药材列表</summary>
        private void PopulateEditHerbItems()
        {
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
                        Unit = herb.Unit ?? string.Empty,
                        Remark = herb.ProcessingMethod,
                        DecocteMethod = herb.DecocteMethod,
                        AllHerbs = _allHerbs
                    });
                }
            }

            // 确保至少有一个空行
            if (EditHerbItems.Count == 0)
            {
                EditHerbItems.Add(new FormulaHerbItemViewModel { Unit = string.Empty, AllHerbs = _allHerbs });
            }

            OnPropertyChanged(nameof(HerbCount));
        }

        #endregion

        #region 扩展命令

        /// <summary>切换验方状态</summary>
        [RelayCommand(CanExecute = nameof(CanToggleStatus))]
        private async Task ToggleStatusAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                var formula = SelectedItem;
                var newStatus = formula.Status == CommonStatus.Enabled ? "禁用" : "启用";
                var confirmed = await Services.Dialog.ShowConfirmAsync($"确认{newStatus}验方 [{formula.Name}] 吗？", "状态切换确认");
                if (!confirmed) return;

                var result = await _formulaRepository.ToggleStatusAsync(formula.Id);
                if (result != null)
                {
                    Logger.LogInformation("验方状态已切换: {FormulaName} -> {NewStatus}", formula.Name, result.Status);
                    await Services.Dialog.ShowSuccessAsync($"验方 '{formula.Name}' 已{(result.Status == CommonStatus.Enabled ? "启用" : "禁用")}", "操作成功");
                    await RefreshAsync();
                }
                else
                {
                    await Services.Dialog.ShowErrorAsync("切换验方状态失败", "操作失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "切换验方状态失败");
                await Services.Dialog.ShowErrorAsync("切换验方状态失败", "操作失败");
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
                var confirmed = await Services.Dialog.ShowConfirmAsync($"确认复制验方 [{SelectedItem.Name}] 吗？", "复制确认");
                if (!confirmed) return;

                var result = await _formulaRepository.CloneFormulaAsync(SelectedItem.Id);
                if (result != null)
                {
                    Logger.LogInformation("验方复制成功: {SourceName} -> {NewName}", SelectedItem.Name, result.Name);
                    await Services.Dialog.ShowSuccessAsync($"验方已复制为 '{result.Name}'", "操作成功");
                    await RefreshAsync();
                }
                else
                {
                    await Services.Dialog.ShowErrorAsync("复制验方失败", "操作失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "复制验方失败");
                await Services.Dialog.ShowErrorAsync("复制验方失败", "操作失败");
            }
        }

        private bool CanCopyFormula() => HasSelection && !IsBusy;

        /// <summary>恢复软删除</summary>
        [RelayCommand(CanExecute = nameof(CanRestore))]
        private async Task RestoreAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                var formula = SelectedItem;
                var confirmed = await Services.Dialog.ShowConfirmAsync($"确认恢复验方 [{formula.Name}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _formulaRepository.RestoreAsync(formula.Id);
                if (result != null)
                {
                    Logger.LogInformation("验方已恢复: {FormulaName}", formula.Name);
                    await Services.Dialog.ShowSuccessAsync($"验方 '{formula.Name}' 已恢复", "操作成功");
                    await RefreshAsync();
                }
                else
                {
                    await Services.Dialog.ShowErrorAsync("恢复验方失败", "操作失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "恢复验方失败");
                await Services.Dialog.ShowErrorAsync("恢复验方失败", "操作失败");
            }
        }

        private bool CanRestore() => HasSelection && !IsBusy && IsAdmin;

        /// <summary>查看审计日志</summary>
        [RelayCommand(CanExecute = nameof(CanShowAuditLog))]
        private void ShowAuditLog()
        {
            if (SelectedItem == null) return;

            Logger.LogInformation("查看验方审计日志：{FormulaId}", SelectedItem.Id);
            _prismDialogService.ShowDialog("EntityAuditLogDialog",
                new DialogParameters
                {
                    { "EntityType", "formula" },
                    { "EntityId", SelectedItem.Id },
                    { "EntityDescription", $"验方：{SelectedItem.Name}" }
                },
                _ => { });
        }

        private bool CanShowAuditLog() => HasSelection;

        /// <summary>添加药材行</summary>
        [RelayCommand(CanExecute = nameof(CanAddHerb))]
        private void AddHerb()
        {
            EditHerbItems.Add(new FormulaHerbItemViewModel { Unit = string.Empty, AllHerbs = _allHerbs });
            OnPropertyChanged(nameof(HerbCount));
        }

        private bool CanAddHerb() => IsEditMode;

        /// <summary>删除药材行</summary>
        [RelayCommand(CanExecute = nameof(CanDeleteHerb))]
        private void DeleteHerb(FormulaHerbItemViewModel? herb)
        {
            if (herb == null) return;
            EditHerbItems.Remove(herb);
            OnPropertyChanged(nameof(HerbCount));
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

        #region 导航

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            await LoadAllHerbsAsync();
        }

        /// <summary>加载所有药材列表</summary>
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
                    var pagedResult = await _herbService.GetPagedAsync(currentPage, pageSize);
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
