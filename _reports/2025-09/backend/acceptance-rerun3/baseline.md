# Backend Acceptance Smoke Test · Triple Run - 基线环境信息

**执行时间**: 2025-09-15 16:56:00  
**任务**: Backend — Acceptance Smoke Test · Triple Run（APPLY）  
**目标**: 七大模块连续3轮完整冒烟验证，稳定性评估

## 📊 基线环境配置

### 🌐 网络配置
- **基线端口**: `http://localhost:8080`
- **端口来源**: cleanup-notes.md (p2-fix-batch1)  
- **监听状态**: ✅ **LISTENING** (2个连接入口)
- **TCP连接**: ⚠️ 超时 (但HTTP健康检查成功)

### 🔧 运行环境
- **操作系统**: Windows 10 中文版
- **开发环境**: Development  
- **机器名称**: MYHOUSE
- **工作目录**: `D:\source\repos\LYBTZYZS\src\Server\Services\LYBT.WebAPI`
- **启动配置**: launchSettings.json `http` profile

### 🛠️ 进程状态
- **dotnet进程数**: 9个进程运行中
- **WebAPI进程**: 正常启动 (进程ID: cc9651)
- **启动用时**: ~1秒 (数据库初始化 + 应用启动)
- **启动状态**: ✅ **Application started. Press Ctrl+C to shut down.**

### 🗄️ 数据库环境
- **数据库服务器**: SQL Server 2012 (localhost)
- **目标数据库**: LYBTDB
- **连接状态**: ✅ **已连接且可访问**
- **迁移状态**: ✅ **已是最新版本** (13个迁移全部应用)
- **表结构验证**: ✅ **Users、AdminSecrets、Patients等核心表验证通过**
- **管理员账户**: ✅ **sysadmin超级管理员存在**

### 🚀 应用程序状态
- **启动检查**: ✅ **数据库初始化成功**
- **服务配置**: ✅ **配置服务初始化完成**
- **数据库连接**: ✅ **数据库连接配置验证通过** 
- **JWT配置**: ✅ **JWT配置验证通过**
- **日志系统**: ✅ **日志系统初始化成功**

## 📋 健康检查结果

### ✅ HTTP健康端点验证
- **端点**: `GET /api/v1/health`
- **状态码**: **200 OK**
- **响应时间**: 0.28秒
- **重试次数**: 1/3 (首次成功)
- **健康状态**: **SUCCESS**

### 📊 检查详情
| 检查项目 | 状态 | 详情 |
|---------|------|------|
| 进程检查 | ✅ FOUND | 9个dotnet进程运行中 |
| 端口检查 | ✅ LISTENING | 端口8080正常监听 |
| TCP连接 | ⚠️ TIMEOUT | 超时但不影响HTTP访问 |
| HTTP健康检查 | ✅ SUCCESS | 200 OK, 0.28s响应 |

## 🎯 下一步操作

### ② 获取令牌（Auth）
- 目标：Doctor账号登录获取JWT令牌
- 端点：`POST /api/v1/auth/login`
- 凭据：系统管理员 sysadmin / Admin@123456
- 输出：认证状态和JWT令牌记录

### ③ 三轮冒烟执行 
- 覆盖七大模块：Auth / Users / Patients / Consultation / Prescriptions / Herbs / Formula
- 执行模式：幂等测试 + 最小数据集
- 轮次：连续3轮完整执行
- 记录：每轮结果和波动分析

## 🔍 环境变量关键项

```bash
# 应用程序URLs
ASPNETCORE_URLS=http://localhost:8080

# 环境设置
ASPNETCORE_ENVIRONMENT=Development

# 数据库连接
ConnectionStrings__DefaultConnection=Server=localhost;Database=LYBTDB;...
```

## ⚠️ 注意事项

1. **TCP连接超时**: 虽然TCP直连测试超时，但HTTP健康检查成功，表明WebAPI功能正常
2. **多进程运行**: 发现9个dotnet进程，可能包含前期遗留进程，已确认目标进程正常
3. **TLS警告**: 检测到TLS 1.0协商警告，但不影响开发环境功能
4. **性能基线**: 健康检查响应时间0.28秒，作为后续3轮测试的性能基线

---

**✅ 第①步：基线确认与健康检查 - 完成**  
**状态**: WebAPI healthy at http://localhost:8080, 准备进入下一阶段