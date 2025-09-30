# AI辅助自动化工作流程

## 1. 概述

本文档定义了基于Issue #808 Desktop架构优化的AI辅助自动化工作流程，确保全过程GitHub自动化AI辅助，以业务开发为主导的高效执行。

## 2. 工作流程设计

### 2.1 整体流程图
```
Issue创建 → 模块化清单 → 实施阶段 → AI审查 → PR合并 → 文档同步 → Issue关闭
    ↓           ↓           ↓         ↓        ↓        ↓         ↓
  Claude    Claude      Claude   Claude+   GitHub   Claude    自动化
  分析      规划        实现     Serena    Workflow  文档      关单
```

### 2.2 参与角色

**Claude Code (主执行者)**：
- 深度分析和方案设计
- 模块化清单生成
- 代码实现和重构
- 初步代码审查
- 文档撰写和同步

**Serena (二级审查)**：
- 深度代码分析
- 架构合规性检查
- 代码质量评估
- 性能影响分析

**GitHub Automation (基础设施)**：
- Issue状态管理
- PR关联验证
- 自动化标签
- 状态同步

## 3. 阶段详细流程

### 3.1 Phase 1: Issue创建与规划

**输入**：架构优化需求
**负责人**：Claude Code
**时间**：0.5天

**执行步骤**：
1. **深度分析** (使用ultrathink)
   ```bash
   Claude: ultrathink分析四份核心文档
   输出: 架构问题识别报告
   ```

2. **模块化清单生成** (使用sequential-thinking)
   ```markdown
   [CLI-1] 基础设施层重组
     - 产出路径: src/Client/Desktop/Core/
     - 验收点: 编译通过，基础功能正常

   [CLI-2] 业务模块标准化
     - 产出路径: src/Client/Desktop/Modules/
     - 验收点: 代码重复率<20%，层次深度≤4

   [CLI-3] 工作台层实现
     - 产出路径: src/Client/Desktop/Workstations/
     - 验收点: 功能完整，性能不下降
   ```

3. **Issue文档创建**
   ```bash
   路径: docs/issues/ISSUE_808_DESKTOP_ARCHITECTURE_OPTIMIZATION.md
   内容: 完整的问题背景、解决方案、验收标准
   ```

**输出**：
- Issue文档 (Markdown格式)
- 模块化执行清单
- 量化验收标准

### 3.2 Phase 2: 实施阶段

**输入**：模块化清单
**负责人**：Claude Code
**时间**：7周 (分3个子阶段)

#### 子阶段2.1: 基础设施重组 (2周)

**Claude执行流程**：
```bash
1. git checkout -b feature/cli-1-infrastructure-refactor
2. 创建Core层3个项目结构
3. 迁移代码文件 (保持Git历史)
4. 更新项目引用和命名空间
5. dotnet build LYBT.All.sln -c Release
6. 运行基础功能测试
7. 更新清单状态: [CLI-1] completed
```

**自动化验证**：
```yaml
验收检查:
  - 编译状态: ✅ 通过
  - 基础功能: ✅ 正常
  - 性能基准: ✅ 在可接受范围
  - 文档更新: ✅ 已同步
```

#### 子阶段2.2: 业务模块标准化 (3周)

**Claude执行流程**：
```bash
1. git checkout -b feature/cli-2-modules-standardization
2. 重组8个业务模块，对齐Server端结构
3. 统一使用Shared.Models.Contracts
4. 标准化ViewModel模式
5. 减少代码重复 (目标<20%)
6. dotnet build LYBT.All.sln -c Release
7. 运行完整回归测试
8. 更新清单状态: [CLI-2] completed
```

**代码质量检查**：
```csharp
// 正确的统一模式示例
using LYBT.Shared.Models.Contracts.Patients;

public class PatientViewModel : ReactiveViewModel<PatientDto>
{
    public ReactiveCommand<Unit, PatientDto> SaveCommand { get; }

    public PatientViewModel(IPatientApiClient apiClient)
    {
        SaveCommand = ReactiveCommand.CreateFromTask(
            async () => await apiClient.SavePatientAsync(Model));
    }
}
```

#### 子阶段2.3: 工作台层实现 (2周)

**Claude执行流程**：
```bash
1. git checkout -b feature/cli-3-workstations-implementation
2. 创建独立的Workstations层
3. 实现模块聚合逻辑
4. 优化启动和模块加载
5. dotnet build LYBT.All.sln -c Release
6. 性能测试和功能验证
7. 更新清单状态: [CLI-3] completed
```

### 3.3 Phase 3: AI审查阶段

**输入**：完成的代码实现
**负责人**：Claude Code + Serena
**时间**：每个子阶段1天

#### Claude初审流程
```bash
Claude执行:
1. 检查架构禁令遵守情况 (禁止CQRS/MediatR等)
2. 验证命名规范和异步约定
3. 检查文档同步状态
4. 验证测试覆盖率
5. 生成初审报告
```

**初审清单**：
```markdown
Architecture Compliance:
- [ ] 未使用禁止的技术栈 (CQRS/MediatR/Redis等)
- [ ] 遵循适度设计原则
- [ ] 依赖关系清晰，无循环依赖

Code Quality:
- [ ] 命名规范符合PascalCase/_camelCase约定
- [ ] 异步方法使用Async后缀
- [ ] 代码重复率<20%

Documentation:
- [ ] API文档已更新
- [ ] 架构文档已同步
- [ ] 迁移日志完整
```

