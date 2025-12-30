using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

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
        private readonly IHerbService _herbService;
        private readonly IHerbRepository _herbRepository;
        private readonly IDialogService _prismDialogService;
        private readonly ISessionManager? _sessionManager;

        #region 扩展属性

        /// <summary>是否允许编辑名称（新建时允许，编辑时不允许）</summary>
        public bool IsNameEditable => IsNew;

        /// <summary>是否为管理员</summary>
        public bool IsAdmin => _sessionManager?.HasPermission(UserRole.Admin) == true;

        /// <summary>状态选项</summary>
        public ObservableCollection<CommonStatus> StatusOptions { get; }

        /// <summary>详情标题</summary>
        public string DetailTitle
        {
            get
            {
                if (CurrentDetail == null) return "药材详情";
                if (IsNew) return "新建药材";
                return IsEditMode ? $"编辑药材 - {CurrentDetail.Name}" : $"药材详情 - {CurrentDetail.Name}";
            }
        }

        #endregion

        public HerbMasterDetailViewModel(
            IMasterDetailServices<HerbListDto, HerbDetailModel> services,
            IHerbService herbService,
            IHerbRepository herbRepository,
            IDialogService prismDialogService,
            ILoggerFactory loggerFactory,
            ISessionManager? sessionManager = null)
            : base(services, loggerFactory)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));
            _sessionManager = sessionManager;

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
                await Services.Loading.ExecuteWithLoadingAsync(async () =>
                {
                    var pagedData = await _herbRepository.GetPagedAsync(CurrentPage, PageSize, SearchText);
                    Services.Pagination.TotalCount = pagedData.TotalCount;

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
                Services.ErrorHandler.HandleException(ex, "获取药材列表");
            }
        }

        /// <summary>加载详情数据</summary>
        protected override async Task LoadDetailAsync(HerbListDto item)
        {
            var result = await _herbService.GetByIdAsync(item.Id);
            if (!result.success || result.data == null)
            {
                Services.ErrorHandler.SetError("LoadDetail", result.error ?? "加载药材详情失败");
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

            Services.DetailEditor.LoadDetail(detail);
            OnPropertyChanged(nameof(DetailTitle));
            OnPropertyChanged(nameof(IsNameEditable));
        }

        /// <summary>创建新详情实例</summary>
        protected override HerbDetailModel CreateNewDetail()
        {
            var detail = HerbDetailModel.CreateNew();
            OnPropertyChanged(nameof(IsNameEditable));
            OnPropertyChanged(nameof(DetailTitle));
            return detail;
        }

        /// <summary>保存详情</summary>
        protected override async Task<bool> SaveDetailAsync(HerbDetailModel detail)
        {
            if (string.IsNullOrWhiteSpace(detail.Name))
            {
                await Services.Dialog.ShowErrorAsync("药材名称不能为空", "验证失败");
                return false;
            }

            if (string.IsNullOrWhiteSpace(detail.Unit))
            {
                await Services.Dialog.ShowErrorAsync("单位不能为空", "验证失败");
                return false;
            }

            if (detail.Price <= 0)
            {
                await Services.Dialog.ShowErrorAsync("售价必须大于0", "验证失败");
                return false;
            }

            if (detail.CostPrice.HasValue && detail.CostPrice <= 0)
            {
                await Services.Dialog.ShowErrorAsync("成本价必须大于0", "验证失败");
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

            var result = IsNew
                ? await _herbService.CreateAsync(input)
                : await _herbService.UpdateAsync(detail.Id, input);

            if (!result.success)
            {
                Services.ErrorHandler.SetError("Save", result.error ?? "保存药材失败");
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

                OnPropertyChanged(nameof(DetailTitle));
            }

            return true;
        }

        /// <summary>删除项</summary>
        protected override async Task<bool> DeleteItemAsync(HerbListDto item)
        {
            var result = await _herbService.DeleteAsync(item.Id);
            if (!result.success)
            {
                Services.ErrorHandler.SetError("Delete", result.error ?? "删除药材失败");
            }
            return result.success;
        }

        #endregion

        #region 扩展命令

        /// <summary>切换药材状态</summary>
        [RelayCommand(CanExecute = nameof(CanToggleStatus))]
        private async Task ToggleStatusAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                var herb = SelectedItem;
                var newStatus = herb.Status == CommonStatus.Enabled ? "禁用" : "启用";
                var confirmed = await Services.Dialog.ShowConfirmAsync($"确认{newStatus}药材 [{herb.Name}] 吗？", "状态切换确认");
                if (!confirmed) return;

                var result = await _herbRepository.ToggleStatusAsync(herb.Id);
                if (result != null)
                {
                    Logger.LogInformation("药材状态已切换: {HerbName} -> {NewStatus}", herb.Name, result.Status);
                    await Services.Dialog.ShowSuccessAsync($"药材 '{herb.Name}' 已{(result.Status == CommonStatus.Enabled ? "启用" : "禁用")}", "操作成功");
                    await RefreshAsync();
                }
                else
                {
                    await Services.Dialog.ShowErrorAsync("切换药材状态失败", "操作失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "切换药材状态失败");
                await Services.Dialog.ShowErrorAsync("切换药材状态失败", "操作失败");
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

            Services.DetailEditor.CreateNew(() => copy);
            OnPropertyChanged(nameof(DetailTitle));

            Logger.LogInformation("复制药材: {SourceName} -> {CopyName}", SelectedItem?.Name, copy.Name);
        }

        private bool CanCopyHerb() => HasSelection && !IsBusy && IsAdmin;

        /// <summary>恢复软删除</summary>
        [RelayCommand(CanExecute = nameof(CanRestore))]
        private async Task RestoreAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                var herb = SelectedItem;
                var confirmed = await Services.Dialog.ShowConfirmAsync($"确认恢复药材 [{herb.Name}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _herbRepository.RestoreAsync(herb.Id);
                if (result != null)
                {
                    Logger.LogInformation("药材已恢复: {HerbName}", herb.Name);
                    await Services.Dialog.ShowSuccessAsync($"药材 '{herb.Name}' 已恢复", "操作成功");
                    await RefreshAsync();
                }
                else
                {
                    await Services.Dialog.ShowErrorAsync("恢复药材失败", "操作失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "恢复药材失败");
                await Services.Dialog.ShowErrorAsync("恢复药材失败", "操作失败");
            }
        }

        private bool CanRestore() => HasSelection && !IsBusy && IsAdmin;

        /// <summary>导入药材</summary>
        [RelayCommand(CanExecute = nameof(CanImport))]
        private async Task ImportHerbsAsync()
        {
            try
            {
                await Services.Loading.ExecuteWithLoadingAsync(async () =>
                {
                    // TODO: 需要ICommonDialogService来选择文件
                    // var filePath = await _dialogService.ShowOpenFileDialogAsync(filter: "Excel文件|*.xlsx", title: "选择药材导入文件");
                    // 暂时跳过文件选择逻辑
                    await Task.CompletedTask;
                }, "导入药材");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入药材失败");
                await Services.Dialog.ShowErrorAsync("导入药材失败", "操作失败");
            }
        }

        private bool CanImport() => !IsBusy && !IsLoading;

        /// <summary>导出药材</summary>
        [RelayCommand(CanExecute = nameof(CanExport))]
        private async Task ExportHerbsAsync()
        {
            try
            {
                await Services.Loading.ExecuteWithLoadingAsync(async () =>
                {
                    // TODO: 需要ICommonDialogService来保存文件
                    Logger.LogInformation("导出药材数据，关键词：{Keyword}", SearchText);
                    var bytes = await _herbRepository.ExportHerbsAsync(SearchText);

                    if (bytes == null || bytes.Length == 0)
                    {
                        await Services.Dialog.ShowErrorAsync("导出失败，请稍后重试", "导出药材");
                        return;
                    }

                    // TODO: 保存到文件
                    await Task.CompletedTask;
                }, "导出药材");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出药材失败");
                await Services.Dialog.ShowErrorAsync("导出药材失败", "操作失败");
            }
        }

        private bool CanExport() => !IsBusy && !IsLoading && Items.Count > 0;

        /// <summary>查看审计日志</summary>
        [RelayCommand(CanExecute = nameof(CanShowAuditLog))]
        private void ShowAuditLog()
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

        private bool CanShowAuditLog() => HasSelection;

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
