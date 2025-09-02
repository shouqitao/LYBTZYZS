# 👨‍💻 LYBTZYZS开发者上手指南 (Developer Onboarding Guide)

## 🎯 快速开始

欢迎加入LYBTZYZS凌隐宝堂中医诊所系统开发团队！本指南将帮助您在**30分钟内**完成开发环境配置并开始第一次代码提交。

**项目状态**: ✅ **生产就绪** | 🏗️ **混合架构** | 📊 **48个项目零编译错误** | 🚀 **新人友好**

---

## 📋 项目概览

### 🏥 业务背景
LYBTZYZS是专为**2-5人小诊所**设计的中医诊疗管理系统，采用现代化混合架构，支持完整的中医诊疗流程。

### 🏗️ 技术架构总览
```
混合架构设计 (2025-09-02当前状态)
├── 后端 Web API (传统三层架构)
│   ├── .NET 8 + ASP.NET Core Web API
│   ├── EF Core 8.0 + SQL Server  
│   ├── JWT认证 + RBAC权限
│   └── 13个API控制器模块
├── 前端 WPF客户端 (UltraThink双层架构)
│   ├── WPF + .NET 8
│   ├── Prism.DryIoc 9.0.537 (模块化)
│   ├── Refit (类型安全HTTP客户端)
│   └── 8个业务模块 + 统一Shell
└── 共享库
    ├── Models (数据传输对象)
    ├── Interfaces (服务接口)
    └── Utilities (工具库)
```

### 📊 项目规模统计
- **总计48个项目组件**
- **代码量**: ~50,000行代码 (精简后)
- **编译状态**: 前后端零警告零错误
- **测试覆盖**: Repository层97个测试用例
- **部署方式**: IIS/Docker/K8s多选择

---

## 🛠️ 开发环境配置 (20分钟)

### 第一步：必备软件安装

#### 1.1 开发工具
```bash
# 必装 (Windows 10/11)
1. Visual Studio 2022 Community+ (17.8+) 
   - 工作负载：ASP.NET和Web开发 + .NET桌面开发
   - 组件：.NET 8 SDK + Entity Framework tools

2. SQL Server 2019+ 或 SQL Server Express (免费)
   - 建议：SQL Server Developer Edition (免费，完整功能)

3. Git (最新版本)
   - 推荐：Git for Windows + Git Extensions GUI

# 推荐 (提升效率)
4. Visual Studio Code (轻量级编辑)
5. Postman (API测试)
6. SQL Server Management Studio (数据库管理)
7. NuGet Package Manager (最新版)
```

#### 1.2 环境验证
```batch
# 执行验证命令确保环境正确
dotnet --version          # 应显示8.0.x
git --version             # 任何版本
sqlcmd -?                 # 应显示帮助信息 (SQL Server已安装)
```

### 第二步：项目获取和配置

#### 2.1 克隆项目
```bash
# 克隆仓库
git clone https://github.com/your-org/LYBTZYZS.git
cd LYBTZYZS

# 检查分支
git status
git log --oneline -5      # 查看最近提交
```

#### 2.2 依赖安装
```batch
# 在项目根目录执行
# 方法一：使用提供的脚本 (推荐)
scripts\setup-dev-environment.bat

# 方法二：手动安装依赖
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln --configuration Debug
```

#### 2.3 数据库初始化
```batch
# 修改连接字符串 (在appsettings.Development.json中)
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=LYBTDB_Dev;Integrated Security=true;TrustServerCertificate=true;"
  }
}

# 执行数据库迁移
cd src\Server\Core\LYBT.Infrastructure
dotnet ef database update --startup-project ..\..\Services\LYBT.WebAPI
```

#### 2.4 验证配置
```batch
# 构建所有项目
dotnet build LYBT.All.sln

# 预期结果：✅ 48个项目零编译错误零警告
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

---

## 🚀 第一次运行 (5分钟)

### 启动后端API
```batch
# 方法一：使用Visual Studio (推荐)
1. 打开 LYBT.Server.sln
2. 设置 LYBT.WebAPI 为启动项目
3. F5 运行 (Debug模式)
4. 浏览器自动打开 https://localhost:7001/swagger

