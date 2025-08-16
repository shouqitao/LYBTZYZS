using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Deployment
{
    /// <summary>
    /// 打包服务接口 - UltraThink Stage 5.3.3
    /// 提供应用打包、资源压缩、安装包生成等功能
    /// </summary>
    public interface IPackagingService
    {
        /// <summary>
        /// 创建发布包
        /// </summary>
        Task<PackageResult> CreatePackageAsync(PackageConfiguration config);
        
        /// <summary>
        /// 验证包完整性
        /// </summary>
        Task<ValidationResult> ValidatePackageAsync(string packagePath);
        
        /// <summary>
        /// 提取包内容
        /// </summary>
        Task<ExtractResult> ExtractPackageAsync(string packagePath, string targetDirectory);
        
        /// <summary>
        /// 生成包清单
        /// </summary>
        Task<PackageManifest> GenerateManifestAsync(string directory);
        
        /// <summary>
        /// 创建增量包
        /// </summary>
        Task<DeltaPackageResult> CreateDeltaPackageAsync(string fromVersion, string toVersion);
        
        /// <summary>
        /// 压缩资源
        /// </summary>
        Task<CompressionResult> CompressResourcesAsync(string sourceDirectory, string outputFile);
        
        /// <summary>
        /// 生成安装脚本
        /// </summary>
        Task<string> GenerateInstallScriptAsync(PackageConfiguration config);
        
        /// <summary>
        /// 签名包
        /// </summary>
        Task<SignatureResult> SignPackageAsync(string packagePath, SigningConfiguration signingConfig);
        
        /// <summary>
        /// 获取包信息
        /// </summary>
        Task<PackageInfo> GetPackageInfoAsync(string packagePath);
    }

    /// <summary>
    /// 打包服务实现
    /// </summary>
    public class PackagingService : IPackagingService
    {
        private readonly ILogger<PackagingService> _logger;
        private readonly IVersionManagementService _versionService;
        private readonly string _workingDirectory;
        private readonly string _outputDirectory;
        
        public PackagingService(
            ILogger<PackagingService> logger,
            IVersionManagementService versionService)
        {
            _logger = logger;
            _versionService = versionService;
            
            _workingDirectory = Path.Combine(Path.GetTempPath(), "LYBT_Package");
            _outputDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LYBT_Releases");
            
            Directory.CreateDirectory(_workingDirectory);
            Directory.CreateDirectory(_outputDirectory);
        }

        #region 核心功能

        public async Task<PackageResult> CreatePackageAsync(PackageConfiguration config)
        {
            try
            {
                _logger.LogInformation("开始创建发布包: {Name} v{Version}", 
                    config.PackageName, config.Version);
                
                var result = new PackageResult
                {
                    StartTime = DateTime.Now,
                    Configuration = config
                };
                
                // 创建临时工作目录
                var tempDir = Path.Combine(_workingDirectory, Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                
                try
                {
                    // 步骤1: 收集文件
                    var progress = new Progress<PackageProgress>(p =>
                    {
                        _logger.LogDebug("打包进度: {Stage} - {Percent}%", p.Stage, p.PercentComplete);
                    });
                    
                    await CollectFilesAsync(config, tempDir, progress);
                    
                    // 步骤2: 处理资源
                    if (config.CompressResources)
                    {
                        await CompressResourcesInternalAsync(tempDir);
                    }
                    
                    // 步骤3: 生成清单
                    var manifest = await GenerateManifestInternalAsync(tempDir, config);
                    result.Manifest = manifest;
                    
                    // 步骤4: 创建包文件
                    var packageFileName = $"{config.PackageName}_{config.Version}_{config.Platform}.zip";
                    var packagePath = Path.Combine(_outputDirectory, packageFileName);
                    
                    await CreateZipPackageAsync(tempDir, packagePath, progress);
                    
                    // 步骤5: 计算校验和
                    result.Checksum = await CalculateChecksumAsync(packagePath);
                    
                    // 步骤6: 签名（如果配置）
                    if (config.SignPackage && config.SigningConfiguration != null)
                    {
                        var signResult = await SignPackageAsync(packagePath, config.SigningConfiguration);
                        result.IsSigned = signResult.Success;
                        result.SignatureInfo = signResult.SignatureInfo;
                    }
                    
                    result.Success = true;
                    result.PackagePath = packagePath;
                    result.PackageSize = new FileInfo(packagePath).Length;
                    result.EndTime = DateTime.Now;
                    result.FilesIncluded = manifest.Files.Count;
                    
                    _logger.LogInformation("发布包创建成功: {Path}, 大小: {Size:F2}MB", 
                        packagePath, result.PackageSize / 1024.0 / 1024.0);
                }
                finally
                {
                    // 清理临时目录
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch { }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建发布包失败");
                return new PackageResult
                {
                    Success = false,
                    Error = ex.Message,
                    EndTime = DateTime.Now
                };
            }
        }

        public async Task<ValidationResult> ValidatePackageAsync(string packagePath)
        {
            try
            {
                _logger.LogInformation("开始验证包: {Path}", packagePath);
                
                var result = new ValidationResult
                {
                    PackagePath = packagePath,
                    ValidationTime = DateTime.Now
                };
                
                // 检查文件存在
                if (!File.Exists(packagePath))
                {
                    result.IsValid = false;
                    result.Errors.Add("包文件不存在");
                    return result;
                }
                
                // 检查文件大小
                var fileInfo = new FileInfo(packagePath);
                if (fileInfo.Length == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("包文件为空");
                    return result;
                }
                
                // 尝试打开ZIP文件
                try
                {
                    using (var archive = ZipFile.OpenRead(packagePath))
                    {
                        // 检查清单文件
                        var manifestEntry = archive.GetEntry("manifest.json");
                        if (manifestEntry == null)
                        {
                            result.Warnings.Add("缺少清单文件");
                        }
                        else
                        {
                            // 读取并验证清单
                            using (var stream = manifestEntry.Open())
                            using (var reader = new StreamReader(stream))
                            {
                                var manifestJson = await reader.ReadToEndAsync();
                                var manifest = JsonSerializer.Deserialize<PackageManifest>(manifestJson);
                                
                                if (manifest != null)
                                {
                                    // 验证文件完整性
                                    foreach (var file in manifest.Files)
                                    {
                                        var entry = archive.GetEntry(file.RelativePath);
                                        if (entry == null)
                                        {
                                            result.Errors.Add($"缺少文件: {file.RelativePath}");
                                        }
                                        else if (entry.Length != file.Size)
                                        {
                                            result.Warnings.Add($"文件大小不匹配: {file.RelativePath}");
                                        }
                                    }
                                }
                            }
                        }
                        
                        result.FileCount = archive.Entries.Count;
                        result.CompressedSize = fileInfo.Length;
                        result.UncompressedSize = archive.Entries.Sum(e => e.Length);
                    }
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.Errors.Add($"无法读取包文件: {ex.Message}");
                    return result;
                }
                
                // 验证签名（如果存在）
                var signatureFile = packagePath + ".sig";
                if (File.Exists(signatureFile))
                {
                    result.HasSignature = true;
                    // TODO: 验证签名
                }
                
                result.IsValid = result.Errors.Count == 0;
                
                _logger.LogInformation("包验证完成: {Valid}, 错误: {Errors}, 警告: {Warnings}", 
                    result.IsValid, result.Errors.Count, result.Warnings.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证包失败");
                return new ValidationResult
                {
                    PackagePath = packagePath,
                    IsValid = false,
                    Errors = { ex.Message }
                };
            }
        }

        public async Task<ExtractResult> ExtractPackageAsync(string packagePath, string targetDirectory)
        {
            try
            {
                _logger.LogInformation("开始提取包: {Package} -> {Target}", packagePath, targetDirectory);
                
                var result = new ExtractResult
                {
                    PackagePath = packagePath,
                    TargetDirectory = targetDirectory,
                    StartTime = DateTime.Now
                };
                
                // 验证包
                var validation = await ValidatePackageAsync(packagePath);
                if (!validation.IsValid)
                {
                    result.Success = false;
                    result.Error = string.Join("; ", validation.Errors);
                    return result;
                }
                
                // 创建目标目录
                Directory.CreateDirectory(targetDirectory);
                
                // 提取文件
                using (var archive = ZipFile.OpenRead(packagePath))
                {
                    var totalEntries = archive.Entries.Count;
                    var extracted = 0;
                    
                    foreach (var entry in archive.Entries)
                    {
                        var targetPath = Path.Combine(targetDirectory, entry.FullName);
                        
                        // 创建目录
                        if (entry.FullName.EndsWith("/"))
                        {
                            Directory.CreateDirectory(targetPath);
                        }
                        else
                        {
                            // 确保目录存在
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                            
                            // 提取文件
                            entry.ExtractToFile(targetPath, true);
                            result.ExtractedFiles.Add(entry.FullName);
                        }
                        
                        extracted++;
                        
                        // 报告进度
                        if (extracted % 10 == 0)
                        {
                            _logger.LogDebug("提取进度: {Extracted}/{Total}", extracted, totalEntries);
                        }
                    }
                }
                
                // 读取清单
                var manifestPath = Path.Combine(targetDirectory, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    var manifestJson = await File.ReadAllTextAsync(manifestPath);
                    result.Manifest = JsonSerializer.Deserialize<PackageManifest>(manifestJson);
                }
                
                result.Success = true;
                result.EndTime = DateTime.Now;
                result.FilesExtracted = result.ExtractedFiles.Count;
                
                _logger.LogInformation("包提取完成: {Files} 个文件", result.FilesExtracted);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取包失败");
                return new ExtractResult
                {
                    PackagePath = packagePath,
                    TargetDirectory = targetDirectory,
                    Success = false,
                    Error = ex.Message,
                    EndTime = DateTime.Now
                };
            }
        }

        public async Task<PackageManifest> GenerateManifestAsync(string directory)
        {
            return await GenerateManifestInternalAsync(directory, null);
        }

        public async Task<DeltaPackageResult> CreateDeltaPackageAsync(string fromVersion, string toVersion)
        {
            try
            {
                _logger.LogInformation("创建增量包: {From} -> {To}", fromVersion, toVersion);
                
                var result = new DeltaPackageResult
                {
                    FromVersion = fromVersion,
                    ToVersion = toVersion,
                    StartTime = DateTime.Now
                };
                
                // 获取版本之间的文件差异
                var fromPackage = Path.Combine(_outputDirectory, $"LYBT_{fromVersion}_Windows.zip");
                var toPackage = Path.Combine(_outputDirectory, $"LYBT_{toVersion}_Windows.zip");
                
                if (!File.Exists(fromPackage) || !File.Exists(toPackage))
                {
                    result.Success = false;
                    result.Error = "源版本或目标版本包不存在";
                    return result;
                }
                
                // 提取并比较文件
                var tempFromDir = Path.Combine(_workingDirectory, "from_" + Guid.NewGuid());
                var tempToDir = Path.Combine(_workingDirectory, "to_" + Guid.NewGuid());
                var deltaDir = Path.Combine(_workingDirectory, "delta_" + Guid.NewGuid());
                
                try
                {
                    // 提取包
                    await ExtractPackageAsync(fromPackage, tempFromDir);
                    await ExtractPackageAsync(toPackage, tempToDir);
                    
                    Directory.CreateDirectory(deltaDir);
                    
                    // 比较文件并创建增量
                    var changes = await CompareDirectoriesAsync(tempFromDir, tempToDir);
                    
                    // 复制变更的文件
                    foreach (var change in changes)
                    {
                        if (change.Type == FileChangeType.Added || change.Type == FileChangeType.Modified)
                        {
                            var sourcePath = Path.Combine(tempToDir, change.RelativePath);
                            var targetPath = Path.Combine(deltaDir, change.RelativePath);
                            
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                            File.Copy(sourcePath, targetPath);
                            
                            result.ChangedFiles.Add(change);
                        }
                        else if (change.Type == FileChangeType.Deleted)
                        {
                            result.DeletedFiles.Add(change.RelativePath);
                        }
                    }
                    
                    // 创建增量清单
                    var deltaManifest = new DeltaManifest
                    {
                        FromVersion = fromVersion,
                        ToVersion = toVersion,
                        CreatedAt = DateTime.Now,
                        Changes = changes,
                        TotalSize = changes.Where(c => c.Type != FileChangeType.Deleted).Sum(c => c.NewSize)
                    };
                    
                    var manifestPath = Path.Combine(deltaDir, "delta-manifest.json");
                    var manifestJson = JsonSerializer.Serialize(deltaManifest, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    await File.WriteAllTextAsync(manifestPath, manifestJson);
                    
                    // 创建增量包
                    var deltaPackageName = $"LYBT_Delta_{fromVersion}_to_{toVersion}.zip";
                    var deltaPackagePath = Path.Combine(_outputDirectory, deltaPackageName);
                    
                    await CreateZipPackageAsync(deltaDir, deltaPackagePath, null);
                    
                    result.Success = true;
                    result.PackagePath = deltaPackagePath;
                    result.PackageSize = new FileInfo(deltaPackagePath).Length;
                    result.EndTime = DateTime.Now;
                    
                    _logger.LogInformation("增量包创建成功: {Path}, 大小: {Size:F2}MB", 
                        deltaPackagePath, result.PackageSize / 1024.0 / 1024.0);
                }
                finally
                {
                    // 清理临时目录
                    try
                    {
                        Directory.Delete(tempFromDir, true);
                        Directory.Delete(tempToDir, true);
                        Directory.Delete(deltaDir, true);
                    }
                    catch { }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建增量包失败");
                return new DeltaPackageResult
                {
                    FromVersion = fromVersion,
                    ToVersion = toVersion,
                    Success = false,
                    Error = ex.Message,
                    EndTime = DateTime.Now
                };
            }
        }

        public async Task<CompressionResult> CompressResourcesAsync(string sourceDirectory, string outputFile)
        {
            try
            {
                _logger.LogInformation("压缩资源: {Source} -> {Output}", sourceDirectory, outputFile);
                
                var result = new CompressionResult
                {
                    SourceDirectory = sourceDirectory,
                    OutputFile = outputFile,
                    StartTime = DateTime.Now
                };
                
                // 统计原始大小
                var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
                result.OriginalSize = files.Sum(f => new FileInfo(f).Length);
                result.FileCount = files.Length;
                
                // 创建压缩包
                await Task.Run(() =>
                {
                    using (var zipArchive = ZipFile.Open(outputFile, ZipArchiveMode.Create))
                    {
                        foreach (var file in files)
                        {
                            var relativePath = Path.GetRelativePath(sourceDirectory, file);
                            var entry = zipArchive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                            result.CompressedFiles.Add(relativePath);
                        }
                    }
                });
                
                // 统计压缩后大小
                result.CompressedSize = new FileInfo(outputFile).Length;
                result.CompressionRatio = (double)result.CompressedSize / result.OriginalSize;
                result.SpaceSaved = result.OriginalSize - result.CompressedSize;
                result.Success = true;
                result.EndTime = DateTime.Now;
                
                _logger.LogInformation("资源压缩完成: {Files} 个文件, 压缩率: {Ratio:P2}", 
                    result.FileCount, 1 - result.CompressionRatio);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "压缩资源失败");
                return new CompressionResult
                {
                    SourceDirectory = sourceDirectory,
                    OutputFile = outputFile,
                    Success = false,
                    Error = ex.Message,
                    EndTime = DateTime.Now
                };
            }
        }

        public async Task<string> GenerateInstallScriptAsync(PackageConfiguration config)
        {
            var script = new StringBuilder();
            
            script.AppendLine("@echo off");
            script.AppendLine($"REM 凌隐宝堂中医诊所系统 v{config.Version} 安装脚本");
            script.AppendLine($"REM 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            script.AppendLine();
            
            script.AppendLine("echo ================================");
            script.AppendLine($"echo 凌隐宝堂中医诊所系统 v{config.Version}");
            script.AppendLine("echo ================================");
            script.AppendLine();
            
            // 检查管理员权限
            script.AppendLine("REM 检查管理员权限");
            script.AppendLine("net session >nul 2>&1");
            script.AppendLine("if %errorLevel% neq 0 (");
            script.AppendLine("    echo 请以管理员身份运行此安装程序");
            script.AppendLine("    pause");
            script.AppendLine("    exit /b 1");
            script.AppendLine(")");
            script.AppendLine();
            
            // 设置安装路径
            script.AppendLine("REM 设置安装路径");
            script.AppendLine($"set INSTALL_PATH=%ProgramFiles%\\LYBT\\{config.PackageName}");
            script.AppendLine("echo 安装路径: %INSTALL_PATH%");
            script.AppendLine();
            
            // 创建目录
            script.AppendLine("REM 创建安装目录");
            script.AppendLine("if not exist \"%INSTALL_PATH%\" mkdir \"%INSTALL_PATH%\"");
            script.AppendLine();
            
            // 停止旧服务
            if (config.IncludeService)
            {
                script.AppendLine("REM 停止现有服务");
                script.AppendLine("sc stop LYBTService >nul 2>&1");
                script.AppendLine("timeout /t 3 /nobreak >nul");
                script.AppendLine();
            }
            
            // 备份旧版本
            script.AppendLine("REM 备份旧版本");
            script.AppendLine("if exist \"%INSTALL_PATH%\\LYBT.exe\" (");
            script.AppendLine("    echo 备份现有版本...");
            script.AppendLine("    xcopy /E /I /Y \"%INSTALL_PATH%\" \"%INSTALL_PATH%.backup.%date:~0,4%%date:~5,2%%date:~8,2%\"");
            script.AppendLine(")");
            script.AppendLine();
            
            // 提取文件
            script.AppendLine("REM 提取安装文件");
            script.AppendLine("echo 正在安装文件...");
            script.AppendLine("powershell -Command \"Expand-Archive -Path '%~dp0package.zip' -DestinationPath '%INSTALL_PATH%' -Force\"");
            script.AppendLine();
            
            // 安装依赖
            if (config.IncludeDependencies)
            {
                script.AppendLine("REM 安装依赖");
                script.AppendLine("echo 正在安装运行时依赖...");
                script.AppendLine("\"%INSTALL_PATH%\\Prerequisites\\dotnet-runtime-8.0.exe\" /quiet /norestart");
                script.AppendLine();
            }
            
            // 注册服务
            if (config.IncludeService)
            {
                script.AppendLine("REM 注册Windows服务");
                script.AppendLine("echo 正在注册服务...");
                script.AppendLine("sc create LYBTService binPath=\"%INSTALL_PATH%\\LYBT.Service.exe\" start=auto");
                script.AppendLine("sc description LYBTService \"凌隐宝堂中医诊所管理系统后台服务\"");
                script.AppendLine("sc start LYBTService");
                script.AppendLine();
            }
            
            // 创建快捷方式
            script.AppendLine("REM 创建快捷方式");
            script.AppendLine("echo 正在创建快捷方式...");
            script.AppendLine("powershell -Command \"$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\\Desktop\\凌隐宝堂中医诊所系统.lnk'); $Shortcut.TargetPath = '%INSTALL_PATH%\\LYBT.exe'; $Shortcut.IconLocation = '%INSTALL_PATH%\\LYBT.ico'; $Shortcut.Save()\"");
            script.AppendLine();
            
            // 配置防火墙
            script.AppendLine("REM 配置防火墙规则");
            script.AppendLine("netsh advfirewall firewall add rule name=\"LYBT API\" dir=in action=allow protocol=TCP localport=7001");
            script.AppendLine();
            
            // 完成
            script.AppendLine("echo.");
            script.AppendLine("echo ================================");
            script.AppendLine("echo 安装完成！");
            script.AppendLine("echo ================================");
            script.AppendLine("echo.");
            script.AppendLine("echo 您可以通过桌面快捷方式启动程序");
            script.AppendLine("pause");
            
            var scriptContent = script.ToString();
            
            // 保存脚本
            var scriptPath = Path.Combine(_outputDirectory, $"install_{config.Version}.bat");
            await File.WriteAllTextAsync(scriptPath, scriptContent);
            
            _logger.LogInformation("安装脚本已生成: {Path}", scriptPath);
            
            return scriptContent;
        }

        public async Task<SignatureResult> SignPackageAsync(string packagePath, SigningConfiguration signingConfig)
        {
            try
            {
                _logger.LogInformation("签名包: {Path}", packagePath);
                
                var result = new SignatureResult
                {
                    PackagePath = packagePath,
                    SigningTime = DateTime.Now
                };
                
                // 计算包的哈希
                var packageHash = await CalculateHashAsync(packagePath, signingConfig.HashAlgorithm);
                
                // 创建签名信息
                result.SignatureInfo = new SignatureInfo
                {
                    Algorithm = signingConfig.Algorithm,
                    HashAlgorithm = signingConfig.HashAlgorithm,
                    Timestamp = DateTime.Now,
                    Signer = signingConfig.CertificateSubject,
                    PackageHash = packageHash
                };
                
                // 模拟签名（实际应该使用证书进行签名）
                var signatureData = $"{packageHash}:{result.SignatureInfo.Timestamp:O}:{result.SignatureInfo.Signer}";
                var signatureBytes = Encoding.UTF8.GetBytes(signatureData);
                
                // 保存签名文件
                var signaturePath = packagePath + ".sig";
                await File.WriteAllBytesAsync(signaturePath, signatureBytes);
                
                result.Success = true;
                result.SignaturePath = signaturePath;
                
                _logger.LogInformation("包签名成功: {SignaturePath}", signaturePath);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "签名包失败");
                return new SignatureResult
                {
                    PackagePath = packagePath,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<PackageInfo> GetPackageInfoAsync(string packagePath)
        {
            try
            {
                var info = new PackageInfo
                {
                    PackagePath = packagePath,
                    FileName = Path.GetFileName(packagePath),
                    FileSize = new FileInfo(packagePath).Length,
                    CreatedTime = File.GetCreationTime(packagePath),
                    ModifiedTime = File.GetLastWriteTime(packagePath)
                };
                
                // 读取清单
                using (var archive = ZipFile.OpenRead(packagePath))
                {
                    var manifestEntry = archive.GetEntry("manifest.json");
                    if (manifestEntry != null)
                    {
                        using (var stream = manifestEntry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            var manifestJson = await reader.ReadToEndAsync();
                            info.Manifest = JsonSerializer.Deserialize<PackageManifest>(manifestJson);
                        }
                    }
                    
                    info.FileCount = archive.Entries.Count;
                    info.UncompressedSize = archive.Entries.Sum(e => e.Length);
                }
                
                // 计算压缩率
                info.CompressionRatio = (double)info.FileSize / info.UncompressedSize;
                
                // 检查签名
                var signaturePath = packagePath + ".sig";
                info.IsSigned = File.Exists(signaturePath);
                
                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取包信息失败");
                throw;
            }
        }

        #endregion

        #region 私有方法

        private async Task CollectFilesAsync(PackageConfiguration config, string targetDirectory, IProgress<PackageProgress>? progress)
        {
            var filesToInclude = new List<string>();
            
            // 收集主程序文件
            if (!string.IsNullOrEmpty(config.SourceDirectory) && Directory.Exists(config.SourceDirectory))
            {
                var files = Directory.GetFiles(config.SourceDirectory, "*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(config.SourceDirectory, file);
                    
                    // 检查排除规则
                    if (config.ExcludePatterns?.Any(pattern => IsMatch(relativePath, pattern)) == true)
                        continue;
                    
                    // 检查包含规则
                    if (config.IncludePatterns?.Any() == true &&
                        !config.IncludePatterns.Any(pattern => IsMatch(relativePath, pattern)))
                        continue;
                    
                    filesToInclude.Add(file);
                }
            }
            
            // 复制文件到目标目录
            var total = filesToInclude.Count;
            var copied = 0;
            
            foreach (var file in filesToInclude)
            {
                var relativePath = Path.GetRelativePath(config.SourceDirectory!, file);
                var targetPath = Path.Combine(targetDirectory, relativePath);
                
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(file, targetPath, true);
                
                copied++;
                progress?.Report(new PackageProgress
                {
                    Stage = "收集文件",
                    CurrentFile = relativePath,
                    ProcessedFiles = copied,
                    TotalFiles = total,
                    PercentComplete = (copied * 100) / total
                });
            }
        }

        private async Task CompressResourcesInternalAsync(string directory)
        {
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file).ToLower();
                if (imageExtensions.Contains(extension))
                {
                    // TODO: 实现图片压缩逻辑
                    await Task.CompletedTask;
                }
            }
        }

        private async Task<PackageManifest> GenerateManifestInternalAsync(string directory, PackageConfiguration? config)
        {
            var manifest = new PackageManifest
            {
                Version = config?.Version ?? _versionService.GetCurrentVersion().ToString(),
                CreatedAt = DateTime.Now,
                Platform = config?.Platform ?? "Windows",
                Architecture = config?.Architecture ?? "x64"
            };
            
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var relativePath = Path.GetRelativePath(directory, file);
                
                manifest.Files.Add(new FileEntry
                {
                    RelativePath = relativePath,
                    Size = fileInfo.Length,
                    Hash = await CalculateHashAsync(file, "SHA256"),
                    ModifiedTime = fileInfo.LastWriteTime
                });
            }
            
            manifest.TotalSize = manifest.Files.Sum(f => f.Size);
            manifest.FileCount = manifest.Files.Count;
            
            // 保存清单
            var manifestPath = Path.Combine(directory, "manifest.json");
            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(manifestPath, manifestJson);
            
            return manifest;
        }

        private async Task CreateZipPackageAsync(string sourceDirectory, string outputPath, IProgress<PackageProgress>? progress)
        {
            await Task.Run(() =>
            {
                ZipFile.CreateFromDirectory(sourceDirectory, outputPath, CompressionLevel.Optimal, false);
            });
        }

        private async Task<string> CalculateChecksumAsync(string filePath)
        {
            return await CalculateHashAsync(filePath, "SHA256");
        }

        private async Task<string> CalculateHashAsync(string filePath, string algorithm)
        {
            using (var stream = File.OpenRead(filePath))
            {
                HashAlgorithm hasher = algorithm.ToUpper() switch
                {
                    "SHA256" => SHA256.Create(),
                    "SHA512" => SHA512.Create(),
                    "MD5" => MD5.Create(),
                    _ => SHA256.Create()
                };
                
                using (hasher)
                {
                    var hash = await hasher.ComputeHashAsync(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }

        private async Task<List<FileChange>> CompareDirectoriesAsync(string fromDir, string toDir)
        {
            var changes = new List<FileChange>();
            
            var fromFiles = Directory.GetFiles(fromDir, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(fromDir, f))
                .ToHashSet();
            
            var toFiles = Directory.GetFiles(toDir, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(toDir, f))
                .ToHashSet();
            
            // 查找新增的文件
            foreach (var file in toFiles.Except(fromFiles))
            {
                var filePath = Path.Combine(toDir, file);
                changes.Add(new FileChange
                {
                    RelativePath = file,
                    Type = FileChangeType.Added,
                    NewSize = new FileInfo(filePath).Length
                });
            }
            
            // 查找删除的文件
            foreach (var file in fromFiles.Except(toFiles))
            {
                changes.Add(new FileChange
                {
                    RelativePath = file,
                    Type = FileChangeType.Deleted
                });
            }
            
            // 查找修改的文件
            foreach (var file in fromFiles.Intersect(toFiles))
            {
                var fromPath = Path.Combine(fromDir, file);
                var toPath = Path.Combine(toDir, file);
                
                var fromHash = await CalculateHashAsync(fromPath, "SHA256");
                var toHash = await CalculateHashAsync(toPath, "SHA256");
                
                if (fromHash != toHash)
                {
                    changes.Add(new FileChange
                    {
                        RelativePath = file,
                        Type = FileChangeType.Modified,
                        OldSize = new FileInfo(fromPath).Length,
                        NewSize = new FileInfo(toPath).Length
                    });
                }
            }
            
            return changes;
        }

        private bool IsMatch(string path, string pattern)
        {
            // 简单的通配符匹配
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(path, regex, RegexOptions.IgnoreCase);
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 包配置
    /// </summary>
    public class PackageConfiguration
    {
        public string PackageName { get; set; } = "LYBT";
        public string Version { get; set; } = "1.0.0";
        public string Platform { get; set; } = "Windows";
        public string Architecture { get; set; } = "x64";
        public string? SourceDirectory { get; set; }
        public string? OutputDirectory { get; set; }
        public List<string>? IncludePatterns { get; set; }
        public List<string>? ExcludePatterns { get; set; }
        public bool CompressResources { get; set; } = true;
        public bool IncludeDependencies { get; set; } = true;
        public bool IncludeDebugSymbols { get; set; } = false;
        public bool IncludeService { get; set; } = false;
        public bool SignPackage { get; set; } = false;
        public SigningConfiguration? SigningConfiguration { get; set; }
    }

    /// <summary>
    /// 签名配置
    /// </summary>
    public class SigningConfiguration
    {
        public string Algorithm { get; set; } = "RSA";
        public string HashAlgorithm { get; set; } = "SHA256";
        public string? CertificatePath { get; set; }
        public string? CertificatePassword { get; set; }
        public string? CertificateSubject { get; set; }
        public bool TimestampServer { get; set; } = true;
        public string? TimestampServerUrl { get; set; }
    }

    /// <summary>
    /// 包结果
    /// </summary>
    public class PackageResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? PackagePath { get; set; }
        public long PackageSize { get; set; }
        public string? Checksum { get; set; }
        public PackageManifest? Manifest { get; set; }
        public PackageConfiguration? Configuration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int FilesIncluded { get; set; }
        public bool IsSigned { get; set; }
        public SignatureInfo? SignatureInfo { get; set; }
    }

    /// <summary>
    /// 包清单
    /// </summary>
    public class PackageManifest
    {
        public string Version { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public List<FileEntry> Files { get; set; } = new();
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
    }

    /// <summary>
    /// 文件条目
    /// </summary>
    public class FileEntry
    {
        public string RelativePath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Hash { get; set; } = string.Empty;
        public DateTime ModifiedTime { get; set; }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public string PackagePath { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime ValidationTime { get; set; }
        public int FileCount { get; set; }
        public long CompressedSize { get; set; }
        public long UncompressedSize { get; set; }
        public bool HasSignature { get; set; }
    }

    /// <summary>
    /// 提取结果
    /// </summary>
    public class ExtractResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string PackagePath { get; set; } = string.Empty;
        public string TargetDirectory { get; set; } = string.Empty;
        public List<string> ExtractedFiles { get; set; } = new();
        public int FilesExtracted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public PackageManifest? Manifest { get; set; }
    }

    /// <summary>
    /// 增量包结果
    /// </summary>
    public class DeltaPackageResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string FromVersion { get; set; } = string.Empty;
        public string ToVersion { get; set; } = string.Empty;
        public string? PackagePath { get; set; }
        public long PackageSize { get; set; }
        public List<FileChange> ChangedFiles { get; set; } = new();
        public List<string> DeletedFiles { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// 文件变更
    /// </summary>
    public class FileChange
    {
        public string RelativePath { get; set; } = string.Empty;
        public FileChangeType Type { get; set; }
        public long OldSize { get; set; }
        public long NewSize { get; set; }
    }

    /// <summary>
    /// 文件变更类型
    /// </summary>
    public enum FileChangeType
    {
        Added,
        Modified,
        Deleted
    }

    /// <summary>
    /// 增量清单
    /// </summary>
    public class DeltaManifest
    {
        public string FromVersion { get; set; } = string.Empty;
        public string ToVersion { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<FileChange> Changes { get; set; } = new();
        public long TotalSize { get; set; }
    }

    /// <summary>
    /// 压缩结果
    /// </summary>
    public class CompressionResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string SourceDirectory { get; set; } = string.Empty;
        public string OutputFile { get; set; } = string.Empty;
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio { get; set; }
        public long SpaceSaved { get; set; }
        public int FileCount { get; set; }
        public List<string> CompressedFiles { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// 签名结果
    /// </summary>
    public class SignatureResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string PackagePath { get; set; } = string.Empty;
        public string? SignaturePath { get; set; }
        public SignatureInfo? SignatureInfo { get; set; }
        public DateTime SigningTime { get; set; }
    }

    /// <summary>
    /// 签名信息
    /// </summary>
    public class SignatureInfo
    {
        public string Algorithm { get; set; } = string.Empty;
        public string HashAlgorithm { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Signer { get; set; } = string.Empty;
        public string PackageHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// 包信息
    /// </summary>
    public class PackageInfo
    {
        public string PackagePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public PackageManifest? Manifest { get; set; }
        public int FileCount { get; set; }
        public long UncompressedSize { get; set; }
        public double CompressionRatio { get; set; }
        public bool IsSigned { get; set; }
    }

    /// <summary>
    /// 打包进度
    /// </summary>
    public class PackageProgress
    {
        public string Stage { get; set; } = string.Empty;
        public string? CurrentFile { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public int PercentComplete { get; set; }
    }

    #endregion
}