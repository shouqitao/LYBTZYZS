# 分析包引用脚本 - 检查重复依赖和版本冲突
# 用于 Issue #1070 - 清理重复的 PackageReference 和优化依赖管理

param(
    [string]$RepoPath = (Get-Location),
    [string]$OutputPath = "package-analysis-report.csv"
)

Write-Host "开始分析包引用..." -ForegroundColor Green
Write-Host "仓库路径: $RepoPath" -ForegroundColor Yellow

# 获取所有 .csproj 文件
$csprojFiles = Get-ChildItem -Path $RepoPath -Filter "*.csproj" -Recurse
Write-Host "找到 $($csprojFiles.Count) 个项目文件" -ForegroundColor Yellow

# 存储包引用信息
$packageReferences = @()
$duplicatePackages = @{}
$versionConflicts = @{}

# 分析每个项目文件
foreach ($csproj in $csprojFiles) {
    Write-Host "分析项目: $($csproj.Name)" -ForegroundColor Cyan
    
    try {
        [xml]$content = Get-Content $csproj.FullName -Encoding UTF8
        
        # 获取 PackageReference 节点
        $packageRefs = $content.Project.ItemGroup.PackageReference
        
        if ($packageRefs) {
            foreach ($package in $packageRefs) {
                if ($package.Include) {
                    $packageInfo = [PSCustomObject]@{
                        ProjectFile = $csproj.Name
                        ProjectPath = $csproj.FullName
                        PackageName = $package.Include
                        Version = $package.Version
                        PrivateAssets = $package.PrivateAssets
                        IncludeAssets = $package.IncludeAssets
                        ExcludeAssets = $package.ExcludeAssets
                    }
                    
                    $packageReferences += $packageInfo
                    
                    # 检查重复包
                    $key = "$($package.Include)"
                    if ($duplicatePackages.ContainsKey($key)) {
                        $duplicatePackages[$key] += $packageInfo
                    } else {
                        $duplicatePackages[$key] = @($packageInfo)
                    }
                    
                    # 检查版本冲突
                    $versionKey = "$($package.Include)_$($package.Version)"
                    if ($versionConflicts.ContainsKey($package.Include)) {
                        if ($versionConflicts[$package.Include] -notcontains $package.Version) {
                            $versionConflicts[$package.Include] += @($package.Version)
                        }
                    } else {
                        $versionConflicts[$package.Include] = @($package.Version)
                    }
                }
            }
        }
    }
    catch {
        Write-Warning "无法解析项目文件: $($csproj.FullName) - $($_.Exception.Message)"
    }
}

# 导出详细分析结果
$packageReferences | Export-Csv -Path $OutputPath -Encoding UTF8 -NoTypeInformation
Write-Host "详细包引用信息已导出到: $OutputPath" -ForegroundColor Green

# 分析重复包（在多个项目中引用的包）
Write-Host "`n=== 重复包分析 ===" -ForegroundColor Yellow
$duplicateCount = 0
foreach ($packageName in $duplicatePackages.Keys) {
    $occurrences = $duplicatePackages[$packageName]
    if ($occurrences.Count -gt 1) {
        $duplicateCount++
        Write-Host "包 '$packageName' 在 $($occurrences.Count) 个项目中被引用:" -ForegroundColor Red
        foreach ($occurrence in $occurrences) {
            Write-Host "  - $($occurrence.ProjectFile) (版本: $($occurrence.Version))" -ForegroundColor White
        }
        Write-Host ""
    }
}

if ($duplicateCount -eq 0) {
    Write-Host "未发现重复包引用" -ForegroundColor Green
} else {
    Write-Host "发现 $duplicateCount 个重复包" -ForegroundColor Red
}

# 分析版本冲突
Write-Host "`n=== 版本冲突分析 ===" -ForegroundColor Yellow
$conflictCount = 0
foreach ($packageName in $versionConflicts.Keys) {
    $versions = $versionConflicts[$packageName] | Where-Object { $_ -ne $null -and $_ -ne "" } | Sort-Object -Unique
    if ($versions.Count -gt 1) {
        $conflictCount++
        Write-Host "包 '$packageName' 存在版本冲突:" -ForegroundColor Red
        foreach ($version in $versions) {
            $projects = ($packageReferences | Where-Object { $_.PackageName -eq $packageName -and $_.Version -eq $version }).ProjectFile
            Write-Host "  版本 $version 使用项目: $($projects -join ', ')" -ForegroundColor White
        }
        Write-Host ""
    }
}

if ($conflictCount -eq 0) {
    Write-Host "未发现版本冲突" -ForegroundColor Green
} else {
    Write-Host "发现 $conflictCount 个版本冲突" -ForegroundColor Red
}

# 统计信息
Write-Host "`n=== 统计信息 ===" -ForegroundColor Yellow
Write-Host "总项目数: $($csprojFiles.Count)" -ForegroundColor White
Write-Host "总包引用数: $($packageReferences.Count)" -ForegroundColor White
Write-Host "唯一包数: $($duplicatePackages.Keys.Count)" -ForegroundColor White
Write-Host "重复包数: $duplicateCount" -ForegroundColor White
Write-Host "版本冲突包数: $conflictCount" -ForegroundColor White

# 检查是否使用了中央包管理
$directoryPackagesProps = Join-Path $RepoPath "Directory.Packages.props"
if (Test-Path $directoryPackagesProps) {
    Write-Host "`n=== 中央包管理检查 ===" -ForegroundColor Yellow
    Write-Host "发现 Directory.Packages.props 文件" -ForegroundColor Green
    
    # 检查是否有项目仍在使用本地版本号
    $localVersions = $packageReferences | Where-Object { $_.Version -ne $null -and $_.Version -ne "" }
    if ($localVersions.Count -gt 0) {
        Write-Host "警告: 发现 $($localVersions.Count) 个包引用仍在使用本地版本号（应该移除）:" -ForegroundColor Red
        $localVersions | ForEach-Object {
            Write-Host "  - $($_.ProjectFile): $($_.PackageName) v$($_.Version)" -ForegroundColor White
        }
    } else {
        Write-Host "所有项目都正确使用中央包管理" -ForegroundColor Green
    }
} else {
    Write-Host "未找到 Directory.Packages.props 文件" -ForegroundColor Red
}

Write-Host "`n分析完成！" -ForegroundColor Green