# 建议命令列表

## 开发环境管理

### 快速启动
```bash
# 交互式开发管理器（推荐）
scripts\dev-manager.bat

# 快速启动开发服务器
scripts\start-dev.bat

# 手动启动API服务
dotnet run --project src/Backend/Services/LYBT.WebAPI
```

### 编译构建
```bash
# 主编译管理器
scripts\build.bat

# 快速编译检查
scripts\build-check.bat

# 编译解决方案
dotnet build LYBT.Backend.sln    # 后端
dotnet build LYBT.Desktop.sln    # 前端
dotnet build LYBT.All.sln        # 完整方案

# 清理和重建
dotnet clean
dotnet build --no-incremental
```

### 数据库管理
```bash
# 交互式数据库管理器
scripts\database-manager.bat

# 添加迁移 - 必须使用 Infrastructure 项目
dotnet ef migrations add [迁移名称] --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 查看迁移历史
dotnet ef migrations list --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

### 测试运行
```bash
# 运行所有测试
dotnet test

# 运行特定项目测试
dotnet test tests/Backend/LYBT.Module.Users.Tests

# 带覆盖率的测试
scripts\test-with-coverage.bat

# API自动化测试
cd tests/api
python api_test_automation.py

# 集成测试
python scripts/integration-test.py
```

### 发布部署
```bash
# 发布生产版本
scripts\publish-production.bat

# 发布到特定文件夹
dotnet publish -c Release -o ./publish
```

## Windows系统命令

### 文件操作
```bash
# 列出文件
dir /b              # 简单列表
dir /s              # 递归列表

# 查找文件
where /r . *.cs     # 查找所有CS文件
findstr /s /i "pattern" *.cs  # 在CS文件中搜索

# 复制文件
copy source dest
xcopy source dest /s /e
robocopy source dest /e
```

### Git操作
```bash
# 基本操作
git status
git add .
git commit -m "message"
git push

# 分支管理
git branch
git checkout -b feature/xxx
git merge main

# 查看历史
git log --oneline
git diff
```

### 进程管理
```bash
# 查看进程
tasklist | findstr dotnet

# 结束进程
taskkill /f /im dotnet.exe
taskkill /PID [进程ID] /F
```

## 开发工具

### Visual Studio
- F5: 调试运行
- Ctrl+F5: 运行（不调试）
- F6: 生成解决方案
- Ctrl+Shift+B: 生成项目

### .NET CLI
```bash
# 查看SDK版本
dotnet --version
dotnet --list-sdks

# 查看包引用
dotnet list package
dotnet list package --outdated

# 添加包引用
dotnet add package [包名]
dotnet remove package [包名]
```

## 默认凭据
- API访问: https://localhost:7001
- Swagger文档: https://localhost:7001/swagger
- 用户名: sysadmin
- 密码: Admin@123456