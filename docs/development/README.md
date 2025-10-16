# 开发指南总览

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-15  
**维护团队**：开发组  

## 🎯 开发指南导航

凌隐宝堂中医诊所管理系统采用**三层对齐架构**，开发指南严格按照Server/Client/Shared三层结构组织，确保开发规范与代码架构完全一致。

### 📋 开发指南结构

| 层级 | 开发指南 | 主要内容 | 目标用户 |
|------|----------|----------|----------|
| **Level 1** | **[开发指南总览](README.md)** | 开发规范、流程、标准指引 | 全体开发者 |
| **Level 2** | **[Server端开发](server/README.md)** | 后端开发、API开发、数据库操作 | 后端开发者 |
| **Level 3** | **[Client端开发](client/README.md)** | WPF开发、UI设计、客户端逻辑 | 前端开发者 |
| **Level 4** | **[共享开发](shared/README.md)** | 跨层开发、通用组件、接口定义 | 架构师、全栈开发者 |

## 🏗️ 开发架构对齐

### Server端开发栈
```
Server端开发 (LYBT.Server)
├── 开发语言：C# (.NET 8)
├── 架构模式：三层架构 (Controller + Service + Repository)
├── 数据库：SQL Server 2019+
├── ORM：Entity Framework Core
├── API文档：Swagger/OpenAPI
└── 测试框架：NUnit + Moq
```

### Client端开发栈
```
Client端开发 (LYBT.Desktop)
├── 开发语言：C# (.NET 8)
├── UI框架：WPF
├── 架构模式：MVVM (五层架构)
├── 依赖注入：Microsoft.Extensions.DependencyInjection
├── 组件库：Material Design
└── 测试框架：NUnit + Moq
```

### 共享开发栈
```
共享开发 (LYBT.Shared)
├── 开发语言：C# (.NET 8)
├── 核心组件：Models + Interfaces + Infrastructure
├── 验证框架：FluentValidation
├── 对象映射：AutoMapper
├── 缓存：MemoryCache
└── 日志：Serilog
```

## 🚀 快速开始

### 1. 环境准备
```bash
# 安装.NET 8 SDK
dotnet --version

# 克隆项目
git clone https://github.com/shouqitao/凌隐宝堂中医诊所.git
cd 凌隐宝堂中医诊所

# 恢复依赖
dotnet restore LYBT.All.sln

# 构建项目
dotnet build LYBT.All.sln -c Release
```

### 2. 开发工具推荐
- **IDE**: Visual Studio 2022 Professional
- **数据库工具**: SQL Server Management Studio
- **API测试**: Postman or Swagger UI
- **版本控制**: Git + GitHub Desktop
- **代码质量**: SonarQube (可选)

### 3. 开发流程
1. **需求分析** → [模块文档](../modules/README.md)
2. **架构设计** → [架构标准](../architecture/README.md)
3. **代码实现** → 对应层级开发指南
4. **测试验证** → [测试指南](shared/testing-guide.md)
5. **代码审查** → [代码规范](shared/coding-standards.md)
6. **部署发布** → [部署指南](../deep/deployment-guide.md)

## 📋 开发规范

### 1. 代码规范
- **命名约定**: PascalCase (类型/方法)，camelCase (变量/字段)
- **文件编码**: UTF-8 with BOM
- **缩进**: 4个空格
- **行长度**: 不超过120字符
- **注释**: 关键逻辑必须注释，API方法必须XML文档

### 2. 项目结构规范
```
LYBT.All.sln
├── LYBT.Server/          # 服务端
│   ├── Controllers/      # API控制器
│   ├── Services/         # 业务服务
│   ├── Repositories/     # 数据访问
│   └── Infrastructure/   # 基础设施
├── LYBT.Desktop/         # 客户端
│   ├── Shell/           # 主程序
│   ├── Core/            # 核心组件
│   ├── Services/        # 业务服务
│   ├── Infrastructure/  # 基础设施
│   └── Modules/         # 业务模块
└── LYBT.Shared/         # 共享组件
    ├── Models/          # 数据模型
    ├── Interfaces/      # 接口定义
    ├── Infrastructure/  # 基础设施
    └── Utilities/       # 工具类
```

