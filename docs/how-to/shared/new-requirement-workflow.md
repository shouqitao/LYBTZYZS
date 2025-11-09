# 新需求工作流（v6.0）

> **生效日期**：2025-10-21
> **适用场景**：架构稳定后的全新需求开发
> **核心理念**：清晰边界、验证优先、增量交付

---

## 🎯 工作流概览

```
需求提出 → Constitution检查 → 创建Issue → 验证优先 → 开发实施 → 代码审查 → 文档同步 → PR合并
   ↓           ↓              ↓          ↓          ↓          ↓          ↓          ↓
 明确目标   技术合规      规范模板   避免无效工作  符合标准   质量门禁   保持同步   交付价值
```

---

## 📋 第1步：需求提出与讨论

### 需求讨论规范

**核心原则**：所有需求讨论必须形成Markdown文档，避免上下文丢失

#### 流程
1. **创建讨论文档**
   ```bash
   # 选择合适的目录
   docs/architecture/shared/{feature-name}-discussion.md   # 架构设计
   docs/architecture/client/{feature-name}-discussion.md   # UI/UX设计
   docs/architecture/server/{feature-name}-discussion.md   # API设计
   ```

2. **逐个问题讨论（一问一答原则）**
   - ✅ 每次只提一个问题（Q1/Q2/Q3），等待用户回答后更新文档
   - ❌ 禁止批量提问（同时问Q3/Q4/Q5）
   - ✅ 逐个问题标记状态：
     - ✅ 已确认
     - ❌ 有问题
     - 🔄 需改进
     - ❓ 待讨论

3. **文档作为唯一事实来源**
   - 讨论结束后，文档即为Single Source of Truth
   - 创建Issue时引用讨论文档
   - 实施时以文档为准，不依赖记忆

### 讨论文档模板

```markdown
# {功能名称} 需求讨论

> **讨论时间**：2025-10-21
> **参与人员**：[用户名]、Claude Code
> **状态**：进行中/已完成

## 📋 讨论问题清单

### Q1: [问题描述]
- **状态**：✅ 已确认
- **回答**：[用户回答]
- **结论**：[讨论结论]

### Q2: [问题描述]
- **状态**：❓ 待讨论
- **回答**：待用户回答

## 📝 需求确认

### 功能描述
[根据讨论确认的功能描述]

### 验收标准
- [ ] 标准1
- [ ] 标准2

### 架构影响
[影响范围]

## 🔗 后续行动

- [ ] 创建GitHub Issue：#[编号]
- [ ] 更新架构文档：[链接]
```

---

## ⚖️ 第2步：Constitution合规性检查

### 检查清单

**创建Issue前必须检查**：
- [ ] **技术黑名单检查**：不使用Redis、CQRS、MediatR、Docker、GraphQL、RabbitMQ等
- [ ] **MVP优先原则**：够用即好，避免过度设计
- [ ] **三层对齐架构**：符合Server/Client/Shared架构规范
- [ ] **依赖注入规范**：仅使用构造函数注入，禁止ServiceLocator
- [ ] **文件组织规范**：输出文件归档到指定目录

### Constitution文档位置
```
.spec-workflow/steering/constitution.md
```

### 自动化检查（可选）

```bash
# 使用lybtzyzs-mvp-compliance Skill
claude "使用MVP合规Skill检查当前需求设计"

# 使用lybtzyzs-arch-compliance Skill
claude "使用架构合规Skill检查依赖方向"
```

---

## 📝 第3步：创建GitHub Issue

### 使用新模板

**模板文档**：`docs/how-to-guides/shared/issue-template-v6.md`

### Issue创建检查清单

- [ ] 检查归档清单（`docs/reports/backlog-archive-2025-10.md`）是否有类似需求
- [ ] 标题符合格式：`[类型][模块] 简洁描述`
- [ ] 包含清晰的功能描述和验收标准
- [ ] 明确架构影响范围
- [ ] 添加正确的labels（type、module、priority）
- [ ] 范围可控（1-3天可完成）
- [ ] 引用需求讨论文档（如有）

### GitHub CLI快速创建

```bash
# 参考issue-template-v6.md中的CLI命令示例
gh issue create \
  --title "[Feature][模块名] 功能描述" \
  --label "type:feature,module:xxx,priority:high" \
  --body "$(cat issue-body.md)"
```

---

## 🔍 第4步：验证优先策略

### 核心原则

**问题验证优先于修复实施**，避免无效工作

### 验证流程

