# P4 Release 部署运行手册

**版本**: P4 Release 1.0  
**更新时间**: 2025-09-12  
**适用环境**: .NET 8.0 + Windows Server  
**状态**: 生产就绪  

## 🚀 快速部署指南

### 前置要求

**系统环境**:
- Windows Server 2019+ 或 Windows 10/11
- .NET 8.0 Runtime (框架依赖模式需要)
- SQL Server 2019+ 或 SQL Server Express
- IIS 10.0+ (可选，用于生产部署)

**网络要求**:
- HTTP端口: 5001 (默认) 或自定义端口
- HTTPS端口: 7001 (可选，用于SSL)
- 数据库端口: 1433 (SQL Server默认)

### 🎯 30秒快速启动

```powershell
# 1. 进入项目目录
cd D:\LYBT-System

# 2. 一键启动WebAPI (推荐生产模式)
.\scripts\run-webapi.ps1 -SelfContained -Port "5001" -Environment "Production"

# 3. 验证服务健康 (可选)
.\scripts\health-check.ps1 -Detailed

# 4. 访问服务
# API: http://localhost:5001
# 文档: http://localhost:5001/swagger
```

**预期结果**:
- ✅ WebAPI服务在5001端口启动成功
- ✅ 健康检查A级评分 (90分以上)
- ✅ Swagger API文档可访问
- ✅ 所有核心模块状态正常

## 📋 详细部署步骤

### 步骤1: 环境准备

#### 1.1 获取部署产物
```powershell
# 从源码构建 (如需要)
git clone <repository-url>
git checkout release/p4-build-run-stability
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -o out/webapi-self --self-contained true

# 或直接使用现有产物
# 产物位置: out/webapi-self (115MB) 或 out/webapi-fx (26MB)
```

#### 1.2 数据库配置
```json
// appsettings.Production.json 中配置数据库连接
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

#### 1.3 安全配置验证
```powershell
# 检查配置文件完整性
dir out/webapi-self/appsettings*.json

# 预期文件:
# appsettings.json - 基础配置
# appsettings.Production.json - 生产环境配置
# appsettings.Security.json - 安全配置
```

### 步骤2: 服务部署

#### 2.1 自包含部署 (推荐)
```powershell
# 复制产物到生产目录
xcopy out\webapi-self C:\WebAPI\LYBT\ /E /I

# 启动服务
cd C:\WebAPI\LYBT
.\scripts\run-webapi.ps1 -SelfContained -Port "80" -Environment "Production"
```

#### 2.2 框架依赖部署 (轻量级)
```powershell
# 确保.NET 8.0 Runtime已安装
dotnet --version  # 应显示8.0.x

# 复制产物
xcopy out\webapi-fx C:\WebAPI\LYBT\ /E /I

# 启动服务
cd C:\WebAPI\LYBT  
.\scripts\run-webapi.ps1 -FrameworkDependent -Port "80" -Environment "Production"
```

### 步骤3: 服务验证

#### 3.1 基础健康检查
```powershell
# 执行完整健康检查
.\scripts\health-check.ps1 -BaseUrl "http://localhost:80" -Detailed -Export

# 检查关键指标:
# - 总体状态: Healthy
# - 健康评分: A级 (90+分)
# - 端点状态: 6/6 健康
# - 响应时间: <2000ms
```

#### 3.2 功能验证测试
```powershell
# 测试核心API端点
curl http://localhost:80/health
curl http://localhost:80/api/v1/health
curl http://localhost:80/swagger

# 预期响应:
# /health: {"status": "Healthy"}
# /api/v1/health: 详细健康信息
# /swagger: Swagger UI页面
```

## 🔧 运维操作

### 日常运维命令

#### 启动服务
```powershell
# 生产环境启动 (推荐)
.\scripts\run-webapi.ps1 -SelfContained -Port "80" -Environment "Production"

# 开发环境启动
.\scripts\run-webapi.ps1 -FrameworkDependent -Port "5001" -Environment "Development" -Wait
```

#### 健康监控  
```powershell
# 一次性检查
.\scripts\health-check.ps1 -Detailed

# 持续监控 (每5分钟检查)
.\scripts\health-check.ps1 -Continuous -Interval 300 -Export

# 导出健康报告
.\scripts\health-check.ps1 -Export -OutputPath "health-$(Get-Date -Format 'yyyyMMdd-HHmm').json"
```

#### 停止服务
```powershell
# 优雅停止 (推荐)
.\scripts\stop-webapi.ps1

# 强制停止 (紧急情况)
.\scripts\stop-webapi.ps1 -Force

# 彻底清理
.\scripts\stop-webapi.ps1 -All -Force
```

### 监控指标

#### 核心健康指标
- **服务可用性**: /health端点响应状态
- **API响应时间**: 平均响应时间 <2秒
- **系统资源**: CPU <80%, 内存 <90%, 磁盘 <80%
- **数据库连接**: 连接池状态和响应时间

#### 业务健康指标  
- **认证服务**: /api/v1/auth/health状态
- **用户管理**: /api/v1/users/health状态
- **患者管理**: /api/v1/patients/health状态
- **中药材管理**: /api/v1/herbs/health状态

## 🚨 故障排除

### 常见问题及解决方案

#### 1. 服务启动失败

**问题症状**:
```
❌ WebAPI启动失败
错误: 进程无法启动或立即退出
```

**排查步骤**:
```powershell
# 1. 检查端口占用
netstat -ano | findstr ":5001"

