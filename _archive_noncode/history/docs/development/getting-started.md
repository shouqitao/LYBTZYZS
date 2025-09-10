# 🚀 快速开始指南

> **前后端协同开发指南** | 48个项目统一管理 | UltraThink架构标准  
> **项目状态**: ✅ **生产就绪** | 🎆 **企业级工具集完成** | **零编译错误**

## 📋 开发环境要求

### 必备软件

- **Visual Studio 2022** (17.8+) - 完整IDE支持
- **.NET 8.0 SDK** - 最新LTS版本
- **SQL Server 2019+** 或 **SQL Server Express LocalDB**
- **Git** - 版本控制
- **Windows 10/11** - 推荐操作系统

### 可选工具

- **SQL Server Management Studio** - 数据库管理
- **Postman** 或 **REST Client** - API测试
- **Git客户端** (GitHub Desktop, SourceTree等)

## ⚡ 一键启动开发环境

### 使用脚本启动 (推荐)

```bash
# 交互式开发管理器 (推荐)
scripts\dev-manager.bat

# 快速启动开发服务器
scripts\start-dev.bat

# 数据库管理
scripts\database-manager.bat
```

### 手动启动步骤

1. **克隆项目**:
   ```bash
   git clone https://github.com/shouqitao/LYBTZYZS.git
   cd LYBTZYZS
   ```

2. **配置数据库**:
   - 修改连接字符串：`src/Server/Services/LYBT.WebAPI/appsettings.json`
   - 默认使用LocalDB：`Server=(localdb)\\mssqllocaldb;Database=LYBTDB;Trusted_Connection=true;`

3. **初始化数据库**:
   ```bash
   # 更新数据库
   dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
   ```

4. **启动后端API**:
   ```bash
   dotnet run --project src/Server/Services/LYBT.WebAPI
   ```

5. **启动前端应用**:
   - 使用Visual Studio打开 `LYBT.Desktop.sln`
   - 设置 `LYBT.Desktop.Shell` 为启动项目
   - 按F5启动调试

## 🔑 默认登录信息

### 系统管理员账户
- **用户名**: `sysadmin`
- **密码**: `Admin@123456`
- **权限**: 系统管理员，拥有全部权限

### 测试医生账户 (如果已创建)
- **用户名**: `doctor01`
- **密码**: `Doctor@123456`
- **权限**: 医生权限，可进行诊疗操作

## 🏗️ 项目架构理解

### 解决方案结构

本项目包含3个解决方案文件：

```bash
# 完整解决方案 (48个项目)
LYBT.All.sln

# 后端解决方案 (11个项目)
LYBT.Server.sln

# 前端解决方案 (20个项目)  
LYBT.Desktop.sln
```

### 核心项目分类

**后端项目** (11个):
- **Core**: Infrastructure (数据访问) + Entities (实体模型)
- **Modules**: 8个业务模块 (Auth, Users, Patients等)
- **Services**: WebAPI (API服务入口)

**前端项目** (20个):
- **Core**: 核心基础设施
- **Modules**: 8个业务模块 (UltraThink双层架构)
- **Workbenches**: 7个工作台 (按角色分工)
- **Shell**: 应用外壳

**共享项目** (3个):
- **Models**: 数据传输对象
- **Interfaces**: 服务接口定义
- **Utilities**: 企业级工具类 (72个方法) ⭐

**测试项目** (14个):
- **Backend**: 后端测试 (10个)
- **Client**: 前端测试 (2个)
- **UltraThink**: 测试基础设施 (2个)

## 🔧 开发工具使用

### 常用开发命令

```bash
# 构建解决方案
dotnet build LYBT.All.sln              # 完整构建
dotnet build LYBT.Server.sln           # 后端构建
dotnet build LYBT.Desktop.sln          # 前端构建

# 数据库操作
dotnet ef migrations add MigrationName --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 测试运行
dotnet test                             # 运行所有测试
dotnet test --logger trx               # 生成测试报告
```

### Visual Studio 配置

**推荐设置**:
- 启动项目：设置为 `LYBT.WebAPI` (后端) + `LYBT.Desktop.Shell` (前端)
- 调试模式：Multiple startup projects
- 代码格式：使用EditorConfig配置

**扩展推荐**:
- GitHub Copilot
- SonarLint
- CodeMaid
- Productivity Power Tools

## 📊 开发工作流

### 1. 功能开发流程

1. **创建功能分支**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **开发前端模块**:
   - 位置：`src/Client/Desktop/Modules/LYBT.Desktop.{ModuleName}/`
   - 架构：UltraThink双层 (QueryService + BusinessService + Module)

3. **开发后端模块**:
   - 位置：`src/Server/Modules/LYBT.Module.{ModuleName}/`
   - 架构：传统三层 (Repository + Service + Controller)

4. **编写测试**:
   - 后端测试：`tests/Backend/LYBT.Module.{ModuleName}.Tests/`
   - 前端测试：`tests/Client/LYBT.Desktop.{ModuleName}.Tests/`

5. **验证质量**:
   ```bash
   # 检查编译状态
   dotnet build LYBT.All.sln
   
   # 运行测试
   dotnet test
   
   # 确保零编译警告
   ```

### 2. 代码规范检查

**编译质量要求**:
- ✅ 零编译错误
- ✅ 零编译警告  
- ✅ 符合.NET编码规范
- ✅ 通过所有单元测试

**代码审查清单**:
- [ ] 遵循UltraThink架构标准
- [ ] 正确使用依赖注入
- [ ] 异常处理完整
- [ ] 包含必要的单元测试
- [ ] XML文档注释完整

## 🚨 故障排除

### 常见问题解决

**编译错误**:
1. 清理并重新构建：`dotnet clean && dotnet build`
2. 检查NuGet包引用版本
3. 确保.NET 8.0 SDK已安装

**数据库连接问题**:
1. 检查连接字符串配置
2. 确认SQL Server服务运行
3. 验证数据库权限

**前端运行问题**:
1. 确认后端API已启动
2. 检查API基础地址配置
3. 验证Refit客户端配置

**依赖注入错误**:
1. 检查服务注册：`AddXxxModule()`
2. 确认接口实现匹配
3. 验证构造函数参数

### 获取帮助

**内部资源**:
- [开发规范](../standards/development-standards.md)
- [架构文档](../architecture/)
- [API文档](../api/)
- [故障排除指南](../troubleshooting/)

**外部资源**:
- [.NET 8 文档](https://docs.microsoft.com/en-us/dotnet/)
- [WPF 开发指南](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

## 📚 下一步

完成快速启动后，建议阅读以下文档：

1. **[系统架构概览](../architecture/system-architecture-overview.md)** - 理解整体设计
2. **[开发规范](../standards/development-standards.md)** - 掌握编码规范  
3. **[API集成指南](../api/api-integration-guide.md)** - 前后端协作
4. **[用户指南](../guides/system-user-guide.md)** - 了解业务流程

---

**开发愉快！** 🎉 如有问题，请查看文档或提交Issue。