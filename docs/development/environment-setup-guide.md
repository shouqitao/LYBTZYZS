# 环境配置指南

> **版本**: 1.0
> **创建日期**: 2025-10-15
> **维护者**: 项目团队
> **相关文档**: [配置管理优化指南](configuration-optimization.md) | [快速开发指南](rapid-development-guide.md) | [部署文档](../deployment/)

## 📋 指南概述

本文档提供 LYBT 系统在不同环境下的详细配置指南，包括开发环境、测试环境和生产环境的配置步骤、自动化脚本和最佳实践。确保环境配置的一致性、可重复性和可维护性。

## 🎯 环境配置目标

### 核心目标
- **一致性**: 确保所有环境配置的一致性
- **可重复性**: 环境配置过程可重复执行
- **自动化**: 最大化自动化配置过程
- **安全性**: 保护敏感配置信息
- **可维护性**: 简化环境维护和更新

### 环境分类
- **开发环境**: 本地开发和调试
- **测试环境**: 集成测试和用户验收测试
- **预生产环境**: 生产前最终验证
- **生产环境**: 正式运行环境

## 🖥️ 开发环境配置

### 1. 系统要求

#### 硬件要求
- **CPU**: 4核心以上
- **内存**: 8GB 以上
- **存储**: 50GB 可用空间
- **网络**: 稳定的互联网连接

#### 软件要求
- **操作系统**: Windows 10/11 或 macOS 10.15+ 或 Ubuntu 20.04+
- **.NET SDK**: 8.0.x 或更高版本
- **数据库**: SQL Server 2019+ 或 SQL Server Express
- **Redis**: 6.0+ (可选，用于缓存)
- **Git**: 2.30+ 版本控制

### 2. 开发工具安装

#### 必需工具安装脚本
```powershell
# Windows PowerShell 安装脚本
# install-dev-tools.ps1

Write-Host "开始安装 LYBT 开发环境工具..." -ForegroundColor Green

# 检查管理员权限
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "请以管理员身份运行此脚本" -ForegroundColor Red
    exit 1
}

# 安装 .NET SDK
Write-Host "安装 .NET SDK 8.0..." -ForegroundColor Yellow
$dotnetInstallerUrl = "https://download.microsoft.com/download/8/0/9/80972686-6df9-4e1a-8b8d-ff9b5b5b5b5b/dotnet-sdk-8.0.100-win-x64.exe"
$dotnetInstallerPath = "$env:TEMP\dotnet-sdk-installer.exe"
Invoke-WebRequest -Uri $dotnetInstallerUrl -OutFile $dotnetInstallerPath
Start-Process -FilePath $dotnetInstallerPath -Args "/quiet" -Wait

# 安装 Visual Studio Code
Write-Host "安装 Visual Studio Code..." -ForegroundColor Yellow
$vscodeInstallerUrl = "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user"
$vscodeInstallerPath = "$env:TEMP\vscode-installer.exe"
Invoke-WebRequest -Uri $vscodeInstallerUrl -OutFile $vscodeInstallerPath
Start-Process -FilePath $vscodeInstallerPath -Args "/VERYSILENT /NORESTART" -Wait

# 安装 Git
Write-Host "安装 Git..." -ForegroundColor Yellow
$gitInstallerUrl = "https://github.com/git-for-windows/git/releases/download/v2.41.0.windows.3/Git-2.41.0.3-64-bit.exe"
$gitInstallerPath = "$env:TEMP\git-installer.exe"
Invoke-WebRequest -Uri $gitInstallerUrl -OutFile $gitInstallerPath
Start-Process -FilePath $gitInstallerPath -Args "/VERYSILENT /NORESTART" -Wait

# 安装 SQL Server Express
Write-Host "安装 SQL Server Express..." -ForegroundColor Yellow
$sqlInstallerUrl = "https://download.microsoft.com/download/7/c/1/7c14f92a-5aeb-4f0a-a302-1e1a5b5b5b5b/SQLEXPR_x64_ENU.exe"
$sqlInstallerPath = "$env:TEMP\sql-express-installer.exe"
Invoke-WebRequest -Uri $sqlInstallerUrl -OutFile $sqlInstallerPath
Start-Process -FilePath $sqlInstallerPath -Args "/QUIET /IACCEPTSQLSERVERLICENSETERMS /SAPWD=DevPassword123!" -Wait

# 安装 Redis (Windows)
Write-Host "安装 Redis..." -ForegroundColor Yellow
$redisUrl = "https://github.com/microsoftarchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.zip"
$redisPath = "C:\Redis"
New-Item -ItemType Directory -Path $redisPath -Force
Invoke-WebRequest -Uri $redisUrl -OutFile "$env:TEMP\redis.zip"
Expand-Archive -Path "$env:TEMP\redis.zip" -DestinationPath $redisPath

# 安装 .NET 工具
Write-Host "安装 .NET 全局工具..." -ForegroundColor Yellow
dotnet tool install --global dotnet-ef
dotnet tool install --global dotnet-aspnet-codegenerator

Write-Host "开发环境工具安装完成！" -ForegroundColor Green
Write-Host "请重启计算机以确保所有工具正常工作" -ForegroundColor Yellow
```

