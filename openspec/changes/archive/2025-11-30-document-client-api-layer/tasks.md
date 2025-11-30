# Tasks: document-client-api-layer

## Phase 1: 创建Client API规范文档

### 1.1 创建client-api-conventions spec
- [x] 1.1.1 定义Refit接口命名规范
- [x] 1.1.2 定义批量操作模式规范
- [x] 1.1.3 定义聚合根API访问规范
- [x] 1.1.4 定义错误处理规范

### 1.2 验证现有实现
- [x] 1.2.1 验证IUserApi符合规范 -> 符合：无BatchDelete，使用标准CRUD
- [x] 1.2.2 验证IHerbApi符合规范 -> 符合：无BatchDelete，标准命名
- [x] 1.2.3 验证IFormulaApi符合规范 -> 符合：无BatchDelete，标准命名
- [x] 1.2.4 验证IMedicalCaseApi符合规范 -> 符合：聚合根模式，子资源通过父路径访问
- [x] 1.2.5 验证IPatientApi符合规范 -> 符合：标准CRUD模式

## Phase 2: 补充代码注释（跳过）

### 2.1 添加规范引用注释
- [x] 2.1.1 在IUserApi添加规范说明注释 -> 跳过：现有注释已足够清晰
- [x] 2.1.2 在IMedicalCaseApi添加聚合根说明注释 -> 跳过：现有注释已说明聚合根模式

## 验收标准

- [x] client-api-conventions spec通过validation
- [x] 所有Refit接口符合规范
- [x] spec与webapi-cleanup形成互补关系

## 完成备注

### 2025-11-30 完成

**验证结果:**

所有Refit接口均符合规范，无需代码变更。

| 接口 | 命名规范 | 批量操作 | 聚合根模式 | 状态更新 |
|------|---------|---------|-----------|---------|
| IUserApi | 符合 | 无batch | N/A | 使用Update |
| IHerbApi | 符合 | 无batch | N/A | 使用Update |
| IFormulaApi | 符合 | 无batch | N/A | N/A |
| IMedicalCaseApi | 符合 | 无batch | 符合 | 使用UpdateStatus |
| IPatientApi | 符合 | 循环删除 | N/A | N/A |

**决策:**
- Phase 2代码注释跳过，现有注释已足够清晰
- 规范文档作为主要产出，供未来开发参考
