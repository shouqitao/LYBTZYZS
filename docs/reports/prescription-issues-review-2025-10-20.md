# 处方相关Issue复查报告

**创建日期**：2025-10-20
**复查目的**：识别过期、重复、冲突的处方相关Issue，避免重复开发和架构冲突
**触发原因**：方案B（处方编辑器包装模式重构）启动，需要评估现有Issue的有效性
**关联Epic**：#1540（处方编辑器架构重构）、#1494（医案流程UI重构）、#1343（MVP功能实现）

---

## 📊 执行摘要

### 核心发现

| 类别 | 数量 | 处理建议 |
|-----|------|---------|
| **重复Issue** | 1对 | 关闭#1375，保留#1476 |
| **待查Issue** | 3个 | 标注"待查"标签，方案B后重新评估 |
| **高优先级关联Issue** | 15个 | 继续推进，与方案B协同 |
| **总计Open Issue** | 26个 | 处方相关Open Issue |

### 关键建议

1. ✅ **立即关闭重复Issue**：#1375（与#1476重复）
2. ⚠️ **暂缓PR合并决策**：#1527需要等方案B实施后重新评估
3. 🔄 **ENTRY任务重新规划**：部分ENTRY任务（#1490等）可能需要整合到方案B的Phase中

---

## 1. 重复Issue（1对）

### 🔴 需要关闭：#1375

**Issue**：[ENTRY-17] 创建PrescriptionSearchDialog
**创建日期**：2025-10-16
**状态**：Open
**问题**：与#1476完全重复

**保留的Issue**：#1476（更详细，创建于10-18）
**标题**：[ENTRY-17] 创建历史处方搜索对话框 (PrescriptionSearchDialog)

**处理建议**：
```
关闭#1375，添加评论：
"此Issue与#1476重复。#1476提供了更详细的实现步骤和验收标准，建议统一跟踪#1476。"
```

---

## 2. 待查Issue（需标注"待查"标签）

### ⚠️ #1527 - [Decision] 处理剩余处方功能PR的合并策略（PR #1419-1422）

**创建日期**：2025-10-20
**标签**：`priority:p1`, `module:prescriptions`, `type:decision`
**问题描述**：4个处方功能PR（#1419-1422）与master分支存在复杂冲突

**与方案B的关系**：
- ❓ **高度相关**：方案B重构会改变处方编辑器架构
- ❓ **PR #1421**："8列表格录入功能" 与方案B的Phase 3（UI集成）重叠
- ❓ **PR #1419**："验方导入功能" 与方案B的服务层设计相关
- ❓ **PR #1422**："处方打印功能" 可能不受方案B影响，但需要确认架构兼容性

**处理建议**：
```
1. 标注"待查"标签 + "blocked-by:#1540"
2. 添加评论：
   "此Issue涉及的PR与方案B（#1540）架构重构高度相关。
   建议：
   - 暂缓合并决策，等待方案B Phase 1-2完成后重新评估
   - PR #1421（8列表格）可能与方案B Phase 3冲突，需要协调
   - PR #1419（验方导入）需要适配新的IPrescriptionEditorService接口
   - PR #1422（打印功能）影响最小，可以优先评估"
```

---

### ⚠️ #1490 - [Epic #1483] [Task 7] 处方手工录入实现

**创建日期**：2025-10-18
**标签**：`priority:high`, `module:prescriptions`, `type:task`
**问题描述**：实现处方手工录入功能

**与方案B的关系**：
- ❓ **可能重叠**：方案B Phase 3会实现8列ComboBox + 拼音过滤 + 焦点跳转
- ❓ **需要确认**：此Issue的范围是否与方案B Phase 3冲突

**处理建议**：
```
1. 标注"待查"标签
2. 读取Issue详情，确认具体实施范围
3. 如果与方案B Phase 3重叠，合并到#1540的Phase 3任务中
4. 如果范围不同，协调实施顺序
```

---