#### macOS 安装脚本
```bash
#!/bin/bash
# install-dev-tools-mac.sh

echo "开始安装 LYBT 开发环境工具..."

# 检查 Homebrew 是否安装
if ! command -v brew &> /dev/null; then
    echo "安装 Homebrew..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
fi

# 更新 Homebrew
brew update

# 安装 .NET SDK
echo "安装 .NET SDK..."
brew install --cask dotnet-sdk

# 安装 Visual Studio Code
echo "安装 Visual Studio Code..."
brew install --cask visual-studio-code

# 安装 Git
echo "安装 Git..."
brew install git

# 安装 SQL Server (使用 Docker)
echo "安装 SQL Server Docker 容器..."
docker pull mcr.microsoft.com/mssql/server:2019-latest
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=DevPassword123!" -p 1433:1433 --name sql-server -d mcr.microsoft.com/mssql/server:2019-latest

# 安装 Redis
echo "安装 Redis..."
brew install redis

# 启动 Redis 服务
brew services start redis

# 安装 .NET 工具
echo "安装 .NET 全局工具..."
dotnet tool install --global dotnet-ef
dotnet tool install --global dotnet-aspnet-codegenerator

echo "开发环境工具安装完成！"
```

### 3. 项目设置

#### 项目克隆和初始化
```powershell
# Windows PowerShell
# setup-project.ps1

# 设置项目路径
$projectPath = "D:\source\repos\LYBTZYZS"

# 检查项目目录是否存在
if (-not (Test-Path $projectPath)) {
    Write-Host "项目目录不存在，请先克隆项目" -ForegroundColor Red
    Write-Host "执行: git clone <repository-url> $projectPath" -ForegroundColor Yellow
    exit 1
}

# 进入项目目录
Set-Location $projectPath

# 恢复 NuGet 包
Write-Host "恢复 NuGet 包..." -ForegroundColor Yellow
dotnet restore LYBT.All.sln

# 构建项目
Write-Host "构建项目..." -ForegroundColor Yellow
dotnet build LYBT.All.sln -c Release

# 运行数据库迁移
Write-Host "运行数据库迁移..." -ForegroundColor Yellow
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/LYBT.Server.API

# 初始化开发环境配置
Write-Host "初始化开发环境配置..." -ForegroundColor Yellow
dotnet user-secrets init

# 设置开发环境密钥
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=LYBT_Dev;Trusted_Connection=true;TrustServerCertificate=true;"
dotnet user-secrets set "Authentication:Jwt:SecretKey" "dev-jwt-secret-key-256-bits-minimum-length-for-security"
dotnet user-secrets set "Authentication:Jwt:Issuer" "LYBT-Dev"
dotnet user-secrets set "Authentication:Jwt:Audience" "LYBT-Client-Dev"

Write-Host "开发环境设置完成！" -ForegroundColor Green
```

#### VS Code 工作区配置
```json
// .vscode/settings.json
{
    "files.encoding": "utf8bom",
    "files.insertFinalNewline": true,
    "files.trimFinalNewlines": true,
    "files.trimTrailingWhitespace": true,

    "csharp.format.enable": true,
    "csharp.format.newLine": "\n",
    "csharp.format.indent.mode": "spaces",
    "csharp.format.indent.size": 4,

    "editor.formatOnSave": true,
    "editor.formatOnType": true,
    "editor.insertSpaces": true,
    "editor.tabSize": 4,
    "editor.detectIndentation": false,

    "dotnet.defaultSolution": "LYBT.All.sln",
    "dotnet.preferCSharpExtension": true,
    "dotnet.testRunSettings": "tests/.runsettings",

    "git.autofetch": true,
    "git.enableSmartCommit": true,
    "git.postCommitCommand": "none",

    "extensions.ignoreRecommendations": false,
    "extensions.autoUpdate": false,

    "launch": {
        "version": "0.2.0",
        "configurations": [
            {
                "name": "Launch LYBT.Server.API",
                "type": "coreclr",
                "request": "launch",
                "program": "${workspaceFolder}/src/Server/LYBT.Server.API/bin/Debug/net8.0/LYBT.Server.API.dll",
                "args": [],
                "cwd": "${workspaceFolder}/src/Server/LYBT.Server.API",
                "console": "internalConsole",
                "stopAtEntry": false,
                "env": {
                    "ASPNETCORE_ENVIRONMENT": "Development"
                }
            },
            {
                "name": "Launch LYBT.Desktop",
                "type": "coreclr",
                "request": "launch",
                "program": "${workspaceFolder}/src/Client/Desktop/LYBT.Desktop/bin/Debug/net8.0-windows/LYBT.Desktop.exe",
                "args": [],
                "cwd": "${workspaceFolder}/src/Client/Desktop/LYBT.Desktop",
                "console": "internalConsole",
                "stopAtEntry": false
            }
        ]
    },

    "tasks": {
        "version": "2.0.0",
        "tasks": [
            {
                "label": "build",
                "command": "dotnet",
                "type": "process",
                "args": [
                    "build",
                    "${workspaceFolder}/LYBT.All.sln",
                    "/property:GenerateFullPaths=true",
                    "/consoleloggerparameters:NoSummary"
                ],
                "problemMatcher": "$msCompile"
            },
            {
                "label": "publish",
                "command": "dotnet",
                "type": "process",
                "args": [
                    "publish",
                    "${workspaceFolder}/src/Server/LYBT.Server.API/LYBT.Server.API.csproj",
                    "/property:GenerateFullPaths=true",
                    "/consoleloggerparameters:NoSummary"
                ],
                "problemMatcher": "$msCompile"
            },
            {
                "label": "watch",
                "command": "dotnet",
                "type": "process",
                "args": [
                    "watch",
                    "run",
                    "--project",
                    "${workspaceFolder}/src/Server/LYBT.Server.API/LYBT.Server.API.csproj"
                ],
                "problemMatcher": "$msCompile"
            }
        ]
    }
}
```

