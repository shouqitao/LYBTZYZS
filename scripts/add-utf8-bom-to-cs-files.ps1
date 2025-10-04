# 添加 UTF-8 BOM 到指定的 C# 文件
# Issue #925: 修复 5 个 Desktop ViewModel 文件缺少 UTF-8 BOM

$files = @(
    "src/Client/Desktop/Shell/Dialogs/ViewModels/ConfirmationDialogViewModel.cs",
    "src/Client/Desktop/Shell/Dialogs/ViewModels/InformationDialogViewModel.cs",
    "src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginWindowViewModel.cs",
    "src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionViewModel.cs",
    "src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserDetailViewModel.cs"
)

$utf8Bom = New-Object System.Text.UTF8Encoding $true
$processedCount = 0

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "处理文件: $file" -ForegroundColor Cyan
        $content = Get-Content $file -Raw
        [System.IO.File]::WriteAllText($file, $content, $utf8Bom)
        $processedCount++
        Write-Host "  ✓ 已添加 UTF-8 BOM" -ForegroundColor Green
    } else {
        Write-Host "  ✗ 文件不存在: $file" -ForegroundColor Red
    }
}

Write-Host "`n完成! 已处理 $processedCount 个文件" -ForegroundColor Green
