using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Herbs.ViewModels.Handlers;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 药材Master-Detail视图模型（组合模式）
    /// OpenSpec: frontend-architecture-unification - 子VM模式 + 对象DP
    ///
    /// 使用IMasterDetailServices实现组合模式
    /// HerbEditorViewModel子VM封装编辑逻辑
    /// </summary>
    public partial class HerbMasterDetailViewModel : MasterDetailViewModelBase<HerbListDto, HerbDetailModel>
    {
        private readonly IHerbService _herbService;
        private readonly IHerbStatusHandler _statusHandler;
        private readonly IHerbImportExportHandler _importExportHandler;
        private readonly IDesktopCacheManager _cacheManager;

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
            IHerbImportExportHandler importExportHandler,
            IDesktopCacheManager cacheManager,
            HerbEditorViewModel herbEditor)
            : base(viewModelServices, masterDetailServices)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
            _importExportHandler = importExportHandler ?? throw new ArgumentNullException(nameof(importExportHandler));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
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
            // OpenSpec: standardize-service-layer - 使用Service替代Repository
            var result = await _herbService.GetByIdAsync(item.Id);
            if (!result.Success || result.Data == null)
            {
                MasterDetailServices.ErrorHandler.SetError("LoadDetail", result.Error ?? "加载药材详情失败");
                return;
            }

            var herb = result.Data;
            var detail = new HerbDetailModel
            {
                Id = herb.Id,
                Name = herb.Name,
                PinYinCode = herb.PinYinCode ?? PinYinHelper.GetPinYinCode(herb.Name),
                Category = herb.Category,
                Properties = herb.Properties,
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

            HerbEditor.InitializeFromDto(new HerbDetailDto
            {
                Id = herb.Id,
                Name = herb.Name,
                PinYinCode = herb.PinYinCode ?? PinYinHelper.GetPinYinCode(herb.Name),
                Category = herb.Category,
                Properties = herb.Properties,
                Origin = herb.Origin,
                Spec = herb.Spec,
                Unit = herb.Unit,
                Price = herb.Price,
                CostPrice = herb.CostPrice,
                Effect = herb.Effect,
                Usage = herb.Usage,
                Remark = herb.Remark,
                Status = herb.Status
            });
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

            // OpenSpec: standardize-service-layer - 使用Service替代Repository
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
            // OpenSpec: standardize-service-layer - 使用Service替代Repository
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

        /// <summary>恢复软删除</summary>
        [RelayCommand(CanExecute = nameof(CanRestore))]
        private async Task RestoreAsync()
        {
            if (SelectedItem == null) return;
            if (await _statusHandler.RestoreAsync(SelectedItem))
            {
                _cacheManager.InvalidateHerbCaches();
                await RefreshAsync();
            }
        }

        private bool CanRestore() => HasSelection && !IsBusy && IsAdmin;

        /// <summary>导入药材</summary>
        [RelayCommand]
        private async Task ImportHerbsAsync()
        {
            if (await _importExportHandler.ImportAsync())
            {
                _cacheManager.InvalidateHerbCaches();
                await RefreshAsync();
            }
        }

        /// <summary>导出药材</summary>
        [RelayCommand]
        private async Task ExportHerbsAsync()
        {
            await _importExportHandler.ExportAsync(SearchText);
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
    }
}
