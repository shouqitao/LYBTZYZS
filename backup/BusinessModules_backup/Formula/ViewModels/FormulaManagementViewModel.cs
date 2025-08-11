using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Shared;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Navigation.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Formula.Shared.ViewModels
{
    /// <summary>
    /// 验方管理视图模型
    /// </summary>
    public class FormulaManagementViewModel : BaseManagementViewModel<FormulaDto>
    {
        private readonly ISharedFormulaService _formulaService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<FormulaManagementViewModel> _logger;

        private string _searchKeyword = string.Empty;
        private FormulaDto _selectedFormula;
        private bool _showSharedOnly = false;
        private bool _showPersonalOnly = false;

        public FormulaManagementViewModel(
            ISharedFormulaService formulaService,
            IDialogService dialogService,
            ILogger<FormulaManagementViewModel> logger)
            : base(logger)
        {
            _formulaService = formulaService;
            _dialogService = dialogService;
            _logger = logger;

            Title = "验方管理";
            InitializeCommands();
            
            // 自动加载数据
            _ = LoadDataAsync();
        }

        #region Properties

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>
        /// 选中的验方
        /// </summary>
        public FormulaDto SelectedFormula
        {
            get => _selectedFormula;
            set
            {
                SetProperty(ref _selectedFormula, value);
                UpdateCommandStates();
            }
        }

        /// <summary>
        /// 只显示共享验方
        /// </summary>
        public bool ShowSharedOnly
        {
            get => _showSharedOnly;
            set
            {
                SetProperty(ref _showSharedOnly, value);
                if (value && _showPersonalOnly)
                    ShowPersonalOnly = false;
                _ = LoadDataAsync();
            }
        }

        /// <summary>
        /// 只显示个人验方
        /// </summary>
        public bool ShowPersonalOnly
        {
            get => _showPersonalOnly;
            set
            {
                SetProperty(ref _showPersonalOnly, value);
                if (value && _showSharedOnly)
                    ShowSharedOnly = false;
                _ = LoadDataAsync();
            }
        }

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand AddFormulaCommand { get; private set; }
        public DelegateCommand EditFormulaCommand { get; private set; }
        public DelegateCommand ViewFormulaCommand { get; private set; }
        public DelegateCommand CopyFormulaCommand { get; private set; }
        public DelegateCommand FavoriteFormulaCommand { get; private set; }
        public DelegateCommand ValidateFormulaCommand { get; private set; }
        public DelegateCommand ShowStatisticsCommand { get; private set; }
        public DelegateCommand ShowClassicFormulasCommand { get; private set; }
        public DelegateCommand ShowFrequentFormulasCommand { get; private set; }

        #endregion

        #region Methods

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            SearchCommand = new DelegateCommand(async () => await SearchFormulasAsync());
            AddFormulaCommand = new DelegateCommand(async () => await AddFormulaAsync());
            EditFormulaCommand = new DelegateCommand(async () => await EditFormulaAsync(), () => SelectedFormula != null);
            ViewFormulaCommand = new DelegateCommand(async () => await ViewFormulaAsync(), () => SelectedFormula != null);
            CopyFormulaCommand = new DelegateCommand(async () => await CopyFormulaAsync(), () => SelectedFormula != null);
            FavoriteFormulaCommand = new DelegateCommand(async () => await FavoriteFormulaAsync(), () => SelectedFormula != null);
            ValidateFormulaCommand = new DelegateCommand(async () => await ValidateFormulaAsync(), () => SelectedFormula != null);
            ShowStatisticsCommand = new DelegateCommand(async () => await ShowStatisticsAsync(), () => SelectedFormula != null);
            ShowClassicFormulasCommand = new DelegateCommand(async () => await ShowClassicFormulasAsync());
            ShowFrequentFormulasCommand = new DelegateCommand(async () => await ShowFrequentFormulasAsync());
        }

        protected override async Task LoadDataAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                ServiceResult<List<FormulaDto>> result;

                if (_showSharedOnly)
                {
                    result = await _formulaService.GetClassicFormulasAsync();
                }
                else if (_showPersonalOnly)
                {
                    // TODO: 获取当前用户ID
                    result = await _formulaService.GetPersonalFormulasAsync(Guid.NewGuid());
                }
                else
                {
                    result = await _formulaService.GetAllFormulasAsync();
                }

                if (result.IsSuccess)
                {
                    var formulas = result.Data;
                    
                    // 应用搜索过滤
                    if (!string.IsNullOrEmpty(SearchKeyword))
                    {
                        formulas = formulas.Where(f => 
                            f.Name.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                            f.Effect.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(f.CreatedByName) && f.CreatedByName.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase))).ToList();
                    }

                    Items = new ObservableCollection<FormulaDto>(formulas);
                    TotalCount = formulas.Count;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

                    _logger.LogInformation("验方数据加载完成，共 {Count} 条记录", TotalCount);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                    _logger.LogWarning("验方数据加载失败: {Error}", result.ErrorMessage);
                }
            });
        }

        /// <summary>
        /// 搜索验方
        /// </summary>
        private async Task SearchFormulasAsync()
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// 添加验方
        /// </summary>
        private async Task AddFormulaAsync()
        {
            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "添加验方" },
                    { "Mode", "Add" }
                };

                _dialogService.ShowDialog("FormulaAddEditDialog", dialogParameters, async (result) =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("FormulaData"))
                    {
                        var formulaDto = result.Parameters.GetValue<FormulaDto>("FormulaData");
                        await ExecuteWithLoadingAsync(async () =>
                        {
                            var serviceResult = await _formulaService.CreateFormulaAsync(formulaDto);
                            if (serviceResult.IsSuccess)
                            {
                                await LoadDataAsync(); // 刷新列表
                                ShowSuccessMessage("验方添加成功");
                                _logger.LogInformation("验方添加成功: {FormulaName}", formulaDto.Name);
                            }
                            else
                            {
                                ErrorMessage = serviceResult.ErrorMessage;
                                _logger.LogWarning("验方添加失败: {Error}", serviceResult.ErrorMessage);
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加验方时发生错误");
                ErrorMessage = $"添加验方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 编辑验方
        /// </summary>
        private async Task EditFormulaAsync()
        {
            if (SelectedFormula == null)
                return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "编辑验方" },
                    { "Mode", "Edit" },
                    { "FormulaId", SelectedFormula.Id }
                };

                _dialogService.ShowDialog("FormulaAddEditDialog", dialogParameters, async (result) =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("FormulaData"))
                    {
                        var formulaDto = result.Parameters.GetValue<FormulaDto>("FormulaData");
                        await ExecuteWithLoadingAsync(async () =>
                        {
                            var serviceResult = await _formulaService.UpdateFormulaAsync(formulaDto);
                            if (serviceResult.IsSuccess)
                            {
                                await LoadDataAsync(); // 刷新列表
                                ShowSuccessMessage("验方更新成功");
                                _logger.LogInformation("验方更新成功: {FormulaName}", formulaDto.Name);
                            }
                            else
                            {
                                ErrorMessage = serviceResult.ErrorMessage;
                                _logger.LogWarning("验方更新失败: {Error}", serviceResult.ErrorMessage);
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑验方时发生错误");
                ErrorMessage = $"编辑验方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 查看验方详情
        /// </summary>
        private async Task ViewFormulaAsync()
        {
            if (SelectedFormula == null)
                return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "验方详情" },
                    { "Mode", "View" },
                    { "FormulaId", SelectedFormula.Id }
                };

                _dialogService.ShowDialog("FormulaDetailDialog", dialogParameters, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查看验方详情时发生错误");
                ErrorMessage = $"查看验方详情时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 复制验方
        /// </summary>
        private async Task CopyFormulaAsync()
        {
            if (SelectedFormula == null)
                return;

            try
            {
                var copiedFormula = new FormulaDto
                {
                    Name = $"{SelectedFormula.Name} (副本)",
                    Effect = SelectedFormula.Effect,
                    Usage = SelectedFormula.Usage,
                    IsShared = false,
                    HerbCount = SelectedFormula.HerbCount,
                    Remark = SelectedFormula.Remark
                };

                await ExecuteWithLoadingAsync(async () =>
                {
                    var result = await _formulaService.CreateFormulaAsync(copiedFormula);
                    if (result.IsSuccess)
                    {
                        await LoadDataAsync(); // 刷新列表
                        ShowSuccessMessage("验方复制成功");
                        _logger.LogInformation("验方复制成功: {FormulaName}", copiedFormula.Name);
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage;
                        _logger.LogWarning("验方复制失败: {Error}", result.ErrorMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制验方时发生错误");
                ErrorMessage = $"复制验方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 收藏验方
        /// </summary>
        private async Task FavoriteFormulaAsync()
        {
            if (SelectedFormula == null)
                return;

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    // TODO: 获取当前用户ID
                    var result = await _formulaService.FavoriteFormulaAsync(SelectedFormula.Id, Guid.NewGuid());
                    if (result.IsSuccess)
                    {
                        ShowSuccessMessage("验方收藏成功");
                        _logger.LogInformation("验方收藏成功: {FormulaName}", SelectedFormula.Name);
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage;
                        _logger.LogWarning("验方收藏失败: {Error}", result.ErrorMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "收藏验方时发生错误");
                ErrorMessage = $"收藏验方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 验证验方组成
        /// </summary>
        private async Task ValidateFormulaAsync()
        {
            if (SelectedFormula == null)
                return;

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    var result = await _formulaService.ValidateFormulaCompositionAsync(SelectedFormula);
                    if (result.IsSuccess)
                    {
                        var isValid = result.Data;
                        var message = isValid ? "验方组成合理" : "验方组成可能存在问题，建议检查药材配伍";
                        ShowSuccessMessage(message);
                        _logger.LogInformation("验方组成验证完成: {FormulaName}, 结果: {IsValid}", SelectedFormula.Name, isValid);
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage;
                        _logger.LogWarning("验方组成验证失败: {Error}", result.ErrorMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证验方组成时发生错误");
                ErrorMessage = $"验证验方组成时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 显示验方统计
        /// </summary>
        private async Task ShowStatisticsAsync()
        {
            if (SelectedFormula == null)
                return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "验方使用统计" },
                    { "FormulaId", SelectedFormula.Id },
                    { "FormulaName", SelectedFormula.Name }
                };

                _dialogService.ShowDialog("FormulaStatisticsDialog", dialogParameters, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示验方统计时发生错误");
                ErrorMessage = $"显示验方统计时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 显示经典验方
        /// </summary>
        private async Task ShowClassicFormulasAsync()
        {
            ShowSharedOnly = true;
            ShowPersonalOnly = false;
            await LoadDataAsync();
        }

        /// <summary>
        /// 显示常用验方
        /// </summary>
        private async Task ShowFrequentFormulasAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _formulaService.GetFrequentlyUsedFormulasAsync(20);
                if (result.IsSuccess)
                {
                    Items = new ObservableCollection<FormulaDto>(result.Data);
                    TotalCount = result.Data.Count;
                    TotalPages = 1;
                    ShowSuccessMessage($"显示了 {TotalCount} 个常用验方");
                    _logger.LogInformation("常用验方加载完成，共 {Count} 条记录", TotalCount);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                    _logger.LogWarning("常用验方加载失败: {Error}", result.ErrorMessage);
                }
            });
        }

        private void UpdateCommandStates()
        {
            EditFormulaCommand?.RaiseCanExecuteChanged();
            ViewFormulaCommand?.RaiseCanExecuteChanged();
            CopyFormulaCommand?.RaiseCanExecuteChanged();
            FavoriteFormulaCommand?.RaiseCanExecuteChanged();
            ValidateFormulaCommand?.RaiseCanExecuteChanged();
            ShowStatisticsCommand?.RaiseCanExecuteChanged();
        }

        private void ShowSuccessMessage(string message)
        {
            // TODO: 实现更好的成功消息提示
            MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}