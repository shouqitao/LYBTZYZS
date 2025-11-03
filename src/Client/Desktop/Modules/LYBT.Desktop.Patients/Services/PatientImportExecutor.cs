using System.ComponentModel;
using System.Data;
using System.Windows;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.ViewModels.Components;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者导入执行器 - 负责BackgroundWorker导入逻辑
/// Issue #1790: 从PatientImportWizardViewModel提取导入执行逻辑(~350行)
/// </summary>
public class PatientImportExecutor : IDisposable
{
    private readonly PatientCommandHandler _commandHandler;
    private readonly PatientImportDataMapper _dataMapper;
    private readonly ILogger<PatientImportExecutor> _logger;
    private BackgroundWorker? _importWorker;

    /// <summary>
    /// 导入进度变化事件
    /// </summary>
    public event EventHandler<ImportProgressInfo>? ProgressChanged;

    /// <summary>
    /// 导入完成事件
    /// </summary>
    public event EventHandler<ImportCompletedEventArgs>? ImportCompleted;

    /// <summary>
    /// 是否正在导入
    /// </summary>
    public bool IsImporting { get; private set; }

    public PatientImportExecutor(
        PatientCommandHandler commandHandler,
        PatientImportDataMapper dataMapper,
        ILogger<PatientImportExecutor> logger)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _dataMapper = dataMapper ?? throw new ArgumentNullException(nameof(dataMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeImportWorker();
    }

    /// <summary>
    /// 初始化BackgroundWorker
    /// Issue #1790: 从PatientImportWizardViewModel提取
    /// </summary>
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

    /// <summary>
    /// 开始导入
    /// </summary>
    public void StartImport(DataTable? previewData)
    {
        if (previewData != null && _importWorker != null && !IsImporting)
        {
            IsImporting = true;
            _importWorker.RunWorkerAsync(previewData);
        }
    }

    /// <summary>
    /// 取消导入
    /// </summary>
    public void CancelImport()
    {
        _importWorker?.CancelAsync();
    }

    /// <summary>
    /// BackgroundWorker执行导入
    /// Issue #1790: 从PatientImportWizardViewModel提取
    /// </summary>
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

                var rowResult = await ProcessSingleImportRow(row, i, dataTable.Columns);

                successCount += rowResult.Success ? 1 : 0;
                failCount += rowResult.Failed ? 1 : 0;
                skipCount += rowResult.Skipped ? 1 : 0;

                if (rowResult.Error != null)
                    errors.Add(rowResult.Error);

                ReportImportProgress(worker, i, totalCount, currentName);
                await Task.Delay(50);
            }

