# CCPM 常见问题FAQ

## 概述

本文档收录CCPM (Code-Claude Project Manager) 系统使用过程中的常见问题和解决方案。基于LYBTZYZS项目实际开发过程中遇到的问题整理，提供快速解答和解决思路。

## 快速导航

- [环境配置问题](#环境配置问题)
- [编译构建问题](#编译构建问题)
- [依赖管理问题](#依赖管理问题)
- [运行时问题](#运行时问题)
- [数据库问题](#数据库问题)
- [性能问题](#性能问题)
- [部署问题](#部署问题)
- [开发工具问题](#开发工具问题)

## 环境配置问题

### Q1: 如何确认开发环境是否正确配置？

**A:** 运行环境检查脚本：

```bash
# 检查 .NET SDK 版本
dotnet --version
# 应该显示 8.0.x 或更高版本

# 检查 Git 配置
git --version
git config --list

# 检查项目结构
dir src\Server\Services\LYBT.WebAPI
dir src\Client\Desktop
```

**预期结果**:
- .NET SDK 8.0 或更高版本
- Git 2.30 或更高版本  
- 项目目录结构完整

### Q2: Visual Studio 无法打开解决方案文件？

**A:** 常见原因和解决方案：

1. **版本兼容性问题**
   ```bash
   # 检查 Visual Studio 版本
   # 需要 Visual Studio 2022 17.8 或更高版本
   ```

2. **解决方案文件损坏**
   ```bash
   # 备份并重新生成解决方案文件
   git status
   git checkout HEAD -- *.sln
   ```

3. **缺少必要的工作负载**
   - 打开 Visual Studio Installer
   - 确保安装了 ".NET 桌面开发" 和 "ASP.NET 和 Web 开发" 工作负载

### Q3: PowerShell 执行策略限制脚本运行？

**A:** 临时或永久修改执行策略：

```powershell
# 临时允许（当前会话）
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# 永久允许（当前用户）
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# 检查当前策略
Get-ExecutionPolicy -List
```

## 编译构建问题

### Q4: 出现 CS0246 "找不到类型或命名空间" 错误？

**A:** 按以下步骤排查：

1. **检查项目引用**
   ```bash
   # 查看项目依赖关系
   dotnet list reference
   
   # 添加缺失的项目引用
   dotnet add reference path/to/project.csproj
   ```

2. **清理和重建**
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

3. **检查 using 语句**
   ```csharp
   // 确保包含正确的命名空间
   using LYBT.Shared.Models;
   using LYBT.Infrastructure.Data;
   ```

### Q5: 前端编译时出现 "找不到IService接口" 错误？

**A:** 这是UltraThink架构重构后的常见问题：

1. **确认接口实现**
   ```csharp
   // 正确的实现方式
   public class UserModule : IUserService
   {
       // 实现 IUserService 接口方法
   }
   ```

2. **检查依赖注入配置**
   ```csharp
   // 在 ServiceCollectionExtensions.cs 中
   services.AddTransient<IUserService, UserModule>();
   ```

3. **验证 ViewModel 注入**
   ```csharp
   // ViewModel 中正确的依赖注入
   public UserManagementViewModel(IUserService userService)
   {
       _userService = userService;
   }
   ```

### Q6: 编译警告 CS1998 "异步方法缺少 await 操作符"？

**A:** LYBTZYZS项目已在Phase 5中解决，解决方案：

1. **添加实际的异步操作**
   ```csharp
   public async Task<ServiceResult<User>> GetUserAsync(Guid id)
   {
       // 添加实际的异步调用
       return await _repository.GetByIdAsync(id);
   }
   ```

2. **移除不必要的async关键字**
   ```csharp
   // 如果没有异步操作，直接返回Task
   public Task<ServiceResult<User>> GetUserAsync(Guid id)
   {
       var result = _repository.GetById(id);
       return Task.FromResult(result);
   }
   ```

## 依赖管理问题

### Q7: NuGet包版本冲突如何解决？

**A:** 使用依赖检查脚本和手动排查：

1. **运行依赖检查**
   ```bash
   # 使用项目提供的脚本
   scripts\dependency-check.ps1
   ```

2. **统一包版本**
   ```xml
   <!-- 在 Directory.Packages.props 中统一管理 -->
   <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.17" />
   <PackageVersion Include="Prism.DryIoc" Version="9.0.537" />
   ```

3. **清理包缓存**
   ```bash
   # 清理全局包缓存
   dotnet nuget locals all --clear
   
   # 重新还原包
   dotnet restore --force
   ```

### Q8: AutoMapper 配置错误导致的运行时异常？

**A:** LYBTZYZS项目AutoMapper配置要点：

1. **正确的配置方式**
   ```csharp
   // 必须提供 ILoggerFactory 参数
   var mapperConfig = new MapperConfiguration(cfg =>
   {
       cfg.AddProfile(new MappingProfile());
   }, NullLoggerFactory.Instance);
   
   var mapper = mapperConfig.CreateMapper();
   ```

2. **检查映射配置**
   ```csharp
   // 在 MappingProfile.cs 中确保所有映射正确
   CreateMap<User, UserDto>()
       .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.ToString()));
   ```

3. **验证映射配置**
   ```csharp
   // 在应用启动时验证映射配置
   mapperConfig.AssertConfigurationIsValid();
   ```

## 运行时问题

### Q9: 应用启动时出现依赖注入异常？

**A:** 检查服务注册和依赖关系：

1. **确认服务注册完整**
   ```csharp
   // 检查 Program.cs 或 Startup.cs 中的服务注册
   builder.Services.AddUserModule();     // 用户模块
   builder.Services.AddPatientModule();  // 患者模块
   // ... 其他模块
   ```

2. **检查循环依赖**
   ```csharp
   // 避免服务之间的循环引用
   // 如果 A 依赖 B，B 不应该直接或间接依赖 A
   ```

3. **验证接口实现**
   ```csharp
   // 确保所有注册的接口都有对应的实现
   services.AddTransient<IUserService, UserModule>();
   ```

### Q10: JWT认证失败，无法访问受保护的API？

**A:** 检查JWT配置和token生成：

1. **验证JWT配置**
   ```json
   {
     "JWT": {
       "SecretKey": "your-256-bit-secret-key-here-make-it-long-enough",
       "Issuer": "LYBT-System",
       "Audience": "LYBT-Users",
       "ExpireHours": 8,
       "RememberMeDays": 30
     }
   }
   ```

2. **检查token格式**
   ```csharp
   // 确保请求头正确设置
   Authorization: Bearer your-jwt-token-here
   ```

3. **验证用户角色**
   ```csharp
   // 确认用户具有所需的角色权限
   [Authorize(Roles = "Admin")]
   [Authorize(Roles = "Doctor,Admin")]
   ```

### Q11: 数据库迁移执行失败？

**A:** 常见迁移问题解决：

1. **检查迁移文件**
   ```bash
   # 查看待应用的迁移
   dotnet ef migrations list --project src\Server\Core\LYBT.Infrastructure --startup-project src\Server\Services\LYBT.WebAPI
   ```

2. **手动执行迁移**
   ```bash
   # 强制执行特定迁移
   dotnet ef database update --project src\Server\Core\LYBT.Infrastructure --startup-project src\Server\Services\LYBT.WebAPI
   ```

3. **回滚和重新创建**
   ```bash
   # 回滚到指定迁移
   dotnet ef database update [MigrationName] --project src\Server\Core\LYBT.Infrastructure --startup-project src\Server\Services\LYBT.WebAPI
   
   # 删除和重新创建迁移
   dotnet ef migrations remove --project src\Server\Core\LYBT.Infrastructure --startup-project src\Server\Services\LYBT.WebAPI
   ```

## 数据库问题

### Q12: 数据库连接字符串配置错误？

**A:** 检查和修复连接字符串：

1. **标准SQL Server连接字符串**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=LYBTDB;Integrated Security=true;TrustServerCertificate=true;"
     }
   }
   ```

2. **测试数据库连接**
   ```bash
   # 使用 sqlcmd 测试连接
   sqlcmd -S localhost -E -Q "SELECT @@VERSION"
   ```

3. **检查SQL Server服务**
   ```powershell
   # 确认SQL Server服务运行状态
   Get-Service -Name "*SQL*" | Where-Object {$_.Status -eq "Running"}
   ```

### Q13: 实体框架上下文初始化失败？

**A:** 检查DbContext配置：

1. **验证AppDbContext配置**
   ```csharp
   // 在 Program.cs 中确认配置
   builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseSqlServer(connectionString));
   ```

2. **检查实体映射**
   ```csharp
   // 确认所有实体都在 AppDbContext 中配置
   public DbSet<User> Users { get; set; }
   public DbSet<Patient> Patients { get; set; }
   // ... 其他实体
   ```

3. **验证迁移历史**
   ```bash
   # 检查迁移历史表
   sqlcmd -S localhost -E -Q "SELECT * FROM __EFMigrationsHistory"
   ```

## 性能问题

### Q14: API响应时间过长？

**A:** 性能优化检查点：

1. **检查数据库查询**
   ```csharp
   // 使用Include避免N+1查询
   var users = await _context.Users
       .Include(u => u.Patients)
       .ToListAsync();
   
   // 使用分页减少数据传输
   var pagedUsers = await _context.Users
       .Skip(skip)
       .Take(take)
       .ToListAsync();
   ```

2. **启用查询日志**
   ```csharp
   // 在开发环境启用SQL查询日志
   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
       optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
   }
   ```

3. **检查内存缓存使用**
   ```csharp
   // 确认缓存服务正确配置和使用
   services.AddMemoryCache();
   ```

### Q15: 应用内存占用过高？

**A:** 内存问题排查：

1. **检查资源释放**
   ```csharp
   // 确保实现IDisposable的对象正确释放
   using (var context = new AppDbContext())
   {
       // 数据库操作
   }
   ```

2. **监控内存使用**
   ```powershell
   # 监控应用进程内存使用
   Get-Process | Where-Object {$_.ProcessName -like "*LYBT*"} | Select-Object ProcessName, WorkingSet64
   ```

3. **检查大对象创建**
   ```csharp
   // 避免创建不必要的大对象
   // 使用流式处理大量数据
   ```

## 部署问题

### Q16: IIS部署后应用无法启动？

**A:** IIS部署问题排查：

1. **检查应用池配置**
   - .NET CLR 版本：无托管代码
   - 托管管道模式：集成
   - 进程模型 > 标识：ApplicationPoolIdentity

2. **确认依赖安装**
   ```bash
   # 确保服务器安装了 .NET 8 Runtime
   dotnet --list-runtimes
   ```

3. **检查应用配置**
   ```json
   // appsettings.Production.json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Warning"
       }
     }
   }
   ```

### Q17: 生产环境数据库连接失败？

**A:** 生产数据库配置：

1. **使用环境特定配置**
   ```json
   // appsettings.Production.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=prod-server;Database=LYBTDB_Prod;User Id=app_user;Password=secure_password;TrustServerCertificate=true;"
     }
   }
   ```

2. **检查防火墙和网络**
   ```bash
   # 测试数据库服务器连通性
   telnet prod-server 1433
   ```

3. **验证数据库权限**
   ```sql
   -- 确认应用用户具有必要权限
   GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO app_user;
   ```

## 开发工具问题

### Q18: Visual Studio IntelliSense 不工作？

**A:** IntelliSense 修复步骤：

1. **重建解决方案**
   ```bash
   # 清理并重建
   dotnet clean
   dotnet build
   ```

2. **清理 Visual Studio 缓存**
   - 关闭 Visual Studio
   - 删除 `bin` 和 `obj` 文件夹
   - 删除 `.vs` 文件夹
   - 重新打开项目

3. **重置用户设置**
   ```bash
   # 重置 Visual Studio 设置（谨慎操作）
   devenv /resetuserdata
   ```

### Q19: Git 提交时出现编码问题？

**A:** Git 编码配置：

1. **设置Git编码**
   ```bash
   git config --global core.autocrlf true
   git config --global core.quotepath false
   git config --global i18n.commitencoding utf-8
   git config --global i18n.logoutputencoding utf-8
   ```

2. **检查文件编码**
   ```bash
   # 确保源文件使用UTF-8编码
   file --mime-encoding *.cs
   ```

### Q20: 如何快速定位性能瓶颈？

**A:** 性能分析工具和方法：

1. **使用内置性能计数器**
   ```csharp
   // 在应用中添加性能监控
   using (var activity = Activity.StartActivity("UserQuery"))
   {
       // 业务逻辑
       return await userService.GetUsersAsync();
   }
   ```

2. **启用详细日志**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Microsoft.EntityFrameworkCore": "Information",
         "System.Net.Http.HttpClient": "Information"
       }
     }
   }
   ```

