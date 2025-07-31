@echo off
:: 设置代码页为UTF-8以支持中文显示
chcp 65001 >nul 2>&1
:: 启用延迟变量扩展
setlocal enabledelayedexpansion

echo ====================================
echo     LYBT 自动化部署系统测试
echo ====================================
echo.

echo 🧪 测试中文字符显示...
echo ✅ 成功：部署完成
echo ❌ 错误：连接失败  
echo ⚠️  警告：服务未启动
echo 🌐 网址：http://192.168.190.243:5297
echo 📁 路径：C:\LYBT\WebAPI
echo 🕒 时间：!date! !time!
echo.

echo 🔧 测试变量扩展...
set "TEST_VAR=测试变量值"
echo 变量值：!TEST_VAR!
echo.

echo 📋 测试文件操作...
echo 测试内容 > temp_test.txt
if exist temp_test.txt (
    echo ✅ 文件创建成功
    del temp_test.txt
    echo ✅ 文件删除成功
) else (
    echo ❌ 文件操作失败
)
echo.

echo 🔍 测试PowerShell调用...
powershell -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; Write-Host '✅ PowerShell中文显示正常' -ForegroundColor Green}"
echo.

echo ✅ 所有测试完成！
echo 👍 脚本编码配置正确，可以正常显示中文
echo.
pause