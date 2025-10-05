## 📋 变更摘要
描述本 PR 解决的问题与范围，并引用功能清单编号（例如 [SRV-1], [CLI-1], [DOC-1]）。

**相关 Issue**: Fixes #<issue_number>

**功能清单编号（必填）**: [XXX-1] [XXX-2]

---

## ✅ 编译验证（必填）

粘贴本地编译命令与结果摘要（成功截图/日志片段）。

```powershell
# Server 端编译
dotnet restore LYBT.Server.sln
dotnet build LYBT.Server.sln -c Release

# Client 端编译（如果涉及）
dotnet restore LYBT.Desktop.sln
dotnet build LYBT.Desktop.sln -c Release

# 完整解决方案编译
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release
```

**编译结果**:
- [ ] ✅ Server 编译成功
- [ ] ✅ Client 编译成功（如适用）
- [ ] ✅ 无编译警告（或已说明原因）

---

## 🧪 验收验证（按 AC）

**测试步骤**:
1.
2.
3.

**测试结果**:
- [ ] 所有验收标准（AC）已通过
- [ ] 手动测试完成
- [ ] 边界条件已测试

---

## 📊 测试覆盖

**单元测试**:
- [ ] 已添加新测试（核心逻辑必填）
- [ ] 已更新现有测试（如修改了逻辑）
- [ ] 测试全部通过

```powershell
# 运行单元测试命令
dotnet test <test_project_path> --configuration Release
```

**测试结果**:
- 新增测试数: X 个
- 测试通过率: 100%
- 覆盖率: XX% (运行 `dotnet test --collect:"XPlat Code Coverage"`)

---

## 🔍 影响范围

**代码变更**:
- 修改的模块:
- 新增的类/方法:
- 删除的代码:

**配置变更**:
- [ ] 无配置变更
- [ ] 修改了 appsettings.json
- [ ] 修改了 .csproj 或 .sln
- [ ] 修改了 NuGet 包依赖

**数据库变更**:
- [ ] 无数据库变更
- [ ] 新增 Migration
- [ ] 修改了 Entity

**文档变更**:
- [ ] 已更新相关文档（`docs/architecture/`, `docs/api/`）
- [ ] 已更新模块 README
- [ ] 无需更新文档

---

## ⚠️ 风险与回滚

**潜在风险**:
-

**回滚方案**:
-

**破坏性变更**:
- [ ] 无破坏性变更
- [ ] 有破坏性变更（已说明升级路径）

---

## 🤖 AI 双审查（GitHub Pro）

### Claude Code 自动初审
- [ ] ✅ Claude Code Review 已通过
- [ ] 🔴 发现严重问题（必须修复后再提交）
- [ ] 🟡 有改进建议（已评估是否修复）

### GitHub Copilot 二审
- [ ] ✅ Copilot Code Review 已通过
- [ ] 🔴 发现严重问题（必须修复）
- [ ] 🟡 有改进建议（已评估是否修复）

**审查反馈处理**:
-

---

## 👤 人工审查

**请求审查者**: @<github_username>

**审查清单**:
- [ ] 代码逻辑正确
- [ ] 符合项目架构标准（Record-Only 系统）
- [ ] 命名规范清晰
- [ ] 注释充分
- [ ] 测试覆盖充分
- [ ] 文档已同步更新

---

## ✅ 提交前检查清单

- [ ] 所有编译通过
- [ ] 所有测试通过
- [ ] Claude Code Review 通过
- [ ] Copilot Code Review 通过
- [ ] 功能清单编号已引用
- [ ] 文档已更新
- [ ] 提交信息清晰规范

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
