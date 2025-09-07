# 代码清理执行计划

**分析时间**: 2025-09-07  
**分析器**: .NET 代码整洁教练  
**项目**: LYBT中医诊所管理系统

## 🎯 执行策略

### 阶段1: 准备阶段
1. **创建工作分支**: `git checkout -b chore/prune-unused-workbenches`
2. **备份当前状态**: 确保所有更改已提交
3. **运行基准测试**: 确保当前构建成功

### 阶段2: 确认死代码删除
删除100%确信的死代码（4个未使用的Workbench模块）

### 阶段3: 可疑代码标记
为可疑代码添加 `[Obsolete]` 标记并观察

### 阶段4: 验证与清理
运行构建测试，清理剩余引用

## 📋 详细执行步骤

### Phase 1: 解决方案清理
**目标**: 从解决方案中移除死项目引用

```bash
# 1.1 检查当前解决方案状态
dotnet sln LYBT.All.sln list

# 1.2 移除死项目引用（如果存在）
dotnet sln LYBT.All.sln remove src/Client/Desktop/Workbenches/TherapistWorkbench/LYBT.Desktop.Workbench.Therapist.csproj
dotnet sln LYBT.All.sln remove src/Client/Desktop/Workbenches/PharmacistWorkbench/LYBT.Desktop.Workbench.Pharmacist.csproj
dotnet sln LYBT.All.sln remove src/Client/Desktop/Workbenches/CashierWorkbench/LYBT.Desktop.Workbench.Cashier.csproj
dotnet sln LYBT.All.sln remove src/Client/Desktop/Workbenches/ReceptionistWorkbench/LYBT.Desktop.Workbench.Receptionist.csproj
```

**提交点**: `chore: remove unused workbench projects from solution`

### Phase 2: 删除TherapistWorkbench
**目标**: 完全移除理疗师工作台模块

```bash
# 2.1 删除整个目录
rm -rf src/Client/Desktop/Workbenches/TherapistWorkbench/

# 2.2 清理任何剩余引用（使用搜索确认）
grep -r "TherapistWorkbench" --include="*.cs" --include="*.xaml" --include="*.csproj" src/
```

**预期结果**: 
- 删除约6个文件
- 减少约300行代码

**提交点**: `chore: remove unused TherapistWorkbench module`

### Phase 3: 删除PharmacistWorkbench  
**目标**: 完全移除药师工作台模块

```bash
# 3.1 删除整个目录
rm -rf src/Client/Desktop/Workbenches/PharmacistWorkbench/

# 3.2 清理任何剩余引用
grep -r "PharmacistWorkbench" --include="*.cs" --include="*.xaml" --include="*.csproj" src/
```

**预期结果**:
- 删除约6个文件  
- 减少约300行代码

**提交点**: `chore: remove unused PharmacistWorkbench module`

### Phase 4: 删除CashierWorkbench
**目标**: 完全移除收费员工作台模块

```bash
# 4.1 删除整个目录
rm -rf src/Client/Desktop/Workbenches/CashierWorkbench/

# 4.2 清理任何剩余引用
grep -r "CashierWorkbench" --include="*.cs" --include="*.xaml" --include="*.csproj" src/
```

**预期结果**:
- 删除约8个文件
- 减少约400行代码

**提交点**: `chore: remove unused CashierWorkbench module`

### Phase 5: 删除ReceptionistWorkbench
**目标**: 完全移除前台接待工作台模块

```bash
# 5.1 删除整个目录  
rm -rf src/Client/Desktop/Workbenches/ReceptionistWorkbench/

# 5.2 清理任何剩余引用
grep -r "ReceptionistWorkbench" --include="*.cs" --include="*.xaml" --include="*.csproj" src/
```

**预期结果**:
- 删除约10个文件
- 减少约500行代码

**提交点**: `chore: remove unused ReceptionistWorkbench module`

