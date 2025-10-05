using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Helpers;
using LYBT.Desktop.Patients.Models;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Patients.ViewModels
{

    /// <summary>
    /// 患者Excel导入向导视图模型
    /// 实现4步向导UI：模板下载→文件选择→数据预览→导入执行
    /// </summary>
    public class PatientImportWizardViewModel : BindableBase, IDisposable
    {

        #region Fields

        private readonly IPatientService _patientService;
        private readonly ILogger<PatientImportWizardViewModel> _logger;

        private ImportWizardStep _currentStep = ImportWizardStep.TemplateDownload;
        private string _selectedFilePath = string.Empty;
        private DataTable? _previewData;
        private ImportValidationResult? _validationResult;
        private BackgroundWorker? _importWorker;
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

        public PatientImportWizardViewModel(
            IPatientService patientService,
            ILogger<PatientImportWizardViewModel> logger)
        {
            _patientService = patientService;
            _logger = logger;

            // 初始化命令
            NextCommand = new DelegateCommand(ExecuteNext, CanExecuteNext);
            PreviousCommand = new DelegateCommand(ExecutePrevious, CanExecutePrevious);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            DownloadTemplateCommand = new DelegateCommand(ExecuteDownloadTemplate);
            SelectFileCommand = new DelegateCommand(ExecuteSelectFile);
            StartImportCommand = new DelegateCommand(ExecuteStartImport, CanExecuteStartImport);
            CancelImportCommand = new DelegateCommand(ExecuteCancelImport, CanExecuteCancelImport);

            // 初始化BackgroundWorker
            InitializeImportWorker();

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
                _importWorker?.CancelAsync();
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
                MessageBox.Show($"下载模板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (ValidationResult != null && ValidationResult.IsValid && _importWorker != null)
            {
                IsImporting = true;
                _importWorker.RunWorkerAsync(PreviewData);
                UpdateButtonStates();
            }
        }

        private bool CanExecuteStartImport()
        {
            return ValidationResult?.IsValid == true && !IsImporting;
        }

        private void ExecuteCancelImport()
        {
            _importWorker?.CancelAsync();
        }

        private bool CanExecuteCancelImport()
        {
            return IsImporting;
        }

        #endregion Command Implementations

        #region Private Methods

        private void InitializeImportWorker()
        {
            _importWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            _importWorker.DoWork += ImportWorker_DoWork;
            _importWorker.ProgressChanged += ImportWorker_ProgressChanged;
            _importWorker.RunWorkerCompleted += ImportWorker_RunWorkerCompleted;
        }

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
            // TODO: 根据当前步骤更新内容视图
            // 这里可以根据CurrentStep返回不同的UserControl或View
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
                MessageBox.Show("请选择有效的Excel文件", "文件选择", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators
        private async Task LoadDataPreviewAsync()
#pragma warning restore CS1998
        {
            try
            {
                IsLoading = true;

                // 读取Excel数据
                var dataTable = ExcelHelper.ImportFromExcel(SelectedFilePath, true);
                PreviewData = dataTable;

                // 验证数据
                ValidationResult = ValidateImportData(dataTable);

                Application.Current.Dispatcher.Invoke(UpdateButtonStates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载数据预览失败");
                MessageBox.Show($"加载数据失败: {ex.Message}", "数据预览", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private ImportValidationResult ValidateImportData(DataTable dataTable)
        {
            var result = new ImportValidationResult();
            var errors = new List<string>();
            var warnings = new List<string>();

            if (dataTable.Rows.Count == 0)
            {
                errors.Add("Excel文件中没有找到数据行");
                result.IsValid = false;
            }
            else
            {
                // 检查必需列
                var requiredColumns = new[] { "姓名", "性别" };
                var optionalColumns = new[] { "年龄", "电话", "证件号", "地址", "过敏史" };

                foreach (var column in requiredColumns)
                {
                    if (!dataTable.Columns.Contains(column))
                    {
                        errors.Add($"缺少必需列: {column}");
                    }
                }

                // 检查列格式并给出提示
                var allExpectedColumns = requiredColumns.Concat(optionalColumns).ToArray();
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (!allExpectedColumns.Contains(column.ColumnName))
                    {
                        warnings.Add($"未识别的列: {column.ColumnName}，此列数据将被忽略");
                    }
                }

                // 验证数据行
                int validRows = 0;
                int invalidRows = 0;
                var duplicateNames = new HashSet<string>();
                var phoneNumbers = new HashSet<string>();
                var idNumbers = new HashSet<string>();

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    var row = dataTable.Rows[i];
                    var rowErrors = new List<string>();
                    var rowWarnings = new List<string>();

                    // 检查是否为空行
                    bool isEmptyRow = true;
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        if (!string.IsNullOrWhiteSpace(row[col]?.ToString()))
                        {
                            isEmptyRow = false;
                            break;
                        }
                    }

                    if (isEmptyRow)
                    {
                        rowWarnings.Add("空行，将被跳过");
                        continue;
                    }

                    // 验证姓名
                    var name = row["姓名"]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        rowErrors.Add("姓名不能为空");
                    }
                    else if (name.Length > 50)
                    {
                        rowErrors.Add("姓名长度不能超过50个字符");
                    }
                    else if (duplicateNames.Contains(name))
                    {
                        rowWarnings.Add($"姓名'{name}'重复，请确认是否为同一人");
                    }
                    else
                    {
                        duplicateNames.Add(name);
                    }

                    // 验证性别
                    var gender = row["性别"]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(gender))
                    {
                        rowErrors.Add("性别不能为空");
                    }
                    else if (gender != "男" && gender != "女" && gender != "未知")
                    {
                        rowErrors.Add("性别只能是'男'、'女'或'未知'");
                    }

                    // 验证年龄（可选）
                    var ageText = row["年龄"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(ageText))
                    {
                        if (!int.TryParse(ageText, out var age) || age < 0 || age > 150)
                        {
                            rowErrors.Add("年龄必须是0-150之间的整数");
                        }
                    }

                    // 验证电话（可选）
                    var phone = row["电话"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(phone))
                    {
                        if (phone.Length < 7 || phone.Length > 15)
                        {
                            rowErrors.Add("电话号码长度应在7-15位之间");
                        }
                        else if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[0-9\-\+\(\)\s]+$"))
                        {
                            rowErrors.Add("电话号码格式不正确，只能包含数字、横线、加号、括号和空格");
                        }
                        else if (phoneNumbers.Contains(phone))
                        {
                            rowWarnings.Add($"电话号码'{phone}'重复");
                        }
                        else
                        {
                            phoneNumbers.Add(phone);
                        }
                    }

                    // 验证证件号（可选）
                    var idNumber = row["证件号"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(idNumber))
                    {
                        if (idNumber.Length != 18 && idNumber.Length != 15)
                        {
                            rowWarnings.Add("证件号长度不是标准的15位或18位，请确认");
                        }
                        else if (idNumbers.Contains(idNumber))
                        {
                            rowErrors.Add($"证件号'{idNumber}'重复，不能导入重复证件号");
                        }
                        else
                        {
                            idNumbers.Add(idNumber);
                        }
                    }

                    // 验证地址（可选）
                    var address = row["地址"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(address) && address.Length > 200)
                    {
                        rowErrors.Add("地址长度不能超过200个字符");
                    }

                    // 验证过敏史（可选）
                    var allergy = row["过敏史"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(allergy) && allergy.Length > 500)
                    {
                        rowErrors.Add("过敏史长度不能超过500个字符");
                    }

                    // 统计结果
                    if (rowErrors.Count > 0)
                    {
                        invalidRows++;
                        errors.Add($"第{i + 2}行: {string.Join("; ", rowErrors)}");
                    }
                    else
                    {
                        validRows++;
                        if (rowWarnings.Count > 0)
                        {
                            warnings.Add($"第{i + 2}行: {string.Join("; ", rowWarnings)}");
                        }
                    }
                }

                result.ValidRowCount = validRows;
                result.InvalidRowCount = invalidRows;
                result.IsValid = errors.Count == 0 && validRows > 0;

                // 添加汇总信息
                if (validRows > 0 && invalidRows == 0)
                {
                    warnings.Add($"验证通过，共{validRows}行有效数据可以导入");
                }
                else if (validRows > 0 && invalidRows > 0)
                {
                    warnings.Add($"部分验证通过，{validRows}行有效数据可以导入，{invalidRows}行数据有错误将被跳过");
                }
            }

            result.Errors = errors;
            result.Warnings = warnings;

            return result;
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

                MessageBox.Show(successMessage, "模板下载成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载模板失败: {FilePath}", filePath);
                MessageBox.Show($"下载模板失败: {ex.Message}\n\n请检查文件路径是否正确，或选择其他保存位置。", "下载失败", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        #endregion Private Methods

        #region Step Content Creation

        private object CreateTemplateDownloadContent()
        {
            // TODO: 返回步骤1的具体UI内容
            return new { StepTitle = "下载导入模板", StepDescription = "请先下载Excel模板文件" };
        }

        private object CreateFileSelectionContent()
        {
            // TODO: 返回步骤2的具体UI内容
            return new { StepTitle = "选择文件", StepDescription = "选择要导入的Excel文件", FilePath = SelectedFilePath };
        }

        private object CreateDataPreviewContent()
        {
            // TODO: 返回步骤3的具体UI内容
            return new { StepTitle = "数据预览", PreviewData, ValidationResult };
        }

        private object CreateImportExecutionContent()
        {
            // TODO: 返回步骤4的具体UI内容
            return new { StepTitle = "导入执行", ProgressInfo, IsImporting };
        }

        #endregion Step Content Creation

        #region BackgroundWorker Events

        private async void ImportWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (e.Argument is not DataTable dataTable || _importWorker == null)
            {
                return;
            }

            var worker = _importWorker;
            var successCount = 0;
            var failCount = 0;
            var skipCount = 0;
            var totalCount = dataTable.Rows.Count;
            var errors = new List<string>();

            try
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    var row = dataTable.Rows[i];
                    var currentName = row["姓名"]?.ToString()?.Trim() ?? $"第{i + 2}行";

                    try
                    {
                        // 检查是否为空行
                        bool isEmptyRow = true;
                        foreach (DataColumn col in dataTable.Columns)
                        {
                            if (!string.IsNullOrWhiteSpace(row[col]?.ToString()))
                            {
                                isEmptyRow = false;
                                break;
                            }
                        }

                        if (isEmptyRow)
                        {
                            skipCount++;
                            _logger.LogInformation($"跳过空行: 第{i + 2}行");
                            continue;
                        }

                        // 验证必需字段
                        var name = row["姓名"]?.ToString()?.Trim();
                        var gender = row["性别"]?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(name))
                        {
                            failCount++;
                            var error = $"第{i + 2}行：姓名不能为空";
                            errors.Add(error);
                            _logger.LogWarning(error);
                            continue;
                        }

                        if (string.IsNullOrEmpty(gender) || (gender != "男" && gender != "女" && gender != "未知"))
                        {
                            failCount++;
                            var error = $"第{i + 2}行 ({name})：性别格式错误，应为'男'、'女'或'未知'";
                            errors.Add(error);
                            _logger.LogWarning(error);
                            continue;
                        }

                        // 创建患者DTO
                        var age = ParseAge(row["年龄"]?.ToString()) ?? 0;
                        var patientDto = new PatientCreateDto
                        {
                            Name = name,
                            Gender = ParseGender(gender),
                            BirthDate = age > 0 ? DateTime.Today.AddYears(-age) : null,
                            PhoneNumber = row["电话"]?.ToString()?.Trim(),
                            IdNumber = row["证件号"]?.ToString()?.Trim(),
                            Address = row["地址"]?.ToString()?.Trim(),
                            AllergyHistory = row["过敏史"]?.ToString()?.Trim()
                        };

                        // 调用API创建患者
                        var result = await _patientService.CreateAsync(patientDto);
                        if (result.IsSuccess)
                        {
                            successCount++;
                            _logger.LogInformation($"成功导入患者: {name} (第{i + 2}行)");
                        }
                        else
                        {
                            failCount++;
                            var error = $"第{i + 2}行 ({name})：{result.ErrorMessage ?? "导入失败，原因未知"}";
                            errors.Add(error);
                            _logger.LogWarning(error);
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        var error = $"第{i + 2}行 ({currentName})：处理数据时发生异常 - {ex.Message}";
                        errors.Add(error);
                        _logger.LogError(ex, $"处理第{i + 2}行数据时发生错误: {currentName}");
                    }

                    // 报告进度
                    var progress = new ImportProgressInfo
                    {
                        PercentComplete = (int)((double)(i + 1) / totalCount * 100),
                        ProcessedCount = i + 1,
                        TotalCount = totalCount,
                        CurrentItem = currentName,
                        Message = $"正在导入患者数据... ({i + 1}/{totalCount})"
                    };

                    worker.ReportProgress(progress.PercentComplete, progress);

                    // 适当的处理延时，避免过快处理导致界面无法响应
                    await Task.Delay(50);
                }

                e.Result = new
                {
                    SuccessCount = successCount,
                    FailCount = failCount,
                    SkipCount = skipCount,
                    Errors = errors,
                    TotalProcessed = successCount + failCount + skipCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入过程中发生严重错误");
                e.Result = new
                {
                    SuccessCount = successCount,
                    FailCount = failCount,
                    SkipCount = skipCount,
                    Error = ex.Message,
                    Errors = errors,
                    TotalProcessed = successCount + failCount + skipCount
                };
            }
        }

        private void ImportWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is ImportProgressInfo progress)
            {
                ProgressInfo = progress;
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators
        private async void ImportWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
#pragma warning restore CS1998
        {
            IsImporting = false;
            UpdateButtonStates();

            if (e.Cancelled)
            {
                MessageBox.Show("导入操作已取消\n已处理的数据未被保存。", "导入取消", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (e.Error != null)
            {
                MessageBox.Show($"导入过程中发生严重错误:\n{e.Error.Message}\n\n请检查Excel文件格式是否正确，或联系技术支持。", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.Result is { } result)
            {
                var successCount = (int)(result.GetType().GetProperty("SuccessCount")?.GetValue(result) ?? 0);
                var failCount = (int)(result.GetType().GetProperty("FailCount")?.GetValue(result) ?? 0);
                var skipCount = (int)(result.GetType().GetProperty("SkipCount")?.GetValue(result) ?? 0);
                var totalProcessed = (int)(result.GetType().GetProperty("TotalProcessed")?.GetValue(result) ?? 0);
                var errors = result.GetType().GetProperty("Errors")?.GetValue(result) as List<string> ?? new List<string>();
                var generalError = result.GetType().GetProperty("Error")?.GetValue(result) as string;

                // 构建详细的结果消息
                var messageBuilder = new System.Text.StringBuilder();
                messageBuilder.AppendLine("患者数据导入完成！");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"总计处理：{totalProcessed} 条");
                messageBuilder.AppendLine($"成功导入：{successCount} 条");
                if (failCount > 0)
                    messageBuilder.AppendLine($"导入失败：{failCount} 条");
                if (skipCount > 0)
                    messageBuilder.AppendLine($"跳过空行：{skipCount} 条");

                // 如果有具体错误，显示前几个错误详情
                if (errors.Count > 0)
                {
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine("失败详情：");
                    var errorCount = Math.Min(5, errors.Count); // 最多显示5个错误
                    for (int i = 0; i < errorCount; i++)
                    {
                        messageBuilder.AppendLine($"• {errors[i]}");
                    }

                    if (errors.Count > 5)
                    {
                        messageBuilder.AppendLine($"• ...还有{errors.Count - 5}个错误，请查看日志获取详细信息");
                    }
                }

                if (!string.IsNullOrEmpty(generalError))
                {
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine($"其他错误：{generalError}");
                }

                var message = messageBuilder.ToString();

                // 根据结果选择对话框类型
                if (successCount > 0 && failCount == 0)
                {
                    // 完全成功
                    MessageBox.Show(message, "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (successCount > 0 && failCount > 0)
                {
                    // 部分成功
                    MessageBox.Show(message, "导入部分成功", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (successCount == 0 && failCount > 0)
                {
                    // 完全失败
                    MessageBox.Show(message, "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // 异常情况
                    MessageBox.Show(message, "导入结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // 只有在有成功导入的情况下才触发刷新事件
                if (successCount > 0)
                {
                    ImportCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        #endregion BackgroundWorker Events

        #region Helper Methods

        private Gender ParseGender(string? genderText)
        {
            return genderText?.Trim() switch
            {
                "男" => Gender.Male,
                "女" => Gender.Female,
                "未知" => Gender.Unknown,
                _ => Gender.Unknown
            };
        }

        private int? ParseAge(string? ageText)
        {
            if (int.TryParse(ageText, out var age) && age > 0 && age <= 150)
            {
                return age;
            }

            return null;
        }

        public void Dispose()
        {
            // 清理 BackgroundWorker
            if (_importWorker != null)
            {
                _importWorker.Dispose();
                _importWorker = null;
            }

            // 清理 DataTable
            _previewData?.Dispose();
            _previewData = null;
        }

        #endregion Helper Methods
    }
}
