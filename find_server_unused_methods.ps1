# PowerShell脚本：专门查找服务器端未使用的私有方法
param(
    [string]$SourcePath = "src/Server"
)

# 获取服务器端所有C#文件
$csFiles = Get-ChildItem -Path $SourcePath -Recurse -Filter "*.cs" | Where-Object { 
    $_.FullName -notmatch "\\obj\\" -and 
    $_.FullName -notmatch "\\bin\\" -and
    $_.FullName -notmatch "Migrations\\" -and
    $_.FullName -notmatch "\.Designer\.cs$" -and
    $_.FullName -notmatch "AssemblyInfo\.cs$" -and
    $_.FullName -notmatch "GlobalUsings\.cs$"
}

Write-Host "分析服务器端 $($csFiles.Count) 个C#文件..."

$unusedMethods = @()
$processedFiles = 0

foreach ($file in $csFiles) {
    $processedFiles++
    if ($processedFiles % 10 -eq 0) {
        Write-Host "已处理 $processedFiles/$($csFiles.Count) 文件..."
    }
    
    $content = Get-Content -Path $file.FullName -Raw
    if (-not $content) { continue }
    
    # 查找私有方法定义（更严格的模式）
    $privateMethodPattern = 'private\s+(?:static\s+)?(?:async\s+)?(?:[\w<>\[\]]+\s+)+(\w+)\s*\([^{]*\)\s*(?:=>\s*[^;]+;|\{)'
    $privateMethodMatches = [regex]::Matches($content, $privateMethodPattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    
    foreach ($match in $privateMethodMatches) {
        $methodName = $match.Groups[1].Value
        
        # 跳过特殊方法
        if ($methodName -match '^(get_|set_|add_|remove_|\.ctor|Finalize|ToString|GetHashCode|Equals)' -or 
            $methodName -eq $file.BaseName -or
            $methodName -match '^On[A-Z]' -or  # 事件处理器
            $methodName -match 'Handler$' -or  # 事件处理器
            $methodName -match '^Handle[A-Z]') { # 事件处理器
            continue
        }
        
        # 计算方法名在文件中出现的次数
        $methodNamePattern = '\b' + [regex]::Escape($methodName) + '\b'
        $occurrences = ([regex]::Matches($content, $methodNamePattern)).Count
        
        # 如果只出现1次（即定义处），则可能未被使用
        if ($occurrences -eq 1) {
            $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
            
            # 获取方法的完整签名
            $methodSignature = $match.Value
            if ($methodSignature.Length -gt 100) {
                $methodSignature = $methodSignature.Substring(0, 97) + "..."
            }
            
            $unusedMethods += [PSCustomObject]@{
                File = $file.FullName.Replace((Get-Location).Path + "\", "")
                Method = $methodName
                Line = $lineNumber
                Signature = $methodSignature.Trim()
            }
        }
    }
}

Write-Host "`n找到 $($unusedMethods.Count) 个可能未使用的服务器端私有方法："

if ($unusedMethods.Count -gt 0) {
    $unusedMethods | Sort-Object File, Line | Format-Table -Wrap -AutoSize
    
    # 保存结果到文件
    $unusedMethods | Sort-Object File, Line | Export-Csv -Path "server_unused_private_methods.csv" -NoTypeInformation -Encoding UTF8
    Write-Host "`n结果已保存到 server_unused_private_methods.csv"
    
    # 按文件分组显示
    Write-Host "`n按文件分组："
    $unusedMethods | Group-Object File | ForEach-Object {
        Write-Host "`n$($_.Name) ($($_.Count) 个方法):"
        $_.Group | ForEach-Object {
            Write-Host "  - $($_.Method) (行 $($_.Line))"
        }
    }
} else {
    Write-Host "没有找到明显未使用的私有方法。"
}