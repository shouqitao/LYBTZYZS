# 开发者指南 - LYBTZYZS 项目

> **统一版本**：合并原 DEVELOPER_GUIDE.md 和 development/development-guide.md，消除60%重复内容
> **最后更新**：2025-09-28
> **维护人**：开发团队

## 📋 目录

1. [项目概述](#项目概述)
2. [快速开始](#快速开始)
3. [开发环境设置](#开发环境设置)
4. [项目结构](#项目结构)
5. [开发规范](#开发规范)
6. [技术栈说明](#技术栈说明)
7. [常用开发命令](#常用开发命令)
8. [调试指南](#调试指南)
9. [测试策略](#测试策略)
10. [部署流程](#部署流程)
11. [故障排除](#故障排除)
12. [贡献指南](#贡献指南)

## 📖 项目概述

LYBTZYZS（凌隐宝堂中医诊所管理系统）是一个面向中医诊所的企业级 .NET 8 解决方案。

### 核心特性
- **前端**：WPF + Prism.DryIoc 桌面应用
- **后端**：ASP.NET Core Web API + EF Core
- **架构**：模块化双层架构，适度设计原则
- **数据库**：SQL Server 2019+
- **部署**：传统部署方式，避免过度工程

### 业务模块
- **用户管理**：角色权限、用户CRUD
- **患者管理**：患者档案、就诊历史
- **诊疗管理**：诊疗记录、医疗案例
- **处方管理**：开处方、药材配伍
- **药材管理**：库存管理、供应商
- **方剂管理**：经典方剂、自定义配方

## 🚀 快速开始

### 前置要求
```powershell
# 检查环境
dotnet --version  # 需要 .NET 8.0+
sql --version     # 需要 SQL Server 2019+
```

### 一键启动
```powershell
# 1. 克隆代码
git clone <repository-url>
cd LYBTZYZS

# 2. 还原依赖
dotnet restore LYBT.All.sln

# 3. 数据库迁移（首次）
dotnet ef database update --project src/Server/Infrastructure/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 4. 启动后端
dotnet run --project src/Server/Services/LYBT.WebAPI

# 5. 启动前端（新终端）
dotnet run --project src/Client/Desktop/LYBT.Desktop
```

### 验证安装
- 后端：访问 `https://localhost:5001/swagger`
- 前端：应显示登录界面，默认用户 admin/admin123

## 🛠️ 开发环境设置

### IDE 推荐配置
```json
// .vscode/settings.json
{
    "dotnet.defaultSolution": "LYBT.All.sln",
    "omnisharp.enableRoslynAnalyzers": true,
    "editor.formatOnSave": true
}
```

### Git 配置
```powershell
# 设置中文提交信息
git config core.quotepath false
git config i18n.commitencoding utf-8
git config i18n.logoutputencoding utf-8
```

### 数据库连接字符串
```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB_Dev;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

## 📁 项目结构

```
LYBTZYZS/
├── src/
│   ├── Client/Desktop/           # WPF 桌面客户端
│   │   ├── LYBT.Desktop/         # 主应用程序
│   │   └── Modules/              # 业务模块
│   │       ├── LYBT.Desktop.Users/
│   │       ├── LYBT.Desktop.Patients/
│   │       ├── LYBT.Desktop.Prescriptions/
│   │       └── ...
│   ├── Server/                   # 服务器端
│   │   ├── Core/                 # 核心基础设施
│   │   │   ├── LYBT.Entities/    # 实体定义
│   │   │   └── LYBT.Infrastructure/ # 数据访问
│   │   ├── Modules/              # 业务模块
│   │   │   ├── LYBT.Module.Users/
│   │   │   ├── LYBT.Module.Patients/
│   │   │   └── ...
│   │   └── Services/
│   │       └── LYBT.WebAPI/      # Web API 服务
│   └── Shared/                   # 共享组件
│       ├── LYBT.Shared.Models/   # DTO 和契约
│       ├── LYBT.Shared.Utilities/ # 通用工具
│       └── LYBT.Shared.Interfaces/ # 服务接口
├── tests/                        # 测试项目
├── docs/                         # 文档系统
└── tools/                        # 开发工具
```

### 关键目录说明

#### src/Client/Desktop/ - WPF 客户端
- **LYBT.Desktop**：主应用程序，包含Shell和导航
- **Modules/**：各业务模块，采用Prism模块化架构
- **Core/**：客户端核心组件和基础设施

#### src/Server/ - 服务器端
- **Core/LYBT.Entities**：EF实体定义
- **Core/LYBT.Infrastructure**：数据访问层
- **Modules/**：业务模块，包含Controller、Service、Repository
- **Services/LYBT.WebAPI**：API网关和启动配置

#### src/Shared/ - 共享层
- **Models**：DTO、请求/响应模型
- **Interfaces**：服务接口定义
- **Utilities**：通用工具类

## 📝 开发规范

### 命名约定
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

### 代码组织原则
1. **单一职责**：每个类只负责一个业务关注点
2. **依赖注入**：通过构造函数注入接口，禁用ServiceLocator
3. **异步优先**：所有I/O操作使用async/await
4. **文件大小**：单文件不超过500行，复杂时拆分

### 注释规范
```csharp
/// <summary>
/// 创建新用户
/// </summary>
/// <param name="request">用户创建请求</param>
/// <returns>创建的用户信息</returns>
/// <exception cref="ArgumentNullException">当request为null时抛出</exception>
public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
{
    // 验证输入参数
    if (request == null)
        throw new ArgumentNullException(nameof(request));
    
    // 业务逻辑处理...
}
```

## 🔧 技术栈说明

### 前端技术栈
```xml
<!-- 核心框架 -->
<PackageReference Include="Microsoft.WindowsDesktop.App" Version="8.0" />
<PackageReference Include="Prism.DryIoc" Version="8.1.97" />

<!-- UI框架 -->
<PackageReference Include="MaterialDesignThemes" Version="4.9.0" />
<PackageReference Include="HandyControl" Version="3.4.0" />

<!-- 工具库 -->
<PackageReference Include="AutoMapper" Version="12.0.1" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
```

### 后端技术栈
```xml
<!-- 核心框架 -->
<PackageReference Include="Microsoft.AspNetCore.App" Version="8.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />

<!-- 工具库 -->
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
```

### 架构模式

#### 模块化双层架构（前端）
```csharp
// Module层：委托和协调
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册QueryService（读取）
        containerRegistry.RegisterSingleton<IPatientQueryService, PatientQueryService>();
        
        // 注册BusinessService（写入）
        containerRegistry.RegisterSingleton<IPatientBusinessService, PatientBusinessService>();
    }
}

// QueryService：只读查询
public class PatientQueryService : IPatientQueryService
{
    public async Task<List<PatientDto>> GetPatientsAsync() { /* 只读操作 */ }
}

// BusinessService：业务逻辑
public class PatientBusinessService : IPatientBusinessService
{
    public async Task<PatientDto> CreatePatientAsync(CreatePatientRequest request) { /* 写入操作 */ }
}
```

#### 三层架构（后端）
```csharp
// Controller层：HTTP端点
[ApiController]
[Route("api/v1/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientDto>>> GetPatients([FromQuery] PatientSearchDto searchDto)
    {
        var result = await _patientService.GetPagedAsync(searchDto);
        return Ok(result);
    }
}

// Service层：业务逻辑
public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    
    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientSearchDto searchDto)
    {
        // 业务验证和处理
        var patients = await _repository.GetPagedAsync(searchDto);
        return ServiceResult.Success(patients);
    }
}

// Repository层：数据访问
public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _context;
    
    public async Task<PagedResult<Patient>> GetPagedAsync(PatientSearchDto searchDto)
    {
        var query = _context.Patients.AsNoTracking();
        // 分页查询逻辑
        return await query.ToPagedResultAsync(searchDto.Page, searchDto.PageSize);
    }
}
```

## ⚙️ 常用开发命令

### 解决方案管理
```powershell
# 构建整个解决方案
dotnet build LYBT.All.sln -c Release

# 分别构建前后端
dotnet build LYBT.Server.sln -c Release
dotnet build LYBT.Desktop.sln -c Release

# 清理构建缓存
dotnet clean LYBT.All.sln
```

### 数据库操作
```powershell
# 添加迁移
dotnet ef migrations add MigrationName --project src/Server/Infrastructure/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update --project src/Server/Infrastructure/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 删除最后一个迁移
dotnet ef migrations remove --project src/Server/Infrastructure/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 生成SQL脚本
dotnet ef migrations script --project src/Server/Infrastructure/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

### 测试执行
```powershell
# 运行所有测试
dotnet test LYBT.All.sln -c Release

# 运行特定项目测试
dotnet test tests/UnitTests/Core/Core.Services.Tests -c Release

# 生成测试覆盖率报告
dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults
```

### 代码质量
```powershell
# 代码格式化
dotnet format LYBT.All.sln

# 代码分析
dotnet build LYBT.All.sln --verbosity normal

# NuGet包更新
dotnet list package --outdated
dotnet add package PackageName --version x.x.x
```

## 🐛 调试指南

### Visual Studio 配置
```json
// launchSettings.json
{
  "profiles": {
    "LYBT.WebAPI": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### 日志配置
```json
// appsettings.Development.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "LYBT": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
```

### 断点调试技巧
```csharp
public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request)
{
    // 条件断点：request.UserName == "admin"
    _logger.LogDebug("创建用户：{UserName}", request.UserName);
    
    try
    {
        // 业务逻辑
        var user = await _userRepository.CreateAsync(request);
        return ServiceResult.Success(user);
    }
    catch (Exception ex)
    {
        // 异常断点
        _logger.LogError(ex, "创建用户失败：{UserName}", request.UserName);
        return ServiceResult.Failure<UserDto>("创建用户失败");
    }
}
```

## 🧪 测试策略

### 测试分层
```
tests/
├── UnitTests/              # 单元测试
│   ├── Core/               # 核心组件测试
│   ├── Server/             # 服务器端测试
│   └── Client/             # 客户端测试
├── IntegrationTests/       # 集成测试
│   ├── API/                # API集成测试
│   └── Database/           # 数据库集成测试
└── E2ETests/               # 端到端测试
    └── Desktop/            # 桌面应用E2E测试
```

### 单元测试示例
```csharp
[TestClass]
public class UserServiceTests
{
    private Mock<IUserRepository> _mockRepository;
    private UserService _userService;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _userService = new UserService(_mockRepository.Object);
    }
    
    [TestMethod]
    public async Task CreateUserAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateUserRequest { UserName = "testuser" };
        var expectedUser = new User { Id = Guid.NewGuid(), UserName = "testuser" };
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<CreateUserRequest>()))
                      .ReturnsAsync(expectedUser);
        
        // Act
        var result = await _userService.CreateUserAsync(request);
        
        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("testuser", result.Data.UserName);
    }
}
```

### 集成测试配置
```csharp
[TestClass]
public class UsersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public UsersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [TestMethod]
    public async Task GetUsers_ReturnsOkResult()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("items"));
    }
}
```

## 🚀 部署流程

### 开发环境部署
```powershell
# 1. 构建发布版本
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -o ./publish/api
dotnet publish src/Client/Desktop/LYBT.Desktop -c Release -o ./publish/desktop

