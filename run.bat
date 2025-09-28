@echo off
REM 快速执行脚本入口
REM 用法: run [command] [args...]
REM 示例: run test
REM       run build
REM       run clean

setlocal enabledelayedexpansion

if "%1"=="" (
    echo ========================================
    echo  LYBT 快速执行脚本
    echo ========================================
    echo.
    echo 可用命令:
    echo   run build        - 构建解决方案
    echo   run test         - 运行测试
    echo   run clean        - 清理项目
    echo   run webapi       - 启动WebAPI服务
    echo   run desktop      - 启动桌面应用
    echo   run db-init      - 初始化数据库
    echo   run db-backup    - 备份数据库
    echo   run help         - 显示帮助
    echo.
    goto :eof
)

set CMD=%1
shift

REM 构建相关
if /i "%CMD%"=="build" (
    call scripts\build.bat %1 %2 %3 %4 %5
    goto :eof
)

if /i "%CMD%"=="build-webapi" (
    call scripts\build-webapi.bat %1 %2 %3 %4 %5
    goto :eof
)

REM 测试相关
if /i "%CMD%"=="test" (
    call scripts\run-tests.bat %1 %2 %3 %4 %5
    goto :eof
)

if /i "%CMD%"=="test-clean" (
    powershell -ExecutionPolicy Bypass -File scripts\clean-test-results.ps1
    goto :eof
)

REM 清理相关
if /i "%CMD%"=="clean" (
    call scripts\clean-solution.bat
    goto :eof
)

if /i "%CMD%"=="clean-all" (
    powershell -ExecutionPolicy Bypass -File scripts\cleanup.ps1
    goto :eof
)

REM 运行应用
if /i "%CMD%"=="webapi" (
    powershell -ExecutionPolicy Bypass -File scripts\run-webapi.ps1 %1 %2 %3 %4 %5
    goto :eof
)

if /i "%CMD%"=="desktop" (
    call scripts\run-desktop.bat %1 %2 %3 %4 %5
    goto :eof
)

REM 数据库管理
if /i "%CMD%"=="db-init" (
    call scripts\initialize-db.bat %1 %2 %3 %4 %5
    goto :eof
)

if /i "%CMD%"=="db-backup" (
    call scripts\backup-database.bat %1 %2 %3 %4 %5
    goto :eof
)

if /i "%CMD%"=="db-restore" (
    call scripts\restore-database.bat %1 %2 %3 %4 %5
    goto :eof
)

REM 帮助
if /i "%CMD%"=="help" (
    echo ========================================
    echo  LYBT 脚本帮助文档
    echo ========================================
    echo.
    echo 构建命令:
    echo   run build [Debug^|Release]    - 构建整个解决方案
    echo   run build-webapi             - 仅构建WebAPI
    echo.
    echo 测试命令:
    echo   run test                     - 运行所有测试
    echo   run test-clean              - 清理测试结果
    echo.
    echo 清理命令:
    echo   run clean                    - 清理解决方案
    echo   run clean-all               - 深度清理（包括临时文件）
    echo.
    echo 应用运行:
    echo   run webapi [port]           - 启动WebAPI服务
    echo   run desktop                 - 启动桌面应用
    echo.
    echo 数据库管理:
    echo   run db-init                 - 初始化数据库
    echo   run db-backup              - 备份数据库
    echo   run db-restore [file]      - 恢复数据库
    echo.
    echo 更多脚本请查看 scripts\README.md
    goto :eof
)

echo 未知命令: %CMD%
echo 使用 'run help' 查看可用命令
exit /b 1