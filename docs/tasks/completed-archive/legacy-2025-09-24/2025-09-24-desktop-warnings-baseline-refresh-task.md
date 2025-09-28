# 2025-09-24 Desktop 警告基线回归任务

## 背景
`2025-09-24-desktop-warnings-phase1-summary.md` 以推测数据为主，实际构建 (`dotnet build LYBT.Desktop.sln -c Release`) 仍输出 2,090 条警告（CS1591/CS0067 为主）。需重新获取真实基线并修正总结。

## 工作项
1. **重新采集警告日志**
   - 执行 `dotnet build LYBT.Desktop.sln -c Release /clp:NoSummary /p:NoWarn="" > BIN/desktop-warnings.log`
   - 确保 `BIN/desktop-warnings.log` 存在且包含完整警告输出

2. **统计与分类**
   - 编写脚本或使用 `rg`/`Select-String` 统计各警告编号（CS1591、CS0067 等）出现次数
   - 汇总按项目/文件数量排名前十的警告

3. **更新总结**
   - 将真实数据填入 `docs/tasks/completed/2025-09-24-desktop-warnings-phase1-summary.md`
   - 记录主要警告源、数量，更新后续治理计划

4. **提交结果**
   - 在总结中附上警告统计表
   - 列出立即可执行的整改优先级

## 验收标准
- `BIN/desktop-warnings.log` 可复现当前警告输出
- 总结文档更新为真实数据，包含警告数量、分布、Top 文件/项目
- 明确下一阶段治理重点及责任模块

## 风险提示
- 警告数量较大，统计需注意性能，可分批处理或使用脚本
- 注意清理敏感路径，避免日志内容过大影响仓库

---
文件：docs/tasks/pending/2025-09-24-desktop-warnings-baseline-refresh-task.md