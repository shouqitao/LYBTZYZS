# Claude Skills引入可行性讨论

## 文档目的

评估在凌隐宝堂中医诊所项目中引入Claude Skills的可行性和实施方案，提升开发效率和代码质量。

## 文档版本

| 日期 | 版本 | 变更描述 | 修改人 |
|------|------|---------|-------|
| 2025-10-21 | v1.0 | 初始版本，完成基础调研 | Claude |
| 2025-10-21 | v2.0 | 完成所有7个问题讨论，确定最终实施方案 | Claude + 用户 |

---

## ✅ 已确认：Skills核心概念

### 1. Skills的定义
- **本质**：模块化的AI能力扩展包，包含指令、脚本和资源
- **结构**：每个Skill是一个文件夹，至少包含SKILL.md文件
- **触发**：Claude根据Skill的description字段自动判断何时调用
- **位置**：安装在`~/.claude/skills`目录或通过插件加载

### 2. Skills的组成
```
skill-name/
├── SKILL.md              # 必需：包含YAML frontmatter和指令
├── scripts/              # 可选：辅助脚本
├── templates/            # 可选：模板文件
└── resources/            # 可选：其他资源
```

**SKILL.md基本格式**：
```yaml
---
name: skill-name
description: 简短描述Skill的用途（Claude用于判断是否调用）
---

# 详细指令

[Markdown格式的指令内容]
```

### 3. 创建Skills的方式
- **方式1**：手动创建文件夹和SKILL.md
- **方式2**：使用`skill-creator` Skill交互式创建（推荐）
- **方式3**：从GitHub仓库复制示例修改

### 4. 最佳实践
- ✅ 每个Skill保持专注（单一职责）
- ✅ 写清晰的description（决定调用时机）
- ✅ 多个小Skills优于一个大Skills
- ✅ 包含示例和使用指南
- ✅ 定期维护和更新

---

## 🔄 改进方向：为凌隐宝堂中医诊所项目定制Skills

基于项目特点和当前痛点，我们可以创建以下Skills：

### 1. 架构合规检查Skill
**目标**：自动化检查代码是否符合三层架构规范

**核心功能**：
- 检查依赖方向（Application不能引用Presentation）
- 验证CQRS模式使用（查询/命令分离）
- 检查DDD聚合根边界
- 验证Repository模式正确性

**预期收益**：
- 减少人工Code Review时间
- 提前发现架构违规
- 统一架构理解

### 2. MVP合规检查Skill
**目标**：确保所有变更符合MVP原则和Constitution约束

**核心功能**：
- 检查是否使用技术黑名单（Redis/CQRS/MediatR/Docker/GraphQL）
- 验证是否过度设计（YAGNI原则）
- 检查是否符合"够用即好"标准
- 验证是否引入不必要的复杂性

**预期收益**：
- 避免过度工程
- 加快迭代速度
- 保持代码简洁

### 3. 文档同步Skill
**目标**：自动化检测代码变更并同步更新文档

**核心功能**：
- 检测API变更（新增/修改/删除端点）
- 识别架构调整（模块/服务变更）
- 检查文档链接有效性
- 生成文档更新清单

**预期收益**：
- 文档与代码100%同步
- 减少手动文档维护
- 提升文档准确性

### 4. Issue生成Skill
**目标**：从Spec文档自动生成GitHub Issues

**核心功能**：
- 解析tasks.md文件
- 创建Epic Issue和子任务
- 自动添加标签和关联
- 生成验收标准

**预期收益**：
- 节省Issue创建时间
- 标准化Issue格式
- 减少人为遗漏

### 5. 测试生成Skill
**目标**：基于业务逻辑自动生成单元测试

**核心功能**：
- 分析Service/Handler代码
- 生成AAA模式测试
- 配置Mock依赖
- 生成测试数据

**预期收益**：
- 提升测试覆盖率
- 节省测试编写时间
- 标准化测试格式

### 6. 数据库迁移Skill
**目标**：安全生成和验证EF Core迁移

**核心功能**：
- 检查实体变更
- 生成迁移命令
- 验证迁移SQL
- 检查数据丢失风险

**预期收益**：
- 减少迁移错误
- 提升数据库安全性
- 标准化迁移流程

---

## ❓ 待讨论问题

### ✅ Q1：Skills的优先级和实施顺序（已确认）

