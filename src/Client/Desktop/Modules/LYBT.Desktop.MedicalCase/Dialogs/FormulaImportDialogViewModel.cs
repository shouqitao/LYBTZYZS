using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// 验方导入弹窗ViewModel - 重新设计版本
    /// OpenSpec: redesign-formula-import-ui
    /// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
    /// OpenSpec: refactor-frontend-srp-patterns - 迁移到DialogViewModelBase
    /// 用于从经验方库搜索选择验方，批量导入药材到处方
    /// </summary>
    public partial class FormulaImportDialogViewModel : DialogViewModelBase
    {
        #region 服务依赖

        private readonly IFormulaSearchProvider _formulaSearchProvider;
        private List<FormulaListDto> _allFormulas = new();

        #endregion

        #region 可观察属性

        /// <summary>
        /// 搜索文本
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// 分类列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        /// <summary>
        /// 选中的分类
        /// </summary>
        [ObservableProperty]
        private string _selectedCategory = "全部";

        /// <summary>
        /// 筛选后的验方列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<FormulaListDto> _filteredFormulas = new();

        /// <summary>
        /// 选中的验方
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private FormulaListDto? _selectedFormula;

        /// <summary>
        /// 选中验方的详情（用于FormulaViewControl预览）
        /// OpenSpec: extract-detail-controls Task 1.4
        /// </summary>
        [ObservableProperty]
        private FormulaDetailDto? _selectedFormulaDetail;

        /// <summary>
        /// 选中验方的药材列表（用于绑定和导入）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<FormulaHerbItemDto> _selectedFormulaHerbs = new();

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 加载提示消息
        /// </summary>
        [ObservableProperty]
        private string _loadingMessage = string.Empty;

        #endregion

        #region 属性变更回调

        /// <summary>
        /// 搜索文本变更时触发筛选
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            FilterFormulas();
        }

        /// <summary>
        /// 选中分类变更时触发筛选
        /// </summary>
        partial void OnSelectedCategoryChanged(string value)
        {
            FilterFormulas();
        }

        /// <summary>
        /// 选中验方变更时加载预览
        /// </summary>
        partial void OnSelectedFormulaChanged(FormulaListDto? value)
        {
            LoadFormulaPreviewAsync();
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数 - 使用IViewModelServices聚合服务
        /// OpenSpec: refactor-frontend-srp-patterns - 迁移到DialogViewModelBase
        /// </summary>
        public FormulaImportDialogViewModel(
            IViewModelServices services,
            IFormulaSearchProvider formulaSearchProvider)
            : base(services)
        {
            _formulaSearchProvider = formulaSearchProvider ?? throw new ArgumentNullException(nameof(formulaSearchProvider));
            Title = "从经验方导入";

            // 初始化分类列表
            InitializeCategories();

            Logger.LogInformation("FormulaImportDialogViewModel已初始化");
        }

        #endregion

        #region DialogViewModelBase重写

        /// <summary>
        /// 对话框打开时的核心处理
        /// </summary>
        protected override void OnDialogOpenedCore(IDialogParameters? parameters)
        {
            LoadFormulasAsync();
        }

        /// <summary>
        /// 是否可以确认（子类重写）
        /// </summary>
        protected override bool CanConfirm() => SelectedFormula != null && SelectedFormulaHerbs.Any();

        /// <summary>
        /// 确认命令（子类重写）
        /// </summary>
        protected override void Confirm()
        {
            var parameters = new DialogParameters
            {
                { "SelectedFormula", SelectedFormula },
                { "SelectedHerbs", SelectedFormulaHerbs.ToList() }
            };
            CloseDialog(parameters, ButtonResult.OK);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化分类列表
        /// </summary>
        private void InitializeCategories()
        {
            Categories = new ObservableCollection<string>
            {
                "全部",
                "内科方",
                "外科方",
                "妇科方",
                "儿科方",
                "验方"
            };
        }

        /// <summary>
        /// 加载验方列表
        /// </summary>
        private async void LoadFormulasAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载验方列表...";

                // 统一默认PageSize，通过搜索/分类筛选定位验方
                var result = await _formulaSearchProvider.GetFormulasPagedAsync(1, SystemConstants.DefaultPageSize);
                _allFormulas = result.Items.ToList();

                // 动态更新分类列表（保留已有分类，添加数据中的新分类）
                var existingCategories = new HashSet<string>(Categories);
                foreach (var formula in _allFormulas)
                {
                    if (!string.IsNullOrWhiteSpace(formula.Category) && !existingCategories.Contains(formula.Category))
                    {
                        Categories.Add(formula.Category);
                        existingCategories.Add(formula.Category);
                    }
                }

                FilterFormulas();
                if (_allFormulas.Count > 0)
                {
                    StatusMessage = $"共 {_allFormulas.Count} 个经验方";
                }
                else
                {
                    StatusMessage = "暂无经验方数据，请先在经验方管理中添加";
                }
                Logger.LogInformation("加载了 {Count} 个验方", _allFormulas.Count);
            }
            catch (HttpRequestException ex)
            {
                StatusMessage = "网络连接失败，请确认后端服务已启动";
                Logger.LogError(ex, "加载验方列表失败: 网络连接问题");
            }
            catch (Exception ex)
            {
                StatusMessage = "加载验方失败，请检查后端服务";
                Logger.LogError(ex, "加载验方列表失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 筛选验方（支持搜索文本和分类筛选）
        /// </summary>
        private void FilterFormulas()
        {
            var filtered = _allFormulas.AsEnumerable();

            // T5-P2-17: 过滤未验证的验方
            filtered = filtered.Where(f => f.ValidationStatus == FormulaValidationStatus.Validated);

            // T5-P2-18: 过滤已禁用的验方
            filtered = filtered.Where(f => f.Status == CommonStatus.Enabled);

            // 1. 分类筛选
            if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "全部")
            {
                filtered = filtered.Where(f => f.Category == SelectedCategory);
            }

            // 2. 文本搜索（支持名称、适应症、功效）
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(f =>
                    f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (f.Effect?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (f.Indications?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            FilteredFormulas = new ObservableCollection<FormulaListDto>(filtered);

            // 更新状态消息
            if (_allFormulas.Count > 0)
            {
                if (FilteredFormulas.Count == _allFormulas.Count)
                {
                    StatusMessage = $"共 {_allFormulas.Count} 个经验方";
                }
                else
                {
                    StatusMessage = $"筛选结果: {FilteredFormulas.Count} / {_allFormulas.Count} 个";
                }
            }
        }

        /// <summary>
        /// 加载验方预览（药材详情）
        /// OpenSpec: extract-detail-controls Task 1.4 - 保存完整detail供FormulaViewControl使用
        /// </summary>
        private async void LoadFormulaPreviewAsync()
        {
            if (SelectedFormula == null)
            {
                SelectedFormulaDetail = null;
                SelectedFormulaHerbs = new ObservableCollection<FormulaHerbItemDto>();
                return;
            }

            try
            {
                IsLoading = true;
                LoadingMessage = "正在加载药材组成...";

                // 获取验方详情（包含药材列表）
                var detail = await _formulaSearchProvider.GetFormulaByIdAsync(SelectedFormula.Id);

                // 保存完整detail供FormulaViewControl使用
                SelectedFormulaDetail = detail;

                if (detail?.Herbs != null && detail.Herbs.Any())
                {
                    SelectedFormulaHerbs = new ObservableCollection<FormulaHerbItemDto>(detail.Herbs);
                }
                else
                {
                    SelectedFormulaHerbs = new ObservableCollection<FormulaHerbItemDto>();
                }

                // 药材加载完成后刷新确认按钮状态
                ConfirmCommand.NotifyCanExecuteChanged();

                LoadingMessage = string.Empty;
            }
            catch (Exception ex)
            {
                SelectedFormulaDetail = null;
                LoadingMessage = "加载药材失败";
                Logger.LogError(ex, "加载验方预览失败，验方ID: {FormulaId}", SelectedFormula.Id);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}
