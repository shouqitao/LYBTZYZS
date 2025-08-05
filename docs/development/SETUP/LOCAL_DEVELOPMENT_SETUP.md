# 本地开发环境配置指南

## 概述
本文档记录了本地开发环境的标准配置。除非特别说明，所有开发工作都使用本地部署的 WebAPI + 前端进行联调。

## 1. WebAPI 配置

### 1.1 启动配置
**文件**: `src/Backend/Services/LYBT.WebAPI/Properties/launchSettings.json`

```json
"http": {
  "commandName": "Project",
  "dotnetRunMessages": true,
  "launchBrowser": true,
  "launchUrl": "swagger",
  "applicationUrl": "http://localhost:5001",
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  }
}
```

**使用说明**：
- 在 Visual Studio 中选择 "http" 配置文件（不是 "https"）
- WebAPI 将在 `http://localhost:5001` 运行
- Swagger 文档地址：`http://localhost:5001/swagger`

### 1.2 数据库连接
**文件**: `src/Backend/Services/LYBT.WebAPI/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

**说明**：
- 使用本地 SQL Server 实例
- Windows 集成认证
- 数据库名：LYBTDB

如需使用 SQL Server 认证，可修改为：
```json
"DefaultConnection": "Server=localhost;Database=LYBTDB;User Id=sa;Password=您的密码;TrustServerCertificate=true;MultipleActiveResultSets=true"
```

## 2. WPF 客户端配置

### 2.1 API 地址配置
**文件**: `src/Frontend/Desktop/Shell/appsettings.json`

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5001/",
    "TimeoutSeconds": 60
  }
}
```

### 2.2 默认配置
以下文件中的默认值也已更新为本地地址：
- `src/Frontend/Desktop/Core/Configuration/ApiConfiguration.cs`
- `src/Frontend/Desktop/Core/Configuration/ApiSettings.cs`

默认 URL: `http://localhost:5001/`

## 3. 开发流程

### 3.1 启动顺序
1. **启动 WebAPI**
   - 打开 `LYBT.Backend.sln`
   - 选择 LYBT.WebAPI 项目
   - 选择 "http" 启动配置
   - 按 F5 启动

2. **启动 WPF 客户端**
   - 打开 `LYBT.Desktop.sln`
   - 选择 LYBT.WPF.Client.Shell 项目
   - 按 F5 启动

### 3.2 验证连接
1. WebAPI 启动后，访问 `http://localhost:5001/swagger` 确认服务正常
2. WPF 客户端启动后，使用以下凭据登录：
   - 用户名：sysadmin
   - 密码：Admin@123456

## 4. 注意事项

### 4.1 HTTPS 说明
- 开发阶段使用 HTTP 以避免证书问题
- 如需使用 HTTPS，需要配置 SSL 证书验证忽略（已在代码中预留）

### 4.2 数据库初始化
- 首次运行时，WebAPI 会自动检查并创建数据库
- 数据库迁移会自动应用

### 4.3 端口冲突
如果 5001 端口被占用，可以修改为其他端口，但需要同时更新：
- WebAPI 的 `launchSettings.json`
- WPF 客户端的 `appsettings.json`

## 5. 生产环境部署
正式发布时，需要修改以下配置：
- 数据库连接字符串
- API 基础地址
- 启用 HTTPS
- 配置正式的认证证书

## 更新历史
- 2024-01-30: 初始配置，统一使用本地环境进行开发