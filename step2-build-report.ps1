# === Step 2: 还原、编译、收集错误报告（不改代码） ===
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Get-Location
$reports  = Join-Path $repoRoot "_reports"
New-Item -ItemType Directory -Force -Path $reports | Out-Null

# A. 还原 & 构建（生成 binlog 便于精准定位）
dotnet restore
dotnet build -nologo -v:m -bl:(Join-Path $reports "build.binlog") 2>&1 `
  | Tee-Object -FilePath (Join-Path $reports "build-console.txt")

# B. 统计最常见编译错误码（Top 10）
Get-Content (Join-Path $reports "build-console.txt") |
  Select-String -Pattern "error CS\d{4}" |
  ForEach-Object { $_.Matches.Value } |
  Group-Object | Sort-Object Count -Descending |
  Select-Object -First 10 |
  Format-Table Name,Count |
  Out-String | Set-Content (Join-Path $reports "error-summary.txt")

# C. 包版本与漏洞检查（帮助判断依赖层面问题）
dotnet list package                | Out-File -Encoding UTF8 (Join-Path $reports "packages.txt")
dotnet list package --vulnerable   | Out-File -Encoding UTF8 (Join-Path $reports "vulnerable.txt")

Write-Host "✅ Step 2 完成：_reports 已生成 build.binlog / build-console.txt / error-summary.txt / packages.txt / vulnerable.txt。"
