# Tasks: cleanup-medicalcase-dead-code

## Phase 1: 删除死代码文件 (10分钟)

### Task 1.1: 删除Mapper死代码
- **文件**: `Mappers/MedicalCaseItemMapper.cs`
- **验证**: LSP确认0引用
- **操作**: 删除文件

### Task 1.2: 删除Event死代码
- **文件**: `ViewModels/Events/HerbListRequestEventArgs.cs`
- **验证**: LSP确认仅自引用
- **操作**: 删除文件

### Task 1.3: 删除CommandHandlers文件夹死代码
- **文件**:
  - `CommandHandlers/IMedicalCaseCommandHandler.cs`
  - `CommandHandlers/MedicalCaseCommandHandler.cs`
- **验证**: 未DI注册，LSP确认无外部使用
- **操作**: 删除整个 `CommandHandlers/` 文件夹

### Task 1.4: 删除Services层死代码
- **文件**:
  - `Services/MedicalCaseCommandHandler.cs`
  - `Services/MedicalCaseValidator.cs`
- **验证**: 未DI注册，仅内部互相引用
- **操作**: 删除文件

### Task 1.5: 删除Interfaces层死代码
- **文件**: `Interfaces/IMedicalCaseCommandHandler.cs`
- **验证**: LSP确认0外部引用
- **操作**: 删除文件

## Phase 2: 编译验证 (5分钟)

### Task 2.1: 全量编译验证
- **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **预期**: 编译成功，0错误

### Task 2.2: 检查残留引用
- **命令**: `grep -r "MedicalCaseItemMapper\|HerbListRequestEventArgs\|MedicalCaseValidator" --include="*.cs"`
- **预期**: 无匹配结果（仅在proposal/tasks.md中出现）

## Completion Checklist

- [x] 7个死代码文件已删除
- [x] 编译通过 (0错误0警告)
- [x] 无残留引用

## Execution Log

- **执行时间**: 2026-01-13 16:15
- **删除文件**:
  - `Mappers/MedicalCaseItemMapper.cs`
  - `ViewModels/Events/HerbListRequestEventArgs.cs`
  - `CommandHandlers/` (整个文件夹，含2个文件)
  - `Services/MedicalCaseCommandHandler.cs`
  - `Services/MedicalCaseValidator.cs`
  - `Interfaces/IMedicalCaseCommandHandler.cs`
- **编译结果**: 成功 (48.75秒)
- **残留引用检查**: 无
