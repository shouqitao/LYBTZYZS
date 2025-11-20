using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 经验方选择对话框ViewModel
    /// Epic #2175 BF-002 Task 3.8 - 实现经验方导入对话框
    /// </summary>
    public class FormulaSelectionDialogViewModel : ViewModelBase, IDialogAware
    {
        #region 字段

        private readonly IFormulaRepository _formulaRepository;
        private bool _isLoading;
        private FormulaDto? _selectedFormula;
        private ObservableCollection<FormulaDto> _formulas = new();
        private string _searchKeyword = string.Empty;

        #endregion

        #region 构造函数

        public FormulaSelectionDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IFormulaRepository formulaRepository)
            : base(eventAggregator, loggerFactory)
        {
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));

            // 初始化Commands
            ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            SearchCommand = new DelegateCommand(ExecuteSearch);

            // 加载经验方列表
            _ = LoadFormulasAsync();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 经验方列表
        /// </summary>
        public ObservableCollection<FormulaDto> Formulas
        {
            get => _formulas;
            private set => SetProperty(ref _formulas, value);
        }

        /// <summary>
        /// 选中的经验方
        /// </summary>
        public FormulaDto? SelectedFormula
        {
            get => _selectedFormula;
            set
            {
                if (SetProperty(ref _selectedFormula, value))
                {
                    ConfirmCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        #endregion

        #region Commands

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand SearchCommand { get; }

        #endregion

        #region Command实现

        private bool CanExecuteConfirm()
        {
            return SelectedFormula != null;
        }

        private void ExecuteConfirm()
        {
            // 返回选中的经验方
            var parameters = new DialogParameters
            {
                { "SelectedFormula", SelectedFormula }
            };

            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private async void ExecuteSearch()
        {
            await LoadFormulasAsync();
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 加载经验方列表
        /// </summary>
        private async Task LoadFormulasAsync()
        {
            try
            {
                IsLoading = true;
                Formulas.Clear();

                Logger.LogDebug("开始加载经验方列表 - 关键词: {Keyword}", SearchKeyword);

                // 分页加载所有经验方（Server端限制pageSize最大100）
                const int pageSize = 100;
                int currentPage = 1;
                int totalLoaded = 0;

                while (true)
                {
                    var keyword = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim();
                    var pagedResult = await _formulaRepository.GetPagedAsync(currentPage, pageSize, keyword);

                    if (pagedResult?.Items == null || !pagedResult.Items.Any())
                    {
                        break; // 没有更多数据
                    }

                    foreach (var formula in pagedResult.Items)
                    {
                        Formulas.Add(formula);
                    }

                    totalLoaded += pagedResult.Items.Count;

                    // 如果当前页数据不足pageSize，说明已经是最后一页
                    if (pagedResult.Items.Count < pageSize)
                    {
                        break;
                    }

                    currentPage++;
                }

                Logger.LogInformation("成功加载 {Count} 个经验方", totalLoaded);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载经验方列表失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region IDialogAware实现

        public string Title => "选择经验方";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            Logger.LogDebug("经验方选择对话框关闭");
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Logger.LogDebug("经验方选择对话框打开");
        }

        #endregion
    }
}
