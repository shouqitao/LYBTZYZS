using System.Data;
using AutoMapper;
using LYBT.Desktop.Core.Helpers;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Win32;
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

        private readonly IHerbService _herbService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;

        #endregion Fields

        #region 额外Commands

        /// <summary>切换状态命令</summary>
        public DelegateCommand ToggleStatusCommand { get; }

        /// <summary>导入药材命令</summary>
        public DelegateCommand ImportHerbsCommand { get; }

        /// <summary>导出药材数据命令</summary>
        public DelegateCommand ExportHerbsCommand { get; }

        /// <summary>导出模板命令</summary>
        public DelegateCommand ExportTemplateCommand { get; }

        #endregion 额外Commands

        #region Constructor

        public HerbManagementViewModel(
            IHerbService herbService,
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
            ImportHerbsCommand = new DelegateCommand(async () => await ExecuteImportAsync(), () => !IsLoading);
            ExportHerbsCommand = new DelegateCommand(async () => await ExecuteExportAsync(), () => !IsLoading);
            ExportTemplateCommand = new DelegateCommand(async () => await ExecuteExportTemplateAsync(), () => !IsLoading);
        }

        /// <summary>Initializes a new instance of the <see cref="HerbManagementViewModel"/> class.兼容性构造函数</summary>
        public HerbManagementViewModel(
            IHerbService herbService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator)
            : this(herbService, dialogService, mapper, eventAggregator, null)
        {
        }

        #endregion Constructor

        #region 重写基类方法

        /// <summary>加载数据</summary>
        protected override async Task<ServiceResult<PagedResult<HerbDto>>> LoadDataAsync(int page, int pageSize, string? keyword = null)
        {
            var herbQuery = new HerbSearchDto
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

        #endregion 重写基类方法

        #region Command执行方法

        /// <summary>切换状态命令执行</summary>
        private async Task ExecuteToggleStatusAsync()
        {
            if (SelectedItem != null)
            {
                await ToggleHerbStatusAsync(SelectedItem);
            }
        }

        /// <summary>导出药材数据</summary>
        private async Task ExecuteExportAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    // 获取所有药材数据
                    var allHerbsResult = await _herbService.GetPagedAsync(new HerbSearchDto
                    {
                        PageIndex = 1,
                        PageSize = 10000,  // 获取大量数据用于导出
                        Keyword = string.Empty
                    });

                    if (allHerbsResult.IsSuccess && allHerbsResult.Data != null)
                    {
                        var herbs = allHerbsResult.Data.Items;

                        // 定义导出列
                        var columns = new Dictionary<string, string>
                        {
                            { "Name", "药材名称" },
                            { "Origin", "产地" },
                            { "Spec", "规格" },
                            { "Unit", "单位" },
                            { "Price", "单价(元/单位)" },
                            { "Effect", "功效说明" },
                            { "Usage", "用法说明" },
                            { "Status", "状态" },
                            { "CreateTime", "创建时间" }
                        };

                        // 转换数据用于导出
                        var exportData = herbs.Select(h => new
                        {
                            Name = h.Name,
                            Origin = h.Origin ?? string.Empty,
                            Spec = h.Spec ?? string.Empty,
                            Unit = h.Unit,
                            Price = h.Price,
                            Effect = h.Effect ?? string.Empty,
                            Usage = h.Usage ?? string.Empty,
                            Status = h.Status == CommonStatus.Enabled ? "正常" : "禁用",
                            CreateTime = h.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
                        });

                        // 导出到Excel
                        ExcelHelper.ExportToExcel(exportData, columns, saveFileDialog.FileName, "药材数据");

                        await _dialogService.ShowSuccessAsync($"成功导出 {herbs.Count()} 条药材数据到:\n{saveFileDialog.FileName}", "导出成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(allHerbsResult.ErrorMessage ?? "获取药材数据失败", "导出失败");
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"导出药材数据失败: {ex.Message}", "导出失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>导入命令执行</summary>
        private async Task ExecuteImportAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "选择要导入的药材数据文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    // 读取Excel数据
                    var dataTable = ExcelHelper.ImportFromExcel(openFileDialog.FileName, true);

                    if (dataTable.Rows.Count == 0)
                    {
                        await _dialogService.ShowWarningAsync("Excel文件中没有找到数据", "导入提示");
                        return;
                    }

                    int successCount = 0;
                    int failCount = 0;
                    var errors = new List<string>();

                    // 处理每行数据
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        try
                        {
                            var row = dataTable.Rows[i];

                            // 验证必填字段
                            var name = row["药材名称"]?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(name))
                            {
                                errors.Add($"第{i + 2}行：药材名称不能为空");
                                failCount++;
                                continue;
                            }

                            // 创建药材DTO
                            var herbDto = new HerbCreateDto
                            {
                                Name = name,
                                Origin = row["产地"]?.ToString()?.Trim() ?? string.Empty,
                                Spec = row["规格"]?.ToString()?.Trim() ?? string.Empty,
                                Unit = row["单位"]?.ToString()?.Trim() ?? "克",
                                Price = ParseDecimal(row["单价(元/单位)"]?.ToString()),
                                Effect = row["功效说明"]?.ToString()?.Trim() ?? string.Empty,
                                Usage = row["用法说明"]?.ToString()?.Trim() ?? string.Empty
                            };

                            // 调用API创建药材
                            var result = await _herbService.CreateAsync(herbDto);
                            if (result.IsSuccess)
                            {
                                successCount++;
                            }
                            else
                            {
                                errors.Add($"第{i + 2}行 {name}：{result.ErrorMessage}");
                                failCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"第{i + 2}行：处理数据时发生错误 - {ex.Message}");
                            failCount++;
                        }
                    }

                    // 显示导入结果
                    var message = $"导入完成！\n成功：{successCount} 条\n失败：{failCount} 条";
                    if (errors.Count > 0 && errors.Count <= 10)
                    {
                        message += $"\n\n错误详情:\n{string.Join("\n", errors)}";
                    }
                    else if (errors.Count > 10)
                    {
                        message += $"\n\n错误详情（前10条）:\n{string.Join("\n", errors.Take(10))}\n... 等其他{errors.Count - 10}条错误";
                    }

                    if (failCount == 0)
                    {
                        await _dialogService.ShowSuccessAsync(message, "导入成功");
                    }
                    else
                    {
                        await _dialogService.ShowWarningAsync(message, "导入完成");
                    }

                    // 刷新数据
                    if (successCount > 0)
                    {
                        await OnRefreshAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"导入药材数据失败: {ex.Message}", "导入失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>导出模板命令执行</summary>
        private async Task ExecuteExportTemplateAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = "药材数据导入模板.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 定义模板列
                    var columns = new[] { "药材名称", "产地", "规格", "单位", "单价(元/单位)", "功效说明", "用法说明" };

                    // 创建示例数据
                    var sampleData = new List<string[]>
                    {
                        new[] { "人参", "长白山", "统片", "克", "8.5", "大补元气，复脉固脱", "煎汤，3-9g" },
                        new[] { "当归", "甘肃岷县", "全当归", "克", "2.3", "补血活血，调经止痛", "煎汤，6-12g" }
                    };

                    // 创建Excel模板
                    ExcelHelper.CreateTemplate(columns, saveFileDialog.FileName, "药材数据", sampleData);

                    await _dialogService.ShowSuccessAsync($"模板文件已保存到:\n{saveFileDialog.FileName}\n\n请按照模板格式填写药材数据，然后使用导入功能。", "模板下载成功");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"下载模板失败: {ex.Message}", "下载失败");
            }
        }

        /// <summary>解析价格字符串</summary>
        private decimal ParseDecimal(string? priceStr)
        {
            if (string.IsNullOrEmpty(priceStr))
            {
                return 0;
            }

            // 移除可能的货币符号和单位
            priceStr = priceStr.Trim()
                .Replace("元", string.Empty)
                .Replace("￥", string.Empty)
                .Replace("$", string.Empty)
                .Replace("/克", string.Empty)
                .Replace("/g", string.Empty)
                .Replace("克", string.Empty);

            if (decimal.TryParse(priceStr, out decimal price) && price >= 0)
            {
                return price;
            }

            return 0;
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

        #endregion Command执行方法
    }
}
