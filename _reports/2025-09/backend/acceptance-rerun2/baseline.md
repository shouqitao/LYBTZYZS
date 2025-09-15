# Backend Acceptance Smoke Test Rerun2 - 基线报告

## 📋 基线信息

**执行时间**: 2025-09-15 13:37:00  
**分支**: release/backend-acceptance-smoketest-rerun2  
**预期端口**: http://localhost:8080 (基于上一轮健康报告记录)

## 🔍 环境检查

### 上一轮健康报告状态
- **文件**: `_reports/2025-09/backend/acceptance/health.json`
- **状态**: FAILED
- **端口**: 8080
- **错误**: TCP connection failed, Port binding conflict

### 当前环境基线

#### 数据库连接
- **服务器**: SQL Server 2012 (localhost)
- **数据库**: LYBTDB
- **连接状态**: ✅ 可用（基于之前日志）
- **迁移状态**: 13个迁移已应用

#### WebAPI进程状态
- **多进程问题**: 检测到多个后台dotnet进程在运行
- **端口冲突**: 端口8080/5001都存在binding问题

#### 关键发现：API版本约束错误
```
System.InvalidOperationException: The constraint reference 'apiVersion' could not be resolved to a type. 
Register the constraint type with 'Microsoft.AspNetCore.Mvc.Versioning'
```

## ⚠️ 严重阻断问题

### P0阻断错误：API版本约束未注册
- **错误类型**: System.InvalidOperationException
- **影响范围**: 所有/api/v1路由无法工作
- **根本原因**: 这正是之前CORS报告中发现的真实问题根因
- **解决方案**: 需要在Program.cs中正确注册API版本约束

### 多进程干扰问题
- **现象**: 多个dotnet进程同时运行WebAPI
- **影响**: 端口冲突导致健康检查失败
- **状态**: 进程已清理

## 📊 基线环境变量

| 环境变量 | 值 | 状态 |
|---------|-----|------|
| ASPNETCORE_ENVIRONMENT | Development | ✅ |
| Target URL | http://localhost:8080 | ❌ 端口不可用 |
| Alternative URL | http://localhost:5001 | ❌ API版本错误 |

## 🚨 健康检查预测

基于发现的API版本约束错误，预测健康检查将失败：
- **HTTP状态码**: 500 Internal Server Error
- **错误类型**: InvalidOperationException
- **失败原因**: apiVersion路由约束未注册

---

*基线报告生成时间: 2025-09-15*  
*状态: 发现P0阻断问题，需要立即修复API版本约束配置*