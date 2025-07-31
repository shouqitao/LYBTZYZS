@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所管理系统 - 数据库管理工具

echo.
echo ====================================================
echo    凌隐宝堂中医诊所管理系统 - 数据库管理工具
echo ====================================================
echo.

set "PROJECT_ROOT=%~dp0.."
set "WEBAPI_DIR=%PROJECT_ROOT%\src\Backend\Services\LYBT.WebAPI"
set "INFRASTRUCTURE_DIR=%PROJECT_ROOT%\src\Backend\Core\LYBT.Infrastructure"

:MENU
echo 请选择操作:
echo.
echo 1. 查看数据库状态
echo 2. 应用待处理的迁移
echo 3. 创建新的迁移
echo 4. 回滚到上一个迁移
echo 5. 完全重建数据库 (⚠️  会删除所有数据)
echo 6. 备份数据库
echo 7. 生成数据库脚本
echo 8. 退出
echo.

set /p "CHOICE=请输入选项 (1-8): "

if "%CHOICE%"=="1" goto CHECK_DB
if "%CHOICE%"=="2" goto UPDATE_DB
if "%CHOICE%"=="3" goto ADD_MIGRATION
if "%CHOICE%"=="4" goto ROLLBACK_MIGRATION
if "%CHOICE%"=="5" goto REBUILD_DB
if "%CHOICE%"=="6" goto BACKUP_DB
if "%CHOICE%"=="7" goto SCRIPT_DB
if "%CHOICE%"=="8" goto EXIT

echo 无效选项，请重新选择
goto MENU

:CHECK_DB
echo.
echo 🔍 检查数据库状态...
cd /d "%WEBAPI_DIR%"
dotnet ef database list --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%"
echo.
echo 📊 迁移历史:
dotnet ef migrations list --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%"
echo.
pause
goto MENU

:UPDATE_DB
echo.
echo 🔄 应用待处理的迁移...
cd /d "%WEBAPI_DIR%"
dotnet ef database update --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%"
if %ERRORLEVEL% equ 0 (
    echo ✅ 迁移应用成功！
) else (
    echo ❌ 迁移应用失败！
)
echo.
pause
goto MENU

:ADD_MIGRATION
echo.
set /p "MIGRATION_NAME=请输入迁移名称: "
if "%MIGRATION_NAME%"=="" (
    echo 迁移名称不能为空
    goto ADD_MIGRATION
)
echo.
echo 📝 创建迁移: %MIGRATION_NAME%
cd /d "%WEBAPI_DIR%"
dotnet ef migrations add "%MIGRATION_NAME%" --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%"
if %ERRORLEVEL% equ 0 (
    echo ✅ 迁移创建成功！
    echo 💡 使用选项2应用此迁移到数据库
) else (
    echo ❌ 迁移创建失败！
)
echo.
pause
goto MENU

:ROLLBACK_MIGRATION
echo.
echo ⚠️  警告: 回滚迁移可能会导致数据丢失！
set /p "CONFIRM=确定要回滚吗? (Y/N): "
if /i not "%CONFIRM%"=="Y" goto MENU

echo.
echo 📋 当前迁移列表:
cd /d "%WEBAPI_DIR%"
dotnet ef migrations list --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%"
echo.
set /p "TARGET_MIGRATION=请输入要回滚到的迁移名称 (留空回滚到上一个): "

if "%TARGET_MIGRATION%"=="" (
    dotnet ef database update --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%"
) else (
    dotnet ef database update "%TARGET_MIGRATION%" --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%"
)

if %ERRORLEVEL% equ 0 (
    echo ✅ 数据库回滚成功！
) else (
    echo ❌ 数据库回滚失败！
)
echo.
pause
goto MENU

:REBUILD_DB
echo.
echo ⚠️⚠️⚠️  警告 ⚠️⚠️⚠️
echo 此操作将:
echo 1. 删除整个数据库
echo 2. 删除所有迁移文件
echo 3. 重新创建初始迁移
echo 4. 重建数据库
echo.
echo 所有数据将永久丢失！
echo.
set /p "CONFIRM1=确定要继续吗? (输入 YES 确认): "
if not "%CONFIRM1%"=="YES" goto MENU

set /p "CONFIRM2=最后确认: 真的要删除所有数据吗? (输入 DELETE 确认): "
if not "%CONFIRM2%"=="DELETE" goto MENU

echo.
echo 🗑️  步骤1: 删除数据库...
cd /d "%WEBAPI_DIR%"
dotnet ef database drop --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%" --force

echo 📁 步骤2: 删除迁移文件...
if exist "%INFRASTRUCTURE_DIR%\Migrations" (
    rmdir /s /q "%INFRASTRUCTURE_DIR%\Migrations"
)

echo 📝 步骤3: 创建初始迁移...
dotnet ef migrations add InitialCreate --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%"

echo 🔄 步骤4: 创建数据库...
dotnet ef database update --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%"

if %ERRORLEVEL% equ 0 (
    echo ✅ 数据库重建完成！
) else (
    echo ❌ 数据库重建失败！
)
echo.
pause
goto MENU

:BACKUP_DB
echo.
echo 💾 数据库备份功能...
echo 💡 提示: 请使用SQL Server Management Studio或sqlcmd进行数据库备份
echo.
echo 示例备份命令:
echo sqlcmd -S localhost -Q "BACKUP DATABASE [LYBTDB] TO DISK = 'C:\Backup\LYBTDB_%%date:~0,4%%%%date:~5,2%%%%date:~8,2%%.bak'"
echo.
pause
goto MENU

:SCRIPT_DB
echo.
echo 📜 生成数据库脚本...
set /p "FROM_MIGRATION=起始迁移 (留空表示从头开始): "
set /p "TO_MIGRATION=结束迁移 (留空表示到最新): "

cd /d "%WEBAPI_DIR%"
if "%FROM_MIGRATION%"=="" if "%TO_MIGRATION%"=="" (
    dotnet ef migrations script --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%" --output "database-script.sql"
) else (
    dotnet ef migrations script "%FROM_MIGRATION%" "%TO_MIGRATION%" --project "%INFRASTRUCTURE_DIR%" --startup-project "%WEBAPI_DIR%" --output "database-script.sql"
)

if %ERRORLEVEL% equ 0 (
    echo ✅ 脚本生成成功: database-script.sql
) else (
    echo ❌ 脚本生成失败！
)
echo.
pause
goto MENU

:EXIT
echo.
echo 👋 再见！
pause
exit /b 0