1. **识别需要验证的问题**
   - Bug报告（是否真实存在？）
   - 性能问题（是否达到优化阈值？）
   - 架构问题（是否确实违反规范？）

2. **执行验证**
   ```bash
   # 使用grep/Read/Bash等工具对比契约、配置、依赖关系
   grep "pattern" file.txt
   dotnet build LYBT.All.sln -c Release --no-restore
   dotnet test LYBT.All.sln -c Release
   ```

3. **生成验证报告**
   ```markdown
   # 验证报告模板

   ## 验证结果
   - ✅ 问题确认存在 → 继续修复
   - ✅ 问题不存在 → 标记为"已验证无需执行"，关闭Issue
   - ⚠️ 无法确定 → 标记为"条件执行"，需运行时验证

   ## 验证依据
   [编译输出、测试结果、代码对比]

   ## 决策
   [是否继续实施、关闭Issue还是条件执行]
   ```

4. **决策**
   - ✅ 如验证确认问题存在 → 创建Issue，按Issue驱动流程修复
   - ✅ 如验证证明问题不存在 → 标记为"已验证无需执行"，更新报告
   - ⚠️ 如验证无法确定 → 标记为"条件执行"

### 工具链

```
sequential-thinking（深度分析） → grep/Read（对比验证） → 生成验证报告 → 决策
```

---

## 🛠️ 第5步：开发实施

### 任务启动前置检查

1. **环境检查**
   ```bash
   git pull origin master
   dotnet build LYBT.All.sln -c Release --no-restore
   dotnet test LYBT.All.sln -c Release
   ```

2. **创建功能分支**
   ```bash
   git checkout -b feature/issue-{编号}-{简短描述}
   # 例如：feature/issue-1600-prescription-numbering
   ```

3. **阅读相关文档**
   - 架构文档：`docs/explanation/architecture/{server|client|shared}/`
   - 开发指南：`docs/how-to-guides/{server|client|shared}/`
   - 快速参考：`docs/reference/quick-reference/`

### 开发规范

**编码规范**：
- ✅ 遵循命名规范（PascalCase、_camelCase、UPPER_SNAKE_CASE）
- ✅ 仅使用构造函数注入
- ✅ 涉及I/O必须async/await
- ✅ 单文件≤500行
- ✅ 代码注释使用中文

**质量标准**：
- ✅ 编译通过：0 errors, 0 warnings
- ✅ 文件编码：UTF-8 with BOM
- ✅ 禁止Emoji（代码中）
- ✅ 新增核心逻辑必须补充测试

### 代码与文档并行开发

**强制性要求**：
- ✅ 代码变更后立即更新相关文档，不允许延迟
- ✅ 实施前评估文档影响范围，列出需要更新的文档清单
- ✅ 开发过程中文档同步进行，不积压到项目结束

**文档更新范围**：
- 架构文档：`docs/explanation/architecture/{server|client|shared}/`
- 开发指南：`docs/how-to-guides/{server|client|shared}/`
- API文档：`docs/reference/api/`
- 快速参考：`docs/reference/quick-reference/`
- 导航索引：`docs/index.md`

---

## 🔍 第6步：代码审查

### 自审清单

**提交PR前必须完成**：

- [ ] **编译质量**：0 errors, 0 warnings
- [ ] **警告处理**：
  - ≤20个 → 直接修复
  - >20个 → 创建Issue跟踪
- [ ] **测试通过**：所有相关单元测试/集成测试通过
- [ ] **文档同步**：相关文档已更新
- [ ] **Constitution合规**：不违反技术约束
- [ ] **代码规范**：符合命名规范、依赖注入规范

### 使用Code Review Mode

```bash
# 触发代码审查模式
/code-review

# 或使用lybtzyzs-mvp-compliance Skill
claude "使用MVP合规Skill检查代码变更"

# 或使用lybtzyzs-arch-compliance Skill
claude "使用架构合规Skill检查依赖方向"
```

### Code Review检查项

- **规范检查**：命名、注释、文件编码
- **架构合规**：三层架构、依赖方向
- **安全性**：SQL注入、XSS、敏感信息泄露
- **性能**：N+1查询、内存泄漏、并发问题
- **可维护性**：代码复杂度、重复代码

---

## 📚 第7步：文档同步验证

### 文档同步检查清单

**PR提交前必须确认**：

