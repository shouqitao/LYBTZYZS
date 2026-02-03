# complete-management-create-logic Tasks

## Overview

- **变更类型**: Enhancement
- **风险等级**: Low
- **预估工作量**: 2-3小时

## Phase 1: UserInputDtoValidator 验证完善

### 1.1 补全UserInputDtoValidator验证规则 [DONE]
- **文件**: `src/Shared/LYBT.Shared.Validators/Users/UserInputDtoValidator.cs`
- **变更**:
  - 添加 `using LYBT.Shared.Primitives.Validation;` (ValidationConstants)
  - 添加 `using LYBT.Shared.Models.Enums;` (UserRole枚举验证)
  - 添加 RealName必填验证 (创建时When条件)
  - 添加 Role枚举有效性验证 (IsInEnum + When条件)
  - 添加 Password长度验证 (6-100字符，When条件)
  - 添加 ConfirmPassword匹配验证 (Equal规则)
  - 添加 Email格式验证 (EmailAddress规则，可选)
  - 添加 PhoneNumber正则验证 (PhoneRegex，可选)
  - 添加 Remark长度验证 (RemarkMaxLength)
- **验证**: 编译通过

### 1.2 编译验证 [DONE]
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

## Phase 2: Herbs导入导出功能完成

### 2.1 完成HerbMasterDetailViewModel导入功能 [DONE]
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`
- **位置**: L320-359 ImportHerbsAsync方法
- **变更**:
  - 调用 `CommonDialogService.ShowOpenFileDialogAsync`
  - 使用 `File.OpenRead` 和 `Path.GetFileName`
  - 调用 `_herbRepository.BatchImportAsync(fileStream, fileName)`
  - 添加成功/失败消息提示
  - 成功后调用 `RefreshAsync()`
- **验证**: 编译通过

### 2.2 完成HerbMasterDetailViewModel导出功能 [DONE]
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`
- **位置**: L364-405 ExportHerbsAsync方法
- **变更**:
  - 调用 `CommonDialogService.ShowSaveFileDialogAsync`
  - 生成默认文件名: `药材导出_{DateTime.Now:yyyyMMdd}.xlsx`
  - 调用 `File.WriteAllBytesAsync(filePath, bytes)`
  - 添加成功/失败消息提示
- **验证**: 编译通过

### 2.3 编译验证 [DONE]
- 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误

## Phase 3: API版本格式统一

### 3.1 修改UsersController API版本 [DONE]
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`
- **位置**: L17
- **变更**: `[ApiVersion("1.0")]` → `[ApiVersion("1")]`
- **验证**: 编译通过

### 3.2 最终编译验证 [DONE]
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

## Dependencies

```
Phase 1 ─────────────────────┐
  UserInputDtoValidator      │
  (独立，可优先执行)          │
                             ├──> Phase 3 最终验证
Phase 2 ─────────────────────┤
  Herbs导入导出               │
  (独立，可并行执行)          │
                             │
Phase 3 ─────────────────────┘
  API版本统一
  (独立，可随时执行)
```

**并行执行策略**: Phase 1 和 Phase 2 可同时进行

## Validation Checklist

- [x] Server解决方案编译通过
- [x] Desktop解决方案编译通过
- [x] UserInputDtoValidator验证规则完整
- [ ] Herbs导入功能正常（文件选择→导入→刷新）[需手动测试]
- [ ] Herbs导出功能正常（保存对话框→文件生成）[需手动测试]
- [x] UsersController API版本格式统一

## Notes

- UserInputDtoValidator使用When条件区分创建/更新场景
- **修正**: 使用 `CommonDialogService` 而非 `MasterDetailServices.Dialog` 获取文件对话框
- **修正**: 导入使用 `BatchImportAsync(Stream, string)` 而非 `ImportHerbsAsync(byte[])`
- System.IO命名空间已在HerbMasterDetailViewModel中引用
- Formula导出功能不在本次范围内（无现有API端点）

---

**生成时间**: 2026-01-23
**执行完成**: 2026-01-24
**状态**: 代码变更完成，待手动功能验证