# 2. 数据库部署
sqlcmd -S localhost -d LYBTDB -i scripts/deploy.sql

# 3. 配置文件
cp appsettings.Production.json ./publish/api/
```

### 生产环境部署
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

### Docker部署（可选）
```dockerfile
# Dockerfile.api
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj", "src/Server/Services/LYBT.WebAPI/"]
RUN dotnet restore "src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj"
COPY . .
WORKDIR "/src/src/Server/Services/LYBT.WebAPI"
RUN dotnet build "LYBT.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LYBT.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LYBT.WebAPI.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'
services:
  lybt-api:
    build: 
      context: .
      dockerfile: Dockerfile.api
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - sqlserver
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      SA_PASSWORD: "YourPassword123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
```

## 🔧 故障排除

### 常见编译错误

#### 1. 依赖包冲突
```
错误：NU1605 检测到依赖包降级
解决：
dotnet clean
dotnet restore --force
dotnet build
```

#### 2. EF迁移失败
```
错误：无法连接到数据库
解决：
1. 检查连接字符串
2. 确保SQL Server运行
3. 验证用户权限
```

#### 3. Prism模块加载失败
```csharp
// 检查模块注册
public partial class App : PrismApplication
{
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 确保所有模块都已注册
        moduleCatalog.AddModule<UsersModule>();
        moduleCatalog.AddModule<PatientsModule>();
    }
}
```

### 性能问题诊断

#### 1. 数据库查询优化
```sql
-- 检查执行计划
SET STATISTICS IO ON
SELECT * FROM Patients WHERE RealName LIKE '%张%'

