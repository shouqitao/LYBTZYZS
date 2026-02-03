# Design: cleanup-medicalcase-dead-code

## Overview

本提案执行纯删除操作，不涉及架构变更或新代码引入。

## Dead Code Detection Methodology

### 1. LSP引用分析

使用 Serena MCP 的 `find_referencing_symbols` 工具进行精确引用分析：

```
mcp__serena__find_referencing_symbols(name_path="ClassName", relative_path="file.cs")
```

**判定标准**:
- 返回结果仅包含自身定义 → 死代码
- 返回结果包含其他已确认死代码 → 死代码
- 返回结果包含活跃代码 → 非死代码

### 2. DI注册检查

检查 `MedicalCaseModule.RegisterTypes()` 方法，确认目标类型未注册到容器。

### 3. 全局搜索验证

使用 Grep 搜索整个代码库确认无遗漏引用。

## Analysis Results Detail

### MedicalCaseItemMapper (Mappers/MedicalCaseItemMapper.cs)

```
find_referencing_symbols("MedicalCaseItemMapper", "Mappers/MedicalCaseItemMapper.cs")
→ 结果: 0外部引用
```

**结论**: 死代码，可安全删除

---

### HerbListRequestEventArgs (ViewModels/Events/HerbListRequestEventArgs.cs)

```
find_referencing_symbols("HerbListRequestEventArgs", "ViewModels/Events/HerbListRequestEventArgs.cs")
→ 结果: 仅自引用
```

**结论**: 死代码，可安全删除

---

### CommandHandlers/IMedicalCaseCommandHandler.cs

```
find_referencing_symbols("IMedicalCaseCommandHandler", "CommandHandlers/IMedicalCaseCommandHandler.cs")
→ 结果: 仅被 CommandHandlers/MedicalCaseCommandHandler.cs 实现
```

**检查实现类**:
```
find_referencing_symbols("MedicalCaseCommandHandler", "CommandHandlers/MedicalCaseCommandHandler.cs")
→ 结果: 未DI注册，无外部调用
```

**结论**: 整个 CommandHandlers/ 文件夹是死代码

---

### Services/MedicalCaseCommandHandler.cs

```
find_referencing_symbols("MedicalCaseCommandHandler", "Services/MedicalCaseCommandHandler.cs")
→ 结果: 仅自引用(ILogger<MedicalCaseCommandHandler>)
```

**DI注册检查**: `MedicalCaseModule.cs` 未注册此类型

**结论**: 死代码，可安全删除

---

### Services/MedicalCaseValidator.cs

```
find_referencing_symbols("MedicalCaseValidator", "Services/MedicalCaseValidator.cs")
→ 结果: 仅被 Services/MedicalCaseCommandHandler 使用
```

由于 Services/MedicalCaseCommandHandler 是死代码，MedicalCaseValidator 也是死代码。

**结论**: 死代码，可安全删除

---

### Interfaces/IMedicalCaseCommandHandler.cs

```
find_referencing_symbols("IMedicalCaseCommandHandler", "Interfaces/IMedicalCaseCommandHandler.cs")
→ 结果: 0外部引用
```

**注意**: 此接口与 CommandHandlers/IMedicalCaseCommandHandler.cs 同名但定义不同：
- `Interfaces/` 版本继承 `ICommandHandler`
- `CommandHandlers/` 版本继承 `ICommandHandlerBase<...>`

两者均为死代码。

**结论**: 死代码，可安全删除

## File Deletion List

```
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
├── CommandHandlers/                          [DELETE FOLDER]
│   ├── IMedicalCaseCommandHandler.cs
│   └── MedicalCaseCommandHandler.cs
├── Interfaces/
│   └── IMedicalCaseCommandHandler.cs         [DELETE]
├── Mappers/
│   └── MedicalCaseItemMapper.cs              [DELETE]
├── Services/
│   ├── MedicalCaseCommandHandler.cs          [DELETE]
│   └── MedicalCaseValidator.cs               [DELETE]
└── ViewModels/Events/
    └── HerbListRequestEventArgs.cs           [DELETE]
```

**总计**: 7个文件

## Non-Dead Code (Verified)

以下类型经LSP验证有活跃引用，**不应删除**:

| 类型 | 引用位置 |
|------|----------|
| PanelStatusToColorConverter | MedicalCaseEditControl.xaml |
| PanelStatusToTextConverter | MedicalCaseEditControl.xaml |
| PrescriptionSavedPayload | MedicalCaseWorkspaceViewModel |
| MedicalCaseCloneMapper | MedicalCaseService |
| MedicalCaseDetailModelMapper | MedicalCaseMasterDetailViewModel |
| MedicalCaseDetailModel | 广泛使用 |

## Execution Safety

1. **编译验证**: 删除后立即执行 `dotnet build` 确认无编译错误
2. **回滚能力**: 所有删除操作在Git中可追溯，必要时可恢复
3. **无运行时风险**: 死代码从未实例化，删除不影响运行时行为