- [ ] **影响范围评估**：列出需要更新的文档清单
- [ ] **架构文档**：已更新对应模块的README和设计文档
- [ ] **开发指南**：已更新相关开发指南
- [ ] **API文档**：已更新API接口文档（如有API变更）
- [ ] **快速参考**：已同步更新Level 1文档（如适用）
- [ ] **导航索引**：已更新`docs/index.md`和相关README

### 使用Documentation Mode

```bash
# 触发文档同步模式
/update-docs

# 或使用lybtzyzs-doc-sync Skill
claude "使用文档同步Skill检查API变更"
```

### 文档同步验证

- **变更检测**：API端点变更、架构调整、数据模型变更
- **链接验证**：所有文档链接有效
- **路径一致性**：使用对齐后的新路径格式
- **过时清理**：删除过时文档，归档到`docs/archive/`

---

## 🚀 第8步：创建Pull Request

### PR标题格式

```
[类型][模块] 简洁描述 (Closes #Issue编号)
```

### PR描述模板

```markdown
## 📝 变更说明
[清晰描述这个PR做了什么]

## 🔗 关联Issue
Closes #[Issue编号]

## ✅ 变更清单
- [ ] 代码变更1
- [ ] 代码变更2
- [ ] 文档更新1
- [ ] 文档更新2

## 🧪 测试情况
- [ ] 单元测试通过
- [ ] 集成测试通过（如适用）
- [ ] 手动测试通过
- [ ] 编译通过：0 errors, 0 warnings

## 📚 文档同步
- [ ] 架构文档已更新
- [ ] 开发指南已更新
- [ ] API文档已更新（如适用）
- [ ] 快速参考已更新（如适用）

## ⚠️ Constitution检查
- [ ] 不违反技术黑名单
- [ ] 符合MVP优先原则
- [ ] 符合三层对齐架构

## 🔍 审查重点
[提示审查者重点关注的部分]

## 📸 截图/演示
[如有UI变更，提供截图或GIF]
```

### GitHub CLI创建PR

```bash
# 推送分支
git push -u origin feature/issue-{编号}-{简短描述}

# 创建PR
gh pr create \
  --title "[Feature][Prescriptions] 实现处方自动编号功能 (Closes #1600)" \
  --body "$(cat pr-body.md)" \
  --base master \
  --label "type:feature,module:prescriptions"
```

---

## ✅ 第9步：PR审查与合并

### 审查检查清单

**审查者必须确认**：

- [ ] **代码质量**：符合编码规范
- [ ] **架构合规**：符合三层对齐架构
- [ ] **测试覆盖**：核心逻辑有测试
- [ ] **文档完整**：相关文档已更新
- [ ] **编译通过**：0 errors, 0 warnings
- [ ] **Constitution合规**：不违反技术约束

### 合并策略

**推荐策略**：Squash and Merge
- ✅ 保持主分支历史简洁
- ✅ 合并提交信息格式：
  ```
  [类型][模块] 简洁描述 (#PR编号)

  详细说明（可选）

  Closes #Issue编号
  ```

### 合并后清理

```bash
# 删除本地分支
git branch -d feature/issue-{编号}-{简短描述}

# 删除远程分支
git push origin --delete feature/issue-{编号}-{简短描述}
```

---

## 🔄 第10步：后台清理（Run-to-Completion Hygiene）

### 清理检查清单

**PR合并后必须执行**：

- [ ] **终止临时进程**：停止为验证启动的WebAPI/桌面端/脚本
- [ ] **释放资源与缓存**：清理内存缓存/临时文件/本地数据沙箱
- [ ] **还原配置与环境变量**：移除测试期设置的临时变量
- [ ] **关闭外部连接**：断开数据库连接、HTTP调试代理、自动化会话
- [ ] **证据归档**：将日志片段/截图/命令输出收敛到PR或Issue评论
- [ ] **端口检查**：确认5001等端口未被占用
- [ ] **文档同步**：如清理步骤依赖脚本或特定命令，在相关README中补充指引

---

## 📊 工作流检查表

### 完整流程检查表

使用此检查表确保每个步骤都已完成：

