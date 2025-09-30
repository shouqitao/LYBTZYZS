using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 配方管理视图模型 - UltraThink架构重构版本
    /// 基于UnifiedListViewModelBase实现配方管理功能
    /// </summary>
    public class FormulaManagementViewModel : UnifiedListViewModelBase<FormulaDto>
    {
        #region 服务依赖

        private readonly IFormulaService _formulaService;

        #endregion

        #region 构造函数

        public FormulaManagementViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IFormulaService formulaService,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            
            PageTitle = "配方管理";
        }

        #endregion

        #region 实现基类抽象方法

        /// <summary>
        /// 获取数据项（实现基类抽象方法）
        /// </summary>
        protected override async Task<IEnumerable<FormulaDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            try
            {
                var result = await _formulaService.GetPagedAsync(page, pageSize, searchText);
                
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
                    await ShowErrorMessageAsync(result.ErrorMessage ?? "加载配方数据失败");
                    return new List<FormulaDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载配方数据时发生异常");
                await ShowErrorMessageAsync("加载配方数据时发生系统错误");
                return new List<FormulaDto>();
            }
        }

        #endregion

        #region 重写虚方法

        /// <summary>
        /// 执行添加操作
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            NavigateTo("MainRegion", "FormulaDetailView");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 执行删除操作
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(FormulaDto item)
        {
            try
            {
                var result = await _formulaService.DeleteAsync(item.Id);
                
                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync($"配方 '{item.Name}' 删除成功");
                    await LoadPageAsync(); // 重新加载数据
                }
                else
                {
                    await ShowErrorMessageAsync(result.ErrorMessage ?? $"删除配方 {item.Name} 失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除配方时发生异常：{FormulaId}", item.Id);
                await ShowErrorMessageAsync($"删除配方 {item.Name} 时发生系统错误");
            }
        }

        /// <summary>
        /// 执行批量删除操作
        /// </summary>
        protected override async Task OnExecuteBatchDeleteAsync(List<FormulaDto> items)
        {
            try
            {
                var selectedIds = items.Select(f => f.Id).ToList();

                // 循环调用DeleteAsync（Shared.Interfaces暂无BatchDeleteAsync）
                int successCount = 0;
                List<string> errors = new();
                foreach (var id in selectedIds)
                {
                    var deleteResult = await _formulaService.DeleteAsync(id);
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
                    await ShowSuccessMessageAsync($"成功删除 {items.Count} 个配方");
                    await LoadPageAsync(); // 重新加载数据
                }
                else
                {
                    await ShowErrorMessageAsync(result.ErrorMessage ?? "批量删除配方失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量删除配方时发生异常");
                await ShowErrorMessageAsync("批量删除配方时发生系统错误");
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
        /// 查看配方详情
        /// </summary>
        public DelegateCommand<FormulaDto> ViewDetailCommand => 
            new DelegateCommand<FormulaDto>(ViewFormulaDetail, CanViewDetail);

        /// <summary>
        /// 编辑配方
        /// </summary>
        public DelegateCommand<FormulaDto> EditFormulaCommand => 
            new DelegateCommand<FormulaDto>(EditFormula, CanEditFormula);

        /// <summary>
        /// 复制配方
        /// </summary>
        public DelegateCommand<FormulaDto> CopyFormulaCommand => 
            new DelegateCommand<FormulaDto>(CopyFormula, CanCopyFormula);

        /// <summary>
        /// 查看配方详情
        /// </summary>
        private void ViewFormulaDetail(FormulaDto formula)
        {
            if (formula == null) return;
            
            var parameters = new NavigationParameters
            {
                { "FormulaId", formula.Id },
                { "ReadOnly", true }
            };
            NavigateTo("MainRegion", "FormulaDetailView", parameters);
        }

        /// <summary>
        /// 编辑配方
        /// </summary>
        private void EditFormula(FormulaDto formula)
        {
            if (formula == null) return;
            
            var parameters = new NavigationParameters
            {
                { "FormulaId", formula.Id }
            };
            NavigateTo("MainRegion", "FormulaDetailView", parameters);
        }

        /// <summary>
        /// 复制配方
        /// </summary>
        private void CopyFormula(FormulaDto formula)
        {
            if (formula == null) return;
            
            var parameters = new NavigationParameters
            {
                { "SourceFormulaId", formula.Id },
                { "Mode", "Copy" }
            };
            NavigateTo("MainRegion", "FormulaDetailView", parameters);
        }

        /// <summary>
        /// 检查是否可以查看详情
        /// </summary>
        private bool CanViewDetail(FormulaDto formula)
        {
            return formula != null && !IsBusy;
        }

        /// <summary>
        /// 检查是否可以编辑
        /// </summary>
        private bool CanEditFormula(FormulaDto formula)
        {
            return formula != null && !IsBusy;
        }

        /// <summary>
        /// 检查是否可以复制
        /// </summary>
        private bool CanCopyFormula(FormulaDto formula)
        {
            return formula != null && !IsBusy && SessionManager?.HasPermission(UserRole.Admin) == true;
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
    }
}