### 4. 开发环境启动脚本

#### 开发环境启动脚本
```powershell
# start-dev-environment.ps1
param(
    [switch]$WithRedis,
    [switch]$WithFrontend,
    [string]$Environment = "Development"
)

Write-Host "启动 LYBT 开发环境..." -ForegroundColor Green

# 设置环境变量
$env:ASPNETCORE_ENVIRONMENT = $Environment

# 启动 Redis (如果需要)
if ($WithRedis) {
    Write-Host "启动 Redis..." -ForegroundColor Yellow
    Start-Process -FilePath "C:\Redis\redis-server.exe" -WindowStyle Hidden
    Start-Sleep -Seconds 2
}

# 启动数据库 (如果需要)
$sqlService = Get-Service -Name "MSSQLSERVER" -ErrorAction SilentlyContinue
if ($sqlService -and $sqlService.Status -ne "Running") {
    Write-Host "启动 SQL Server..." -ForegroundColor Yellow
    Start-Service -Name "MSSQLSERVER"
}

# 构建项目
Write-Host "构建项目..." -ForegroundColor Yellow
dotnet build LYBT.All.sln -c Release

# 启动后端 API
Write-Host "启动后端 API..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/Server/LYBT.Server.API", "--urls", "http://localhost:5000" -PassThru

# 等待 API 启动
Start-Sleep -Seconds 5

# 启动前端 (如果需要)
if ($WithFrontend) {
    Write-Host "启动前端应用..." -ForegroundColor Yellow
    Set-Location "src/Client/Desktop"
    $frontendProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "LYBT.Desktop" -PassThru
    Set-Location "../../.."
}

# 等待用户输入停止
Write-Host "开发环境已启动！" -ForegroundColor Green
Write-Host "API 地址: http://localhost:5000" -ForegroundColor Cyan
if ($WithFrontend) {
    Write-Host "前端应用已启动" -ForegroundColor Cyan
}
Write-Host "按任意键停止开发环境..." -ForegroundColor Yellow

$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# 停止进程
if ($apiProcess) {
    $apiProcess.Kill()
}
if ($frontendProcess) {
    $frontendProcess.Kill()
}

Write-Host "开发环境已停止" -ForegroundColor Red
```

## 🧪 测试环境配置

### 1. Docker 测试环境

#### Docker Compose 配置
```yaml
# docker-compose.test.yml
version: '3.8'

services:
  # SQL Server 数据库
  sql-server:
    image: mcr.microsoft.com/mssql/server:2019-latest
    container_name: lybt-sql-test
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=TestPassword123!
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sql_test_data:/var/opt/mssql/data
      - ./scripts/test/create-test-database.sql:/docker-entrypoint-initdb.d/create-test-database.sql
    networks:
      - lybt-test-network
    healthcheck:
      test: ["CMD", "/opt/mssql-tools/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "TestPassword123!", "-Q", "SELECT 1"]
      interval: 30s
      timeout: 10s
      retries: 5

  # Redis 缓存
  redis:
    image: redis:7-alpine
    container_name: lybt-redis-test
    ports:
      - "6379:6379"
    volumes:
      - redis_test_data:/data
    networks:
      - lybt-test-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 10s
      retries: 5

  # 后端 API
  lybt-api:
    build:
      context: .
      dockerfile: src/Server/LYBT.Server.API/Dockerfile
      target: test
    container_name: lybt-api-test
    environment:
      - ASPNETCORE_ENVIRONMENT=Testing
      - ConnectionStrings__DefaultConnection=Server=sql-server,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true;
      - ConnectionStrings__Redis=redis:6379
      - Authentication__Jwt__SecretKey=test-jwt-secret-key-256-bits-minimum
      - Authentication__Jwt__Issuer=LYBT-Test
      - Authentication__Jwt__Audience=LYBT-Client-Test
    ports:
      - "5001:80"
    depends_on:
      sql-server:
        condition: service_healthy
      redis:
        condition: service_healthy
    networks:
      - lybt-test-network
    volumes:
      - ./test-results:/app/test-results
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 5

  # 测试运行器
  test-runner:
    build:
      context: .
      dockerfile: Dockerfile.test
    container_name: lybt-test-runner
    environment:
      - ASPNETCORE_ENVIRONMENT=Testing
      - ConnectionStrings__DefaultConnection=Server=sql-server,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true;
      - ConnectionStrings__Redis=redis:6379
      - Authentication__Jwt__SecretKey=test-jwt-secret-key-256-bits-minimum
    depends_on:
      lybt-api:
        condition: service_healthy
    networks:
      - lybt-test-network
    volumes:
      - ./test-results:/app/test-results
      - ./tests:/app/tests
    command: ["dotnet", "test", "--logger", "trx", "--results-directory", "/app/test-results"]

volumes:
  sql_test_data:
  redis_test_data:

networks:
  lybt-test-network:
    driver: bridge
```

