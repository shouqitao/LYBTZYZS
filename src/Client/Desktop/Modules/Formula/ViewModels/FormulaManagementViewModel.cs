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
using LYBT.Desktop.Core.Helpers;
using Microsoft.Win32;
using System.Data;
using System.Linq;

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
        public DelegateCommand ImportFormulasCommand { get; }
        
        /// <summary>导出验方命令</summary>
        public DelegateCommand ExportFormulasCommand { get; }
        
        /// <summary>导出模板命令</summary>
        public DelegateCommand ExportTemplateCommand { get; }
        
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
            ImportFormulasCommand = new DelegateCommand(async () => await ExecuteImportAsync(), () => !IsLoading);
            ExportFormulasCommand = new DelegateCommand(async () => await ExecuteExportAsync(), () => !IsLoading);
            ExportTemplateCommand = new DelegateCommand(async () => await ExecuteExportTemplateAsync(), () => !IsLoading);
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

        /// <summary>导出数据（重写基类方法，调用Excel导出）</summary>
        protected override async Task OnExportAsync()
        {
            await ExecuteExportAsync();
        }

        /// <summary>更新Command状态</summary>
        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            ToggleStatusCommand.RaiseCanExecuteChanged();
            CopyCommand.RaiseCanExecuteChanged();
            ImportFormulasCommand.RaiseCanExecuteChanged();
            ExportFormulasCommand.RaiseCanExecuteChanged();
            ExportTemplateCommand.RaiseCanExecuteChanged();
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

        /// <summary>导出验方数据</summary>
        private async Task ExecuteExportAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"验方数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    // 获取所有验方数据
                    var allFormulasResult = await _formulaService.GetPagedAsync(new FormulaQueryDto
                    {
                        PageIndex = 1,
                        PageSize = 10000,  // 获取大量数据用于导出
                        Keyword = string.Empty
                    });

                    if (allFormulasResult.IsSuccess && allFormulasResult.Data != null)
                    {
                        var formulas = allFormulasResult.Data.Items;
                        
                        // 定义导出列
                        var columns = new Dictionary<string, string>
                        {
                            { "Name", "验方名称" },
                            { "Category", "分类" },
                            { "Effect", "功效" },
                            { "Usage", "用法" },
                            { "Indications", "主治症状" },
                            { "Contraindications", "禁忌症" },
                            { "HerbNames", "药材组成" },
                            { "IsShared", "是否共享" },
                            { "Status", "状态" },
                            { "CreateTime", "创建时间" }
                        };

                        // 转换数据用于导出
                        var exportData = formulas.Select(f => new
                        {
                            Name = f.Name,
                            Category = f.Category ?? "未分类",
                            Effect = f.Effect ?? "",
                            Usage = f.Usage ?? "",
                            Indications = f.Indications ?? "",
                            Contraindications = f.Contraindications ?? "",
                            HerbNames = f.GetHerbNamesList(20),
                            IsShared = f.IsShared ? "是" : "否",
                            Status = f.Status == CommonStatus.Enabled ? "正常" : "禁用",
                            CreateTime = f.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
                        });

                        // 导出到Excel
                        ExcelHelper.ExportToExcel(exportData, columns, saveFileDialog.FileName, "验方数据");
                        
                        await _dialogService.ShowSuccessAsync($"成功导出 {formulas.Count()} 条验方数据到:\n{saveFileDialog.FileName}", "导出成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(allFormulasResult.ErrorMessage ?? "获取验方数据失败", "导出失败");
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"导出验方数据失败: {ex.Message}", "导出失败");
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
                    Title = "选择要导入的验方数据文件"
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
                            var name = row["验方名称"]?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(name))
                            {
                                errors.Add($"第{i + 2}行：验方名称不能为空");
                                failCount++;
                                continue;
                            }

                            // 创建验方DTO（简化版，不包含药材组成）
                            var formulaDto = new FormulaCreateDto
                            {
                                Name = name,
                                Effect = row["功效"]?.ToString()?.Trim() ?? "",
                                Usage = row["用法"]?.ToString()?.Trim() ?? "",
                                Indications = row["主治症状"]?.ToString()?.Trim() ?? "",
                                Contraindications = row["禁忌症"]?.ToString()?.Trim() ?? "",
                                IsShared = ParseBoolean(row["是否共享"]?.ToString()),
                                Herbs = new List<FormulaHerbItemCreateDto>() // 空药材列表，需要后续手动添加
                            };

                            // 调用API创建验方
                            var result = await _formulaService.CreateAsync(formulaDto);
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

                    message += "\n\n注意：导入的验方暂无药材组成，请手动添加药材。";

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
                await _dialogService.ShowErrorAsync($"导入验方数据失败: {ex.Message}", "导入失败");
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
                    FileName = "验方数据导入模板.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 定义模板列
                    var columns = new[] { "验方名称", "功效", "用法", "主治症状", "禁忌症", "是否共享" };
                    
                    // 创建示例数据
                    var sampleData = new List<string[]>
                    {
                        new[] { "桂枝汤", "解肌发表，调和营卫", "水煎服，日二服", "外感风寒，营卫不和", "热病及阴虚内热者忌用", "是" },
                        new[] { "麻黄汤", "发汗解表，宣肺平喘", "水煎服，温服", "外感风寒表实证", "表虚有汗者忌用", "是" }
                    };

                    // 创建Excel模板
                    ExcelHelper.CreateTemplate(columns, saveFileDialog.FileName, "验方数据", sampleData);
                    
                    await _dialogService.ShowSuccessAsync($"模板文件已保存到:\n{saveFileDialog.FileName}\n\n请按照模板格式填写验方数据，然后使用导入功能。\n\n注意：导入后需手动添加具体药材组成。", "模板下载成功");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"下载模板失败: {ex.Message}", "下载失败");
            }
        }

        /// <summary>解析布尔值字符串</summary>
        private bool ParseBoolean(string? boolStr)
        {
            if (string.IsNullOrEmpty(boolStr)) return false;
            
            boolStr = boolStr.Trim().ToLower();
            return boolStr == "是" || boolStr == "true" || boolStr == "1" || boolStr == "yes";
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