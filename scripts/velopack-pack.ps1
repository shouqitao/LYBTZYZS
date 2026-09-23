<#
.SYNOPSIS
    LYBTZYZS Desktop 发布打包（Velopack：Setup.exe + 更新源）。

.DESCRIPTION
    产出 Velopack 更新源目录（默认 dist/releases）：
      Setup.exe        安装器（自包含运行时，免装 .NET）
      RELEASES-<ch>.json / releases.<ch>.json   Velopack 馈源
      *-full.nupkg    完整包
      *-delta.nupkg   增量包（输出目录内已有更早版本时自动生成）

    版本号优先级：-Version 参数 > Directory.Build.props 的 VersionPrefix。
    打包版本与程序集版本通过 -p:Version 一并盖章，保证
    「安装包版本 == 应用内日志/关于页版本」。

.PARAMETER Version
    SemVer 版本号，如 0.0.2。省略时取 Directory.Build.props 的 VersionPrefix；
    显式传入时必须与 VersionPrefix 同 major.minor 且不低于它（版本策略见 12-desktop-release.md §0）。

.PARAMETER Channel
    Velopack 通道名（默认 win）。不同通道互不串更新。

.PARAMETER Msi
    额外产出 machine-wide 引导 .msi 包（默认只出 Setup.exe）。

.PARAMETER Clean
    打包前清空输出目录（会丢弃历史包 → 无法生成增量包）。

.EXAMPLE
    pwsh scripts/velopack-pack.ps1 -Version 0.0.2
    pwsh scripts/velopack-pack.ps1 -Version 0.0.2 -Msi

