# GitHub Issue模板 v6.0

> **适用于**：架构稳定后的新需求管理
> **生效日期**：2025-10-21
> **核心原则**：清晰边界、可验证、符合Constitution

---

## 📝 Issue创建前检查清单

### 必须先完成的步骤

- [ ] **检查归档清单**：查看 `docs/reports/backlog-archive-2025-10.md` 是否有类似需求
- [ ] **检查Constitution**：确认不违反技术黑名单和MVP原则
- [ ] **检查架构文档**：确认符合三层对齐架构规范
- [ ] **检查现有Issue**：避免创建重复Issue

### 明确需求类型

- [ ] **功能增强**（Feature） - 新增功能或现有功能改进
- [ ] **Bug修复**（Bug） - 修复已知问题
- [ ] **重构优化**（Refactor） - 代码结构优化（不改变功能）
- [ ] **文档更新**（Documentation） - 仅文档修改
- [ ] **测试补充**（Test） - 仅测试覆盖补充
- [ ] **架构调整**（Architecture） - 架构层面的调整

---

## 📋 Issue标题格式

### 格式规范
```
[类型][模块] 简洁描述（目标导向）
```

### 示例
- ✅ `[Feature][Prescriptions] 实现处方自动编号功能`
- ✅ `[Bug][Patients] 修复患者搜索时的空指针异常`
- ✅ `[Refactor][Infrastructure] 优化Repository基类依赖注入`
- ❌ `修改处方功能` - 缺少类型和模块
- ❌ `[Feature] 优化系统` - 描述过于笼统

### 类型标签
- `[Feature]` - 功能增强
- `[Bug]` - Bug修复
- `[Refactor]` - 重构优化
- `[Docs]` - 文档更新
- `[Test]` - 测试补充
- `[Architecture]` - 架构调整

### 模块标签
- `[Patients]` - 患者管理
- `[Consultation]` - 就诊管理
- `[Prescriptions]` - 处方管理
- `[Formula]` - 验方管理
- `[Herbs]` - 药材管理
- `[MedicalCase]` - 病案管理
- `[Users]` - 用户管理
- `[Auth]` - 认证授权
- `[Infrastructure]` - 基础设施
- `[Shared]` - 共享组件

---

## 📖 Issue正文模板

### Feature类型

```markdown
## 📝 功能描述
[清晰描述要实现的功能，1-3句话]

## 🎯 业务目标
[说明为什么需要这个功能，解决什么问题]

## ✅ 验收标准
- [ ] 功能标准1（可测试、可验证）
- [ ] 功能标准2
- [ ] 性能标准（如有）
- [ ] 安全标准（如有）

## 🏗️ 架构影响范围
[影响哪些模块？是否需要修改API/数据库/配置？]

## 📚 参考资料
- 相关文档：[链接]
- 设计讨论：[链接]
- 归档需求：backlog-archive-2025-10.md #[编号]（如适用）

## 🚧 实现建议（可选）
[技术方案建议、实现思路、注意事项]

## ⚠️ Constitution检查
- [ ] 不违反技术黑名单（无Redis/CQRS/MediatR/Docker/GraphQL）
- [ ] 符合MVP优先原则（够用即好）
- [ ] 符合三层对齐架构
```

### Bug类型

```markdown
## 🐛 Bug描述
[清晰描述问题现象]

## 🔍 复现步骤
1. 步骤1
2. 步骤2
3. 步骤3

## 📸 预期行为 vs 实际行为
- **预期**：[应该怎样]
- **实际**：[实际怎样]
- **截图/日志**：[如有]

## 🌍 环境信息
- 操作系统：Windows 10/11
- .NET版本：8.0
- 代码分支：master
- 最后一次正常的commit：[如知道]

## 🔧 修复建议（可选）
[可能的原因分析、修复思路]

## 📚 相关代码位置
- 文件路径：[文件路径:行号]
- 相关Issue：#[编号]
```

### Refactor类型

```markdown
## 🔄 重构目标
[说明为什么需要重构，要达成什么目标]

## 📊 当前问题
[描述当前代码的问题：复杂度、重复代码、性能瓶颈等]

## 🎯 重构范围
- [ ] 影响文件1
- [ ] 影响文件2
- [ ] 影响API：是/否
- [ ] 影响数据库：是/否

## ✅ 验收标准
- [ ] 功能无变化（所有现有测试通过）
- [ ] 代码复杂度降低（Cyclomatic Complexity < 10）
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 性能不降低（如适用）

## 📚 参考资料
- 设计模式：[链接]
- 相关讨论：[链接]

## ⚠️ 风险评估
[重构可能带来的风险、需要注意的地方]
```

---

## 🏷️ GitHub Labels规范

