using System.Collections.ObjectModel;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// Issue #2246: 验方导入弹窗ViewModel
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

        private string _previewText = "请选择一个验方查看药材组成";
        /// <summary>
        /// 预览文本
        /// </summary>
        public string PreviewText
        {
            get => _previewText;
            set => SetProperty(ref _previewText, value);
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

        /// <summary>
        /// 选中验方的药材列表（用于导入）
        /// </summary>
        public List<FormulaHerbItemDto> SelectedFormulaHerbs { get; private set; } = new();

        #endregion

        #region IDialogAware

        public string Title => "从验方导入";

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

            _logger.LogInformation("FormulaImportDialogViewModel已初始化");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载验方列表
        /// </summary>
        private async void LoadFormulasAsync()
        {
            try
            {
                StatusMessage = "正在加载验方列表...";

                var result = await _formulaRepository.GetPagedAsync(1, 500);
                _allFormulas = result.Items.ToList();
                FilteredFormulas = new ObservableCollection<FormulaDto>(_allFormulas);

                StatusMessage = $"共 {_allFormulas.Count} 个验方";
                _logger.LogInformation("加载了 {Count} 个验方", _allFormulas.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = "加载验方失败";
                _logger.LogError(ex, "加载验方列表失败");
            }
        }

        /// <summary>
        /// 筛选验方
        /// </summary>
        private void FilterFormulas()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredFormulas = new ObservableCollection<FormulaDto>(_allFormulas);
            }
            else
            {
                var searchLower = SearchText.ToLowerInvariant();
                var filtered = _allFormulas.Where(f =>
                    f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (f.Effect?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (f.Indications?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

                FilteredFormulas = new ObservableCollection<FormulaDto>(filtered);
            }

            StatusMessage = $"筛选结果: {FilteredFormulas.Count} 个验方";
        }

        /// <summary>
        /// 加载验方预览
        /// </summary>
        private async void LoadFormulaPreviewAsync()
        {
            if (SelectedFormula == null)
            {
                PreviewText = "请选择一个验方查看药材组成";
                SelectedFormulaHerbs = new List<FormulaHerbItemDto>();
                return;
            }

            try
            {
                // 获取验方详情（包含药材列表）
                var detail = await _formulaRepository.GetByIdAsync(SelectedFormula.Id);
                if (detail?.Herbs != null && detail.Herbs.Any())
                {
                    SelectedFormulaHerbs = detail.Herbs;
                    PreviewText = string.Join(", ", detail.Herbs.Select(h =>
                        $"{h.HerbName}{h.Quantity}{h.Unit}"));
                }
                else
                {
                    SelectedFormulaHerbs = new List<FormulaHerbItemDto>();
                    PreviewText = "该验方暂无药材组成";
                }
            }
            catch (Exception ex)
            {
                PreviewText = "加载药材预览失败";
                _logger.LogError(ex, "加载验方预览失败，验方ID: {FormulaId}", SelectedFormula.Id);
            }
        }

        private bool CanConfirm() => SelectedFormula != null && SelectedFormulaHerbs.Any();

        private void ExecuteConfirm()
        {
            var parameters = new DialogParameters
            {
                { "SelectedFormula", SelectedFormula },
                { "SelectedHerbs", SelectedFormulaHerbs }
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
