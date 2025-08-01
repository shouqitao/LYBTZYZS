# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在处理此代码库时提供指导。

## 项目概述

凌隐宝堂中医诊所管理系统 (LYBT) - 基于 .NET 8 Web API 后端和 WPF 桌面前端的中医诊所管理系统。

## 常用开发命令

### 构建命令
```bash
# 构建整个解决方案
dotnet build LYBTZYZS.sln

# 仅构建后端
cd src/Backend
dotnet build LYBT.Backend.sln

# 仅构建前端
cd src/Frontend
dotnet build LYBT.Client.sln
```

### 开发服务器
```bash
# 快速启动开发服务器 (Windows)
启动控制台.bat
# 或
scripts\start-dev.bat

# 手动启动 WebAPI
cd src/Backend/Services/LYBT.WebAPI
dotnet run
```

### 数据库管理
```bash
# 交互式数据库管理器 (Windows)
scripts\database-manager.bat

# 添加新迁移
dotnet ef migrations add 迁移名称 --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

### 测试
```bash
# 运行所有测试
dotnet test

# 运行测试并生成覆盖率报告
dotnet test /p:CollectCoverage=true
```

## 高层架构

### 后端结构（整洁架构）

1. **基础设施层** (`LYBT.Infrastructure`)
   - 所有模块的统一 `AppDbContext` - **所有数据库操作都通过这个单一上下文进行**
   - Entity Framework Core 迁移
   - 身份验证（JWT）和授权服务
   - 缓存服务（内存和分布式）
   - 数据库初始化和种子数据

2. **核心层** (`LYBT.Common`, `LYBT.Models`)
   - 共享枚举、工具类和扩展方法
   - 领域模型和 DTO
   - 通用响应包装器：`ApiResponse<T>`
   - 分页支持

3. **业务模块** (`src/Backend/Modules/` 中的 15+ 个模块)
   每个模块遵循以下模式：
   ```
   LYBT.Module.[名称]/
   ├── Interfaces/      # 服务契约
   ├── Services/        # 业务逻辑
   ├── Repositories/    # 数据访问
   ├── Mapping/         # AutoMapper 配置
   └── [名称]Module.cs  # 依赖注入注册
   ```

4. **Web API 层** (`LYBT.WebAPI`)
   - 控制器继承自 `BaseController`
   - Swagger 文档位于 `/swagger`
   - 全局异常处理中间件
   - JWT 身份验证和基于角色的授权

### 前端结构（WPF + Prism）

- **Shell**：主应用程序容器
- **Modules**：特定功能的 UI 模块
- **Shared**：通用 UI 组件和样式
- 使用 Prism 框架和 DryIoc 容器

### 关键业务模块

- **Auth**：身份验证和授权
- **Users**：用户管理
- **Patients**：患者档案
- **Doctors**：医生管理
- **Registration**：预约挂号
- **DiagnosisTreatment**：诊断治疗
- **Prescriptions**：处方管理
- **Herbs**：中药材目录
- **Pharmacy**：药房调配
- **Billing**：费用结算

### 重要约定

1. **数据库**：所有模块共享基础设施层的单一 `AppDbContext`
2. **API 响应**：始终包装在 `ApiResponse<T>` 中
3. **身份验证**：JWT Bearer 令牌和基于角色的授权
4. **异步模式**：始终使用 async/await
5. **依赖注入**：仅使用构造函数注入
6. **AutoMapper**：用于所有 DTO-实体转换

### 开发提示

- 数据库在首次运行时自动初始化
- 默认登录：用户名 `sysadmin`，密码 `Admin@123456`
- API 运行在 `http://localhost:5297`（开发环境）
- 使用 `scripts/` 中的批处理脚本执行常见任务
- 所有迁移必须添加到 Infrastructure 项目

### 显示语言约定
- 中文显示