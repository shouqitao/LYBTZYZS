# LYBTZYZS项目定制Skills设计方案

> **文档版本**：v1.0
> **创建日期**：2025-10-22
> **分析方法**：UltraThink（28步深度分析）
> **状态**：设计阶段

---

## 📋 执行摘要

基于项目实际痛点和工作流分析，设计4个高价值定制Skills，与现有3个Skills（mvp-compliance、arch-compliance、doc-sync）形成完整的开发生命周期覆盖。

**核心价值**：
- **时间节省**：48.4小时/年（直接节省）
- **质量提升**：45小时/年（间接收益）
- **ROI**：第一年4.6x，后续年份9.3x
- **实施成本**：15-18小时（6周完成）

---

## 🎯 设计的4个Skills

### 1. lybtzyzs-issue-template（Issue标准化创建）

**核心能力**：
- 根据任务类型生成标准化Issue模板
- 自动关联Epic/Milestone
- 生成验收标准清单
- 智能推荐相关文档和技术约束

**触发关键词**：
- 中文：创建Issue、新建任务、Issue模板、标准化Issue
- 英文：generate issue、create issue、issue template

**输入**：
- Issue类型（Feature/Bug Fix/Refactor/Documentation/Test/Chore）
- 简要描述
- 相关Epic（可选）
- 优先级（高/中/低）

**输出**：
```markdown
## 📝 任务描述
[清晰描述要做什么]

## 🎯 目标
- 业务目标
- 技术目标

## ✅ 验收标准
- [ ] 功能验收1
- [ ] 功能验收2
- [ ] 质量验收（编译通过，测试通过）

## 📐 设计方案
[架构设计、技术选型]

## 📚 参考资料
- [相关文档]
- [相关Issue]

## ⏱️ 预计工作量
X小时

## ⚠️ 技术约束
[从Constitution提取]
```

**技术实现**：
- memory（项目知识库）
- github（Issue管理）
- Read（Constitution检查）

**性能指标**：<5秒

**优先级**：⭐⭐⭐⭐⭐（立即实施）

---

### 2. lybtzyzs-code-review（代码规范自动审查）

**核心能力**：
- 自动检查命名规范（PascalCase/_camelCase/UPPER_SNAKE_CASE）
- 验证MVVM模式正确性（WPF项目）
- 检查DI使用规范（禁止ServiceLocator）
- 验证异步模式（async/await，禁止.Wait()/.Result）
- 检查中文注释完整性

**触发关键词**：
- 中文：代码审查、检查代码规范、代码质量检查、review
- 英文：code review、check code、review this code

**输入**：
- 目标目录或文件列表（默认：当前git diff）
- 检查严格程度（严格/标准/宽松）

**输出**：
```markdown
## 🔍 代码审查报告

扫描范围：src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
文件数：8个
代码行数：2,345行

## 🔴 严重问题（必须修复）
1. ServiceLocator反模式 (src/.../ViewModel.cs:45)
2. 阻塞调用.Wait() (src/.../Service.cs:123)

## 🟡 警告（建议优化）
1. 私有字段未使用_前缀 (src/.../ViewModel.cs:20)
2. 公开方法缺少中文注释 (src/.../Service.cs:56)

## 🟢 通过检查
- ✅ 异步模式使用正确
- ✅ MVVM架构清晰

总体评分：9.2/10（优秀）
```

**检查维度**：
1. **命名规范**：类型/公开成员/私有字段/常量/异步方法
2. **MVVM模式**：ViewModel不操作UI、Command使用、属性通知
3. **DI模式**：构造函数注入、禁止Container.Resolve
4. **异步约束**：I/O必须async、避免阻塞
5. **中文注释**：公开API必须有注释

**技术实现**：
- Grep（快速模式匹配）
- serena（深度代码分析）
- sequential-thinking（复杂逻辑判断）

**性能指标**：<20秒（<100个文件）

**优先级**：⭐⭐⭐⭐⭐（立即实施）

---

### 3. lybtzyzs-pr-generator（PR描述自动生成）

**核心能力**：
- 自动分析git commits并提取Issue关联
- 生成符合项目规范的PR标题和描述
- 执行编译和测试验证（可选）
- 分析代码影响范围和变更统计
- 自动添加Claude Code生成标记

**触发关键词**：
- 中文：生成PR、创建Pull Request、PR描述、合并请求
- 英文：generate PR、create pull request、merge request

**输入**：
- 目标分支（默认：master）
- 是否执行编译验证（默认：否）
- 额外的PR说明（可选）

