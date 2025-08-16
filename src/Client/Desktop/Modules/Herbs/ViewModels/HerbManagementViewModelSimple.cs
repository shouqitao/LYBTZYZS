using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using Prism.Commands;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 中药材管理视图模型（简化重构版）- UltraThink Phase 5 DTO统一化
    /// </summary>
    public class HerbManagementViewModelSimple : BaseServiceManagementViewModel<HerbDto, IHerbService>
    {
        private readonly ICustomDialogService _commonDialogService;
        private readonly ICustomDialogService _dialogService;
        private readonly IHerbApiService _herbApiService;

        protected override string ModuleName => "中药材管理";

        #region Commands

        public DelegateCommand<HerbDto> ToggleStatusCommand { get; }
        public DelegateCommand<HerbDto> BatchUpdateStatusCommand { get; }

        #endregion

        public HerbManagementViewModelSimple(
            IHerbService herbService,
            IHerbApiService herbApiService,
            ICustomDialogService commonDialogService,
            ICustomDialogService dialogService,
            Prism.Events.IEventAggregator eventAggregator)
            : base(herbService, eventAggregator)
        {
            _commonDialogService = commonDialogService;
            _dialogService = dialogService;
            _herbApiService = herbApiService;

            // 初始化命令
            ToggleStatusCommand = new DelegateCommand<HerbDto>(async herb => await ToggleStatusAsync(herb));
            BatchUpdateStatusCommand = new DelegateCommand<HerbDto>(async herb => await BatchUpdateStatusAsync(herb));
        }

        #region 重写基类方法

        protected override async Task<ServiceResult<LYBT.Shared.Models.Contracts.Common.PagedResult<HerbDto>>> LoadDataFromServiceAsync(PagedQueryBaseDto request)
        {
            try
            {
                var query = new HerbPagedQueryDto
                {
                    PageIndex = request.PageIndex,
                    PageSize = request.PageSize,
                    Keyword = SearchKeyword
                };

                var pagedResult = await Service.GetPagedAsync(query);
                var result = pagedResult.Data;
                return ServiceResult<LYBT.Shared.Models.Contracts.Common.PagedResult<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<LYBT.Shared.Models.Contracts.Common.PagedResult<HerbDto>>.Failure($"加载中药材列表失败: {ex.Message}");
            }
        }

        protected override async Task AddAsync()
        {
            try
            {
                var dialog = new Views.HerbAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "新增中药材";

                // 创建ViewModel并设置为添加模式
                var viewModel = new HerbAddEditDialogViewModel(_herbApiService, null); // null表示新增
                dialog.DataContext = viewModel;

                // 设置保存成功回调
                viewModel.SaveCompleteCallback = (success) =>
                {
                    if (success)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                };

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync("中药材添加成功", "成功");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"添加中药材失败: {ex.Message}", "错误");
            }
        }

        protected override async Task EditAsync(HerbDto item)
        {
            if (item == null) return;

            try
            {
                var dialog = new Views.HerbAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "编辑中药材";

                // 创建ViewModel并设置为编辑模式
                var viewModel = new HerbAddEditDialogViewModel(_herbApiService, item);
                dialog.DataContext = viewModel;

                // 设置保存成功回调
                viewModel.SaveCompleteCallback = (success) =>
                {
                    if (success)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                };

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync("中药材编辑成功", "成功");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"编辑中药材失败: {ex.Message}", "错误");
            }
        }

        protected override async Task DeleteAsync(HerbDto item)
        {
            if (item == null) return;

            // 中药材不支持删除，只能禁用
            await ToggleStatusAsync(item);
        }

        #endregion

        #region 额外方法

        /// <summary>
        /// 切换中药材状态
        /// </summary>
        private async Task ToggleStatusAsync(HerbDto herb)
        {
            if (herb == null) return;

            var action = herb.Status == CommonStatus.Enabled ? "禁用" : "启用";
            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要{action}中药材 {herb.Name} 吗？",
                $"{action}中药材");

            if (confirm)
            {
                var newStatus = herb.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
                var statusDto = new CommonStatusUpdateDto
                {
                    Status = newStatus,
                    Reason = $"手动{action}中药材"
                };
                
                // UltraThink统一标准：使用UpdateAsync替代UpdateStatusAsync
                var updateDto = new HerbUpdateDto 
                {
                    Name = herb.Name,
                    Effect = herb.Effect, // UltraThink标准：使用Effect替代Description
                    Price = herb.Price,
                    Unit = herb.Unit,
                    Status = newStatus
                };
                var result = await Service.UpdateAsync(herb.Id, updateDto);

                if (result.IsSuccess)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync($"中药材{action}成功", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"中药材{action}失败",
                        "错误");
                }
            }
        }

        /// <summary>
        /// 批量更新中药材状态
        /// </summary>
        private async Task BatchUpdateStatusAsync(HerbDto herb)
        {
            // 简化版本，可以扩展为真正的批量操作
            await ToggleStatusAsync(herb);
        }

        #endregion
    }
}