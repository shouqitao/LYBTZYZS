# LYBTZYZS 开发者指导手册

**文档版本**：v1.0  
**创建时间**：2025-09-25  
**维护负责**：Claude Code  
**关联文档**：[CLAUDE.md](../CLAUDE.md), [文档系统](DOCUMENTATION_SYSTEM.md)

## 🚀 快速开始

### 新开发者必读清单

**5分钟了解项目**：
1. ✅ 阅读 [项目README](../README.md) - 了解系统概览和技术架构
2. ✅ 查看 [当前状态速览](../README.md#当前状态概览2025-09-24) - 掌握项目现状
3. ✅ 阅读 [CLAUDE.md](../CLAUDE.md) - 掌握开发约束和规范

**15分钟环境准备**：
4. ✅ 安装 .NET 8 SDK + Visual Studio/VS Code
5. ✅ 配置 SQL Server 本地实例
6. ✅ 克隆代码并执行初次构建验证

**30分钟深入了解**：
7. ✅ 浏览 [架构文档](architecture/README.md) - 理解系统设计
8. ✅ 查看 [API文档](api/README.md) - 熟悉接口规范  
9. ✅ 阅读 [开发规范](development/README.md) - 掌握编码标准

### 关键约束提醒 ⚠️

🚫 **明确禁止**：
- **不得引入CQRS和MediatR**：项目明确决策不采用这些模式
- **不得绕过ReadRepository**：读操作必须走`Controller → QueryService → ReadRepository`路径
- **不得在ViewModel中直接访问容器**：必须通过构造函数注入接口

✅ **必须遵循**：
- **中文优先**：代码注释、提交信息、终端输出均使用中文
- **异步优先**：所有I/O操作必须使用async/await
- **接口注入**：采用构造函数注入，禁用ServiceLocator模式

## 📁 项目结构导览

### 解决方案架构
```
LYBTZYZS/
├── LYBT.All.sln          # 完整解决方案（28个项目）
├── LYBT.Server.sln       # 后端专用（10个项目）
├── LYBT.Desktop.sln      # 前端专用（15个项目）
├── README.md             # 项目总览
├── CLAUDE.md             # Claude Code开发规范
└── docs/                 # 文档中心
    ├── DEVELOPER_GUIDE.md    # 👈 当前文档
    ├── DOCUMENTATION_SYSTEM.md # 文档系统规范
    ├── api/              # API接口文档
    ├── architecture/     # 架构设计文档
    ├── development/      # 开发规范集合
    ├── prd/             # 产品需求文档
    ├── tasks/           # 任务管理
    └── reports/         # 分析报告
```

### 代码结构概览
```
src/
├── Server/              # ASP.NET Core Web API
│   ├── Core/           # 核心基础设施（Entities + Infrastructure）
│   ├── Modules/        # 8个业务模块（Auth, Users, Patients等）
│   └── Services/       # API服务层（LYBT.WebAPI）
├── Client/Desktop/     # WPF Prism客户端
│   ├── Shell/         # 应用程序壳
│   ├── Core/          # 核心基础设施
│   ├── Services/      # 业务服务和API客户端
│   ├── Workbenches/   # 工作台系统
│   └── Modules/       # 8个业务模块UI
└── Shared/            # 前后端共享
    ├── Models/        # DTO模型
    ├── Interfaces/    # API接口定义
    └── Utilities/     # 通用工具
```

## ⚙️ 开发环境设置

### 环境要求
- **.NET SDK**：8.0 或更高版本
- **IDE**：Visual Studio 2022 17.8+ 或 VS Code with C# extension
- **数据库**：SQL Server 2019+ 或 LocalDB
- **Node.js**：16+ （用于前端工具链，如需要）

### 数据库配置
```sql
-- 创建开发数据库
CREATE DATABASE LYBTDB;

-- 默认连接字符串（appsettings.Development.json）
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=true;MultipleActiveResultSets=true"
}

-- 默认管理员账号
用户名: sysadmin
密码: LybtAdmin2025@SecurePass!
```

### 一键构建脚本
```powershell
# 完整构建流程
dotnet restore LYBT.All.sln
dotnet build LYBT.Server.sln -c Release --no-restore

# ⚠️ 桌面端当前编译失败，待修复
# dotnet build LYBT.Desktop.sln -c Release --no-restore

# 启动API服务
dotnet run --project src/Server/Services/LYBT.WebAPI

# 运行服务端测试（当前部分失败）
dotnet test LYBT.Server.sln -c Release
```

## 📚 核心技术指南

### 后端开发路径

**控制器层**（`src/Server/Services/LYBT.WebAPI/Controllers/`）：
- 负责HTTP请求处理和响应格式化
- 参考：[API文档](api/README.md)

**业务服务层**（`src/Server/Modules/*/Services/`）：
- **QueryService**：只读查询，走ReadRepository
- **BusinessService**：写操作和业务逻辑
- 参考：[服务层架构指南](development/README.md#服务层规范)

**数据访问层**（`src/Server/Core/LYBT.Infrastructure/`）：
- 统一DbContext和ReadRepository模式
- 自动审计字段、软删除、缓存策略
- 参考：[数据访问规范](development/audit-field-automation-solution.md)

### 前端开发路径

**MVVM架构**（WPF + Prism.DryIoc）：
- **Views**：XAML用户界面
- **ViewModels**：视图逻辑，通过接口注入服务
- **Models**：数据模型，使用Shared.Models中的DTO
- 参考：[Desktop开发指南](development/Desktop-UltraThink-Refactoring-2025.md)

**API通信**（Refit HTTP客户端）：
- 8个强类型API接口（IAuthApi, IUserApi等）
- 统一错误处理和JWT认证
- 参考：[API接口文档](api/README.md#核心接口列表)

## 🔧 常用开发任务

### 添加新的API端点
1. 在对应的Controller中添加Action
2. 更新Shared.Interfaces中的API接口定义
3. 在前端通过注入的API客户端调用
4. 更新API文档和Swagger注释

### 添加新的业务实体
1. 在Core/Entities中定义实体类
2. 在Shared/Models中定义对应DTO
3. 配置AutoMapper映射关系
4. 在Infrastructure中添加DbSet
5. 生成数据库迁移

### 修复编译错误
当前已知的主要问题：
- **Desktop端**：事件重复定义，需统一到UnifiedEvents.cs
- **测试失败**：AutoMapper配置和API契约不匹配

参考：[当前优先级任务](../README.md#当前优先级thinker)

## 📋 任务和工作流程

### 任务管理系统
- **任务发布**：Thinker在`docs/tasks/pending/`发布任务
- **任务认领**：开发者查看pending目录选择任务
- **进度跟踪**：使用TodoWrite工具跟踪进度
- **完成报告**：在`docs/tasks/completed/`提交总结

### 代码提交规范
```bash
# Git提交信息格式
<类型>(范围): <主题>

# 示例
feat(user): 添加用户密码重置功能
fix(api): 修复患者查询接口空值异常
docs(readme): 更新API文档链接
```

**提交类型**：
- `feat`：新功能
- `fix`：缺陷修复  
- `refactor`：重构代码
- `docs`：文档更新
- `test`：测试相关
- `chore`：构建/工具变更

### 代码审查清单
- [ ] 遵循命名约定（PascalCase类型，camelCase参数，_camelCase私有字段）
- [ ] 异步方法使用Async后缀
- [ ] 添加XML文档注释
- [ ] 通过单元测试验证
- [ ] 更新相关文档

## 🛠 调试和故障排除

### 常见问题解决方案

**编译失败**：
```powershell
# 清理构建缓存
dotnet clean
dotnet restore
dotnet build

# 检查NuGet包版本冲突
dotnet list package --outdated
```

**数据库连接问题**：
```powershell
# 检查SQL Server状态
services.msc # 查找SQL Server服务

# 测试连接字符串
sqlcmd -S localhost -E -Q "SELECT @@VERSION"
```

**API调用失败**：
- 检查JWT Token有效性
- 验证API基础地址配置
- 查看Swagger文档确认接口签名

### 调试工具推荐
- **API测试**：Postman + Swagger UI
- **数据库**：SQL Server Management Studio
- **日志查看**：Serilog + 控制台输出
- **性能分析**：dotnet-counters, Application Insights

## 📖 进阶学习资源

### 内部文档深入阅读
- [架构决策记录](architecture/) - 理解设计决策背景
- [性能优化报告](reports/) - 学习系统优化实践
- [测试策略指南](development/testing-best-practices.md) - 掌握测试方法

### 技术栈官方文档
- [.NET 8 文档](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [WPF应用开发](https://docs.microsoft.com/dotnet/desktop/wpf/)
- [Prism Library](https://prismlibrary.com/)

## 🆘 获取帮助

### 内部支持
- **架构问题**：查阅architecture/目录或提交architecture review任务
- **API问题**：参考api/README.md或查看Swagger文档
- **开发规范**：查询development/目录相关文档

### 问题报告流程
1. 搜索existing issues和文档
2. 准备最小可复现示例
3. 在docs/tasks/pending/创建问题描述任务
4. 包含环境信息、错误日志、期望行为

---

## 🔗 快速导航

| 目标 | 文档路径 |
|------|----------|
| 项目概览 | [../README.md](../README.md) |
| 开发约束 | [../CLAUDE.md](../CLAUDE.md) |
| API参考 | [api/README.md](api/README.md) |
| 架构设计 | [architecture/README.md](architecture/README.md) |
| 开发规范 | [development/README.md](development/README.md) |
| 任务管理 | [tasks/README.md](tasks/README.md) |
| 文档系统 | [DOCUMENTATION_SYSTEM.md](DOCUMENTATION_SYSTEM.md) |

---

**欢迎贡献！** 如发现文档有误或需要补充，请按照任务流程提交改进建议。