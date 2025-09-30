# 2025-09-24 Desktop 构建警告治理一期任务

## 背景
`LYBT.Desktop.sln -c Release` 当前仍产生约 2,090 个警告，主要集中在：
- CS1591：公共类型缺少 XML 文档注释（Core/Shell/Modules 全面存在）
- CS0067：未使用事件定义（SessionManager 等）
- 其他次要警告（异步/可空）暂不在本期范围

之前的“构建优化”仅通过 `dotnet clean` 暂时消除缓存，并未解决真实警告。需开展正式治理，建立基线并制定修复计划。

## 目标
1. 统计 Desktop 项目中 CS1591、CS0067 警告的基线数据，按项目/文件列出 Top 警告源。
2. 制定分阶段的清理计划，优先处置 Core 与 Shell 模块的公共 API。
3. 提交整改方案与验收标准，为后续警告归零/CI 门槛打基础。

## 工作项
1. **基线统计**
   - `dotnet build LYBT.Desktop.sln -c Release /clp:NoSummary /warnaserror- > desktop-warnings.log`
   - 提取 CS1591、CS0067 警告行，统计排序（按项目/文件/数量）
   - 在回传文档中附表列出：项目、警告类型、主要文件数量

2. **重点范围确认**
   - 锁定处理优先级：`LYBT.Desktop.Core`、`LYBT.Desktop.Shell`、关键 Workstation 模块
   - 明确 CS0067 是否可通过移除事件或添加 `#pragma warning disable`（注记理由）治理

3. **整改计划**
   - 拟定分批修复方案（例如：第 1 批 Core/Shell 注释完善，第 2 批 Modules，第 3 批 Workstation）
   - 约定验收方式：`dotnet build` 警告数目标、代码审查点

4. **文档输出**
   - `docs/tasks/completed/2025-09-24-desktop-warnings-phase1-summary.md`：包含基线数据、治理计划、短期行动项

## 验收标准
- 基线统计表完整，能对准具体项目/文件
- 提出可执行的分阶段清理方案，明确责任模块、警告类型、预期减少量
- 上述输出附在总结文档中，供后续任务参考

## 风险提示
- XML 注释补齐可能涉及大量代码改动，需要协调是否采用文档生成模板或通过减少公共可见性来达到目的
- 未使用事件需确认是否将来会启用，避免直接删除引发功能缺失
- 警告归零若涉及可空类型/异步逻辑，需计划后续阶段逐步处理

---
文件：docs/tasks/pending/2025-09-24-desktop-warnings-phase1-task.md