# 方法二：命令行启动  
cd src\Server\Services\LYBT.WebAPI
dotnet run
```

### 启动前端WPF
```batch
# 方法一：使用Visual Studio (推荐)
1. 打开 LYBT.Desktop.sln (新窗口)
2. 设置 LYBT.Desktop.Shell 为启动项目
3. F5 运行 (Debug模式)
4. 登录界面自动弹出

# 方法二：命令行启动
cd src\Client\Desktop\Shell
dotnet run
```

### 验证系统可用性
```bash
# 1. API健康检查
curl https://localhost:7001/api/v1/health

# 2. 登录系统
用户名: sysadmin
密码: Admin@123456

# 3. 浏览所有模块
验证8个业务模块是否可正常打开
```

---

## 🧭 项目结构详解

### 项目组织架构
```
LYBTZYZS/ (根目录)
├── 📁 src/                          # 源代码 (44个项目)
│   ├── 📁 Server/                   # 后端 (16个项目)
│   │   ├── 📁 Core/                 # 核心基础设施 (2个)
│   │   │   ├── LYBT.Infrastructure  # ⭐ 数据层统一 (EF Core + 迁移)
│   │   │   └── LYBT.Entities        # 实体模型定义
│   │   ├── 📁 Modules/              # 业务模块 (13个)
│   │   │   ├── LYBT.Module.Auth     # 认证授权模块
│   │   │   ├── LYBT.Module.Users    # 用户管理模块
│   │   │   ├── LYBT.Module.Patients # 患者管理模块
│   │   │   ├── LYBT.Module.MedicalCase # 医案管理模块
│   │   │   ├── LYBT.Module.Consultation # 诊断记录模块
│   │   │   ├── LYBT.Module.Prescriptions # 处方管理模块
│   │   │   ├── LYBT.Module.Herbs    # 中药材管理模块
│   │   │   ├── LYBT.Module.Formula  # 验方管理模块
│   │   │   ├── LYBT.Module.Health   # 健康检查模块
│   │   │   ├── LYBT.Module.Monitoring # 系统监控模块
│   │   │   ├── LYBT.Module.Cache    # 缓存管理模块
│   │   │   ├── LYBT.Module.Security # 安全管理模块
│   │   │   └── LYBT.Module.Performance # 性能监控模块
│   │   └── 📁 Services/
│   │       └── LYBT.WebAPI          # ⭐ Web API 入口项目
│   ├── 📁 Client/Desktop/           # WPF前端 (24个项目)
│   │   ├── 📁 Core/                 # 前端核心 (2个)
│   │   │   ├── LYBT.Desktop.Core    # 核心基础设施
│   │   │   └── LYBT.Desktop.Infrastructure # 前端基础设施
│   │   ├── 📁 Modules/              # 前端业务模块 (9个)
│   │   │   ├── Auth/                # 🔐 认证模块 (UltraThink双层)
│   │   │   ├── Users/               # 👥 用户管理 (UltraThink双层)
│   │   │   ├── Patients/            # 🏥 患者管理 (UltraThink双层)
│   │   │   ├── MedicalCase/         # 📋 医案管理 (UltraThink双层)
│   │   │   ├── Consultation/        # 🩺 诊断管理 (UltraThink双层)
│   │   │   ├── Prescriptions/       # 💊 处方管理 (UltraThink双层)
│   │   │   ├── Herbs/               # 🌿 中药材管理 (UltraThink双层)
│   │   │   ├── Formula/             # 📜 验方管理 (UltraThink双层)
│   │   │   └── Users/               # 👤 用户设置 (UltraThink双层)
│   │   ├── 📁 Services/             # 前端服务层 (1个)
│   │   ├── 📁 Shell/                # ⭐ 主程序外壳 (1个)
│   │   └── 📁 Workbenches/          # 工作台 (11个)
│   │       ├── Core/                # 工作台核心
│   │       ├── ConsultationWorkbench/ # 诊疗工作台
│   │       └── CashierWorkbench/    # 收费工作台
│   └── 📁 Shared/                   # 共享库 (4个项目)
│       ├── LYBT.Shared.Models       # ⭐ 数据传输对象 (DTO)
│       ├── LYBT.Shared.Interfaces   # 服务接口定义
│       ├── LYBT.Shared.Utilities    # 工具类库
│       └── LYBT.Shared.Common       # 通用组件
├── 📁 tests/                        # 测试项目 (4个)
│   ├── Unit/                        # 单元测试
│   ├── Integration/                 # 集成测试
│   ├── API/                         # API测试脚本
│   └── Performance/                 # 性能测试
├── 📁 docs/                         # 文档系统
├── 📁 scripts/                      # 开发脚本
└── 📁 tools/                        # 开发工具
```

### 🔑 关键项目说明

#### ⭐ 核心入口项目 (必须了解)
1. **LYBT.WebAPI** - 后端API总入口
   - 配置所有模块服务注册
   - JWT认证和授权配置
   - Swagger API文档生成
   - 健康检查和监控端点

2. **LYBT.Desktop.Shell** - WPF前端主程序
   - Prism容器配置和模块加载
   - 主窗口和导航管理
   - 全局异常处理和日志
   - 用户认证状态管理

3. **LYBT.Infrastructure** - 统一数据层
   - AppDbContext (EF Core上下文)
   - 数据库迁移管理
   - Repository基础实现
   - 连接字符串配置

#### 🏗️ 架构模式说明

**后端传统三层架构** (稳定成熟):
```csharp
Controller层 (API端点)
    ↓
