# 生成PR描述命令 (/generate-pr)

基于当前分支的commits和Issue内容，自动生成符合项目规范的Pull Request描述。

## 📋 执行流程

### 1️⃣ 收集信息
```bash
# 获取当前分支名和Issue号
git branch --show-current

# 获取提交历史（从分支点到HEAD）
git log --oneline $(git merge-base HEAD master)..HEAD

# 获取文件变更统计
git diff --stat $(git merge-base HEAD master)..HEAD

# 获取关联的Issue信息
gh issue view <issue-number> --json title,body,labels
```

### 2️⃣ 分析变更内容
使用MCP工具分析代码变更：
- `mcp__serena__search_for_pattern` - 搜索关键变更模式
- `git diff --name-only` - 获取变更文件列表
- 统计新增/修改/删除的行数

### 3️⃣ 生成PR描述

#### 标准模板结构
```markdown
## 📋 Issue 关联

Fixes #<issue-number>
Related #<related-issue>

## 🎯 功能清单

### [模块-1] 功能描述
- ✅ 子任务1
- ✅ 子任务2
- ✅ 子任务3

### [模块-2] 功能描述
- ✅ 子任务1
- ✅ 子任务2

## 🔍 编译验证

\`\`\`bash
# 编译验证命令
dotnet build LYBT.All.sln -c Release
# 结果: 编译成功，0个错误，0个警告

# 测试验证命令
dotnet test LYBT.Server.sln -c Release
# 结果: 已通过! - 失败: 0，通过: X，总计: X
\`\`\`

## ✅ 验收标准

- [x] **编译通过**: 0错误0警告
- [x] **测试通过**: 所有单元测试通过
- [x] **架构合规**: 架构测试通过
- [x] **文档同步**: 相关文档已更新
- [x] **代码规范**: 符合命名和代码规范

## 📊 产出文件

### 代码
- `path/to/file.cs`: +XX行，功能描述

### 文档
- `docs/path/to/doc.md`: 新增/更新，内容说明

### 测试
- `tests/path/to/test.cs`: +XX行，XX个测试

## 📚 相关文档

- [技术标准](https://github.com/shouqitao/LYBTZYZS/blob/master/docs/development/standards.md)
- [相关设计文档](相关文档链接)

## 🤖 AI 审查清单

- [ ] **GitHub Actions 自动审查**:
- [ ] **Claude Code 初审**:

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
\`\`\`

### 4️⃣ 自动填充内容

- **Issue关联**：从分支名提取Issue号（如refactor/issue-1113 → #1113）
- **功能清单**：从commits的message提取（解析[模块-X]前缀）
- **编译验证**：自动执行编译和测试命令
- **产出文件**：从git diff统计生成
- **验收标准**：基于Issue的验收标准自动勾选

### 5️⃣ 验证PR内容

检查生成的PR描述是否包含：
- ✅ 明确的Issue关联（Fixes #XX）
- ✅ 清晰的功能清单
- ✅ 编译验证结果
- ✅ 完整的验收标准
- ✅ 产出文件列表

## 🎯 使用场景

- 完成功能清单后准备创建PR
- 需要生成标准化的PR描述
- 确保PR包含所有必要信息

## ⚡ 快速使用

在对话中输入：`/generate-pr`

Claude Code将：
1. 分析当前分支和commits
2. 读取关联的Issue
3. 自动生成完整的PR描述
4. 执行编译和测试验证
5. 输出可直接用于gh pr create的文本

## 💡 高级用法

### 指定Issue号
\`\`\`
/generate-pr #1113
\`\`\`

### 包含特定验证
\`\`\`
/generate-pr --with-tests --with-coverage
\`\`\`