            e.Result = CreateImportResult(successCount, failCount, skipCount, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入过程中发生严重错误");
            e.Result = CreateImportResult(successCount, failCount, skipCount, errors, ex.Message);
        }
    }

    /// <summary>
    /// 处理单行导入数据
    /// Issue #1789: 从ImportWorker_DoWork提取，封装单行处理逻辑
    /// </summary>
    private async Task<ImportRowResult> ProcessSingleImportRow(DataRow row, int rowIndex, DataColumnCollection columns)
    {
        var currentName = row["姓名"]?.ToString()?.Trim() ?? $"第{rowIndex + 2}行";

        try
        {
            if (_dataMapper.IsImportRowEmpty(row, columns))
            {
                _logger.LogInformation($"跳过空行: 第{rowIndex + 2}行");
                return ImportRowResult.CreateSkip();
            }

            var validationError = _dataMapper.ValidateImportRequiredFields(row, rowIndex);
            if (validationError != null)
            {
                _logger.LogWarning(validationError);
                return ImportRowResult.CreateFail(validationError);
            }

            var patientDto = _dataMapper.CreatePatientDtoFromRow(row);
            var result = await _commandHandler.CreatePatientAsync(patientDto);

            if (result.IsSuccess && result.Data != null)
            {
                _logger.LogInformation($"成功导入患者: {currentName} (第{rowIndex + 2}行)");
                return ImportRowResult.CreateSuccess();
            }
            else
            {
                var error = $"第{rowIndex + 2}行 ({currentName})：创建失败 - {result.ErrorMessage}";
                _logger.LogWarning(error);
                return ImportRowResult.CreateFail(error);
            }
        }
        catch (Exception ex)
        {
            var error = $"第{rowIndex + 2}行 ({currentName})：处理数据时发生异常 - {ex.Message}";
            _logger.LogError(ex, $"处理第{rowIndex + 2}行数据时发生错误: {currentName}");
            return ImportRowResult.CreateFail(error);
        }
    }

    /// <summary>
    /// 报告导入进度
    /// Issue #1789: 从ImportWorker_DoWork提取，封装进度报告逻辑
    /// </summary>
    private static void ReportImportProgress(BackgroundWorker worker, int currentIndex, int totalCount, string currentName)
    {
        var progress = new ImportProgressInfo
        {
            PercentComplete = (int)((double)(currentIndex + 1) / totalCount * 100),
            ProcessedCount = currentIndex + 1,
            TotalCount = totalCount,
            CurrentItem = currentName,
            Message = $"正在导入患者数据... ({currentIndex + 1}/{totalCount})"
        };

        worker.ReportProgress(progress.PercentComplete, progress);
    }

    /// <summary>
    /// 创建导入结果对象
    /// Issue #1789: 从ImportWorker_DoWork提取，封装结果创建逻辑
    /// </summary>
    private static object CreateImportResult(int successCount, int failCount, int skipCount, List<string> errors, string? errorMessage = null)
    {
        var result = new
        {
            SuccessCount = successCount,
            FailCount = failCount,
            SkipCount = skipCount,
            Errors = errors,
            TotalProcessed = successCount + failCount + skipCount
        };

        if (errorMessage != null)
        {
            return new
            {
                result.SuccessCount,
                result.FailCount,
                result.SkipCount,
                Error = errorMessage,
                result.Errors,
                result.TotalProcessed
            };
        }

        return result;
    }

    /// <summary>
    /// 导入行处理结果
    /// Issue #1789: ProcessSingleImportRow的返回类型
    /// </summary>
    private readonly struct ImportRowResult
    {
        public bool Success { get; }
        public bool Failed { get; }
        public bool Skipped { get; }
        public string? Error { get; }

        private ImportRowResult(bool success, bool failed, bool skipped, string? error)
        {
            Success = success;
            Failed = failed;
            Skipped = skipped;
            Error = error;
        }

        public static ImportRowResult CreateSuccess() => new(true, false, false, null);
        public static ImportRowResult CreateFail(string error) => new(false, true, false, error);
        public static ImportRowResult CreateSkip() => new(false, false, true, null);
    }

    /// <summary>
    /// 进度变化处理
    /// Issue #1790: 从PatientImportWizardViewModel提取
    /// </summary>
    private void ImportWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        if (e.UserState is ImportProgressInfo progress)
        {
            ProgressChanged?.Invoke(this, progress);
        }
    }

    /// <summary>
    /// 导入完成处理
    /// Issue #1790: 从PatientImportWizardViewModel提取
    /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators
    private async void ImportWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
#pragma warning restore CS1998
    {
        IsImporting = false;

        if (e.Cancelled)
        {
            ImportCompleted?.Invoke(this, new ImportCompletedEventArgs
            {
                Cancelled = true,
                Message = "导入操作已取消\n已处理的数据未被保存。",
                Title = "导入取消",
                MessageType = MessageBoxImage.Information
            });
        }
        else if (e.Error != null)
        {
            ImportCompleted?.Invoke(this, new ImportCompletedEventArgs
            {
                Error = e.Error,
                Message = $"导入过程中发生严重错误:\n{e.Error.Message}\n\n请检查Excel文件格式是否正确，或联系技术支持。",
                Title = "导入错误",
                MessageType = MessageBoxImage.Error
            });
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
            MessageBoxImage messageType;
            string title;

            if (successCount > 0 && failCount == 0)
            {
                // 完全成功
                messageType = MessageBoxImage.Information;
                title = "导入成功";
            }
            else if (successCount > 0 && failCount > 0)
            {
                // 部分成功
                messageType = MessageBoxImage.Warning;
                title = "导入部分成功";
            }
            else if (successCount == 0 && failCount > 0)
            {
                // 完全失败
                messageType = MessageBoxImage.Error;
                title = "导入失败";
            }
            else
            {
                // 异常情况
                messageType = MessageBoxImage.Information;
                title = "导入结果";
            }

            ImportCompleted?.Invoke(this, new ImportCompletedEventArgs
            {
                SuccessCount = successCount,
                FailCount = failCount,
                SkipCount = skipCount,
                TotalProcessed = totalProcessed,
                Message = message,
                Title = title,
                MessageType = messageType
            });
        }
    }

    public void Dispose()
    {
        if (_importWorker != null)
        {
            _importWorker.Dispose();
            _importWorker = null;
        }
    }
}

/// <summary>
/// 导入完成事件参数
/// Issue #1790: 封装导入完成事件数据
/// </summary>
public class ImportCompletedEventArgs : EventArgs
{
    public bool Cancelled { get; set; }
    public Exception? Error { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public int SkipCount { get; set; }
    public int TotalProcessed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public MessageBoxImage MessageType { get; set; }
}