### 3. Git工作流
```bash
# 1. 创建功能分支
git checkout -b feature/patient-management

# 2. 开发提交
git add .
git commit -m "feat: 添加患者管理功能"

# 3. 推送分支
git push origin feature/patient-management

# 4. 创建Pull Request
# 5. 代码审查
# 6. 合并到主分支
```

## 🔧 开发工具配置

### 1. Visual Studio配置
```json
// .vscode/settings.json (如果使用VS Code)
{
    "csharp.format.enable": true,
    "csharp.format.style": "space",
    "csharp.format.indent.size": 4,
    "csharp.format.newLine": "crlf",
    "csharp.format.space.afterCast": false,
    "csharp.format.space.afterColon": true,
    "csharp.format.space.afterComma": true,
    "csharp.format.space.afterDot": false,
    "csharp.format.space.afterSemicolon": true,
    "csharp.format.space.aroundBinaryOperators": "beforeAndAfter",
    "csharp.format.space.beforeColon": false,
    "csharp.format.space.beforeComma": false,
    "csharp.format.space.beforeDot": false,
    "csharp.format.space.beforeOpenSquare": false,
    "csharp.format.space.betweenEmptySquare": false
}
```

### 2. 代码格式化
```bash
# 安装格式化工具
dotnet tool install -g dotnet-format

# 格式化整个解决方案
dotnet format LYBT.All.sln

# 检查格式化问题
dotnet format LYBT.All.sln --verify-no-changes
```

### 3. 静态代码分析
```bash
# 安装分析工具
dotnet tool install -g dotnet-sonarscanner

# 运行代码分析
dotnet sonarscanner begin /k:"LYBT-Clinic" /d:sonar.login="your-token"
dotnet build LYBT.All.sln -c Release
dotnet sonarscanner end /d:sonar.login="your-token"
```

## 🧪 测试策略

### 1. 测试层级
- **单元测试**: 测试单个类或方法
- **集成测试**: 测试多个组件协作
- **端到端测试**: 测试完整业务流程
- **性能测试**: 测试系统性能指标

### 2. 测试工具
- **单元测试**: NUnit
- **模拟框架**: Moq
- **集成测试**: ASP.NET Core Test Framework
- **UI测试**: FlaUI (WPF)

### 3. 测试运行
```bash
# 运行所有测试
dotnet test LYBT.All.sln -c Release

# 运行特定项目测试
dotnet test LYBT.Server.Tests -c Release

# 生成测试覆盖率报告
dotnet test LYBT.All.sln -c Release --collect:"XPlat Code Coverage"
```

## 📊 性能优化

### 1. 后端优化
- **数据库优化**: 索引优化、查询优化
- **缓存策略**: 内存缓存、分布式缓存
- **异步编程**: 使用async/await
- **连接池**: 合理配置数据库连接池

### 2. 前端优化
- **UI虚拟化**: 大数据集虚拟化显示
- **数据绑定**: 优化数据绑定性能
- **异步操作**: UI线程不阻塞
- **内存管理**: 及时释放资源

### 3. 通用优化
- **代码优化**: 避免不必要的计算
- **内存优化**: 合理使用内存
- **网络优化**: 减少网络请求
- **并发优化**: 合理使用并发

## 🔒 安全开发

### 1. 身份认证
- **JWT认证**: 使用JWT进行身份认证
- **双轨认证**: 普通用户 + 超级管理员
- **令牌管理**: 合理的令牌过期时间

### 2. 数据安全
- **输入验证**: 所有输入必须验证
- **SQL注入防护**: 使用参数化查询
- **敏感数据**: 敏感数据加密存储
- **访问控制**: 实现细粒度权限控制

