using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using HerbStatus = LYBT.Shared.Models.Enums.HerbStatus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.Windows.Data;

namespace LYBT.WPF.Client.Modules.SystemManagement.Herbs.ViewModels
{
    /// <summary>
    /// 药材管理视图模型
    /// </summary>
    public class HerbManagementViewModel : BindableBase
    {
        private readonly IHerbService _herbService;
        private string _searchKeyword = string.Empty;
        private HerbInfo? _selectedHerb;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private bool _isLoading = false;
        private int _lowStockCount = 0;

        public ObservableCollection<HerbInfo> Herbs { get; }
        public ICollectionView HerbsView { get; }

        // Commands
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand ImportHerbsCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<HerbInfo> EditHerbCommand { get; }
        public DelegateCommand<HerbInfo> DeleteHerbCommand { get; }
        public DelegateCommand<HerbInfo> ManageStockCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }
        public DelegateCommand ExportHerbsCommand { get; }
        public DelegateCommand ExportTemplateCommand { get; }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>选中的药材</summary>
        public HerbInfo? SelectedHerb
        {
            get => _selectedHerb;
            set => SetProperty(ref _selectedHerb, value);
        }

        /// <summary>当前页码</summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>页大小</summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>总记录数</summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>库存不足数量</summary>
        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        /// <summary>状态文本</summary>
        public string StatusText => $"共 {TotalCount} 种药材，第 {CurrentPage} 页，共 {TotalPages} 页";

