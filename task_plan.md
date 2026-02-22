# Task Plan: 文档体系完善与优化

## Goal
修复文档体系中的断链、过时数据和模板合规缺失，使全部 6 层文档达到设计标准。

## Phases

### Phase 1: README.md 断链 + 统计修复 [complete]
- [ ] 修复 3 处断链 (system-architecture→system-overview, adr/→decisions/, 删除 security.md)
- [ ] 更新 ADR 数量 6→8，architecture 文件总数
- [ ] 更新 docs/README.md 版本号

### Phase 2: API 文档 /draft→/suspend 同步 [complete]
- [ ] 04-api-reference/README.md: 端点索引表
- [ ] 04-api-reference/medical-cases.md: 端点定义 + 描述文字
- [ ] 两个文件的变更记录更新

### Phase 3: 05-development FAQ 补全 [complete]
- [ ] README.md: 添加常见问题章节
- [ ] code-standards.md: 添加常见违规案例
- [ ] patterns.md: 添加常见陷阱/反模式
- [ ] testing.md: 添加常见测试问题
- [ ] (setup.md 已有 FAQ, 跳过)

### Phase 4: 06-operations 故障排查补全 [complete]
- [ ] deployment.md: 添加故障排查章节
- [ ] configuration.md: 添加常见配置问题章节
- [ ] (README.md 已有监控/健康检查, 跳过)

## Decisions
| Decision | Rationale |
|----------|-----------|
| 不添加架构文档"设计决策表" | 决策追踪在 PRD + ADR 中，不重复 |
| 03-architecture 取消 | 7 文件全部已有变更记录 |
| 04-api-reference 变更记录取消 | 10 文件全部已有变更记录 |
| FAQ 内容基于实际项目经验 | 避免空泛填充，写实用内容 |

### Phase 5: plans/ 目录归档整理 [complete]
- [x] 创建 archive/ 子目录
- [x] 移动 24 个历史文件
- [x] 创建 README.md 索引

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| (无) | | |
