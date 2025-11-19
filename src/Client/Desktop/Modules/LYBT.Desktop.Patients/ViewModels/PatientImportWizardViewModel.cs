using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Helpers;
using LYBT.Desktop.Infrastructure.Interfaces; // Issue #2147: 添加ICommonDialogService命名空间
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Patients.ViewModels
{

    /// <summary>
    /// 患者Excel导入向导视图模型 - Phase 2模块化架构
    /// Issue #1114 - 直接使用Repository，去除Service层
    /// Issue #1790 - 拆分为ViewModel+Executor+DataMapper三层架构
    /// </summary>
    public class PatientImportWizardViewModel : BindableBase, IDisposable
    {

        #region Fields

        // Issue #1790: 注入Executor处理导入逻辑
        private readonly PatientImportExecutor _importExecutor;
        private readonly IExcelParserService _excelParserService;
        private readonly ILogger<PatientImportWizardViewModel> _logger;
        // Issue #2147: 注入ICommonDialogService，替代MessageBox.Show直接调用
        private readonly ICommonDialogService _dialogService;

        private ImportWizardStep _currentStep = ImportWizardStep.TemplateDownload;
        private string _selectedFilePath = string.Empty;
        private DataTable? _previewData;
        private ImportValidationResult? _validationResult;
        private ImportProgressInfo _progressInfo = new();
        private bool _isImporting = false;
        private bool _isLoading = false;

        #endregion Fields

        #region Events

        /// <summary>
        /// 导入完成事件
        /// </summary>
        public event EventHandler? ImportCompleted;

        /// <summary>
        /// 导入取消事件
        /// </summary>
        public event EventHandler? ImportCancelled;

        #endregion Events

        #region Properties

        /// <summary>
        /// 当前步骤
        /// </summary>
        public ImportWizardStep CurrentStep
        {
            get => _currentStep;
            set
            {
                SetProperty(ref _currentStep, value);
                UpdateStepStyles();
                UpdateButtonStates();
                UpdateStepContent();
            }
        }

        /// <summary>
        /// 选中的文件路径
        /// </summary>
        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set => SetProperty(ref _selectedFilePath, value);
        }

        /// <summary>
        /// 预览数据
        /// </summary>
        public DataTable? PreviewData
        {
            get => _previewData;
            set => SetProperty(ref _previewData, value);
        }

        /// <summary>
        /// 验证结果
        /// </summary>
        public ImportValidationResult? ValidationResult
        {
            get => _validationResult;
            set => SetProperty(ref _validationResult, value);
        }

        /// <summary>
        /// 导入进度信息
        /// </summary>
        public ImportProgressInfo ProgressInfo
        {
            get => _progressInfo;
            set => SetProperty(ref _progressInfo, value);
        }

        /// <summary>
        /// 是否正在导入
        /// </summary>
        public bool IsImporting
        {
            get => _isImporting;
            set => SetProperty(ref _isImporting, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 步骤1样式
        /// </summary>
        public Style Step1Style { get; private set; } = Application.Current.FindResource("PendingStep") as Style ?? new Style();

        /// <summary>
        /// 步骤2样式
        /// </summary>
        public Style Step2Style { get; private set; } = Application.Current.FindResource("PendingStep") as Style ?? new Style();

        /// <summary>
        /// 步骤3样式
        /// </summary>
        public Style Step3Style { get; private set; } = Application.Current.FindResource("PendingStep") as Style ?? new Style();

        /// <summary>
        /// 步骤4样式
        /// </summary>
        public Style Step4Style { get; private set; } = Application.Current.FindResource("PendingStep") as Style ?? new Style();

        /// <summary>
        /// 当前步骤内容
        /// </summary>
        public object? CurrentStepContent { get; private set; }

        /// <summary>
        /// 步骤描述
        /// </summary>
        public string StepDescription { get; private set; } = "第1步：下载患者数据导入模板";

        /// <summary>
        /// 下一步按钮文本
        /// </summary>
        public string NextButtonText { get; private set; } = "下一步";

        /// <summary>
        /// 是否可以进入下一步
        /// </summary>
        public bool CanGoNext { get; private set; } = true;

        /// <summary>
        /// 是否可以返回上一步
        /// </summary>
        public bool CanGoPrevious { get; private set; } = false;

        #endregion Properties

        #region Commands

        /// <summary>
        /// 下一步命令
        /// </summary>
        public ICommand NextCommand { get; }

        /// <summary>
        /// 上一步命令
        /// </summary>
        public ICommand PreviousCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 下载模板命令
        /// </summary>
        public ICommand DownloadTemplateCommand { get; }

        /// <summary>
        /// 选择文件命令
        /// </summary>
        public ICommand SelectFileCommand { get; }

        /// <summary>
        /// 开始导入命令
        /// </summary>
        public ICommand StartImportCommand { get; }

        /// <summary>
        /// 取消导入命令
        /// </summary>
        public ICommand CancelImportCommand { get; }

        #endregion Commands

        #region Constructor

        // Issue #1790: 注入PatientImportExecutor替代BackgroundWorker
        // Issue #1781 Task 8 Phase 1: 注入ExcelParserService
        // Issue #2147: 注入ICommonDialogService
        public PatientImportWizardViewModel(
            PatientImportExecutor importExecutor,
            IExcelParserService excelParserService,
            ICommonDialogService dialogService,
            ILogger<PatientImportWizardViewModel> logger)
        {
            // Issue #1790: 注入Executor
            _importExecutor = importExecutor ?? throw new ArgumentNullException(nameof(importExecutor));
            _excelParserService = excelParserService;
            _logger = logger;
            // Issue #2147: 注入ICommonDialogService
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 订阅Executor事件
            _importExecutor.ProgressChanged += OnImportProgressChanged;
            _importExecutor.ImportCompleted += OnImportCompleted;

            // 初始化命令
            NextCommand = new DelegateCommand(ExecuteNext, CanExecuteNext);
            PreviousCommand = new DelegateCommand(ExecutePrevious, CanExecutePrevious);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            DownloadTemplateCommand = new DelegateCommand(ExecuteDownloadTemplate);
            SelectFileCommand = new DelegateCommand(ExecuteSelectFile);
            StartImportCommand = new DelegateCommand(ExecuteStartImport, CanExecuteStartImport);
            CancelImportCommand = new DelegateCommand(ExecuteCancelImport, CanExecuteCancelImport);

            // 更新初始状态
            UpdateStepStyles();
            UpdateButtonStates();
            UpdateStepContent();
        }

        #endregion Constructor

        #region Command Implementations

        private void ExecuteNext()
        {
            switch (CurrentStep)
            {
                case ImportWizardStep.TemplateDownload:
                    CurrentStep = ImportWizardStep.FileSelection;
                    break;

                case ImportWizardStep.FileSelection:
                    if (ValidateFileSelection())
                    {
                        CurrentStep = ImportWizardStep.DataPreview;
                        _ = Task.Run(LoadDataPreviewAsync);
                    }

                    break;

                case ImportWizardStep.DataPreview:
                    CurrentStep = ImportWizardStep.ImportExecution;
                    break;

                case ImportWizardStep.ImportExecution:
                    // 完成向导
                    ExecuteCancel();
                    break;
            }
        }

        private bool CanExecuteNext()
        {
            return !IsImporting && CanGoNext;
        }

        private void ExecutePrevious()
        {
            switch (CurrentStep)
            {
                case ImportWizardStep.FileSelection:
                    CurrentStep = ImportWizardStep.TemplateDownload;
                    break;

                case ImportWizardStep.DataPreview:
                    CurrentStep = ImportWizardStep.FileSelection;
                    break;

                case ImportWizardStep.ImportExecution:
                    CurrentStep = ImportWizardStep.DataPreview;
                    break;
            }
        }

        private bool CanExecutePrevious()
        {
            return !IsImporting && CanGoPrevious;
        }

        private void ExecuteCancel()
        {
            if (IsImporting)
            {
                _importExecutor.CancelImport();
            }

            // 触发取消事件，让父窗口处理关闭逻辑
            ImportCancelled?.Invoke(this, EventArgs.Empty);
        }

        private async void ExecuteDownloadTemplate()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = "患者导入模板.xlsx",
                    Title = "保存患者导入模板"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await DownloadTemplateAsync(saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载模板失败");
                // Issue #2147: 替换MessageBox.Show为ICommonDialogService
                await _dialogService.ShowErrorAsync($"下载模板失败: {ex.Message}", "错误");
            }
        }

        private void ExecuteSelectFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "选择要导入的患者数据文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    SelectedFilePath = openFileDialog.FileName;
                    UpdateButtonStates();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "选择文件失败");
            }
        }

        private void ExecuteStartImport()
        {
            if (ValidationResult != null && ValidationResult.IsValid)
            {
                IsImporting = true;
                _importExecutor.StartImport(PreviewData);
                UpdateButtonStates();
            }
        }

        private bool CanExecuteStartImport()
        {
            return ValidationResult?.IsValid == true && !IsImporting;
        }

        private void ExecuteCancelImport()
        {
            _importExecutor.CancelImport();
        }

        private bool CanExecuteCancelImport()
        {
            return IsImporting;
        }

        #endregion Command Implementations

        #region Private Methods

        private void UpdateStepStyles()
        {
            var completedStyle = Application.Current.FindResource("CompletedStep") as Style ?? new Style();
            var activeStyle = Application.Current.FindResource("ActiveStep") as Style ?? new Style();
            var pendingStyle = Application.Current.FindResource("PendingStep") as Style ?? new Style();

            Step1Style = CurrentStep >= ImportWizardStep.TemplateDownload ?
                (CurrentStep == ImportWizardStep.TemplateDownload ? activeStyle : completedStyle) : pendingStyle;

            Step2Style = CurrentStep >= ImportWizardStep.FileSelection ?
                (CurrentStep == ImportWizardStep.FileSelection ? activeStyle : completedStyle) : pendingStyle;

            Step3Style = CurrentStep >= ImportWizardStep.DataPreview ?
                (CurrentStep == ImportWizardStep.DataPreview ? activeStyle : completedStyle) : pendingStyle;

            Step4Style = CurrentStep >= ImportWizardStep.ImportExecution ?
                (CurrentStep == ImportWizardStep.ImportExecution ? activeStyle : completedStyle) : pendingStyle;

            RaisePropertyChanged(nameof(Step1Style));
            RaisePropertyChanged(nameof(Step2Style));
            RaisePropertyChanged(nameof(Step3Style));
            RaisePropertyChanged(nameof(Step4Style));
        }

        private void UpdateButtonStates()
        {
            CanGoPrevious = CurrentStep != ImportWizardStep.TemplateDownload && !IsImporting;

            CanGoNext = CurrentStep switch
            {
                ImportWizardStep.TemplateDownload => true,
                ImportWizardStep.FileSelection => !string.IsNullOrEmpty(SelectedFilePath),
                ImportWizardStep.DataPreview => ValidationResult?.IsValid == true,
                ImportWizardStep.ImportExecution => false,
                _ => false
            }

&& !IsImporting;

            NextButtonText = CurrentStep switch
            {
                ImportWizardStep.ImportExecution => "完成",
                _ => "下一步"
            };

            StepDescription = CurrentStep switch
            {
                ImportWizardStep.TemplateDownload => "第1步：下载患者数据导入模板",
                ImportWizardStep.FileSelection => "第2步：选择要导入的Excel文件",
                ImportWizardStep.DataPreview => "第3步：预览和验证数据",
                ImportWizardStep.ImportExecution => "第4步：执行导入操作",
                _ => string.Empty
            };

            RaisePropertyChanged(nameof(CanGoNext));
            RaisePropertyChanged(nameof(CanGoPrevious));
            RaisePropertyChanged(nameof(NextButtonText));
            RaisePropertyChanged(nameof(StepDescription));

            // 更新命令状态
            (NextCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (PreviousCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (StartImportCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (CancelImportCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateStepContent()
        {
            CurrentStepContent = CurrentStep switch
            {
                ImportWizardStep.TemplateDownload => CreateTemplateDownloadContent(),
                ImportWizardStep.FileSelection => CreateFileSelectionContent(),
                ImportWizardStep.DataPreview => CreateDataPreviewContent(),
                ImportWizardStep.ImportExecution => CreateImportExecutionContent(),
                _ => null
            };

            RaisePropertyChanged(nameof(CurrentStepContent));
        }

        private bool ValidateFileSelection()
        {
            if (string.IsNullOrEmpty(SelectedFilePath) || !File.Exists(SelectedFilePath))
            {
                // Issue #2147: 替换MessageBox.Show为ICommonDialogService（fire-and-forget模式）
                _ = _dialogService.ShowWarningAsync("请选择有效的Excel文件", "文件选择");
                return false;
            }

            return true;
        }

        private async Task LoadDataPreviewAsync()
        {
            try
            {
                IsLoading = true;

                // Issue #1781 Task 8 Phase 1: 使用ExcelParserService解析和验证数据
                var dataTable = await _excelParserService.ParseExcelFileAsync(SelectedFilePath);
                PreviewData = dataTable;

                // 使用ExcelParserService验证数据
                ValidationResult = _excelParserService.ValidateImportData(dataTable);

                Application.Current.Dispatcher.Invoke(UpdateButtonStates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载数据预览失败");
                // Issue #2147: 替换MessageBox.Show为ICommonDialogService
                await _dialogService.ShowErrorAsync($"加载数据失败: {ex.Message}", "数据预览");
            }
            finally
            {
                IsLoading = false;
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators
        private async Task DownloadTemplateAsync(string filePath)
#pragma warning restore CS1998
        {
            try
            {
                // 定义模板列及其说明
                var columns = new[] { "姓名", "性别", "年龄", "电话", "证件号", "地址", "过敏史" };

                // 创建示例数据 - 提供多个示例以便用户理解
                var sampleData = new List<string[]>
                {
                    new[] { "张三", "男", "35", "13800138000", "110101198801011234", "北京市朝阳区建国路1号", "青霉素过敏" },
                    new[] { "李四", "女", "28", "13900139000", "110101199201020002", "北京市海淀区中关村大街2号", "无" },
                    new[] { "王五", "男", "42", "18600186000", string.Empty, "上海市浦东新区陆家嘴3号", "海鲜过敏" },
                    new[] { "赵六", "女", string.Empty, "15300153000", "310101198103150004", "广州市天河区珠江新城4号", "花粉过敏" },
                    new[] { "钱七", "未知", "65", string.Empty, string.Empty, "深圳市南山区科技园5号", "无" }
                };

                // 创建Excel模板
                ExcelHelper.CreateTemplate(columns, filePath, "患者数据导入模板", sampleData);

                var successMessage = $"患者导入模板已成功保存到:\n{filePath}\n\n" +
                    "模板说明：\n" +
                    "• 必填字段：姓名、性别\n" +
                    "• 选填字段：年龄、电话、证件号、地址、过敏史\n" +
                    "• 性别填写：男、女、未知\n" +
                    "• 年龄范围：0-150之间的整数\n" +
                    "• 电话格式：支持数字、横线、加号、括号\n" +
                    "• 证件号：建议15位或18位\n\n" +
                    "请按照模板格式填写患者数据，然后使用导入功能。";

                // Issue #2147: 替换MessageBox.Show为ICommonDialogService
                await _dialogService.ShowInfoAsync(successMessage, "模板下载成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载模板失败: {FilePath}", filePath);
                // Issue #2147: 替换MessageBox.Show为ICommonDialogService
                await _dialogService.ShowErrorAsync($"下载模板失败: {ex.Message}\n\n请检查文件路径是否正确，或选择其他保存位置。", "下载失败");
                throw;
            }
        }

        #endregion Private Methods

        #region Step Content Creation

        private object CreateTemplateDownloadContent()
        {
            return new { StepTitle = "下载导入模板", StepDescription = "请先下载Excel模板文件" };
        }

        private object CreateFileSelectionContent()
        {
            return new { StepTitle = "选择文件", StepDescription = "选择要导入的Excel文件", FilePath = SelectedFilePath };
        }

        private object CreateDataPreviewContent()
        {
            return new { StepTitle = "数据预览", PreviewData, ValidationResult };
        }

        private object CreateImportExecutionContent()
        {
            return new { StepTitle = "导入执行", ProgressInfo, IsImporting };
        }

        #endregion Step Content Creation

        #region Event Handlers

        /// <summary>
        /// 处理导入进度变化
        /// Issue #1790: 从Executor接收进度更新
        /// </summary>
        private void OnImportProgressChanged(object? sender, ImportProgressInfo progress)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressInfo = progress;
            });
        }

        /// <summary>
        /// 处理导入完成
        /// Issue #1790: 从Executor接收完成通知
        /// </summary>
        private void OnImportCompleted(object? sender, ImportCompletedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsImporting = false;
                UpdateButtonStates();

                // Issue #2147: 替换MessageBox.Show为ICommonDialogService（根据MessageType动态选择方法）
                _ = e.MessageType switch
                {
                    MessageBoxImage.Error => _dialogService.ShowErrorAsync(e.Message, e.Title),
                    MessageBoxImage.Warning => _dialogService.ShowWarningAsync(e.Message, e.Title),
                    _ => _dialogService.ShowInfoAsync(e.Message, e.Title)
                };

                // 只有在有成功导入的情况下才触发刷新事件
                if (e.SuccessCount > 0)
                {
                    ImportCompleted?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        #endregion Event Handlers

        #region Dispose

        public void Dispose()
        {
            // 取消订阅事件
            _importExecutor.ProgressChanged -= OnImportProgressChanged;
            _importExecutor.ImportCompleted -= OnImportCompleted;

            // 清理 Executor
            _importExecutor?.Dispose();

            // 清理 DataTable
            _previewData?.Dispose();
            _previewData = null;
        }

        #endregion Dispose
    }
}
