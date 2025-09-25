# PRD 文档中心

- **维护人**：Thinker（产品需求）
- **最后更新**：2025-09-25

## 编写原则
1. **业务驱动**：所有开发任务需基于有效 PRD；未立项的需求不得进入开发。
2. **轻量高效**：保留背景、目标、范围、验收四要素，避免堆砌赘述。
3. **实时更新**：需求变更必须同步修订 PRD，并在变更记录中注明责任人、时间和影响面。

## 目录结构
| 文档 | 说明 |
|------|------|
| `PRD-template.md` | 新 PRD 模板（必填字段） |
| `PRD-summary-template.md` | 完成总结模板 |
| `README.md`（本页） | 指南与流程 |
| `historic/`（可选） | 历史 PRD（如需保留） |

> 旧的 CCPM 文档已归档到 `docs/reports/archive/ccpm/`，仅作为历史参考。

## 工作流程
1. **创建 PRD**：复制 `PRD-template.md`，命名为 `YYYY-MM-DD-<topic>-prd.md`。
2. **评审通过**：Thinker + 相关角色评审后才可发布任务。
3. **任务派发**：在 `docs/tasks/pending` 创建对应任务，正文引用 PRD。
4. **实施反馈**：开发完成由 Coder 在 `PRD-summary-template.md` 生成总结，存入 `docs/prds-summary/`。
5. **关闭与归档**：若 PRD 作废或被新版本取代，在文首注明并移动到 `historic/`。

## 推荐命名
- PRD：`2025-10-01-desktop-patient-intake-prd.md`
- 总结：`PRD-desktop-patient-intake-20251001-summary.md`
- 相关任务：`docs/tasks/pending/2025-10-01-desktop-patient-intake-task.md`

## 审阅责任
| 角色 | 责任 |
|------|------|
| Thinker | 撰写/评审 PRD，维护目录 |
| Coder | 反馈实现细节，提交总结 |
| QA | 校验验收标准，补充测试计划 |
| PM/BA | 协调需求优先级，确认业务范围 |

## 关联索引
- PRD 总结：`docs/prds-summary/`
- 任务索引：`docs/tasks/INDEX.md`
- 阶段报告：`docs/reports/INDEX.md`

如需扩展模板或流程，请先在任务中提出并获 Thinker 批准后更新本页。
