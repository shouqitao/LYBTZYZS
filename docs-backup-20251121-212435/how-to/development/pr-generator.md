---
name: lybtzyzs-pr-generator
description: 为LYBTZYZS项目自动生成Pull Request描述，分析git commits提取Issue关联、生成符合项目规范的PR标题和描述、可选执行编译验证、分析代码影响范围、自动添加Claude Code标记。触发关键词：生成PR、创建Pull Request、PR描述、合并请求、generate PR、create pull request、merge request
---

# LYBTZYZS PR描述自动生成器

## 核心能力

1. **智能分析commits** - 提取Issue关联（#1567）、识别commit类型（feat/fix/refactor）
2. **生成标准PR标题** - 遵循约定式提交规范（feat/fix/refactor(scope): description）
3. **提取问题背景** - 从关联Issue中提取问题描述和目标
4. **分析代码变更** - 统计文件变更（新增/修改/删除）、代码行数统计
5. **执行编译验证**（可选）- 验证代码编译通过、执行测试套件
6. **生成影响范围报告** - 分析变更影响的模块和功能
7. **自动添加标记** - 添加Claude Code生成标记和Co-Authored-By
8. **直接在GitHub创建PR** - 自动创建PR并返回URL

## 何时使用

- 功能分支开发完成，准备合并到主分支
- 需要规范化PR描述，确保信息完整
- PR涉及多个commits，需要自动汇总
- 需要验证代码编译和测试状态
- 新成员不熟悉PR描述规范

## 工作流程

1. 检测当前分支和目标分支（默认：master）
2. 获取commits列表和diff统计
3. 提取Issue关联（从commit message）
4. 从GitHub获取Issue详情（标题、描述、验收标准）
5. 可选：执行编译和测试验证
6. 生成PR标题和描述（符合项目规范）
7. 在GitHub上创建PR并返回URL

## 输入要求

**必需**：
- 无（默认从当前分支创建PR到master）

**可选**：
- `target_branch` - 目标分支（默认：`master`）
- `verify_build` - 是否执行编译验证（默认：`false`）
  - `true` - 执行 `dotnet build` 和 `dotnet test`
  - `false` - 跳过验证
- `additional_notes` - 额外的PR说明（补充信息）

## 输出格式

**最终输出**：
- ✅ **GitHub PR URL**（主要输出）：如 `https://github.com/shouqitao/LYBTZYZS/pull/123`
- ✅ **PR编号**：如 `#123`
- ✅ **PR状态**：Open
- ✅ **编译状态**（如果执行验证）：✅ 通过 / ❌ 失败

**PR内容格式**（在GitHub上显示）：