#### Serena二审流程
```bash
Serena执行:
1. 深度代码静态分析
2. 架构模式合规性检查
3. 性能影响评估
4. 安全性审查
5. 生成二审报告
```

**二审检查点**：
```markdown
Technical Depth:
- [ ] 代码复杂度在可接受范围
- [ ] 内存使用模式优化
- [ ] 异常处理机制完善

Business Logic:
- [ ] 业务逻辑正确实现
- [ ] 数据一致性保证
- [ ] 用户体验无降级
```

### 3.4 Phase 4: PR管理与合并

**输入**：通过双重审查的代码
**负责人**：Claude Code + GitHub Automation
**时间**：每个子阶段0.5天

#### PR自动生成流程
```bash
Claude执行:
1. 自动生成PR草稿内容
2. 包含编译结果和验收状态
3. 添加关单关键字 "Fixes #808"
4. 生成变更摘要和影响分析
```

**PR模板内容**：
```markdown
## 变更摘要
实现Issue #808的[CLI-X]模块，完成XXX功能重构。

## 编译验证
```bash
dotnet build LYBT.All.sln -c Release
结果: ✅ 成功
时间: XX秒 (基准±XX%)
```

## 功能测试
- [x] 基础功能正常
- [x] 回归测试通过
- [x] 性能在可接受范围

## 审查状态
- [x] Claude Code 初审通过
- [x] Serena 二审通过

## 关联Issue
Fixes #808
```

#### GitHub自动化处理
```yaml
GitHub Workflow:
  - PR创建时: 自动运行CI/CD
  - 审查完成时: 自动添加标签
  - 合并后: 自动更新Issue状态
  - Issue关闭: 自动同步项目看板
```

### 3.5 Phase 5: 文档同步与归档

**输入**：合并完成的PR
**负责人**：Claude Code
**时间**：每个子阶段0.5天

#### 文档更新流程
```bash
Claude执行:
1. 更新架构文档
2. 创建迁移指南
3. 更新API文档
4. 归档分析报告
5. 更新项目README
```

**文档更新清单**：
```markdown
Architecture Documentation:
- [ ] docs/architecture/desktop-architecture.md
- [ ] docs/development/desktop-migration-guide.md

API Documentation:
- [ ] docs/api/desktop-api-reference.md
- [ ] OpenAPI规范更新

Reports & Analysis:
- [ ] docs/reports/desktop-refactoring-summary.md
- [ ] docs/reports/INDEX.md 登记
- [ ] docs/optimization/ 方案执行结果
```

## 4. 质量门禁机制

### 4.1 每个子阶段门禁

**编译门禁**：
```bash
dotnet build LYBT.All.sln -c Release
要求: 必须编译通过，无警告
```

**测试门禁**：
```bash
dotnet test LYBT.Server.sln -c Release
要求: 所有测试通过，覆盖率不下降
```

**架构门禁**：
```bash
代码重复率检查: <20%
层次深度检查: ≤4层
依赖关系检查: 无循环依赖
```

**性能门禁**：
```bash
编译时间: 不超过基准+20%
启动时间: 不超过基准+10%
内存使用: 在可接受范围内
```

### 4.2 阶段完成标准

每个子阶段完成必须满足：
1. ✅ 所有门禁检查通过
2. ✅ 双重AI审查通过
3. ✅ 文档完全同步
4. ✅ Issue清单项标记完成

## 5. 应急预案

### 5.1 回滚机制
```bash
如果发现严重问题:
1. git revert <commit-hash>  # 代码回滚
2. 恢复到上一个稳定milestone
3. 分析问题原因
4. 调整实施策略
5. 重新开始问题阶段
```

### 5.2 问题升级
```bash
技术问题升级路径:
Claude分析 → Serena深度分析 → 人工干预决策

业务问题升级路径:
业务影响评估 → 干系人沟通 → 方案调整 → 重新执行
```

## 6. 成功指标

### 6.1 过程指标
- **自动化程度**: >90%的流程由AI完成
- **响应速度**: Issue到首次实施<1天
- **审查效率**: 双重审查<4小时完成
- **文档同步**: 100%的代码变更有对应文档更新

### 6.2 结果指标
- **架构健康度**: 从90/100提升至95/100
- **代码重复率**: 从40%降低至<20%
- **层次复杂度**: 从5-6层简化至≤4层
- **维护效率**: 新功能开发时间减少30%

## 7. 经验总结与优化

### 7.1 流程改进
每个Phase完成后：
1. 收集执行过程中的问题点
2. 分析AI辅助的效率瓶颈
3. 优化下一阶段的自动化程度
4. 更新流程文档和最佳实践

### 7.2 知识积累
```markdown
Best Practices Repository:
- 成功的重构模式记录
- 常见问题解决方案
- AI提示词优化
- 自动化脚本改进
```

---

**创建时间**：2025-09-29
**适用项目**：LYBT Desktop架构优化
**维护者**：Claude Code
**版本**：1.0