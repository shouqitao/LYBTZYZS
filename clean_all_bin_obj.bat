@echo off
echo 清理所有 bin 和 obj 文件夹...
echo.

REM 清理前端项目
echo 清理前端项目...
for /d /r "src\Frontend" %%d in (bin obj) do (
    if exist "%%d" (
        echo 删除: %%d
        rd /s /q "%%d"
    )
)

REM 清理后端项目
echo.
echo 清理后端项目...
for /d /r "src\Backend" %%d in (bin obj) do (
    if exist "%%d" (
        echo 删除: %%d
        rd /s /q "%%d"
    )
)

REM 清理共享项目
echo.
echo 清理共享项目...
for /d /r "src\Shared" %%d in (bin obj) do (
    if exist "%%d" (
        echo 删除: %%d
        rd /s /q "%%d"
    )
)

REM 清理测试项目
echo.
echo 清理测试项目...
for /d /r "tests" %%d in (bin obj) do (
    if exist "%%d" (
        echo 删除: %%d
        rd /s /q "%%d"
    )
)

echo.
echo 清理完成！
pause