#### 测试 Dockerfile
```dockerfile
# Dockerfile.test
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test
WORKDIR /app

# 复制项目文件
COPY ["src/Server/LYBT.Server.API/LYBT.Server.API.csproj", "src/Server/LYBT.Server.API/"]
COPY ["src/Server/Core/LYBT.Core/LYBT.Core.csproj", "src/Server/Core/LYBT.Core/"]
COPY ["src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj", "src/Server/Core/LYBT.Infrastructure/"]
COPY ["tests/LYBT.Server.UnitTests/LYBT.Server.UnitTests.csproj", "tests/LYBT.Server.UnitTests/"]
COPY ["tests/LYBT.Server.IntegrationTests/LYBT.Server.IntegrationTests.csproj", "tests/LYBT.Server.IntegrationTests/"]

# 恢复依赖
RUN dotnet restore "src/Server/LYBT.Server.API/LYBT.Server.API.csproj"

# 复制源代码
COPY . .

# 构建项目
RUN dotnet build "LYBT.All.sln" -c Release --no-restore

# 运行测试
FROM test AS test-runner
RUN dotnet test "LYBT.All.sln" -c Release --no-build --logger "trx;LogFileName=test_results.trx" --results-directory "/app/test-results"
```

### 2. 测试环境自动化脚本

#### 测试环境启动脚本
```bash
#!/bin/bash
# start-test-environment.sh

set -e

echo "启动 LYBT 测试环境..."

# 检查 Docker 是否运行
if ! docker info > /dev/null 2>&1; then
    echo "错误: Docker 未运行，请先启动 Docker"
    exit 1
fi

# 清理之前的容器和镜像
echo "清理之前的测试环境..."
docker-compose -f docker-compose.test.yml down -v
docker system prune -f

# 构建和启动服务
echo "构建和启动测试服务..."
docker-compose -f docker-compose.test.yml up --build -d

# 等待服务启动
echo "等待服务启动..."
sleep 30

# 检查服务状态
echo "检查服务状态..."
docker-compose -f docker-compose.test.yml ps

# 等待数据库准备就绪
echo "等待数据库准备就绪..."
until docker-compose -f docker-compose.test.yml exec -T sql-server /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestPassword123! -Q "SELECT 1" > /dev/null 2>&1; do
    echo "等待 SQL Server 启动..."
    sleep 5
done

# 运行数据库迁移
echo "运行数据库迁移..."
docker-compose -f docker-compose.test.yml exec lybt-api dotnet ef database update

# 运行测试
echo "运行测试..."
docker-compose -f docker-compose.test.yml up --build test-runner

# 检查测试结果
echo "检查测试结果..."
if [ -f "test-results/test_results.trx" ]; then
    echo "测试结果已保存到 test-results/test_results.trx"
    # 显示测试摘要
    if command -v dotnet-trx &> /dev/null; then
        dotnet-trx test-results/test_results.trx
    fi
else
    echo "警告: 未找到测试结果文件"
fi

# 停止服务
echo "停止测试服务..."
docker-compose -f docker-compose.test.yml down

echo "测试环境运行完成！"
```

#### 测试数据库初始化脚本
```sql
-- scripts/test/create-test-database.sql
-- 创建测试数据库
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'LYBT_Test')
BEGIN
    CREATE DATABASE LYBT_Test;
END
GO

-- 使用测试数据库
USE LYBT_Test;
GO

-- 创建测试用户
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'lybt_test_user')
BEGIN
    CREATE USER lybt_test_user FOR LOGIN lybt_test_user;
    ALTER ROLE db_owner ADD MEMBER lybt_test_user;
END
GO

-- 插入测试数据
-- 这里可以插入测试所需的初始数据

PRINT '测试数据库 LYBT_Test 创建完成';
GO
```

### 3. CI/CD 集成

