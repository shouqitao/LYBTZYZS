@echo off
REM ========================================
REM UltraThink 简单修复脚本
REM ========================================

cd /d "%~dp0\.."

echo ========================================
echo    修复 Infrastructure 编译错误
echo ========================================
echo.

echo [1/3] 添加缺失的 NuGet 包...
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Logging.Abstractions --version 8.0.2
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.2
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Configuration.Abstractions --version 8.0.0
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Options --version 8.0.2
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Caching.Abstractions --version 8.0.0
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Caching.Memory --version 8.0.1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Http --version 8.0.1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 8.0.11
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package System.Diagnostics.DiagnosticSource --version 8.0.1
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.AspNetCore.Http.Abstractions --version 2.2.0
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.AspNetCore.Mvc.Core --version 2.2.5

echo.
echo [2/3] 清理编译缓存...
rd /s /q src\Backend\Core\LYBT.Infrastructure\bin 2>nul
rd /s /q src\Backend\Core\LYBT.Infrastructure\obj 2>nul

echo.
echo [3/3] 尝试编译...
dotnet build src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj --configuration Debug

if errorlevel 1 (
    echo.
    echo [警告] 仍有编译错误
    echo.
    echo 建议手动修复以下文件中的错误：
    echo   - SlowQueryAnalyzer.cs
    echo   - DatabaseStatisticsCollector.cs
    echo   - UnifiedDatabaseOptimizerRefactored.cs
    echo.
    echo 主要问题：
    echo   1. 只读属性不能赋值
    echo   2. 缺失的属性定义
    echo   3. 类型转换错误
) else (
    echo.
    echo [成功] Infrastructure 编译成功！
)

pause