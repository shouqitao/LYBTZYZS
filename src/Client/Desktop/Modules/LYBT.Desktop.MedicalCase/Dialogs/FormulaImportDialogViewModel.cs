using System.Collections.ObjectModel;
using System.Net.Http;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// OpenSpec: redesign-formula-import-ui
    /// 验方导入弹窗ViewModel - 重新设计版本
    /// 用于从经验方库搜索选择验方，批量导入药材到处方
    /// </summary>
    public class FormulaImportDialogViewModel : BindableBase, IDialogAware
    {
        #region 服务依赖

        private readonly IFormulaRepository _formulaRepository;
        private readonly ILogger<FormulaImportDialogViewModel> _logger;
        private List<FormulaDto> _allFormulas = new();

        #endregion

        #region 属性

        private string _searchText = string.Empty;
        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterFormulas();
                }
            }
        }

        private ObservableCollection<string> _categories = new();
        /// <summary>
        /// 分类列表
        /// </summary>
        public ObservableCollection<string> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private string _selectedCategory = "全部";
        /// <summary>
        /// 选中的分类
        /// </summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    FilterFormulas();
                }
            }
        }

        private ObservableCollection<FormulaDto> _filteredFormulas = new();
        /// <summary>
        /// 筛选后的验方列表
        /// </summary>
        public ObservableCollection<FormulaDto> FilteredFormulas
        {
            get => _filteredFormulas;
            set => SetProperty(ref _filteredFormulas, value);
        }

        private FormulaDto? _selectedFormula;
        /// <summary>
        /// 选中的验方
        /// </summary>
        public FormulaDto? SelectedFormula
        {
            get => _selectedFormula;
            set
            {
                if (SetProperty(ref _selectedFormula, value))
                {
                    LoadFormulaPreviewAsync();
                    ConfirmCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private FormulaDto? _selectedFormulaDetail;
        /// <summary>
        /// 选中验方的详情（用于FormulaViewControl预览）
        /// OpenSpec: extract-detail-controls Task 1.4
        /// </summary>
        public FormulaDto? SelectedFormulaDetail
        {
            get => _selectedFormulaDetail;
            set => SetProperty(ref _selectedFormulaDetail, value);
        }

        private ObservableCollection<FormulaHerbItemDto> _selectedFormulaHerbs = new();
        /// <summary>
        /// 选中验方的药材列表（用于绑定和导入）
        /// </summary>
        public ObservableCollection<FormulaHerbItemDto> SelectedFormulaHerbs
        {
            get => _selectedFormulaHerbs;
            set => SetProperty(ref _selectedFormulaHerbs, value);
        }

        private string _statusMessage = string.Empty;
        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isLoading;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _loadingMessage = string.Empty;
        /// <summary>
        /// 加载提示消息
        /// </summary>
        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        #endregion

        #region IDialogAware

        public string Title => "从经验方导入";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            LoadFormulasAsync();
        }

        #endregion

        #region 命令

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public FormulaImportDialogViewModel(
            IFormulaRepository formulaRepository,
            ILogger<FormulaImportDialogViewModel> logger)
        {
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 初始化分类列表
            InitializeCategories();

            _logger.LogInformation("FormulaImportDialogViewModel已初始化");
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
                var result = await _formulaRepository.GetPagedAsync(1, SystemConstants.DefaultPageSize);
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
                _logger.LogInformation("加载了 {Count} 个验方", _allFormulas.Count);
            }
            catch (HttpRequestException ex)
            {
                StatusMessage = "网络连接失败，请确认后端服务已启动";
                _logger.LogError(ex, "加载验方列表失败: 网络连接问题");
            }
            catch (Exception ex)
            {
                StatusMessage = "加载验方失败，请检查后端服务";
                _logger.LogError(ex, "加载验方列表失败");
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

            // 1. 分类筛选
            if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "全部")
            {
                filtered = filtered.Where(f => f.Category == SelectedCategory);
            }

            // 2. 文本搜索（支持名称、适应症、功效）
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(f =>
                    f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (f.Effect?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (f.Indications?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            FilteredFormulas = new ObservableCollection<FormulaDto>(filtered);

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
                var detail = await _formulaRepository.GetByIdAsync(SelectedFormula.Id);

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

                LoadingMessage = string.Empty;
            }
            catch (Exception ex)
            {
                SelectedFormulaDetail = null;
                LoadingMessage = "加载药材失败";
                _logger.LogError(ex, "加载验方预览失败，验方ID: {FormulaId}", SelectedFormula.Id);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanConfirm() => SelectedFormula != null && SelectedFormulaHerbs.Any();

        private void ExecuteConfirm()
        {
            var parameters = new DialogParameters
            {
                { "SelectedFormula", SelectedFormula },
                { "SelectedHerbs", SelectedFormulaHerbs.ToList() }
            };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion
    }
}
