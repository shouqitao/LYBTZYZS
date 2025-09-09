using System.Collections.ObjectModel;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Interfaces.Services;

// UltraThink v2.0: 直接使用FormulaDto，移除Info模型引用
using LYBT.Shared.Models.Contracts.Formula;

// // using Prism.Dialogs; // Removed for Prism 8.1.97 compatibility // Temporarily disabled due to Prism 9 compatibility
using Prism.Commands;

namespace LYBT.Desktop.Prescriptions.ViewModels
{

    /// <summary>
    /// 验方模板选择对话框视图模型
    /// </summary>
    /// <summary>
    /// 验方模板选择对话框ViewModel - UltraThink架构统一
    /// </summary>
    public class FormulaTemplateDialogViewModel : DialogViewModelBase
    {
        private readonly IFormulaService _formulaService;
        private ObservableCollection<FormulaDto> _availableTemplates = new();
        private FormulaDto? _selectedTemplate;
        private string _searchText = string.Empty;

        /// <summary>
        /// 可选择的验方模板列表
        /// </summary>
        public ObservableCollection<FormulaDto> AvailableTemplates
        {
            get => _availableTemplates;
            set => SetProperty(ref _availableTemplates, value);
        }

        /// <summary>
        /// 选中的验方模板
        /// </summary>
        public FormulaDto? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                SetProperty(ref _selectedTemplate, value);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; } = null!;

        /// <summary>
        /// 查看详情命令
        /// </summary>
        public DelegateCommand<FormulaDto> ViewDetailsCommand { get; } = null!;

        /// <summary>
        /// 选中的验方模板（用于返回结果）
        /// </summary>
        public FormulaDto? Result { get; private set; }

        public FormulaTemplateDialogViewModel(IFormulaService formulaService) : base()
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            Title = "选择验方模板";

            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            ViewDetailsCommand = new DelegateCommand<FormulaDto>(ExecuteViewDetails);

            // 初始化加载验方模板列表
            _ = LoadTemplatesAsync();
        }

        /// <summary>
        /// 加载验方模板列表
        /// </summary>
        private async Task LoadTemplatesAsync()
        {
            try
            {
                IsLoading = true;
                var result = await _formulaService.GetPagedAsync(new FormulaQueryDto { PageSize = 100 });
                if (result.IsSuccess && result.Data != null)
                {
                    AvailableTemplates = new ObservableCollection<FormulaDto>(result.Data.Items);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("加载验方模板列表", ex);
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
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadTemplatesAsync();
                return;
            }

            try
            {
                IsLoading = true;
                var result = await _formulaService.GetPagedAsync(new FormulaQueryDto
                {
                    Name = SearchText,
                    PageSize = 100
                });

                if (result.IsSuccess && result.Data != null)
                {
                    AvailableTemplates = new ObservableCollection<FormulaDto>(result.Data.Items);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("搜索验方模板", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 查看验方详情
        /// </summary>
        private void ExecuteViewDetails(FormulaDto? formula)
        {
            if (formula == null)
            {
                return;
            }

            StatusMessage = string.Format(SystemConstants.FeaturePendingTemplate, $"查看验方 '{formula.Name}' 详情");
        }

        /// <summary>
        /// 执行确认逻辑
        /// </summary>
        protected override Task<bool> ExecuteConfirmAsync()
        {
            if (SelectedTemplate == null)
            {
                return Task.FromResult(false);
            }

            Result = SelectedTemplate;
            return Task.FromResult(true);
        }

        /// <summary>
        /// 检查是否可以确认
        /// </summary>
        protected override bool CanConfirm()
        {
            return !IsLoading && SelectedTemplate != null;
        }
    }
}
