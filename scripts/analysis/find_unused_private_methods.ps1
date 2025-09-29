# PowerShell脚本：查找未使用的私有方法
param(
    [string]$SourcePath = "src"
)

# 获取所有C#文件
$csFiles = Get-ChildItem -Path $SourcePath -Recurse -Filter "*.cs" | Where-Object { 
    $_.FullName -notmatch "\\obj\\" -and 
    $_.FullName -notmatch "\\bin\\" -and
    $_.FullName -notmatch "Migrations\\" -and
    $_.FullName -notmatch "\.Designer\.cs$" -and
    $_.FullName -notmatch "AssemblyInfo\.cs$" -and
    $_.FullName -notmatch "GlobalUsings\.cs$"
}

Write-Host "分析 $($csFiles.Count) 个C#文件..."

$unusedMethods = @()
$processedFiles = 0

foreach ($file in $csFiles) {
    $processedFiles++
    if ($processedFiles % 10 -eq 0) {
        Write-Host "已处理 $processedFiles/$($csFiles.Count) 文件..."
    }
    
    $content = Get-Content -Path $file.FullName -Raw
    if (-not $content) { continue }
    
    # 查找私有方法定义（包括静态私有方法）
    $privateMethodMatches = [regex]::Matches($content, 'private\s+(?:static\s+)?(?:async\s+)?(?:\w+\s+)*\w+\s+(\w+)\s*\([^{]*\)\s*(?:=>\s*[^;]+;|\{)', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    
    foreach ($match in $privateMethodMatches) {
        $methodName = $match.Groups[1].Value
        
        # 跳过构造函数、属性访问器、事件访问器
        if ($methodName -match '^(get_|set_|add_|remove_|\.ctor)' -or $methodName -eq $file.BaseName) {
            continue
        }
        
        # 计算方法名在文件中出现的次数
        $occurrences = ([regex]::Matches($content, [regex]::Escape($methodName))).Count
        
        # 如果只出现1次（即定义处），则可能未被使用
        if ($occurrences -eq 1) {
            $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
            
            $unusedMethods += [PSCustomObject]@{
                File = $file.FullName.Replace((Get-Location).Path + "\", "")
                Method = $methodName
                Line = $lineNumber
                Definition = $match.Value.Split("`n")[0].Trim()
            }
        }
    }
}

Write-Host "`n找到 $($unusedMethods.Count) 个可能未使用的私有方法："
$unusedMethods | Sort-Object File, Line | Format-Table -AutoSize

# 保存结果到文件
$unusedMethods | Sort-Object File, Line | Export-Csv -Path "unused_private_methods.csv" -NoTypeInformation -Encoding UTF8
Write-Host "`n结果已保存到 unused_private_methods.csv"