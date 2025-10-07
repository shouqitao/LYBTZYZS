## 关联 Issue

<!-- 使用以下关键字之一关联 Issue: Closes, Fixes, Resolves -->
<!-- 示例: Closes #123 -->

## 概述

<!-- 简要描述此 PR 的目的和改动内容 -->

## 实施内容

<!-- 详细列出实施的功能点或修复的问题 -->

### 主要改动
-
-
-

## 验收标准检查

<!-- 勾选已完成的验收标准 -->
- [ ] 符合 Issue 中的所有验收标准
- [ ] 代码遵循项目规范（命名、格式、注释）
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 相关测试通过
- [ ] 文档已同步更新

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

## AI 审查清单

- [ ] **GitHub Copilot 初审** (自动触发)
- [ ] **Claude Code 二审** (评论模式，可选)

> **说明**:
> - **GitHub Copilot**: 自动对 PR 进行初审，检查代码规范和最佳实践
> - **Claude Code**: 可选的深度架构审查，以评论模式发布审查意见（因 GitHub 限制，PR 作者不能 approve 自己的 PR）
> - 如需 Claude Code 审查，请在评论中提及或手动触发

## 影响评估

**风险等级**: <!-- 🟢 低 / 🟡 中 / 🔴 高 -->

**影响范围**:
-

**是否需要数据迁移**: <!-- 是/否 -->

## 其他说明

<!-- 可选：补充说明、已知问题、后续计划等 -->

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
