using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Deployment
{
    /// <summary>
    /// 版本管理服务接口 - UltraThink Stage 5.3.3
    /// 提供语义化版本控制、版本比较、更新检测等功能
    /// </summary>
    public interface IVersionManagementService
    {
        /// <summary>
        /// 获取当前版本
        /// </summary>
        SemanticVersion GetCurrentVersion();
        
        /// <summary>
        /// 获取程序集版本
        /// </summary>
        AssemblyVersionInfo GetAssemblyVersion();
        
        /// <summary>
        /// 比较版本
        /// </summary>
        VersionComparison CompareVersions(string version1, string version2);
        
        /// <summary>
        /// 检查更新
        /// </summary>
        Task<UpdateCheckResult> CheckForUpdatesAsync();
        
        /// <summary>
        /// 获取版本历史
        /// </summary>
        Task<List<VersionRelease>> GetVersionHistoryAsync();
        
        /// <summary>
        /// 生成版本号
        /// </summary>
        string GenerateNextVersion(VersionIncrement increment);
        
        /// <summary>
        /// 创建发布说明
        /// </summary>
        Task<ReleaseNotes> GenerateReleaseNotesAsync(string fromVersion, string toVersion);
        
        /// <summary>
        /// 验证版本兼容性
        /// </summary>
        CompatibilityResult CheckCompatibility(string targetVersion);
        
        /// <summary>
        /// 获取版本元数据
        /// </summary>
        VersionMetadata GetVersionMetadata();
        
        /// <summary>
        /// 更新版本信息
        /// </summary>
        Task UpdateVersionInfoAsync(string newVersion, ReleaseNotes releaseNotes);
    }

    /// <summary>
    /// 版本管理服务实现
    /// </summary>
    public class VersionManagementService : IVersionManagementService
    {
        private readonly ILogger<VersionManagementService> _logger;
        private readonly string _versionFilePath;
        private readonly string _historyFilePath;
        private readonly string _updateUrl = "https://api.lybt.com/updates/check";
        
        private SemanticVersion _currentVersion;
        private List<VersionRelease> _versionHistory;
        private readonly object _lock = new object();

        public VersionManagementService(ILogger<VersionManagementService> logger)
        {
            _logger = logger;
            
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LYBT");
            Directory.CreateDirectory(appDataPath);
            
            _versionFilePath = Path.Combine(appDataPath, "version.json");
            _historyFilePath = Path.Combine(appDataPath, "version-history.json");
            
            InitializeVersion();
            LoadVersionHistory();
        }

        #region 初始化

        private void InitializeVersion()
        {
            try
            {
                // 尝试从文件加载版本信息
                if (File.Exists(_versionFilePath))
                {
                    var json = File.ReadAllText(_versionFilePath);
                    var versionInfo = JsonSerializer.Deserialize<VersionInfo>(json);
                    if (versionInfo != null)
                    {
                        _currentVersion = SemanticVersion.Parse(versionInfo.Version);
                    }
                }
                else
                {
                    // 从程序集获取版本
                    var assembly = Assembly.GetExecutingAssembly();
                    var version = assembly.GetName().Version;
                    
                    _currentVersion = new SemanticVersion
                    {
                        Major = version?.Major ?? 1,
                        Minor = version?.Minor ?? 0,
                        Patch = version?.Build ?? 0,
                        PreRelease = "beta",
                        BuildMetadata = DateTime.Now.ToString("yyyyMMdd")
                    };
                    
                    // 保存版本信息
                    SaveVersionInfo();
                }
                
                _logger.LogInformation("当前版本: {Version}", _currentVersion.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化版本信息失败");
                _currentVersion = new SemanticVersion { Major = 1, Minor = 0, Patch = 0 };
            }
        }

        private void LoadVersionHistory()
        {
            try
            {
                _versionHistory = new List<VersionRelease>();
                
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    var history = JsonSerializer.Deserialize<List<VersionRelease>>(json);
                    if (history != null)
                    {
                        _versionHistory = history;
                    }
                }
                else
                {
                    // 创建初始版本历史
                    _versionHistory.Add(new VersionRelease
                    {
                        Version = "1.0.0",
                        ReleaseDate = DateTime.Now.AddMonths(-6),
                        ReleaseType = ReleaseType.Major,
                        Changes = new List<ChangeEntry>
                        {
                            new() { Type = ChangeType.Feature, Description = "初始版本发布" },
                            new() { Type = ChangeType.Feature, Description = "基础功能实现" }
                        }
                    });
                    
                    SaveVersionHistory();
                }
                
                _logger.LogDebug("加载了 {Count} 个版本历史记录", _versionHistory.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载版本历史失败");
                _versionHistory = new List<VersionRelease>();
            }
        }

        #endregion

        #region 公共方法

        public SemanticVersion GetCurrentVersion()
        {
            return _currentVersion;
        }

        public AssemblyVersionInfo GetAssemblyVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetName();
            
            var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            
            return new AssemblyVersionInfo
            {
                AssemblyVersion = name.Version?.ToString() ?? "0.0.0.0",
                FileVersion = fileVersionInfo.FileVersion ?? "0.0.0.0",
                ProductVersion = fileVersionInfo.ProductVersion ?? "0.0.0",
                AssemblyName = name.Name ?? "Unknown",
                CompanyName = fileVersionInfo.CompanyName ?? "凌隐宝堂",
                ProductName = fileVersionInfo.ProductName ?? "中医诊所管理系统",
                Copyright = fileVersionInfo.LegalCopyright ?? $"© {DateTime.Now.Year} 凌隐宝堂",
                BuildDate = File.GetLastWriteTime(assembly.Location)
            };
        }

        public VersionComparison CompareVersions(string version1, string version2)
        {
            try
            {
                var v1 = SemanticVersion.Parse(version1);
                var v2 = SemanticVersion.Parse(version2);
                
                var comparison = v1.CompareTo(v2);
                
                return new VersionComparison
                {
                    Version1 = version1,
                    Version2 = version2,
                    Result = comparison,
                    IsNewer = comparison > 0,
                    IsOlder = comparison < 0,
                    IsEqual = comparison == 0,
                    MajorDifference = v2.Major - v1.Major,
                    MinorDifference = v2.Minor - v1.Minor,
                    PatchDifference = v2.Patch - v1.Patch,
                    IsBreakingChange = v2.Major > v1.Major
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "版本比较失败: {V1} vs {V2}", version1, version2);
                throw;
            }
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                _logger.LogInformation("开始检查更新");
                
                // 模拟检查更新（实际应该调用API）
                await Task.Delay(500);
                
                // 模拟有新版本
                var latestVersion = new SemanticVersion
                {
                    Major = _currentVersion.Major,
                    Minor = _currentVersion.Minor + 1,
                    Patch = 0
                };
                
                var hasUpdate = latestVersion.CompareTo(_currentVersion) > 0;
                
                var result = new UpdateCheckResult
                {
                    CurrentVersion = _currentVersion.ToString(),
                    LatestVersion = latestVersion.ToString(),
                    HasUpdate = hasUpdate,
                    UpdateType = hasUpdate ? DetermineUpdateType(_currentVersion, latestVersion) : UpdateType.None,
                    CheckTime = DateTime.Now
                };
                
                if (hasUpdate)
                {
                    result.ReleaseNotes = new ReleaseNotes
                    {
                        Version = latestVersion.ToString(),
                        ReleaseDate = DateTime.Now,
                        Highlights = new List<string>
                        {
                            "性能优化提升50%",
                            "新增批量处方功能",
                            "修复已知问题"
                        },
                        Features = new List<string>
                        {
                            "智能诊断辅助",
                            "处方模板管理",
                            "数据导入导出"
                        },
                        Fixes = new List<string>
                        {
                            "修复打印预览问题",
                            "解决内存泄漏",
                            "优化数据库查询"
                        },
                        BreakingChanges = new List<string>()
                    };
                    
                    result.DownloadUrl = $"https://download.lybt.com/releases/{latestVersion}.exe";
                    result.DownloadSize = 45 * 1024 * 1024; // 45MB
                    result.Checksum = "SHA256:1234567890ABCDEF...";
                }
                
                _logger.LogInformation("更新检查完成: {HasUpdate}", hasUpdate ? "有新版本" : "已是最新");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查更新失败");
                return new UpdateCheckResult
                {
                    CurrentVersion = _currentVersion.ToString(),
                    HasUpdate = false,
                    CheckTime = DateTime.Now,
                    Error = ex.Message
                };
            }
        }

        public async Task<List<VersionRelease>> GetVersionHistoryAsync()
        {
            return await Task.FromResult(_versionHistory.OrderByDescending(v => v.ReleaseDate).ToList());
        }

        public string GenerateNextVersion(VersionIncrement increment)
        {
            var nextVersion = _currentVersion.Clone();
            
            switch (increment)
            {
                case VersionIncrement.Major:
                    nextVersion.Major++;
                    nextVersion.Minor = 0;
                    nextVersion.Patch = 0;
                    break;
                    
                case VersionIncrement.Minor:
                    nextVersion.Minor++;
                    nextVersion.Patch = 0;
                    break;
                    
                case VersionIncrement.Patch:
                    nextVersion.Patch++;
                    break;
                    
                case VersionIncrement.PreRelease:
                    if (string.IsNullOrEmpty(nextVersion.PreRelease))
                    {
                        nextVersion.PreRelease = "alpha.1";
                    }
                    else
                    {
                        // 增加预发布版本号
                        var match = Regex.Match(nextVersion.PreRelease, @"(\w+)\.(\d+)");
                        if (match.Success)
                        {
                            var label = match.Groups[1].Value;
                            var number = int.Parse(match.Groups[2].Value);
                            nextVersion.PreRelease = $"{label}.{number + 1}";
                        }
                    }
                    break;
                    
                case VersionIncrement.Build:
                    nextVersion.BuildMetadata = DateTime.Now.ToString("yyyyMMddHHmmss");
                    break;
            }
            
            _logger.LogInformation("生成新版本号: {Current} -> {Next}", 
                _currentVersion.ToString(), nextVersion.ToString());
            
            return nextVersion.ToString();
        }

        public async Task<ReleaseNotes> GenerateReleaseNotesAsync(string fromVersion, string toVersion)
        {
            try
            {
                var from = SemanticVersion.Parse(fromVersion);
                var to = SemanticVersion.Parse(toVersion);
                
                // 收集版本之间的所有变更
                var changes = _versionHistory
                    .Where(v => 
                    {
                        var ver = SemanticVersion.Parse(v.Version);
                        return ver.CompareTo(from) > 0 && ver.CompareTo(to) <= 0;
                    })
                    .SelectMany(v => v.Changes)
                    .ToList();
                
                var releaseNotes = new ReleaseNotes
                {
                    Version = toVersion,
                    ReleaseDate = DateTime.Now,
                    FromVersion = fromVersion,
                    ToVersion = toVersion
                };
                
                // 分类变更
                releaseNotes.Features = changes
                    .Where(c => c.Type == ChangeType.Feature)
                    .Select(c => c.Description)
                    .ToList();
                
                releaseNotes.Fixes = changes
                    .Where(c => c.Type == ChangeType.Fix)
                    .Select(c => c.Description)
                    .ToList();
                
                releaseNotes.Improvements = changes
                    .Where(c => c.Type == ChangeType.Improvement)
                    .Select(c => c.Description)
                    .ToList();
                
                releaseNotes.BreakingChanges = changes
                    .Where(c => c.Type == ChangeType.Breaking)
                    .Select(c => c.Description)
                    .ToList();
                
                // 生成亮点
                releaseNotes.Highlights = GenerateHighlights(releaseNotes);
                
                _logger.LogInformation("生成发布说明: {From} -> {To}", fromVersion, toVersion);
                
                return releaseNotes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成发布说明失败");
                throw;
            }
        }

        public CompatibilityResult CheckCompatibility(string targetVersion)
        {
            try
            {
                var target = SemanticVersion.Parse(targetVersion);
                var current = _currentVersion;
                
                var result = new CompatibilityResult
                {
                    CurrentVersion = current.ToString(),
                    TargetVersion = targetVersion,
                    IsCompatible = true,
                    CompatibilityLevel = CompatibilityLevel.Full
                };
                
                // 检查主版本兼容性
                if (target.Major != current.Major)
                {
                    result.IsCompatible = false;
                    result.CompatibilityLevel = CompatibilityLevel.None;
                    result.Issues.Add("主版本不兼容，可能存在破坏性变更");
                }
                // 检查次版本兼容性
                else if (Math.Abs(target.Minor - current.Minor) > 2)
                {
                    result.CompatibilityLevel = CompatibilityLevel.Limited;
                    result.Warnings.Add("版本跨度较大，建议逐步升级");
                }
                
                // 检查数据库兼容性
                if (target.Major > current.Major)
                {
                    result.RequiresDatabaseMigration = true;
                    result.Warnings.Add("需要执行数据库迁移");
                }
                
                // 检查配置兼容性
                if (target.Minor > current.Minor)
                {
                    result.RequiresConfigurationUpdate = true;
                    result.Warnings.Add("可能需要更新配置文件");
                }
                
                // 生成迁移建议
                if (!result.IsCompatible || result.CompatibilityLevel != CompatibilityLevel.Full)
                {
                    result.MigrationSteps = GenerateMigrationSteps(current, target);
                }
                
                _logger.LogInformation("兼容性检查: {Current} -> {Target}, 结果: {Level}", 
                    current, targetVersion, result.CompatibilityLevel);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "兼容性检查失败");
                return new CompatibilityResult
                {
                    CurrentVersion = _currentVersion.ToString(),
                    TargetVersion = targetVersion,
                    IsCompatible = false,
                    CompatibilityLevel = CompatibilityLevel.Unknown,
                    Issues = { ex.Message }
                };
            }
        }

        public VersionMetadata GetVersionMetadata()
        {
            var assembly = GetAssemblyVersion();
            
            return new VersionMetadata
            {
                Version = _currentVersion.ToString(),
                BuildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "LOCAL",
                BuildDate = assembly.BuildDate,
                CommitHash = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "unknown",
                Branch = Environment.GetEnvironmentVariable("GIT_BRANCH") ?? "master",
                BuildEnvironment = GetBuildEnvironment(),
                RuntimeVersion = Environment.Version.ToString(),
                OSVersion = Environment.OSVersion.ToString(),
                MachineName = Environment.MachineName,
                Is64Bit = Environment.Is64BitOperatingSystem,
                ProcessorCount = Environment.ProcessorCount,
                CLRVersion = Environment.Version.ToString(),
                WorkingDirectory = Environment.CurrentDirectory
            };
        }

        public async Task UpdateVersionInfoAsync(string newVersion, ReleaseNotes releaseNotes)
        {
            try
            {
                var version = SemanticVersion.Parse(newVersion);
                
                lock (_lock)
                {
                    // 更新当前版本
                    _currentVersion = version;
                    
                    // 添加到版本历史
                    _versionHistory.Add(new VersionRelease
                    {
                        Version = newVersion,
                        ReleaseDate = DateTime.Now,
                        ReleaseType = DetermineReleaseType(_currentVersion, version),
                        Changes = ConvertReleaseNotesToChanges(releaseNotes),
                        ReleaseNotes = releaseNotes
                    });
                    
                    // 保存更新
                    SaveVersionInfo();
                    SaveVersionHistory();
                }
                
                _logger.LogInformation("版本信息已更新: {Version}", newVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新版本信息失败");
                throw;
            }
        }

        #endregion

        #region 私有方法

        private void SaveVersionInfo()
        {
            try
            {
                var versionInfo = new VersionInfo
                {
                    Version = _currentVersion.ToString(),
                    UpdatedAt = DateTime.Now,
                    Metadata = GetVersionMetadata()
                };
                
                var json = JsonSerializer.Serialize(versionInfo, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(_versionFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存版本信息失败");
            }
        }

        private void SaveVersionHistory()
        {
            try
            {
                var json = JsonSerializer.Serialize(_versionHistory, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存版本历史失败");
            }
        }

        private UpdateType DetermineUpdateType(SemanticVersion current, SemanticVersion latest)
        {
            if (latest.Major > current.Major)
                return UpdateType.Major;
            if (latest.Minor > current.Minor)
                return UpdateType.Minor;
            if (latest.Patch > current.Patch)
                return UpdateType.Patch;
            if (latest.PreRelease != current.PreRelease)
                return UpdateType.PreRelease;
            
            return UpdateType.None;
        }

        private ReleaseType DetermineReleaseType(SemanticVersion from, SemanticVersion to)
        {
            if (to.Major > from.Major)
                return ReleaseType.Major;
            if (to.Minor > from.Minor)
                return ReleaseType.Minor;
            if (to.Patch > from.Patch)
                return ReleaseType.Patch;
            
            return ReleaseType.PreRelease;
        }

        private List<string> GenerateHighlights(ReleaseNotes notes)
        {
            var highlights = new List<string>();
            
            if (notes.Features.Count > 0)
                highlights.Add($"新增 {notes.Features.Count} 项功能");
            
            if (notes.Fixes.Count > 0)
                highlights.Add($"修复 {notes.Fixes.Count} 个问题");
            
            if (notes.Improvements.Count > 0)
                highlights.Add($"包含 {notes.Improvements.Count} 项改进");
            
            if (notes.BreakingChanges.Count > 0)
                highlights.Add($"⚠️ 包含 {notes.BreakingChanges.Count} 项破坏性变更");
            
            return highlights;
        }

        private List<ChangeEntry> ConvertReleaseNotesToChanges(ReleaseNotes notes)
        {
            var changes = new List<ChangeEntry>();
            
            changes.AddRange(notes.Features.Select(f => new ChangeEntry
            {
                Type = ChangeType.Feature,
                Description = f
            }));
            
            changes.AddRange(notes.Fixes.Select(f => new ChangeEntry
            {
                Type = ChangeType.Fix,
                Description = f
            }));
            
            changes.AddRange(notes.Improvements.Select(i => new ChangeEntry
            {
                Type = ChangeType.Improvement,
                Description = i
            }));
            
            changes.AddRange(notes.BreakingChanges.Select(b => new ChangeEntry
            {
                Type = ChangeType.Breaking,
                Description = b
            }));
            
            return changes;
        }

        private List<string> GenerateMigrationSteps(SemanticVersion from, SemanticVersion to)
        {
            var steps = new List<string>();
            
            if (to.Major > from.Major)
            {
                steps.Add("1. 备份当前数据库和配置文件");
                steps.Add("2. 运行数据库迁移脚本");
                steps.Add("3. 更新配置文件格式");
                steps.Add("4. 验证系统兼容性");
            }
            else if (to.Minor > from.Minor)
            {
                steps.Add("1. 备份配置文件");
                steps.Add("2. 检查新功能配置项");
                steps.Add("3. 更新必要的配置");
            }
            
            steps.Add($"{steps.Count + 1}. 重启应用程序");
            
            return steps;
        }

        private string GetBuildEnvironment()
        {
            #if DEBUG
                return "Debug";
            #else
                return "Release";
            #endif
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 语义版本
    /// </summary>
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public string? PreRelease { get; set; }
        public string? BuildMetadata { get; set; }

        public override string ToString()
        {
            var version = $"{Major}.{Minor}.{Patch}";
            
            if (!string.IsNullOrEmpty(PreRelease))
                version += $"-{PreRelease}";
            
            if (!string.IsNullOrEmpty(BuildMetadata))
                version += $"+{BuildMetadata}";
            
            return version;
        }

        public static SemanticVersion Parse(string version)
        {
            var pattern = @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9\.-]+))?(?:\+([a-zA-Z0-9\.-]+))?$";
            var match = Regex.Match(version, pattern);
            
            if (!match.Success)
                throw new FormatException($"无效的版本格式: {version}");
            
            return new SemanticVersion
            {
                Major = int.Parse(match.Groups[1].Value),
                Minor = int.Parse(match.Groups[2].Value),
                Patch = int.Parse(match.Groups[3].Value),
                PreRelease = match.Groups[4].Success ? match.Groups[4].Value : null,
                BuildMetadata = match.Groups[5].Success ? match.Groups[5].Value : null
            };
        }

        public int CompareTo(SemanticVersion? other)
        {
            if (other == null) return 1;
            
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;
            
            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;
            
            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;
            
            // 有预发布版本的认为比没有的小
            if (string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
                return 1;
            if (!string.IsNullOrEmpty(PreRelease) && string.IsNullOrEmpty(other.PreRelease))
                return -1;
            
            // 比较预发布版本
            if (!string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
            {
                result = string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
                if (result != 0) return result;
            }
            
            return 0;
        }

        public SemanticVersion Clone()
        {
            return new SemanticVersion
            {
                Major = Major,
                Minor = Minor,
                Patch = Patch,
                PreRelease = PreRelease,
                BuildMetadata = BuildMetadata
            };
        }
    }

    /// <summary>
    /// 版本增量类型
    /// </summary>
    public enum VersionIncrement
    {
        Major,
        Minor,
        Patch,
        PreRelease,
        Build
    }

    /// <summary>
    /// 更新类型
    /// </summary>
    // UpdateType enum moved to AutoUpdateService.cs to avoid duplication

    /// <summary>
    /// 发布类型
    /// </summary>
    public enum ReleaseType
    {
        PreRelease,
        Patch,
        Minor,
        Major
    }

    /// <summary>
    /// 变更类型
    /// </summary>
    public enum ChangeType
    {
        Feature,
        Fix,
        Improvement,
        Breaking,
        Security,
        Performance,
        Documentation
    }

    /// <summary>
    /// 兼容性级别
    /// </summary>
    public enum CompatibilityLevel
    {
        Full,
        Partial,
        Limited,
        None,
        Unknown
    }

    /// <summary>
    /// 版本信息
    /// </summary>
    public class VersionInfo
    {
        public string Version { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public VersionMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// 程序集版本信息
    /// </summary>
    public class AssemblyVersionInfo
    {
        public string AssemblyVersion { get; set; } = string.Empty;
        public string FileVersion { get; set; } = string.Empty;
        public string ProductVersion { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Copyright { get; set; } = string.Empty;
        public DateTime BuildDate { get; set; }
    }

    /// <summary>
    /// 版本比较结果
    /// </summary>
    public class VersionComparison
    {
        public string Version1 { get; set; } = string.Empty;
        public string Version2 { get; set; } = string.Empty;
        public int Result { get; set; }
        public bool IsNewer { get; set; }
        public bool IsOlder { get; set; }
        public bool IsEqual { get; set; }
        public int MajorDifference { get; set; }
        public int MinorDifference { get; set; }
        public int PatchDifference { get; set; }
        public bool IsBreakingChange { get; set; }
    }

    /// <summary>
    /// 更新检查结果
    /// </summary>
    public class UpdateCheckResult
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public bool HasUpdate { get; set; }
        public UpdateType UpdateType { get; set; }
        public DateTime CheckTime { get; set; }
        public ReleaseNotes? ReleaseNotes { get; set; }
        public string? DownloadUrl { get; set; }
        public long DownloadSize { get; set; }
        public string? Checksum { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// 版本发布
    /// </summary>
    public class VersionRelease
    {
        public string Version { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public ReleaseType ReleaseType { get; set; }
        public List<ChangeEntry> Changes { get; set; } = new();
        public ReleaseNotes? ReleaseNotes { get; set; }
    }

    /// <summary>
    /// 变更条目
    /// </summary>
    public class ChangeEntry
    {
        public ChangeType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? IssueNumber { get; set; }
        public string? Author { get; set; }
    }

    /// <summary>
    /// 发布说明
    /// </summary>
    public class ReleaseNotes
    {
        public string Version { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string? FromVersion { get; set; }
        public string? ToVersion { get; set; }
        public List<string> Highlights { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public List<string> Fixes { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> BreakingChanges { get; set; } = new();
        public string? MarkdownContent { get; set; }
    }

    /// <summary>
    /// 兼容性结果
    /// </summary>
    public class CompatibilityResult
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string TargetVersion { get; set; } = string.Empty;
        public bool IsCompatible { get; set; }
        public CompatibilityLevel CompatibilityLevel { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool RequiresDatabaseMigration { get; set; }
        public bool RequiresConfigurationUpdate { get; set; }
        public List<string> MigrationSteps { get; set; } = new();
    }

    /// <summary>
    /// 版本元数据
    /// </summary>
    public class VersionMetadata
    {
        public string Version { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public DateTime BuildDate { get; set; }
        public string CommitHash { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string BuildEnvironment { get; set; } = string.Empty;
        public string RuntimeVersion { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public bool Is64Bit { get; set; }
        public int ProcessorCount { get; set; }
        public string CLRVersion { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
    }

    #endregion
}