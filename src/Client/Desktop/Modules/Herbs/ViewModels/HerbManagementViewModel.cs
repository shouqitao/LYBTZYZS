using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.Herbs.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 中药材管理视图模型（UltraThink 现代架构版）
    /// 基于ModernManagementViewModel，统一的管理界面模式
    /// 零编译警告，现代化MVVM架构
    /// </summary>
    public class HerbManagementViewModel : ModernManagementViewModel<HerbDto>
    {
        #region Fields

        private readonly HerbModule _herbService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;

        #endregion

        #region 额外Commands

        /// <summary>切换状态命令</summary>
        public DelegateCommand ToggleStatusCommand { get; }
        
        /// <summary>导入药材命令</summary>
        public DelegateCommand ImportHerbsCommand { get; }
        
        /// <summary>导出模板命令</summary>
        public DelegateCommand ExportTemplateCommand { get; }

        #endregion


        #region Constructor

        public HerbManagementViewModel(
            HerbModule herbService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, errorHandlingService)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化额外命令
            ToggleStatusCommand = new DelegateCommand(async () => await ExecuteToggleStatusAsync(), () => HasSelectedItem);
            ImportHerbsCommand = new DelegateCommand(async () => await ExecuteImportAsync());
            ExportTemplateCommand = new DelegateCommand(async () => await ExecuteExportTemplateAsync());
        }

        /// <summary>兼容性构造函数</summary>
        public HerbManagementViewModel(
            HerbModule herbService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator)
            : this(herbService, dialogService, mapper, eventAggregator, null)
        {
        }

        #endregion


        #region 重写基类方法

        /// <summary>加载数据</summary>
        protected override async Task<ServiceResult<PagedResult<HerbDto>>> LoadDataAsync(int page, int pageSize, string? keyword = null)
        {
            var herbQuery = new HerbPagedQueryDto
            {
                PageIndex = page,
                PageSize = pageSize,
                Keyword = keyword ?? string.Empty
            };
            return await _herbService.GetPagedAsync(herbQuery);
        }

        /// <summary>添加项</summary>
        protected override async Task OnAddAsync()
        {
            var parameters = new Dictionary<string, object> { ["IsEditMode"] = false };
            var result = await _dialogService.ShowDialogAsync("HerbAddEditDialog", parameters);
            
            if (result.Result == true)
            {
                await _dialogService.ShowSuccessAsync("药材信息添加成功", "成功");
            }
        }

        /// <summary>编辑项</summary>
        protected override async Task OnEditAsync(HerbDto item)
        {
            var parameters = new Dictionary<string, object>
            {
                ["IsEditMode"] = true,
                ["Herb"] = item
            };
            var result = await _dialogService.ShowDialogAsync("HerbAddEditDialog", parameters);
            
            if (result.Result == true)
            {
                await _dialogService.ShowSuccessAsync($"药材 {item.Name} 信息更新成功", "成功");
            }
        }

        /// <summary>删除项（实际是禁用）</summary>
        protected override async Task OnDeleteAsync(HerbDto item)
        {
            await ToggleHerbStatusAsync(item);
        }

        /// <summary>查看详情</summary>
        protected override async Task OnViewDetailsAsync(HerbDto item)
        {
            var result = await _herbService.GetByIdAsync(item.Id);
            
            if (result.IsSuccess && result.Data != null)
            {
                var herbDetail = result.Data;
                var detailInfo = $"药材详情：\n\n" +
                               $"名称: {herbDetail.Name}\n" +
                               $"产地: {herbDetail.Origin ?? "未知"}\n" +
                               $"规格: {herbDetail.Spec ?? "未知"}\n" +
                               $"单价: ¥{herbDetail.Price:F2}/{herbDetail.Unit}\n" +
                               $"功效: {herbDetail.Effect ?? "未录入"}\n" +
                               $"用法: {herbDetail.Usage ?? "未录入"}\n" +
                               $"状态: {(herbDetail.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                               $"备注: {herbDetail.Remark ?? "无"}";

                await _dialogService.ShowInformationAsync(detailInfo, $"药材详情 - {herbDetail.Name}");
            }
            else
            {
                await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "获取药材详情失败", "错误");
            }
        }

        /// <summary>更新Command状态</summary>
        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            ToggleStatusCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Command执行方法

        /// <summary>切换状态命令执行</summary>
        private async Task ExecuteToggleStatusAsync()
        {
            if (SelectedItem != null)
            {
                await ToggleHerbStatusAsync(SelectedItem);
            }
        }

        /// <summary>导入命令执行</summary>
        private async Task ExecuteImportAsync()
        {
            var filePath = await _dialogService.ShowOpenFileDialogAsync("选择药材导入文件", "Excel文件|*.xlsx;*.xls|CSV文件|*.csv|所有文件|*.*");
            
            if (!string.IsNullOrEmpty(filePath))
            {
                await _dialogService.ShowInformationAsync(
                    $"已选择导入文件：\n{filePath}\n\n药材批量导入功能将在后续版本中提供", 
                    "导入功能说明");
            }
        }

        /// <summary>导出模板命令执行</summary>
        private async Task ExecuteExportTemplateAsync()
        {
            var filePath = await _dialogService.ShowSaveFileDialogAsync("导出药材模板", "Excel文件|*.xlsx|CSV文件|*.csv|所有文件|*.*", "药材导入模板.xlsx");
            
            if (!string.IsNullOrEmpty(filePath))
            {
                await _dialogService.ShowInformationAsync(
                    $"模板导出路径：\n{filePath}\n\n药材导入模板生成功能将在后续版本中提供", 
                    "模板导出说明");
            }
        }

        /// <summary>切换药材状态</summary>
        private async Task ToggleHerbStatusAsync(HerbDto herb)
        {
            var isEnabled = herb.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}药材 {herb.Name} 吗？",
                $"{action}药材");

            if (confirm)
            {
                ServiceResult result = isEnabled 
                    ? await _herbService.DisableAsync(herb.Id)
                    : await _herbService.EnableAsync(herb.Id);

                if (result.IsSuccess)
                {
                    await _dialogService.ShowInformationAsync($"药材{action}成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"药材{action}失败",
                        "错误");
                }
            }
        }

        #endregion
    }
}