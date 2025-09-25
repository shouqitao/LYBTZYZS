# 任务：服务器端业务服务分层与术语统一再落实

- **发布日期**：2025-09-24
- **发布人**：Thinker（ChatGPT）

## 背景
docs/reports/server-refactoring-completion-report-2025-09-24.md 宣称业务服务已通过仓储层访问并统一术语，但代码仍存在原有问题：
- 多个 BusinessService (PatientBusinessService 等) 仍直接依赖 AppDbContext。
- AppDbContext、实体注释等仍大量使用“看诊”等旧术语。

## 目标
- 将所有 BusinessService 重构为仅依赖对应的 Repository。
- 清除服务器端残留的“看诊”等旧术语，统一为“诊疗”。

## 工作内容
1. **BusinessService 重构**
   - PatientBusinessService、UserBusinessService、HerbBusinessService、FormulaBusinessService、MedicalCaseBusinessService、ConsultationBusinessService、PrescriptionBusinessService 等全部改为通过仓储接口工作。
   - 跨模块数据访问必须通过领域服务或仓储扩展实现，禁止直接注入其它 DbContext。
   - 清理事务管理代码，确保事务由仓储或上层协调。
2. **术语统一**
   - 更新 AppDbContext、实体模型、服务、控制器、错误消息中的“看诊”等旧术语。
   - 检查资源文件、文档注释，确保与 README/需求文档一致。
3. **测试与验证**
   - 补充或更新单元测试，确保服务逻辑保持不变。
4. **文档同步**
   - 更新 server-refactoring-completion-report-2025-09-24.md，记录实际改动。
   - 在 README 中注明分层规范已经落实。

## 验收标准
- 所有业务服务不再注入 AppDbContext，只使用仓储接口。
- 代码库中“看诊”等旧术语全部统一为“诊疗”。
- 测试通过，文档更新。

> 完成后请在 docs/tasks/completed/2025-09-24-server-BusinessService重构-summary.md 中总结执行情况。