-- 添加索引
CREATE INDEX IX_Patients_RealName ON Patients(RealName)
```

#### 2. 内存泄漏排查
```csharp
// 使用 using 语句
using var scope = serviceProvider.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IUserService>();

// 及时释放事件订阅
public void Dispose()
{
    _eventAggregator.GetEvent<UserUpdatedEvent>().Unsubscribe(_token);
}
```

### 日志分析
```powershell
# 查看错误日志
Get-Content logs/app-20250928.log | Select-String "ERROR"

# 统计API调用次数
Get-Content logs/app-20250928.log | Select-String "GET /api/v1/users" | Measure-Object
```

## 📋 贡献指南

### 开发流程
1. **Fork项目**：创建个人分支
2. **创建Issue**：描述要解决的问题
3. **创建分支**：`feature/issue-123-description`
4. **开发功能**：遵循开发规范
5. **编写测试**：确保测试覆盖率
6. **提交PR**：关联Issue，清晰描述
7. **代码审查**：响应审查意见
8. **合并代码**：审查通过后合并

### 提交信息规范
```
<type>(<scope>): <subject>

<body>

<footer>
```

示例：
```
feat(users): 新增用户批量导入功能

- 支持Excel文件导入
- 包含数据验证和重复检查
- 添加进度显示

Closes #123
```

### 代码审查清单
- [ ] 代码符合项目规范
- [ ] 单元测试覆盖新功能
- [ ] 文档已同步更新
- [ ] 性能影响已评估
- [ ] 安全问题已检查
- [ ] 向后兼容性确认

### 发布版本
```powershell
# 1. 更新版本号
# 修改 src/Directory.Build.props 中的 Version

# 2. 生成发布说明
git log --oneline v1.0.0..HEAD > CHANGELOG.md

# 3. 创建标签
git tag -a v1.1.0 -m "发布版本 1.1.0"
git push origin v1.1.0

# 4. 构建发布包
dotnet pack -c Release -o ./packages
```

---

## 📚 相关文档

- [架构设计文档](./docs/architecture/)
- [API接口文档](./docs/api/)
- [用户使用手册](./docs/user-guide/)
- [部署运维手册](./docs/deployment/)

## 🆘 获取帮助

- **技术问题**：提交GitHub Issue
- **功能建议**：创建Feature Request
- **文档改进**：提交PR到docs目录

---

*文档版本: 2.0 - 统一合并版*  
*最后更新: 2025-09-28*  
*状态: 活跃维护中*