#### GitHub Actions 配置
```yaml
# .github/workflows/test.yml
name: Test Environment

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  unit-tests:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Unit Tests
      run: dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test_results.trx" --results-directory TestResults

    - name: Upload Test Results
      if: failure()
      uses: actions/upload-artifact@v3
      with:
        name: unit-test-results
        path: TestResults/

  integration-tests:
    runs-on: ubuntu-latest
    needs: unit-tests

    services:
      sql-server:
        image: mcr.microsoft.com/mssql/server:2019-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: TestPassword123!
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestPassword123! -Q 'SELECT 1'"
          --health-interval 30s
          --health-timeout 10s
          --health-retries 5

      redis:
        image: redis:7-alpine
        ports:
          - 6379:6379
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 30s
          --health-timeout 10s
          --health-retries 5

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Database Migrations
      run: dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/LYBT.Server.API
      env:
        ConnectionStrings__DefaultConnection: Server=localhost,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true;

    - name: Run Integration Tests
      run: dotnet test tests/LYBT.Server.IntegrationTests --no-build --verbosity normal --logger "trx;LogFileName=test_results.trx" --results-directory TestResults
      env:
        ASPNETCORE_ENVIRONMENT: Testing
        ConnectionStrings__DefaultConnection: Server=localhost,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true;
        ConnectionStrings__Redis: localhost:6379
        Authentication__Jwt__SecretKey: test-jwt-secret-key-256-bits-minimum
        Authentication__Jwt__Issuer: LYBT-Test
        Authentication__Jwt__Audience: LYBT-Client-Test

    - name: Upload Test Results
      if: failure()
      uses: actions/upload-artifact@v3
      with:
        name: integration-test-results
        path: TestResults/

  performance-tests:
    runs-on: ubuntu-latest
    needs: integration-tests
    if: github.ref == 'main'

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Setup Node.js
      uses: actions/setup-node@v3
      with:
        node-version: '18'

    - name: Install k6
      run: |
        sudo gpg -k
        sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
        echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
        sudo apt-get update
        sudo apt-get install k6

    - name: Restore and Build
      run: |
        dotnet restore
        dotnet build --no-restore

    - name: Start Application
      run: |
        dotnet run --project src/Server/LYBT.Server.API --urls http://localhost:5000 &
        sleep 30

    - name: Run Performance Tests
      run: |
        k6 run --out json=performance-results.json tests/performance/load-test.js

    - name: Upload Performance Results
      uses: actions/upload-artifact@v3
      with:
        name: performance-results
        path: performance-results.json
```

## 🚀 生产环境配置

### 1. Kubernetes 生产环境

#### Kubernetes 配置
```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: lybt-production
  labels:
    name: lybt-production
    environment: production

---
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: lybt-config
  namespace: lybt-production
  labels:
    app: lybt
    environment: production
data:
  appsettings.json: |
    {
      "Application": {
        "Name": "LYBT.Server",
        "Environment": "Production",
        "Logging": {
          "Level": "Warning",
          "EnableConsole": false,
          "EnableFile": true,
          "FilePath": "/app/logs/lybt.log",
          "RollingInterval": "Day",
          "RetainedFileCountLimit": 30
        }
      },
      "Server": {
        "Urls": "http://0.0.0.0:80",
        "Cors": {
          "AllowOrigins": ["https://lybt.example.com"],
          "AllowMethods": ["GET", "POST", "PUT", "DELETE"],
          "AllowHeaders": ["Authorization", "Content-Type"]
        }
      },
      "Database": {
        "Provider": "SqlServer",
        "ConnectionStringName": "DefaultConnection",
        "EnableRetryOnFailure": true,
        "MaxRetryCount": 5,
        "CommandTimeout": 60
      },
      "Cache": {
        "Provider": "Redis",
        "EnableDistributedCache": true,
        "DefaultExpirationMinutes": 60
      },
      "HealthCheck": {
        "Enabled": true,
        "IntervalSeconds": 30,
        "TimeoutSeconds": 10
      }
    }

---
# k8s/secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: lybt-secrets
  namespace: lybt-production
  labels:
    app: lybt
    environment: production
type: Opaque
data:
  # Base64 编码的配置值
  connection-string: <base64-encoded-connection-string>
  jwt-secret: <base64-encoded-jwt-secret>
  redis-connection: <base64-encoded-redis-connection-string>

---
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: lybt-api
  namespace: lybt-production
  labels:
    app: lybt-api
    environment: production
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: lybt-api
  template:
    metadata:
      labels:
        app: lybt-api
        environment: production
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "80"
        prometheus.io/path: "/metrics"
    spec:
      containers:
      - name: lybt-api
        image: lybt-registry.example.com/lybt-api:latest
        imagePullPolicy: Always
        ports:
        - containerPort: 80
          name: http
        - containerPort: 443
          name: https
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: connection-string
        - name: ConnectionStrings__Redis
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: redis-connection
        - name: Authentication__Jwt__SecretKey
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: jwt-secret
        volumeMounts:
        - name: config-volume
          mountPath: /app/config
        - name: logs-volume
          mountPath: /app/logs
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /health/startup
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 6
      volumes:
      - name: config-volume
        configMap:
          name: lybt-config
      - name: logs-volume
        emptyDir: {}
      imagePullSecrets:
      - name: lybt-registry-secret
      securityContext:
        runAsNonRoot: true
        runAsUser: 1000
        fsGroup: 1000

---
# k8s/service.yaml
apiVersion: v1
kind: Service
metadata:
  name: lybt-api-service
  namespace: lybt-production
  labels:
    app: lybt-api
    environment: production
spec:
  selector:
    app: lybt-api
  ports:
  - name: http
    port: 80
    targetPort: 80
    protocol: TCP
  - name: https
    port: 443
    targetPort: 443
    protocol: TCP
  type: ClusterIP

---
# k8s/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: lybt-api-ingress
  namespace: lybt-production
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/force-ssl-redirect: "true"
    nginx.ingress.kubernetes.io/limit-connections: "100"
    nginx.ingress.kubernetes.io/limit-rps: "50"
    nginx.ingress.kubernetes.io/rate-limit: "100"
spec:
  tls:
  - hosts:
    - lybt-api.example.com
    secretName: lybt-api-tls
  rules:
  - host: lybt-api.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: lybt-api-service
            port:
              number: 80

---
# k8s/hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: lybt-api-hpa
  namespace: lybt-production
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: lybt-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 10
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
      - type: Percent
        value: 100
        periodSeconds: 15
```

