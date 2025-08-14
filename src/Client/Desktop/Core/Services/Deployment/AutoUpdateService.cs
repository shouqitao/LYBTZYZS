using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Deployment
{
    /// <summary>
    /// 自动更新服务接口 - UltraThink Stage 5.3.3
    /// 提供应用自动更新、回滚、增量更新等功能
    /// </summary>
    public interface IAutoUpdateService
    {
        /// <summary>
        /// 检查更新
        /// </summary>
        Task<UpdateInfo?> CheckForUpdateAsync();
        
        /// <summary>
        /// 下载更新
        /// </summary>
        Task<DownloadResult> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<DownloadProgress>? progress = null);
        
        /// <summary>
        /// 应用更新
        /// </summary>
        Task<UpdateResult> ApplyUpdateAsync(string packagePath);
        
        /// <summary>
        /// 安排更新
        /// </summary>
        Task<ScheduleResult> ScheduleUpdateAsync(UpdateInfo updateInfo, UpdateSchedule schedule);
        
        /// <summary>
        /// 回滚更新
        /// </summary>
        Task<RollbackResult> RollbackUpdateAsync(string targetVersion);
        
        /// <summary>
        /// 获取更新历史
        /// </summary>
        List<UpdateHistoryEntry> GetUpdateHistory();
        
        /// <summary>
        /// 配置自动更新
        /// </summary>
        void ConfigureAutoUpdate(AutoUpdateConfiguration configuration);
        
        /// <summary>
        /// 启动自动检查
        /// </summary>
        Task StartAutoCheckAsync();
        
        /// <summary>
        /// 停止自动检查
        /// </summary>
        void StopAutoCheck();
        
        /// <summary>
        /// 注册更新事件处理器
        /// </summary>
        void RegisterUpdateHandler(IUpdateEventHandler handler);
    }

    /// <summary>
    /// 自动更新服务实现
    /// </summary>
    public class AutoUpdateService : IAutoUpdateService, IDisposable
    {
        private readonly ILogger<AutoUpdateService> _logger;
        private readonly IVersionManagementService _versionService;
        private readonly IPackagingService _packagingService;
        private readonly HttpClient _httpClient;
        
        private readonly string _updateDirectory;
        private readonly string _backupDirectory;
        private readonly List<UpdateHistoryEntry> _updateHistory = new();
        private readonly List<IUpdateEventHandler> _eventHandlers = new();
        
        private AutoUpdateConfiguration _configuration;
        private Timer? _autoCheckTimer;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lock = new object();

        public AutoUpdateService(
            ILogger<AutoUpdateService> logger,
            IVersionManagementService versionService,
            IPackagingService packagingService,
            HttpClient httpClient)
        {
            _logger = logger;
            _versionService = versionService;
            _packagingService = packagingService;
            _httpClient = httpClient;
            
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LYBT");
            
            _updateDirectory = Path.Combine(appDataPath, "Updates");
            _backupDirectory = Path.Combine(appDataPath, "Backups");
            
            Directory.CreateDirectory(_updateDirectory);
            Directory.CreateDirectory(_backupDirectory);
            
            _configuration = new AutoUpdateConfiguration();
            LoadUpdateHistory();
        }

        #region 核心功能

        public async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            try
            {
                _logger.LogInformation("开始检查更新");
                NotifyEvent(UpdateEventType.CheckStarted, null);
                
                // 从版本服务检查更新
                var updateCheck = await _versionService.CheckForUpdatesAsync();
                
                if (!updateCheck.HasUpdate)
                {
                    _logger.LogInformation("没有可用更新");
                    NotifyEvent(UpdateEventType.NoUpdateAvailable, null);
                    return null;
                }
                
                var updateInfo = new UpdateInfo
                {
                    CurrentVersion = updateCheck.CurrentVersion,
                    AvailableVersion = updateCheck.LatestVersion,
                    UpdateType = ConvertUpdateType(updateCheck.UpdateType),
                    ReleaseNotes = updateCheck.ReleaseNotes,
                    DownloadUrl = updateCheck.DownloadUrl ?? "",
                    FileSize = updateCheck.DownloadSize,
                    Checksum = updateCheck.Checksum ?? "",
                    IsMandatory = false,
                    PublishedDate = DateTime.Now
                };
                
                // 检查是否为强制更新
                if (_configuration.MandatoryUpdateVersions?.Contains(updateInfo.AvailableVersion) == true)
                {
                    updateInfo.IsMandatory = true;
                }
                
                // 检查兼容性
                var compatibility = _versionService.CheckCompatibility(updateInfo.AvailableVersion);
                updateInfo.IsCompatible = compatibility.IsCompatible;
                updateInfo.CompatibilityWarnings = compatibility.Warnings;
                
                _logger.LogInformation("发现新版本: {Version}", updateInfo.AvailableVersion);
                NotifyEvent(UpdateEventType.UpdateAvailable, updateInfo);
                
                return updateInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查更新失败");
                NotifyEvent(UpdateEventType.CheckFailed, null, ex.Message);
                return null;
            }
        }

        public async Task<DownloadResult> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<DownloadProgress>? progress = null)
        {
            try
            {
                _logger.LogInformation("开始下载更新: {Version}", updateInfo.AvailableVersion);
                NotifyEvent(UpdateEventType.DownloadStarted, updateInfo);
                
                var result = new DownloadResult
                {
                    UpdateInfo = updateInfo,
                    StartTime = DateTime.Now
                };
                
                // 确定下载路径
                var fileName = $"LYBT_{updateInfo.AvailableVersion}.zip";
                var downloadPath = Path.Combine(_updateDirectory, fileName);
                
                // 检查是否已下载
                if (File.Exists(downloadPath))
                {
                    var fileInfo = new FileInfo(downloadPath);
                    if (fileInfo.Length == updateInfo.FileSize)
                    {
                        _logger.LogInformation("更新包已存在，跳过下载");
                        result.Success = true;
                        result.FilePath = downloadPath;
                        result.EndTime = DateTime.Now;
                        return result;
                    }
                }
                
                // 下载文件
                using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? updateInfo.FileSize;
                    var buffer = new byte[8192];
                    var downloadedBytes = 0L;
                    
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        int bytesRead;
                        var lastReportTime = DateTime.Now;
                        
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;
                            
                            // 报告进度
                            if ((DateTime.Now - lastReportTime).TotalMilliseconds > 100)
                            {
                                var downloadProgress = new DownloadProgress
                                {
                                    TotalBytes = totalBytes,
                                    DownloadedBytes = downloadedBytes,
                                    PercentComplete = (int)((downloadedBytes * 100) / totalBytes),
                                    BytesPerSecond = CalculateSpeed(downloadedBytes, result.StartTime),
                                    EstimatedTimeRemaining = CalculateTimeRemaining(downloadedBytes, totalBytes, result.StartTime)
                                };
                                
                                progress?.Report(downloadProgress);
                                NotifyEvent(UpdateEventType.DownloadProgress, updateInfo, downloadProgress.PercentComplete.ToString());
                                
                                lastReportTime = DateTime.Now;
                            }
                        }
                    }
                }
                
                // 验证下载文件
                if (!string.IsNullOrEmpty(updateInfo.Checksum))
                {
                    var actualChecksum = await CalculateFileChecksumAsync(downloadPath);
                    if (actualChecksum != updateInfo.Checksum)
                    {
                        File.Delete(downloadPath);
                        throw new InvalidOperationException("文件校验和不匹配");
                    }
                }
                
                result.Success = true;
                result.FilePath = downloadPath;
                result.FileSize = new FileInfo(downloadPath).Length;
                result.EndTime = DateTime.Now;
                
                _logger.LogInformation("更新下载完成: {Path}", downloadPath);
                NotifyEvent(UpdateEventType.DownloadCompleted, updateInfo);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载更新失败");
                NotifyEvent(UpdateEventType.DownloadFailed, updateInfo, ex.Message);
                
                return new DownloadResult
                {
                    UpdateInfo = updateInfo,
                    Success = false,
                    Error = ex.Message,
                    EndTime = DateTime.Now
                };
            }
        }

        public async Task<UpdateResult> ApplyUpdateAsync(string packagePath)
        {
            try
            {
                _logger.LogInformation("开始应用更新: {Package}", packagePath);
                NotifyEvent(UpdateEventType.InstallStarted, null);
                
                var result = new UpdateResult
                {
                    PackagePath = packagePath,
                    StartTime = DateTime.Now
                };
                
                // 验证包
                var validation = await _packagingService.ValidatePackageAsync(packagePath);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException($"包验证失败: {string.Join(", ", validation.Errors)}");
                }
                
                // 获取包信息
                var packageInfo = await _packagingService.GetPackageInfoAsync(packagePath);
                var targetVersion = packageInfo.Manifest?.Version ?? "Unknown";
                
                // 创建备份
                var backupPath = await CreateBackupAsync();
                result.BackupPath = backupPath;
                
                // 提取更新包
                var extractPath = Path.Combine(_updateDirectory, "Extract_" + Guid.NewGuid());
                var extractResult = await _packagingService.ExtractPackageAsync(packagePath, extractPath);
                
                if (!extractResult.Success)
                {
                    throw new InvalidOperationException($"提取更新包失败: {extractResult.Error}");
                }
                
                // 创建更新脚本
                var updateScript = GenerateUpdateScript(extractPath, backupPath);
                var scriptPath = Path.Combine(_updateDirectory, "update.bat");
                await File.WriteAllTextAsync(scriptPath, updateScript);
                
                // 启动更新进程
                var updateProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = scriptPath,
                        Arguments = $"\"{Process.GetCurrentProcess().Id}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                updateProcess.Start();
                
                result.Success = true;
                result.UpdatedVersion = targetVersion;
                result.EndTime = DateTime.Now;
                result.RequiresRestart = true;
                
                // 记录更新历史
                AddUpdateHistory(new UpdateHistoryEntry
                {
                    Version = targetVersion,
                    UpdateTime = DateTime.Now,
                    UpdateType = UpdateType.Full,
                    Success = true,
                    BackupPath = backupPath
                });
                
                _logger.LogInformation("更新应用成功，需要重启");
                NotifyEvent(UpdateEventType.InstallCompleted, null);
                
                // 触发应用重启
                if (_configuration.AutoRestart)
                {
                    await Task.Delay(3000);
                    Application.Restart();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用更新失败");
                NotifyEvent(UpdateEventType.InstallFailed, null, ex.Message);
                
                return new UpdateResult
                {
                    PackagePath = packagePath,
                    Success = false,
                    Error = ex.Message,
                    EndTime = DateTime.Now
                };
            }
        }

        public async Task<ScheduleResult> ScheduleUpdateAsync(UpdateInfo updateInfo, UpdateSchedule schedule)
        {
            try
            {
                _logger.LogInformation("安排更新: {Version} 在 {Time}", 
                    updateInfo.AvailableVersion, schedule.ScheduledTime);
                
                var result = new ScheduleResult
                {
                    UpdateInfo = updateInfo,
                    Schedule = schedule,
                    ScheduledAt = DateTime.Now
                };
                
                // 计算延迟时间
                var delay = schedule.ScheduledTime - DateTime.Now;
                if (delay.TotalMilliseconds <= 0)
                {
                    // 立即执行
                    var downloadResult = await DownloadUpdateAsync(updateInfo);
                    if (downloadResult.Success)
                    {
                        var updateResult = await ApplyUpdateAsync(downloadResult.FilePath!);
                        result.Success = updateResult.Success;
                        result.Error = updateResult.Error;
                    }
                }
                else
                {
                    // 安排任务
                    _ = Task.Delay(delay).ContinueWith(async _ =>
                    {
                        try
                        {
                            var downloadResult = await DownloadUpdateAsync(updateInfo);
                            if (downloadResult.Success)
                            {
                                await ApplyUpdateAsync(downloadResult.FilePath!);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "执行计划更新失败");
                        }
                    });
                    
                    result.Success = true;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安排更新失败");
                return new ScheduleResult
                {
                    UpdateInfo = updateInfo,
                    Schedule = schedule,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<RollbackResult> RollbackUpdateAsync(string targetVersion)
        {
            try
            {
                _logger.LogInformation("开始回滚到版本: {Version}", targetVersion);
                NotifyEvent(UpdateEventType.RollbackStarted, null);
                
                var result = new RollbackResult
                {
                    TargetVersion = targetVersion,
                    StartTime = DateTime.Now
                };
                
                // 查找备份
                var historyEntry = _updateHistory
                    .Where(h => h.Version == targetVersion && h.Success && !string.IsNullOrEmpty(h.BackupPath))
                    .OrderByDescending(h => h.UpdateTime)
                    .FirstOrDefault();
                
                if (historyEntry == null || !Directory.Exists(historyEntry.BackupPath))
                {
                    throw new InvalidOperationException($"找不到版本 {targetVersion} 的备份");
                }
                
                // 创建当前版本备份
                var currentBackup = await CreateBackupAsync();
                
                // 恢复备份
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                await RestoreBackupAsync(historyEntry.BackupPath, appDirectory);
                
                result.Success = true;
                result.RolledBackFrom = _versionService.GetCurrentVersion().ToString();
                result.EndTime = DateTime.Now;
                result.RequiresRestart = true;
                
                // 记录回滚历史
                AddUpdateHistory(new UpdateHistoryEntry
                {
                    Version = targetVersion,
                    UpdateTime = DateTime.Now,
                    UpdateType = UpdateType.Rollback,
                    Success = true,
                    BackupPath = currentBackup,
                    Notes = $"从 {result.RolledBackFrom} 回滚"
                });
                
                _logger.LogInformation("回滚成功，需要重启");
                NotifyEvent(UpdateEventType.RollbackCompleted, null);
                
                if (_configuration.AutoRestart)
                {
                    await Task.Delay(3000);
                    Application.Restart();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回滚失败");
                NotifyEvent(UpdateEventType.RollbackFailed, null, ex.Message);
                
                return new RollbackResult
                {
                    TargetVersion = targetVersion,
                    Success = false,
                    Error = ex.Message,
                    EndTime = DateTime.Now
                };
            }
        }

        public List<UpdateHistoryEntry> GetUpdateHistory()
        {
            lock (_lock)
            {
                return _updateHistory.OrderByDescending(h => h.UpdateTime).ToList();
            }
        }

        public void ConfigureAutoUpdate(AutoUpdateConfiguration configuration)
        {
            _configuration = configuration;
            _logger.LogInformation("自动更新配置已更新");
            
            if (_configuration.EnableAutoCheck)
            {
                _ = StartAutoCheckAsync();
            }
            else
            {
                StopAutoCheck();
            }
        }

        public async Task StartAutoCheckAsync()
        {
            if (_autoCheckTimer != null)
            {
                _logger.LogWarning("自动检查已在运行");
                return;
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            _autoCheckTimer = new Timer(async _ =>
            {
                try
                {
                    if (!_configuration.EnableAutoCheck)
                        return;
                    
                    var updateInfo = await CheckForUpdateAsync();
                    if (updateInfo != null)
                    {
                        if (_configuration.AutoDownload)
                        {
                            var downloadResult = await DownloadUpdateAsync(updateInfo);
                            
                            if (downloadResult.Success && _configuration.AutoInstall)
                            {
                                // 检查是否在允许的时间窗口
                                if (IsInMaintenanceWindow())
                                {
                                    await ApplyUpdateAsync(downloadResult.FilePath!);
                                }
                                else
                                {
                                    _logger.LogInformation("更新已下载，等待维护窗口");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "自动检查更新失败");
                }
            }, null, TimeSpan.Zero, _configuration.CheckInterval);
            
            _logger.LogInformation("自动更新检查已启动");
        }

        public void StopAutoCheck()
        {
            _autoCheckTimer?.Dispose();
            _autoCheckTimer = null;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            
            _logger.LogInformation("自动更新检查已停止");
        }

        public void RegisterUpdateHandler(IUpdateEventHandler handler)
        {
            lock (_eventHandlers)
            {
                _eventHandlers.Add(handler);
            }
        }

        #endregion

        #region 私有方法

        private void LoadUpdateHistory()
        {
            try
            {
                var historyFile = Path.Combine(_updateDirectory, "update-history.json");
                if (File.Exists(historyFile))
                {
                    var json = File.ReadAllText(historyFile);
                    var history = System.Text.Json.JsonSerializer.Deserialize<List<UpdateHistoryEntry>>(json);
                    if (history != null)
                    {
                        _updateHistory.AddRange(history);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载更新历史失败");
            }
        }

        private void SaveUpdateHistory()
        {
            try
            {
                var historyFile = Path.Combine(_updateDirectory, "update-history.json");
                var json = System.Text.Json.JsonSerializer.Serialize(_updateHistory, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(historyFile, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存更新历史失败");
            }
        }

        private void AddUpdateHistory(UpdateHistoryEntry entry)
        {
            lock (_lock)
            {
                _updateHistory.Add(entry);
                
                // 只保留最近50条记录
                while (_updateHistory.Count > 50)
                {
                    _updateHistory.RemoveAt(0);
                }
                
                SaveUpdateHistory();
            }
        }

        private async Task<string> CreateBackupAsync()
        {
            var currentVersion = _versionService.GetCurrentVersion().ToString();
            var backupName = $"Backup_{currentVersion}_{DateTime.Now:yyyyMMddHHmmss}";
            var backupPath = Path.Combine(_backupDirectory, backupName);
            
            Directory.CreateDirectory(backupPath);
            
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            await CopyDirectoryAsync(appDirectory, backupPath);
            
            _logger.LogInformation("创建备份: {Path}", backupPath);
            
            return backupPath;
        }

        private async Task RestoreBackupAsync(string backupPath, string targetPath)
        {
            await CopyDirectoryAsync(backupPath, targetPath);
            _logger.LogInformation("恢复备份: {From} -> {To}", backupPath, targetPath);
        }

        private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
        {
            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourceDir, file);
                    var targetFile = Path.Combine(targetDir, relativePath);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                    File.Copy(file, targetFile, true);
                }
            });
        }

        private string GenerateUpdateScript(string updatePath, string backupPath)
        {
            var script = new System.Text.StringBuilder();
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            script.AppendLine("@echo off");
            script.AppendLine("echo 正在更新凌隐宝堂中医诊所系统...");
            script.AppendLine();
            
            // 等待主进程退出
            script.AppendLine("set PID=%1");
            script.AppendLine(":WAIT");
            script.AppendLine("tasklist /FI \"PID eq %PID%\" 2>NUL | find /I /N \"%PID%\" >NUL");
            script.AppendLine("if %ERRORLEVEL% EQU 0 (");
            script.AppendLine("    timeout /t 1 /nobreak >nul");
            script.AppendLine("    goto WAIT");
            script.AppendLine(")");
            script.AppendLine();
            
            // 应用更新
            script.AppendLine("echo 正在应用更新文件...");
            script.AppendLine($"xcopy /E /Y /I \"{updatePath}\\*\" \"{appDirectory}\"");
            script.AppendLine();
            
            // 重启应用
            script.AppendLine("echo 更新完成，正在重启应用...");
            script.AppendLine($"start \"\" \"{Path.Combine(appDirectory, "LYBT.exe")}\"");
            script.AppendLine();
            
            // 清理
            script.AppendLine("echo 清理临时文件...");
            script.AppendLine($"rmdir /S /Q \"{updatePath}\"");
            script.AppendLine("del \"%~f0\"");
            
            return script.ToString();
        }

        private async Task<string> CalculateFileChecksumAsync(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = await sha256.ComputeHashAsync(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        private long CalculateSpeed(long downloadedBytes, DateTime startTime)
        {
            var elapsed = DateTime.Now - startTime;
            if (elapsed.TotalSeconds < 1)
                return downloadedBytes;
            
            return (long)(downloadedBytes / elapsed.TotalSeconds);
        }

        private TimeSpan CalculateTimeRemaining(long downloadedBytes, long totalBytes, DateTime startTime)
        {
            var elapsed = DateTime.Now - startTime;
            if (downloadedBytes == 0)
                return TimeSpan.MaxValue;
            
            var remainingBytes = totalBytes - downloadedBytes;
            var speed = CalculateSpeed(downloadedBytes, startTime);
            
            if (speed == 0)
                return TimeSpan.MaxValue;
            
            return TimeSpan.FromSeconds(remainingBytes / speed);
        }

        private bool IsInMaintenanceWindow()
        {
            if (_configuration.MaintenanceWindows == null || _configuration.MaintenanceWindows.Count == 0)
                return true;
            
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;
            var currentDay = now.DayOfWeek;
            
            return _configuration.MaintenanceWindows.Any(w =>
                w.DaysOfWeek.Contains(currentDay) &&
                currentTime >= w.StartTime &&
                currentTime <= w.EndTime);
        }

        private UpdateType ConvertUpdateType(object updateType)
        {
            // 将版本服务的更新类型转换为本地UpdateType
            return updateType?.ToString()?.ToLower() switch
            {
                "none" => UpdateType.None,
                "patch" => UpdateType.Patch,
                "minor" => UpdateType.Minor,
                "major" => UpdateType.Major,
                "full" => UpdateType.Full,
                "delta" => UpdateType.Delta,
                "prerelease" => UpdateType.PreRelease,
                "rollback" => UpdateType.Rollback,
                _ => UpdateType.Full
            };
        }

        private void NotifyEvent(UpdateEventType eventType, UpdateInfo? updateInfo, string? message = null)
        {
            var eventArgs = new UpdateEventArgs
            {
                EventType = eventType,
                UpdateInfo = updateInfo,
                Message = message,
                Timestamp = DateTime.Now
            };
            
            List<IUpdateEventHandler> handlers;
            lock (_eventHandlers)
            {
                handlers = _eventHandlers.ToList();
            }
            
            foreach (var handler in handlers)
            {
                try
                {
                    handler.HandleUpdateEvent(eventArgs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新事件处理器执行失败");
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopAutoCheck();
            _httpClient?.Dispose();
            
            _logger.LogInformation("自动更新服务已关闭");
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 更新信息
    /// </summary>
    public class UpdateInfo
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string AvailableVersion { get; set; } = string.Empty;
        public UpdateType UpdateType { get; set; }
        public ReleaseNotes? ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
        public bool IsCompatible { get; set; } = true;
        public List<string> CompatibilityWarnings { get; set; } = new();
        public DateTime PublishedDate { get; set; }
    }

    /// <summary>
    /// 更新类型
    /// </summary>
    public enum UpdateType
    {
        None,       // 无更新
        Patch,
        Minor,
        Major,
        Full,
        Delta,
        PreRelease, // 预发布版本
        Rollback
    }

    /// <summary>
    /// 下载结果
    /// </summary>
    public class DownloadResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public UpdateInfo? UpdateInfo { get; set; }
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// 下载进度
    /// </summary>
    public class DownloadProgress
    {
        public long TotalBytes { get; set; }
        public long DownloadedBytes { get; set; }
        public int PercentComplete { get; set; }
        public long BytesPerSecond { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }

    /// <summary>
    /// 更新结果
    /// </summary>
    public class UpdateResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string PackagePath { get; set; } = string.Empty;
        public string? UpdatedVersion { get; set; }
        public string? BackupPath { get; set; }
        public bool RequiresRestart { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// 更新计划
    /// </summary>
    public class UpdateSchedule
    {
        public DateTime ScheduledTime { get; set; }
        public bool ForceUpdate { get; set; }
        public bool NotifyUser { get; set; }
        public int NotifyBeforeMinutes { get; set; } = 15;
    }

    /// <summary>
    /// 计划结果
    /// </summary>
    public class ScheduleResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public UpdateInfo? UpdateInfo { get; set; }
        public UpdateSchedule? Schedule { get; set; }
        public DateTime ScheduledAt { get; set; }
    }

    /// <summary>
    /// 回滚结果
    /// </summary>
    public class RollbackResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string TargetVersion { get; set; } = string.Empty;
        public string? RolledBackFrom { get; set; }
        public bool RequiresRestart { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// 更新历史条目
    /// </summary>
    public class UpdateHistoryEntry
    {
        public string Version { get; set; } = string.Empty;
        public DateTime UpdateTime { get; set; }
        public UpdateType UpdateType { get; set; }
        public bool Success { get; set; }
        public string? BackupPath { get; set; }
        public string? Error { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// 自动更新配置
    /// </summary>
    public class AutoUpdateConfiguration
    {
        public bool EnableAutoCheck { get; set; } = true;
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(6);
        public bool AutoDownload { get; set; } = true;
        public bool AutoInstall { get; set; } = false;
        public bool AutoRestart { get; set; } = false;
        public bool NotifyBeforeInstall { get; set; } = true;
        public bool AllowBetaVersions { get; set; } = false;
        public List<string>? MandatoryUpdateVersions { get; set; }
        public List<MaintenanceWindow>? MaintenanceWindows { get; set; }
        public int MaxRetryCount { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// 维护窗口
    /// </summary>
    public class MaintenanceWindow
    {
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    /// <summary>
    /// 更新事件类型
    /// </summary>
    public enum UpdateEventType
    {
        CheckStarted,
        CheckFailed,
        NoUpdateAvailable,
        UpdateAvailable,
        DownloadStarted,
        DownloadProgress,
        DownloadCompleted,
        DownloadFailed,
        InstallStarted,
        InstallCompleted,
        InstallFailed,
        RollbackStarted,
        RollbackCompleted,
        RollbackFailed
    }

    /// <summary>
    /// 更新事件参数
    /// </summary>
    public class UpdateEventArgs
    {
        public UpdateEventType EventType { get; set; }
        public UpdateInfo? UpdateInfo { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 更新事件处理器接口
    /// </summary>
    public interface IUpdateEventHandler
    {
        void HandleUpdateEvent(UpdateEventArgs args);
    }

    /// <summary>
    /// 应用程序类（模拟）
    /// </summary>
    internal static class Application
    {
        public static void Restart()
        {
            // 实际实现应该重启应用
            Process.Start(Process.GetCurrentProcess().MainModule!.FileName);
            Environment.Exit(0);
        }
    }

    #endregion
}