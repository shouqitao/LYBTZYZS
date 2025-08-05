# 快速开始指南

本文档将帮助您快速搭建开发环境并开始开发工作。

## 前置要求

### 开发工具
- **Visual Studio 2022** (17.0或更高版本)
- **.NET 8 SDK**
- **SQL Server 2019** 或 **SQL Server LocalDB**
- **Git**
- **Node.js** (用于前端构建工具)

### 推荐工具
- **Visual Studio Code** (用于查看文档和脚本)
- **SQL Server Management Studio (SSMS)**
- **Postman** (API测试)

## 获取代码

```bash
git clone https://github.com/your-org/LYBTZYZS.git
cd LYBTZYZS
```

## 项目结构

```
LYBTZYZS/
├── LYBT.All.sln          # 总解决方案
├── src/
│   ├── Backend/          # 后端项目
│   ├── Frontend/         # 前端项目
│   └── Shared/           # 共享项目
├── docs/                 # 文档
├── scripts/              # 脚本
└── BIN/                  # 输出目录
```

## 快速启动

### 1. 配置数据库

编辑 `src/Backend/Services/LYBT.WebAPI/appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LYBTDB;Trusted_Connection=True;"
  }
}
```

### 2. 初始化数据库

使用开发管理器：
```bash
scripts\dev-manager.bat
# 选择数据库管理选项
```

或手动执行：
```bash
cd src/Backend/Services/LYBT.WebAPI
dotnet ef database update --project ../../Core/LYBT.Infrastructure
```

### 3. 启动后端服务

```bash
scripts\start-dev.bat
```

或手动启动：
```bash
cd src/Backend/Services/LYBT.WebAPI
dotnet run
```

API文档地址：https://localhost:7001/swagger

### 4. 启动前端应用

```bash
cd src/Frontend/Desktop/Shell
dotnet run
```

## 默认登录凭据

- 用户名：`sysadmin`
- 密码：`Admin@123456`

## 开发流程

1. **创建功能分支**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **进行开发**
   - 遵循[编码规范](../standards/coding-standards.md)
   - 编写单元测试
   - 更新相关文档

3. **提交代码**
   ```bash
   git add .
   git commit -m "feat: 添加新功能"
   ```

4. **推送并创建PR**
   ```bash
   git push origin feature/your-feature-name
   ```

## 常用命令

### 构建项目
```bash
# 构建所有项目
dotnet build LYBT.All.sln

# 构建后端
dotnet build src/Backend/LYBT.Backend.sln

# 构建前端
dotnet build src/Frontend/Desktop/LYBT.Desktop.sln
```

### 运行测试
```bash
dotnet test
```

### 添加数据库迁移
```bash
cd src/Backend/Services/LYBT.WebAPI
dotnet ef migrations add MigrationName --project ../../Core/LYBT.Infrastructure
```

## 下一步

- 阅读[架构概述](../architecture/README.md)了解系统设计
- 查看[开发规范](../standards/README.md)了解团队约定
- 参考[API文档](../api/README.md)了解接口设计

## 获取帮助

- 查看[常见问题](./troubleshooting.md)
- 联系项目组成员
- 提交Issue到项目仓库