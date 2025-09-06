using System.Collections.ObjectModel;
using LYBT.Desktop.Core.Interfaces.Services;

// UltraThink v2.0重构: 直接使用FormulaDto，移除Info模型引用
using LYBT.Shared.Models.Contracts.Formula;
using Prism.Commands;
using Prism.Mvvm;
using IFormulaService = LYBT.Shared.Interfaces.Services.IFormulaService;

namespace LYBT.Desktop.Prescriptions.ViewModels {

    /// <summary>
    /// 验方选择对话框视图模型
    /// </summary>
    public class SelectFormulaDialogViewModel : BindableBase {
        private readonly IFormulaService _formulaService;
        private readonly ICustomDialogService _dialogService;

        private bool _isLoading;
        private string _searchKeyword = string.Empty;
        private string _selectedCategory = "全部";
        private FormulaDto? _selectedFormula;
        private ObservableCollection<FormulaDto> _formulas;
        private ObservableCollection<string> _categories;
        private string _previewText = string.Empty;

        #region 属性

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword {
            get => _searchKeyword;
            set {
                if (SetProperty(ref _searchKeyword, value)) {
                    SearchCommand.Execute();
                }
            }
        }

        /// <summary>
        /// 选中的分类
        /// </summary>
        public string SelectedCategory {
            get => _selectedCategory;
            set {
                if (SetProperty(ref _selectedCategory, value)) {
                    FilterByCategory();
                }
            }
        }

