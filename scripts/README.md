# P4 Release - WebAPI运行脚本使用指南

本目录包含P4 Release阶段的WebAPI服务运行和健康检查脚本，提供一键启动、停止和监控功能。

## 🚀 快速开始

### 1. 启动WebAPI服务

```powershell
# 基础启动（默认使用自包含版本）
.\scripts\run-webapi.ps1

# 使用框架依赖版本启动
.\scripts\run-webapi.ps1 -FrameworkDependent

# 指定端口和环境
.\scripts\run-webapi.ps1 -Port "8080" -Environment "Development"
```

### 2. 健康检查

```powershell
# 基础健康检查
.\scripts\health-check.ps1

# 详细健康检查（包含业务API）
.\scripts\health-check.ps1 -Detailed

# 持续监控模式
.\scripts\health-check.ps1 -Continuous -Interval 60
```

### 3. 停止服务

```powershell
# 优雅停止
.\scripts\stop-webapi.ps1

# 强制停止
.\scripts\stop-webapi.ps1 -Force
```

## 📋 脚本详细说明

### run-webapi.ps1 - WebAPI启动脚本
- 自动检测并停止现有WebAPI进程
- 智能端口占用检查  
- 自动验证部署产物完整性
- 启动后自动健康检查

### health-check.ps1 - 健康检查脚本  
- 核心健康端点检查
- 业务API健康检查
- 系统资源监控
- 健康评分体系(A-F级)

### stop-webapi.ps1 - 服务停止脚本
- 优雅停止和强制停止
- 超时保护机制
- 资源清理和状态报告

## 🔧 故障排除

### 端口占用错误
```powershell
.\scripts\stop-webapi.ps1 -Force
.\scripts\run-webapi.ps1 -Port "5002"
```

### 部署产物不存在
```powershell
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -o out/webapi-self --self-contained true
```

---
**文档版本**: P4 Release 1.0  
**更新时间**: 2025-09-12  
**适用版本**: .NET 8.0 + LYBT系统
