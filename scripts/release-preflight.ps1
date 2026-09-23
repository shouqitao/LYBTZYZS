<#
.SYNOPSIS
    LYBTZYZS 发布前门禁 —— 版本号标准 §4.3「发布 checklist」的机器化实现。

.DESCRIPTION
    一次性校验以下五项，**全部通过才放行**（任一失败即 throw / 非零退出）：

      ① 版本单源：读 Directory.Build.props 的 <VersionPrefix>                —— 标准 §1
      ② 馈源清单：releases.<channel>.json 内版本 == VersionPrefix            —— 标准 §4.3
         （且 VersionPrefix 必须是清单中的最新版本——旧版本条目保留用于增量包）
      ③ 校验清单：SHA256SUMS.txt 与输出目录实际文件**逐项一致**（无缺失/无多余/哈希相符）—— 标准 §4.3
      ④ 标签一致与号码冻结：v{VersionPrefix} 不得已存在于远端                     —— 标准 §3（已发布号永不复用）
         （本地同名 tag 若存在，必须指向 HEAD——否则会为错误提交打标签）             —— 标准 §4.3
      ⑤ 通过后打印建议的 gh release create 命令（资产 = Setup.exe + 清单 + nupkg + SHA256SUMS.txt）

    门禁**不提供跳过开关**：标准 §4「任何角色不得绕过、不得放宽」。远端不可达时校验失败即中止发布。

.PARAMETER OutputDir
    打包输出目录（默认 dist/releases，与 velopack-pack.ps1 一致）。

.PARAMETER Channel
    Velopack 通道名（默认 win），决定清单文件名 releases.<channel>.json。

.PARAMETER Remote
    远端名（默认 origin，即 GitHub 主远端）。

.EXAMPLE
    pwsh scripts/velopack-pack.ps1
    pwsh scripts/release-preflight.ps1
    # 通过后按打印的 gh release create 命令发布

.NOTES
    前置：已完成打包（velopack-pack.ps1）；已 git push（本地与远端一致）。
