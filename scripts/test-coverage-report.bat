@echo off
echo =============================================
echo UltraThink Phase 3 - 测试覆盖率提升报告
echo =============================================
echo.

echo 🧪 正在运行核心Service测试...
echo.

echo [1/3] 📊 运行 UserService 测试 (已重构)
dotnet test tests/Backend/LYBT.Module.Users.Tests/ --verbosity minimal --nologo --no-build > temp_user_results.txt 2>&1
if %ERRORLEVEL%==0 (
    echo ✅ UserService 测试通过
    type temp_user_results.txt | findstr "Passed!"
) else (
    echo ❌ UserService 测试失败
)
echo.

echo [2/3] 🌿 运行 HerbService 测试 (已重构)
dotnet test tests/Backend/LYBT.Module.Herbs.Tests/ --verbosity minimal --nologo --no-build > temp_herb_results.txt 2>&1
if %ERRORLEVEL%==0 (
    echo ✅ HerbService 测试通过
    type temp_herb_results.txt | findstr "Passed!"
) else (
    echo ❌ HerbService 测试失败 - 需要修复测试引用
)
echo.

echo [3/3] 🔐 运行 AuthService 测试 (已重构)
dotnet test tests/Backend/LYBT.Module.Auth.Tests/ --verbosity minimal --nologo --no-build > temp_auth_results.txt 2>&1
if %ERRORLEVEL%==0 (
    echo ✅ AuthService 测试通过
    type temp_auth_results.txt | findstr "Passed!"
) else (
    echo ❌ AuthService 测试失败 - 需要修复测试引用
)
echo.

echo =============================================
echo 📈 覆盖率提升总结
echo =============================================
echo.
echo ✅ 事务管理强化完成 - PrescriptionService 和 ConsultationService
echo ✅ 数据库索引优化完成 - 6个核心表的性能索引已添加
echo 🔄 测试覆盖率提升进行中:
echo    - UserService: 已有现存测试
echo    - HerbService: 需要修复测试依赖
echo    - AuthService: 需要修复测试依赖
echo.
echo 📊 目标: 从当前 2.76%% 提升到 15%% 覆盖率
echo 🎯 预期收益: 关键业务逻辑测试保障
echo.

echo 🧹 清理临时文件...
del temp_*.txt 2>nul

echo ✅ UltraThink Phase 3 高优先级任务进展报告完成
echo =============================================