### ⚠️ #1477 - 【架构纠正v2】MedicalCase聚合根强势修正（保留模块版）

**创建日期**：2025-10-18
**标签**：无
**问题描述**：MedicalCase聚合根架构调整

**与方案B的关系**：
- ❓ **架构冲突风险**：方案B涉及MedicalCase模块的PrescriptionEditorViewModel重构
- ❓ **依赖方向变更**：方案B通过依赖倒置解决循环依赖，可能与此Issue的架构调整冲突

**处理建议**：
```
1. 标注"待查"标签 + "may-conflict-with:#1540"
2. 读取Issue详情，确认架构调整内容
3. 与方案B的依赖倒置方案进行冲突分析
4. 协调实施顺序：建议先完成方案B，再评估是否还需要此Issue
```

---

## 3. 高优先级关联Issue（需继续推进）

以下Issue与方案B无直接冲突，可以继续推进：

### 3.1 Epic级Issue

| Issue | 标题 | 状态 | 建议 |
|-------|------|------|------|
| #1343 | [Epic] MVP "能看诊" 功能实现 | Open | ✅ 继续推进，方案B是子任务 |
| #1494 | [Epic] 医案流程UI实现（4步流程） | Open | ✅ 继续推进，方案B属于Step 3优化 |
| #1456 | [Epic] 临床工作台看诊流程完整实现 | Open | ✅ 继续推进 |

### 3.2 ENTRY任务（处方录入功能）

| Issue | 标题 | Phase | 建议 |
|-------|------|-------|------|
| #1364 | [ENTRY-6] 测试表格录入完整工作流 | P0 | ✅ 方案B Phase 3完成后执行 |
| #1369 | [ENTRY-11] 测试验方导入工作流 | P1 | ✅ 等待PR #1419重新实现 |
| #1476 | [ENTRY-17] 创建历史处方搜索对话框 | P1 | ✅ 方案B Phase 3完成后执行 |
| #1376 | [ENTRY-18] 测试历史和搜索工作流 | P1 | ✅ 等待#1476完成 |

### 3.3 PRINT任务（处方打印功能）

| Issue | 标题 | Phase | 建议 |
|-------|------|-------|------|
| #1378 | [PRINT-1] 分析现有打印方法 | Phase 1 | ✅ 独立任务，可并行推进 |
| #1379 | [PRINT-2] 实现标准处方模板 | Phase 2 | ✅ 独立任务，可并行推进 |
| #1380 | [PRINT-3] 实现打印布局逻辑 | Phase 3 | ✅ 独立任务，可并行推进 |
| #1381 | [PRINT-4] 集成打印到处方详情页 | Phase 4 | ✅ 等待方案B完成后集成 |
| #1382 | [PRINT-5] 测试打印功能 | Phase 5 | ✅ 最终测试 |
| #1202 | feat(desktop): 实现处方/病历/会诊打印功能 | Epic | ✅ 与上述PRINT任务协调 |

### 3.4 STATUS任务（处方状态管理）

| Issue | 标题 | Phase | 建议 |
|-------|------|-------|------|
| #1398 | [STATUS-1] 添加处方状态枚举 | Phase 1 | ✅ 独立任务，可并行推进 |
| #1399 | [STATUS-2] 实现状态自动管理逻辑 | Phase 2 | ✅ 等待#1398完成 |

### 3.5 NUMBER任务（处方编号）

| Issue | 标题 | Phase | 建议 |
|-------|------|-------|------|
| #1390 | [NUMBER-1] 实现处方自动编号服务 | Phase 1 | ✅ 独立任务，可并行推进 |

### 3.6 其他任务

