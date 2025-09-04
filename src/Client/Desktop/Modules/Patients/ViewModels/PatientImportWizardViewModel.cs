using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoMapper;
using LYBT.Desktop.Core.Helpers;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Services;
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
    public class PatientImportWizardViewModel : BindableBase
    {
        #region Fields
        
        private readonly IPatientService _patientService;
        private readonly ICustomDialogService _dialogService;
        private readonly ILogger<PatientImportWizardViewModel> _logger;
        
        private ImportWizardStep _currentStep = ImportWizardStep.TemplateDownload;
        private string _selectedFilePath = string.Empty;
        private DataTable? _previewData;
        private ImportValidationResult? _validationResult;
        private BackgroundWorker? _importWorker;
        private ImportProgressInfo _progressInfo = new();
        private bool _isImporting = false;
        private bool _isLoading = false;

        #endregion

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

        #endregion

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

        #endregion

        #region Constructor

        public PatientImportWizardViewModel(
            IPatientService patientService,
            ICustomDialogService dialogService,
            ILogger<PatientImportWizardViewModel> logger)
        {
            _patientService = patientService;
            _dialogService = dialogService;
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

        #endregion

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
            
            // 关闭窗口或返回主界面
            // TODO: 实现窗口关闭逻辑
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

        #endregion

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
            } && !IsImporting;

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
                _ => ""
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
                await _dialogService.ShowErrorAsync($"加载数据失败: {ex.Message}", "数据预览");
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
                var requiredColumns = new[] { "姓名", "性别", "年龄" };
                foreach (var column in requiredColumns)
                {
                    if (!dataTable.Columns.Contains(column))
                    {
                        errors.Add($"缺少必需列: {column}");
                    }
                }

                // 验证数据行
                int validRows = 0;
                int invalidRows = 0;

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    var row = dataTable.Rows[i];
                    var rowErrors = new List<string>();

                    // 验证姓名
                    var name = row["姓名"]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        rowErrors.Add("姓名不能为空");
                    }

                    // 验证性别
                    var gender = row["性别"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(gender) && gender != "男" && gender != "女")
                    {
                        rowErrors.Add("性别只能是'男'或'女'");
                    }

                    if (rowErrors.Count > 0)
                    {
                        invalidRows++;
                        errors.Add($"第{i + 2}行: {string.Join(", ", rowErrors)}");
                    }
                    else
                    {
                        validRows++;
                    }
                }

                result.ValidRowCount = validRows;
                result.InvalidRowCount = invalidRows;
                result.IsValid = errors.Count == 0;
            }

            result.Errors = errors;
            result.Warnings = warnings;

            return result;
        }

        private async Task DownloadTemplateAsync(string filePath)
        {
            try
            {
                // 创建模板数据表
                var templateTable = new DataTable();
                
                // 添加列
                templateTable.Columns.Add("姓名", typeof(string));
                templateTable.Columns.Add("性别", typeof(string));
                templateTable.Columns.Add("年龄", typeof(int));
                templateTable.Columns.Add("电话", typeof(string));
                templateTable.Columns.Add("证件号", typeof(string));
                templateTable.Columns.Add("地址", typeof(string));
                templateTable.Columns.Add("过敏史", typeof(string));

                // 添加示例数据
                var sampleRow = templateTable.NewRow();
                sampleRow["姓名"] = "张三";
                sampleRow["性别"] = "男";
                sampleRow["年龄"] = 35;
                sampleRow["电话"] = "13800138000";
                sampleRow["证件号"] = "110101198801011234";
                sampleRow["地址"] = "北京市朝阳区";
                sampleRow["过敏史"] = "无";
                templateTable.Rows.Add(sampleRow);

                // 导出到Excel
                ExcelHelper.ExportToExcel(templateTable.AsEnumerable().ToList(), new Dictionary<string, string>(), filePath, "患者数据");

                await _dialogService.ShowSuccessAsync("模板下载成功！", "下载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载模板失败");
                throw;
            }
        }

        #endregion

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

        #endregion

        #region BackgroundWorker Events

        private async void ImportWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (e.Argument is not DataTable dataTable || _importWorker == null)
                return;

            var worker = _importWorker;
            var successCount = 0;
            var failCount = 0;
            var totalCount = dataTable.Rows.Count;

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

                    try
                    {
                        // 创建患者DTO
                        var patientDto = new PatientCreateDto
                        {
                            Name = row["姓名"]?.ToString()?.Trim() ?? "",
                            Gender = ParseGender(row["性别"]?.ToString()),
                            Age = ParseAge(row["年龄"]?.ToString()) ?? 0,
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
                        }
                        else
                        {
                            failCount++;
                            _logger.LogWarning($"导入第{i + 2}行失败: {result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError(ex, $"处理第{i + 2}行数据时发生错误");
                    }

                    // 报告进度
                    var progress = new ImportProgressInfo
                    {
                        PercentComplete = (int)((double)(i + 1) / totalCount * 100),
                        ProcessedCount = i + 1,
                        TotalCount = totalCount,
                        CurrentItem = row["姓名"]?.ToString() ?? $"第{i + 2}行",
                        Message = $"正在导入患者数据... ({i + 1}/{totalCount})"
                    };

                    worker.ReportProgress(progress.PercentComplete, progress);

                    // 模拟处理延时（可选）
                    await Task.Delay(100);
                }

                e.Result = new { SuccessCount = successCount, FailCount = failCount };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入过程中发生错误");
                e.Result = new { SuccessCount = successCount, FailCount = failCount, Error = ex.Message };
            }
        }

        private void ImportWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is ImportProgressInfo progress)
            {
                ProgressInfo = progress;
            }
        }

        private async void ImportWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            IsImporting = false;
            UpdateButtonStates();

            if (e.Cancelled)
            {
                await _dialogService.ShowInformationAsync("导入操作已取消", "导入取消");
            }
            else if (e.Error != null)
            {
                await _dialogService.ShowErrorAsync($"导入过程中发生错误: {e.Error.Message}", "导入错误");
            }
            else if (e.Result is { } result)
            {
                var successCount = (int)(result.GetType().GetProperty("SuccessCount")?.GetValue(result) ?? 0);
                var failCount = (int)(result.GetType().GetProperty("FailCount")?.GetValue(result) ?? 0);

                var message = $"导入完成！\n成功：{successCount} 条\n失败：{failCount} 条";
                
                if (failCount == 0)
                {
                    await _dialogService.ShowSuccessAsync(message, "导入成功");
                }
                else
                {
                    await _dialogService.ShowWarningAsync(message, "导入完成");
                }
            }
        }

        #endregion

        #region Helper Methods

        private Gender ParseGender(string? genderText)
        {
            return genderText?.Trim() switch
            {
                "男" => Gender.Male,
                "女" => Gender.Female,
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

        #endregion
    }
}