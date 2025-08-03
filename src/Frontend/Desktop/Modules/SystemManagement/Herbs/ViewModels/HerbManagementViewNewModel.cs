using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.ViewModels.Base;
using Prism.Commands;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.SystemManagement.Herbs.ViewModels
{
    /// <summary>
    /// 草药管理视图模型（使用新基类）
    /// </summary>
    public class HerbManagementViewNewModel : BaseListViewModel<HerbDto>
    {
        private readonly IHerbService _herbService;
        private readonly IDialogService _dialogService;
        
        private string _filterStatus = string.Empty;
        private string _filterStock = string.Empty;
        private bool _selectAll;

        public HerbManagementViewNewModel(
            IHerbService herbService,
            IDialogService dialogService)
        {
            _herbService = herbService;
            _dialogService = dialogService;
            
            PageTitle = "草药管理";
            
            // 初始化命令
            EditHerbCommand = new DelegateCommand<HerbDto>(ExecuteEditHerb);
            ToggleHerbStatusCommand = new DelegateCommand<HerbDto>(async herb => await ExecuteToggleStatus(herb));
        }

        #region 属性

        /// <summary>
        /// 状态筛选
        /// </summary>
        public string FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        /// <summary>
        /// 库存筛选
        /// </summary>
        public string FilterStock
        {
            get => _filterStock;
            set
            {
                if (SetProperty(ref _filterStock, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        /// <summary>
        /// 全选
        /// </summary>
        public bool SelectAll
        {
            get => _selectAll;
            set
            {
                if (SetProperty(ref _selectAll, value))
                {
                    foreach (var item in Items)
                    {
                        // 假设 HerbDto 有 IsSelected 属性
                        // item.IsSelected = value;
                    }
                }
            }
        }

        #endregion

        #region 命令

        public DelegateCommand<HerbDto> EditHerbCommand { get; }
        public DelegateCommand<HerbDto> ToggleHerbStatusCommand { get; }

        #endregion

        #region 方法重写

        /// <summary>
        /// 获取数据
        /// </summary>
        protected override async Task<IEnumerable<HerbDto>> GetDataAsync()
        {
            var query = new HerbPagedQueryDto
            {
                Keyword = SearchText,
                PageIndex = CurrentPage,
                PageSize = PageSize
            };

            // 应用筛选条件
            if (!string.IsNullOrEmpty(FilterStatus))
            {
                query.IsActive = FilterStatus == "Active";
            }

            var result = await _herbService.GetPagedAsync(query);
            if (result.Success && result.Data != null)
            {
                TotalCount = result.Data.TotalCount;
                return result.Data.Items;
            }

            return Enumerable.Empty<HerbDto>();
        }

        /// <summary>
        /// 执行新增
        /// </summary>
        protected override async Task ExecuteAddAsync()
        {
            var parameters = new DialogParameters
            {
                { "Mode", "Add" }
            };

            _dialogService.ShowDialog("HerbEditDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    _ = LoadDataAsync();
                }
            });

            await Task.CompletedTask;
        }

        /// <summary>
        /// 执行批量禁用
        /// </summary>
        protected override async Task PerformBatchDisableAsync(IList<HerbDto> items)
        {
            var ids = items.Select(x => x.Id).ToList();
            var result = await _herbService.BatchDisableAsync(ids);
            
            if (!result.Success)
            {
                throw new Exception(result.Message);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 执行编辑
        /// </summary>
        private void ExecuteEditHerb(HerbDto herb)
        {
            var parameters = new DialogParameters
            {
                { "Mode", "Edit" },
                { "HerbId", herb.Id }
            };

            _dialogService.ShowDialog("HerbEditDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    _ = LoadDataAsync();
                }
            });
        }

        /// <summary>
        /// 执行切换状态
        /// </summary>
        private async Task ExecuteToggleStatus(HerbDto herb)
        {
            try
            {
                var action = herb.IsActive ? "禁用" : "启用";
                var result = MessageBox.Show(
                    $"确定要{action}草药 [{herb.Name}] 吗？",
                    $"确认{action}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    
                    // 调用服务切换状态
                    var response = herb.IsActive 
                        ? await _herbService.DisableAsync(herb.Id)
                        : await _herbService.EnableAsync(herb.Id);

                    if (response.Success)
                    {
                        await LoadDataAsync();
                        MessageBox.Show($"{action}成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"{action}失败：{response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}