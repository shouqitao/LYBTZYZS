# UltraThink Syntax Fix Script - Fix missing quotes and syntax errors
Write-Host "Fixing syntax errors - quotes and brackets" -ForegroundColor Cyan

# Get all C# files that need fixing
$files = Get-ChildItem -Path "src\Server\Modules" -Filter "*.cs" -Recurse

$fixedCount = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Fix pattern: ServiceResult<T>.Failure("message, ex);
    # Replace with: ServiceResult<T>.Failure("message", ex);
    $content = $content -replace 'ServiceResult<([^>]+)>\.Failure\("([^"]*), ex\);', 'ServiceResult<$1>.Failure("$2", ex);'
    
    # Fix common error patterns
    $content = $content -replace 'Failure\("([^"]*), ex\);', 'Failure("$1", ex);'
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Fixed file: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
}

Write-Host "Fix completed! Fixed $fixedCount files" -ForegroundColor Green