**问题背景**：
- 我们有6个候选Skill（架构合规、MVP合规、文档同步、Issue生成、测试生成、数据库迁移）
- 当前MVP阶段有57个任务待完成（Epic #1343）
- 需要平衡Skills开发成本和收益

**最终决策**：✅ **选项C：混合策略**

**实施顺序**：
1. **MVP合规检查Skill**（优先级最高）
   - 检测技术黑名单违规（Redis/CQRS/MediatR/Docker/GraphQL）
   - 识别过度设计模式
   - 验证YAGNI原则
   - 预计开发时间：1-2小时

2. **架构合规检查Skill**（优先级高）
   - 验证三层架构依赖方向
   - 检查DDD聚合根边界
   - 验证Repository模式
   - 预计开发时间：1-2小时

3. **文档同步Skill**（优先级中）
   - 检测API变更
   - 识别架构调整
   - 生成文档更新清单
   - 预计开发时间：1-2小时

**总投入时间**：约4-6小时
**预期收益**：
- ✅ 保证MVP合规（避免技术违规）
- ✅ 提升代码质量（架构一致性）
- ✅ 减少文档手动维护（自动化检测）

**延后的Skills**（MVP完成后实施）：
- Issue生成Skill
- 测试生成Skill
- 数据库迁移Skill

---

### ✅ Q2：Skills的存放位置（已确认）

**问题背景**：
- Claude Skills标准位置是`~/.claude/skills`（全局）
- 但我们希望Skills随项目版本控制
- 个人开发者需要平衡"自动加载"和"版本控制"

**最终决策**：✅ **选项C：混合模式（符号链接方式）**

**实施方案**：
```
项目仓库结构：
.claude/skills/
  ├── lybtzyzs-mvp-compliance/      # MVP合规检查Skill
  ├── lybtzyzs-arch-compliance/     # 架构合规检查Skill
  └── lybtzyzs-doc-sync/            # 文档同步Skill

同步机制：
创建符号链接（symbolic link）从全局目录到项目目录
```

**技术实现（Windows PowerShell）**：
```powershell
# 一次性设置脚本：scripts/setup-skills.ps1
$projectSkills = "D:\source\repos\LYBTZYZS\.claude\skills"
$globalSkills = "$env:USERPROFILE\.claude\skills"

# 确保全局目录存在
New-Item -ItemType Directory -Force -Path $globalSkills

# 为每个Skill创建符号链接（需要管理员权限）
Get-ChildItem $projectSkills -Directory | ForEach-Object {
    $linkPath = Join-Path $globalSkills $_.Name
    New-Item -ItemType SymbolicLink -Path $linkPath -Target $_.FullName -Force
}
```

**优势**：
- ✅ 版本控制（Skills在Git仓库中）
- ✅ 自动同步（符号链接自动反映更改）
- ✅ 项目隔离（`lybtzyzs-`前缀避免冲突）
- ✅ 零维护成本（设置一次，永久生效）
- ✅ 备份安全（Git备份，换电脑可恢复）

**命名规范**：
- 统一前缀：`lybtzyzs-*`
- 语义化：`lybtzyzs-mvp-compliance`、`lybtzyzs-arch-compliance`

---

### ✅ Q3：Skills与现有MCP工具的协同（已确认）

**问题背景**：
- 当前已有MCP工具：serena、filesystem、git、sequential-thinking、context7等
- Skills可以调用MCP工具
- 需要明确Skills和MCP工具的分工

**最终决策**：✅ **选项A：Skills调用MCP工具**

**协同模式**：
```
Skills（工作流层）
  ├─ 定义"做什么"（What to do）
  ├─ 编排多步骤流程
  └─ 提供领域知识

MCP工具（执行层）
  ├─ 定义"怎么做"（How to do）
  ├─ 执行具体操作
  └─ 提供底层能力
```

**实战示例**：

**1. MVP合规检查Skill**：
```markdown
# SKILL.md 指令示例

## 检查流程
1. 使用 `grep` 工具搜索技术黑名单关键字：
   - Redis、CQRS、MediatR、Docker、GraphQL等
2. 使用 `serena` 工具分析代码复杂度
3. 使用 `sequential-thinking` 评估设计合理性
4. 生成合规报告
```

**2. 架构合规检查Skill**：
```markdown
# SKILL.md 指令示例

## 检查流程
1. 使用 `serena` 工具分析项目依赖关系
2. 验证依赖方向（Application不能引用Presentation）
3. 使用 `grep` 检查聚合根边界
4. 生成架构合规报告
```

