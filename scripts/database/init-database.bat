@echo off
echo ====================================
echo 初始化 LYBT 中医诊所管理系统数据库
echo ====================================
echo.

echo 正在检查 EF Core 工具...
dotnet tool list -g | find "dotnet-ef" >nul
if errorlevel 1 (
    echo 安装 EF Core 工具...
    dotnet tool install --global dotnet-ef
)

echo.
echo 开始为各模块创建数据库迁移和更新数据库...
echo.

rem 定义所有需要初始化的模块
set MODULES=Users Patients Doctors Auth Herbs FormulaTemplates Prescriptions Records DiagnosisTreatment Billing Pharmacy Registration Queueing TreatmentRoom Sync Diagnostics

for %%M in (%MODULES%) do (
    echo.
    echo =====================================
    echo 处理模块: LYBT.Module.%%M
    echo =====================================
    
    echo 检查是否存在迁移文件...
    if not exist "LYBT.Module.%%M\Migrations" (
        echo 为 LYBT.Module.%%M 添加初始迁移...
        dotnet ef migrations add InitialCreate --project LYBT.Module.%%M --startup-project LYBT.WebAPI --no-build
        if errorlevel 1 (
            echo 错误: 为 LYBT.Module.%%M 添加迁移失败
            pause
            exit /b 1
        )
    ) else (
        echo LYBT.Module.%%M 迁移文件已存在，跳过添加迁移
    )
    
    echo 更新 LYBT.Module.%%M 数据库...
    dotnet ef database update --project LYBT.Module.%%M --startup-project LYBT.WebAPI --no-build
    if errorlevel 1 (
        echo 错误: 更新 LYBT.Module.%%M 数据库失败
        pause
        exit /b 1
    )
    
    echo LYBT.Module.%%M 数据库更新完成
)

echo.
echo ====================================
echo 数据库初始化完成！
echo ====================================
echo.

rem 检查基础设施数据库
if exist "LYBT.Infrastructure\Migrations" (
    echo 更新基础设施数据库...
    dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI --no-build
)

echo.
echo 所有数据库初始化完成！
echo 你现在可以运行应用程序了。
echo.
pause