        /// <summary>
        /// 选中的验方 - UltraThink v2.0: 直接使用FormulaDto
        /// </summary>
        public FormulaDto? SelectedFormula {
            get => _selectedFormula;
            set {
                if (SetProperty(ref _selectedFormula, value)) {
                    UpdatePreview();
                    ConfirmCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 验方列表 - UltraThink v2.0: 直接使用FormulaDto
        /// </summary>
        public ObservableCollection<FormulaDto> Formulas {
            get => _formulas;
            set => SetProperty(ref _formulas, value);
        }

        /// <summary>
        /// 分类列表
        /// </summary>
        public ObservableCollection<string> Categories {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        /// <summary>
        /// 预览文本
        /// </summary>
        public string PreviewText {
            get => _previewText;
            set => SetProperty(ref _previewText, value);
        }

        /// <summary>
        /// 是否显示空状态
        /// </summary>
        public bool ShowEmptyState => !IsLoading && (_formulas == null || !_formulas.Any());

        /// <summary>
        /// 空状态消息
        /// </summary>
        public string EmptyStateMessage {
            get {
                if (!string.IsNullOrWhiteSpace(SearchKeyword)) {
                    return $"未找到包含 \"{SearchKeyword}\" 的验方";
                }

                if (SelectedCategory != "全部") {
                    return $"分类 \"{SelectedCategory}\" 下暂无验方";
                }

                return "暂无验方数据";
            }
        }

        #endregion 属性

        #region 命令

        public DelegateCommand SearchCommand { get; } = null!;
        public DelegateCommand RefreshCommand { get; } = null!;
        public DelegateCommand ConfirmCommand { get; } = null!;
        public DelegateCommand CancelCommand { get; } = null!;
        public DelegateCommand<FormulaDto> SelectFormulaCommand { get; } = null!;
        public DelegateCommand<FormulaDto> ViewDetailsCommand { get; } = null!;

        #endregion 命令

        #region 回调

        public Action<FormulaDto>? OnFormulaSelected { get; set; }
        public Action? OnCancelled { get; set; }

        #endregion 回调

        private List<FormulaDto> _allFormulas = new();

        public SelectFormulaDialogViewModel(
            IFormulaService formulaService,
            ICustomDialogService dialogService) {
            _formulaService = formulaService;
            _dialogService = dialogService;

            _formulas = new ObservableCollection<FormulaDto>();
            _categories = new ObservableCollection<string> { "全部" };

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            RefreshCommand = new DelegateCommand(async () => await LoadFormulasAsync());
            ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            SelectFormulaCommand = new DelegateCommand<FormulaDto>(ExecuteSelectFormula);
            ViewDetailsCommand = new DelegateCommand<FormulaDto>(ExecuteViewDetails);

            // 初始加载
            Task.Run(async () => await LoadFormulasAsync());
        }

        private async Task LoadFormulasAsync() {
            try {
                IsLoading = true;

                // UltraThink v2.0: 直接使用FormulaService搜索DTOs (使用空字符串获取所有)
                var result = await _formulaService.SearchAsync("");
                if (result.IsSuccess && result.Data != null) {
                    // UltraThink v2.0: 直接使用DTOs，无需转换
                    _allFormulas = result.Data.ToList();

                    // 提取分类
                    var categories = _allFormulas
                        .Where(f => !string.IsNullOrWhiteSpace(f.Category))
                        .Select(f => f.Category)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

                    Categories.Clear();
                    Categories.Add("全部");
                    foreach (var category in categories) {
                        Categories.Add(category);
                    }

                    // 显示所有验方
                    Formulas = new ObservableCollection<FormulaDto>(_allFormulas);
                    RaisePropertyChanged(nameof(ShowEmptyState));
                    RaisePropertyChanged(nameof(EmptyStateMessage));
                } else {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "加载验方列表失败",
                        "加载失败");
                }
            } catch (Exception ex) {
                await _dialogService.ShowErrorAsync(
                    $"加载验方列表时发生错误：{ex.Message}",
                    "系统错误");
            } finally {
                IsLoading = false;
            }
        }

        private Task ExecuteSearchAsync() {
            try {
                IsLoading = true;

                if (string.IsNullOrWhiteSpace(SearchKeyword)) {
                    // 如果搜索词为空，显示当前分类的所有验方
                    FilterByCategory();
                } else {
                    // 在当前分类中搜索
                    var filteredFormulas = _allFormulas.Where(f => {
                        // 分类筛选
                        if (SelectedCategory != "全部" && f.Category != SelectedCategory) {
                            return false;
                        }

                        // 关键词搜索（名称、效果、备注）
                        var keyword = SearchKeyword.ToLower();
                        return f.Name.ToLower().Contains(keyword) ||
                               (!string.IsNullOrWhiteSpace(f.Effect) && f.Effect.ToLower().Contains(keyword)) ||
                               (!string.IsNullOrWhiteSpace(f.Remark) && f.Remark.ToLower().Contains(keyword));
                    }).ToList();

                    Formulas = new ObservableCollection<FormulaDto>(filteredFormulas);
                    RaisePropertyChanged(nameof(ShowEmptyState));
                    RaisePropertyChanged(nameof(EmptyStateMessage));
                }
            } finally {
                IsLoading = false;
            }

            return Task.CompletedTask;
        }

        private void FilterByCategory() {
            if (SelectedCategory == "全部") {
                Formulas = new ObservableCollection<FormulaDto>(_allFormulas);
            } else {
                var filtered = _allFormulas.Where(f => f.Category == SelectedCategory).ToList();
                Formulas = new ObservableCollection<FormulaDto>(filtered);
            }

            RaisePropertyChanged(nameof(ShowEmptyState));
            RaisePropertyChanged(nameof(EmptyStateMessage));
        }

        private void UpdatePreview() {
            if (SelectedFormula == null) {
                PreviewText = "请选择一个验方查看详情";
                return;
            }

            // UltraThink v2.0: 直接使用DTO属性构建预览
            var preview = $"【{SelectedFormula.Name}】\n\n";

            if (!string.IsNullOrWhiteSpace(SelectedFormula.Category)) {
                preview += $"分类：{SelectedFormula.Category}\n";
            }

            if (!string.IsNullOrWhiteSpace(SelectedFormula.Source)) {
                preview += $"来源：{SelectedFormula.Source}\n";
            }

            if (!string.IsNullOrWhiteSpace(SelectedFormula.Effect)) {
                preview += $"功效：{SelectedFormula.Effect}\n";
            }

            if (!string.IsNullOrWhiteSpace(SelectedFormula.Remark)) {
                preview += $"备注：{SelectedFormula.Remark}\n";
            }

            preview += $"\n创建时间：{SelectedFormula.CreateTime:yyyy-MM-dd HH:mm}";

            PreviewText = preview;
        }

        private void ExecuteSelectFormula(FormulaDto formula) {
            SelectedFormula = formula;
        }

        private void ExecuteViewDetails(FormulaDto formula) {
            if (formula == null) {
                return;
            }

            // 选中并更新预览
            SelectedFormula = formula;

            // 可以在这里添加更多详情展示逻辑
            _dialogService.ShowInformationAsync(
                PreviewText,
                $"验方详情 - {formula.Name}");
        }

        private bool CanExecuteConfirm() {
            return SelectedFormula != null;
        }

        private void ExecuteConfirm() {
            if (SelectedFormula != null) {
                OnFormulaSelected?.Invoke(SelectedFormula);
            }
        }

        private void ExecuteCancel() {
            OnCancelled?.Invoke();
        }
    }

    /// <summary>
    /// 验方分类选项
    /// </summary>
    public class FormulaCategoryOption {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Count { get; set; } = 0;
    }
}
