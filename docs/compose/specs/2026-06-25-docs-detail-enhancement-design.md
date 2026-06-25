# 文档完善设计文档

> 日期: 2026-06-25 | 状态: Draft | 范围: 全量文档细节补充

## [S1] 问题定义

项目已有 ~96 个文档文件，结构完整但细节不足。主要问题：

1. **事实错误**：5 处文档与代码不一致（MVVM 框架名、JWT 时效、响应格式、BaseReadRepository 引用、Receptionist 药材权限）
2. **缺失内容**：API 示例不完整、架构图缺失、代码示例过少、故障排查为零、验收标准模糊
3. **交叉矛盾**：6 处跨文档数据不一致（US 数量、Token 时效、备份保留期、RTO、日志路径、权限定义）

## [S2] 完善范围

### 2.1 事实错误修复（5 处，立即修复）

| 文件 | 行号 | 问题 | 修复 |
|------|------|------|------|
| `05-development/03-code-standards.md` | 72 | 写 Prism `BindableBase` | 改为 CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]` |
| `05-development/04-patterns.md` | 139 | ViewModel 示例用旧 Prism | 改为 `CoreViewModelBase`/`NavigableViewModelBase` |
| `05-development/standards/STD-06-JWT-Security.md` | 12,45 | Token 时效 15min/7d | 改为 30min/7d+30d（与 09-security-architecture.md 一致） |
| `03-architecture/localwebapi/api-endpoints.md` | 238 | 说 LocalWebAPI 不用 ApiResponse | 改为"使用 ApiResponse<T> 包装"（与 AGENTS.md 一致） |
| `03-architecture/decisions/0001` | 23 | 引用已删除的 BaseReadRepository | 移除或更新引用 |

### 2.2 交叉矛盾修复（6 处）

| 矛盾 | 文件 A | 文件 B | 统一值 |
|------|--------|--------|--------|
| US 数量 136 vs 128 | `01-prd.md:11` | `01-prd.md:91,239` | 重新计数，统一为实际值 |
| AccessToken 2h vs 8h | `12-nfr.md:157` | `02-configuration.md:35` | 统一为 30min（生产） |
| 备份保留 30d vs 7d | `12-nfr.md:108` | `07-backup-recovery.md:62` | 统一为 7d（合理值） |
| RTO 30min vs 1-4h | `12-nfr.md:124` | `07-backup-recovery.md:226` | 统一为 1h |
| 日志路径 LOCALAPPDATA vs APPDATA | `11-platform.md:541` | `01-deployment.md:78` | 统一为实际路径 |
| Receptionist 药材权限 | `03-users.md:86` | `05-herbs.md:49` | 统一为 DoctorOrReceptionist |

### 2.3 API 参考补充（04-api-reference/）

每个端点补充：
- 完整请求 JSON 示例（含必填/选填字段标注）
- 完整响应 JSON 示例（成功 + 失败）
- curl 命令示例
- 错误码表

优先级：`01-auth.md` → `06-medical-cases.md` → `02-users.md` → 其余

### 2.4 架构文档补充（03-architecture/）

| 文件 | 补充内容 |
|------|---------|
| `01-system-overview.md` | Mermaid 整体架构图 |
| `02-desktop.md` | MVVM 组件关系图、导航流程图 |
| `03-server.md` | 请求生命周期时序图（Controller→Service→Repository→DbContext） |
| `04-data-model.md` | Mermaid ER 图 |
| `05-dual-mode.md` | 模式切换流程图、数据流对比图 |
| `06-error-handling.md` | 错误传播流程图 |
| `09-security-architecture.md` | 登录时序图（Mermaid 替换 ASCII）、JWT 验证流程图 |
| `10-printing-architecture.md` | 渲染管线流程图、分页逻辑图 |
| `decisions/` | 新增 ADR: Mapperly 迁移、CommunityToolkit.Mvvm 采用 |

### 2.5 开发指南补充（05-development/）

| 文件 | 补充内容 |
|------|---------|
| `01-setup.md` | 补充 git clone URL、默认密码、.env 配置、端口说明、常见问题扩展 |
| `03-code-standards.md` | 补充 Repository/Service/Mapperly/FluentValidation 代码示例 |
| `04-patterns.md` | 补充 6 个缺失模式的代码示例（状态机、事件通信、SwitchingApiClient 等） |
| `05-testing.md` | 补充完整集成测试示例、ServerFixture 用法、Respawn 配置 |
| `12-testing-standards.md` | 补充断言 Helper 实现、Builder 模式实现、CI 集成指南 |

### 2.6 运维文档补充（06-operations/）

| 文件 | 补充内容 |
|------|---------|
| `01-deployment.md` | 补充环境变量表、Postman 健康检查验证、IIS 配置要点 |
| `02-configuration.md` | 补充 ConnectionStrings 示例、FeatureToggles、ClinicSettings、环境变量覆盖语法 |
| `07-backup-recovery.md` | 补充 PowerShell 自动备份脚本、异地备份策略、恢复验证步骤 |
| `08-monitoring-alerting.md` | 补充 SQL 监控查询、告警升级流程、通知渠道配置 |
| `09-deployment-rollback.md` | 补充回滚后验证清单、数据库迁移回退策略 |

### 2.7 需求文档补充（02-requirements/）

每个模块补充：
- 边界条件和异常场景的验收标准
- 跨模块交互的边界 case
- 错误码定义和 HTTP 状态码映射

重点修复：
- `03-users.md`：Receptionist 药材权限矛盾
- `05-herbs.md`：引用检查实体名修正（Prescription → PrescriptionItem）
- `07-medical-cases.md`：DosageCount 上限、医案号溢出处理
- `12-nfr.md`：Token 时效、备份保留期、RTO 统一

## [S3] 实施策略

### 分批执行

| 批次 | 范围 | 估计工作量 |
|------|------|-----------|
| **Batch 1** | 事实错误修复 + 交叉矛盾修复（共 11 处） | 小 |
| **Batch 2** | API 参考补充（12 个文件，~106 个端点示例） | 大 |
| **Batch 3** | 架构文档补充（10 个文件 + 2 个新 ADR） | 中 |
| **Batch 4** | 开发指南补充（5 个文件） | 中 |
| **Batch 5** | 运维文档补充（5 个文件） | 中 |
| **Batch 6** | 需求文档补充（10 个文件） | 中 |

### 每个 Batch 的流程

1. 读取目标文件
2. 读取对应源代码（验证事实）
3. 补充/修正内容
4. 交叉引用检查（确保新内容与相关文档一致）
5. 提交

## [S4] 质量标准

- 所有代码示例必须可编译/可运行
- 所有 JSON 示例必须与 ApiResponse<T> 格式一致
- 所有 Mermaid 图必须语法正确
- 所有跨文档引用必须一致
- 每个文档底部变更记录表更新
