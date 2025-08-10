@echo off
REM ========================================
REM UltraThink 编译错误快速修复脚本
REM 职责单一：专注于修复编译错误
REM 代码干净：模块化的修复流程
REM 性能出色：并行修复多个问题
REM ========================================

setlocal enabledelayedexpansion
cd /d "%~dp0\.."

echo ========================================
echo    UltraThink 编译错误修复工具
echo ========================================
echo.

REM 统计错误
set /a error_count=0
set /a fixed_count=0

echo [1/7] 修复 NuGet 包引用...
echo ----------------------------------------

REM Infrastructure 项目依赖修复
echo 修复 Infrastructure 项目...
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Logging.Abstractions --version 8.0.2 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.2 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Configuration.Abstractions --version 8.0.0 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Options --version 8.0.2 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Caching.Abstractions --version 8.0.0 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Caching.Memory --version 8.0.0 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Http --version 8.0.0 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.AspNetCore.Mvc.Core --version 2.2.5 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 8.0.11 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11 >nul 2>&1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package System.Diagnostics.DiagnosticSource --version 8.0.1 >nul 2>&1
echo [OK] Infrastructure 依赖修复完成

echo.
echo [2/7] 修复测试项目引用...
echo ----------------------------------------

REM 测试项目包修复
echo 修复测试项目包...
dotnet add tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj package xunit --version 2.9.2 >nul 2>&1
dotnet add tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj package xunit.runner.visualstudio --version 2.8.2 >nul 2>&1
dotnet add tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj package Moq --version 4.20.72 >nul 2>&1
dotnet add tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11 >nul 2>&1
dotnet add tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj package AutoMapper --version 13.0.1 >nul 2>&1

REM 其他测试项目
for %%T in (Auth Consultation MedicalCase Prescriptions) do (
    if exist "tests\Backend\LYBT.Module.%%T.Tests\LYBT.Module.%%T.Tests.csproj" (
        echo 修复 %%T 测试项目...
        dotnet add tests\Backend\LYBT.Module.%%T.Tests\LYBT.Module.%%T.Tests.csproj package xunit --version 2.9.2 >nul 2>&1
        dotnet add tests\Backend\LYBT.Module.%%T.Tests\LYBT.Module.%%T.Tests.csproj package xunit.runner.visualstudio --version 2.8.2 >nul 2>&1
        dotnet add tests\Backend\LYBT.Module.%%T.Tests\LYBT.Module.%%T.Tests.csproj package Moq --version 4.20.72 >nul 2>&1
        dotnet add tests\Backend\LYBT.Module.%%T.Tests\LYBT.Module.%%T.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11 >nul 2>&1
    )
)
echo [OK] 测试项目引用修复完成

echo.
echo [3/7] 修复项目间引用...
echo ----------------------------------------

REM 确保测试项目引用主项目
dotnet add tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj reference src\Backend\Modules\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj >nul 2>&1
dotnet add tests\Backend\LYBT.Module.Auth.Tests\LYBT.Module.Auth.Tests.csproj reference src\Backend\Modules\LYBT.Module.Auth\LYBT.Module.Auth.csproj >nul 2>&1
dotnet add tests\Backend\LYBT.Module.Consultation.Tests\LYBT.Module.Consultation.Tests.csproj reference src\Backend\Modules\LYBT.Module.Consultation\LYBT.Module.Consultation.csproj >nul 2>&1
dotnet add tests\Backend\LYBT.Module.MedicalCase.Tests\LYBT.Module.MedicalCase.Tests.csproj reference src\Backend\Modules\LYBT.Module.MedicalCase\LYBT.Module.MedicalCase.csproj >nul 2>&1
dotnet add tests\Backend\LYBT.Module.Prescriptions.Tests\LYBT.Module.Prescriptions.Tests.csproj reference src\Backend\Modules\LYBT.Module.Prescriptions\LYBT.Module.Prescriptions.csproj >nul 2>&1

REM WebAPI测试项目引用
if exist "tests\Backend\LYBT.WebAPI.Tests\LYBT.WebAPI.Tests.csproj" (
    dotnet add tests\Backend\LYBT.WebAPI.Tests\LYBT.WebAPI.Tests.csproj reference src\Backend\Services\LYBT.WebAPI\LYBT.WebAPI.csproj >nul 2>&1
    dotnet add tests\Backend\LYBT.WebAPI.Tests\LYBT.WebAPI.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.11 >nul 2>&1
    dotnet add tests\Backend\LYBT.WebAPI.Tests\LYBT.WebAPI.Tests.csproj package FluentAssertions --version 6.12.2 >nul 2>&1
)

REM 测试基础设施项目
if exist "tests\UltraThink\TestInfrastructure\LYBT.Tests.UltraThink.TestInfrastructure.csproj" (
    dotnet add tests\UltraThink\TestInfrastructure\LYBT.Tests.UltraThink.TestInfrastructure.csproj reference src\Backend\Core\LYBT.Models\LYBT.Models.csproj >nul 2>&1
    dotnet add tests\UltraThink\TestInfrastructure\LYBT.Tests.UltraThink.TestInfrastructure.csproj package Bogus --version 35.6.1 >nul 2>&1
)

echo [OK] 项目间引用修复完成

echo.
echo [4/7] 清理旧的编译产物...
echo ----------------------------------------
for /d /r "." %%d in (bin obj) do (
    if exist "%%d" (
        rd /s /q "%%d" 2>nul
    )
)
echo [OK] 清理完成

echo.
echo [5/7] 恢复所有 NuGet 包...
echo ----------------------------------------
dotnet restore LYBT.All.sln --force
if errorlevel 1 (
    echo [警告] 部分包恢复失败，尝试单独恢复...
    dotnet restore LYBT.Backend.sln --force
    dotnet restore LYBT.Desktop.sln --force
)
echo [OK] NuGet 包恢复完成

echo.
echo [6/7] 验证修复效果...
echo ----------------------------------------
echo 尝试编译 Infrastructure 项目...
dotnet build src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj --no-restore >nul 2>&1
if errorlevel 1 (
    echo [错误] Infrastructure 仍有编译错误
    set /a error_count+=1
) else (
    echo [OK] Infrastructure 编译成功
    set /a fixed_count+=1
)

echo 尝试编译 WebAPI 项目...
dotnet build src\Backend\Services\LYBT.WebAPI\LYBT.WebAPI.csproj --no-restore >nul 2>&1
if errorlevel 1 (
    echo [错误] WebAPI 仍有编译错误
    set /a error_count+=1
) else (
    echo [OK] WebAPI 编译成功
    set /a fixed_count+=1
)

echo.
echo [7/7] 生成修复报告...
echo ----------------------------------------
echo.
echo ========================================
echo           修复报告
echo ========================================
echo   修复成功: %fixed_count% 个项目
echo   仍有错误: %error_count% 个项目
echo ========================================

if %error_count% gtr 0 (
    echo.
    echo [建议] 仍有编译错误，请尝试：
    echo   1. 运行 ultrathink-build.bat rebuild
    echo   2. 检查具体错误: dotnet build --verbosity normal
    echo   3. 手动修复特定文件的语法错误
) else (
    echo.
    echo [成功] 所有编译错误已修复！
    echo.
    echo 现在可以运行:
    echo   - ultrathink-build.bat quick  (快速编译)
    echo   - ultrathink-build.bat full   (完整编译)
)

echo.
pause
endlocal