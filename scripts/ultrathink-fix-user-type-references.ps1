# UltraThink修复User模块类型引用错误
# 1. UserModel -> User (实体类)
# 2. 添加缺失的using指令
# 3. ActionType的命名空间引用

Write-Host "UltraThink Fix User Module Type References" -ForegroundColor Cyan
Write-Host "Target: Fix UserModel, UserValidationHelper, ActionType references" -ForegroundColor Yellow

$fixes = @(
    # 修复UserModel -> User类型引用
    @{
        'pattern' = 'UserModel'
        'replacement' = 'User'
        'description' = 'Replace UserModel with User entity'
    }
    
    # 修复ActionType引用，添加命名空间using
    @{
        'pattern' = '^(\s*using [^;]+;\s*)*(?!.*LYBT\.Shared\.Models\.Enums.*)(using LYBT\.Shared\.Models\.Common;)'
        'replacement' = '$1using LYBT.Shared.Models.Enums;' + "`r`n" + '$3'
        'description' = 'Add missing LYBT.Shared.Models.Enums using directive'
    }
)

$additionalUsings = @(
    # 为User模块文件添加Entities.Users命名空间
    @{
        'pattern' = '^(\s*using [^;]+;\s*)*(?!.*LYBT\.Entities\.Users.*)(using LYBT\.Shared\.Models\.Common;)'
        'replacement' = '$1using LYBT.Entities.Users;' + "`r`n" + '$3'
        'description' = 'Add missing LYBT.Entities.Users using directive'
    }
    
    # 为UserValidationHelper添加模块内部引用
    @{
        'pattern' = '^(\s*using [^;]+;\s*)*(?!.*LYBT\.Module\.Users\.Helpers.*)(using LYBT\.Shared\.Models\.Common;)'
        'replacement' = '$1using LYBT.Module.Users.Helpers;' + "`r`n" + '$3'
        'description' = 'Add missing LYBT.Module.Users.Helpers using directive'
    }
)

$fixedCount = 0

# 获取User模块中有类型错误的文件
$targetFiles = @(
    Get-ChildItem -Path "src\Server\Modules\LYBT.Module.Users" -Filter "*.cs" -Recurse
) | Where-Object {
    $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
    $content -and ($content -match "UserModel|ActionType.*Create|ActionType.*Update|ActionType.*Delete")
}

Write-Host "Processing $($targetFiles.Count) files with type reference errors..." -ForegroundColor Green

foreach ($file in $targetFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileModified = $false
    
    # 应用主要修复
    foreach ($fix in $fixes) {
        $oldContent = $content
        $content = $content -replace $fix['pattern'], $fix['replacement']
        if ($content -ne $oldContent) {
            Write-Host "  Applied: $($fix['description']) in $($file.Name)" -ForegroundColor Gray
            $fileModified = $true
        }
    }
    
    # 添加缺失的using指令
    if ($file.Name -match "UserAccountService|UserCrudService|UserBatchService|UserBusinessHelper") {
        foreach ($usingFix in $additionalUsings) {
            $oldContent = $content
            $content = $content -replace $usingFix['pattern'], $usingFix['replacement']
            if ($content -ne $oldContent) {
                Write-Host "  Added: $($usingFix['description']) in $($file.Name)" -ForegroundColor Gray
                $fileModified = $true
            }
        }
    }
    
    # 保存更改
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Fixed: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
}

Write-Host "`n=== UltraThink User Type Reference Fix Summary ===" -ForegroundColor Cyan
Write-Host "Fixed files: $fixedCount" -ForegroundColor Green
Write-Host "Next: Final compilation test" -ForegroundColor Yellow