**输出**：
```markdown
## 📋 标题
refactor(medicalcase): Issue #1567 - 医案流程UI重构

## 🔗 关联Issue
Closes #1567
Related #1565

## 🎯 问题背景
[从Issue.body提取]

## 💡 解决方案
[核心改动说明]

## 🔧 核心改动
- 变更1
- 变更2

## 📝 提交记录（9个commits）
- commit1: 描述
- commit2: 描述

## ✅ 测试验证
- 编译状态：✅ 0 errors, 2 warnings
- 测试状态：✅ 通过

## 📦 影响范围
- 新增文件：6个
- 修改文件：7个
- 代码统计：+3,804 additions, -434 deletions

## 📚 相关文档
- [文档链接]

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

**技术实现**：
- Bash (git命令)：获取commits、diff统计
- mcp__github：获取Issue详情
- Bash (dotnet命令)：编译和测试验证
- mcp__serena：代码影响分析（可选）

**性能指标**：<30秒（不含编译）

**优先级**：⭐⭐⭐⭐（短期实施）

---

### 4. lybtzyzs-test-generator（测试用例自动生成）

**核心能力**：
- 为Repository/Service/ViewModel生成xUnit测试
- 遵循AAA模式（Arrange-Act-Assert）
- 自动配置Mock对象（使用NSubstitute）
- 生成边界条件测试用例
- 符合项目测试规范

**触发关键词**：
- 中文：生成测试、创建测试用例、为XXX写测试、单元测试
- 英文：generate test、create test、unit test、test generation

**输入**：
- 目标类的文件路径
- 类型（Repository/Service/ViewModel）

**输出**：
完整的测试类文件，包含：
- 正常流程测试
- 异常处理测试
- 边界条件测试
- Mock配置和验证

**测试模式**：
1. **Repository测试**：Mock IApi接口，验证ApiResponse处理
2. **Service测试**：Mock Repository，验证业务逻辑
3. **ViewModel测试**：Mock IRegionManager/IEventAggregator，验证Command

**技术实现**：
- mcp__serena：分析目标类结构和依赖
- context7：查询测试框架最佳实践
- Write：生成测试文件

**性能指标**：<15秒（单个类）

**优先级**：⭐⭐⭐（中期实施）

---

## 🔄 Skills工作流集成

### 开发生命周期映射

```
Phase 1 - 任务规划
→ lybtzyzs-issue-template：标准化Issue创建
→ Constitution检查：自动提醒技术约束

Phase 2 - 架构设计
→ lybtzyzs-mvp-compliance：检查MVP原则
→ lybtzyzs-arch-compliance：验证三层架构

Phase 3 - 编码实现
→ lybtzyzs-code-review：实时代码规范检查
→ lybtzyzs-test-generator：自动生成测试用例

Phase 4 - 提交合并
→ lybtzyzs-pr-generator：自动生成PR描述
→ lybtzyzs-code-review：最终代码审查

