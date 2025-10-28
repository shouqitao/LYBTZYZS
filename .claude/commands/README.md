# Claude Code Slash Commands - LYBTZYZS项目专用命令

**版本**: 1.0.0
**创建日期**: 2025-10-09
**灵感来源**: [SuperClaude Framework](https://github.com/SuperClaude-Org/SuperClaude_Framework)

本目录包含25个专业化的slash命令，用于提升LYBTZYZS项目的AI辅助开发效率。

---

## 📋 命令分类索引

### 🏗️ 架构与代码质量（Architecture & Quality）

| 命令 | 描述 | 使用场景 |
|------|------|---------|
| `/review-arch` | 架构合规性审查 | PR合并前检查、架构验证 |
| `/code-review` | 代码质量审查 | 代码审查、质量检查 |
| `/analyze-complexity` | 复杂度分析 | 识别需要简化的代码 |
| `/analyze-dependencies` | 依赖关系分析 | 检测循环依赖、违规依赖 |
| `/code-rabbit` | CodeRabbit评审处理 | 处理CodeRabbit评审意见 |

### ⚡ 性能与安全（Performance & Security）

| 命令 | 描述 | 使用场景 |
|------|------|---------|
| `/analyze-perf` | 性能分析 | 识别性能瓶颈、优化建议 |
| `/analyze-queries` | 数据库查询分析 | 检测N+1问题、慢查询 |
| `/security-scan` | 安全扫描 | 检测安全漏洞和风险 |

### 🔄 重构与规划（Refactoring & Planning）

| 命令 | 描述 | 使用场景 |
|------|------|---------|
| `/refactor-plan` | 重构规划（UltraThink） | 大型重构前的深度分析 |
| `/brainstorm` | 头脑风暴 | 探索问题解决方案 |
| `/deep-research` | 深度研究 | 技术调研、最佳实践研究 |

### 🧪 测试（Testing）

| 命令 | 描述 | 使用场景 |
|------|------|---------|
| `/test-coverage` | 测试覆盖率分析 | 识别未覆盖代码 |
| `/generate-tests` | 生成测试代码 | 自动生成单元测试模板 |

### 📝 文档（Documentation）

| 命令 | 描述 | 使用场景 |
|------|------|---------|
| `/generate-readme` | 生成README | 为模块生成文档 |
| `/update-docs` | 更新文档 | 同步代码变更到文档 |
| `/generate-api-doc` | 生成API文档 | WebAPI文档生成 |
| `/release-notes` | 生成发布说明 | 版本发布时生成Changelog |

### 🔧 代码生成（Code Generation）

| 命令 | 描述 | 使用场景 |
|------|------|---------|
| `/generate-dto` | 生成DTO类 | 基于Entity生成DTO |
| `/generate-migration` | 生成EF迁移 | 数据库迁移脚本生成 |

### 📦 项目管理（Project Management）

| 命令 | 描述 | 使用场景 |
|------|------|---------|
| `/create-issue` | 创建Issue | 标准化Issue创建 |
| `/generate-pr` | 生成PR描述 | 自动生成PR描述 |
| `/sprint-summary` | Sprint总结 | 生成周期总结报告 |
| `/re-init` | 更新项目规范 | 同步CLAUDE.md规范到项目 |

### 💡 通用助手（General Assistant）

| 命令 | 描述 | 使用场景 |
|------|------|---------|
| `/ask` | 智能问答 | 项目相关问题解答 |
| `/prompt` | 复杂提示处理 | 处理带@引用的复杂提示 |

---

## 🚀 快速开始

### 使用命令
在Claude Code对话中直接输入斜杠命令：
```
/review-arch
```

### 带参数的命令
```
/code-review src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs
/analyze-perf Module.Patients
/create-issue bug 用户登录失败
```

---

## 🧠 工作模式（7种行为模式）

基于SuperClaude的理念，我们定义了7种专业化的工作模式（详见 `CLAUDE.md` 工作模式章节）：

1. **🔍 Code Review Mode** - 代码审查模式
2. **🏗️ Architecture Mode** - 架构审查模式
3. **⚡ Performance Mode** - 性能优化模式
4. **🔄 Refactoring Mode** - 重构规划模式
5. **🧪 Testing Mode** - 测试驱动模式
6. **📝 Documentation Mode** - 文档同步模式
7. **🧠 Research Mode** - 深度研究模式

---

## 🎯 命令设计原则

### 1. **专业化**
每个命令专注于一个特定任务，避免功能重叠。

### 2. **标准化**
所有命令遵循项目规范：
- 符合 `docs/development/README.md`
- 遵守 `docs/quick-reference/development-checklist.md`
- 对接 `docs/architecture/` 架构标准

### 3. **自动化**
充分利用MCP工具：
- `mcp__serena__*` - 语义代码分析
- `mcp__sequential-thinking` - 深度思考
- `git`、`gh` - 版本控制和Issue管理
- `context7` - 库文档查询

### 4. **可组合**
命令之间可以组合使用：
```
/analyze-perf → /create-issue → /refactor-plan → /generate-pr
```

---

## 📊 与SuperClaude对比

| 特性 | SuperClaude | LYBTZYZS |
|------|-------------|----------|
| **Slash命令数** | 25个 | 25个 ✅ |
| **行为模式** | 7种 | 7种 ✅ |
| **MCP服务器** | 8个 | 8个 ✅ |
| **AI代理** | 15个 | 通过命令实现 ✅ |
| **项目特化** | 通用 | LYBTZYZS专用 ✅ |

---

## 🔧 工具集成

### 已集成的MCP工具
1. **serena** - 语义代码检索与编辑
2. **context7** - 库文档查询
3. **memory** - 知识图谱存储
4. **sequential-thinking** - 结构化推理（UltraThink）
5. **git** - 版本控制
6. **filesystem** - 文件操作
7. **playwright** - 浏览器自动化
8. **time** - 时间工具

---

## 📚 参考资料

### 项目文档
- `docs/development/README.md` - 技术标准
- `docs/quick-reference/development-checklist.md` - 工作流程
- `docs/architecture/` - 架构标准
- `CLAUDE.md` - AI协同工作规范

### 灵感来源
- [SuperClaude Framework](https://github.com/SuperClaude-Org/SuperClaude_Framework) - 16.6k stars，MIT协议
- 借鉴其命令化、模式化的设计理念
- 结合LYBTZYZS项目实际需求定制化

---

## 🎓 最佳实践

### 命令执行顺序推荐

#### 新功能开发
```
1. /brainstorm     → 探索方案
2. /create-issue   → 创建Issue
3. /generate-tests → 生成测试骨架
4. [实现代码]
5. /code-review    → 代码审查
6. /test-coverage  → 检查覆盖率
7. /generate-pr    → 生成PR描述
```

#### 重构任务
```
1. /analyze-perf      → 识别性能问题
2. /analyze-complexity → 识别复杂代码
3. /refactor-plan     → UltraThink规划
4. /create-issue      → 创建Epic
5. [执行重构]
6. /review-arch       → 架构验证
7. /generate-pr       → 生成PR
```

#### 发布准备
```
1. /review-arch     → 架构检查
2. /security-scan   → 安全扫描
3. /test-coverage   → 覆盖率检查
4. /release-notes   → 生成Release Notes
5. /sprint-summary  → 生成总结
```

---

## 🤝 贡献

### 添加新命令
1. 在 `.claude/commands/` 创建 `{name}.md`
2. 遵循现有命令的格式
3. 更新本README的索引表
4. 测试命令功能

### 命令模板
```markdown
# 命令标题 (/command-name)

简要描述命令的功能。

## 执行流程
1. 步骤1
2. 步骤2

## 使用方式
\`\`\`
/command-name [参数]
\`\`\`

## 输出格式
{描述输出内容}
```

---

**🤖 Created with Claude Code**

Version: 1.0.0 | Last Updated: 2025-10-09
