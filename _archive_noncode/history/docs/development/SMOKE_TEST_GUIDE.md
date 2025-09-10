# 冒烟测试指南

> UltraThink Phase 3 实用化优化 - PowerShell自动化测试工具

## 📋 概述

冒烟测试（Smoke Test）是一种快速验证系统基本功能是否正常的测试方法。本项目提供了完整的PowerShell自动化冒烟测试工具，用于验证凌隐宝堂中医诊所系统的核心功能。

## 🎯 测试范围

### 快速测试 (约30秒)
- ✅ 系统健康检查
- ✅ 数据库连接验证
- ✅ API文档可访问性
- ✅ 用户登录功能
- ✅ 基础认证API

### 完整测试 (约2-3分钟)
- ✅ 所有快速测试项目
- ✅ 多个业务API端点验证
- ✅ 详细的响应时间统计
- ✅ 完整的JSON测试报告
- ✅ 自动服务启停管理

## 🚀 使用方法

### 方法一：使用交互式菜单（推荐）

```batch
# 运行交互式测试菜单
scripts\run-smoke-tests.bat
```

菜单选项：
- **[1] 快速冒烟测试** - 30秒内验证核心功能
- **[2] 完整冒烟测试** - 需要手动启动WebAPI
- **[3] 完整测试(自动启动WebAPI)** - 全自动测试
- **[4] 查看上次测试报告** - 查看详细结果
- **[5] 清理测试文件** - 清理临时文件

### 方法二：直接运行PowerShell脚本

```powershell
# 快速测试
powershell -ExecutionPolicy Bypass -File "scripts\quick-smoke-test.ps1"

# 完整测试（不启动WebAPI）
powershell -ExecutionPolicy Bypass -File "scripts\smoke-test.ps1" -StartWebAPI $false

# 完整测试（自动启动WebAPI）
powershell -ExecutionPolicy Bypass -File "scripts\smoke-test.ps1" -StartWebAPI $true

# 自定义参数
powershell -ExecutionPolicy Bypass -File "scripts\smoke-test.ps1" -BaseUrl "https://localhost:7002" -OutputPath "custom-report.json"
```

## 📊 测试报告

### 控制台输出示例

```
================================================================
🧪 凌隐宝堂中医诊所系统 - 冒烟测试报告
================================================================

📊 测试摘要:
   总计测试: 8
   通过测试: 7
   失败测试: 1
   测试时长: 45.23 秒
   通过率: 87.5%

📋 详细结果:
   ✅ Health Check (2.15 秒) - 系统健康检查通过
   ✅ Database Connection (3.42 秒) - 数据库连接正常
   ❌ Login Functionality (0.85 秒) - 登录测试失败: 用户名或密码错误
   ✅ Swagger Documentation (1.23 秒) - Swagger文档可访问
   ✅ Authenticated API (2.67 秒) - 认证API访问正常
   ✅ Herbs API (1.89 秒) - API端点正常
   ✅ Patients API (1.95 秒) - API端点正常
   ✅ Version API (0.98 秒) - API端点正常

📄 详细报告已保存至: temp/smoke-test-results.json
================================================================
```

### JSON报告格式

```json
{
  "StartTime": "2025-08-20T10:30:00.000Z",
  "EndTime": "2025-08-20T10:30:45.230Z",
  "TotalDuration": 45.23,
  "Tests": [
    {
      "TestName": "Health Check",
      "Passed": true,
      "Message": "系统健康检查通过",
      "Details": {
        "status": "Healthy"
      },
      "Duration": 2.15,
      "Timestamp": "2025-08-20T10:30:02.150Z"
    }
  ],
  "Summary": {
    "Total": 8,
    "Passed": 7,
    "Failed": 1,
    "Warnings": 0
  }
}
```

## 📈 退出代码说明

| 退出代码 | 含义 | 通过率范围 |
|---------|------|----------|
| 0 | 系统状态良好 | ≥ 80% |
| 1 | 系统有轻微问题 | 60-79% |
| 2 | 系统有严重问题 | < 60% |
| 3 | 测试过程发生错误 | N/A |

## 🔧 配置参数

### smoke-test.ps1 参数

| 参数 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| BaseUrl | string | "https://localhost:7001" | API服务器地址 |
| OutputPath | string | "temp/smoke-test-results.json" | 报告输出路径 |
| StartWebAPI | bool | true | 是否自动启动WebAPI服务 |
| TimeoutSeconds | int | 30 | 单个测试超时时间 |

### quick-smoke-test.ps1 参数

| 参数 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| BaseUrl | string | "https://localhost:7001" | API服务器地址 |

## 🔍 故障排除

### 常见问题

#### 1. PowerShell执行策略错误
```
错误: 无法加载文件，因为在此系统上禁止运行脚本
解决: powershell -ExecutionPolicy Bypass -File "脚本路径"
```

#### 2. SSL证书错误
```
错误: 基础连接已关闭: 无法为SSL/TLS安全通道建立信任关系
解决: 脚本已自动处理开发环境SSL证书问题
```

#### 3. 端口占用问题
```
错误: 端口7001已被占用
解决: 脚本会自动检测并提示，或使用不同端口参数
```

#### 4. 数据库连接失败
```
错误: Database Connection 测试失败
检查: 
  - SQL Server服务是否运行
  - 连接字符串是否正确
  - 数据库权限是否充足
```

### 调试技巧

1. **查看详细错误**：查看 `temp/smoke-test-results.json` 文件中的详细错误信息

2. **手动测试**：使用浏览器或Postman手动测试失败的端点

3. **检查服务状态**：
   ```powershell
   # 检查端口监听
   netstat -an | findstr "7001"
   
   # 检查进程
   tasklist | findstr "dotnet"
   ```

## 📅 使用场景

### 开发阶段
- 每次代码提交前运行快速测试
- 重要功能开发完成后运行完整测试

### 部署阶段
- 生产环境部署前验证
- 系统升级后功能检查

### 运维阶段
- 定期健康检查
- 故障排查初步诊断

### 持续集成
```yaml
# Azure DevOps Pipeline 示例
- task: PowerShell@2
  displayName: 'Run Smoke Tests'
  inputs:
    filePath: 'scripts/smoke-test.ps1'
    arguments: '-StartWebAPI $true -OutputPath "$(Agent.TempDirectory)/smoke-test-results.json"'
    
- task: PublishTestResults@2
  displayName: 'Publish Test Results'
  inputs:
    testResultsFormat: 'JUnit'
    testResultsFiles: '$(Agent.TempDirectory)/smoke-test-results.json'
```

## 🎯 最佳实践

1. **定期执行**：建议每天至少运行一次完整冒烟测试

2. **自动化集成**：将测试集成到CI/CD流水线中

3. **结果监控**：设置通过率阈值告警机制

4. **日志保存**：保留历史测试报告用于趋势分析

5. **环境隔离**：在不同环境中使用不同的配置参数

## 📚 相关文档

- [API文档](../api/) - 详细的API接口说明
- [部署指南](../deployment/) - 系统部署相关文档
- [故障排除](../troubleshooting/) - 常见问题解决方案
- [开发规范](./DEVELOPMENT_STANDARDS.md) - 开发规范和约定

---

> 💡 **提示**: 如有问题或建议，请参考 [故障排除文档](../troubleshooting/) 或联系开发团队。