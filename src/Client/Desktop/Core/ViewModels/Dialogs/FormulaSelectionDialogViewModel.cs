using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Mvvm;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Core.ViewModels.Dialogs
{
    /// <summary>
    /// 验方选择对话框ViewModel
    /// </summary>
    public class FormulaSelectionDialogViewModel : ObservableObject, ICustomDialogAware
    {
        private readonly IFormulaService _formulaService;
        private readonly ILogger<FormulaSelectionDialogViewModel> _logger;

        #region Properties

        private string _title = "选择验方";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

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
            set
            {
                SetProperty(ref _selectedFormula, value);
                OnPropertyChanged(nameof(CanConfirm));
            }
        }

        public bool CanConfirm => SelectedFormula != null;

        #endregion

        #region Commands

        public ICommand SearchCommand { get; }
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Events

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<CustomDialogResult>? RequestClose;

        #endregion

        #region Constructor

        public FormulaSelectionDialogViewModel(
            IFormulaService formulaService,
            ILogger<FormulaSelectionDialogViewModel> logger)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化命令
            SearchCommand = new AsyncRelayCommand(SearchFormulasAsync);
            ConfirmCommand = new RelayCommand(ConfirmSelection, () => CanConfirm);
            CancelCommand = new RelayCommand(CancelSelection);
        }

        #endregion

        #region ICustomDialogAware Implementation

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        /// <param name="parameters">对话框参数</param>
        public void OnDialogOpened(Dictionary<string, object> parameters)
        {
            try
            {
                // 设置标题
                if (parameters.ContainsKey("Title"))
                {
                    Title = parameters["Title"].ToString() ?? "选择验方";
                }

                // 设置搜索关键词
                if (parameters.ContainsKey("SearchKeyword"))
                {
                    SearchKeyword = parameters["SearchKeyword"].ToString() ?? string.Empty;
                }

                // 加载验方列表
                _ = LoadFormulasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开验方选择对话框时发生错误");
            }
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed()
        {
            // 清理资源
            Formulas.Clear();
            SelectedFormula = null;
        }

        /// <summary>
        /// 检查是否可以关闭对话框
        /// </summary>
        /// <returns>true: 可以关闭, false: 不可以关闭</returns>
        public bool CanCloseDialog()
        {
            // 对于验方选择对话框，总是允许关闭
            return true;
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

                // 调用验方服务获取数据
                var result = await _formulaService.GetFormulasAsync(SearchKeyword);

                if (result.IsSuccess)
                {
                    var formulaInfos = result.Data ?? new List<FormulaInfo>();

                    Formulas.Clear();
                    foreach (var formula in formulaInfos)
                    {
                        Formulas.Add(formula);
                    }

                    _logger.LogDebug("加载了 {Count} 个验方", Formulas.Count);
                }
                else
                {
                    _logger.LogWarning("加载验方列表失败: {Message}", result.ErrorMessage);
                    Formulas.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载验方列表时发生异常");
                Formulas.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 搜索验方
        /// </summary>
        private async Task SearchFormulasAsync()
        {
            await LoadFormulasAsync();
        }

        /// <summary>
        /// 确认选择
        /// </summary>
        private void ConfirmSelection()
        {
            if (SelectedFormula == null)
                return;

            try
            {
                var result = new CustomDialogResult
                {
                    Result = true,
                    Parameters = new Dictionary<string, object>
                    {
                        ["SelectedFormula"] = SelectedFormula,
                        ["FormulaId"] = SelectedFormula.Id,
                        ["FormulaName"] = SelectedFormula.Name,
                        ["HerbNames"] = SelectedFormula.HerbNames ?? string.Empty
                    },
                    Data = SelectedFormula
                };

                RequestClose?.Invoke(result);
                _logger.LogDebug("确认选择验方: {FormulaName} (ID: {FormulaId})", 
                    SelectedFormula.Name, SelectedFormula.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认选择验方时发生错误");
            }
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        private void CancelSelection()
        {
            try
            {
                var result = new CustomDialogResult
                {
                    Result = false,
                    Parameters = new Dictionary<string, object>()
                };

                RequestClose?.Invoke(result);
                _logger.LogDebug("取消验方选择");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消验方选择时发生错误");
            }
        }

        #endregion
    }
}