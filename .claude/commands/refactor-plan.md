# 重构规划命令 (/refactor-plan)

使用UltraThink深度分析和规划重构任务，生成详细的实施路线图。

## 📋 执行流程

### 1️⃣ 激活UltraThink模式

使用`mcp__sequential-thinking__sequentialthinking`进行结构化分析：
- 初始思考步数：20-30步
- 可根据复杂度动态调整
- 支持分支推理和修正

### 2️⃣ 重构分析步骤

#### Step 1-5：问题识别
```
1. 读取现有代码（使用mcp__serena__read_file）
2. 识别代码异味（Code Smells）
3. 分析架构违规
4. 评估技术债务
5. 量化影响范围
```

#### Step 6-10：根因分析
```
6. 追溯问题根源
7. 分析历史演进（git log）
8. 识别设计缺陷
9. 评估修复难度
10. 确定优先级
```

#### Step 11-15：方案设计
```
11. 设计重构目标架构
12. 对比多个候选方案
13. 评估风险与收益
14. 选择最优方案
15. 设计迁移策略
```

#### Step 16-20：实施规划
```
16. 拆分Phase（4-6个Phase）
17. 定义每个Phase的验收标准
18. 评估工期
19. 识别依赖关系
20. 生成实施清单
```

### 3️⃣ 读取相关文档

**必读标准**：
- `docs/development/standards.md`
- `docs/development/minimal-practice.md`
- `docs/architecture/server-module-design-standard.md`（Server重构）
- `docs/architecture/client/unified-design-standard.md`（Desktop重构）

**参考案例**：
- `docs/reports/desktop-architecture-optimization-analysis.md`（重构分析范例）
- 历史重构Issue和PR

### 4️⃣ 生成重构计划文档

#### 文档模板
```markdown
# {模块名} 重构计划

**规划日期**：{日期}
**分析方法**：UltraThink {N}步分析
**预期工期**：{X}周

---

## 📊 执行摘要

{3-5句话总结问题、方案、收益}

---

## 一、问题识别

### 1.1 P0 - 严重问题
#### 问题1：{标题}
- **问题描述**：{详细说明}
- **代码证据**：
  \`\`\`csharp
  // src/path/to/file.cs:123
  {问题代码}
  \`\`\`
- **影响分析**：
  - 性能影响：{量化数据}
  - 维护成本：{评估}
  - 扩展性：{评估}

### 1.2 P1 - 中等问题
{同上格式}

---

## 二、根因分析

### 2.1 历史演进
{使用git log分析问题如何产生}

### 2.2 设计缺陷
{识别架构或设计层面的根本问题}

### 2.3 技术债务量化
| 指标 | 当前值 | 目标值 |
|------|--------|--------|
| 代码重复率 | XX% | <5% |
| 圈复杂度 | XX | <10 |
| 依赖深度 | XX层 | ≤3层 |

---

## 三、重构方案设计

### 3.1 目标架构
{描述重构后的理想架构}

\`\`\`
[架构图或目录结构]
\`\`\`

### 3.2 方案对比
| 方案 | 优点 | 缺点 | 风险 | 推荐度 |
|------|------|------|------|--------|
| A: {方案A} | ... | ... | ... | ⭐⭐⭐⭐⭐ |
| B: {方案B} | ... | ... | ... | ⭐⭐⭐ |

### 3.3 最终选择：方案{X}
{详细说明为什么选择此方案}

---

## 四、实施路线图

### Phase 1：{Phase标题}（Week 1-2）
**目标**：{目标描述}

**任务清单**：
- [ ] [TASK-1] {任务描述}
- [ ] [TASK-2] {任务描述}
- [ ] [TASK-3] {任务描述}

**验收标准**：
- ✅ {验收条件1}
- ✅ {验收条件2}

**产出**：
- {文件1}
- {文件2}

---

### Phase 2：{Phase标题}（Week 3-4）
{同上格式}

---

### Phase 3：{Phase标题}（Week 5-6）
{同上格式}

---

## 五、风险评估与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| 破坏现有功能 | 中 | 高 | 完善测试覆盖 |
| 工期延误 | 低 | 中 | Phase化实施 |
| 性能回归 | 低 | 高 | 基准测试 |

---

## 六、ROI分析

### 投入
- 开发时间：{X}周
- 测试时间：{Y}周
- 文档时间：{Z}周
- **总计**：{T}周

### 收益
- 性能提升：{量化}
- 维护成本降低：{量化}
- 代码质量提升：{量化}
- **ROI**：{计算}

---

## 七、后续工作

- [ ] 创建Epic Issue
- [ ] 拆分子Issue（按Phase）
- [ ] 更新架构文档
- [ ] 通知团队
```

### 5️⃣ 创建GitHub Issue

自动创建Epic Issue和子Issue：
```bash
# 创建Epic
gh issue create --title "refactor: {模块名}重构 - Epic" \
  --label "type:refactor,epic:refactor-{module}" \
  --body "{重构计划摘要}"

# 创建Phase Issues
gh issue create --title "refactor: {模块名} Phase 1 - {标题}" \
  --label "type:refactor,epic:refactor-{module},priority:p1" \
  --body "{Phase 1详细任务清单}"
```

## 🎯 使用场景

- 准备大型重构前的规划
- 架构优化的方案设计
- 技术债务的系统性清理
- 模块重组的路线图制定

## ⚡ 快速使用

### 重构整个模块
```
/refactor-plan Module.Patients
```

### 重构特定层
```
/refactor-plan Desktop.Services
```

### 针对已知问题规划
```
/refactor-plan Issue #1114
```

## 🧠 UltraThink分析示例

SuperClaude风格的深度思考流程：

```
[Thought 1/28] 读取Desktop.Services目录结构...
→ 发现：28个子目录，职责混乱

[Thought 2/28] 对比Server端模块化架构...
→ 识别：Desktop缺少模块化设计

[Thought 3/28] 分析Service层价值...
→ 结论：Service层仅做Repository包装，无业务逻辑

...

[Thought 15/28] 设计最优架构方案...
→ 方案：删除Service层，Repository下沉到各模块

...

[Thought 28/28] 生成4-Phase实施路线图
→ 预期：5-7周完成，性能提升50%+
```

## 📚 参考案例

- **Desktop架构优化**：`docs/reports/desktop-architecture-optimization-analysis.md`
  - UltraThink 28步分析
  - 5个问题识别（P0+P1）
  - 4-Phase实施路线图
  - ROI 4.3x

## 🔧 工具链

- `mcp__sequential-thinking` - 结构化推理
- `mcp__serena__find_symbol` - 代码分析
- `mcp__serena__search_for_pattern` - 模式搜索
- `git log` - 历史演进分析
- `gh issue create` - Issue创建
