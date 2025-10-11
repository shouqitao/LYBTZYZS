## 关联 Issue

<!-- 使用以下关键字之一关联 Issue: Closes, Fixes, Resolves -->
<!-- 示例: Closes #123 -->

**关闭Issue**: <!-- Closes #xxx -->

**关联Epic** (如适用): <!-- Epic #xxx -->

## 概述

<!-- 简要描述此 PR 的目的和改动内容 -->

## 实施内容

<!-- 详细列出实施的功能点或修复的问题 -->

### 主要改动
-
-
-

### 变更文件统计
<!-- 可使用 git diff --stat 生成 -->
```
<!-- 粘贴文件变更统计 -->
```

## 验收标准检查

<!-- 勾选已完成的验收标准 -->
- [ ] 符合 Issue 中的所有验收标准
- [ ] 代码遵循项目规范（命名、格式、注释）
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 相关测试通过
- [ ] 文档已同步更新

## 📁 文件位置检查

<!-- 确保所有文件都在正确的目录位置 -->
- [ ] 所有新增文件位于正确目录（docs/scripts/src/tests）
- [ ] 根目录无新增临时文件（参考白名单：`.config/root-files-whitelist.json`）
- [ ] 已检查.gitignore覆盖临时文件模式
- [ ] Pre-commit hook检查通过（如未通过请说明原因）

**文件位置规范提醒**：
- 📄 文档 → `docs/` 对应子目录
- 🔧 脚本 → `scripts/` 对应子目录
- 📊 报告 → `docs/reports/`
- 🖼️ 截图 → `docs/assets/screenshots/`
- ⚙️ 配置 → `.config/` 或 `config/`

## 编译与测试验证

<!-- 粘贴编译和测试命令及结果 -->

**编译验证**:
```bash
dotnet build LYBT.All.sln -c Release
```

**测试验证**:
```bash
dotnet test LYBT.Server.sln -c Release
# 或
dotnet test LYBT.Desktop.sln -c Release
```

**结果**:
```
<!-- 粘贴编译和测试输出 -->
```

## 技术合规检查

### 架构合规
- [ ] 遵循三层架构(Server)或四层MVVM(Desktop)
- [ ] 依赖注入仅用构造函数注入
- [ ] 异步方法正确使用async/await
- [ ] 不使用黑名单技术(Redis/CQRS/Docker/微服务/GraphQL/消息队列)

### 代码质量
- [ ] 命名规范: 类型PascalCase,私有字段_camelCase
- [ ] 文件编码: UTF-8 with BOM
- [ ] 单文件代码 ≤500行
- [ ] 代码注释完整(中文)

### 测试覆盖
- [ ] 新增/修改代码有对应单元测试
- [ ] 测试覆盖率 ≥80% (核心逻辑)
- [ ] 集成测试通过(如适用)

## AI 审查清单

- [ ] **GitHub Copilot 初审** (自动触发)
- [ ] **Claude Code 二审** (评论模式，可选)

> **说明**:
> - **GitHub Copilot**: 自动对 PR 进行初审，检查代码规范和最佳实践
> - **Claude Code**: 可选的深度架构审查，以评论模式发布审查意见（因 GitHub 限制，PR 作者不能 approve 自己的 PR）
> - 如需 Claude Code 审查，请在评论中提及或手动触发 `/code-review`

## 影响评估

**风险等级**: <!-- 🟢 低 / 🟡 中 / 🔴 高 -->

**影响范围**:
- **模块**: <!-- Server/Desktop/Shared/Tests -->
- **影响用户**: <!-- 所有用户/管理员/开发者 -->
- **破坏性变更**: <!-- 是/否，如是请说明 -->

**是否需要数据迁移**: <!-- 是/否 -->

**性能影响**: <!-- 无/优化/可能降低(说明原因) -->

## 回归测试

<!-- 列出需要回归测试的功能点 -->
- [ ] 用户登录流程
- [ ] 数据查询与展示
- [ ] 权限验证
- [ ] <!-- 其他关键功能 -->

## 文档更新

<!-- 勾选已更新的文档 -->
- [ ] API 文档 (`docs/api/`)
- [ ] 架构文档 (`docs/architecture/`)
- [ ] 开发指南 (`docs/development/`)
- [ ] README 或索引文件
- [ ] 报告归档 (`docs/reports/INDEX.md`)
- [ ] 不需要文档更新

## Epic 进度同步

<!-- 如果PR关联Epic，自动触发epic-sync -->
<!-- epic-sync工作流会在PR合并后自动更新Epic状态 -->

**Epic关联**: <!-- Epic #xxx -->
**Task完成**: <!-- 本PR完成哪个Task: #xxx -->
**Epic剩余Task**: <!-- 还有多少Task未完成 -->

## 其他说明

<!-- 可选：补充说明、已知问题、后续计划等 -->

### 已知限制
<!-- 本PR的已知限制或待优化项 -->

### 后续计划
<!-- 后续需要继续完成的工作 -->

### 参考资料
<!-- 相关文档、讨论、设计方案等链接 -->

---

## ✅ PR 提交前最终检查

提交PR前，请确认以下所有项:

- [ ] PR标题符合规范: `{type}({scope}): {description}`
- [ ] 关联了正确的Issue (使用 Closes/Fixes/Resolves #xxx)
- [ ] 如果是Epic Task，添加了 `epic:{epic-name}` 标签
- [ ] 所有验收标准已勾选
- [ ] 编译和测试结果已粘贴
- [ ] 技术合规检查已完成
- [ ] 文档已同步更新
- [ ] 代码已格式化 (`dotnet format`)
- [ ] PR描述清晰完整

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