### 3. 网络安全
- **HTTPS**: 强制使用HTTPS
- **CORS**: 配置跨域资源共享
- **CSRF**: 防止跨站请求伪造
- **XSS**: 防止跨站脚本攻击

## 🚀 部署指南

### 1. 开发环境
```bash
# 启动服务端
cd LYBT.Server
dotnet run --environment Development

# 启动客户端
cd LYBT.Desktop
dotnet run --environment Development
```

### 2. 测试环境
```bash
# 构建发布版本
dotnet publish LYBT.Server -c Release -o ./publish
dotnet publish LYBT.Desktop -c Release -o ./publish

# 部署到IIS
# 详见: ../deep/deployment-guide.md
```

### 3. 生产环境
```bash
# Docker部署 (可选)
docker build -t lybt-clinic .
docker run -d -p 5001:80 lybt-clinic

# 传统部署
# 详见: ../deep/deployment-guide.md
```

## 📋 质量保证

### 1. 代码审查
- **代码规范**: 符合项目代码规范
- **架构合规**: 遵循项目架构标准
- **性能考虑**: 考虑性能影响
- **安全考虑**: 考虑安全风险

### 2. 自动化检查
- **编译检查**: 自动编译验证
- **测试检查**: 自动运行测试
- **代码分析**: 静态代码分析
- **安全扫描**: 安全漏洞扫描

### 3. 持续集成
- **自动构建**: 代码提交自动构建
- **自动测试**: 构建成功自动测试
- **自动部署**: 测试通过自动部署
- **自动通知**: 构建失败通知

## 📚 学习资源

### 1. 技术文档
- **.NET文档**: https://docs.microsoft.com/dotnet/
- **ASP.NET Core**: https://docs.microsoft.com/aspnet/core/
- **WPF文档**: https://docs.microsoft.com/dotnet/desktop/wpf/
- **Entity Framework**: https://docs.microsoft.com/ef/core/

### 2. 最佳实践
- **C#编码规范**: Microsoft C# Coding Conventions
- **ASP.NET Core最佳实践**: ASP.NET Core Best Practices
- **WPF最佳实践**: WPF Best Practices
- **Entity Framework最佳实践**: EF Core Best Practices

### 3. 社区资源
- **GitHub**: 项目源代码和问题跟踪
- **Stack Overflow**: 技术问答
- **技术博客**: 团队技术分享
- **在线课程**: 相关技术课程

## 🔗 相关文档

### 核心文档
- **[架构总览](../architecture/README.md)** - 三层对齐架构设计原理
- **[快速参考](../quick-reference/README.md)** - 常用API、配置、代码模式
- **[模块文档](../modules/README.md)** - 8个业务模块详细说明

### 开发指南
- **[Server端开发指南](server/README.md)** - 后端开发规范和实践
- **[Client端开发指南](client/README.md)** - WPF客户端开发指南
- **[共享开发指南](shared/README.md)** - 跨层组件开发指南

### 专业指南
- **[测试指南](shared/testing-guide.md)** - 单元测试、集成测试指南
- **[部署指南](../deep/deployment-guide.md)** - 从开发到生产的部署流程
- **[性能优化指南](../deep/performance-optimization.md)** - 系统性能优化策略

## 🎯 开发路线图

### 短期目标 (1-2个月)
- [ ] 完善开发文档和工具链
- [ ] 建立代码审查流程
- [ ] 实现自动化测试
- [ ] 优化开发环境配置

### 中期目标 (3-6个月)
- [ ] 完善持续集成流程
- [ ] 实现自动化部署
- [ ] 优化性能监控
- [ ] 建立安全扫描流程

### 长期目标 (6-12个月)
- [ ] 完善开发平台
- [ ] 实现DevOps流程
- [ ] 建立质量度量体系
- [ ] 优化团队协作流程

---

**文档维护**：开发组 | **最后更新**：2025-10-15  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核