3. **使用诊断工具**
   - Visual Studio 诊断工具
   - dotMemory 或 PerfView
   - SQL Server Profiler

## 紧急处理流程

### 当遇到未在FAQ中覆盖的问题时：

1. **收集问题信息**
   - 详细的错误信息和堆栈跟踪
   - 复现步骤
   - 环境信息（操作系统、.NET版本等）

2. **尝试基础解决方案**
   - 重启应用和相关服务
   - 清理和重建项目
   - 检查最近的代码变更

3. **寻求帮助**
   - 查询官方文档和社区
   - 在团队内部讨论
   - 记录新发现的问题和解决方案

4. **更新文档**
   - 将新问题和解决方案添加到FAQ
   - 更新相关的故障排除指南
   - 分享经验给团队成员

## 相关资源

- [CCPM 故障排除指南](CPM-故障排除指南.md) - 系统性故障诊断流程
- [CCPM 错误代码参考](CPM-错误代码参考.md) - 错误代码详细说明
- [CCPM 应急响应预案](CPM-应急响应预案.md) - 紧急情况处理流程
- [Microsoft .NET 官方文档](https://docs.microsoft.com/en-us/dotnet/)
- [Entity Framework Core 文档](https://docs.microsoft.com/en-us/ef/core/)

## 更新记录

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0.0 | 2025-01-31 | 初始版本，收录LYBTZYZS项目实践中的20个常见问题 | Claude |

---

**贡献指南**:
1. 遇到新问题时，请按照模板格式添加到对应分类中
2. 解决方案应该经过验证并包含具体的操作步骤
3. 定期回顾和更新过时的信息
4. 保持问题编号的连续性，方便引用