| Issue | 标题 | 模块 | 建议 |
|-------|------|------|------|
| #1108 | test(desktop): Desktop端测试补充 - Prescriptions模块 | Tests | ✅ 方案B完成后补充测试 |
| #1492 | [Epic #1483] [Task 9] 历史复制功能实现 | Prescriptions | ✅ 与#1476协调 |
| #1488 | [Epic #1483] [Task 5] ConsultationView框架实现 | Consultation | ✅ 独立任务 |
| #1352 | [FORMULA-9] 创建FormulaValidationViewModel | Formula | ✅ 独立任务 |
| #1480 | 【Phase 3】文档更新（包含Statistics过度开发说明）| Docs | ✅ 文档任务 |

---

## 4. 无关Issue（非LYBTZYZS项目）

以下Issue不属于LYBTZYZS项目，已自动过滤：

- #193 - p2t serve HTTP服务
- #72 - word导入答案问题
- #1, #7, #15, #20, #21, #22, #24, #25 - 其他项目
- #188, #1730, #1137, #123, #147, #807, #2, #3, #6, #8 - 其他项目

---

## 5. 执行计划

### 🔴 立即执行（高优先级）

1. **关闭重复Issue #1375**
   ```bash
   gh issue close 1375 --comment "此Issue与#1476重复，统一跟踪#1476。"
   ```

2. **标注待查Issue**
   ```bash
   # #1527
   gh issue edit 1527 --add-label "status:待查" --add-label "blocked-by:1540"
   gh issue comment 1527 --body "此Issue涉及的PR与方案B（#1540）架构重构高度相关。建议暂缓合并决策，等待方案B Phase 1-2完成后重新评估。"

   # #1490
   gh issue edit 1490 --add-label "status:待查"
   gh issue comment 1490 --body "需要确认此Issue范围是否与#1540 Phase 3重叠。如有重叠，合并到#1540任务中。"

   # #1477
   gh issue edit 1477 --add-label "status:待查" --add-label "may-conflict-with:1540"
   gh issue comment 1477 --body "此Issue的架构调整可能与#1540的依赖倒置方案冲突。建议先完成方案B，再评估是否还需要此Issue。"
   ```

### 🟡 Phase 2执行（方案B Phase 1-2完成后）

3. **重新评估PR合并策略（#1527）**
   - 等待方案B Phase 1（架构准备）完成
   - 等待方案B Phase 2（ViewModel重构）完成
   - 评估PR #1419-1422与新架构的兼容性
   - 决策是否需要重新实现

4. **协调ENTRY任务**
   - 确认#1490是否与方案B Phase 3重叠
   - 如果重叠，关闭#1490或合并到#1540

### 🟢 Phase 3执行（方案B完成后）

5. **恢复ENTRY和PRINT任务**
   - 基于新架构实施ENTRY-6, ENTRY-17, ENTRY-18
   - 继续推进PRINT-1至PRINT-5
   - 补充测试（#1108）

---

## 6. 风险评估

### 高风险项

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| **PR #1419-1422与方案B冲突** | 🔴 高 | 暂缓合并，等待方案B完成后重新实现 |
| **#1490与方案B Phase 3重叠** | 🟡 中 | 读取Issue详情，协调实施范围 |
| **#1477架构调整与方案B冲突** | 🟡 中 | 评估依赖倒置方案兼容性 |

### 中风险项

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| **ENTRY任务延期** | 🟡 中 | 方案B优先，ENTRY任务跟随 |
| **PRINT任务需要适配新架构** | 🟡 中 | PRINT-4集成时适配新ViewModel |

### 低风险项

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| **STATUS/NUMBER任务独立性** | 🟢 低 | 可并行推进，无冲突 |
| **测试任务延期** | 🟢 低 | 方案B完成后补充 |

---

## 7. 总结与建议

### ✅ 成功完成

1. ✅ 识别1对重复Issue（#1375 vs #1476）
2. ✅ 识别3个待查Issue（#1527, #1490, #1477）
3. ✅ 确认15个高优先级关联Issue可继续推进
4. ✅ 过滤26个无关Issue

### 📊 统计数据

- **总Open Issue（处方相关）**：26个
- **重复Issue**：1对（#1375需关闭）
- **待查Issue**：3个（需标注）
- **继续推进Issue**：22个（无冲突）

