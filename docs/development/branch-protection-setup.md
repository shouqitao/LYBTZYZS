# 分支保护配置指南 - GitHub Pro

**目的**: 利用 GitHub Pro 权益，配置严格的分支保护规则，强制执行代码审查与 CI 检查。

**相关**: Issue #935 - GitHub Pro 双审查机制配置

---

## 📋 配置步骤

### 1. 进入仓库设置

1. 打开 GitHub 仓库页面: https://github.com/shouqitao/LYBTZYZS
2. 点击 **Settings** (设置) 标签
3. 在左侧菜单中，点击 **Branches** (分支)

### 2. 添加分支保护规则

点击 **Add rule** (添加规则) 按钮，配置以下规则：

---

## 🔒 `master` 分支保护规则

### 基础配置

**Branch name pattern** (分支名称模式):
```
master
```

### 规则 1: 要求 Pull Request 审查

- ✅ **Require a pull request before merging** (合并前需要 Pull Request)
  - ✅ **Require approvals** (需要审批): **1** 次审批
  - ✅ **Dismiss stale pull request approvals when new commits are pushed** (新提交时撤销旧审批)
  - ✅ **Require review from Code Owners** (需要 CODEOWNERS 审查) ⭐ **GitHub Pro 功能**

### 规则 2: 要求状态检查通过

- ✅ **Require status checks to pass before merging** (合并前需要状态检查通过)
  - ✅ **Require branches to be up to date before merging** (需要分支是最新的)

  **必须通过的检查项** (点击搜索框选择):
  - ✅ `CI - Main Pipeline / code-quality-gate`
  - ✅ `CI - Main Pipeline / test-quality-gate`
  - ✅ `CI - Coverage / coverage-check`
  - ✅ `CI - Quality / architecture-compliance`
  - ✅ `CI - Quality / governance-validation`
  - ✅ `Claude Code Review / claude-review`
  - ✅ `Docs Sync / check`
  - ✅ `Validate and Track / build`

### 规则 3: 要求线性提交历史

- ✅ **Require linear history** (需要线性历史)
  - 强制使用 Squash merge 或 Rebase merge，禁止 Merge commit

### 规则 4: 要求签名提交（可选）

- ⚠️ **Require signed commits** (需要签名提交)
  - 根据团队需求选择是否启用

### 规则 5: 锁定分支（阻止直接推送）

- ✅ **Require a pull request before merging** 已自动启用此功能
- ℹ️ 即使是管理员也无法直接推送到 `master`

### 规则 6: 不允许绕过保护规则

- ❌ **Do not allow bypassing the above settings** (不允许绕过上述设置)
  - 确保所有人（包括管理员）都遵守规则

### 规则 7: 限制谁可以推送到匹配的分支

- ✅ **Restrict who can push to matching branches** (限制谁可以推送)
  - 添加允许合并的团队或用户
  - 通常仅允许项目维护者

---

## 🔒 `develop` 分支保护规则（可选）

如果使用 `develop` 分支作为集成分支，可以配置相对宽松的规则：

**Branch name pattern**:
```
develop
```

### 基础规则

- ✅ **Require a pull request before merging**
  - **Require approvals**: **0** 次（或 1 次，根据需求）
- ✅ **Require status checks to pass before merging**
  - 选择关键 CI 检查（如 `CI - Main Pipeline`）

---

## 🔒 Release 分支保护规则

**Branch name pattern**:
```
release/*
```

### 基础规则

- ✅ **Require a pull request before merging**
  - **Require approvals**: **2** 次（更严格）
- ✅ **Require status checks to pass before merging**
  - 所有 CI 检查必须通过
- ✅ **Require linear history**

---

## 📊 保护规则优先级

GitHub 分支保护规则按照以下优先级匹配：
1. 精确匹配（如 `master`）
2. 通配符匹配（如 `release/*`）
3. 默认分支设置

---

## ✅ 验证分支保护

### 测试步骤

1. **尝试直接推送到 `master`**:
   ```powershell
   git checkout master
   git commit -m "test"
   git push origin master
   ```
   **预期结果**: ❌ 推送失败，提示需要 Pull Request

2. **创建 PR 并尝试在 CI 未通过时合并**:
   - 创建一个会导致 CI 失败的 PR
   - **预期结果**: ❌ Merge 按钮被禁用，提示需要 CI 通过

3. **创建 PR 并在未审批时尝试合并**:
   - 创建一个 CI 通过的 PR
   - **预期结果**: ❌ Merge 按钮被禁用，提示需要审批

4. **完整流程测试**:
   - 创建 PR
   - 等待 Claude Code Review 和 Copilot Review 完成
   - 等待所有 CI 检查通过
   - 请求人工审批
   - **预期结果**: ✅ Merge 按钮可用，可以合并

---

## 🎁 GitHub Pro 专属功能

启用以下功能以充分利用 GitHub Pro 权益：

### 1. CODEOWNERS 自动审查
- ✅ **Require review from Code Owners** (分支保护规则中)
- 📄 配置文件: `.github/CODEOWNERS`

### 2. Multiple Reviewers
- ✅ 可以要求多个审批者（非 Pro 版本仅支持 1 个）
- 根据 PR 重要性调整审批数量

### 3. Draft Pull Requests
- 📝 使用 Draft PR 进行 WIP（Work In Progress）开发
- Draft PR 不会触发通知，减少噪音

### 4. Protected branches for private repos
- 🔒 私有仓库也可以使用分支保护（非 Pro 版本仅限公开仓库）

### 5. Code review assignments
- 👥 自动根据 CODEOWNERS 分配审查者
- 均衡审查负载

---

## 📝 配置检查清单

完成分支保护配置后，勾选以下项目：

- [ ] `master` 分支保护规则已配置
- [ ] 需要至少 1 次审批
- [ ] 需要 CODEOWNERS 审查
- [ ] 所有关键 CI 检查已添加
- [ ] 需要线性提交历史
- [ ] 不允许绕过保护规则
- [ ] 已测试保护规则有效

---

## 🔗 相关资源

- **GitHub Docs - Protected Branches**: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches
- **GitHub Docs - CODEOWNERS**: https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners
- **GitHub Docs - Status Checks**: https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/about-status-checks

---

**最后更新**: 2025-10-05
**维护人**: Claude Code + GitHub Pro 管理员