Service层 (业务逻辑)
    ↓  
Repository层 (数据访问)
    ↓
AppDbContext (EF Core)
```

**前端UltraThink双层架构** (现代化):
```csharp
Module层 (纯委托入口)
    ├── QueryService (查询专业层)
    └── BusinessService (业务逻辑层)
```

---

## 💻 开发工作流程

### 日常开发流程

#### 1. 开始新功能开发
```bash
# 1. 同步最新代码
git pull origin master
git status

# 2. 创建功能分支
git checkout -b feature/新功能名称
git push -u origin feature/新功能名称

# 3. 开发环境验证
dotnet build LYBT.All.sln
# 确保编译通过后开始开发
```

#### 2. 代码编写规范

**后端API开发** (传统三层架构):
```csharp
// 1. 定义DTO (在Shared.Models中)
public class CreatePatientDto
{
    public string Name { get; set; }
    public string Phone { get; set; }
    // ...
}

// 2. 实现Service业务逻辑
public class PatientService : IPatientService
{
    public async Task<ServiceResult<Patient>> CreateAsync(CreatePatientDto dto)
    {
        // 业务逻辑实现
    }
}

// 3. 实现Controller API端点
[ApiController]
[Route("api/v1/[controller]")]
public class PatientsController : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Create(CreatePatientDto dto)
    {
        var result = await _patientService.CreateAsync(dto);
        return HandleServiceResult(result, "患者创建成功");
    }
}
```

**前端WPF开发** (UltraThink双层架构):
```csharp
// 1. 定义QueryService (查询专业层)
public class PatientQueryService : IPatientQueryService
{
    public async Task<ServiceResult<PagedResult<PatientDto>>> SearchAsync(PatientSearchDto criteria)
    {
        // 复杂查询实现
    }
}

// 2. 定义BusinessService (业务逻辑层)
public class PatientBusinessService : IPatientBusinessService
{
    public async Task<ServiceResult<PatientDto>> CreatePatientAsync(CreatePatientDto dto)
    {
        // CRUD操作和业务流程
    }
}

// 3. 定义Module (纯委托层)
public class PatientModule : IPatientModule
{
    public async Task<ServiceResult<PatientDto>> CreatePatientAsync(CreatePatientDto dto)
        => await _businessService.CreatePatientAsync(dto);
        