# 2. 检查依赖文件
dir out/webapi-self/LYBT.WebAPI.exe
dir out/webapi-self/appsettings*.json

# 3. 检查权限
# 确保当前用户对部署目录有读写权限

# 4. 查看详细错误
.\scripts\run-webapi.ps1 -Verbose
```

**解决方案**:
- 端口占用: 更换端口或停止占用进程
- 文件缺失: 重新发布部署产物  
- 权限问题: 以管理员权限运行或调整文件夹权限
- .NET缺失: 安装.NET 8.0 Runtime (框架依赖模式)

#### 2. 健康检查失败

**问题症状**:
```
❌ /health - 不健康: 连接被拒绝
总体状态: Unhealthy
健康评分: F级 (< 60分)
```

**排查步骤**:
```powershell
# 1. 确认服务状态
Get-Process -Name "LYBT.WebAPI"

# 2. 检查端口监听
netstat -an | findstr ":5001.*LISTENING"

# 3. 测试基础连接
Test-NetConnection -ComputerName localhost -Port 5001

# 4. 查看应用日志
# 检查WebAPI控制台输出或事件查看器
```

**解决方案**:
- 服务未启动: 重新启动WebAPI服务
- 端口不通: 检查防火墙和网络配置
- 应用异常: 查看详细日志，修复配置错误
- 数据库问题: 验证数据库连接字符串和服务状态

#### 3. 性能问题

**问题症状**:
```
⚠️  API响应时间过长 (>5秒)
⚠️  健康评分: C级 (70-79分)
⚠️  系统资源使用率高
```

**排查步骤**:
```powershell
# 1. 系统资源检查
.\scripts\health-check.ps1 -Detailed

# 2. 进程资源使用
Get-Process -Name "LYBT.WebAPI" | Select-Object CPU,WorkingSet,VirtualMemorySize

# 3. 数据库性能
# 检查数据库连接数和查询性能
```

**解决方案**:
- 高CPU: 检查死循环或计算密集操作  
- 高内存: 排查内存泄漏，重启服务
- 慢查询: 优化数据库索引和查询语句
- 网络延迟: 检查数据库连接和网络配置

### 应急处理流程

#### 紧急故障处理
```powershell
# 1. 立即停止有问题的服务
.\scripts\stop-webapi.ps1 -Force

# 2. 检查系统状态  
.\scripts\health-check.ps1 -BaseUrl "http://localhost:5001"

# 3. 清理并重启
Start-Sleep -Seconds 5
.\scripts\run-webapi.ps1 -SelfContained -Environment "Production"

# 4. 验证恢复
.\scripts\health-check.ps1 -Detailed

# 5. 记录事件
# 导出健康报告和系统日志用于后续分析
```

## 📊 生产环境建议

### 部署架构

#### 单机部署 (小型环境)
```
Windows Server
├── WebAPI服务 (端口80/443)
├── SQL Server Express  
└── 监控脚本 (每5分钟健康检查)
```

#### 负载均衡部署 (中大型环境)
```
负载均衡器 (Nginx/IIS ARR)
├── WebAPI节点1 (端口5001)
├── WebAPI节点2 (端口5002)
└── SQL Server (独立服务器)
```

### 监控告警

#### 监控策略
```powershell
# 生产监控脚本 (每5分钟执行)
$healthResult = .\scripts\health-check.ps1 -Export -OutputPath "logs\health-$(Get-Date -Format 'HHmm').json"
if ($LASTEXITCODE -ne 0) {
    # 发送告警邮件或短信
    Send-Alert -Message "WebAPI健康检查失败" -Level "Critical"
}
```

#### 关键指标阈值
- **可用性**: >99.9% (允许每月43分钟停机)
- **响应时间**: P95 <2秒, P99 <5秒
- **错误率**: <0.1% (1000次请求中少于1次错误)
- **资源使用**: CPU <70%, 内存 <80%, 磁盘 <75%

### 备份与恢复

#### 数据备份
```sql
-- 数据库备份脚本 (每日执行)
BACKUP DATABASE [LYBTDB] 
TO DISK = N'C:\Backup\LYBTDB_Full_$(Get-Date -Format "yyyyMMdd").bak'
WITH FORMAT, INIT, COMPRESSION;
```

#### 配置备份
```powershell
# 配置文件备份
$backupDir = "C:\Backup\Config\$(Get-Date -Format 'yyyyMMdd')"
New-Item -Path $backupDir -ItemType Directory -Force
Copy-Item -Path "appsettings*.json" -Destination $backupDir
```

## 📞 支持与联系

### 技术支持
- **系统架构**: 查看 `build-stability-report.md` 了解系统架构和稳定性评估
- **脚本使用**: 查看 `scripts/README.md` 了解详细的脚本使用方法
- **API文档**: 访问 `http://localhost:5001/swagger` 查看完整API文档

### 文档资源
- **构建报告**: `_reports/2025-09/release/build-matrix.md`
- **测试报告**: `_reports/2025-09/release/test-matrix.md`  
- **发布摘要**: `_reports/2025-09/release/publish-summary.md`
- **稳定性报告**: `_reports/2025-09/release/build-stability-report.md`

---

**手册版本**: P4 Release 1.0  
**最后更新**: 2025-09-12 22:50  
**维护状态**: 生产就绪 ✅