### 2. 生产环境部署脚本

#### 部署脚本
```bash
#!/bin/bash
# deploy-production.sh

set -e

# 配置变量
NAMESPACE="lybt-production"
DOCKER_REGISTRY="lybt-registry.example.com"
IMAGE_TAG="latest"
ENVIRONMENT="production"

echo "开始部署 LYBT 生产环境..."

# 检查 kubectl 是否可用
if ! command -v kubectl &> /dev/null; then
    echo "错误: kubectl 未安装或不在 PATH 中"
    exit 1
fi

# 检查集群连接
if ! kubectl cluster-info &> /dev/null; then
    echo "错误: 无法连接到 Kubernetes 集群"
    exit 1
fi

# 创建命名空间
echo "创建命名空间: $NAMESPACE"
kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

# 应用配置
echo "应用配置文件..."
kubectl apply -f k8s/configmap.yaml -n $NAMESPACE
kubectl apply -f k8s/secret.yaml -n $NAMESPACE

# 构建和推送镜像
echo "构建和推送 Docker 镜像..."
docker build -t $DOCKER_REGISTRY/lybt-api:$IMAGE_TAG -f src/Server/LYBT.Server.API/Dockerfile .
docker push $DOCKER_REGISTRY/lybt-api:$IMAGE_TAG

# 部署应用
echo "部署应用..."
kubectl apply -f k8s/deployment.yaml -n $NAMESPACE
kubectl apply -f k8s/service.yaml -n $NAMESPACE
kubectl apply -f k8s/ingress.yaml -n $NAMESPACE
kubectl apply -f k8s/hpa.yaml -n $NAMESPACE

# 等待部署完成
echo "等待部署完成..."
kubectl rollout status deployment/lybt-api -n $NAMESPACE --timeout=600s

# 验证部署
echo "验证部署状态..."
kubectl get pods -n $NAMESPACE -l app=lybt-api
kubectl get services -n $NAMESPACE
kubectl get ingress -n $NAMESPACE

# 健康检查
echo "执行健康检查..."
sleep 30
HEALTH_URL="https://lybt-api.example.com/health"
if curl -f -s $HEALTH_URL > /dev/null; then
    echo "✅ 健康检查通过"
else
    echo "❌ 健康检查失败"
    exit 1
fi

echo "✅ 生产环境部署完成！"
echo "API 地址: https://lybt-api.example.com"
```

#### 回滚脚本
```bash
#!/bin/bash
# rollback-production.sh

set -e

NAMESPACE="lybt-production"
DEPLOYMENT="lybt-api"

echo "开始回滚 LYBT 生产环境..."

# 获取部署历史
echo "获取部署历史..."
kubectl rollout history deployment/$DEPLOYMENT -n $NAMESPACE

# 询问回滚到哪个版本
echo "请输入要回滚到的版本号 (REVISION):"
read -r REVISION

if [[ -z "$REVISION" ]]; then
    echo "错误: 版本号不能为空"
    exit 1
fi

# 执行回滚
echo "回滚到版本: $REVISION"
kubectl rollout undo deployment/$DEPLOYMENT -n $NAMESPACE --to-revision=$REVISION

# 等待回滚完成
echo "等待回滚完成..."
kubectl rollout status deployment/$DEPLOYMENT -n $NAMESPACE --timeout=600s

# 验证回滚
echo "验证回滚状态..."
kubectl get pods -n $NAMESPACE -l app=lybt-api

# 健康检查
echo "执行健康检查..."
sleep 30
HEALTH_URL="https://lybt-api.example.com/health"
if curl -f -s $HEALTH_URL > /dev/null; then
    echo "✅ 回滚成功，健康检查通过"
else
    echo "❌ 回滚失败，健康检查失败"
    exit 1
fi

echo "✅ 生产环境回滚完成！"
```

### 3. 监控和日志