**分工原则**：
- ✅ Skills负责：领域规则、检查流程、报告模板
- ✅ MCP工具负责：代码分析、文件操作、数据查询
- ✅ 避免重复：不在Skills中重新实现MCP工具功能

---

### ✅ Q4：Skills的维护和更新机制（已确认）

**问题背景**：
- Skills需要随项目演进更新
- Constitution、架构规范可能变化
- 个人开发者需要简单高效的维护方式

**最终决策**：✅ **选项D：实用主义 + 简单版本号**

**维护策略**：
```
触发时机：
- 发现Skill建议不当时（Claude执行时发现问题）
- Constitution或架构规范变更时（顺手更新）
- 无需定期审查（零额外成本）

版本管理：
- 在SKILL.md顶部添加简单版本号
- 记录重大变更（v1.0 → v1.1 → v2.0）
- 无需复杂的版本文档
```

**SKILL.md模板**：
```yaml
---
name: lybtzyzs-mvp-compliance
description: 检查代码是否符合MVP原则和Constitution约束
version: v1.1
last_updated: 2025-10-21
---

## 变更记录
- v1.1 (2025-10-21): 新增GraphQL黑名单检测
- v1.0 (2025-10-21): 初始版本

## 检查内容
[详细指令...]
```

**更新流程**：
1. 发现问题 → 更新SKILL.md → 更新版本号和变更记录
2. Git提交：`git commit -m "feat(skills): 更新MVP合规Skill v1.1 - 新增GraphQL检测"`
3. 符号链接自动同步（无需手动操作）

**优势**：
- ✅ 极简主义，零额外管理成本
- ✅ 符合个人开发节奏
- ✅ 自然发现问题（使用时即验证）
- ✅ Git历史自动追踪变更

---

### ✅ Q5：Skills的测试和验证（已确认）

**问题背景**：
- Skills是指令集，如何确保其有效性？
- 错误的Skill可能误导Claude
- 需要平衡验证质量和开发成本

**最终决策**：✅ **选项D：混合策略（首次测试用例 + 后续实际使用）**

**验证策略**：
```
首次创建时：
- 准备1-2个简单测试场景
- 在测试场景上运行Skill
- 验证Claude输出是否符合预期

后续使用时：
- 在真实任务中自然验证
- 发现问题立即修复
- 更新测试场景（如遇到新边界情况）
```

**测试场景模板**：
```markdown
## 测试场景

### 场景1：检测Redis黑名单
**测试代码**：
```csharp
public class CacheService {
    private IDistributedCache _cache; // 应触发警告：Redis黑名单
}
```
**预期输出**：检测到Redis/分布式缓存黑名单违规

### 场景2：检测过度设计
**测试代码**：
简单用户CRUD功能 + Event Sourcing + CQRS
**预期输出**：建议移除Event Sourcing和CQRS，使用简单Repository模式
```

**验证流程**：
1. 创建Skill后，在SKILL.md底部添加测试场景
2. 手动运行测试：将测试代码提交给Claude，触发Skill
3. 检查Claude输出是否符合预期
4. 不符合则修改SKILL.md指令
5. 后续使用中持续验证

**优势**：
- ✅ 平衡质量和成本（仅1-2个简单场景）
- ✅ 测试场景即文档（帮助理解Skill用途）
- ✅ 可重复验证（修改后重新测试）
- ✅ 持续改进（发现新问题补充场景）

---

### ✅ Q6：Skills的性能和Token消耗（已确认）

**问题背景**：
- Skills会占用Context Token
- 大型Skills可能影响性能
- 需要控制规模，但避免过早优化

**最终决策**：✅ **选项D：实用主义（保持简洁，按需扩展）**

**性能策略**：
```
当前阶段：
- 仅创建3个核心Skills（MVP合规、架构合规、文档同步）
- 预估Token消耗：3000-4500 tokens（占200K预算的<3%）
- 无需复杂的性能优化

未来扩展：
- 如Skills数量>10个，或单个Skill>2000 tokens
- 再考虑按需加载或拆分优化
- 遵循YAGNI原则（避免过早优化）
```

**简洁性原则**：
```
每个Skill目标：1000-1500 tokens
- 避免冗长示例（测试场景1-2个即可）
- 避免重复内容（引用Constitution而非复制）
- 精简指令（聚焦核心检查流程）

示例对比：
❌ 冗长版本（3000 tokens）：
- 详细列举所有技术黑名单及解释
- 包含10个测试场景
- 重复Constitution内容

✅ 简洁版本（1200 tokens）：
- 引用Constitution技术黑名单
- 2个核心测试场景
- 聚焦检查流程
```

