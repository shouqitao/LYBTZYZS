using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using Prism.Commands;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 中药材管理视图模型（增强版 - 迁移自SystemManagement）
    /// UltraThink Phase 5 DTO统一化: HerbInfo→HerbDto
    /// </summary>
    public class HerbManagementViewModelEnhanced : BaseServiceManagementViewModel<HerbDto, IHerbService>
    {
        private readonly ICustomDialogService _commonDialogService;
        private readonly ICustomDialogService _dialogService;
        private readonly IHerbApiService _herbApiService;

        protected override string ModuleName => "中药材管理";

        #region Properties

        private int _lowStockCount = 0;
        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        #endregion

        #region Commands

        public DelegateCommand ImportHerbsCommand { get; }
        public DelegateCommand ExportTemplateCommand { get; }
        public DelegateCommand<HerbDto> ManageStockCommand { get; }

        #endregion

        public HerbManagementViewModelEnhanced(
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
            
            // 初始化增强功能命令
            ImportHerbsCommand = new DelegateCommand(async () => await ImportHerbs());
            ExportTemplateCommand = new DelegateCommand(ExportTemplate);
            ManageStockCommand = new DelegateCommand<HerbDto>(ManageStock);
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
                    Name = SearchKeyword
                };

                var pagedResult = await Service.GetPagedAsync(query);
                var result = pagedResult.Data;

                // 更新库存不足数量 (暂时注释库存相关功能)
                // TODO: 集成库存管理服务后启用
                LowStockCount = 0;

                return ServiceResult<LYBT.Shared.Models.Contracts.Common.PagedResult<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<LYBT.Shared.Models.Contracts.Common.PagedResult<HerbDto>>.Failure($"加载药材列表失败: {ex.Message}");
            }
        }

        protected override async Task AddAsync()
        {
            try
            {
                var dialog = new Views.HerbAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"打开新增药材对话框失败: {ex.Message}", "错误");
            }
        }

        protected override async Task EditAsync(HerbDto item)
        {
            if (item == null)
                return;

            try
            {
                var dialog = new Views.HerbAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;

                // 为编辑模式创建ViewModel并设置药材信息
                var viewModel = new ViewModels.HerbAddEditDialogViewModel(_herbApiService, item);
                dialog.DataContext = viewModel;

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"打开编辑药材对话框失败: {ex.Message}", "错误");
            }
        }

        protected override async Task DeleteAsync(HerbDto item)
        {
            if (item == null) return;

            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要删除药材「{item.Name}」吗？",
                "删除确认");

            if (confirm)
            {
                var result = await Service.DeleteAsync(item.Id);
                if (result.IsSuccess)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync("药材删除成功", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "删除药材失败",
                        "错误");
                }
            }
        }

        #endregion

        #region 增强功能 - 迁移自SystemManagement

        /// <summary>
        /// 导入中药材数据
        /// </summary>
        private async Task ImportHerbs()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                    DefaultExt = "xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    // 读取Excel文件
                    var dataTable = Core.Helpers.ExcelHelper.ImportFromExcel(dialog.FileName);

                    if (dataTable.Rows.Count == 0)
                    {
                        await _commonDialogService.ShowWarningAsync("Excel文件中没有数据", "提示");
                        return;
                    }

                    // 验证列
                    var requiredColumns = new[] { "药材名称*", "单位*", "单价（元）*" };
                    foreach (var column in requiredColumns)
                    {
                        if (!dataTable.Columns.Contains(column))
                        {
                            await _commonDialogService.ShowErrorAsync($"Excel文件缺少必需的列：{column}", "错误");
                            return;
                        }
                    }

                    // 导入数据
                    int successCount = 0;
                    int failCount = 0;
                    var errors = new List<string>();

                    foreach (System.Data.DataRow row in dataTable.Rows)
                    {
                        try
                        {
                            var herbName = row["药材名称*"]?.ToString()?.Trim();
                            if (string.IsNullOrWhiteSpace(herbName))
                            {
                                failCount++;
                                errors.Add($"第{dataTable.Rows.IndexOf(row) + 2}行：药材名称不能为空");
                                continue;
                            }

                            var dto = new HerbCreateDto
                            {
                                Name = herbName,
                                PinYinCode = LYBT.Shared.Utilities.Helpers.CommonHelper.GetPinyinCode(herbName),
                                WuBiCode = LYBT.Shared.Utilities.Helpers.CommonHelper.GetWuBiCode(herbName),
                                Origin = row.Table.Columns.Contains("产地") ? row["产地"]?.ToString()?.Trim() : null,
                                Spec = row.Table.Columns.Contains("规格") ? row["规格"]?.ToString()?.Trim() : null,
                                Unit = row["单位*"]?.ToString()?.Trim() ?? "克",
                                Price = decimal.TryParse(row["单价（元）*"]?.ToString(), out var price) ? price : 0,
                                Effect = row.Table.Columns.Contains("功效说明") ? row["功效说明"]?.ToString()?.Trim() : null,
                                Usage = row.Table.Columns.Contains("用法") ? row["用法"]?.ToString()?.Trim() : null,
                                Remark = row.Table.Columns.Contains("备注") ? row["备注"]?.ToString()?.Trim() : null,
                                Status = CommonStatus.Enabled
                            };

                            // 验证数据
                            if (dto.Price <= 0)
                            {
                                failCount++;
                                errors.Add($"第{dataTable.Rows.IndexOf(row) + 2}行：单价必须大于0");
                                continue;
                            }

                            // 调用服务创建药材
                            var response = await Service.CreateAsync(dto);
                            if (response.IsSuccess)
                            {
                                successCount++;
                            }
                            else
                            {
                                failCount++;
                                errors.Add($"第{dataTable.Rows.IndexOf(row) + 2}行：{response.ErrorMessage ?? "创建失败"}");
                            }
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            errors.Add($"第{dataTable.Rows.IndexOf(row) + 2}行：{ex.Message}");
                        }
                    }

                    // 显示导入结果
                    var message = $"导入完成！\n成功：{successCount} 条\n失败：{failCount} 条";
                    if (errors.Count > 0)
                    {
                        message += $"\n\n错误详情（仅显示前10条）：\n{string.Join("\n", errors.Take(10))}";
                        if (errors.Count > 10)
                        {
                            message += $"\n... 还有 {errors.Count - 10} 条错误";
                        }
                    }

                    await _commonDialogService.ShowInformationAsync(message, "导入结果");

                    // 刷新列表
                    if (successCount > 0)
                    {
                        await RefreshAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"导入药材失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 导出药材导入模板
        /// </summary>
        private void ExportTemplate()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                    FileName = $"药材导入模板_{DateTime.Now:yyyyMMdd}.xlsx",
                    DefaultExt = "xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 定义模板列
                    var columns = new[]
                    {
                        "药材名称*",
                        "产地",
                        "规格",
                        "单位*",
                        "单价（元）*",
                        "功效说明",
                        "用法",
                        "备注"
                    };

                    // 添加示例数据
                    var sampleData = new List<string[]>
                    {
                        new[] { "人参", "吉林", "优质", "克", "100.00", "大补元气，复脉固脱", "煎服，3-9g", "示例数据，导入时请删除" },
                        new[] { "当归", "甘肃", "特级", "克", "50.00", "补血活血，调经止痛", "煎服，6-12g", "示例数据，导入时请删除" }
                    };

                    // 创建模板
                    Core.Helpers.ExcelHelper.CreateTemplate(columns, dialog.FileName, "药材导入模板", sampleData);

                    _commonDialogService.ShowInformationAsync(
                        "药材导入模板创建成功！\n\n说明：\n1. 带*号的列为必填项\n2. 拼音码和五笔码将在导入时自动生成\n3. 请删除示例数据后再导入实际数据", 
                        "导出成功").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"导出模板失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 管理库存
        /// </summary>
        private void ManageStock(HerbDto herb)
        {
            if (herb == null)
                return;

            try
            {
                // TODO: 实现库存管理对话框
                // 临时显示功能开发中的提示
                _commonDialogService.ShowInformationAsync(
                    $"药材「{herb.Name}」的库存管理功能正在开发中\n\n功能规划：\n- 库存入库/出库\n- 库存预警设置\n- 库存历史记录\n- 批次管理", 
                    "功能开发中").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开库存管理对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion
    }
}