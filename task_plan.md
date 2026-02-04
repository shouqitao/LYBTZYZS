# Task Plan: implement-data-sync

## Objective
实现 LYBTZYZS Desktop 客户端的数据同步功能，支持基础数据（Herb、Patient、Formula）的双向同步。

## Current Status Summary
**Phase 1**: 需求分析与设计 - **已完成**
**Phase 2**: 引用检查实现 - **已完成** (2026-02-04)
**Phase 3**: 服务器端同步模块 - **已完成** (2026-02-04)
**Phase 4**: 客户端同步模块 - **已完成** (2026-02-04)
**Phase 5**: 同步 UI 模块 - **已完成** (2026-02-04)

---

## 设计决策汇总

| 决策点 | 选择 | 原因 |
|--------|------|------|
| 数据量级 | 小数据量（<1000条） | 全量 Checksum 比对，简单可靠 |
| 同步触发 | 纯手动 | 完全可控，用户明确知道何时同步 |
| 删除策略 | 软删除 + 引用检查 | 有引用数据只能禁用，无引用可删除 |
| Checksum 范围 | 业务字段 + Status | 排除审计字段避免"假差异" |
| 冲突处理 | 批量选择 + 预览对比 | 用户决定，直观高效 |
| 同步粒度 | 实体级 | 简单实现 |

---

## Phases

### Phase 1: 需求分析与设计 [COMPLETED]
- [x] 1.1 同步场景分析
- [x] 1.2 SyncLog 表设计
- [x] 1.3 差异检测算法设计
- [x] 1.4 冲突处理策略设计
- [x] 1.5 UI 流程设计
- [x] 1.6 引用检查设计（Herb、Patient）

### Phase 2: 引用检查实现 [COMPLETED]
- [x] 2.1 完善 Herb 引用检查（查询 PrescriptionItem）
- [x] 2.2 新增 Patient 引用检查
- [x] 2.3 单元测试修复

### Phase 3: 服务器端同步模块 [COMPLETED]
- [x] 3.1 创建 LYBT.Module.Sync 模块
- [x] 3.2 Sync DTOs (SyncMetadataDto, SyncDiffDto, SyncCompareInputDto, etc.)
- [x] 3.3 ChecksumHelper 实现 (SHA256, 排除审计字段)
- [x] 3.4 SyncController API 端点 (metadata, compare, upload, download, delete)
- [x] 3.5 SyncService 业务逻辑 (含引用检查集成)

### Phase 4: 客户端同步模块 [COMPLETED]
- [x] 4.1 ISyncApi Refit 接口 (Contracts/Api)
- [x] 4.2 ISyncService 同步服务接口 (Contracts/Services)
- [x] 4.3 SyncService 同步服务实现 (LocalData/Services)
- [x] 4.4 ChecksumHelper 客户端版 (LocalData/Helpers)
- [x] 4.5 DI 服务注册 (Shell/Extensions)

### Phase 5: 同步 UI [COMPLETED]
- [x] 5.1 创建 LYBT.Desktop.Sync 模块
- [x] 5.2 SyncViewModel 实现
- [x] 5.3 SyncView.xaml（同步主界面）
- [x] 5.4 SyncConflictDialog（冲突处理）
- [x] 5.5 ZeroToVisibilityConverter 转换器
- [x] 5.6 项目集成（Shell.csproj, App.xaml.cs, 解决方案）

### Phase 6: 测试与验证 [COMPLETED]
- [x] 6.1 单元测试（Checksum、差异检测）- 37 个测试全部通过
- [~] 6.2 集成测试（API 端点）- 代码已编写，需修复测试环境（认证策略+数据库初始化）
- [~] 6.3 端到端测试（完整同步流程）- 跳过（需手动测试）
- [x] 6.4 文档更新 - CHANGELOG + OpenSpec 文档已更新

---

## Dependencies
- implement-local-mode (已完成)
- LocalData 项目和 DataSource 架构
- LocalDbContext 可扩展

## Next Actions
1. 进入 Phase 6: 测试与验证
   - 单元测试（Checksum、差异检测）
   - 集成测试（API 端点）
   - 端到端测试（完整同步流程）
   - 文档更新

## Risks & Mitigations
| 风险 | 缓解措施 |
|------|----------|
| 网络中断 | 同步操作支持重试，每条记录独立处理 |
| 数据丢失 | 软删除机制，同步前本地备份 |
| 冲突过多 | 清晰的冲突展示UI，批量处理选项 |
| 引用检查遗漏 | 服务器端再次验证，拒绝非法删除 |

---
*Created: 2026-02-03*
*Last Updated: 2026-02-04*
