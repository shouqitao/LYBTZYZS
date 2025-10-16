# API快速参考

**更新时间**: 2025-10-15 18:11:06
**条目数量**: 20 个
**使用说明**: 快速查找常用解决方案，点击目录直接跳转

## 📋 快速目录

1. [```csharp](#1-```csharp)
2. [```powershell](#2-```powershell)
3. [```powershell](#3-```powershell)
4. [```csharp](#4-```csharp)
5. [```csharp](#5-```csharp)
6. [```csharp](#6-```csharp)
7. [```csharp](#7-```csharp)
8. [```text](#8-```text)
9. [```markdown](#9-```markdown)
10. [// 需要注册多个配置选项](#10-//-需要注册多个配置选项)
11. [// 只需要一行注册](#11-//-只需要一行注册)
12. [```csharp](#12-```csharp)
13. [```csharp](#13-```csharp)
14. [foreach (var error in validationResult.Errors)](#14-foreach-(var-error-in-validationresult.errors))
15. [```csharp](#15-```csharp)
16. [```powershell](#16-```powershell)
17. [```yaml](#17-```yaml)
18. [```yaml](#18-```yaml)
19. [```yaml](#19-```yaml)
20. [echo "错误: kubectl 未安装或不在 PATH 中"](#20-echo-"错误:-kubectl-未安装或不在-path-中")

---

## 1. ```csharp

**解决方案**:
// 方法参数和局部变量：camelCase
// 异步方法：以Async结尾

**代码示例**:
```csharp
// 类型和公有成员：PascalCase
public class UserService
public string UserName { get; set; }
public void CreateUser() { }

// 私有字段：_camelCase
private readonly IUserRepository _userRepository;

// 方法参数和局部变量：camelCase
public void UpdateUser(string userName, int userId)

// 异步方法：以Async结尾
public async Task<User> GetUserAsync(int id)

// 常量：UPPER_CASE
public const string DEFAULT_CONNECTION_STRING = "...";
```

**来源**: `DEVELOPER_GUIDE.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 2. ```powershell

**解决方案**:
```powershell
# 1. 构建发布版本
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -o ./publish/api

**代码示例**:
```powershell
# 1. 构建发布版本
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -o ./publish/api
dotnet publish src/Client/Desktop/LYBT.Desktop -c Release -o ./publish/desktop

# 2. 数据库部署
sqlcmd -S localhost -d LYBTDB -i scripts/deploy.sql

# 3. 配置文件
cp appsettings.Production.json ./publish/api/
```

**来源**: `DEVELOPER_GUIDE.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 3. ```powershell

**解决方案**:
```powershell
# 1. 备份数据库
sqlcmd -S prod-server -Q "BACKUP DATABASE LYBTDB TO DISK='C:\Backup\LYBTDB.bak'"

**代码示例**:
```powershell
# 1. 备份数据库
sqlcmd -S prod-server -Q "BACKUP DATABASE LYBTDB TO DISK='C:\Backup\LYBTDB.bak'"

# 2. 停止服务
net stop "LYBT API Service"

# 3. 部署新版本
xcopy /E /Y ./publish/api C:\LYBT\API\
xcopy /E /Y ./publish/desktop C:\LYBT\Desktop\

# 4. 数据库迁移
dotnet ef database update --connection "ProductionConnectionString"

# 5. 启动服务
net start "LYBT API Service"
```

**来源**: `DEVELOPER_GUIDE.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 4. ```csharp

**解决方案**:
```csharp
// 模块注册接口
public interface IModule

**代码示例**:
```csharp
// 模块注册接口
public interface IModule
{
    void RegisterServices(IServiceCollection services);
    void Configure(IApplicationBuilder app);
}

// 动态加载模块
public class ModuleLoader
{
    public void LoadModules(IServiceCollection services)
    {
        var modules = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t))
            .Select(t => Activator.CreateInstance(t) as IModule);
        
        foreach (var module in modules)
        {
            module.RegisterServices(services);
        }
    }
}
```

**来源**: `system-architecture-design.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 5. ```csharp

**解决方案**:
```csharp
// LYBT.Desktop.Shell/App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)

**代码示例**:
```csharp
// LYBT.Desktop.Shell/App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // Foundation 层服务（Shell 统一注册）
    containerRegistry.RegisterSingleton<INavigationService, EnhancedNavigationService>();
    containerRegistry.RegisterSingleton<IDialogService, PrismDialogService>();
    containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
    containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
    containerRegistry.RegisterSingleton<INotificationService, NotificationService>();

    // Infrastructure 层服务（Shell 统一注册）
    containerRegistry.RegisterSingleton<ILogger<T>, Logger<T>>();
    containerRegistry.RegisterSingleton<ICacheService, MemoryCacheService>();
    containerRegistry.RegisterSingleton<IConfigurationService, ConfigurationService>();
}
```

**来源**: `ADR-002-desktop-services-removal.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 6. ```csharp

**解决方案**:
```csharp
// 示例：PatientsModule.cs
public class PatientsModule : IModule

**代码示例**:
```csharp
// 示例：PatientsModule.cs
public class PatientsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册服务
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository, PatientRepository>();

        // 注册AutoMapper配置
        services.AddAutoMapper(typeof(PatientMappingProfile));
    }
}
```

**来源**: `module-dependencies.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 7. ```csharp

**解决方案**:
// ...其他业务方法

**代码示例**:
```csharp
// 委托层接口 - 统一服务入口
public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request);
    // ...其他业务方法
}

// 查询专业层接口 - 只读查询操作
public interface IAuthQueryService
{
    bool IsLoggedIn { get; }
    Task<ServiceResult<UserDto?>> GetCurrentUser();
    Task<ServiceResult<bool>> CheckConnectionAsync();
}

