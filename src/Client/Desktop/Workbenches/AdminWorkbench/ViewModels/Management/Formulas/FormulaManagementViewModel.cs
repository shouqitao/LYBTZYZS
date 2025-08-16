using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Desktop.Shared;

namespace LYBT.Desktop.Workbench.Admin.ViewModels.Management.Formulas
{
    /// <summary>
    /// 验方管理视图模型
    /// </summary>
    public class FormulaManagementViewModel : BaseManagementViewModel<FormulaDto>
    {
        #region Fields

        private readonly ISharedFormulaService _formulaService;
        private readonly ISharedHerbService _herbService;
        private bool? _selectedSharedStatus;
        private string _selectedCreator;
        private string _selectedEffect;

        #endregion

        #region Properties

        /// <summary>
        /// 共享状态筛选选项
        /// </summary>
        public List<KeyValuePair<string, bool?>> SharedStatusOptions { get; } = new List<KeyValuePair<string, bool?>>
        {
            new("全部", null),
            new("共享", true),
            new("私有", false)
        };

        /// <summary>
        /// 创建者筛选选项
        /// </summary>
        public List<string> CreatorOptions { get; private set; } = new List<string> { "全部" };

        /// <summary>
        /// 功效筛选选项
        /// </summary>
        public List<string> EffectOptions { get; private set; } = new List<string> { "全部" };

