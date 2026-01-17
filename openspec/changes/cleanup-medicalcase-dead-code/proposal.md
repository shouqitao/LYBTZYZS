# Proposal: cleanup-medicalcase-dead-code

## Metadata
- **Created**: 2026-01-13
- **Status**: applied
- **Author**: Claude Code
- **Type**: cleanup

## Summary

清理 LYBT.Desktop.MedicalCase 模块中的死代码，移除未使用的类、接口和方法，减少代码维护负担。

## Problem Statement

MedicalCase模块经过多次重构（包括OpenSpec: refactor-viewmodel-layer、simplify-workspace-architecture等），
遗留了大量未使用的代码。这些死代码：

1. 增加编译时间和程序集体积
2. 干扰代码阅读和理解
3. 在DI容器中未注册，从未实例化
4. 与活跃代码存在命名冲突（如两个不同的 `IMedicalCaseCommandHandler` 接口）

## Solution

删除通过LSP引用分析确认的死代码文件。

## Dead Code Analysis Results

使用 `mcp__serena__find_referencing_symbols` (LSP) 工具验证，以下文件/类型**零外部引用**：

### 确认删除清单

| 文件路径 | 类型 | 死代码原因 |
|----------|------|------------|
| `Mappers/MedicalCaseItemMapper.cs` | Mapper | 0引用，从未被调用 |
| `ViewModels/Events/HerbListRequestEventArgs.cs` | EventArgs | 仅自引用，无订阅者 |
| `CommandHandlers/IMedicalCaseCommandHandler.cs` | Interface | 仅被同文件夹下死代码实现 |
| `CommandHandlers/MedicalCaseCommandHandler.cs` | Class | 未DI注册，从未实例化 |
| `Services/MedicalCaseCommandHandler.cs` | Class | 未DI注册，从未实例化，与上面接口重名 |
| `Services/MedicalCaseValidator.cs` | Class | 仅被死代码 Services/MedicalCaseCommandHandler 使用 |
| `Interfaces/IMedicalCaseCommandHandler.cs` | Interface | 0外部引用 |

### 验证方法

1. **LSP引用分析**: `find_referencing_symbols` 返回结果仅包含自身或同为死代码的调用者
2. **DI注册检查**: `MedicalCaseModule.RegisterTypes()` 未注册上述类型
3. **Grep全局搜索**: 确认无其他代码路径引用

## Impact Analysis

- **删除文件**: 7个
- **估计删除代码行数**: ~1000行
- **编译影响**: 无（已验证这些类从未被使用）
- **运行时影响**: 无（从未注册到DI容器）

## Risks

- **低风险**: 所有删除目标均经过LSP引用验证
- **缓解措施**: 删除前执行完整编译验证

## Acceptance Criteria

1. 所有7个文件已删除
2. `dotnet build LYBT.All.sln -c Release` 编译通过
3. 无任何编译警告（与删除相关）

## References

- OpenSpec: refactor-viewmodel-layer
- OpenSpec: simplify-workspace-architecture
- OpenSpec: standardize-module-structure