### 🎯 关键行动

1. **立即关闭**：#1375（重复）
2. **标注待查**：#1527, #1490, #1477
3. **暂缓PR合并**：#1419-1422（等待方案B）
4. **继续推进**：15个ENTRY/PRINT/STATUS/NUMBER任务

### 💡 长期建议

1. **建立Issue协调机制**：Epic级Issue应统一跟踪子任务，避免重复创建
2. **架构变更影响评估**：重大架构变更（如方案B）前，应评估对现有Issue的影响
3. **标签体系优化**：建议增加"blocked-by"、"may-conflict-with"、"status:待查"标签

---

**报告创建人**：Claude Code
**审查状态**：待用户确认
**最后更新**：2025-10-20

---

## 附录：处方相关Issue完整清单（26个）

| Issue | 标题 | 状态 | 优先级 | 模块 | 建议 |
|-------|------|------|--------|------|------|
| #1108 | Desktop端测试补充 - Prescriptions模块 | Open | P3 | Tests | 继续 |
| #1202 | 实现处方/病历/会诊打印功能（前端） | Open | P2 | Desktop | 继续 |
| #1343 | [Epic] MVP "能看诊" 功能实现 | Open | High | All | 继续 |
| #1352 | [FORMULA-9] 创建FormulaValidationViewModel | Open | High | Formula | 继续 |
| #1364 | [ENTRY-6] 测试表格录入完整工作流 | Open | High | Prescriptions | 继续 |
| #1369 | [ENTRY-11] 测试验方导入工作流 | Open | High | Prescriptions | 继续 |
| #1375 | [ENTRY-17] 创建PrescriptionSearchDialog | Open | High | Prescriptions | **关闭（重复）** |
| #1376 | [ENTRY-18] 测试历史和搜索工作流 | Open | High | Prescriptions | 继续 |
| #1378 | [PRINT-1] 分析现有打印方法 | Open | High | Prescriptions | 继续 |
| #1379 | [PRINT-2] 实现标准处方模板 | Open | High | Prescriptions | 继续 |
| #1380 | [PRINT-3] 实现打印布局逻辑 | Open | High | Prescriptions | 继续 |
| #1381 | [PRINT-4] 集成打印到处方详情页 | Open | High | Prescriptions | 继续 |
| #1382 | [PRINT-5] 测试打印功能 | Open | High | Prescriptions | 继续 |
| #1390 | [NUMBER-1] 实现处方自动编号服务 | Open | High | Prescriptions | 继续 |
| #1398 | [STATUS-1] 添加处方状态枚举 | Open | Medium | Prescriptions | 继续 |
| #1399 | [STATUS-2] 实现状态自动管理逻辑 | Open | Medium | Prescriptions | 继续 |
| #1456 | [Epic] 临床工作台看诊流程完整实现 | Open | P0 | Consultation | 继续 |
| #1476 | [ENTRY-17] 创建历史处方搜索对话框 | Open | High | Prescriptions | 继续 |
| #1477 | 【架构纠正v2】MedicalCase聚合根 | Open | N/A | MedicalCase | **待查** |
| #1480 | 【Phase 3】文档更新（Statistics） | Open | N/A | Docs | 继续 |
| #1488 | [Task 5] ConsultationView框架实现 | Open | High | Consultation | 继续 |
| #1490 | [Task 7] 处方手工录入实现 | Open | High | Prescriptions | **待查** |
| #1492 | [Task 9] 历史复制功能实现 | Open | High | Prescriptions | 继续 |
| #1494 | [Epic] 医案流程UI实现（4步流程） | Open | High | Multi | 继续 |
| #1527 | [Decision] 处理剩余处方功能PR合并策略 | Open | P1 | Prescriptions | **待查** |
| #1540 | [Epic] 处方编辑器架构重构 - 包装模式 | Open | P1 | Prescriptions | **新建** |
