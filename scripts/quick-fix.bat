@echo off
REM ======================================
REM 快速修复常见编译错误
REM 自动修复已知的常见问题
REM ======================================
setlocal enabledelayedexpansion

echo ========================================
echo 凌隐宝堂系统 - 自动修复工具
echo ========================================
echo.

echo 请选择修复类型：
echo 1. 修复属性名不匹配 (CurrentPage/PageIndex等)
echo 2. 修复中文编码问题
echo 3. 修复命名空间引用
echo 4. 修复所有已知问题
echo 5. 清理和重建
echo.
set /p fix_choice="请输入选择 (1-5): "

if "!fix_choice!"=="1" goto :fix_properties
if "!fix_choice!"=="2" goto :fix_encoding
if "!fix_choice!"=="3" goto :fix_namespaces
if "!fix_choice!"=="4" goto :fix_all
if "!fix_choice!"=="5" goto :clean_rebuild
goto :end

:fix_properties
echo.
echo 正在修复属性名不匹配问题...
echo ======================================

REM 修复 CurrentPage -> PageIndex
powershell -Command "(Get-Content -Path 'src\Frontend\Desktop\Services\*.cs' -Raw) -replace 'CurrentPage\s*=\s*request\.CurrentPage', 'PageIndex = request.CurrentPage' | Set-Content -Path $_.FullName" 2>nul

REM 修复 SearchKeyword -> Keyword  
powershell -Command "(Get-Content -Path 'src\Frontend\Desktop\Services\*.cs' -Raw) -replace 'SearchKeyword\s*=\s*request\.SearchKeyword', 'Keyword = request.SearchKeyword' | Set-Content -Path $_.FullName" 2>nul

REM 修复 SortAscending -> IsAscending
powershell -Command "(Get-Content -Path 'src\Frontend\Desktop\Services\*.cs' -Raw) -replace 'SortAscending\s*=\s*request\.SortAscending', 'IsAscending = request.SortAscending' | Set-Content -Path $_.FullName" 2>nul

echo ✅ 属性名修复完成
goto :check_result

:fix_encoding
echo.
echo 正在修复中文编码问题...
echo ======================================

REM 创建Python脚本修复编码
echo import os > fix_encoding.py
echo import chardet >> fix_encoding.py
echo import codecs >> fix_encoding.py
echo. >> fix_encoding.py
echo def fix_file_encoding(filepath): >> fix_encoding.py
echo     try: >> fix_encoding.py
echo         with open(filepath, 'rb') as f: >> fix_encoding.py
echo             raw_data = f.read() >> fix_encoding.py
echo             result = chardet.detect(raw_data) >> fix_encoding.py
echo             encoding = result['encoding'] >> fix_encoding.py
echo         if encoding and encoding.lower() != 'utf-8': >> fix_encoding.py
echo             with codecs.open(filepath, 'r', encoding=encoding) as f: >> fix_encoding.py
echo                 content = f.read() >> fix_encoding.py
echo             with codecs.open(filepath, 'w', encoding='utf-8-sig') as f: >> fix_encoding.py
echo                 f.write(content) >> fix_encoding.py
echo             print(f'Fixed: {filepath}') >> fix_encoding.py
echo     except Exception as e: >> fix_encoding.py
echo         print(f'Error fixing {filepath}: {e}') >> fix_encoding.py
echo. >> fix_encoding.py
echo for root, dirs, files in os.walk('src'): >> fix_encoding.py
echo     for file in files: >> fix_encoding.py
echo         if file.endswith(('.cs', '.xaml')): >> fix_encoding.py
echo             filepath = os.path.join(root, file) >> fix_encoding.py
echo             fix_file_encoding(filepath) >> fix_encoding.py

python fix_encoding.py 2>nul
del fix_encoding.py 2>nul

echo ✅ 编码修复完成
goto :check_result

:fix_namespaces
echo.
echo 正在修复命名空间引用...
echo ======================================

REM 添加常见缺失的using语句
powershell -Command "Get-ChildItem -Path 'src\Frontend\Desktop' -Filter '*.cs' -Recurse | ForEach-Object { $content = Get-Content $_.FullName -Raw; if ($content -match 'MedicalCaseStatus' -and $content -notmatch 'using LYBT.Shared.Models.Enums;') { $content = 'using LYBT.Shared.Models.Enums;`n' + $content; Set-Content -Path $_.FullName -Value $content } }" 2>nul

echo ✅ 命名空间修复完成
goto :check_result

:fix_all
echo.
echo 正在执行所有修复...
echo ======================================
call :fix_properties
call :fix_encoding  
call :fix_namespaces
echo.
echo ✅ 所有修复已完成
goto :check_result

:clean_rebuild
echo.
echo 正在清理和重建...
echo ======================================

echo 清理bin和obj目录...
for /d /r "src" %%d in (bin obj) do (
    if exist "%%d" (
        echo 删除: %%d
        rd /s /q "%%d" 2>nul
    )
)

echo.
echo 清理NuGet缓存...
dotnet nuget locals all --clear

echo.
echo 还原NuGet包...
dotnet restore LYBT.Backend.sln
dotnet restore LYBT.Desktop.sln

echo.
echo 重新编译...
dotnet build LYBT.Backend.sln --no-incremental
dotnet build LYBT.Desktop.sln --no-incremental

echo ✅ 清理和重建完成
goto :end

:check_result
echo.
echo 正在验证修复结果...
call scripts\build-check.bat 4

:end
echo.
echo 修复脚本执行完成
pause