    public async Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto criteria)
        => await _queryService.SearchAsync(criteria);
}
```

#### 3. 测试和验证
```bash
# 1. 运行单元测试
dotnet test tests/Unit/

# 2. 手动功能测试
# 后端：使用Swagger界面测试新API
# 前端：运行WPF程序测试新界面

# 3. 集成测试
dotnet test tests/Integration/
```

#### 4. 提交和部署
```bash
# 1. 代码提交
git add .
git commit -m "feat: 添加患者快速搜索功能"

# 2. 推送分支
git push origin feature/新功能名称

# 3. 创建Pull Request
# 使用GitHub/GitLab界面创建PR，等待代码评审

# 4. 合并主分支后部署
git checkout master
git pull origin master
scripts\deploy-to-staging.bat  # 部署到测试环境
```

---

## 🎯 核心业务概念

### 中医诊疗流程
```
患者接待 → 医案创建 → 四诊记录 → 辨证论治 → 处方开具 → 案例完成
    ↓         ↓         ↓         ↓         ↓         ↓
 Patients → MedicalCase → Consultation → Diagnosis → Prescriptions → Completed
```

### 核心实体关系
```csharp
// 核心业务关系 (1对多关系)
Patient (患者)
    ├── MedicalCase (医案) [1:N]
    │   └── Consultation (诊断) [1:1] 
    │       └── Prescriptions (处方) [1:N]
    │           └── PrescriptionItems (处方项) [1:N]
    │               └── Herb (中药材) [N:1]
    └── Formula (验方模板) [N:N]

// 用户权限关系
User (用户)
    ├── Role: Admin (系统管理员)
    │   └── 权限: 所有模块全部操作
    └── Role: Doctor (医生)
        └── 权限: 诊疗相关模块操作
