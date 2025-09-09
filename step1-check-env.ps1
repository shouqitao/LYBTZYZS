# === Step 1: 环境与规则快照（不编译、不改代码） ===
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# 建议：在仓库根目录运行（示例：D:\source\repos\LYBTZYZS）
$repoRoot = Get-Location
$reports  = Join-Path $repoRoot "_reports"
New-Item -ItemType Directory -Force -Path $reports | Out-Null

# A. .NET 环境
dotnet --info          | Out-File -Encoding UTF8 (Join-Path $reports "dotnet-info.txt")
dotnet --list-sdks     | Out-File -Encoding UTF8 (Join-Path $reports "dotnet-sdks.txt")
dotnet --list-runtimes | Out-File -Encoding UTF8 (Join-Path $reports "dotnet-runtimes.txt")

# B. 关键规则文件快照（存在性与修改时间）
$rulePaths = @(
  ".\.claude\prds\PRD-Consistency-Unification.md",
  ".\.ai\rules.json",
  ".\.editorconfig",
  ".\Directory.Build.props",
  ".\Directory.Packages.props"
)

$rulesFile = Join-Path $reports "rules-files.txt"
"FullName`tExists`tLastWriteTime`tLength" | Out-File -Encoding UTF8 $rulesFile
foreach ($p in $rulePaths) {
  $f = Resolve-Path $p -ErrorAction SilentlyContinue
  if ($f) {
    $fi = Get-Item $f
    "$($fi.FullName)`tYES`t$($fi.LastWriteTime)`t$($fi.Length)" | Out-File -Append -Encoding UTF8 $rulesFile
  } else {
    "$((Join-Path $repoRoot $p))`tNO`t`t" | Out-File -Append -Encoding UTF8 $rulesFile
  }
}

# C. 风格取样（命名/API 路由/nullable）
$styleFile = Join-Path $reports "style-samples.txt"
Get-ChildItem -Recurse -Include *.cs | Select-String -Pattern `
  "\bUserName\b", "/api/v1", "/api/v", "#\s*nullable\s+enable" |
  Group-Object Path | ForEach-Object {
    "== $($_.Name) ==";
    ($_.Group | Select-Object -ExpandProperty Line) -join "`n";
    ""
  } | Out-File -Encoding UTF8 $styleFile

Write-Host "✅ Step 1 完成：_reports 目录已生成 dotnet-info / rules-files / style-samples 等文件。"
