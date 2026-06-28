# 第三阶段：遗留问题修复设计

> 日期: 2026-06-25 | 状态: Draft | 范围: 文档审计发现的遗留代码+文档问题

## [S1] 问题定义

前两阶段完成后，仍有以下遗留问题：

1. **ReportsController 无文档**：3 个报表端点存在但 API 参考文档中无对应文件
2. **新策略未应用**：`AdminOrSuperAdmin` 和 `DoctorOrReceptionist` 已注册但未应用到任何 Controller
3. **/auto-login 端点未实现**：DTO `AutoLoginRequest` 已存在但 Controller 未实现

## [S2] 修复范围

### 2.1 新增 Reports API 文档

**文件**：创建 `docs/04-api-reference/13-reports.md`

内容：3 个端点的完整请求/响应示例、curl 命令

### 2.2 应用新权限策略

将以下 Controller 的权限策略更新为更精确的匹配：

| Controller | 当前策略 | 建议策略 | 原因 |
|-----------|---------|---------|------|
| UsersController (部分端点) | AdminOnly | AdminOrSuperAdmin | sysadmin 应能管理所有用户 |
| DiagnosticsController | AdminOnly | AdminOrSuperAdmin | sysadmin 应能访问诊断 |
| ConfigurationController | AdminOnly | AdminOrSuperAdmin | sysadmin 应能管理配置 |

### 2.3 实现 /auto-login 端点（可选）

**决策**：需要确认是否为计划功能。DTO 已存在但 Controller 未实现。

## [S3] 实施策略

| 批次 | 范围 | 优先级 |
|------|------|--------|
| **Batch 1** | Reports API 文档 | 高 |
| **Batch 2** | 应用新权限策略 | 高 |
| **Batch 3** | /auto-login 端点（如确认需要） | 中 |

## [S4] 质量标准

- 新文档遵循现有 API 参考格式
- 策略变更不破坏现有权限模型
- 每个修改附带测试验证
