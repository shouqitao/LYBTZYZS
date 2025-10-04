using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 药材管理视图模型 - UltraThink架构重构版本
    /// 基于UnifiedListViewModelBase实现药材管理功能
    /// </summary>
    public class HerbManagementViewModel : UnifiedListViewModelBase<HerbDto>
    {
        #region 服务依赖

        private readonly IHerbService _herbService;

        #endregion

        #region 构造函数

        public HerbManagementViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IHerbService herbService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));

            PageTitle = "药材管理";
        }

        #endregion

        #region 实现基类抽象方法

        /// <summary>
        /// 获取数据项（实现基类抽象方法）
        /// </summary>
        protected override async Task<IEnumerable<HerbDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            try
            {
                var result = await _herbService.GetPagedAsync(page, pageSize, searchText);

                if (result.IsSuccess && result.Data != null)
                {
                    var pagedData = result.Data;

                    // 更新分页信息
                    TotalCount = pagedData.TotalCount;
                    CurrentPage = pagedData.CurrentPage;
                    PageSize = pagedData.PageSize;

                    return pagedData.Items;
                }
                else
                {
                    await ShowErrorMessageAsync(result.ErrorMessage ?? "加载药材数据失败");
                    return new List<HerbDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材数据时发生异常");
                await ShowErrorMessageAsync("加载药材数据时发生系统错误");
                return new List<HerbDto>();
            }
        }

        #endregion

        #region 重写虚方法

        /// <summary>
        /// 执行添加操作
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            NavigateTo("MainRegion", "HerbDetailView");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 执行删除操作
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(HerbDto item)
        {
            try
            {
                var result = await _herbService.DeleteAsync(item.Id);

                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync($"药材 '{item.Name}' 删除成功");
                    await LoadPageAsync(); // 重新加载数据
                }
                else
                {
                    await ShowErrorMessageAsync(result.ErrorMessage ?? $"删除药材 {item.Name} 失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除药材时发生异常：{HerbId}", item.Id);
                await ShowErrorMessageAsync($"删除药材 {item.Name} 时发生系统错误");
            }
        }

        /// <summary>
        /// 执行批量删除操作
        /// </summary>
        protected override async Task OnExecuteBatchDeleteAsync(List<HerbDto> items)
        {
            try
            {
                var selectedIds = items.Select(h => h.Id).ToList();

                // 循环调用DeleteAsync（Shared.Interfaces暂无BatchDeleteAsync）
                int successCount = 0;
                List<string> errors = new();
                foreach (var id in selectedIds)
                {
                    var deleteResult = await _herbService.DeleteAsync(id);
                    if (deleteResult.IsSuccess)
                        successCount++;
                    else if (!string.IsNullOrEmpty(deleteResult.ErrorMessage))
                        errors.Add(deleteResult.ErrorMessage);
                }
                var result = successCount == selectedIds.Count
                    ? ServiceResult.Success()
                    : ServiceResult.Failure(string.Join("; ", errors));

                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync($"成功删除 {items.Count} 个药材");
                    await LoadPageAsync(); // 重新加载数据
                }
                else
                {
                    await ShowErrorMessageAsync(result.ErrorMessage ?? "批量删除药材失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量删除药材时发生异常");
                await ShowErrorMessageAsync("批量删除药材时发生系统错误");
            }
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 页面加载时调用
        /// </summary>
        protected override async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            await base.OnNavigatedToAsync(navigationContext);
            await LoadPageAsync();
        }

        #endregion

        #region 自定义功能

        /// <summary>
        /// 查看药材详情
        /// </summary>
        public DelegateCommand<HerbDto> ViewDetailCommand =>
            new DelegateCommand<HerbDto>(ViewHerbDetail, CanViewDetail);

        /// <summary>
        /// 编辑药材
        /// </summary>
        public DelegateCommand<HerbDto> EditCommand =>
            new DelegateCommand<HerbDto>(EditHerb, CanEditHerb);

        /// <summary>
        /// 复制药材
        /// </summary>
        public DelegateCommand<HerbDto> CopyHerbCommand =>
            new DelegateCommand<HerbDto>(CopyHerb, CanCopyHerb);

        /// <summary>
        /// 查看药材详情
        /// </summary>
        private void ViewHerbDetail(HerbDto herb)
        {
            if (herb == null) return;

            var parameters = new NavigationParameters
            {
                { "HerbId", herb.Id },
                { "ReadOnly", true }
            };
            NavigateTo("MainRegion", "HerbDetailView", parameters);
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        private void EditHerb(HerbDto herb)
        {
            if (herb == null) return;

            var parameters = new NavigationParameters
            {
                { "HerbId", herb.Id }
            };
            NavigateTo("MainRegion", "HerbDetailView", parameters);
        }

        /// <summary>
        /// 复制药材
        /// </summary>
        private void CopyHerb(HerbDto herb)
        {
            if (herb == null) return;

            var parameters = new NavigationParameters
            {
                { "SourceHerbId", herb.Id },
                { "Mode", "Copy" }
            };
            NavigateTo("MainRegion", "HerbDetailView", parameters);
        }

        /// <summary>
        /// 检查是否可以查看详情
        /// </summary>
        private bool CanViewDetail(HerbDto herb)
        {
            return herb != null && !IsBusy;
        }

        /// <summary>
        /// 检查是否可以编辑
        /// </summary>
        private bool CanEditHerb(HerbDto herb)
        {
            return herb != null && !IsBusy;
        }

        /// <summary>
        /// 检查是否可以复制
        /// </summary>
        private bool CanCopyHerb(HerbDto herb)
        {
            return herb != null && !IsBusy && SessionManager?.HasPermission(UserRole.Admin) == true;
        }

        /// <summary>
        /// 切换状态命令
        /// </summary>
        public DelegateCommand<HerbDto> ToggleStatusCommand =>
            new DelegateCommand<HerbDto>(async item => await ToggleStatus(item), CanToggleStatus);

        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand =>
            new DelegateCommand(ExecuteFirstPage, () => CanGoPreviousPage && !IsLoading);

        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand =>
            new DelegateCommand(ExecuteLastPage, () => CanGoNextPage && !IsLoading);

        /// <summary>
        /// 导入药材命令
        /// </summary>
        public DelegateCommand ImportHerbsCommand =>
            new DelegateCommand(async () => await ExecuteImportHerbsAsync(), () => !IsLoading);

        /// <summary>
        /// 导出模板命令
        /// </summary>
        public DelegateCommand ExportTemplateCommand =>
            new DelegateCommand(async () => await ExecuteExportTemplateAsync(), () => !IsLoading);

        /// <summary>
        /// 导出药材命令
        /// </summary>
        public DelegateCommand ExportHerbsCommand =>
            new DelegateCommand(async () => await ExecuteExportHerbsAsync(), () => Items.Count > 0 && !IsLoading);

        /// <summary>
        /// 切换状态
        /// </summary>
        private async Task ToggleStatus(HerbDto herb)
        {
            if (herb == null) return;

            try
            {
                // TODO: 调用服务切换状态 (假设服务接口需要扩展)
                await ShowSuccessMessageAsync($"切换药材 '{herb.Name}' 状态功能开发中");
                // var result = await _herbService.ToggleStatusAsync(herb.Id);
                // if (result.IsSuccess)
                // {
                //     await ShowSuccessMessageAsync($"药材 '{herb.Name}' 状态已更新");
                //     await LoadPageAsync();
                // }
                // else
                // {
                //     await ShowErrorMessageAsync(result.ErrorMessage ?? "切换状态失败");
                // }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "切换药材状态时发生异常");
                await ShowErrorMessageAsync("切换状态时发生系统错误");
            }
        }

        /// <summary>
        /// 检查是否可以切换状态
        /// </summary>
        private bool CanToggleStatus(HerbDto herb)
        {
            return herb != null && !IsBusy;
        }

        /// <summary>
        /// 跳转首页
        /// </summary>
        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
        }

        /// <summary>
        /// 跳转末页
        /// </summary>
        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
        }

        /// <summary>
        /// 导入药材
        /// </summary>
        private async Task ExecuteImportHerbsAsync()
        {
            try
            {
                // TODO: 实现导入逻辑
                await ShowSuccessMessageAsync("导入药材功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入药材时发生异常");
                await ShowErrorMessageAsync("导入药材时发生系统错误");
            }
        }

        /// <summary>
        /// 导出模板
        /// </summary>
        private async Task ExecuteExportTemplateAsync()
        {
            try
            {
                // TODO: 实现导出模板逻辑
                await ShowSuccessMessageAsync("导出模板功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出模板时发生异常");
                await ShowErrorMessageAsync("导出模板时发生系统错误");
            }
        }

        /// <summary>
        /// 导出药材
        /// </summary>
        private async Task ExecuteExportHerbsAsync()
        {
            try
            {
                // TODO: 实现导出逻辑
                await ShowSuccessMessageAsync("导出药材功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出药材时发生异常");
                await ShowErrorMessageAsync("导出药材时发生系统错误");
            }
        }

        #endregion

        #region 搜索功能增强

        /// <summary>
        /// 按分类搜索
        /// </summary>
        public DelegateCommand<string> SearchByCategoryCommand =>
            new DelegateCommand<string>(SearchByCategory);

        /// <summary>
        /// 按分类搜索
        /// </summary>
        private async void SearchByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;

            SearchText = $"分类:{category}";
            await LoadPageAsync();
        }

        #endregion

        #region 命令刷新

        /// <summary>
        /// 刷新所有命令的可执行状态
        /// </summary>
        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();

            EditCommand?.RaiseCanExecuteChanged();
            ToggleStatusCommand?.RaiseCanExecuteChanged();
            FirstPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
            ImportHerbsCommand?.RaiseCanExecuteChanged();
            ExportTemplateCommand?.RaiseCanExecuteChanged();
            ExportHerbsCommand?.RaiseCanExecuteChanged();
            ViewDetailCommand?.RaiseCanExecuteChanged();
            CopyHerbCommand?.RaiseCanExecuteChanged();
            SearchByCategoryCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
