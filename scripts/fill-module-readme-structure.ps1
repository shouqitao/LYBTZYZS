# Fill module README sections (项目结构/技术栈/快速开始) with actual content where placeholders exist

param(
  [string]$Root = "."
)

$ErrorActionPreference = 'Stop'

function Has-Placeholder([string]$content, [string]$section) {
  $m = [Regex]::Match($content, "(?s)(?m)^##\s*.*${section}.*?\r?\n(?<body>[\s\S]*?)(?=^##\s|\Z)")
  if (-not $m.Success) { return $false }
  return $m.Groups['body'].Value -match '\[待补充\]'
}

function Replace-Section([string]$content, [string]$section, [string]$body) {
  return [Regex]::Replace($content, "(?s)(?m)^##\s*.*${section}.*?\r?\n[\s\S]*?(?=^##\s|\Z)", "## ${section}`r`n${body}`r`n")
}

function Build-Server-Structure([string]$moduleDir) {
  $parts = @()
  foreach ($d in 'Interfaces','Services','Repositories','Mapping') {
    if (Test-Path (Join-Path $moduleDir $d)) { $parts += "- ${d}: 模块${d}层/文件" }
  }
  if ($parts.Count -eq 0) { $parts = @('- 结构概览：请补充子目录与职责说明') }
  return ($parts -join "`r`n")
}

function Build-Client-Structure([string]$moduleDir) {
  $parts = @()
  foreach ($d in 'Views','ViewModels','Services','Interfaces') {
    if (Test-Path (Join-Path $moduleDir $d)) { $parts += "- ${d}: 模块${d}层/文件" }
  }
  if ($parts.Count -eq 0) { $parts = @('- 结构概览：请补充子目录与职责说明') }
  return ($parts -join "`r`n")
}

function Build-Server-Tech() {
  return "- .NET 8 + ASP.NET Core`r`n- Entity Framework Core`r`n- AutoMapper`r`n- FluentValidation (如使用)"
}
function Build-Client-Tech() {
  return "- .NET 8 + WPF`r`n- Prism.DryIoc`r`n- Refit (类型安全 HTTP 客户端)`r`n- AutoMapper (如使用)"
}

function Build-QuickStart([string]$type) {
  if ($type -eq 'server') {
    return "- 还原依赖：dotnet restore LYBT.Server.sln`r`n- 构建：dotnet build LYBT.Server.sln -c Release --no-restore`r`n- 运行 WebAPI：dotnet run --project src/Server/Services/LYBT.WebAPI"
  } else {
    return "- 还原依赖：dotnet restore LYBT.Desktop.sln`r`n- 构建：dotnet build LYBT.Desktop.sln -c Release --no-restore`r`n- 运行示例：在 Shell 或具体模块项目中 F5 调试"
  }
}

$changed = @()

# Server modules
Get-ChildItem -Path (Join-Path $Root 'src/Server/Modules') -Directory | ForEach-Object {
  $readme = Join-Path $_.FullName 'README.md'
  if (-not (Test-Path $readme)) { return }
  $text = Get-Content -Raw -LiteralPath $readme
  $updated = $false
  if (Has-Placeholder $text '📦\s*项目结构') {
    $body = Build-Server-Structure $_.FullName
    $text = Replace-Section $text '📦 项目结构' $body; $updated = $true
  }
  if (Has-Placeholder $text '🛠\s*技术栈') {
    $body = Build-Server-Tech
    $text = Replace-Section $text '🛠 技术栈' $body; $updated = $true
  }
  if (Has-Placeholder $text '🚀\s*快速开始') {
    $body = Build-QuickStart 'server'
    $text = Replace-Section $text '🚀 快速开始' $body; $updated = $true
  }
  if ($updated) { [IO.File]::WriteAllText($readme, $text, [Text.Encoding]::UTF8); $changed += $readme }
}

# Client modules
Get-ChildItem -Path (Join-Path $Root 'src/Client/Desktop/Modules') -Directory | ForEach-Object {
  $readme = Join-Path $_.FullName 'README.md'
  if (-not (Test-Path $readme)) { return }
  $text = Get-Content -Raw -LiteralPath $readme
  $updated = $false
  if (Has-Placeholder $text '📦\s*项目结构') {
    $body = Build-Client-Structure $_.FullName
    $text = Replace-Section $text '📦 项目结构' $body; $updated = $true
  }
  if (Has-Placeholder $text '🛠\s*技术栈') {
    $body = Build-Client-Tech
    $text = Replace-Section $text '🛠 技术栈' $body; $updated = $true
  }
  if (Has-Placeholder $text '🚀\s*快速开始') {
    $body = Build-QuickStart 'client'
    $text = Replace-Section $text '🚀 快速开始' $body; $updated = $true
  }
  if ($updated) { [IO.File]::WriteAllText($readme, $text, [Text.Encoding]::UTF8); $changed += $readme }
}

if ($changed.Count -gt 0) {
  Write-Host ("Updated files:`n" + ($changed -join "`n"))
} else {
  Write-Host 'No structure/tech/quickstart placeholders to fill.'
}

