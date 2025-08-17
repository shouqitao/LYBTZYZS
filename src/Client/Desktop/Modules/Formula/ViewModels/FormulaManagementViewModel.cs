using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Microsoft.Extensions.Logging;
using AutoMapper;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Shared.Interfaces.Services;


namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 验方管理主视图模型
    /// </summary>
    public class FormulaManagementViewModel : BindableBase
    {
        private readonly IFormulaService _formulaService;
        private readonly ILogger<FormulaManagementViewModel> _logger;
        private readonly IMapper _mapper; // UltraThink架构：注入AutoMapper

        #region Properties

        private ObservableCollection<FormulaInfo> _formulas = new();
        public ObservableCollection<FormulaInfo> Formulas
        {
            get => _formulas;
            set => SetProperty(ref _formulas, value);
        }

        private FormulaInfo? _selectedFormula;
        public FormulaInfo? SelectedFormula
        {
            get => _selectedFormula;
            set => SetProperty(ref _selectedFormula, value);
        }

        private string _searchText = string.Empty;
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

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _selectedCategory = "全部";
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

        private ObservableCollection<FormulaInfo> _allFormulas = new();

        public ObservableCollection<string> Categories { get; } = new()
        {
            "全部", "内科方", "外科方", "妇科方", "儿科方",
            "皮肤科方", "五官科方", "骨伤科方", "经典方",
            "时方", "验方", "其他"
        };

        #endregion

        #region Commands

        public DelegateCommand LoadFormulasCommand { get; }
        public DelegateCommand AddFormulaCommand { get; }
        public DelegateCommand<FormulaInfo> EditFormulaCommand { get; }
        public DelegateCommand<FormulaInfo> DeleteFormulaCommand { get; }
        public DelegateCommand<FormulaInfo> ViewFormulaCommand { get; }
        public DelegateCommand<FormulaInfo> CopyFormulaCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand ClearFiltersCommand { get; }

        #endregion

        #region Constructor

        public FormulaManagementViewModel(
            IFormulaService formulaService,
            ILogger<FormulaManagementViewModel> logger,
            IMapper mapper) // UltraThink架构：注入AutoMapper
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化命令
            LoadFormulasCommand = new DelegateCommand(async () => await LoadFormulasAsync());
            AddFormulaCommand = new DelegateCommand(AddFormula);
            EditFormulaCommand = new DelegateCommand<FormulaInfo>(EditFormula);
            DeleteFormulaCommand = new DelegateCommand<FormulaInfo>(async (f) => await DeleteFormulaAsync(f));
            ViewFormulaCommand = new DelegateCommand<FormulaInfo>(ViewFormula);
            CopyFormulaCommand = new DelegateCommand<FormulaInfo>(async (f) => await CopyFormulaAsync(f));
            RefreshCommand = new DelegateCommand(async () => await LoadFormulasAsync());
            ClearFiltersCommand = new DelegateCommand(ClearFilters);

            // 初始加载数据
            Task.Run(async () => await LoadFormulasAsync());
        }

        #endregion

        #region Methods

        private async Task LoadFormulasAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载验方数据...";

                var result = await _formulaService.GetFormulasAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    // UltraThink架构修复：使用AutoMapper将FormulaDto转换为FormulaInfo
                    var formulaInfoList = _mapper.Map<List<FormulaInfo>>(result.Data);
                    
                    _allFormulas = new ObservableCollection<FormulaInfo>(formulaInfoList);
                    FilterFormulas();
                    StatusMessage = $"已加载 {_allFormulas.Count} 个验方模板";
                }
                else
                {
                    StatusMessage = result.ErrorMessage ?? "加载验方失败";
                    _logger.LogWarning("加载验方失败: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                _logger.LogError(ex, "加载验方时出错");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterFormulas()
        {
            var filtered = _allFormulas.AsEnumerable();

            // 按分类过滤
            if (SelectedCategory != "全部")
            {
                filtered = filtered.Where(f => f.Category == SelectedCategory);
            }

            // 按关键字过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(f =>
                    (f.Name?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (f.Indications?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (f.Source?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            Formulas = new ObservableCollection<FormulaInfo>(filtered.OrderBy(f => f.Name));
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategory = "全部";
            FilterFormulas();
        }

        private void AddFormula()
        {
            // TODO: 实现添加验方对话框
            StatusMessage = "添加验方功能待实现";
        }

        private void EditFormula(FormulaInfo? formula)
        {
            if (formula == null) return;

            // TODO: 实现编辑验方对话框
            StatusMessage = $"编辑验方 '{formula.Name}' 功能待实现";
        }

        private void ViewFormula(FormulaInfo? formula)
        {
            if (formula == null) return;

            // TODO: 实现查看验方对话框
            StatusMessage = $"查看验方 '{formula.Name}' 功能待实现";
        }

        private async Task DeleteFormulaAsync(FormulaInfo? formula)
        {
            if (formula == null) return;

            try
            {
                // TODO: 添加确认对话框
                var deleteResult = await _formulaService.DeleteAsync(formula.Id);
                if (deleteResult.IsSuccess)
                {
                    _allFormulas.Remove(formula);
                    FilterFormulas();
                    StatusMessage = "验方已删除";
                }
                else
                {
                    StatusMessage = deleteResult.ErrorMessage ?? "删除失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除失败: {ex.Message}";
                _logger.LogError(ex, "删除验方时出错");
            }
        }

        private async Task CopyFormulaAsync(FormulaInfo? formula)
        {
            if (formula == null) return;

            try
            {
                StatusMessage = "正在复制验方...";
                var newName = $"{formula.Name}_副本";
                var copyResult = await _formulaService.CopyAsync(formula.Id, newName);
                
                if (copyResult.IsSuccess)
                {
                    await LoadFormulasAsync();
                    StatusMessage = $"验方 '{formula.Name}' 已复制";
                }
                else
                {
                    StatusMessage = copyResult.ErrorMessage ?? "复制失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"复制失败: {ex.Message}";
                _logger.LogError(ex, "复制验方时出错");
            }
        }

        #endregion
    }
}