@echo off
echo === 构建后端解决方案并检查警告 ===
cd /d D:\source\repos\LYBTZYZS

echo.
echo 开始构建...
dotnet build LYBT.Backend.sln --no-restore > build_output.txt 2>&1

echo.
echo === CS警告列表 ===
findstr /i "warning CS" build_output.txt

echo.
echo === 错误列表 ===
findstr /i "error CS" build_output.txt

echo.
echo === 构建完成 ===
pause