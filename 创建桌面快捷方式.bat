@echo off
chcp 65001 >nul
echo 正在创建桌面快捷方式...

set DESKTOP=%USERPROFILE%\Desktop
set TARGET="%~dp0启动凌隐宝堂系统.bat"
set SHORTCUT="%DESKTOP%\凌隐宝堂管理系统.lnk"
set ICONPATH="%~dp0src\Frontend\Desktop\Shell\bin\Debug\net8.0-windows\LYBT.WPF.Client.Shell.exe"

powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%SHORTCUT%'); $s.TargetPath = '%TARGET%'; $s.WorkingDirectory = '%~dp0'; $s.IconLocation = '%ICONPATH%'; $s.Description = '凌隐宝堂中医诊所管理系统'; $s.Save()"

echo ✓ 桌面快捷方式创建成功！
echo.
echo 您现在可以从桌面直接启动"凌隐宝堂管理系统"
pause