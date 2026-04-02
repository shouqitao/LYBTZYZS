using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Formula.Mappers;
using LYBT.Desktop.Formula.ViewModels.Handlers;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Prism.Regions;

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
        private readonly IFormulaStatusHandler _statusHandler;
        // OpenSpec: cross-module-decoupling - 使用IHerbSearchProvider替代IHerbRepository
        private readonly IHerbSearchProvider _herbSearchProvider;
        private readonly IDesktopCacheManager _cacheManager;
        private readonly FormulaDetailModelMapper _mapper = new();

        // 编辑模式下的药材列表
        private ObservableCollection<FormulaHerbItemViewModel> _editHerbItems = new();

        // 所有药材列表（用于拼音码快速匹配）
        private readonly ObservableCollection<HerbListDto> _allHerbs = new();

        #region 扩展属性

        /// <inheritdoc/>
        protected override string EntityDisplayName => "验方";

        /// <inheritdoc/>
        protected override string? GetDetailDisplayName() => CurrentDetail?.Name;

        /// <summary>编辑模式下的药材列表</summary>
        public ObservableCollection<FormulaHerbItemViewModel> EditHerbItems
        {
            get => _editHerbItems;
            set => SetProperty(ref _editHerbItems, value);
        }

        /// <summary>药材数量</summary>
        public int HerbCount => EditHerbItems?.Count(h => h.HerbId != Guid.Empty) ?? 0;

        #endregion

        /// <summary>
        /// 构造函数
        /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
        /// </summary>
        public FormulaMasterDetailViewModel(
            IViewModelServices viewModelServices,
            IMasterDetailServices<FormulaListDto, FormulaDetailModel> masterDetailServices,
            IFormulaRepository formulaRepository,
            IFormulaStatusHandler statusHandler,
            IHerbSearchProvider herbSearchProvider,
            IDesktopCacheManager cacheManager)
            : base(viewModelServices, masterDetailServices)
        {
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
            _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
            _herbSearchProvider = herbSearchProvider ?? throw new ArgumentNullException(nameof(herbSearchProvider));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));

            PageTitle = "验方管理";

            // 监听属性变化 - DetailTitle 已由基类自动通知
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsEditMode) && IsEditMode)
                {
                    PopulateEditHerbItems();
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
                await MasterDetailServices.Loading.ExecuteWithLoadingAsync(async () =>
                {
                    var pagedData = await _formulaRepository.GetPagedAsync(CurrentPage, PageSize, SearchText);
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
                var dto = await _formulaRepository.GetByIdAsync(item.Id);
                if (dto == null)
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync($"验方 '{item.Name}' 不存在或已被删除", "加载失败");
                    return;
                }

                var detail = _mapper.ToItem(dto);
                MasterDetailServices.DetailEditor.LoadDetail(detail);
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
            return FormulaDetailModel.CreateNew();
        }

        /// <summary>保存详情</summary>
        protected override async Task<bool> SaveDetailAsync(FormulaDetailModel detail)
        {
            if (string.IsNullOrWhiteSpace(detail.Name))
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("验方名称不能为空", "验证失败");
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

                // OpenSpec: adopt-mapperly-unified-mapping - 使用MappingService替代手动映射
                var inputDto = _mapper.ToInputDto(detail);

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
                    detail.UpdatedAt = DateTime.UtcNow;
                    Logger.LogInformation("验方更新成功: {FormulaId} - {FormulaName}", detail.Id, detail.Name);
                }

                _cacheManager.InvalidateFormulaCaches();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存验方失败: {FormulaName}", detail.Name);
                var errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage(
                    IsNew ? "创建验方" : "更新验方", ex);
                MasterDetailServices.ErrorHandler.SetError("Save", errorMessage);
                return false;
            }
        }

        /// <summary>删除项</summary>
        protected override async Task<bool> DeleteItemAsync(FormulaListDto item)
        {
            // OpenSpec: simplify-desktop-data-layer - 使用Repository替代Service
            var success = await _formulaRepository.DeleteAsync(item.Id);
            if (!success)
            {
                MasterDetailServices.ErrorHandler.SetError("Delete", $"删除验方 '{item.Name}' 失败");
            }
            else
            {
                Logger.LogInformation("验方删除成功: {FormulaId} - {FormulaName}", item.Id, item.Name);
                _cacheManager.InvalidateFormulaCaches();
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

                var result = await _formulaRepository.CloneFormulaAsync(SelectedItem.Id);
                if (result != null)
                {
                    Logger.LogInformation("验方复制成功: {SourceName} -> {NewName}", SelectedItem.Name, result.Name);
                    await MasterDetailServices.Dialog.ShowSuccessAsync($"验方已复制为 '{result.Name}'", "操作成功");
                    _cacheManager.InvalidateFormulaCaches();
                    await RefreshAsync();
                }
                else
                {
                    await MasterDetailServices.Dialog.ShowErrorAsync("复制验方失败", "操作失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "复制验方失败");
                await MasterDetailServices.Dialog.ShowErrorAsync("复制验方失败", "操作失败");
            }
        }

        private bool CanCopyFormula() => HasSelection && !IsBusy;

        /// <summary>恢复软删除</summary>
        [RelayCommand(CanExecute = nameof(CanRestore))]
        private async Task RestoreAsync()
        {
            if (SelectedItem == null) return;
            if (await _statusHandler.RestoreAsync(SelectedItem))
            {
                _cacheManager.InvalidateFormulaCaches();
                await RefreshAsync();
            }
        }

        private bool CanRestore() => HasSelection && !IsBusy && IsAdmin;

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

                // OpenSpec: cross-module-decoupling - 使用IHerbSearchProvider替代IHerbRepository
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