Phase 5 - 文档同步
→ lybtzyzs-doc-sync：检测并生成文档更新清单
```

### 典型工作流场景

**场景1：新Feature开发**
```
创建Issue → MVP检查 → 编码 → 代码审查 → 生成测试 → PR生成 → 文档同步
```

**场景2：Bug修复**
```
创建Issue → 修复 → 代码审查 → PR生成
```

**场景3：重构**
```
创建Issue → 架构检查 → 重构 → 生成测试 → 代码审查 → PR生成 → 文档同步
```

---

## 📊 成本效益分析

### 开发成本

| Phase | 任务 | 预计工时 |
|-------|------|----------|
| Phase 1 | lybtzyzs-issue-template MVP | 1-2小时 |
| Phase 2 | lybtzyzs-code-review MVP | 2-3小时 |
| Phase 2 | lybtzyzs-pr-generator MVP | 3-4小时 |
| Phase 3 | lybtzyzs-pr-generator完整版 | +2小时 |
| Phase 3 | lybtzyzs-test-generator MVP | 4-5小时 |
| Phase 4 | 优化和文档 | 2小时 |
| **总计** | **15-18小时** |

### 收益分析

**时间节省（每年）**：
- lybtzyzs-issue-template：5分钟/次 × 50次 = **4.2小时**
- lybtzyzs-code-review：10分钟/次 × 100次 = **16.7小时**
- lybtzyzs-pr-generator：15分钟/次 × 50次 = **12.5小时**
- lybtzyzs-test-generator：30分钟/次 × 30次 = **15小时**
- **合计直接节省：48.4小时/年**

**质量提升收益（每年）**：
- 减少PR返工：**10小时**
- 减少Bug（代码审查）：**20小时**
- 测试覆盖率提升：**15小时**
- **合计质量收益：45小时/年**

**ROI计算**：
- 第一年ROI = (48.4 + 45 - 10) / 18 = **4.6x**
- 后续年份ROI = (48.4 + 45) / 10 = **9.3x**

---

## 🚀 实施路线图

### Phase 1: 基础验证（Week 1）
**目标**：验证Skill架构可行性

**交付**：
- ✅ lybtzyzs-issue-template MVP
- ✅ Skills设计文档

**验收标准**：
- 能够生成标准化Issue模板
- description触发机制正常
- 输出格式符合规范

### Phase 2: 核心工具（Week 2-3）
**目标**：实现高频使用的Skills

**交付**：
- ✅ lybtzyzs-code-review
- ✅ lybtzyzs-pr-generator MVP

**验收标准**：
- code-review能发现实际代码问题
- pr-generator生成的描述可直接使用
- 性能符合预期（<30秒）

### Phase 3: 高级功能（Week 4-5）
**目标**：完善Skills功能

**交付**：
- ✅ lybtzyzs-pr-generator完整版
- ✅ lybtzyzs-test-generator MVP

**验收标准**：
- pr-generator包含编译状态
- test-generator生成的测试可运行
- 所有Skills性能达标

### Phase 4: 优化和集成（Week 6）
**目标**：Skills间协同和用户体验优化

**交付**：
- ✅ Skills集成文档
- ✅ 性能优化（缓存、并行）
- ✅ 错误处理完善
- ✅ 用户指南

**验收标准**：
- Skills可无缝协同工作
- 错误提示清晰友好
- 性能提升30%以上

---

## ⚠️ 风险评估

| 风险 | 概率 | 影响 | 缓解策略 |
|------|------|------|----------|
| Skill触发准确性不足 | 中 | 中 | description包含丰富关键词；支持手动触发 |
| MCP工具调用失败 | 低-中 | 高 | 所有调用包装try-catch；提供降级方案 |
| 生成内容质量不稳定 | 中 | 中 | 使用固定模板；提供编辑建议 |
| 性能不达预期 | 中 | 中 | 实施优化策略；提供进度反馈 |
| 与现有Skills冲突 | 低 | 低 | 明确触发关键词边界；功能互补 |
| 维护成本高 | 中 | 中 | 遵循项目规范；定期回归测试 |

**总体风险评级**：低-中（可接受）

---

## 🎯 成功标准

### 功能标准
- ✅ 所有4个Skills在6周内完成
- ✅ 每个Skill性能达标（<30秒）
- ✅ 触发准确率>90%
- ✅ 生成内容可用率>85%

### 效率标准
- ✅ 第一年节省时间>40小时
- ✅ ROI>4x
- ✅ 用户满意度>80%

### 质量标准
- ✅ 代码质量提升（减少PR返工）
- ✅ Bug率降低（代码审查）
- ✅ 测试覆盖率提升

---

## 📚 参考资料

**Claude Skills官方文档**：
- https://github.com/anthropics/skills
- https://support.claude.com/en/articles/12512198-creating-custom-skills

**项目相关文档**：
- `CLAUDE.md` - 项目工作流和规范
- `.spec-workflow/steering/constitution.md` - 技术约束
- `.claude/core/` - 核心规则和工作模式
- `docs/architecture/` - 架构文档

**现有Skills**：
- `.claude/skills/lybtzyzs-mvp-compliance/`
- `.claude/skills/lybtzyzs-arch-compliance/`
- `.claude/skills/lybtzyzs-doc-sync/`

---

## 🔜 下一步行动

1. ✅ **立即执行**（本周）：
   - 创建lybtzyzs-issue-template SKILL.md
   - 测试触发机制
   - 收集初步反馈

2. ⏭️ **短期执行**（2-3周内）：
   - 实施lybtzyzs-code-review
   - 实施lybtzyzs-pr-generator MVP
   - 迭代优化

3. 📅 **中期执行**（1-2个月内）：
   - 实施lybtzyzs-test-generator
   - 完善所有Skills功能
   - 编写用户指南

---

## 📝 变更记录

| 版本 | 日期 | 作者 | 变更说明 |
|------|------|------|----------|
| v1.0 | 2025-10-22 | Claude | 初始版本（基于UltraThink 28步分析） |

---

**文档状态**：✅ 设计完成，待实施
**审批状态**：待用户审批
**下次审查**：Phase 1完成后
