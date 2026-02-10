# Research Findings: 需求文档深化

## 代码调研关键发现

### 1. 本地模式实现完整度 (颠覆性发现)
- **所有 5 个 LocalXxxDataSource 方法覆盖率均为 100%**
- LocalUserDataSource 已完整实现 (此前假设不存在)
- LocalAuthService: BCrypt 登录 + 密码修改 + 5次锁定/15分钟
- LocalDbContext: 9 个 DbSet，与远程 100% 对齐

### 2. 同步模块
- 仅支持 Herb / Patient / Formula (硬编码 switch)
- MedicalCase / User 同步: 无代码支持
- 冲突解决: 手动 (SyncConflictDialog)
- 无网络状态检测

### 3. 打印模块
- 批量打印已实现 (BatchPrintAsync)
- PDF 导出不支持 (降级为 XPS)
- 诊所信息硬编码 ("中医门诊")

### 4. 价格模型
- PrescriptionItem.UnitPrice = 快照值
- 药材价格变更不影响已有处方
- FormulaHerbItem 不含价格字段

### 5. 医案编号
- 格式: MC + yyyyMMdd + 3位序号
- 本地生成，查当天记录数 + 1
- CaseNumber 非唯一约束，Guid Id 为真正标识

## 回填完成统计
- 回填文件: 11 个
- 原始标记: 57 处 (待讨论 + TBD + 待扩展)
- 回填后残留: 0 处
- 决策记录: 24 处"已确定"标记

## 决策产出
- 设计文档: docs/plans/2026-02-10-requirements-deepening-design.md (22 决策点)
- 实施计划: docs/plans/2026-02-10-requirements-deepening-plan.md (12 Tasks)

---
*Updated: 2026-02-10 (FINAL)*
