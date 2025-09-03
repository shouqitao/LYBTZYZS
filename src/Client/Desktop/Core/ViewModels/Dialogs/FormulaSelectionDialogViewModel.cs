using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Core.ViewModels.Dialogs
{
    /// <summary>
    /// 验方选择对话框ViewModel - UltraThink优化版本
    /// 继承DialogViewModelBase，使用标准化错误处理
    /// </summary>
    public class FormulaSelectionDialogViewModel : DialogViewModelBase
    {
        private readonly IFormulaService _formulaService;
        private string _searchKeyword = string.Empty;
        private FormulaDto? _selectedFormula;

        #region Properties

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchCommand?.Execute();
                }
            }
        }

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
                    ConfirmCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 验方列表
        /// </summary>
        public ObservableCollection<FormulaDto> Formulas { get; } = new();

        #endregion

        #region Commands

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; } = null!;

        #endregion

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        public FormulaSelectionDialogViewModel(
            IFormulaService formulaService,
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            Title = "选择验方";

            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());

            // 初始化加载验方列表
            _ = LoadFormulasAsync();
        }

        /// <summary>
        /// 简化构造函数（使用ContainerLocator）
        /// </summary>
        public FormulaSelectionDialogViewModel(IFormulaService formulaService)
            : base()
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            Title = "选择验方";

            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());

            // 初始化加载验方列表
            _ = LoadFormulasAsync();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 执行确认逻辑
        /// </summary>
        protected override Task<bool> ExecuteConfirmAsync()
        {
            return Task.FromResult(SelectedFormula != null);
        }

        /// <summary>
        /// 检查是否可以确认
        /// </summary>
        protected override bool CanConfirm()
        {
            return !IsLoading && SelectedFormula != null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 加载验方列表
        /// </summary>
        private async Task LoadFormulasAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();

                var result = await _formulaService.SearchAsync(SearchKeyword ?? string.Empty);
                if (result.IsSuccess && result.Data != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Formulas.Clear();
                        foreach (var formula in result.Data)
                        {
                            Formulas.Add(formula);
                        }
                    });

                    StatusMessage = $"已加载 {Formulas.Count} 个验方";
                }
                else
                {
                    StatusMessage = result.ErrorMessage ?? "加载验方列表失败";
                    Formulas.Clear();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("加载验方列表", ex);
                Formulas.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行搜索
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            await LoadFormulasAsync();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 获取选择结果
        /// </summary>
        public FormulaDto? GetSelectedFormula()
        {
            return SelectedFormula;
        }

        #endregion
    }
}