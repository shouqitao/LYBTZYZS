# Tasks: consolidate-shared-utilities

## Phase 1: 创建Desktop.Utilities项目

- [ ] **Task 1.1**: 创建项目结构
  - 创建 `src/Client/Desktop/Core/LYBT.Desktop.Utilities/LYBT.Desktop.Utilities.csproj`
  - 添加到 `LYBT.All.sln`
  - 创建目录结构: Configuration/, Constants/, Excel/, Http/, Localization/, Logging/, Security/
  - 验证：项目创建成功，编译通过

## Phase 2: 迁移工具类

- [ ] **Task 2.1**: 迁移ExcelHelper
  - 移动 `Infrastructure/Helpers/ExcelHelper.cs` → `Utilities/Excel/ExcelHelper.cs`
  - 更新命名空间为 `LYBT.Desktop.Utilities.Excel`
  - 更新所有引用
  - 验证：编译通过

- [ ] **Task 2.2**: 迁移ConfigurationExtensions
  - 移动 `Infrastructure/Configuration/ConfigurationExtensions.cs` → `Utilities/Configuration/ConfigurationExtensions.cs`
  - 更新命名空间
  - 更新引用

- [ ] **Task 2.3**: 迁移SystemConstants
  - 移动 `Infrastructure/Constants/SystemConstants.cs` → `Utilities/Constants/SystemConstants.cs`
  - 更新命名空间
  - 更新引用

- [ ] **Task 2.4**: 迁移ClientErrorMessageMapper
  - 移动 `Infrastructure/Localization/ClientErrorMessageMapper.cs` → `Utilities/Localization/ClientErrorMessageMapper.cs`
  - 更新命名空间
  - 更新引用

- [ ] **Task 2.5**: 迁移DesktopSerilogConfiguration
  - 移动 `Infrastructure/Logging/DesktopSerilogConfiguration.cs` → `Utilities/Logging/DesktopSerilogConfiguration.cs`
  - 更新命名空间
  - 更新引用

- [ ] **Task 2.6**: 迁移SensitiveInfoFilter
  - 移动 `Infrastructure/Security/SensitiveInfoFilter.cs` → `Utilities/Security/SensitiveInfoFilter.cs`
  - 更新命名空间
  - 更新引用

- [ ] **Task 2.7**: 迁移RetryPolicyExtensions
  - 移动 `Foundation/Http/RetryPolicyExtensions.cs` → `Utilities/Http/RetryPolicyExtensions.cs`
  - 更新命名空间
  - 更新引用
  - 验证：全部迁移完成，编译通过

## Phase 3: 清理与合并

- [ ] **Task 3.1**: 删除SimpleMapper
  - 删除 `src/Client/Desktop/Core/LYBT.Desktop.Models/Mappers/SimpleMapper.cs`
  - 删除空的Mappers目录
  - 验证：编译通过

- [ ] **Task 3.2**: 合并ValidationConstants
  - 将 `Validators/Common/ValidationConstants.cs` 中的独有常量合并到 `Models/Constants/ValidationConstants.cs`
  - 更新 Validators 项目引用 Models 命名空间
  - 删除 `Validators/Common/ValidationConstants.cs`
  - 验证：编译通过，测试通过

## Phase 4: 清理空目录

- [ ] **Task 4.1**: 清理Infrastructure空目录
  - 检查并删除迁移后的空目录
  - 保留有内容的目录

## Phase 5: 统一验证格式

- [ ] **Task 5.1**: 审查DataAnnotation与FluentValidator覆盖情况
  - 列出所有使用DataAnnotation验证的DTO
  - 对比对应的FluentValidator是否覆盖相同规则
  - 记录缺失的验证规则

- [ ] **Task 5.2**: 补充FluentValidator规则
  - 将DataAnnotation独有的规则补充到FluentValidator
  - 确保验证行为等效
  - 验证：FluentValidator覆盖所有原DataAnnotation规则

- [ ] **Task 5.3**: 移除DataAnnotation验证特性
  - 移除DTO上的`[Required]`、`[StringLength]`等验证特性
  - 保留非验证用途的特性（如`[JsonPropertyName]`）
  - 更新ValidationConstants消息格式为FluentValidation风格
  - 验证：编译通过

- [ ] **Task 5.4**: 验证API端点行为
  - 测试关键API端点的验证响应
  - 确认错误消息格式正确
  - 验证：验证行为与移除前一致

## Validation

- [ ] **Final**: 完整验证
  - `dotnet build LYBT.All.sln -c Release` 编译通过
  - `dotnet test` 全部测试通过
  - 无新增编译警告
  - 验证所有迁移的类可正常引用

## Summary

| 阶段 | 任务数 | 预计工作量 |
|------|--------|-----------|
| Phase 1 | 1 | 创建项目 |
| Phase 2 | 7 | 迁移工具类 |
| Phase 3 | 2 | 清理与合并 |
| Phase 4 | 1 | 清理空目录 |
| Phase 5 | 4 | 统一验证格式 |
| Validation | 1 | 最终验证 |
| **总计** | **16** | - |
