using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 验方管理视图模型（UltraThink 现代架构版）
    /// 基于ModernManagementViewModel，统一的管理界面模式
    /// 零编译警告，现代化MVVM架构
    /// </summary>
    public class FormulaManagementViewModel : ModernManagementViewModel<FormulaDto>
    {
        #region Fields

        private readonly IFormulaService _formulaService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;

        #endregion

        #region 额外Commands

        /// <summary>切换状态命令</summary>
        public DelegateCommand ToggleStatusCommand { get; }
        
        /// <summary>复制验方命令</summary>
        public DelegateCommand CopyCommand { get; }
        
        /// <summary>导入验方命令</summary>
        public DelegateCommand ImportCommand { get; }
        
        /// <summary>清空筛选命令</summary>
        public DelegateCommand ClearFiltersCommand { get; }

        #endregion


        #region Constructor

        public FormulaManagementViewModel(
            IFormulaService formulaService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, errorHandlingService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化额外命令
            ToggleStatusCommand = new DelegateCommand(async () => await ExecuteToggleStatusAsync(), () => HasSelectedItem);
            CopyCommand = new DelegateCommand(async () => await ExecuteCopyAsync(), () => HasSelectedItem);
            ImportCommand = new DelegateCommand(async () => await ExecuteImportAsync());
            ClearFiltersCommand = new DelegateCommand(async () => await ExecuteClearFiltersAsync());
        }

        /// <summary>兼容性构造函数</summary>
        public FormulaManagementViewModel(
            IFormulaService formulaService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator)
            : this(formulaService, dialogService, mapper, eventAggregator, null)
        {
        }

        #endregion


        #region 重写基类方法

        /// <summary>加载数据</summary>
        protected override async Task<ServiceResult<PagedResult<FormulaDto>>> LoadDataAsync(int page, int pageSize, string? keyword = null)
        {
            var formulaQuery = new FormulaQueryDto
            {
                PageIndex = page,
                PageSize = pageSize,
                Keyword = keyword ?? string.Empty
            };
            return await _formulaService.GetPagedAsync(formulaQuery);
        }

        /// <summary>添加项</summary>
        protected override async Task OnAddAsync()
        {
            var parameters = new Dictionary<string, object>();
            var result = await _dialogService.ShowDialogAsync("AddFormulaDialog", parameters);
            
            if (result.Result == true)
            {
                await _dialogService.ShowSuccessAsync("验方添加成功", "成功");
            }
        }

        /// <summary>编辑项</summary>
        protected override async Task OnEditAsync(FormulaDto item)
        {
            var parameters = new Dictionary<string, object> { ["Formula"] = item };
            var result = await _dialogService.ShowDialogAsync("EditFormulaDialog", parameters);
            
            if (result.Result == true)
            {
                await _dialogService.ShowSuccessAsync($"验方 {item.Name} 更新成功", "成功");
            }
        }

        /// <summary>删除项（实际是禁用）</summary>
        protected override async Task OnDeleteAsync(FormulaDto item)
        {
            await ToggleFormulaStatusAsync(item);
        }

        /// <summary>查看详情</summary>
        protected override async Task OnViewDetailsAsync(FormulaDto item)
        {
            var result = await _formulaService.GetByIdAsync(item.Id);
            
            if (result.IsSuccess && result.Data != null)
            {
                var formulaDetail = result.Data;
                var detailInfo = $"验方详情：\n\n" +
                               $"名称: {formulaDetail.Name}\n" +
                               $"分类: {formulaDetail.Category ?? "未分类"}\n" +
                               $"状态: {(formulaDetail.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                               $"备注: {formulaDetail.Remark ?? "无"}";

                await _dialogService.ShowInformationAsync(detailInfo, $"验方详情 - {formulaDetail.Name}");
            }
            else
            {
                await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "获取验方详情失败", "错误");
            }
        }

        /// <summary>导出数据</summary>
        protected override async Task OnExportAsync()
        {
            var filePath = await _dialogService.ShowSaveFileDialogAsync("导出验方", "JSON文件|*.json|所有文件|*.*", "验方导出.json");
            
            if (!string.IsNullOrEmpty(filePath))
            {
                await _dialogService.ShowInformationAsync(
                    $"导出路径：\n{filePath}\n\n验方批量导出功能将在后续版本中提供", 
                    "导出功能说明");
            }
        }

        /// <summary>更新Command状态</summary>
        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            ToggleStatusCommand.RaiseCanExecuteChanged();
            CopyCommand.RaiseCanExecuteChanged();
        }

        #endregion


        #region Command执行方法

        /// <summary>切换状态命令执行</summary>
        private async Task ExecuteToggleStatusAsync()
        {
            if (SelectedItem != null)
            {
                await ToggleFormulaStatusAsync(SelectedItem);
            }
        }

        /// <summary>复制命令执行</summary>
        private async Task ExecuteCopyAsync()
        {
            if (SelectedItem != null)
            {
                await _dialogService.ShowInformationAsync(
                    $"验方复制功能：\n\n将复制验方 '{SelectedItem.Name}'\n\n验方复制功能将在后续版本中提供", 
                    "复制功能说明");
            }
        }

        /// <summary>导入命令执行</summary>
        private async Task ExecuteImportAsync()
        {
            var filePath = await _dialogService.ShowOpenFileDialogAsync("选择验方文件", "JSON文件|*.json|所有文件|*.*");
            
            if (!string.IsNullOrEmpty(filePath))
            {
                await _dialogService.ShowInformationAsync(
                    $"已选择导入文件：\n{filePath}\n\n验方批量导入功能将在后续版本中提供", 
                    "导入功能说明");
            }
        }

        /// <summary>清空筛选命令执行</summary>
        private async Task ExecuteClearFiltersAsync()
        {
            SearchKeyword = string.Empty;
            await ExecuteAsync(async () => await OnRefreshAsync(), "清空筛选条件");
        }

        /// <summary>切换验方状态</summary>
        private async Task ToggleFormulaStatusAsync(FormulaDto formula)
        {
            var isEnabled = formula.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}验方 {formula.Name} 吗？",
                $"{action}验方");

            if (confirm)
            {
                ServiceResult result = isEnabled 
                    ? await _formulaService.DisableAsync(formula.Id)
                    : await _formulaService.EnableAsync(formula.Id);

                if (result.IsSuccess)
                {
                    await _dialogService.ShowInformationAsync($"验方{action}成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"验方{action}失败",
                        "错误");
                }
            }
        }

        #endregion
    }
}