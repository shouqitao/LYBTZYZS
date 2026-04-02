using System.ComponentModel;
using System.Data;
using System.Windows;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Patients.Models;
// OpenSpec: refactor-frontend-srp-patterns - PatientService已迁移到Services命名空间
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者导入执行器 - 负责异步导入逻辑
/// Issue #1790: 从PatientImportWizardViewModel提取导入执行逻辑(~350行)
/// Phase 1.2: 重构为Task-based异步模式 (移除BackgroundWorker)
/// </summary>
public class PatientImportExecutor : IDisposable
{
    private readonly PatientService _commandHandler;
    private readonly PatientImportDataMapper _dataMapper;
    private readonly ILogger<PatientImportExecutor> _logger;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// 是否正在导入
    /// </summary>
    public bool IsImporting { get; private set; }

    public PatientImportExecutor(
        PatientService commandHandler,
        PatientImportDataMapper dataMapper,
        ILogger<PatientImportExecutor> logger)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _dataMapper = dataMapper ?? throw new ArgumentNullException(nameof(dataMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 开始导入 (Task-based异步模式)
    /// </summary>
    public async Task<ImportResult> StartImportAsync(
        DataTable previewData,
        IProgress<ImportProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (previewData == null)
            throw new ArgumentNullException(nameof(previewData));

        if (IsImporting)
            throw new InvalidOperationException("导入操作正在进行中");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsImporting = true;

        try
        {
            return await ExecuteImportAsync(previewData, progress, _cts.Token);
        }
        finally
        {
            IsImporting = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    /// 执行导入逻辑
    /// Phase 1.2: 从ImportWorker_DoWork重构
    /// </summary>
    private async Task<ImportResult> ExecuteImportAsync(
        DataTable dataTable,
        IProgress<ImportProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        var failCount = 0;
        var skipCount = 0;
        var totalCount = dataTable.Rows.Count;
        var errors = new List<string>();

        try
        {
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = dataTable.Rows[i];
                var currentName = row["姓名"]?.ToString()?.Trim() ?? $"第{i + 2}行";

                var rowResult = await ProcessSingleImportRow(row, i, dataTable.Columns, cancellationToken);

                successCount += rowResult.Success ? 1 : 0;
                failCount += rowResult.Failed ? 1 : 0;
                skipCount += rowResult.Skipped ? 1 : 0;

                if (rowResult.Error != null)
                    errors.Add(rowResult.Error);

                ReportImportProgress(progress, i, totalCount, currentName);
                await Task.Delay(50, cancellationToken);
            }

            return CreateImportResult(successCount, failCount, skipCount, errors);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("导入操作已取消");
            return CreateImportResult(successCount, failCount, skipCount, errors, "导入操作已取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入过程中发生严重错误");
            return CreateImportResult(successCount, failCount, skipCount, errors, ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入患者", ex));
        }
    }

    /// <summary>
    /// 处理单行导入数据
    /// Issue #1789: 从ImportWorker_DoWork提取，封装单行处理逻辑
    /// Phase 1.2: 添加CancellationToken参数
    /// </summary>
    private async Task<ImportRowResult> ProcessSingleImportRow(
        DataRow row,
        int rowIndex,
        DataColumnCollection columns,
        CancellationToken cancellationToken)
    {
        var currentName = row["姓名"]?.ToString()?.Trim() ?? $"第{rowIndex + 2}行";

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

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

            if (result.Success && result.Data != null)
            {
                _logger.LogInformation($"成功导入患者: {currentName} (第{rowIndex + 2}行)");
                return ImportRowResult.CreateSuccess();
            }
            else
            {
                var error = $"第{rowIndex + 2}行 ({currentName})：创建失败 - {result.Error}";
                _logger.LogWarning(error);
                return ImportRowResult.CreateFail(error);
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw to propagate cancellation
        }
        catch (Exception ex)
        {
            var error = $"第{rowIndex + 2}行 ({currentName})：处理数据时发生异常 - {ClientErrorMessageMapper.GetSafeOperationFailureMessage("处理导入行", ex)}";
            _logger.LogError(ex, $"处理第{rowIndex + 2}行数据时发生错误: {currentName}");
            return ImportRowResult.CreateFail(error);
        }
    }

    /// <summary>
    /// 报告导入进度
    /// Phase 1.2: 从BackgroundWorker改为IProgress<T>
    /// </summary>
    private static void ReportImportProgress(
        IProgress<ImportProgressInfo>? progress,
        int currentIndex,
        int totalCount,
        string currentName)
    {
        if (progress == null) return;

        var progressInfo = new ImportProgressInfo
        {
            PercentComplete = (int)((double)(currentIndex + 1) / totalCount * 100),
            ProcessedCount = currentIndex + 1,
            TotalCount = totalCount,
            CurrentItem = currentName,
            Message = $"正在导入患者数据... ({currentIndex + 1}/{totalCount})"
        };

        progress.Report(progressInfo);
    }

    /// <summary>
    /// 创建导入结果对象
    /// Issue #1789: 从ImportWorker_DoWork提取，封装结果创建逻辑
    /// </summary>
    private static ImportResult CreateImportResult(int successCount, int failCount, int skipCount, List<string> errors, string? errorMessage = null)
    {
        return new ImportResult(successCount, failCount, skipCount, errors, errorMessage);
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
    /// 批量导入结果
    /// consolidate-code-quality: 替代匿名对象，消除反射
    /// Phase 1.2: 改为public，供外部调用者使用
    /// </summary>
    public sealed record ImportResult(
        int SuccessCount,
        int FailCount,
        int SkipCount,
        List<string> Errors,
        string? Error = null)
    {
        public int TotalProcessed => SuccessCount + FailCount + SkipCount;

        /// <summary>
        /// 获取导入结果的消息类型和标题
        /// </summary>
        public (string Title, MessageBoxImage MessageType) GetDisplayInfo()
        {
            return (SuccessCount, FailCount) switch
            {
                ( > 0, 0) => ("导入成功", MessageBoxImage.Information),
                ( > 0, > 0) => ("导入部分成功", MessageBoxImage.Warning),
                (0, > 0) => ("导入失败", MessageBoxImage.Error),
                _ => ("导入结果", MessageBoxImage.Information)
            };
        }

        /// <summary>
        /// 构建详细的结果消息
        /// </summary>
        public string BuildMessage()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("患者数据导入完成!");
            sb.AppendLine();
            sb.AppendLine($"总计处理:{TotalProcessed} 条");
            sb.AppendLine($"成功导入:{SuccessCount} 条");

            if (FailCount > 0)
                sb.AppendLine($"导入失败:{FailCount} 条");
            if (SkipCount > 0)
                sb.AppendLine($"跳过空行:{SkipCount} 条");

            AppendErrorDetails(sb);
            AppendGeneralError(sb);

            return sb.ToString();
        }

        private void AppendErrorDetails(System.Text.StringBuilder sb)
        {
            if (Errors.Count == 0) return;

            sb.AppendLine();
            sb.AppendLine("失败详情:");
            const int maxErrorsToShow = 5;
            foreach (var error in Errors.Take(maxErrorsToShow))
            {
                sb.AppendLine($"• {error}");
            }

            if (Errors.Count > maxErrorsToShow)
            {
                sb.AppendLine($"• ...还有{Errors.Count - maxErrorsToShow}个错误，请查看日志获取详细信息");
            }
        }

        private void AppendGeneralError(System.Text.StringBuilder sb)
        {
            if (string.IsNullOrEmpty(Error)) return;

            sb.AppendLine();
            sb.AppendLine($"其他错误:{Error}");
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}

/// <summary>
/// 导入完成事件参数
/// Issue #1790: 封装导入完成事件数据
/// Phase 1.2: 保留用于向后兼容，但不再由PatientImportExecutor使用
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