```markdown
# {功能名称} 工作流检查表

## 需求阶段
- [ ] 创建需求讨论文档
- [ ] 逐个问题讨论并确认
- [ ] 需求讨论文档完成

## 准备阶段
- [ ] Constitution合规性检查
- [ ] 检查归档清单是否有类似需求
- [ ] 创建GitHub Issue
- [ ] Issue标题、标签、正文符合规范

## 验证阶段
- [ ] 识别需要验证的问题
- [ ] 执行验证（编译、测试、代码对比）
- [ ] 生成验证报告
- [ ] 验证决策（继续/关闭/条件执行）

## 开发阶段
- [ ] 环境检查（git pull、编译、测试）
- [ ] 创建功能分支
- [ ] 阅读相关文档
- [ ] 实施代码变更
- [ ] 同步更新文档
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 测试通过

## 审查阶段
- [ ] 自审清单完成
- [ ] Code Review Mode检查
- [ ] 文档同步验证
- [ ] Constitution合规检查

## 提交阶段
- [ ] 创建Pull Request
- [ ] PR标题、描述符合规范
- [ ] PR通过审查
- [ ] PR合并到主分支

## 清理阶段
- [ ] 删除功能分支
- [ ] 后台清理（进程、资源、端口）
- [ ] 证据归档
- [ ] 关闭Issue

## 完成
- [ ] 功能已交付
- [ ] 文档已同步
- [ ] 无遗留问题
```

---

## 🎓 最佳实践

### DO（推荐）

✅ **小步快跑**：每个Issue范围可控，1-3天可完成
✅ **验证优先**：先验证问题真实性，再实施修复
✅ **文档同步**：代码与文档并行开发，不延迟
✅ **增量交付**：每个PR提供可验证的价值
✅ **质量门禁**：编译0 warnings，测试必须通过
✅ **一问一答**：需求讨论逐个问题确认

### DON'T（避免）

❌ **范围蔓延**：Issue范围过大，无法短期完成
❌ **跳过验证**：未验证问题就开始修复，浪费时间
❌ **文档滞后**：代码完成后才更新文档
❌ **批量提问**：一次提出多个问题，导致混乱
❌ **违反Constitution**：使用技术黑名单、过度设计
❌ **忽略警告**：编译有warnings但继续提交

---

## 🔧 工具链支持

### MCP工具

- **serena**：语义代码编辑、符号查找
- **filesystem**：文件操作、目录管理
- **git**：版本控制、分支管理
- **github**：Issue/PR管理
- **context7**：文档查询、最佳实践
- **sequential-thinking**：深度分析、推理验证

### Claude Skills

- **lybtzyzs-mvp-compliance**：MVP合规检查
- **lybtzyzs-arch-compliance**：架构合规检查
- **lybtzyzs-doc-sync**：文档同步检查

### Modes

- `/code-review`：代码审查模式
- `/review-arch`：架构审查模式
- `/update-docs`：文档同步模式
- `/deep-research`：深度研究模式

---

## 📚 参考资料

### 核心文档
- **Issue模板**：`docs/how-to-guides/shared/issue-template-v6.md`
- **归档清单**：`docs/reports/backlog-archive-2025-10.md`
- **清理指南**：`docs/how-to-guides/shared/issue-cleanup-guide.md`
- **Constitution**：`.spec-workflow/steering/constitution.md`

### 架构文档
- **Server架构**：`docs/explanation/architecture/server/README.md`
- **Client架构**：`docs/explanation/architecture/client/README.md`
- **Shared架构**：`docs/explanation/architecture/shared/README.md`

### 开发指南
- **任务工作流**：`docs/how-to-guides/shared/task-workflow-checklist.md`
- **文件组织**：`.claude/core/FILE-ORGANIZATION.md`
- **MCP工具**：`.claude/core/MCP-TOOLS-ORCHESTRATION.md`

---

## 💡 常见问题

### Q1: 如何判断Issue范围是否合适？
**A1**：一个Issue应该在1-3天内完成。如果需要更长时间，拆分成多个子Issue。

### Q2: 什么时候需要创建需求讨论文档？
**A2**：当需求不清晰、涉及架构调整、或需要UI/UX设计时，必须创建讨论文档。

### Q3: 验证优先策略如何应用？
**A3**：对于Bug报告、性能问题、架构问题，先验证问题是否真实存在，再决定是否修复。

### Q4: 如何确保文档同步不滞后？
**A4**：代码变更时立即更新文档，不要等到PR提交前才更新。在开发过程中保持同步。

### Q5: 如何使用归档清单？
**A5**：创建新Issue前，先检查`docs/reports/backlog-archive-2025-10.md`，如有类似需求，参考其功能描述和实现思路。

---

**v6.0工作流说明**：
- ✅ 基于稳定架构的新起点
- ✅ 强调验证优先，避免无效工作
- ✅ 文档与代码并行开发
- ✅ 清晰的质量门禁和检查清单
- ✅ 完整的工具链支持
