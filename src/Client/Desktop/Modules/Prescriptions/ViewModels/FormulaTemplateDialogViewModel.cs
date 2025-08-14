using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Prism.Commands;
using Prism.Mvvm;
// using Prism.Dialogs; // Temporarily disabled due to Prism 9 compatibility
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    /// <summary>
    /// 验方模板选择对话框视图模型
    /// </summary>
    public class FormulaTemplateDialogViewModel : BindableBase // Temporarily remove IDialogAware due to Prism 9 compatibility issues
    {
        private readonly IFormulaService _formulaService;
        private readonly ILogger<FormulaTemplateDialogViewModel> _logger;

        #region Dialog Properties

        public string Title => "选择验方模板";
        // public event Action<IDialogResult>? RequestClose; // Removed for Prism 9 compatibility

        #endregion

        #region Properties

        private ObservableCollection<FormulaInfo> _formulas = new();
        public ObservableCollection<FormulaInfo> Formulas
        {
            get => _formulas;
            set => SetProperty(ref _formulas, value);
        }

        private ObservableCollection<FormulaInfo> _filteredFormulas = new();
        public ObservableCollection<FormulaInfo> FilteredFormulas
        {
            get => _filteredFormulas;
            set => SetProperty(ref _filteredFormulas, value);
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

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<string> Categories { get; } = new()
        {
            "全部", "经典验方", "内科", "外科", "妇科", 
            "儿科", "皮肤科", "五官科", "骨伤科", "其他"
        };

        #endregion

        #region Commands

        public DelegateCommand SelectCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<FormulaInfo> ViewDetailsCommand { get; }

        #endregion

        #region Constructor

        public FormulaTemplateDialogViewModel(
            IFormulaService formulaService,
            ILogger<FormulaTemplateDialogViewModel> logger)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化命令
            SelectCommand = new DelegateCommand(Select, CanSelect)
                .ObservesProperty(() => SelectedFormula);
            CancelCommand = new DelegateCommand(Cancel);
            RefreshCommand = new DelegateCommand(async () => await LoadFormulasAsync());
            ViewDetailsCommand = new DelegateCommand<FormulaInfo>(ViewDetails);

            // 初始加载数据
            Task.Run(async () => await LoadFormulasAsync());
        }

        #endregion

        #region Dialog Methods (Temporarily disabled due to Prism 9 compatibility)

        // public bool CanCloseDialog() => !IsLoading;

        // public void OnDialogClosed()
        // {
        //     // 清理资源
        // }

        // public void OnDialogOpened(IDialogParameters parameters)
        // {
        //     // 可以根据参数设置初始过滤条件
        //     if (parameters.ContainsKey("Category"))
        //     {
        //         SelectedCategory = parameters.GetValue<string>("Category");
        //     }
        // }

        #endregion

        #region Methods

        private async Task LoadFormulasAsync()
        {
            try
            {
                IsLoading = true;
                var result = await _formulaService.GetFormulasAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    // Convert FormulaDto to FormulaInfo
                    var formulaInfos = result.Data.Select(dto => {
                        var info = new FormulaInfo();
                        info.Id = dto.Id;
                        info.Name = dto.Name;
                        info.Category = "其他"; // Default category
                        info.Effect = dto.Effect;
                        info.Usage = dto.Usage;
                        info.Remark = dto.Remark;
                        info.IsShared = dto.IsShared;
                        info.CreateTime = dto.CreateTime;
                        info.UpdateTime = dto.UpdateTime;
                        return info;
                    }).ToList();
                    Formulas = new ObservableCollection<FormulaInfo>(formulaInfos);
                    FilterFormulas();
                }
                else
                {
                    _logger.LogWarning("加载验方模板失败: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载验方模板时出错");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterFormulas()
        {
            var filtered = Formulas.AsEnumerable();

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
                    (f.DosageInstruction?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            FilteredFormulas = new ObservableCollection<FormulaInfo>(filtered);
        }

        private bool CanSelect()
        {
            return SelectedFormula != null;
        }

        private void Select()
        {
            if (SelectedFormula != null)
            {
                // TODO: Implement dialog close logic when Prism dialog support is added
                // var parameters = new DialogParameters
                // {
                //     { "SelectedFormula", SelectedFormula }
                // };
                // RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
            }
        }

        private void Cancel()
        {
            // TODO: Implement dialog cancel logic when Prism dialog support is added
            // RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private void ViewDetails(FormulaInfo? formula)
        {
            if (formula == null) return;

            // TODO: 显示验方详情对话框
            // 可以显示验方的组成、功效、用法等详细信息
            _logger.LogInformation("查看验方详情: {FormulaName}", formula.Name);
        }

        #endregion
    }
}