#### Prometheus 监控配置
```yaml
# k8s/monitoring.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
  namespace: monitoring
data:
  prometheus.yml: |
    global:
      scrape_interval: 15s
      evaluation_interval: 15s

    rule_files:
      - "lybt_rules.yml"

    scrape_configs:
      - job_name: 'lybt-api'
        static_configs:
          - targets: ['lybt-api-service.lybt-production.svc.cluster.local:80']
        metrics_path: /metrics
        scrape_interval: 15s

    alerting:
      alertmanagers:
        - static_configs:
            - targets:
              - alertmanager:9093

---
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-rules
  namespace: monitoring
data:
  lybt_rules.yml: |
    groups:
    - name: lybt-api
      rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.1
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value }} errors per second"

      - alert: HighResponseTime
        expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High response time detected"
          description: "95th percentile response time is {{ $value }} seconds"

      - alert: DatabaseConnectionFailure
        expr: up{job="lybt-api"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "API is down"
          description: "LYBT API has been down for more than 1 minute"

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: prometheus
  namespace: monitoring
spec:
  replicas: 1
  selector:
    matchLabels:
      app: prometheus
  template:
    metadata:
      labels:
        app: prometheus
    spec:
      containers:
      - name: prometheus
        image: prom/prometheus:latest
        ports:
        - containerPort: 9090
        volumeMounts:
        - name: config-volume
          mountPath: /etc/prometheus
        - name: rules-volume
          mountPath: /etc/prometheus/rules
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "200m"
      volumes:
      - name: config-volume
        configMap:
          name: prometheus-config
      - name: rules-volume
        configMap:
          name: prometheus-rules
```

#### ELK 日志配置
```yaml
# k8s/logging.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: elasticsearch-config
  namespace: logging
data:
  elasticsearch.yml: |
    cluster.name: "lybt-logs"
    network.host: 0.0.0.0
    discovery.type: single-node
    xpack.security.enabled: false

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: elasticsearch
  namespace: logging
spec:
  replicas: 1
  selector:
    matchLabels:
      app: elasticsearch
  template:
    metadata:
      labels:
        app: elasticsearch
    spec:
      containers:
      - name: elasticsearch
        image: docker.elastic.co/elasticsearch/elasticsearch:8.5.0
        ports:
        - containerPort: 9200
        - containerPort: 9300
        env:
        - name: discovery.type
          value: single-node
        - name: ES_JAVA_OPTS
          value: "-Xms512m -Xmx512m"
        volumeMounts:
        - name: config-volume
          mountPath: /usr/share/elasticsearch/config/elasticsearch.yml
          subPath: elasticsearch.yml
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
      volumes:
      - name: config-volume
        configMap:
          name: elasticsearch-config

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: kibana
  namespace: logging
spec:
  replicas: 1
  selector:
    matchLabels:
      app: kibana
  template:
    metadata:
      labels:
        app: kibana
    spec:
      containers:
      - name: kibana
        image: docker.elastic.co/kibana/kibana:8.5.0
        ports:
        - containerPort: 5601
        env:
        - name: ELASTICSEARCH_HOSTS
          value: "http://elasticsearch:9200"
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
```

## 🔧 环境配置自动化

### 1. 配置管理工具

#### 配置同步脚本
```python
#!/usr/bin/env python3
# sync-config.py

import os
import sys
import json
import yaml
import argparse
from pathlib import Path

class ConfigSync:
    def __init__(self, source_env, target_env):
        self.source_env = source_env
        self.target_env = target_env
        self.config_dir = Path("configs")

    def load_config(self, env):
        """加载指定环境的配置"""
        config_file = self.config_dir / f"{env}.yaml"
        if not config_file.exists():
            raise FileNotFoundError(f"配置文件不存在: {config_file}")

        with open(config_file, 'r', encoding='utf-8') as f:
            return yaml.safe_load(f)

    def save_config(self, env, config):
        """保存配置到指定环境"""
        config_file = self.config_dir / f"{env}.yaml"
        with open(config_file, 'w', encoding='utf-8') as f:
            yaml.dump(config, f, default_flow_style=False, allow_unicode=True)

    def sync_config(self):
        """同步配置"""
        source_config = self.load_config(self.source_env)

        try:
            target_config = self.load_config(self.target_env)
        except FileNotFoundError:
            target_config = {}

        # 合并配置
        merged_config = self.merge_configs(target_config, source_config)

        # 保存配置
        self.save_config(self.target_env, merged_config)

        print(f"✅ 配置已从 {self.source_env} 同步到 {self.target_env}")

    def merge_configs(self, target, source):
        """合并配置"""
        result = target.copy()

        for key, value in source.items():
            if key not in result:
                result[key] = value
            elif isinstance(value, dict) and isinstance(result[key], dict):
                result[key] = self.merge_configs(result[key], value)

        return result

    def validate_config(self, env):
        """验证配置"""
        config = self.load_config(env)

        # 检查必需的配置项
        required_keys = [
            "Application.Name",
            "Database.ConnectionStringName",
            "Authentication.Jwt.Issuer"
        ]

        missing_keys = []
        for key in required_keys:
            if not self.get_nested_value(config, key):
                missing_keys.append(key)

        if missing_keys:
            print(f"❌ 配置验证失败，缺少必需的配置项: {', '.join(missing_keys)}")
            return False

        print(f"✅ 配置验证通过: {env}")
        return True

    def get_nested_value(self, config, key):
        """获取嵌套配置值"""
        keys = key.split('.')
        value = config

        for k in keys:
            if isinstance(value, dict) and k in value:
                value = value[k]
            else:
                return None

        return value

def main():
    parser = argparse.ArgumentParser(description="配置同步工具")
    parser.add_argument("action", choices=["sync", "validate"], help="操作类型")
    parser.add_argument("--source", help="源环境")
    parser.add_argument("--target", help="目标环境")
    parser.add_argument("--env", help="验证的环境")

    args = parser.parse_args()

    if args.action == "sync":
        if not args.source or not args.target:
            print("错误: sync 操作需要指定 --source 和 --target")
            sys.exit(1)

        sync = ConfigSync(args.source, args.target)
        sync.sync_config()

    elif args.action == "validate":
        if not args.env:
            print("错误: validate 操作需要指定 --env")
            sys.exit(1)

        sync = ConfigSync("", args.env)
        sync.validate_config(args.env)

if __name__ == "__main__":
    main()
```

