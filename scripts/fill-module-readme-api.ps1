# Populate module README API 接口/相关文档 sections with actual data

param(
  [string]$Root = ".",
  [string[]]$Modules = @()
)

$ErrorActionPreference = 'Stop'

function Parse-ControllerInfo {
  param([string]$file)
  $text = Get-Content -Raw -LiteralPath $file
  $classMatch = [Regex]::Match($text, '(?m)^\s*public\s+class\s+(?<name>\w+)\s*:\s*')
  if (-not $classMatch.Success) { return $null }
  $className = $classMatch.Groups['name'].Value
  $routeMatch = [Regex]::Match($text, '\[Route\("(?<route>[^\"]+)"\)\]')
  $classRoute = if ($routeMatch.Success) { $routeMatch.Groups['route'].Value } else { '' }
  $endpointMatches = [Regex]::Matches($text, '\[(Http(Get|Post|Put|Patch|Delete))\("(?<path>[^\"]*)"\)\)\]')
  $endpoints = @()
  foreach ($m in $endpointMatches) {
    $verb = $m.Groups[1].Value
    $path = $m.Groups['path'].Value
    if ([string]::IsNullOrWhiteSpace($path)) { $path = '' }
    $endpoints += @{ Verb = $verb; Path = $path }
  }
  return @{ Name = $className; Route = $classRoute; Endpoints = $endpoints }
}

function Map-ModuleName {
  param([string]$controllerName)
  $name = $controllerName -replace 'Controller$',''
  $name = $name -replace 'Operation$',''
  return $name
}

# Build controllers map
$controllersPath = Join-Path $Root 'src/Server/Services/LYBT.WebAPI/Controllers'
$controllers = @{}
Get-ChildItem -Path $controllersPath -File -Filter '*Controller.cs' | ForEach-Object {
  $info = Parse-ControllerInfo -file $_.FullName
  if ($null -ne $info) {
    $module = Map-ModuleName -controllerName $info.Name
    if (-not $controllers.ContainsKey($module)) { $controllers[$module] = @() }
    $controllers[$module] += ,$info
  }
}

# Build Refit APIs map
$refitPath = Join-Path $Root 'src/Shared/LYBT.Shared.Interfaces/Api'
$refitMap = @{}
Get-ChildItem -Path $refitPath -File -Filter 'I*Api.cs' | ForEach-Object {
  $name = [IO.Path]::GetFileNameWithoutExtension($_.Name) # e.g., IUserApi
  $module = $name -replace '^I','' -replace 'Api$','' # User
  $text = Get-Content -Raw -LiteralPath $_.FullName
  $epMatches = [Regex]::Matches($text, '\[Refit\.(?<verb>Get|Post|Put|Patch|Delete)\("(?<path>[^\"]+)"\)\)\]')
  $eps = @()
  foreach ($m in $epMatches) {
    $eps += @{ Verb = $m.Groups['verb'].Value.ToUpperInvariant(); Path = $m.Groups['path'].Value }
  }
  $refitMap[$module] = @{ Interface = $name; Endpoints = $eps }
}

function Update-ServerModuleReadme {
  param([string]$readmePath)
  $text = Get-Content -Raw -LiteralPath $readmePath
  $moduleName = Split-Path -Leaf (Split-Path -Parent $readmePath) # e.g., LYBT.Module.Users
  $modBase = ($moduleName -replace '^LYBT\.Module\.','')
  if (-not $controllers.ContainsKey($modBase)) { return $false }
  $list = $controllers[$modBase]
  # Build API section text
  $sb = New-Object System.Text.StringBuilder
  foreach ($ctrl in $list) {
    $route = $ctrl.Route
    if ([string]::IsNullOrWhiteSpace($route)) { $route = 'api/v{version:apiVersion}/[controller]' }
    # Compose sample prefix for v1
    $prefix = $route.Replace('v{version:apiVersion}','v1').Replace('[controller]', ($ctrl.Name -replace 'Controller$',''))
    [void]$sb.AppendLine("- 控制器: ${($ctrl.Name)}  路由前缀: /${prefix}")
    if ($ctrl.Endpoints.Count -gt 0) {
      foreach ($ep in $ctrl.Endpoints) {
        $suffix = if ([string]::IsNullOrWhiteSpace($ep.Path)) { '' } else { '/' + $ep.Path }
        [void]$sb.AppendLine("  - ${($ep.Verb.ToUpper())} /${prefix}${suffix}")
      }
    }
  }
  $apiSection = $sb.ToString().TrimEnd()
  if ([string]::IsNullOrWhiteSpace($apiSection)) { return $false }
  # Replace API 接口 section body
  $text = [Regex]::Replace($text, '(?s)(?m)^##\s*🔌\s*API 接口\s*\r?\n[\s\S]*?(?=^##\s|\Z)', "## 🔌 API 接口`r`n$apiSection`r`n")
  # Update 相关文档: add links
  $refs = "- docs/architecture/overview.md`n- docs/api/README.md`n- docs/modules/index.md`n- src/Shared/LYBT.Shared.Interfaces/Api/I${modBase}Api.cs"
  $text = [Regex]::Replace($text, '(?s)(?m)^##\s*📚\s*相关文档\s*\r?\n[\s\S]*?(?=^##\s|\Z)', "## 📚 相关文档`r`n$refs`r`n")
  [IO.File]::WriteAllText($readmePath, $text, [Text.Encoding]::UTF8)
  return $true
}

