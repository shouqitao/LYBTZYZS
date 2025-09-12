# P3 Record-Only Smoke Validation Scripts

## 概述

本目录包含P3冒烟验证所需的自动化脚本，用于验证删减后系统在Record-Only（CRUD + 历史查询）基线下的完整运行能力。

## 脚本清单

### 1. run-webapi.ps1 - WebAPI服务启动脚本

**用途**: 启动本地WebAPI服务，为冒烟测试提供稳定的后端API

**功能特性**:
- 自动检测并停止现有WebAPI进程
- 可选的项目清理和重建
- 智能等待服务就绪（健康检查）
- 实时进程监控和日志记录
- 优雅的Ctrl+C停止处理

**使用方法**:
```powershell
# 基础启动（推荐）
.\run-webapi.ps1

# 清理后启动
.\run-webapi.ps1 -Clean

# 重建后启动
.\run-webapi.ps1 -Rebuild

# 自定义超时时间
.\run-webapi.ps1 -TimeoutSeconds 120
```

**服务地址**:
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5001
- 健康检查: https://localhost:7001/api/v1/health
- Swagger文档: https://localhost:7001/swagger

### 2. smoke.ps1 - API冒烟测试脚本

**用途**: 执行自动化API冒烟测试，验证4个核心模块的CRUD操作

**功能特性**:
- 按模块顺序执行测试（Herbs → Formula → Patients → Consultation → Prescriptions）
- 完整的数据生命周期测试（创建→查询→更新→删除）
- 详细的测试结果记录和错误报告
- 支持并发测试和性能监控
- 自动数据清理（测试后）

**使用方法**:
```powershell
# 执行完整冒烟测试
.\smoke.ps1

# 仅测试特定模块
.\smoke.ps1 -Modules @("Herbs", "Patients")

# 跳过数据清理
.\smoke.ps1 -SkipCleanup

# 详细输出模式
.\smoke.ps1 -Verbose
```

### 3. test-matrix.ps1 - 测试矩阵验证脚本

**用途**: 运行架构测试和单元测试，确保系统合规性

**功能特性**:
- 执行ArchTests架构合规性测试
- 运行核心业务模块单元测试
- 生成测试覆盖率报告
- 验证Record-Only模式合规性

**使用方法**:
```powershell
# 运行完整测试矩阵
.\test-matrix.ps1

# 仅运行架构测试
.\test-matrix.ps1 -ArchOnly

# 包含覆盖率报告
.\test-matrix.ps1 -Coverage
```

## 执行顺序

**标准冒烟验证流程**:

1. **启动WebAPI服务**:
   ```powershell
   .\run-webapi.ps1
   ```

2. **执行API冒烟测试**（新终端窗口）:
   ```powershell
   .\smoke.ps1
   ```

3. **运行测试矩阵验证**:
   ```powershell
   .\test-matrix.ps1
   ```

4. **检查验证结果**:
   - API测试报告: `api-smoke-results.json`
   - 测试矩阵报告: `test-matrix-results.json`
   - 详细日志: `webapi-startup.log`

## 输出文件

验证过程中生成的文件：

```
scripts/validation/
├── webapi-startup.log          # WebAPI启动日志
├── api-smoke-results.json      # API冒烟测试结果
├── test-matrix-results.json    # 测试矩阵验证结果
├── smoke-test-data.json        # 测试期间创建的数据记录
└── validation-summary.md       # 最终验证总结报告
```

## 故障排除

### WebAPI启动失败
- 检查端口7001/5001是否被占用
- 确认SQL Server服务正在运行
- 查看webapi-startup.log获取详细错误信息

### API测试失败
- 确认WebAPI服务正常运行（健康检查通过）
- 检查数据库连接和初始数据
- 查看API返回的具体错误信息

### 架构测试失败
- 检查是否有超范围功能残留
- 确认所有智能推荐相关代码已清除
- 运行完整清理构建 `dotnet clean && dotnet build`

## Record-Only模式验证要点

冒烟测试专注验证以下Record-Only基线功能：

### ✅ 允许的功能
- 患者档案基础CRUD操作
- 诊断记录四诊数据录入
- 处方开具和药材组合
- 中药材信息管理
- 验方模板管理
- 历史记录查询和分页
- 数据导入导出功能

### ❌ 禁止的功能
- 智能推荐和AI辅助功能
- 配伍检查和自动验证
- 规则引擎和决策支持
- 复杂工作流和自动化
- 高级统计分析功能

## 联系信息

- 验证计划: `_reports/2025-09/validation/smoke-plan.md`
- UI检查清单: `_reports/2025-09/validation/ui-smoke-checklist.md`
- 脚本问题反馈: 请查看相应的日志文件