### 2. 环境健康检查

#### 健康检查脚本
```bash
#!/bin/bash
# health-check.sh

set -e

# 配置
ENVIRONMENT=${1:-production}
BASE_URL=""
HEALTH_ENDPOINT="/health"
TIMEOUT=10

# 根据环境设置 URL
case $ENVIRONMENT in
    "development")
        BASE_URL="http://localhost:5000"
        ;;
    "testing")
        BASE_URL="http://localhost:5001"
        ;;
    "staging")
        BASE_URL="https://staging-api.lybt.example.com"
        ;;
    "production")
        BASE_URL="https://api.lybt.example.com"
        ;;
    *)
        echo "错误: 不支持的环境 $ENVIRONMENT"
        exit 1
        ;;
esac

HEALTH_URL="${BASE_URL}${HEALTH_ENDPOINT}"

echo "检查 $ENVIRONMENT 环境健康状态..."
echo "健康检查 URL: $HEALTH_URL"

# 执行健康检查
response=$(curl -s -w "\n%{http_code}" --connect-timeout $TIMEOUT "$HEALTH_URL" || echo "000")

# 解析响应
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | head -n -1)

# 检查结果
if [ "$http_code" = "200" ]; then
    echo "✅ 健康检查通过"
    echo "响应状态码: $http_code"
    echo "响应内容: $body"

    # 检查响应内容
    if echo "$body" | grep -q "Healthy"; then
        echo "✅ 应用状态正常"
    else
        echo "⚠️  应用响应异常，但 HTTP 状态正常"
    fi
else
    echo "❌ 健康检查失败"
    echo "HTTP 状态码: $http_code"
    echo "响应内容: $body"

    # 根据状态码提供建议
    case $http_code in
        "000")
            echo "建议: 检查网络连接和服务是否运行"
            ;;
        "503")
            echo "建议: 服务可能正在启动或不可用"
            ;;
        "500")
            echo "建议: 检查应用日志，可能存在内部错误"
            ;;
        *)
            echo "建议: 检查应用配置和部署状态"
            ;;
    esac

    exit 1
fi
```

## 📚 最佳实践和故障排除

### 1. 配置最佳实践

#### 配置命名规范
- **环境特定**: 使用环境后缀，如 `appsettings.Production.json`
- **功能分组**: 按功能模块分组配置，如 `Database:`, `Authentication:`
- **层次结构**: 使用嵌套结构，如 `Cache:Redis:ConnectionString`
- **大小写敏感**: 使用 PascalCase 命名法

#### 安全最佳实践
- **敏感信息**: 永远不要在代码中硬编码敏感信息
- **最小权限**: 使用最小权限原则配置数据库和服务访问
- **加密存储**: 敏感配置使用加密存储
- **定期轮换**: 定期轮换密钥和密码

### 2. 常见问题解决

#### 连接字符串问题
```bash
# 测试数据库连接
sqlcmd -S localhost -U sa -P password -Q "SELECT 1"

# 检查连接字符串格式
dotnet run --project TestConnection --connection-string="..."
```

#### 端口冲突问题
```bash
# 检查端口占用
netstat -ano | findstr :5000

# 杀死占用进程
taskkill /PID <PID> /F
```

#### 依赖问题
```bash
# 清理并重建
dotnet clean
dotnet restore
dotnet build
```

## 🔄 版本历史

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0 | 2025-10-15 | 初始版本 | 项目团队 |

## 📞 联系方式

- **维护者**: 项目团队
- **技术支持**: DevOps 团队
- **反馈渠道**: GitHub Issues 或内部反馈系统

---

*本文档遵循项目文档标准编写，如有疑问请参考相关文档或联系维护者。*