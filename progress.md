# Progress Log

## Session: 2026-02-18 设计文档覆盖问题修复

### Phase 0: 任务清单生成 - complete
- [x] 基于审查结果 (findings.md) 生成 6 Phase / 45 任务清单

### Phase 1: dual-mode.md CRITICAL 修复 - complete
- 变更文件: `docs/03-architecture/dual-mode.md` (+40 行)

### Phase 2: server.md 设计补全 - complete
- 变更文件: `docs/03-architecture/server.md` (+180 行)

### Phase 3: desktop.md 设计补全 - complete
- 变更文件: `docs/03-architecture/desktop.md` (+386 行)

### Phase 4: API Reference 错误码补全 - partial
- 变更文件: 7 个 API reference 文档 (+229 行)
- 剩余: patients check-reference + sync 客户端码

---

## Session: 2026-02-19 会话恢复 + 完成

### 上下文恢复
- [x] session-catchup 执行
- [x] 进度对齐: Phase 1-3 complete, Phase 4 partial, Phase 5-6 pending

### Phase 4 收尾 - complete
- [x] patients.md: 新增 check-reference + batch-check-reference 端点定义 (FR-PAT-011/012)
- [x] sync.md: 补充客户端错误码 (ERR-70501~70505)

### Phase 5: 补充修复 - complete
- [x] data-model.md: MC-D06 筛选唯一索引 (BR-001 Active-only)
- [x] data-model.md: MC-D07 禁用药材显示规则 "(已停用)"
- [x] data-model.md: MC-D14 处方总价计算公式
- [x] dual-mode.md: TBD-01 补充不活跃超时 15分钟
- [x] users.md API: toggle-status 补充 USER-D03 最后管理员保护

### Phase 6: 验证 - complete
- [x] 交叉引用检查: 发现 40 个错误路径 (../../02-requirements/ -> ../02-requirements/)
- [x] 批量修复: 10 个文件的链接路径
- [x] 覆盖率抽检: 8 MISSING + 14 PARTIAL + 2 DISCREPANCY 全部验证通过
- [x] planning files 最终更新

### 变更文件汇总

| 文件 | 变更类型 | Session |
|------|----------|---------|
| docs/03-architecture/dual-mode.md | 新增 MedicalCase 同步 + 超时标注 + 链接修复 | 18+19 |
| docs/03-architecture/server.md | 新增运维安全章节 (9节) + 链接修复 | 18+19 |
| docs/03-architecture/desktop.md | 新增 14 设计章节 + 链接修复 | 18+19 |
| docs/03-architecture/data-model.md | 索引/显示规则/价格公式 | 19 |
| docs/04-api-reference/patients.md | 错误码 + check-reference 端点 + 链接修复 | 18+19 |
| docs/04-api-reference/sync.md | 错误码 (服务端+客户端) + 链接修复 | 18+19 |
| docs/04-api-reference/users.md | 错误码 + toggle-status 业务规则 + 链接修复 | 18+19 |
| docs/04-api-reference/herbs.md | 错误码 + 链接修复 | 18+19 |
| docs/04-api-reference/formulas.md | 错误码 + 链接修复 | 18+19 |
| docs/04-api-reference/medical-cases.md | 错误码 + 链接修复 | 18+19 |
| docs/04-api-reference/README.md | 扩展码标注 + 链接修复 | 18+19 |
