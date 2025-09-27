# 后端测试覆盖率报告（2025-09-22）

- 范围：`LYBT.Server.sln`（后端模块）
- 责任：研发/测试（联合）
- 生成方式：Coverlet + ReportGenerator（Cobertura + HTML）

## 运行指令（可复制）
```bash
# 1) 还原+构建
dotnet restore LYBT.Server.sln
dotnet build LYBT.Server.sln -c Release --no-restore

# 2) 测试 + 覆盖率
dotnet test tests -c Release \
  --collect:"XPlat Code Coverage" \
  --results-directory BIN/TestResults \
  --settings .runsettings

# 3) 报告生成
reportgenerator \
  -reports:BIN/TestResults/**/coverage.cobertura.xml \
  -targetdir:BIN/TestResults/coverage \
  -reporttypes:Html;Cobertura
```

## 产物位置
- Cobertura：`BIN/TestResults/**/coverage.cobertura.xml`
- HTML：`BIN/TestResults/coverage/index.html`

## 指标总览（占位）
- Line Coverage（总体）：— %（目标 ≥ 90%）
- Branch Coverage（总体）：— %（目标 ≥ 80%）
- 关键模块（目标 ≥ 95%）：
  - Auth：— %
  - Users：— %
  - Prescriptions：— %
  - MedicalCase：— %

> 注：首次生成后，将以上“— %”以实际值替换，并保存该报告版本。

## 发现与结论（占位）
- 低覆盖热点：…
- 框架/集成替身建议：SQLite In-Memory 替代 EF InMemory 以匹配关系型行为
- 异常与边界路径：使用 FluentAssertions + Verify 快照补强

## 后续建议
- 在 CI 中将该报告持久化为构建产物；关键阈值仅提醒，不阻断（Phase 2.1）
- Phase 3 可考虑将关键模块阈值提升并纳入阻断门禁