**监控和调整**：
- 如某个Skill超过2000 tokens → 考虑拆分
- 如总Token消耗超过10K → 考虑按需加载
- 定期审查（发现冗余内容时精简）

**优势**：
- ✅ 极简主义，零管理成本
- ✅ 避免过早优化（YAGNI）
- ✅ 灵活调整（按需扩展）
- ✅ 性能可控（当前占比<3%）

---

### ✅ Q7：Constitution检查的自动化程度（已确认）

**问题背景**：
- Constitution定义了强制性原则（技术黑名单、MVP原则）
- MVP合规Skill可以自动检查部分规则
- 需要平衡自动化效率和判断准确性

**最终决策**：✅ **选项B：半自动（检查 + 人工确认）**

**自动化策略**：
```
明确规则（自动检查，无需确认）：
├─ 技术黑名单检测
│  └─ Redis、CQRS、MediatR、Docker、GraphQL等
├─ 明显过度设计模式
│  └─ Event Sourcing用于简单CRUD
│  └─ 复杂抽象层用于简单场景
└─ 检测到违规 → 直接报告并建议修复

模糊规则（建议 + 人工确认）：
├─ 复杂度判断
│  └─ 这个抽象层是否必要？
│  └─ 提供分析报告 + 等待确认
├─ 架构选择
│  └─ 这个设计是否过度？
│  └─ 列出优缺点 + 等待决策
└─ 提供建议方案 → 用户确认后执行
```

**MVP合规Skill检查流程**：
```markdown
## 检查流程

### 第一步：自动检查（使用grep + serena工具）
1. 扫描技术黑名单关键字
   - 如发现 → 直接报告违规
2. 检测明显过度设计模式
   - 如发现 → 直接报告并建议简化

### 第二步：复杂度分析（使用sequential-thinking工具）
1. 分析代码复杂度
2. 评估设计合理性
3. 如发现可疑设计：
   - 生成分析报告（当前方案 vs 简单方案）
   - 列出优缺点对比
   - 等待用户确认是否修改

### 第三步：生成报告
- 自动检查结果（明确违规）
- 建议确认项（需决策）
- 修复建议
```

**分类示例**：

**自动检查（直接报告）**：
- ✅ 检测到`IDistributedCache` → 违规：Redis黑名单
- ✅ 简单CRUD + Event Sourcing → 违规：过度设计
- ✅ 使用MediatR → 违规：技术黑名单

**人工确认（提供建议）**：
- ❓ 三层抽象用于简单场景 → 建议：评估是否必要
- ❓ 自定义框架vs标准库 → 建议：对比分析
- ❓ 复杂工厂模式用于简单对象创建 → 建议：简化方案

**优势**：
- ✅ 明确违规立即发现（效率）
- ✅ 模糊情况人工决策（准确性）
- ✅ 平衡自动化和安全性
- ✅ 避免误判阻止合理设计

---

## 📊 可行性评估总结

### 技术可行性：⭐⭐⭐⭐⭐（高）
- ✅ Claude Code原生支持Skills
- ✅ 创建简单（SKILL.md即可）
- ✅ 可调用现有MCP工具
- ✅ 官方文档完善

### 成本效益分析：⭐⭐⭐⭐（中高）
- ✅ 开发成本低（每个Skill约1-2小时）
- ✅ 长期收益高（减少重复工作）
- ⚠️ 需要维护成本（随项目演进更新）
- ⚠️ 学习成本（团队需要理解Skills概念）

### 风险评估：⭐⭐⭐（中）
- ⚠️ Skills质量依赖指令编写
- ⚠️ 可能误导Claude（如指令不当）
- ✅ 可逐步引入（降低风险）
- ✅ 可随时禁用/删除

### 对当前MVP的影响：⭐⭐⭐⭐（正面）
- ✅ MVP合规检查直接支持当前任务
- ✅ 文档同步减少手动工作
- ✅ Issue生成节省时间
- ⚠️ 需要投入时间开发Skills（可能延迟1-2天）

---

## 🎯 最终实施方案（基于Q1-Q7决策）

### 📋 决策总结