        /// <summary>总页数</summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        public HerbManagementViewModel(IHerbService herbService)
        {
            _herbService = herbService;
            Herbs = new ObservableCollection<HerbInfo>();
            HerbsView = CollectionViewSource.GetDefaultView(Herbs);

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearch);
            AddHerbCommand = new DelegateCommand(ExecuteAddHerb);
            ImportHerbsCommand = new DelegateCommand(ExecuteImportHerbs);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            EditHerbCommand = new DelegateCommand<HerbInfo>(ExecuteEditHerb);
            DeleteHerbCommand = new DelegateCommand<HerbInfo>(ExecuteDeleteHerb);
            ManageStockCommand = new DelegateCommand<HerbInfo>(ExecuteManageStock);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, CanExecuteFirstPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage, CanExecuteNextPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, CanExecuteLastPage);
            ExportHerbsCommand = new DelegateCommand(ExecuteExportHerbs);
            ExportTemplateCommand = new DelegateCommand(ExecuteExportTemplate);

            // 加载初始数据
            LoadHerbs();
        }

        private async void LoadHerbs()
        {
            IsLoading = true;
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始加载药材列表，搜索关键词: '{SearchKeyword}', 页码: {CurrentPage}");
                
                var request = new HerbPagedQueryDto
                {
                    CurrentPage = CurrentPage,
                    PageSize = PageSize
                };

                // 设置搜索条件
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    request.Name = SearchKeyword;
                }

                System.Diagnostics.Debug.WriteLine($"发送请求: Name={request.Name}, Page={request.CurrentPage}, PageSize={request.PageSize}");
                
                var result = await _herbService.SearchHerbsAsync(request);
                
                System.Diagnostics.Debug.WriteLine($"API返回结果: TotalCount={result.TotalCount}, Items.Count={result.Items.Count}");

                Herbs.Clear();
                foreach (var herb in result.Items)
                {
                    System.Diagnostics.Debug.WriteLine($"添加药材: {herb.Name} - {herb.Id}");
                    Herbs.Add(herb);
                }

                TotalCount = result.TotalCount;
                LowStockCount = Herbs.Count(h => h.Stock < 10);

                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(TotalPages));

                // 更新分页命令状态
                FirstPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
                LastPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载药材列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetPinyin(string herbName)
        {
            // 简单的拼音映射，实际项目中应使用专业的拼音转换库
            var pinyinMap = new Dictionary<string, string>
            {
                { "人参", "RS" }, { "当归", "DG" }, { "黄芪", "HQ" }, { "川芎", "CX" }, { "白术", "BS" },
                { "茯苓", "FL" }, { "甘草", "GC" }, { "熟地黄", "SDH" }, { "白芍", "BS" }, { "枸杞子", "GQZ" }
            };
            return pinyinMap.ContainsKey(herbName) ? pinyinMap[herbName] : herbName.Substring(0, 1);
        }

        private string GetHerbEffect(string herbName)
        {
            var effectMap = new Dictionary<string, string>
            {
                { "人参", "大补元气，复脉固脱，补脾益肺，生津养血，安神益智" },
                { "当归", "补血活血，调经止痛，润肠通便" },
                { "黄芪", "补气升阳，固表止汗，利水消肿，生津养血，行滞通痹，托毒排脓，敛疮生肌" },
                { "川芎", "活血行气，祛风止痛" },
                { "白术", "健脾益气，燥湿利水，止汗，安胎" }
            };
            return effectMap.ContainsKey(herbName) ? effectMap[herbName] : "待完善";
        }

        private void ExecuteSearch()
        {
            System.Diagnostics.Debug.WriteLine($"执行搜索，关键词: '{SearchKeyword}'");
            CurrentPage = 1; // 搜索时重置到第一页
            LoadHerbs();
        }

        private void ExecuteAddHerb()
        {
            try
            {
                var dialog = new Views.AddHerbDialog();
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    // 刷新列表
                    LoadHerbs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开新增药材对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteImportHerbs()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
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
                        MessageBox.Show("Excel文件中没有数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    // 验证列
                    var requiredColumns = new[] { "药材名称*", "单位*", "单价（元）*", "初始库存*" };
                    foreach (var column in requiredColumns)
                    {
                        if (!dataTable.Columns.Contains(column))
                        {
                            MessageBox.Show($"Excel文件缺少必需的列：{column}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                Stock = int.TryParse(row["初始库存*"]?.ToString(), out var stock) ? stock : 0,
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
                            var response = await _herbService.CreateHerbAsync(dto);
                            if (response.IsSuccess)
                            {
                                successCount++;
                            }
                            else
                            {
                                failCount++;
                                errors.Add($"第{dataTable.Rows.IndexOf(row) + 2}行：{response.Message}");
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
                    
                    MessageBox.Show(message, "导入结果", MessageBoxButton.OK, 
                        failCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
                    
                    // 刷新列表
                    if (successCount > 0)
                    {
                        LoadHerbs();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入药材失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteRefresh()
        {
            LoadHerbs();
        }

        private void ExecuteEditHerb(HerbInfo herb)
        {
            if (herb == null) return;
            
            try
            {
                var dialog = new Views.EditHerbDialog();
                dialog.Owner = Application.Current.MainWindow;
                
                // 设置要编辑的药材信息
                var viewModel = dialog.DataContext as ViewModels.EditHerbDialogViewModel;
                viewModel?.SetHerb(herb);
                
                if (dialog.ShowDialog() == true)
                {
                    // 刷新列表
                    LoadHerbs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开编辑药材对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteDeleteHerb(HerbInfo herb)
        {
            if (herb == null) return;
            
            var result = MessageBox.Show($"确定要删除药材 '{herb.Name}' 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _herbService.DeleteHerbAsync(herb.Id);
                    if (response.IsSuccess)
                    {
                        MessageBox.Show("药材删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadHerbs(); // 刷新列表
                    }
                    else
                    {
                        MessageBox.Show($"删除药材失败: {response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除药材失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteManageStock(HerbInfo herb)
        {
            if (herb == null) return;
            
            try
            {
                // TODO: 实现库存管理对话框
                MessageBox.Show($"管理药材 '{herb.Name}' 库存功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开库存管理对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
            LoadHerbs();
        }

        private bool CanExecuteFirstPage()
        {
            return CurrentPage > 1;
        }

        private void ExecutePreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadHerbs();
            }
        }

        private bool CanExecutePreviousPage()
        {
            return CurrentPage > 1;
        }

        private void ExecuteNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadHerbs();
            }
        }

        private bool CanExecuteNextPage()
        {
            return CurrentPage < TotalPages;
        }

        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
            LoadHerbs();
        }

        private bool CanExecuteLastPage()
        {
            return CurrentPage < TotalPages;
        }

        /// <summary>
        /// 转换HerbDto到HerbInfo
        /// </summary>
        private HerbInfo ConvertToHerbInfo(HerbDto dto)
        {
            return new HerbInfo
            {
                Id = dto.Id,
                // Code = dto.Code, // BaseHerbModel中没有Code属性
                Name = dto.Name,
                PinYinCode = dto.PinYinCode,
                // WuBiCode = dto.WuBiCode, // HerbDto中没有WuBiCode属性
                // Alias = dto.Alias, // BaseHerbModel中没有Alias属性
                // Category = dto.Category, // BaseHerbModel中没有Category属性
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Stock = (int)dto.Stock, // 需要转换为int
                // MinStock = dto.MinStock, // BaseHerbModel中没有MinStock属性
                // MaxStock = dto.MaxStock, // BaseHerbModel中没有MaxStock属性
                BatchNo = "",  // 批次号需要从库存记录获取
                ExpireDate = DateTime.Now.AddYears(2),  // 过期日期需要从库存记录获取
                Effect = dto.Effect,
                // Properties = dto.Properties, // BaseHerbModel中没有Properties属性
                // Usage = dto.Usage, // BaseHerbModel中没有Usage属性
                // Contraindications = dto.Contraindications, // BaseHerbModel中没有Contraindications属性
                Status = (HerbStatus)dto.Status, // 需要转换为枚举
                IsActive = dto.IsActive,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                Remark = dto.Remark
            };
        }

        /// <summary>
        /// 执行导出药材
        /// </summary>
        private async void ExecuteExportHerbs()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                    FileName = $"药材列表_{DateTime.Now:yyyyMMdd}.xlsx",
                    DefaultExt = "xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    
                    // 获取所有药材数据（不分页）
                    var request = new HerbPagedQueryDto
                    {
                        CurrentPage = 1,
                        PageSize = int.MaxValue
                    };
                    
                    var result = await _herbService.SearchHerbsAsync(request);
                    
                    // 定义导出列（不包含拼音码和五笔码）
                    var columns = new Dictionary<string, string>
                    {
                        { "Name", "药材名称" },
                        { "Origin", "产地" },
                        { "Spec", "规格" },
                        { "Unit", "单位" },
                        { "Price", "单价（元）" },
                        { "Stock", "库存数量" },
                        { "Effect", "功效说明" },
                        { "Usage", "用法" },
                        { "StatusDescription", "状态" },
                        { "CreateTimeString", "创建时间" },
                        { "Remark", "备注" }
                    };
                    
                    // 准备导出数据
                    var exportData = result.Items.Select(h => new
                    {
                        h.Name,
                        h.Origin,
                        h.Spec,
                        h.Unit,
                        h.Price,
                        h.Stock,
                        h.Effect,
                        h.Usage,
                        StatusDescription = h.Status == HerbStatus.Active ? "正常" : "停用",
                        CreateTimeString = h.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        h.Remark
                    }).ToList();
                    
                    // 导出到Excel
                    Core.Helpers.ExcelHelper.ExportToExcel(exportData, columns, dialog.FileName, "药材列表");
                    
                    MessageBox.Show($"成功导出 {result.TotalCount} 条药材记录", "导出成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出药材失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行导出模板
        /// </summary>
        private void ExecuteExportTemplate()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                    FileName = $"药材导入模板_{DateTime.Now:yyyyMMdd}.xlsx",
                    DefaultExt = "xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 定义模板列（不包含拼音码和五笔码）
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
                    
                    MessageBox.Show("药材导入模板创建成功！\n\n说明：\n1. 带*号的列为必填项\n2. 拼音码和五笔码将在导入时自动生成\n3. 请删除示例数据后再导入实际数据", 
                        "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出模板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}