#>
[CmdletBinding()]
param(
    [string]$OutputDir = "dist/releases",
    [string]$Channel = "win",
    [string]$Remote = "origin"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    Write-Host "==> 发布前门禁（版本号标准 §4.3）"

    # ---- ① 版本单源（标准 §1）----
    $props = Get-Content "Directory.Build.props" -Raw
    if ($props -notmatch '<VersionPrefix>([^<]+)</VersionPrefix>') {
        throw "① 版本单源缺失：Directory.Build.props 中没有 VersionPrefix"
    }
    $versionPrefix = $Matches[1].Trim()
    if ($versionPrefix -notmatch '^\d+\.\d+\.\d+$') {
        throw "① VersionPrefix 不是 SemVer 三段式：$versionPrefix"
    }
    Write-Host "    ① 版本单源 VersionPrefix = $versionPrefix"

    # ---- ② 馈源清单版本（标准 §4.3）----
    if (-not (Test-Path $OutputDir)) {
        throw "② 输出目录不存在：$OutputDir（先执行 scripts/velopack-pack.ps1）"
    }
    $manifestPath = Join-Path $OutputDir "releases.$Channel.json"
    if (-not (Test-Path $manifestPath)) {
        throw "② 缺少馈源清单：$manifestPath"
    }
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $manifestVersions = @($manifest.Assets | ForEach-Object { $_.Version } | Select-Object -Unique)
    if ($manifestVersions -notcontains $versionPrefix) {
        throw "② 清单 $manifestPath 不含版本单源 $versionPrefix（实际：$($manifestVersions -join ', ')）"
    }
    $latest = $manifestVersions | Sort-Object { [version]$_ } | Select-Object -Last 1
    if ([version]$latest -ne [version]$versionPrefix) {
        throw "② 清单最新版本为 $latest，与版本单源 $versionPrefix 不一致——不得发布非最新版本"
    }
    Write-Host "    ② 清单 releases.$Channel.json 最新版本 = $latest（含历史版本：$($manifestVersions -join ', ')）"

    # ---- ③ SHA256SUMS.txt 与实际文件逐项一致（标准 §4.3）----
    $sumsPath = Join-Path $OutputDir "SHA256SUMS.txt"
    if (-not (Test-Path $sumsPath)) {
        throw "③ 缺少校验清单：$sumsPath"
    }
    $declared = @{}
    foreach ($line in Get-Content $sumsPath) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^([0-9A-Fa-f]{64})\s+(.+)$') {
            throw "③ SHA256SUMS.txt 行格式非法：$line"
        }
        $declared[$Matches[2].Trim()] = $Matches[1].ToUpperInvariant()
    }
    if ($declared.Count -eq 0) {
        throw "③ SHA256SUMS.txt 为空：$sumsPath"
    }

    $actualFiles = Get-ChildItem $OutputDir -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" }
    $mismatches = @()
    foreach ($f in $actualFiles) {
        if (-not $declared.ContainsKey($f.Name)) {
            $mismatches += "未登记：$($f.Name)"
            continue
        }
        $hash = (Get-FileHash $f.FullName -Algorithm SHA256).Hash
        if ($hash -ne $declared[$f.Name]) {
            $mismatches += "哈希不符：$($f.Name)（清单 $($declared[$f.Name].Substring(0,12))… / 实际 $($hash.Substring(0,12))…）"
        }
    }
    foreach ($name in $declared.Keys) {
        if (-not (Test-Path (Join-Path $OutputDir $name))) {
            $mismatches += "清单已登记但文件缺失：$name"
        }
    }
    if ($mismatches.Count -gt 0) {
        throw "③ SHA256SUMS.txt 与实际产物不一致：`n    - $($mismatches -join "`n    - ")"
    }
    Write-Host "    ③ SHA256SUMS.txt 逐项一致（$($declared.Count) 项）"

    # ---- ④ tag 一致与已发布号冻结（标准 §3 / §4.3）----
    $tag = "v$versionPrefix"
    $remoteTag = (& git ls-remote --tags $Remote "refs/tags/$tag" 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "④ 无法查询远端 $Remote 的 tag（网络/权限？）：$remoteTag"
    }
    if (-not [string]::IsNullOrWhiteSpace($remoteTag)) {
        throw "④ 远端已存在 tag $tag —— 该版本号已发布（标准 §3「已发布版本号永不复用」）。" +
        "若要再次发布，请先按标准 §3 提升 Directory.Build.props 的 VersionPrefix 并重新打包。"
    }
    $localTag = (& git tag --list $tag | Out-String).Trim()
    if (-not [string]::IsNullOrWhiteSpace($localTag)) {
        $tagCommit = (& git rev-list -n 1 $tag | Out-String).Trim()
        $headCommit = (& git rev-parse HEAD | Out-String).Trim()
        if ($tagCommit -ne $headCommit) {
            throw "④ 本地 tag $tag 指向 $($tagCommit.Substring(0,7))，与 HEAD $($headCommit.Substring(0,7)) 不一致——" +
            "发布必须为当前提交打标签（标准 §4.3「tag == VersionPrefix」）"
        }
        Write-Host "    ④ 本地 tag $tag 已存在且指向 HEAD（$($headCommit.Substring(0,7))）"
    }
    else {
        Write-Host "    ④ 远端无 $tag（版本号未发布过），本地待打标签"
    }

    # ---- ⑤ 放行 + 建议发布命令 ----
    $remoteUrl = (& git remote get-url $Remote | Out-String).Trim()
    $slug = ($remoteUrl -replace '^git@[^:]+:', '' -replace '^https?://[^/]+/', '' -replace '\.git$', '')
    $assets = @()
    foreach ($name in @("releases.$Channel.json", "SHA256SUMS.txt") + ($actualFiles | Where-Object { $_.Name -like "*.nupkg" -or $_.Name -like "*Setup*.exe" } | ForEach-Object { $_.Name })) {
        $assets += (Join-Path $OutputDir $name)
    }

    Write-Host "==> 门禁通过（① 单源 ② 清单 ③ 校验清单 ④ 标签/冻结）"
    Write-Host "==> 建议发布命令："
    Write-Host "    git tag $tag && git push $Remote $tag"
    Write-Host ("    gh release create {0} --repo {1} --title `"{0}`" --notes-file <发布说明.md> {2}" -f $tag, $slug, ($assets -join " "))
}
finally {
    Pop-Location
}
