# Ensure module README structure for src/Server and src/Client modules
# Adds missing sections with placeholders in a consistent order:
#   1) 项目概述  2) 项目结构  3) 技术栈  4) 快速开始  5) API 接口（可选）  6) 相关文档（可选）

param(
  [string]$Root = "."
)

$ErrorActionPreference = 'Stop'

function Test-HasSection([string]$content, [string]$keyword) {
  return [Regex]::IsMatch($content, "(?m)^##\s*.*$([Regex]::Escape($keyword))")
}

function Add-Section([string]$content, [string]$heading, [string]$body) {
  $nl = "`r`n"
  if ($content -notmatch "${nl}$") { $content += $nl }
  return $content + $nl + $heading + $nl + $body + $nl
}

function Get-ModuleName([string]$path) {
  $dir = Split-Path -Parent $path
  return Split-Path -Leaf $dir
}

function Get-ModuleBase([string]$moduleName) {
  if ($moduleName -match '^LYBT\.Module\.(.+)$') { return $Matches[1] }
  if ($moduleName -match '^LYBT\.(.+)$') { return $Matches[1] }
  return $moduleName
}

$targets = Get-ChildItem -Path $Root -Recurse -File -Filter README.md |
  Where-Object { $_.FullName -match "\\src\\(Server|Client)\\" -and $_.FullName -notmatch "\\.worktrees\\" }

$changed = @()
foreach ($f in $targets) {
  $text = Get-Content -Raw -LiteralPath $f.FullName
  $module = Get-ModuleName $f.FullName
  $moduleBase = Get-ModuleBase $module
  $isServer = $f.FullName -match "\\src\\Server\\"
  $isClient = $f.FullName -match "\\src\\Client\\"
  $inModules = $f.FullName -match "\\src\\Server\\Modules\\|\\src\\Client\\Desktop\\Modules\\"
  $updated = $false

  # Mandatory sections and placeholders
  $sections = @(
    @{ key = '项目概述'; heading = '## 🎯 项目概述'; body = "- [待补充] 简要描述 ${module} 的职责、边界及与其他模块关系。" },
    @{ key = '项目结构'; heading = '## 📦 项目结构'; body = "- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。" },
    @{ key = '技术栈';   heading = '## 🛠 技术栈'; body = "- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。" },
    @{ key = '快速开始'; heading = '## 🚀 快速开始'; body = "- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。" }
  )

  # Optional sections: API 接口, 相关文档 (now enforced as required per request)
  $apiPrefix = ($moduleBase.ToLowerInvariant())
  if ($isServer -and $inModules) {
    $apiBody = "- [待补充] API 路由前缀：/api/v1/${apiPrefix}`n- [待补充] 控制器与端点：列出主要 Controller 与示例端点`n- 参考 WebAPI：src/Server/Services/LYBT.WebAPI/README.md"
  } elseif ($isClient) {
    $apiBody = "- [待补充] 集成的 API/Refit 客户端：例如 I${moduleBase}Api`n- [待补充] 关键调用路径与鉴权方式（JWT Bearer）"
  } else {
    $apiBody = "- [待补充] 相关 API 或对外接口描述"
  }

  $refsBody = "- docs/architecture/overview.md`n- docs/api/README.md`n- docs/modules/index.md`n- [待补充] 本模块相关的设计/实现文档链接"

  $sections += @(
    @{ key = 'API 接口'; heading = '## 🔌 API 接口'; body = $apiBody },
    @{ key = '相关文档'; heading = '## 📚 相关文档'; body = $refsBody }
  )

  foreach ($s in $sections) {
    if (-not (Test-HasSection $text $s.key)) {
      $text = Add-Section $text $s.heading $s.body
      $updated = $true
    }
  }

  if ($updated) {
    [System.IO.File]::WriteAllText($f.FullName, $text, [System.Text.Encoding]::UTF8)
    $changed += $f.FullName
  }
}

if ($changed.Count -gt 0) {
  Write-Host ("Updated files:`n" + ($changed -join "`n"))
} else {
  Write-Host 'All module README files already contain required sections.'
}