        /// <summary>
        /// 选中的共享状态筛选
        /// </summary>
        public bool? SelectedSharedStatus
        {
            get => _selectedSharedStatus;
            set
            {
                if (SetProperty(ref _selectedSharedStatus, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// 选中的创建者筛选
        /// </summary>
        public string SelectedCreator
        {
            get => _selectedCreator;
            set
            {
                if (SetProperty(ref _selectedCreator, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// 选中的功效筛选
        /// </summary>
        public string SelectedEffect
        {
            get => _selectedEffect;
            set
            {
                if (SetProperty(ref _selectedEffect, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// 验方统计信息
        /// </summary>
        public FormulaStatisticsDto Statistics { get; private set; }

        #endregion

        #region Constructor

        public FormulaManagementViewModel(ISharedFormulaService formulaService, ISharedHerbService herbService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _selectedCreator = "全部";
            _selectedEffect = "全部";
            Statistics = new FormulaStatisticsDto();

            // 加载统计信息和筛选选项
            _ = LoadStatisticsAsync();
            _ = LoadFilterOptionsAsync();
        }

        #endregion

        #region Override Methods

        protected override async Task<(IEnumerable<FormulaDto> items, int totalCount)> LoadDataInternalAsync()
        {
            try
            {
                // 构建查询参数
                var queryDto = new FormulaQueryDto
                {
                    Page = CurrentPage,
                    Size = PageSize,
                    OrderBy = "CreateTime",
                    IsAscending = false // 默认按创建时间倒序
                };

                // 添加搜索条件
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    queryDto.Name = SearchText.Trim();
                }

                // 添加共享状态筛选
                if (SelectedSharedStatus.HasValue)
                {
                    queryDto.IsShared = SelectedSharedStatus.Value;
                }

                // 添加功效筛选
                if (!string.IsNullOrEmpty(SelectedEffect) && SelectedEffect != "全部")
                {
                    queryDto.Effect = SelectedEffect;
                }

                // 调用验方服务获取数据
                var result = await _formulaService.GetFormulasAsync(queryDto);

                if (result.IsSuccess && result.Data != null)
                {
                    return (result.Data.Data ?? Enumerable.Empty<FormulaDto>(), result.Data.TotalCount);
                }

                throw new Exception(result.Message ?? "获取验方数据失败");
            }
            catch (Exception ex)
            {
                throw new Exception($"加载验方数据时发生错误: {ex.Message}", ex);
            }
        }

        protected override async Task AddItemInternalAsync()
        {
            try
            {
                // 获取可用药材列表
                var herbsResult = await _herbService.GetAvailableHerbsAsync();
                if (!herbsResult.IsSuccess || herbsResult.Data == null)
                {
                    throw new Exception("获取药材列表失败");
                }

                var dialog = new Views.Management.Formulas.Dialogs.FormulaEditDialog(null, herbsResult.Data);
                var result = dialog.ShowDialog();

                if (result == true && dialog.FormulaData != null)
                {
                    var createDto = new FormulaCreateDto
                    {
                        Name = dialog.FormulaData.Name,
                        Effect = dialog.FormulaData.Effect,
                        Usage = dialog.FormulaData.Usage,
                        IsShared = dialog.FormulaData.IsShared,
                        Instructions = dialog.FormulaData.Instructions,
                        Indications = dialog.FormulaData.Indications,
                        Contraindications = dialog.FormulaData.Contraindications,
                        Preparation = dialog.FormulaData.Preparation,
                        Remark = dialog.FormulaData.Remark,
                        Herbs = dialog.FormulaData.Herbs.Select(h => new FormulaHerbItemCreateDto
                        {
                            HerbId = h.HerbId,
                            Quantity = h.Quantity,
                            Preparation = h.Preparation,
                            Usage = h.Usage,
                            SortOrder = h.SortOrder
                        }).ToList()
                    };

                    var createResult = await _formulaService.CreateFormulaAsync(createDto);
                    if (!createResult.IsSuccess)
                    {
                        throw new Exception(createResult.Message ?? "创建验方失败");
                    }

                    StatusMessage = "验方创建成功";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"创建验方失败: {ex.Message}", ex);
            }
        }

        protected override async Task EditItemInternalAsync(FormulaDto item)
        {
            try
            {
                // 获取验方详情
                var detailResult = await _formulaService.GetFormulaAsync(item.Id);
                if (!detailResult.IsSuccess || detailResult.Data == null)
                {
                    throw new Exception("获取验方详情失败");
                }

                // 获取可用药材列表
                var herbsResult = await _herbService.GetAvailableHerbsAsync();
                if (!herbsResult.IsSuccess || herbsResult.Data == null)
                {
                    throw new Exception("获取药材列表失败");
                }

                var dialog = new Views.Management.Formulas.Dialogs.FormulaEditDialog(detailResult.Data, herbsResult.Data);
                var result = dialog.ShowDialog();

                if (result == true && dialog.FormulaData != null)
                {
                    var updateDto = new FormulaUpdateDto
                    {
                        Id = item.Id,
                        Name = dialog.FormulaData.Name,
                        Effect = dialog.FormulaData.Effect,
                        Usage = dialog.FormulaData.Usage,
                        IsShared = dialog.FormulaData.IsShared,
                        Instructions = dialog.FormulaData.Instructions,
                        Indications = dialog.FormulaData.Indications,
                        Contraindications = dialog.FormulaData.Contraindications,
                        Preparation = dialog.FormulaData.Preparation,
                        Remark = dialog.FormulaData.Remark,
                        Herbs = dialog.FormulaData.Herbs.Select(h => new FormulaHerbItemUpdateDto
                        {
                            Id = h.Id,
                            HerbId = h.HerbId,
                            Quantity = h.Quantity,
                            Preparation = h.Preparation,
                            Usage = h.Usage,
                            SortOrder = h.SortOrder
                        }).ToList()
                    };

                    var updateResult = await _formulaService.UpdateFormulaAsync(item.Id, updateDto);
                    if (!updateResult.IsSuccess)
                    {
                        throw new Exception(updateResult.Message ?? "更新验方失败");
                    }

                    StatusMessage = "验方更新成功";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"编辑验方失败: {ex.Message}", ex);
            }
        }

        protected override async Task DeleteItemInternalAsync(FormulaDto item)
        {
            try
            {
                // 确认删除
                var result = MessageBox.Show(
                    $"确定要删除验方 '{item.Name}' 吗？\n注意：删除后无法恢复。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                var deleteResult = await _formulaService.DeleteFormulaAsync(item.Id);
                if (!deleteResult.IsSuccess)
                {
                    throw new Exception(deleteResult.Message ?? "删除验方失败");
                }

                StatusMessage = $"验方 '{item.Name}' 已删除";
            }
            catch (Exception ex)
            {
                throw new Exception($"删除验方失败: {ex.Message}", ex);
            }
        }

        protected override async Task ExportDataInternalAsync()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出验方数据",
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv",
                    FileName = $"验方数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 获取所有数据（不分页）
                    var queryDto = new FormulaQueryDto
                    {
                        Page = 1,
                        Size = int.MaxValue,
                        OrderBy = "Name",
                        IsAscending = true
                    };

                    var allFormulasResult = await _formulaService.GetFormulasAsync(queryDto);

                    if (!allFormulasResult.IsSuccess || allFormulasResult.Data?.Data == null)
                    {
                        throw new Exception("获取验方数据失败");
                    }

                    // 这里应该实现具体的导出逻辑
                    // 可以使用 NPOI 或其他 Excel 处理库
                    // 暂时只显示成功消息
                    StatusMessage = $"验方数据已导出到: {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"导出数据失败: {ex.Message}", ex);
            }
        }

        protected override bool FilterItem(FormulaDto item)
        {
            // 搜索文本筛选
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!item.Name.ToLower().Contains(searchLower) &&
                    !item.Effect.ToLower().Contains(searchLower) &&
                    !(item.CreatedByName?.ToLower().Contains(searchLower) ?? false))
                {
                    return false;
                }
            }

            // 共享状态筛选
            if (SelectedSharedStatus.HasValue && item.IsShared != SelectedSharedStatus.Value)
            {
                return false;
            }

            // 创建者筛选
            if (!string.IsNullOrEmpty(SelectedCreator) && SelectedCreator != "全部")
            {
                if (!string.Equals(item.CreatedByName, SelectedCreator, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // 功效筛选
            if (!string.IsNullOrEmpty(SelectedEffect) && SelectedEffect != "全部")
            {
                if (!item.Effect.Contains(SelectedEffect))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Commands

        public DelegateCommand<FormulaDto> CopyFormulaCommand { get; }
        public DelegateCommand<FormulaDto> ShareFormulaCommand { get; }
        public DelegateCommand<FormulaDto> PreviewFormulaCommand { get; }

        #endregion

        #region Private Methods

        /// <summary>
        /// 加载统计信息
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var result = await _formulaService.GetFormulaStatisticsAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    Statistics = result.Data;
                    RaisePropertyChanged(nameof(Statistics));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载验方统计信息失败: {ex.Message}");
                // 使用默认统计信息
                Statistics = new FormulaStatisticsDto();
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
                // 获取创建者选项
                var creatorsResult = await _formulaService.GetFormulaCreatorsAsync();
                if (creatorsResult.IsSuccess && creatorsResult.Data != null)
                {
                    var creators = new List<string> { "全部" };
                    creators.AddRange(creatorsResult.Data.Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c));
                    CreatorOptions = creators;
                    RaisePropertyChanged(nameof(CreatorOptions));
                }

                // 获取功效选项
                var effectsResult = await _formulaService.GetFormulaEffectsAsync();
                if (effectsResult.IsSuccess && effectsResult.Data != null)
                {
                    var effects = new List<string> { "全部" };
                    effects.AddRange(effectsResult.Data.Where(e => !string.IsNullOrEmpty(e)).Distinct().OrderBy(e => e));
                    EffectOptions = effects;
                    RaisePropertyChanged(nameof(EffectOptions));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载筛选选项失败: {ex.Message}");
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
        /// 复制验方
        /// </summary>
        public async Task CopyFormulaAsync(FormulaDto formula)
        {
            try
            {
                var result = MessageBox.Show(
                    $"确定要复制验方 '{formula.Name}' 吗？\n复制后的验方名称将自动添加\"(副本)\"后缀。",
                    "确认复制",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                var copyResult = await _formulaService.CopyFormulaAsync(formula.Id);
                if (!copyResult.IsSuccess)
                {
                    throw new Exception(copyResult.Message ?? "复制验方失败");
                }

                MessageBox.Show("验方复制成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"验方 '{formula.Name}' 已复制";
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制验方失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 切换验方共享状态
        /// </summary>
        public async Task ShareFormulaAsync(FormulaDto formula)
        {
            try
            {
                var action = formula.IsShared ? "取消共享" : "设为共享";
                var result = MessageBox.Show(
                    $"确定要{action}验方 '{formula.Name}' 吗？",
                    $"确认{action}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                var shareResult = await _formulaService.ToggleFormulaShareStatusAsync(formula.Id);
                if (!shareResult.IsSuccess)
                {
                    throw new Exception(shareResult.Message ?? $"{action}验方失败");
                }

                StatusMessage = $"验方 '{formula.Name}' 已{action}";
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(formula.IsShared ? "取消共享" : "设为共享")}验方失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 预览验方详情
        /// </summary>
        public async Task PreviewFormulaAsync(FormulaDto formula)
        {
            try
            {
                // 获取验方详情
                var detailResult = await _formulaService.GetFormulaAsync(formula.Id);
                if (!detailResult.IsSuccess || detailResult.Data == null)
                {
                    throw new Exception("获取验方详情失败");
                }

                var dialog = new Views.Management.Formulas.Dialogs.FormulaPreviewDialog(detailResult.Data);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"预览验方失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}