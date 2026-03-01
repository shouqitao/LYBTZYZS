using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Herbs.ViewModels.Handlers;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 药材Master-Detail视图模型（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 使用IMasterDetailServices实现组合模式
    /// </summary>
    public partial class HerbMasterDetailViewModel : MasterDetailViewModelBase<HerbListDto, HerbDetailModel>
    {
        private readonly IHerbRepository _herbRepository;
        private readonly IHerbStatusHandler _statusHandler;
        private readonly IHerbImportExportHandler _importExportHandler;
        private readonly IDesktopCacheManager _cacheManager;

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
            IHerbRepository herbRepository,
            IHerbStatusHandler statusHandler,
            IHerbImportExportHandler importExportHandler,
            IDesktopCacheManager cacheManager)
            : base(viewModelServices, masterDetailServices)
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _statusHandler = statusHandler ?? throw new ArgumentNullException(nameof(statusHandler));
            _importExportHandler = importExportHandler ?? throw new ArgumentNullException(nameof(importExportHandler));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));

            PageTitle = "药材管理";
            StatusOptions = new ObservableCollection<CommonStatus>(Enum.GetValues<CommonStatus>());
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
                    var pagedData = await _herbRepository.GetPagedAsync(CurrentPage, PageSize, SearchText);
                    MasterDetailServices.Pagination.TotalCount = pagedData.TotalCount;

                    Items.Clear();
                    foreach (var item in pagedData.Items ?? Enumerable.Empty<HerbListDto>())
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
            // OpenSpec: simplify-desktop-data-layer - 使用Repository替代Service
            var result = await _herbRepository.GetByIdWithResultAsync(item.Id);
            if (!result.success || result.data == null)
            {
                MasterDetailServices.ErrorHandler.SetError("LoadDetail", result.error ?? "加载药材详情失败");
                return;
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

            MasterDetailServices.DetailEditor.LoadDetail(detail);
            OnPropertyChanged(nameof(IsNameEditable));
        }

        /// <summary>创建新详情实例</summary>
        protected override HerbDetailModel CreateNewDetail()
        {
            var detail = HerbDetailModel.CreateNew();
            OnPropertyChanged(nameof(IsNameEditable));
            return detail;
        }

        /// <summary>保存详情</summary>
        protected override async Task<bool> SaveDetailAsync(HerbDetailModel detail)
        {
            if (string.IsNullOrWhiteSpace(detail.Name))
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("药材名称不能为空", "验证失败");
                return false;
            }

            if (string.IsNullOrWhiteSpace(detail.Unit))
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("单位不能为空", "验证失败");
                return false;
            }

            if (detail.Price <= 0)
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("售价必须大于0", "验证失败");
                return false;
            }

            if (detail.CostPrice.HasValue && detail.CostPrice <= 0)
            {
                await MasterDetailServices.Dialog.ShowErrorAsync("成本价必须大于0", "验证失败");
                return false;
            }

            var input = new HerbInputDto
            {
                Id = detail.Id,
                Name = detail.Name.Trim(),
                PinYinCode = detail.PinYinCode?.Trim(),
                Origin = detail.Origin?.Trim(),
                Spec = detail.Spec?.Trim(),
                Unit = detail.Unit.Trim(),
                Price = detail.Price,
                CostPrice = detail.CostPrice,
                Effect = detail.Effect?.Trim(),
                Usage = detail.Usage?.Trim(),
                Remark = detail.Remark?.Trim()
            };

            // OpenSpec: simplify-desktop-data-layer - 使用Repository替代Service
            var result = IsNew
                ? await _herbRepository.CreateWithResultAsync(input)
                : await _herbRepository.UpdateWithResultAsync(detail.Id, input);

            if (!result.success)
            {
                MasterDetailServices.ErrorHandler.SetError("Save", result.error ?? "保存药材失败");
                return false;
            }

            if (result.data != null)
            {
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

            }

            _cacheManager.InvalidateHerbCaches();
            return true;
        }

        /// <summary>删除项</summary>
        protected override async Task<bool> DeleteItemAsync(HerbListDto item)
        {
            // OpenSpec: simplify-desktop-data-layer - 使用Repository替代Service
            var result = await _herbRepository.DeleteWithResultAsync(item.Id);
            if (!result.success)
            {
                MasterDetailServices.ErrorHandler.SetError("Delete", result.error ?? "删除药材失败");
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
