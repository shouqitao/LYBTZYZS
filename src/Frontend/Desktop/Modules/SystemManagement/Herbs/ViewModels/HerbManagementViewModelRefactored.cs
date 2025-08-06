using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Prism.Commands;

using LYBT.WPF.Client.Core.Interfaces.Services;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.SystemManagement.Herbs.ViewModels
{
    /// <summary>
    /// 中药材管理视图模型（重构版）
    /// </summary>
    public class HerbManagementViewModelRefactored : BaseManagementViewModel<HerbInfo, IHerbApiService>
    {
        private readonly ICommonDialogService _commonDialogService;
        private readonly IDialogService _dialogService;

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
        public DelegateCommand<HerbInfo> ManageStockCommand { get; }

        #endregion

        public HerbManagementViewModelRefactored(IHerbApiService service,
            ICommonDialogService commonDialogService,
            IDialogService dialogService)
            : base(service)
        {
            _commonDialogService = commonDialogService;
            _dialogService = dialogService;
            // 初始化额外的命令
            ImportHerbsCommand = new DelegateCommand(async () => await ImportHerbs());
            ExportTemplateCommand = new DelegateCommand(ExportTemplate);
            ManageStockCommand = new DelegateCommand<HerbInfo>(ManageStock);
        }

        #region 重写基类方法

        protected override async Task<ServiceResult<PagedResult<HerbInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                var query = new HerbPagedQueryDto
                {
                    CurrentPage = request.CurrentPage,
                    PageSize = request.PageSize,
                    Name = SearchKeyword
                };

                var response = await Service.GetPagedHerbsAsync(query);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var paginatedResult = response.Content;
                    
                    // 转换为前端模型
                    var herbInfos = paginatedResult.Items.Select(dto => ConvertToHerbInfo(dto)).ToList();
                    
                    // 更新库存不足数量
                    LowStockCount = herbInfos.Count(h => h.Stock < 10);

                    var result = new PagedResult<HerbInfo>
                    {
                        Items = herbInfos,
                        TotalCount = paginatedResult.TotalCount,
                        CurrentPage = paginatedResult.CurrentPage,
                        PageSize = paginatedResult.PageSize
                    };

                    return ServiceResult<PagedResult<HerbInfo>>.Success(result);
                }
                else
                {
                    var error = response.Error?.Content ?? "获取药材列表失败";
                    return ServiceResult<PagedResult<HerbInfo>>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载药材列表异常: {ex.Message}");
                return ServiceResult<PagedResult<HerbInfo>>.Failure($"加载药材列表失败: {ex.Message}");
            }
        }

        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(HerbInfo item)
        {
            try
            {
                var response = await Service.DeleteHerbAsync(item.Id);
                if (response.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    var error = response.Error?.Content ?? "删除药材失败";
                    return ServiceResult<bool>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除药材失败: {ex.Message}");
            }
        }

        protected override string GetItemDisplayName(HerbInfo item)
        {
            return item.Name ?? string.Empty;
        }

        protected override void ExecuteAdd()
        {
            try
            {
                var dialog = new Views.AddHerbDialog();
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开新增药材对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override void ExecuteEdit(HerbInfo item)
        {
            if (item == null) return;

            try
            {
                var dialog = new Views.EditHerbDialog();
                dialog.Owner = Application.Current.MainWindow;
                
                // 设置要编辑的药材信息
                var viewModel = dialog.DataContext as ViewModels.EditHerbDialogViewModel;
                viewModel?.SetHerb(item);
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开编辑药材对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override void ExecuteView(HerbInfo item)
        {
            if (item == null) return;

            try
            {
                var parameters = new DialogParameters
                {
                    { "herbId", item.Id }
                };

                _dialogService.ShowDialog("ViewHerbDialog", parameters, result =>
                {
                    // 如果返回了编辑参数，执行编辑
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("herb"))
                    {
                        var herbToEdit = result.Parameters.GetValue<HerbInfo>("herb");
                        ExecuteEdit(herbToEdit);
                    }
                });
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开查看药材对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion

        #region 额外功能

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
                        _commonDialogService.ShowWarningAsync("Excel文件中没有数据", "提示").GetAwaiter().GetResult();
                        return;
                    }
                    
                    // 验证列
                    var requiredColumns = new[] { "药材名称*", "单位*", "单价（元）*", "初始库存*" };
                    foreach (var column in requiredColumns)
                    {
                        if (!dataTable.Columns.Contains(column))
                        {
                            _commonDialogService.ShowErrorAsync($"Excel文件缺少必需的列：{column}", "错误").GetAwaiter().GetResult();
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
                                /* Stock = int.TryParse(row["初始库存*"]?.ToString(), */ out var stock) ? stock : 0,
                                Effect = row.Table.Columns.Contains("功效说明") ? row["功效说明"]?.ToString()?.Trim() : null,
                                Usage = row.Table.Columns.Contains("用法") ? row["用法"]?.ToString()?.Trim() : null,
                                Remark = row.Table.Columns.Contains("备注") ? row["备注"]?.ToString()?.Trim() : null,
                                Status = HerbStatus.Active
                            };
                            
                            // 验证数据
                            if (dto.Price <= 0)
                            {
                                failCount++;
                                errors.Add($"第{dataTable.Rows.IndexOf(row) + 2}行：单价必须大于0");
                                continue;
                            }
                            
                            if (dto.Stock < 0)
                            {
                                failCount++;
                                errors.Add($"第{dataTable.Rows.IndexOf(row) + 2}行：库存不能为负数");
                                continue;
                            }
                            
                            // 调用服务创建药材
                            var response = await Service.CreateHerbAsync(dto);
                            if (response.IsSuccessStatusCode)
                            {
                                successCount++;
                            }
                            else
                            {
                                failCount++;
                                errors.Add($"第{dataTable.Rows.IndexOf(row) + 2}行：{response.Error?.Content ?? "创建失败"}");
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
                    
                    _commonDialogService.ShowWarningAsync(message, "导入结果").GetAwaiter().GetResult();
                    
                    // 刷新列表
                    if (successCount > 0)
                    {
                        RefreshCommand.Execute();
                    }
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"导入药材失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

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
                        "初始库存*",
                        "功效说明",
                        "用法",
                        "备注"
                    };
                    
                    // 添加示例数据
                    var sampleData = new List<string[]>
                    {
                        new[] { "人参", "吉林", "优质", "克", "100.00", "500", "大补元气，复脉固脱", "煎服，3-9g", "示例数据，导入时请删除" },
                        new[] { "当归", "甘肃", "特级", "克", "50.00", "1000", "补血活血，调经止痛", "煎服，6-12g", "示例数据，导入时请删除" }
                    };
                    
                    // 创建模板
                    Core.Helpers.ExcelHelper.CreateTemplate(columns, dialog.FileName, "药材导入模板", sampleData);
                    
                    _commonDialogService.ShowInformationAsync("药材导入模板创建成功！\n\n说明：\n1. 带*号的列为必填项\n2. 拼音码和五笔码将在导入时自动生成\n3. 请删除示例数据后再导入实际数据", "导出成功").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"导出模板失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ManageStock(HerbInfo herb)
        {
            if (herb == null) return;
            
            try
            {
                // 创建ViewModel并传入Service依赖
                var viewModel = new StockManagementDialogViewModel(Service, _commonDialogService);
                viewModel.SetHerb(herb);
                
                // 创建对话框并设置ViewModel
                var dialog = new Views.StockManagementDialog();
                dialog.DataContext = viewModel;
                dialog.Owner = Application.Current.MainWindow;
                
                // 设置关闭回调
                viewModel.CloseDialogCallback = () => dialog.Close();
                viewModel.SaveCompleteCallback = (success) =>
                {
                    if (success)
                    {
                        // 刷新列表
                        RefreshCommand.Execute();
                        // 更新库存预警计数
                        UpdateLowStockCount();
                    }
                };

                void UpdateLowStockCount()
                {
                    // 计算库存不足的药材数量（暂定小于10为库存不足）
                    LowStockCount = Items.Count(item => item.Stock < 10);
                }

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开库存管理对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion

        #region 辅助方法

        private HerbInfo ConvertToHerbInfo(HerbDto dto)
        {
            return new HerbInfo
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                PinYinCode = dto.PinYinCode,
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit ?? "克",
                Price = dto.Price,
                /* Stock = (int)dto.Stock, */
                /* BatchNo = "", */  // 批次号需要从库存记录获取
                ExpireDate = DateTime.Now.AddYears(2),  // 过期日期需要从库存记录获取
                Effect = dto.Effect,
                Status = (HerbStatus)dto.Status,
                IsActive = dto.IsActive,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                Remark = dto.Remark
            };
        }

        #endregion
    }
}