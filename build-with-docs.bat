@echo off
echo 开始构建项目并生成文档...
echo.

echo 构建模块项目...
echo.

REM 构建所有模块项目（这将触发文档复制）
dotnet build LYBT.Common --verbosity minimal
dotnet build LYBT.Models --verbosity minimal
dotnet build LYBT.Infrastructure --verbosity minimal
dotnet build LYBT.Module.Auth --verbosity minimal
dotnet build LYBT.Module.Billing --verbosity minimal
dotnet build LYBT.Module.DiagnosisTreatment --verbosity minimal
dotnet build LYBT.Module.Doctors --verbosity minimal
dotnet build LYBT.Module.FormulaTemplates --verbosity minimal
dotnet build LYBT.Module.Herbs --verbosity minimal
dotnet build LYBT.Module.Patients --verbosity minimal
dotnet build LYBT.Module.Pharmacy --verbosity minimal
dotnet build LYBT.Module.Prescriptions --verbosity minimal
dotnet build LYBT.Module.Queueing --verbosity minimal
dotnet build LYBT.Module.Records --verbosity minimal
dotnet build LYBT.Module.Registration --verbosity minimal
dotnet build LYBT.Module.Sync --verbosity minimal
dotnet build LYBT.Module.TreatmentRoom --verbosity minimal
dotnet build LYBT.Module.Users --verbosity minimal

echo.
echo 构建完成！文档已自动复制到 Documentation 文件夹。
echo.

REM 显示文档文件列表
echo 生成的文档文件：
dir Documentation\*.md /b

echo.
echo 使用方法：
echo 1. 运行此脚本构建所有模块并自动复制文档
echo 2. 文档文件会自动输出到 Documentation 文件夹
echo 3. 每个模块的文档都以"项目名_文档类型.md"格式命名
echo 4. 支持 FUNCTIONALITY.md 和 README.md 两种文档类型
echo.
pause