```markdown
## 📋 PR标题
refactor(medicalcase): Issue #1567 - 医案流程UI重构

## 🔗 关联Issue
Closes #1567
Related #1565

## 🎯 问题背景

[从Issue.body自动提取]

当前医案流程UI存在以下问题：
1. 患者信息不始终可见，导致医生操作时需要频繁切换
2. 步骤导航不清晰，用户不知道当前进度
3. 上一步/下一步按钮逻辑混乱，不同步骤行为不一致

## 💡 解决方案

本PR实施以下改进：
1. **患者信息条始终可见** - 在顶部固定显示患者基本信息
2. **统一步骤导航** - 中央显示当前步骤名称和进度
3. **标准化操作按钮** - 统一上一步/下一步按钮样式和行为
4. **引入Region导航** - 使用Prism Region实现模块化视图切换

## 🔧 核心改动

### 1. UI架构调整
- 修改 `MedicalCaseFlowView.xaml` 布局结构（4行Grid）
- Row 0: 顶部导航栏（返回主页 + 当前步骤）
- Row 1: 患者信息条（始终可见）
- Row 2: 主内容区（Region动态加载）
- Row 3: 底部操作栏（上一步 + 下一步 + 暂停）

### 2. ViewModel逻辑优化
- 重构 `MedicalCaseFlowViewModel.cs` 步骤导航逻辑
- 引入 `ConsultationStep` 枚举（替代旧的FlowStep）
- 实现 `PreviousStepCommand` 和 `NextStepCommand`
- 支持Prism Region导航（`WorkflowContentRegion`）

### 3. 新增患者选择模块
- 创建 `PatientSelectionView.xaml` 和 `PatientSelectionViewModel.cs`
- 实现患者列表展示和搜索功能
- 集成到医案流程起始点

## 📝 提交记录（9个commits）

1. `0c5801b4` - fix(medicalcase): 修复返回主页按钮根据用户角色导航
2. `7c452259` - fix(patients): 修复PatientSelectionView数据绑定错误
3. `3fb73927` - feat(medicalcase): Step 1上一步按钮显示"开始看诊"
4. `1fed13bf` - fix(medicalcase): 修复Step 2-3上一步按钮颜色（绿色主题）
5. `b35ded7a` - fix(medicalcase): 修复上一步按钮的DataTrigger与Command.CanExecute冲突
6. `43a6ce95` - feat(medicalcase): 优化医案流程UI布局和导航逻辑
7. `a1b2c3d4` - refactor(medicalcase): 重构步骤枚举和导航参数
8. `e5f6g7h8` - feat(medicalcase): 新增患者选择视图和ViewModel
9. `i9j0k1l2` - docs(medicalcase): 更新医案模块架构文档

## ✅ 测试验证

### 编译状态
✅ **编译通过**: 0 errors, 2 warnings

```
dotnet build LYBT.All.sln -c Release --no-restore
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 测试状态
✅ **测试通过**: 45/45 tests passed

```
dotnet test LYBT.All.sln -c Release --no-build
Test Run Successful.
Total tests: 45
     Passed: 45
```

## 📦 影响范围

### 新增文件（6个）
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PatientSelectionViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PatientSelectionView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PatientSelectionView.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/ConsultationStep.cs`
- `tests/Client/LYBT.Desktop.MedicalCase.Tests/ViewModels/PatientSelectionViewModelTests.cs`
- `docs/architecture/client/medicalcase-ui-refactor.md`

### 修改文件（7个）
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs`
- `docs/architecture/client/mvvm-architecture.md`
- `docs/modules/medicalcase/README.md`
- `README.md`

### 代码统计
- **+3,804 additions** - 新增代码
- **-434 deletions** - 删除代码
- **净增加**: +3,370 lines

### 影响模块
- ✅ **Client/MedicalCase模块** - 核心变更
- ✅ **Client/Clinical角色模块** - 导航入口调整
- ✅ **文档系统** - 架构文档同步更新

## 📚 相关文档

- 架构设计：`docs/architecture/client/medicalcase-ui-refactor.md`
- MVVM规范：`docs/development/client/mvvm-patterns.md`
- Prism导航：`docs/development/client/prism-navigation-guide.md`
- 验收标准：Issue #1567验收清单

## ⚠️ 注意事项

1. **向后兼容性** - 本次重构保持API接口不变，不影响其他模块
2. **数据迁移** - 无需数据库迁移，仅UI层改动
3. **测试覆盖** - 新增ViewModel已补充单元测试
4. **文档同步** - 架构文档已同步更新

## 🔜 后续计划

- [ ] Issue #1568 - 实现患者选择时检测未完成医案
- [ ] Issue #1570 - 优化医案流程性能（加载速度<1秒）
- [ ] Issue #1571 - 添加医案流程E2E测试

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

## 技术实现

**使用的MCP工具链**:
1. **Bash (git)** - 获取commits、diff统计、分支信息
   ```bash
   git log origin/master..HEAD --oneline
   git diff origin/master..HEAD --stat
   git status
   git remote get-url origin
   ```