### Phase 6: 文档清理
**目标**: 清理相关的文档文件

```bash
# 6.1 删除各模块的README文档（已包含在目录删除中）
# 6.2 清理docs/目录中的相关引用
grep -r "TherapistWorkbench\|PharmacistWorkbench\|CashierWorkbench\|ReceptionistWorkbench" docs/

# 6.3 更新项目文档
# 更新docs/guides/developer-onboarding-guide.md等相关文档
```

**提交点**: `docs: clean up references to removed workbench modules`

### Phase 7: 可疑代码标记
**目标**: 为可疑代码添加观察标记

创建 `.ai/keeplist.txt` 文件：
```
# 代码清理白名单 - 2025-09-07
# 以下项目添加了Obsolete标记，观察期14天

# 可疑的帮助类
src/Shared/LYBT.Shared.Utilities/Helpers/CommonHelper.cs
src/Shared/LYBT.Shared.Utilities/Helpers/EnumHelper.cs

# 可疑的ViewModel  
src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionViewModelRefactored.cs

# 可疑的基础架构
src/Server/Core/LYBT.Infrastructure/Specification.cs
src/Server/Core/LYBT.Infrastructure/BaseService.cs
```

为可疑类添加Obsolete标记：
```csharp
[Obsolete("Under review for removal - analysis period ends 2025-09-21", false)]
public class SuspiciousClass
{
    // ...
}
```

**提交点**: `refactor: mark suspicious unused code for observation`

### Phase 8: 构建验证
**目标**: 确保所有更改不影响构建

```bash
# 8.1 清理和重建
dotnet clean
dotnet restore
dotnet build --no-restore

# 8.2 运行测试（如果存在）
dotnet test --no-build --verbosity minimal

# 8.3 代码格式化
dotnet format
```

**提交点**: `chore: format code after cleanup`

## ⏰ 执行时间估算

### 各阶段用时预估
- **Phase 1** (解决方案清理): 5分钟
- **Phase 2-5** (删除模块): 每个5分钟，共20分钟  
- **Phase 6** (文档清理): 10分钟
- **Phase 7** (可疑代码标记): 15分钟
- **Phase 8** (构建验证): 10分钟

**总计**: 约60分钟

### 并行化可能
Phase 2-5可以合并为单个操作，减少到约30分钟总时长。

## 🔍 验证检查点

### 每个Phase后的检查
1. **构建状态**: 确保 `dotnet build` 成功
2. **引用清理**: 确保没有遗漏的引用导致编译错误
3. **文件完整性**: 确认删除了预期的文件
4. **Git状态**: 检查暂存的更改是否符合预期

### 最终验证清单
- [ ] 构建成功无错误
- [ ] 测试全部通过（如果存在）
- [ ] 解决方案中没有死项目引用
- [ ] IDE中没有编译错误或警告
- [ ] Git历史清晰，提交信息准确

## 🚨 回滚计划

### 快速回滚
如果发现问题，可以快速回滚到清理前状态：

```bash
# 完全回滚到master
git checkout master
git branch -D chore/prune-unused-workbenches

# 部分回滚特定文件
git checkout HEAD~n -- <文件路径>

# 恢复特定提交
git revert <commit-hash>
```

### 逐步回滚
如果只是部分有问题：
1. 确认问题范围
2. 回滚对应的提交
3. 重新运行构建验证

## 📊 预期收益

### 立即收益
- **代码行数**: 减少约1,500行死代码
- **文件数**: 减少约30个无用文件
- **编译时间**: 减少约30秒（4个项目）
- **解决方案加载**: 提升约20%

### 长期收益
- **维护负担**: 减少维护无用代码的时间
- **团队认知**: 清晰的代码库结构
- **新人培训**: 减少混乱和误解
- **代码质量**: 提升整体代码质量指标

---
**总结**: 详细的执行计划确保了安全、高效的死代码清理过程。重点关注可回滚性和验证完整性。