| 问题 | 决策 | 关键要点 |
|-----|------|---------|
| Q1: 优先级 | 混合策略 | MVP合规 → 架构合规 → 文档同步 |
| Q2: 存放位置 | 混合模式（符号链接） | `.claude/skills/` + 符号链接到全局 |
| Q3: 工具协同 | Skills调用MCP工具 | Skills定义工作流，MCP工具执行操作 |
| Q4: 维护机制 | 实用主义 + 简单版本号 | 发现问题时更新，无需定期审查 |
| Q5: 测试验证 | 混合策略 | 首次1-2个测试场景 + 后续实际使用 |
| Q6: 性能控制 | 实用主义 | 每个Skill 1000-1500 tokens，避免过早优化 |
| Q7: 自动化程度 | 半自动 | 明确规则自动检查，模糊规则人工确认 |

---

### 🚀 Phase 1：核心Skills创建（立即开始，预计4-6小时）

**Skill 1：MVP合规检查（优先级最高）**
- 名称：`lybtzyzs-mvp-compliance`
- 功能：检测技术黑名单、过度设计
- 自动化：半自动（黑名单自动检查，复杂度建议确认）
- 目标规模：1200 tokens
- 测试场景：2个（Redis检测、过度设计检测）
- 预计开发时间：1-2小时

**Skill 2：架构合规检查（优先级高）**
- 名称：`lybtzyzs-arch-compliance`
- 功能：验证三层架构、DDD边界、Repository模式
- 自动化：半自动（依赖方向自动检查，边界建议确认）
- 目标规模：1500 tokens
- 测试场景：2个（依赖方向违规、聚合根边界）
- 预计开发时间：1-2小时

**Skill 3：文档同步检查（优先级中）**
- 名称：`lybtzyzs-doc-sync`
- 功能：检测API变更、架构调整、生成文档更新清单
- 自动化：半自动（API变更自动检测，影响范围建议确认）
- 目标规模：1300 tokens
- 测试场景：2个（API新增端点、架构调整）
- 预计开发时间：1-2小时

---

### Phase 2：延后的Skills（MVP完成后）
- Issue生成Skill
- 测试生成Skill
- 数据库迁移Skill

---

## ✅ 下一步行动计划

### 立即执行（按顺序）

**1. 环境准备（5分钟）**
- [ ] 创建项目目录：`.claude/skills/`
- [ ] 创建同步脚本：`scripts/setup-skills.ps1`
- [ ] 运行同步脚本，创建符号链接

**2. 创建MVP合规检查Skill（1-2小时）**
- [ ] 创建目录：`.claude/skills/lybtzyzs-mvp-compliance/`
- [ ] 编写SKILL.md（含版本号、检查流程、测试场景）
- [ ] 测试验证（提交测试代码，验证Claude输出）
- [ ] Git提交

**3. 创建架构合规检查Skill（1-2小时）**
- [ ] 创建目录：`.claude/skills/lybtzyzs-arch-compliance/`
- [ ] 编写SKILL.md（含版本号、检查流程、测试场景）
- [ ] 测试验证
- [ ] Git提交

**4. 创建文档同步Skill（1-2小时）**
- [ ] 创建目录：`.claude/skills/lybtzyzs-doc-sync/`
- [ ] 编写SKILL.md（含版本号、检查流程、测试场景）
- [ ] 测试验证
- [ ] Git提交

**5. 实战验证（30分钟）**
- [ ] 在实际MVP任务中使用Skills
- [ ] 收集反馈，优化指令
- [ ] 更新版本号和变更记录

---

### 成功标准

**Phase 1完成标志**：
- ✅ 3个Skills创建完成并通过测试
- ✅ 符号链接配置成功，Claude可自动加载
- ✅ 在至少1个真实任务中成功使用
- ✅ Git提交，Skills随项目版本控制

**预期收益**：
- 📊 减少技术违规（自动检测黑名单）
- 🏗️ 提升架构一致性（自动验证三层架构）
- 📝 减少文档手动维护（自动检测变更）
- ⏱️ 节省时间：约20-30%（每个任务节省10-15分钟）

---

## 参考资料

- [Claude Skills官方文档](https://docs.claude.com/en/docs/claude-code/skills)
- [Anthropic Skills GitHub仓库](https://github.com/anthropics/skills)
- [如何创建自定义Skills](https://support.claude.com/en/articles/12512198-how-to-create-custom-skills)
- [Simon Willison关于Skills的评论](https://simonwillison.net/2025/Oct/16/claude-skills/)