2. **mcp__github__search_issues** - 通过关键词搜索关联Issue
3. **mcp__github__get_issue** - 获取Issue详情（标题、描述、验收标准）
4. **mcp__github__create_pull_request** - 在GitHub创建PR
5. **Bash (dotnet)** - 编译和测试验证（可选）
   ```bash
   dotnet build LYBT.All.sln -c Release --no-restore
   dotnet test LYBT.All.sln -c Release --no-build
   ```
6. **mcp__serena** - 代码影响分析（可选）
   ```
   find_symbol: 分析变更文件的符号
   find_referencing_symbols: 分析影响范围
   ```

**实现逻辑**:
```
1. 环境检测 → 当前分支、目标分支、是否有远程仓库
2. 获取commits → git log origin/master..HEAD --oneline
3. 提取Issue编号 → 正则匹配 #\d+ 或 Issue #\d+
4. 获取Issue详情 → mcp__github__get_issue（批量）
5. 分析变更统计 → git diff --stat（文件数、代码行数）
6. 可选：编译验证 → dotnet build + dotnet test
7. 生成PR内容 → 标题（feat/fix/refactor(scope): Issue #XXXX）
                → 描述（问题背景 + 解决方案 + 改动 + 统计）
8. 创建PR → mcp__github__create_pull_request
9. 输出结果 → PR URL + 编号 + 编译状态
```

## PR标题规范

**格式**: `<type>(<scope>): Issue #<number> - <description>`

**type类型**:
- `feat` - 新功能
- `fix` - Bug修复
- `refactor` - 重构（不改变功能）
- `perf` - 性能优化
- `docs` - 文档更新
- `test` - 测试相关
- `chore` - 构建/工具链变更

**scope范围**:
- `medicalcase` - 医案模块
- `patients` - 患者模块
- `prescriptions` - 处方模块
- `herbs` - 药材模块
- `formula` - 方剂模块
- `auth` - 认证模块
- `shared` - 共享组件
- `server` - 服务端通用
- `client` - 客户端通用

**示例**:
- ✅ `feat(medicalcase): Issue #1567 - 医案流程UI重构`
- ✅ `fix(patients): Issue #1401 - 修复患者列表加载400错误`
- ✅ `refactor(prescriptions): Issue #1402 - 处方Service层优化`

## 限制条件

- 需要git仓库配置了GitHub远程地址
- 需要GitHub认证（通过mcp__github工具）
- 当前分支必须有commits（相对于目标分支）
- 编译验证需要.NET SDK已安装（dotnet命令可用）
- Issue关联需要commits中包含 `#<number>` 或 `Issue #<number>`
- 性能：<30秒（不含编译），<3分钟（含编译和测试）

## 最佳实践

1. **清晰的commit message** - 每个commit包含Issue编号（#1567）
2. **原子性commits** - 每个commit做一件事，便于生成描述
3. **先验证再创建PR** - 使用 `verify_build: true` 确保代码通过编译
4. **补充额外说明** - 使用 `additional_notes` 添加特殊说明（如破坏性变更）
5. **及时更新文档** - PR涉及架构变更时，确保文档已同步
6. **关注代码统计** - 大规模变更（>1000行）需要特别说明

## 性能指标

- **基础信息收集**（git + github）：<10秒
- **Issue详情获取**（批量API）：<5秒
- **PR内容生成**：<3秒
- **GitHub PR创建**：<2秒
- **端到端完成**（不含编译）：<30秒
- **编译验证**（可选）：1-3分钟（取决于项目大小）
- **测试执行**（可选）：30秒-2分钟

**性能优化策略**:
- 并行获取多个Issue详情
- 缓存git diff结果
- 编译验证可选（默认跳过）
- 增量编译（--no-restore）

## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0 | 2025-10-22 | 初始版本，支持自动生成PR描述和GitHub创建 |

---

**维护者**：Claude Code
**反馈渠道**：GitHub Issues
**最后更新**：2025-10-22
