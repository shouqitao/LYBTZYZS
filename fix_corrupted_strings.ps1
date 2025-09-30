# 修复损坏字符串常量的PowerShell脚本
# Issue #815 Phase 3 - 修复批量命名空间替换造成的字符串损坏

Write-Host "开始修复损坏的字符串常量..." -ForegroundColor Green

# 定义修复映射表
$fixMappings = @{
    # ApiErrorHandler.cs 修复
    "未授权，请重新登�?" = "未授权，请重新登录"
    "服务器内部错�?" = "服务器内部错误"
    "服务暂时不可�?" = "服务暂时不可用"
    "如果解析失败，返回原始错误消�?" = "如果解析失败，返回原始错误消息"
    
    # PermissionService.cs 修复
    "只有sysadmin有所有权�?" = "只有sysadmin有所有权限"
    "检查用户是否有管理员权�?" = "检查用户是否有管理员权限"
    "检查用户是否有超级管理员权�?" = "检查用户是否有超级管理员权限"
    "管理员有所有模�?" = "管理员有所有模块"
    "患者管�?" = "患者管理"
    "获取用户角色的显示名�?" = "获取用户角色的显示名称"
    "管理员有所有权�?" = "管理员有所有权限"
    "获取角色的显示名�?" = "获取角色的显示名称"
    "系统管理�?" = "系统管理员"
    "收银�?" = "收银员"
    "理疗�?" = "理疗师"
    
    # 其他常见损坏模式
    "�?" = "录"  # 登录的录
    "ufffd" = ""  # 清除损坏的Unicode字符
    "＄" = ""  # 清除错误的全角美元符号
}

# 需要修复的文件列表
$filesToFix = @(
    "src\Client\Desktop\Services\ApiErrorHandler.cs",
    "src\Client\Desktop\Services\PermissionService.cs",
    "src\Client\Desktop\Services\Handlers\AuthHeaderHandler.cs",
    "src\Client\Desktop\Services\UserSessionManager.cs",
    "src\Client\Desktop\Modules\Users\ViewModels\UserEditViewModel.cs"
)

foreach ($file in $filesToFix) {
    $fullPath = Join-Path $PWD $file
    if (Test-Path $fullPath) {
        Write-Host "修复文件: $file" -ForegroundColor Yellow
        
        # 读取文件内容
        $content = Get-Content $fullPath -Raw -Encoding UTF8
        
        # 应用修复映射
        foreach ($key in $fixMappings.Keys) {
            if ($content.Contains($key)) {
                $content = $content.Replace($key, $fixMappings[$key])
                Write-Host "  修复: $key -> $($fixMappings[$key])" -ForegroundColor Cyan
            }
        }
        
        # 额外的字符串修复 - 修复分行的字符串字面量
        $content = $content -replace '"\s*\r?\n\s*"', ''  # 合并分行字符串
        $content = $content -replace '"[^"]*\r?\n[^"]*"', '""'  # 修复包含换行的字符串
        
        # 写回文件
        $content | Out-File $fullPath -Encoding UTF8 -NoNewline
        
        Write-Host "  完成修复: $file" -ForegroundColor Green
    } else {
        Write-Host "文件不存在: $file" -ForegroundColor Red
    }
}

Write-Host "字符串修复完成！" -ForegroundColor Green