function Update-ClientModuleReadme {
  param([string]$readmePath)
  $text = Get-Content -Raw -LiteralPath $readmePath
  $moduleName = Split-Path -Leaf (Split-Path -Parent $readmePath) # e.g., Users
  $modBase = $moduleName
  if (-not $refitMap.ContainsKey($modBase)) {
    # try singular
    if ($modBase -match 's$') {
      $modBase = $modBase.Substring(0, $modBase.Length-1)
    }
  }
  if (-not $refitMap.ContainsKey($modBase)) { return $false }
  $ref = $refitMap[$modBase]
  $sb = New-Object System.Text.StringBuilder
  [void]$sb.AppendLine("- Refit 接口: ${($ref.Interface)}")
  foreach ($ep in $ref.Endpoints) {
    [void]$sb.AppendLine("  - ${($ep.Verb)} ${($ep.Path)}")
  }
  $apiSection = $sb.ToString().TrimEnd()
  if ([string]::IsNullOrWhiteSpace($apiSection)) { return $false }
  $text = [Regex]::Replace($text, '(?s)(?m)^##\s*🔌\s*API 接口\s*\r?\n[\s\S]*?(?=^##\s|\Z)', "## 🔌 API 接口`r`n$apiSection`r`n")
  # 相关文档: add shared interface link
  $refs = "- docs/architecture/overview.md`n- docs/modules/index.md`n- src/Shared/LYBT.Shared.Interfaces/Api/I${modBase}Api.cs"
  $text = [Regex]::Replace($text, '(?s)(?m)^##\s*📚\s*相关文档\s*\r?\n[\s\S]*?(?=^##\s|\Z)', "## 📚 相关文档`r`n$refs`r`n")
  [IO.File]::WriteAllText($readmePath, $text, [Text.Encoding]::UTF8)
  return $true
}

$changed = @()

# Server modules
$serverDir = Join-Path $Root 'src/Server/Modules'
Get-ChildItem -Path $serverDir -Directory | ForEach-Object {
  $modBase = ($_.Name -replace '^LYBT\.Module\.', '')
  if ($Modules.Count -gt 0 -and $Modules -notcontains $modBase) { return }
  $readme = Join-Path $_.FullName 'README.md'
  if (Test-Path $readme) {
    if (Update-ServerModuleReadme -readmePath $readme) { $changed += $readme }
  }
}

# Client modules
$clientDir = Join-Path $Root 'src/Client/Desktop/Modules'
Get-ChildItem -Path $clientDir -Directory | ForEach-Object {
  $modBase = $_.Name
  if ($Modules.Count -gt 0 -and $Modules -notcontains $modBase -and $Modules -notcontains ($modBase -replace 's$','')) { return }
  $readme = Join-Path $_.FullName 'README.md'
  if (Test-Path $readme) {
    if (Update-ClientModuleReadme -readmePath $readme) { $changed += $readme }
  }
}

if ($changed.Count -gt 0) {
  Write-Host ("Updated files:`n" + ($changed -join "`n"))
} else {
  Write-Host 'No API/document links updates needed.'
}