```

### 8个业务模块功能
| 模块 | 功能 | 前端架构 | 后端架构 |
|------|------|----------|----------|
| **Auth** | 身份认证、会话管理 | UltraThink双层 | 传统三层 |
| **Users** | 用户管理、角色权限 | UltraThink双层 | 传统三层 |
| **Patients** | 患者档案、联系方式 | UltraThink双层 | 传统三层 |
| **MedicalCase** | 医案管理、流程控制 | UltraThink双层 | 传统三层 |
| **Consultation** | 四诊记录、辨证论治 | UltraThink双层 | 传统三层 |
| **Prescriptions** | 处方开具、药材配伍 | UltraThink双层 | 传统三层 |
| **Herbs** | 中药材管理、价格维护 | UltraThink双层 | 传统三层 |
| **Formula** | 验方模板、经典处方 | UltraThink双层 | 传统三层 |

---

## 🔧 开发工具和技巧

### Visual Studio配置优化

#### 必装扩展
```
1. Productivity Power Tools (微软官方)
2. CodeMaid (代码清理和格式化)  
3. Roslynator (代码分析和重构)
4. Entity Framework Power Tools (EF Core辅助)
5. Web Essentials (Web开发增强)
6. NuGet Package Manager (包管理)
```

#### 调试配置
```json
// launchSettings.json - API项目调试配置
{
  "profiles": {
    "LYBT.WebAPI": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

#### 代码片段 (Code Snippets)
```csharp
// 快速创建ServiceResult返回方法
public async Task<ServiceResult<$TYPE$>> $METHOD$Async($PARAMS$)
{
    try
    {
        $IMPLEMENTATION$
        return ServiceResult<$TYPE$>.Success(result, "$SUCCESS_MESSAGE$");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "$ERROR_MESSAGE$");
        return ServiceResult<$TYPE$>.Failure($"$ERROR_MESSAGE$: {ex.Message}");
    }
}
```

### 数据库开发工具

#### EF Core命令速查
```bash
# 添加迁移 (必须在Infrastructure项目目录下执行)
cd src\Server\Core\LYBT.Infrastructure
dotnet ef migrations add 迁移名称 --startup-project ..\..\Services\LYBT.WebAPI

# 更新数据库
dotnet ef database update --startup-project ..\..\Services\LYBT.WebAPI

# 回滚到指定迁移
dotnet ef database update 之前的迁移名称 --startup-project ..\..\Services\LYBT.WebAPI

# 删除最后一个迁移
dotnet ef migrations remove --startup-project ..\..\Services\LYBT.WebAPI

# 生成SQL脚本
dotnet ef migrations script --startup-project ..\..\Services\LYBT.WebAPI -o migration-script.sql
```

#### 数据库管理脚本
```sql
-- 快速查看表结构和数据
SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    c.max_length,
    c.is_nullable,
    ty.name AS DataType
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.system_type_id = ty.system_type_id
WHERE t.name IN ('Users', 'Patients', 'MedicalCases', 'Consultations')
ORDER BY t.name, c.column_id;

-- 性能监控查询
SELECT 
    DB_NAME() as DatabaseName,
    COUNT(*) as ConnectionCount,
    AVG(CAST(wait_time_ms AS FLOAT)) as AvgWaitTime
FROM sys.dm_exec_requests 
WHERE database_id = DB_ID();
```

### API开发和测试

#### Postman集合配置
```json
{
  "info": {
    "name": "LYBTZYZS API Collection",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://localhost:7001"
    },
    {
      "key": "token",
      "value": "{{jwt_token}}"
    }
  ],
  "auth": {
    "type": "bearer",
    "bearer": [
      {
        "key": "token",
        "value": "{{token}}"
      }
    ]
  }
}
```

#### curl命令快速测试
```bash
# 登录获取token
curl -X POST "https://localhost:7001/api/v1/auth/login" ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"sysadmin\",\"password\":\"Admin@123456\"}" ^
  -k

# 使用token访问受保护API  
curl -X GET "https://localhost:7001/api/v1/users" ^
  -H "Authorization: Bearer YOUR_TOKEN_HERE" ^
  -k

# 健康检查
curl -X GET "https://localhost:7001/api/v1/health" -k
```

---

## 📚 学习资源和最佳实践

### 必读文档 (优先级排序)

#### 🔥 高优先级 (必须掌握)
1. **[CLAUDE.md](../../CLAUDE.md)** - 项目开发指南和约束
2. **[混合架构设计详解](../../CLAUDE.md#🏗️-混合架构设计详解)** - 前后端架构理解
3. **[后端API总览](../api/backend-api-overview.md)** - API接口规范
4. **[部署指南](../deployment/deployment-guide.md)** - 部署和运维

#### ⚡ 中优先级 (建议阅读)  
1. **[UltraThink双层架构重构报告](../ultrathink/ultrathink-backend-modules-refactoring-complete-20250831.md)** - 前端架构深度理解
2. **[API响应标准](../api/api-standards.md)** - API设计规范
3. **[文件组织规范](../development/FILE_ORGANIZATION.md)** - 项目文件管理

#### 📖 低优先级 (深入学习)
1. **[故障排除指南](../troubleshooting/)** - 问题诊断和解决
2. **[性能优化指南](../optimization/)** - 系统调优
3. **[测试策略文档](../testing/)** - 测试编写规范

### 在线学习资源

#### .NET技术栈
- **[.NET 8官方文档](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)** - .NET 8新特性
- **[EF Core文档](https://docs.microsoft.com/en-us/ef/core/)** - Entity Framework Core
- **[ASP.NET Core文档](https://docs.microsoft.com/en-us/aspnet/core/)** - Web API开发

#### WPF前端技术
- **[Prism官方文档](https://prismlibrary.github.io/docs/)** - MVVM和模块化
- **[WPF官方文档](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)** - WPF基础
- **[MaterialDesignInXAML](https://materialdesigninxaml.net/)** - 现代UI设计

#### 架构设计模式
- **[领域驱动设计](https://martinfowler.com/bliki/DomainDrivenDesign.html)** - DDD概念
- **[Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)** - 整洁架构
- **[微服务模式](https://microservices.io/)** - 微服务设计 (可选学习)

---

## 🤝 团队协作规范

### Git工作流程

#### 分支策略 (Git Flow简化版)
```
master (主分支)
    ├── develop (开发分支)
    ├── feature/功能名称 (功能分支)
    ├── hotfix/紧急修复 (热修复分支)
    └── release/版本号 (发布分支)
```

#### 提交消息规范
```bash
# 格式: <type>(<scope>): <subject>
# 
# type: feat|fix|docs|style|refactor|test|chore
# scope: 模块名 (Auth|Users|Patients|MedicalCase等)
# subject: 简洁描述 (50字符内)

# 示例
feat(Patients): 添加患者快速搜索功能
fix(Auth): 修复JWT令牌过期检查逻辑  
docs(README): 更新API文档链接
refactor(Users): 重构用户服务层架构
test(MedicalCase): 添加医案创建单元测试
chore(build): 更新NuGet包版本
```

#### Pull Request规范
```markdown
## 🎯 功能概述
简要描述本次PR的主要功能或修复内容

## 📝 变更内容
- [ ] 新增功能：具体功能描述
- [ ] Bug修复：具体问题和解决方案
- [ ] 文档更新：更新的文档内容
- [ ] 测试覆盖：新增的测试用例

## 🧪 测试方案
- [ ] 单元测试通过
- [ ] 集成测试通过
- [ ] 手动测试验证
- [ ] API接口测试

## 📸 截图或演示
(如果涉及UI变更，请提供截图)

## 🔗 相关Issue
Closes #123, Related to #456

## ✅ 检查清单
- [ ] 代码遵循项目规范
- [ ] 编译无警告无错误
- [ ] 已添加/更新相关文档
- [ ] 已添加/更新测试用例
```

### 代码审查 (Code Review)

#### 审查重点 (按优先级)
1. **🔒 安全性审查**
   - SQL注入风险检查
   - JWT令牌处理安全性
   - 用户输入验证完整性
   - 敏感信息泄露防护

2. **🏗️ 架构一致性**
   - 前端是否遵循UltraThink双层架构
   - 后端是否遵循传统三层架构
   - ServiceResult<T>统一返回格式
   - 依赖注入正确使用

3. **💻 代码质量**
   - 命名规范 (PascalCase/camelCase)
   - 异常处理完整性
   - 日志记录适当性
   - 单元测试覆盖率

4. **📊 性能考虑**
   - 数据库查询优化
   - 异步编程正确使用
   - 内存缓存合理利用
   - API响应时间优化

#### 审查检查清单
```markdown
## 🔍 代码审查检查清单

### 架构和设计
- [ ] 遵循项目既定架构模式
- [ ] 服务层职责分离清晰
- [ ] 接口设计合理，易于测试
- [ ] 异常处理策略正确

### 代码质量
- [ ] 命名清晰，表达意图明确
- [ ] 方法长度适中 (<50行)
- [ ] 避免代码重复
- [ ] 注释恰当，解释复杂逻辑

### 安全性
- [ ] 输入验证充分
- [ ] 权限检查正确
- [ ] 敏感信息处理安全
- [ ] SQL注入防护到位

### 性能
- [ ] 数据库查询优化
- [ ] 合理使用缓存
- [ ] 避免N+1查询问题
- [ ] 异步操作正确使用

### 测试
- [ ] 关键业务逻辑有单元测试覆盖
- [ ] 测试用例覆盖正常和异常流程
- [ ] Mock对象使用恰当
- [ ] 测试数据隔离性好
```

### 团队沟通

#### 日常沟通渠道
- **技术讨论**: 项目Wiki + GitHub Discussions
- **问题报告**: GitHub Issues
- **紧急联系**: 团队IM群组
- **代码评审**: GitHub Pull Requests评论

#### 会议节奏
- **每日站会**: 15分钟同步进展和阻塞
- **每周技术分享**: 1小时新技术学习和经验分享
- **双周Sprint评审**: 功能演示和用户反馈
- **月度技术债务清理**: 重构和代码质量提升

---

## 🐛 常见问题和解决方案

### 开发环境问题

#### Q1: 编译错误 "找不到类型或命名空间"
```bash
# 解决方案
1. 清理解决方案
dotnet clean LYBT.All.sln

2. 恢复NuGet包
dotnet restore LYBT.All.sln

3. 重新构建
dotnet build LYBT.All.sln

4. 检查项目引用
# 确保所有ProjectReference正确配置
```

#### Q2: 数据库连接失败
```csharp
// 检查连接字符串 (appsettings.Development.json)
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=LYBTDB_Dev;Integrated Security=true;TrustServerCertificate=true;"
  }
}

// 验证SQL Server服务运行状态
services.msc → SQL Server (SQLEXPRESS) 确保"正在运行"

// 测试连接
sqlcmd -S .\SQLEXPRESS -E -Q "SELECT @@VERSION"
```

#### Q3: JWT认证失败
```json
// 检查JWT配置 (appsettings.Development.json)
{
  "Jwt": {
    "SecretKey": "开发环境至少32位字符的密钥",
    "Issuer": "LYBT-Development", 
    "Audience": "LYBT-Clients",
    "ExpirationHours": 8
  }
}

// 验证默认登录账户
用户名: sysadmin
密码: Admin@123456
```

### 业务逻辑问题

#### Q4: 前端模块加载失败
```csharp
// 检查模块注册 (App.xaml.cs或Shell项目中)
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 确保所有模块都已注册
    moduleCatalog.AddModule<AuthModule>();
    moduleCatalog.AddModule<UsersModule>();
    moduleCatalog.AddModule<PatientsModule>();
    // ... 其他8个模块
}

// 检查依赖注入配置
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 确保所有服务都已注册
    containerRegistry.RegisterSingleton<IAuthModule, AuthModule>();
    // ... 其他服务注册
}
```

#### Q5: API调用返回500错误
```bash
# 查看详细错误信息
1. 检查API项目控制台输出
2. 查看logs目录下的日志文件
3. 使用Swagger界面查看错误详情

# 常见原因和解决方案
- 数据库连接问题：检查连接字符串
- 服务注入失败：检查Startup.cs中服务注册
- 验证失败：检查DTO验证属性
- 业务逻辑异常：添加try-catch并记录日志
```

### 性能问题

#### Q6: API响应缓慢 (>5秒)
```csharp
// 分析步骤
1. 启用详细日志记录
"Logging": {
  "LogLevel": {
    "LYBT": "Debug",
    "Microsoft.EntityFrameworkCore": "Information"
  }
}

2. 检查数据库查询
// 避免N+1查询，使用Include预加载
var patients = await _context.Patients
    .Include(p => p.MedicalCases)
    .ThenInclude(mc => mc.Consultations)
    .ToListAsync();

3. 启用查询缓存
// 在Service中使用MemoryCache
var cacheKey = $"patients_page_{pageIndex}_{pageSize}";
if (!_cache.TryGetValue(cacheKey, out var result))
{
    result = await QueryPatientsFromDatabase();
    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
}
```

#### Q7: 前端界面卡顿
```csharp
// WPF性能优化
1. 使用异步加载数据
public async Task LoadPatientsAsync()
{
    IsLoading = true;
    try
    {
        var result = await _patientModule.GetPatientsAsync();
        Patients = new ObservableCollection<PatientDto>(result.Data);
    }
    finally
    {
        IsLoading = false;
    }
}

2. 启用UI虚拟化
<DataGrid VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          ItemsSource="{Binding Patients}" />

3. 分页加载大量数据
// 不要一次性加载超过1000条记录
var pageSize = 50;
var patients = await _patientModule.GetPagedPatientsAsync(pageIndex, pageSize);
```

---

## 🎓 进阶学习路径

### 新手阶段 (1-2周)
- [ ] 完成开发环境配置
- [ ] 运行项目并熟悉基本功能
- [ ] 阅读核心文档 (CLAUDE.md + API文档)
- [ ] 完成第一个简单功能开发 (如添加字段)

### 熟练阶段 (1-2个月)
- [ ] 深入理解混合架构设计
- [ ] 掌握前端UltraThink双层架构模式
- [ ] 掌握后端传统三层架构模式
- [ ] 能独立开发完整的CRUD功能
- [ ] 编写相应的单元测试

### 专家阶段 (3-6个月)
- [ ] 能够进行架构优化和重构
- [ ] 性能调优和问题诊断
- [ ] 指导新团队成员
- [ ] 参与技术方案决策
- [ ] 贡献开源组件或工具

### 学习建议

#### 技术深度学习路径
```
基础技能
├── C# 12 + .NET 8 新特性
├── Entity Framework Core 高级特性
├── ASP.NET Core Web API 最佳实践
└── WPF + Prism MVVM 模式

架构技能
├── 领域驱动设计 (DDD)
├── 微服务架构模式 (可选)
├── 事件驱动架构 (可选)  
└── 测试驱动开发 (TDD)

运维技能
├── Docker 容器化技术
├── CI/CD 持续集成部署
├── 监控和日志分析
└── 数据库性能优化
```

#### 实践项目建议
1. **个人练习**: 基于现有模块，扩展一个小功能
2. **团队协作**: 参与代码评审和技术讨论
3. **开源贡献**: 为项目文档或工具类库做贡献
4. **技术分享**: 在团队会议上分享学到的新技术

---

## 📞 获取帮助

### 内部支持渠道
- **技术文档**: 项目docs目录完整文档体系
- **代码示例**: 现有模块代码作为最佳实践参考
- **团队导师**: 安排经验丰富的开发者作为导师
- **定期答疑**: 每周技术答疑时间

### 外部学习资源
- **官方文档**: Microsoft Docs (.NET/EF Core/ASP.NET Core)
- **社区论坛**: Stack Overflow, Reddit r/dotnet
- **视频教程**: Microsoft Learn, Pluralsight, Udemy
- **技术博客**: Scott Hanselman, Jon Skeet, Martin Fowler

### 紧急问题处理
1. **编译错误**: 参考常见问题解决方案
2. **运行时异常**: 查看日志文件和调试信息
3. **业务逻辑问题**: 咨询业务专家或产品经理
4. **技术难题**: 在团队技术群或GitHub Issues提问

---

## 🎉 欢迎加入LYBTZYZS团队！

恭喜您完成开发者上手指南的学习！您现在已经具备了：

✅ **环境配置能力**: 能够独立配置开发环境并运行项目  
✅ **架构理解能力**: 理解混合架构设计和各模块职责  
✅ **开发流程掌握**: 掌握日常开发、测试、提交的完整流程  
✅ **问题解决能力**: 具备常见问题的分析和解决能力  

### 下一步行动计划
1. **选择一个简单任务开始** - 建议从文档更新或小功能添加开始
2. **寻找代码导师** - 与资深团队成员建立联系
3. **参与代码评审** - 积极参与团队的代码评审过程
4. **持续学习提升** - 按照进阶学习路径持续提升技能

### 团队文化价值观
- **代码质量优先** - 宁可慢一点，也要保证代码质量
- **协作胜于竞争** - 团队成功才是真正的成功
- **用户价值导向** - 所有技术决策都要考虑用户价值
- **持续学习改进** - 保持好奇心，不断学习新技术

**欢迎来到LYBTZYZS开发团队，让我们一起构建出色的中医诊疗系统！** 🏥✨

---

**文档维护**: 本文档会随着项目发展持续更新，如发现内容过期或有改进建议，请提交GitHub Issue。

**最后更新**: 2025-09-02 | **文档版本**: v1.0 | **适用项目版本**: LYBTZYZS v2.0+