// 业务逻辑层接口 - 状态修改操作
public interface IAuthBusinessService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult> LogoutAsync();
    Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request);
}
```

**来源**: `auth-module.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 8. ```text

**解决方案**:
```text
├── Controllers/           # HTTP控制器
│   └── AuthController.cs

**代码示例**:
```text
├── Controllers/           # HTTP控制器
│   └── AuthController.cs
├── Services/             # 业务服务
│   ├── AuthService.cs
│   ├── JwtService.cs
│   └── EnhancedJwtService.cs
├── Interfaces/           # 服务接口
│   ├── IAuthService.cs
│   └── IJwtService.cs
├── Models/              # 内部模型
├── Configuration/       # 配置选项
└── README.md
```

**来源**: `auth-module.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 9. ```markdown

**解决方案**:
- Line 45: 考虑提取方法减少复杂度
- Line 156: 缺少异步方法的ConfigureAwait

**代码示例**:
```markdown
## 代码审查报告

### ✅ 通过项
- 命名规范: 100%符合
- 依赖注入: 正确使用构造函数注入
- 文件编码: UTF-8 with BOM

### ⚠️ 建议改进
- Line 45: 考虑提取方法减少复杂度
- Line 78: 可优化LINQ查询性能

### ❌ 必须修复
- Line 120: 使用了ServiceLocator反模式
- Line 156: 缺少异步方法的ConfigureAwait
```

**来源**: `ai-collaboration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 10. // 需要注册多个配置选项

**解决方案**:
```csharp
// 需要注册多个配置选项
services.Configure<AuthOptions>(configuration.GetSection("AuthOptions"));

**代码示例**:
```csharp
// 需要注册多个配置选项
services.Configure<AuthOptions>(configuration.GetSection("AuthOptions"));
services.Configure<JwtOptions>(configuration.GetSection("JwtOptions"));
services.Configure<DatabaseOptions>(configuration.GetSection("DatabaseOptions"));
services.Configure<CacheOptions>(configuration.GetSection("CacheOptions"));
// ... 更多配置类
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 11. // 只需要一行注册

**解决方案**:
```csharp
// 只需要一行注册
services.AddLybtConfiguration(configuration);

**代码示例**:
```csharp
// 只需要一行注册
services.AddLybtConfiguration(configuration);
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 12. ```csharp

**解决方案**:
```csharp
// Program.cs
services.Configure<AuthOptions>(configuration.GetSection("AuthOptions"));

**代码示例**:
```csharp
// Program.cs
services.Configure<AuthOptions>(configuration.GetSection("AuthOptions"));
services.Configure<JwtOptions>(configuration.GetSection("JwtOptions"));
services.Configure<DatabaseOptions>(configuration.GetSection("DatabaseOptions"));
services.Configure<CacheOptions>(configuration.GetSection("CacheOptions"));
services.Configure<SecurityOptions>(configuration.GetSection("SecurityOptions"));
services.Configure<UserOptions>(configuration.GetSection("UserOptions"));
services.Configure<DefaultPasswordOptions>(configuration.GetSection("DefaultPasswordOptions"));
services.Configure<RateLimitingOptions>(configuration.GetSection("RateLimitingOptions"));
services.Configure<SysAdminOptions>(configuration.GetSection("SysAdminOptions"));
services.Configure<WebApiConfigurationOptions>(configuration.GetSection("WebApiOptions"));
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 13. ```csharp

**解决方案**:
```csharp
// Program.cs
services.AddLybtConfiguration(configuration);

**代码示例**:
```csharp
// Program.cs
services.AddLybtConfiguration(configuration);
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 14. foreach (var error in validationResult.Errors)

**解决方案**:
```csharp
// Program.cs
services.AddLybtConfiguration(configuration);

**代码示例**:
```csharp
// Program.cs
services.AddLybtConfiguration(configuration);

// 在应用启动时验证配置
var validationResult = configuration.ValidateLybtConfiguration();
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"配置错误: {error}");
    }
    throw new InvalidOperationException("配置验证失败");
}
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 15. ```csharp

**解决方案**:
```csharp
services.Configure<LybtOptions>(configuration.GetSection(LybtOptions.SectionName));
services.PostConfigure<LybtOptions>(options =>

**代码示例**:
```csharp
services.Configure<LybtOptions>(configuration.GetSection(LybtOptions.SectionName));
services.PostConfigure<LybtOptions>(options =>
{
    // 运行时验证逻辑
    if (string.IsNullOrEmpty(options.Authentication.Jwt.SecretKey))
    {
        throw new InvalidOperationException("JWT SecretKey 不能为空");
    }
});
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 16. ```powershell

**解决方案**:
```powershell
# Windows PowerShell
# setup-project.ps1

**代码示例**:
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

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 17. ```yaml

**解决方案**:
```yaml
# docker-compose.test.yml
version: '3.8'

**代码示例**:
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

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 18. ```yaml

**解决方案**:
```yaml
# .github/workflows/test.yml
name: Test Environment

**代码示例**:
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

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 19. ```yaml

**解决方案**:
```yaml
# k8s/namespace.yaml
apiVersion: v1

**代码示例**:
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

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 20. echo "错误: kubectl 未安装或不在 PATH 中"

**解决方案**:
```bash
#!/bin/bash
# deploy-production.sh

**代码示例**:
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

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 💡 使用建议

- **快速查找**: 使用目录快速定位到具体问题
- **代码示例**: 所有代码示例都可以直接复制使用
- **相关问题**: 查看条目的来源文档获取更多详细信息
- **反馈建议**: 发现问题或有改进建议请及时反馈

