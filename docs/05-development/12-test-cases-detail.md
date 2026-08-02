# 测试用例详情

## P0 测试用例（阻断发布）

### TC-001: C1 模块加载修复验证

**前置条件**：Doctor 账户已创建
**步骤**：
1. 以 Doctor 身份登录
2. 检查导航栏是否显示 6 个模块（Patients/Herbs/Formula/MedicalCase/Registration/CardReader）
3. 检查首页是否为 ClinicalWorkspace

**预期**：Doctor 加载 6 个模块，首页为 ClinicalWorkspace
**关联**：US-SHELL-003, D1 决策

### TC-002: D1 审计日志字段级追踪

**前置条件**：已有 Active 医案
**步骤**：
1. 修改医案的 Consultation 字段（如 TcmDiagnosis）
2. 调用 GET `/api/v1/medicalcases/{id}/audit-logs`
3. 验证返回的 ChangedFields 包含实际变更字段

**预期**：审计日志记录 20 字段 diff，含操作人/时间/原因
**关联**：US-MC-017, D1 决策

### TC-003: D2 打印回写验证

**前置条件**：已有 Completed 医案
**步骤**：
1. 打印处方
2. 调用 GET `/api/v1/medicalcases/{id}`
3. 验证 IsPrinted=true, PrintVersion=1, PrintCount=1

**预期**：打印后字段正确回写
**关联**：US-PRINT-004, D2 决策

### TC-004: D3 限流验证

**步骤**：
1. 连续 6 次错误密码登录
2. 第 6 次应返回 429 Too Many Requests

**预期**：超限后返回 429
**关联**：US-AUTH-013, D3 决策

### TC-005: D5 引用检查验证

**前置条件**：患者有关联医案
**步骤**：
1. 尝试删除该患者
2. 验证返回 422 + 引用检查错误

**预期**：被引用的患者不可删除
**关联**：BR-DEL-001, D5 决策

### TC-006: D7 权限修复验证

**前置条件**：Receptionist 账户已创建
**步骤**：
1. 以 Receptionist 登录
2. 尝试创建挂号
3. 验证操作成功

**预期**：Receptionist 可创建挂号
**关联**：D7 决策

## P1 测试用例

### TC-007: D4 Restore 验证

**前置条件**：已软删除患者
**步骤**：
1. 以 SuperAdmin 登录
2. 调用 POST `/api/v1/patients/{id}/restore`
3. 验证患者恢复为正常状态

**预期**：软删除可恢复
**关联**：D4 决策

### TC-008: D9 历史聚合验证

**前置条件**：患者有 3 个已完成医案
**步骤**：
1. 以 Doctor 登录
2. 调用 GET `/api/v1/medicalcases/patient/{patientId}/history`
3. 验证返回 3 条历史记录

**预期**：返回跨医案的历史聚合数据
**关联**：US-MC-008/009, D9 决策
