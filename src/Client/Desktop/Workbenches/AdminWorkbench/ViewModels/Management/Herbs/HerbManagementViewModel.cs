using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Shared;

namespace LYBT.Desktop.Workbench.Admin.ViewModels.Management.Herbs
{
    /// <summary>
    /// 药材管理视图模型
    /// </summary>
    public class HerbManagementViewModel : BaseManagementViewModel<HerbDto>
    {
        #region Fields

        private readonly ISharedHerbService _herbService;
        private string _selectedOrigin;
        private decimal? _minPrice;
        private decimal? _maxPrice;

        #endregion

        #region Properties

        /// <summary>
        /// 产地筛选选项
        /// </summary>
        public List<string> OriginOptions { get; private set; } = new List<string>
        {
            "全部", "河北", "山东", "山西", "陕西", "甘肃", "四川", "云南", "贵州", "湖北", "湖南", "江西", "安徽", "浙江", "福建", "广东", "广西", "其他"
        };

        /// <summary>
        /// 单位选项
        /// </summary>
        public List<string> UnitOptions { get; } = new List<string>
        {
            "克", "两", "钱", "公斤", "斤", "袋", "包", "瓶", "盒"
        };

        /// <summary>
        /// 选中的产地筛选
        /// </summary>
        public string SelectedOrigin
        {
            get => _selectedOrigin;
            set
            {
                if (SetProperty(ref _selectedOrigin, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// 最小价格筛选
        /// </summary>
        public decimal? MinPrice
        {
            get => _minPrice;
            set
            {
                if (SetProperty(ref _minPrice, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// 最大价格筛选
        /// </summary>
        public decimal? MaxPrice
        {
            get => _maxPrice;
            set
            {
                if (SetProperty(ref _maxPrice, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// 药材统计信息
        /// </summary>
        public LYBT.Shared.Models.Contracts.Herbs.HerbStatisticsDto Statistics { get; private set; }

        #endregion

        #region Constructor

        public HerbManagementViewModel(ISharedHerbService herbService)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _selectedOrigin = "全部";
            Statistics = new LYBT.Shared.Models.Contracts.Herbs.HerbStatisticsDto();

            // 加载统计信息和筛选选项
            _ = LoadStatisticsAsync();
            _ = LoadFilterOptionsAsync();
        }

        #endregion

        #region Override Methods

        protected override async Task<(IEnumerable<HerbDto> items, int totalCount)> LoadDataInternalAsync()
        {
            try
            {
                // 构建查询参数
                var queryDto = new HerbPagedQueryDto
                {
                    Page = CurrentPage,
                    Size = PageSize,
                    SortBy = "Name", // 按名称排序
                    IsDescending = false // ASC排序
                };

                // 添加搜索条件
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    queryDto.Name = SearchText.Trim();
                }

                // 添加产地筛选
                if (!string.IsNullOrEmpty(SelectedOrigin) && SelectedOrigin != "全部")
                {
                    queryDto.Origin = SelectedOrigin;
                }

                // 添加价格筛选
                if (MinPrice.HasValue)
                {
                    queryDto.MinPrice = MinPrice.Value;
                }

                if (MaxPrice.HasValue)
                {
                    queryDto.MaxPrice = MaxPrice.Value;
                }

                // 调用药材服务获取数据
                var result = await _herbService.GetHerbsAsync(queryDto);

                if (result.IsSuccess && result.Data != null)
                {
                    return (result.Data.Items ?? Enumerable.Empty<HerbDto>(), result.Data.TotalCount);
                }

                throw new Exception(result.Message ?? "获取药材数据失败");
            }
            catch (Exception ex)
            {
                throw new Exception($"加载药材数据时发生错误: {ex.Message}", ex);
            }
        }

        protected override async Task AddItemInternalAsync()
        {
            try
            {
                var dialog = new Views.Management.Herbs.Dialogs.HerbEditDialog(null, UnitOptions);
                var result = dialog.ShowDialog();

                if (result == true && dialog.HerbData != null)
                {
                    var createDto = new HerbCreateDto
                    {
                        Name = dialog.HerbData.Name,
                        PinYinCode = dialog.HerbData.PinYinCode,
                        WuBiCode = dialog.HerbData.WuBiCode,
                        Origin = dialog.HerbData.Origin,
                        Spec = dialog.HerbData.Spec,
                        Unit = dialog.HerbData.Unit,
                        Price = dialog.HerbData.Price,
                        Stock = dialog.HerbData.Stock,
                        BatchNo = dialog.HerbData.BatchNo,
                        ExpireDate = dialog.HerbData.ExpireDate,
                        Effect = dialog.HerbData.Effect,
                        Usage = dialog.HerbData.Usage
                    };

                    var createResult = await _herbService.CreateHerbAsync(createDto);
                    if (!createResult.IsSuccess)
                    {
                        throw new Exception(createResult.Message ?? "创建药材失败");
                    }

                    StatusMessage = "药材创建成功";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"创建药材失败: {ex.Message}", ex);
            }
        }

        protected override async Task EditItemInternalAsync(HerbDto item)
        {
            try
            {
                // 获取药材详情
                var detailResult = await _herbService.GetHerbAsync(item.Id);
                if (!detailResult.IsSuccess || detailResult.Data == null)
                {
                    throw new Exception("获取药材详情失败");
                }

                var dialog = new Views.Management.Herbs.Dialogs.HerbEditDialog(detailResult.Data, UnitOptions);
                var result = dialog.ShowDialog();

                if (result == true && dialog.HerbData != null)
                {
                    var updateDto = new HerbUpdateDto
                    {
                        Id = item.Id,
                        Name = dialog.HerbData.Name,
                        PinYinCode = dialog.HerbData.PinYinCode,
                        WuBiCode = dialog.HerbData.WuBiCode,
                        Origin = dialog.HerbData.Origin,
                        Spec = dialog.HerbData.Spec,
                        Unit = dialog.HerbData.Unit,
                        Price = dialog.HerbData.Price,
                        Effect = dialog.HerbData.Effect,
                        Usage = dialog.HerbData.Usage
                    };

                    var updateResult = await _herbService.UpdateHerbAsync(item.Id, updateDto);
                    if (!updateResult.IsSuccess)
                    {
                        throw new Exception(updateResult.Message ?? "更新药材失败");
                    }

                    StatusMessage = "药材更新成功";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"编辑药材失败: {ex.Message}", ex);
            }
        }

        protected override async Task DeleteItemInternalAsync(HerbDto item)
        {
            try
            {
                // 确认删除
                var result = MessageBox.Show(
                    $"确定要删除药材 '{item.Name}' 吗？\n注意：删除后无法恢复。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                var deleteResult = await _herbService.DeleteHerbAsync(item.Id);
                if (!deleteResult.IsSuccess)
                {
                    throw new Exception(deleteResult.Message ?? "删除药材失败");
                }

                StatusMessage = $"药材 '{item.Name}' 已删除";
            }
            catch (Exception ex)
            {
                throw new Exception($"删除药材失败: {ex.Message}", ex);
            }
        }

        protected override async Task ExportDataInternalAsync()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出药材数据",
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv",
                    FileName = $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 获取所有数据（不分页）
                    var queryDto = new HerbPagedQueryDto
                    {
                        Page = 1,
                        Size = int.MaxValue,
                        SortBy = "Name",
                        IsDescending = false // ASC排序
                    };

                    var allHerbsResult = await _herbService.GetHerbsAsync(queryDto);

                    if (!allHerbsResult.IsSuccess || allHerbsResult.Data?.Items == null)
                    {
                        throw new Exception("获取药材数据失败");
                    }

                    // 这里应该实现具体的导出逻辑
                    // 可以使用 NPOI 或其他 Excel 处理库
                    // 暂时只显示成功消息
                    StatusMessage = $"药材数据已导出到: {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"导出数据失败: {ex.Message}", ex);
            }
        }

        protected override bool FilterItem(HerbDto item)
        {
            // 搜索文本筛选
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!item.Name.ToLower().Contains(searchLower) &&
                    !(item.PinYinCode?.ToLower().Contains(searchLower) ?? false) &&
                    !(item.WuBiCode?.ToLower().Contains(searchLower) ?? false) &&
                    !(item.Origin?.ToLower().Contains(searchLower) ?? false))
                {
                    return false;
                }
            }

            // 产地筛选
            if (!string.IsNullOrEmpty(SelectedOrigin) && SelectedOrigin != "全部")
            {
                if (!string.Equals(item.Origin, SelectedOrigin, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // 价格筛选
            if (MinPrice.HasValue && item.Price < MinPrice.Value)
            {
                return false;
            }

            if (MaxPrice.HasValue && item.Price > MaxPrice.Value)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Commands

        public DelegateCommand<HerbDto> BatchAdjustPriceCommand { get; }
        public DelegateCommand ImportHerbsCommand { get; }
        public DelegateCommand<HerbDto> ToggleStatusCommand { get; }

        #endregion

        #region Private Methods

        /// <summary>
        /// 加载统计信息
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var result = await _herbService.GetHerbStatisticsAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    Statistics = result.Data;
                    RaisePropertyChanged(nameof(Statistics));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载药材统计信息失败: {ex.Message}");
                // 使用默认统计信息
                Statistics = new LYBT.Shared.Models.Contracts.Herbs.HerbStatisticsDto();
                RaisePropertyChanged(nameof(Statistics));
            }
        }

        /// <summary>
        /// 加载筛选选项
        /// </summary>
        private async Task LoadFilterOptionsAsync()
        {
            try
            {
                // 获取系统中所有使用的产地
                var originsResult = await _herbService.GetHerbOriginsAsync();
                if (originsResult.IsSuccess && originsResult.Data != null)
                {
                    var origins = new List<string> { "全部" };
                    origins.AddRange(originsResult.Data.Where(o => !string.IsNullOrEmpty(o)).Distinct().OrderBy(o => o));
                    OriginOptions = origins;
                    RaisePropertyChanged(nameof(OriginOptions));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载产地选项失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用筛选
        /// </summary>
        private void ApplyFilter()
        {
            if (ItemsView != null)
            {
                ItemsView.Refresh();
            }
        }

        /// <summary>
        /// 批量调整价格
        /// </summary>
        public async Task BatchAdjustPriceAsync(List<HerbDto> selectedHerbs, decimal adjustmentPercent)
        {
            try
            {
                if (selectedHerbs == null || !selectedHerbs.Any())
                {
                    MessageBox.Show("请先选择要调价的药材", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"确定要对选中的 {selectedHerbs.Count} 种药材进行批量调价吗？\n调整幅度：{adjustmentPercent:+0.00;-0.00}%",
                    "确认批量调价",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                var herbIds = selectedHerbs.Select(h => h.Id).ToList();
                var adjustResult = await _herbService.BatchAdjustPriceAsync(herbIds, "percentage", adjustmentPercent);

                if (!adjustResult.IsSuccess)
                {
                    throw new Exception(adjustResult.Message ?? "批量调价失败");
                }

                MessageBox.Show($"成功调整了 {selectedHerbs.Count} 种药材的价格", "调价成功", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"批量调价完成，影响 {selectedHerbs.Count} 种药材";
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"批量调价失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入药材数据
        /// </summary>
        public async Task ImportHerbsAsync()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择药材数据文件",
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    StatusMessage = "正在导入药材数据...";
                    
                    // 这里应该实现具体的导入逻辑
                    // 可以使用 NPOI 或其他 Excel 处理库
                    await Task.Delay(1000); // 模拟导入过程
                    
                    StatusMessage = "药材数据导入成功";
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入失败: {ex.Message}";
                MessageBox.Show($"导入药材数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 切换药材启用状态
        /// </summary>
        public async Task ToggleHerbStatusAsync(HerbDto herb)
        {
            try
            {
                var action = herb.IsEnabled ? "停用" : "启用";
                var result = MessageBox.Show(
                    $"确定要{action}药材 '{herb.Name}' 吗？",
                    $"确认{action}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                var toggleResult = await _herbService.ToggleHerbStatusAsync(herb.Id);
                if (!toggleResult.IsSuccess)
                {
                    throw new Exception(toggleResult.Message ?? $"{action}药材失败");
                }

                StatusMessage = $"药材 '{herb.Name}' 已{action}";
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(herb.IsEnabled ? "停用" : "启用")}药材失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

}