### 必选标签（至少1个）
- `type:feature` - 功能增强
- `type:bug` - Bug修复
- `type:refactor` - 重构优化
- `type:docs` - 文档更新
- `type:test` - 测试补充
- `type:architecture` - 架构调整

### 模块标签（必选1个）
- `module:patients` - 患者管理
- `module:consultation` - 就诊管理
- `module:prescriptions` - 处方管理
- `module:formula` - 验方管理
- `module:herbs` - 药材管理
- `module:medicalcase` - 病案管理
- `module:users` - 用户管理
- `module:auth` - 认证授权
- `module:infrastructure` - 基础设施
- `module:shared` - 共享组件

### 优先级标签（可选）
- `priority:critical` - 紧急（Bug/安全问题）
- `priority:high` - 高优先级（MVP核心功能）
- `priority:medium` - 中等优先级（重要但不紧急）
- `priority:low` - 低优先级（优化改进）

### Epic标签（可选）
- `epic:xxx` - 归属的Epic编号

---

## 📐 Issue质量检查清单

### 创建Issue后，自查以下内容：

- [ ] **标题清晰**：包含类型、模块、简洁描述
- [ ] **目标明确**：说明要做什么、为什么做
- [ ] **验收标准**：可测试、可验证
- [ ] **范围可控**：单个Issue可在1-3天内完成
- [ ] **标签完整**：至少有type和module标签
- [ ] **无重复**：与现有Issue不重复
- [ ] **符合Constitution**：不违反技术约束
- [ ] **架构对齐**：符合三层对齐架构

---

## 🚫 常见错误

### 避免以下问题：

❌ **笼统描述**
- "优化系统性能" → ✅ "优化PrescriptionRepository查询性能，减少N+1查询"

❌ **范围过大**
- "实现完整的就诊流程" → ✅ "实现就诊记录查询功能"

❌ **缺少验收标准**
- "修复Bug" → ✅ "修复患者搜索空指针异常，确保空输入返回空列表"

❌ **技术细节过多**
- 在标题中写"使用Task.Run异步化Repository调用" → ✅ "优化Repository异步调用性能"

❌ **多个功能混合**
- "实现处方编号和状态管理" → ✅ 拆分成2个Issue

---

## 📚 参考资料

- **归档清单**：`docs/reports/backlog-archive-2025-10.md`
- **Constitution**：`.spec-workflow/steering/constitution.md`
- **架构文档**：`docs/explanation/architecture/`
- **任务工作流**：`docs/how-to-guides/shared/task-workflow-checklist.md`
- **新需求工作流**：`docs/how-to-guides/shared/new-requirement-workflow.md`

---

## 💡 快速创建Issue

### GitHub CLI命令

```bash
# 创建Feature类型Issue
gh issue create \
  --title "[Feature][Prescriptions] 实现处方自动编号功能" \
  --label "type:feature,module:prescriptions,priority:high" \
  --body "$(cat <<'EOF'
## 📝 功能描述
实现处方自动编号功能，确保每个处方有唯一编号。

## 🎯 业务目标
便于处方管理和追溯，满足临床工作需要。

## ✅ 验收标准
- [ ] 处方保存时自动生成唯一编号
- [ ] 编号格式：RX-YYYYMMDD-NNNN
- [ ] 处方详情页显示编号
- [ ] 编号服务单元测试通过

## 🏗️ 架构影响范围
- 新增：PrescriptionNumberService
- 修改：PrescriptionRepository、PrescriptionEditorViewModel

## 📚 参考资料
- 归档需求：backlog-archive-2025-10.md #1390-#1392

## ⚠️ Constitution检查
- [x] 不违反技术黑名单
- [x] 符合MVP优先原则
- [x] 符合三层对齐架构
EOF
)"

# 创建Bug类型Issue
gh issue create \
  --title "[Bug][Patients] 修复患者搜索时的空指针异常" \
  --label "type:bug,module:patients,priority:critical" \
  --body "$(cat <<'EOF'
## 🐛 Bug描述
患者搜索功能在输入为空时抛出空指针异常。

## 🔍 复现步骤
1. 打开患者管理页面
2. 点击搜索按钮但不输入任何内容
3. 系统抛出 NullReferenceException

## 📸 预期行为 vs 实际行为
- **预期**：返回空列表或提示输入搜索条件
- **实际**：抛出异常，系统崩溃

## 🌍 环境信息
- 操作系统：Windows 11
- .NET版本：8.0
- 代码分支：master

## 📚 相关代码位置
- PatientRepository.cs:125
EOF
)"
```

---

**v6.0更新说明**：
- ✅ 简化模板结构，聚焦核心信息
- ✅ 增加Constitution检查清单
- ✅ 增加归档清单引用机制
- ✅ 优化验收标准格式
- ✅ 增加CLI快速创建命令