.NOTES
    前置：dotnet tool install -g vpk
    增量更新依赖「输出目录中保留旧版本包」——不要使用 -Clean，除非确实要重发基线。
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Channel = "win",
    [string]$Runtime = "win-x64",
    [string]$PackId = "LYBTZYZS",
    [string]$PackTitle = "凌隐宝堂中医诊所管理系统",
    [string]$PackAuthors = "凌隐宝堂",
    [string]$OutputDir = "dist/releases",
    [string]$PublishDir = "dist/publish",
    [string]$NotesFile = "",
    [switch]$Msi,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $shellProject = "src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj"
    $iconPath = "src/Client/Desktop/Shell/Assets/Icons/App/app.ico"

    # ---- 1. 解析版本号（单源 = Directory.Build.props 的 VersionPrefix） ----
    $props = Get-Content "Directory.Build.props" -Raw
    if ($props -notmatch '<VersionPrefix>([^<]+)</VersionPrefix>') {
        throw "Directory.Build.props 中没有 VersionPrefix——版本单源缺失，拒绝打包"
    }
    $versionPrefix = $Matches[1].Trim()

    if (-not $Version) {
        $Version = $versionPrefix
    }
    if ($Version -notmatch '^\d+\.\d+\.\d+$') {
        throw "版本号必须为 SemVer 三段式（如 0.0.1），当前：$Version"
    }

    # 版本策略（docs/06-operations/12-desktop-release.md §0）：单源 VersionPrefix 决定「当前版本线」——
    # ① 显式 -Version 不得低于 VersionPrefix；② 不得偏离 VersionPrefix 的 major.minor 线
    #    （升到 0.1.x/1.0.x 前必须先改 VersionPrefix，否则打包会被这里拦下）。
    # 两条都由单源推导，不在脚本里硬编码任何版本号。
    $versionValue = [version]$Version
    $prefixValue = [version]$versionPrefix
    if ($versionValue -lt $prefixValue) {
        throw "显式 -Version $Version 低于版本单源 VersionPrefix $versionPrefix（Directory.Build.props）"
    }
    if ($versionValue.Major -ne $prefixValue.Major -or $versionValue.Minor -ne $prefixValue.Minor) {
        throw "版本线不符：-Version $Version 的 major.minor 与版本单源 VersionPrefix $versionPrefix 不一致。" +
        "升版本线必须先更新 Directory.Build.props 的 VersionPrefix（版本策略见 docs/06-operations/12-desktop-release.md §0）"
    }

    Write-Host "==> 打包 LYBTZYZS Desktop v$Version (channel=$Channel, runtime=$Runtime)"

    # ---- 2. 工具校验 ----
    if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
        throw "未找到 vpk（Velopack CLI）。请先执行：dotnet tool install -g vpk"
    }
    Write-Host "    vpk: $((vpk --version) 2>&1 | Select-Object -First 1)"

    # ---- 3. dotnet publish ----
    # 刻意 **不** 使用 PublishSingleFile：单文件会把整个应用压成一个大文件，
    # Velopack 的增量包（按文件差分）将退化为全量包，丧失「增量更新」能力。
    if ($Clean -and (Test-Path $PublishDir)) { Remove-Item $PublishDir -Recurse -Force }
    Write-Host "==> dotnet publish（自包含、非单文件 → 保留增量差分粒度）..."
    dotnet publish $shellProject -c Release -r $Runtime --self-contained true `
        -p:PublishSingleFile=false `
        -p:Version=$Version `
        -p:InformationalVersion=$Version `
        -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败（exit $LASTEXITCODE）" }

    $mainExe = Join-Path $PublishDir "LYBT.Desktop.Shell.exe"
    if (-not (Test-Path $mainExe)) { throw "发布目录缺少主程序：$mainExe" }

    # ---- 4. vpk pack ----
    if ($Clean -and (Test-Path $OutputDir)) { Remove-Item $OutputDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

    $packArgs = @(
        "pack",
        "--packId", $PackId,
        "--packVersion", $Version,
        "--packDir", $PublishDir,
        "--mainExe", "LYBT.Desktop.Shell.exe",
        "--outputDir", $OutputDir,
        "--channel", $Channel,
        "--runtime", $Runtime,
        "--packTitle", $PackTitle,
        "--packAuthors", $PackAuthors,
        # 默认 BestSpeed：输出目录内存在旧版本时自动生成 *-delta.nupkg
        "--delta", "BestSpeed"
    )
    if (Test-Path $iconPath) { $packArgs += @("--icon", $iconPath) }
    if ($NotesFile -and (Test-Path $NotesFile)) { $packArgs += @("--releaseNotes", $NotesFile) }
    if ($Msi) { $packArgs += @("--msi", "true") }

    Write-Host "==> vpk pack ..."
    & vpk @packArgs
    if ($LASTEXITCODE -ne 0) { throw "vpk pack 失败（exit $LASTEXITCODE）" }

    # ---- 5. 馈源自检（客户端能否发现本次版本） ----
    # Velopack 客户端读的是 releases.<channel>.json（清单），不是 RELEASES（后者仅兼容旧版 Squirrel 客户端）。
    $manifestPath = Join-Path $OutputDir "releases.$Channel.json"
    if (-not (Test-Path $manifestPath)) { throw "缺少馈源清单：$manifestPath" }

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $packedVersions = @($manifest.Assets | ForEach-Object { $_.Version } | Select-Object -Unique)
    if ($packedVersions -notcontains $Version) {
        throw "馈源清单中不含本次版本 $Version（实际：$($packedVersions -join ', ')）"
    }
    Write-Host "==> 馈源自检通过：releases.$Channel.json 含版本 $($packedVersions -join ', ')（资产 $($manifest.Assets.Count) 个）"

    # ---- 6. 产物清单 + SHA256 ----
    Write-Host "==> 产物（$OutputDir）:"
    $artifacts = Get-ChildItem $OutputDir -File | Sort-Object Name
    $manifestLines = @()
    foreach ($f in $artifacts) {
        $hash = (Get-FileHash $f.FullName -Algorithm SHA256).Hash
        $sizeMb = [math]::Round($f.Length / 1MB, 2)
        Write-Host ("    {0,-45} {1,8} MB  {2}" -f $f.Name, $sizeMb, $hash.Substring(0, 12))
        $manifestLines += "$hash  $($f.Name)"
    }

    $sumsPath = Join-Path $OutputDir "SHA256SUMS.txt"
    $manifestLines | Set-Content $sumsPath -Encoding UTF8
    Write-Host "==> 校验清单: $sumsPath"

    $hasDelta = (Get-ChildItem $OutputDir -Filter "*-delta.nupkg" -File).Count -gt 0
    Write-Host "==> 增量包: $(if ($hasDelta) { '已生成' } else { '未生成（输出目录内无更早版本——首次发版属正常）' })"
    Write-Host "==> 下一步："
    Write-Host "    · 自建更新源：把 $OutputDir 整体同步到 FeedUrl 指向的目录（需可被 HTTP 目录访问）"
    Write-Host "    · GitHub Releases（主渠道）：为版本打 tag 后上传 $OutputDir 内的 releases.$Channel.json 与 *.nupkg"
    Write-Host "      （releases.$Channel.json 是必须的清单资产——Velopack 的 git 源据此枚举包）"
    Write-Host "    · Gitee Releases（可选镜像渠道）：同上" 
}
finally {
    Pop-Location
}
