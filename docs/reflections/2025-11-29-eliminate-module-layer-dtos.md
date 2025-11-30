# 消除 MedicalCase 模块层冗余 DTO 反思报告

**日期**: 2025-11-29
**类型**: 架构重构
**影响模块**: LYBT.Module.MedicalCase

---

## 1. 问题发现

在审查 `MedicalCaseMappingProfile.cs` 时发现两个可疑的 Module 层 DTO:
- `MedicalCaseDetailResponse.cs`
- `MedicalCasePrescriptionDto.cs`

### 问题分析

| 类名 | 位置 | 问题 |
|------|------|------|
| `MedicalCaseDetailResponse` | Module.Dtos | 与 `MedicalCaseDetailDto` (Shared层) 功能完全重复 |
| `MedicalCasePrescriptionDto` | Module.Dtos | 与 `PrescriptionDto` (Shared层) 95%字段相同 |

### 违反原则

1. **DRY (Don't Repeat Yourself)**: 相同功能的 DTO 在两个位置定义
2. **DTO-ARCH-001**: DTO 应统一定义在 Shared 层
3. **架构一致性**: 其他模块(Patients、Prescriptions、Consultation)都没有 Module 层 Dtos 目录

---

## 2. 重构内容

### 删除文件
```
src/Server/Modules/LYBT.Module.MedicalCase/Dtos/
├── MedicalCaseDetailResponse.cs  [删除]
└── MedicalCasePrescriptionDto.cs [删除]
```

### 更新文件
| 文件 | 变更 |
|------|------|
| `MedicalCaseMappingProfile.cs` | 删除对已删除类的映射配置 |
| `MedicalCaseController.cs` | 使用 `PrescriptionDto` 替代 `MedicalCasePrescriptionDto` |
| `MedicalCaseService.cs` | 同上 |
| `IMedicalCaseService.cs` | 同上 |
| `MedicalCaseServiceTests.cs` | 同上 |
| `MedicalCaseControllerIntegrationTests.cs` | 移除无用 using |

### 复用现有映射

`Prescription -> PrescriptionDto` 映射已存在于 `PrescriptionMappingProfile.cs`，无需重复定义。

---

## 3. 模块对比分析

| 模块 | Module层Dtos目录 | 状态 |
|------|------------------|------|
| LYBT.Module.Patients | 无 | 符合规范 |
| LYBT.Module.Prescriptions | 无 | 符合规范 |
| LYBT.Module.Consultation | 无 | 符合规范 |
| LYBT.Module.Users | 无 | 符合规范 |
| LYBT.Module.Auth | 无 | 符合规范 |
| LYBT.Module.Herbs | 无 | 符合规范 |
| LYBT.Module.Formula | 无 | 符合规范 |
| **LYBT.Module.MedicalCase** | **有(已删除)** | **已修复** |

### 结论
MedicalCase 是唯一存在 Module 层 DTO 冗余的模块，重构后所有模块架构一致。

---

## 4. 经验教训

### 4.1 技术债务成因分析

这些冗余 DTO 可能源于:
1. **渐进式开发**: 早期开发时 Shared 层 DTO 不完善，临时在 Module 层创建
2. **缺乏审查**: 后续添加 Shared 层完整 DTO 后，未清理旧代码
3. **复制粘贴**: 开发新功能时复制现有代码而非复用

### 4.2 预防措施

1. **代码审查清单**: 新增 DTO 时检查 Shared 层是否已有类似定义
2. **架构测试**: 考虑添加 ArchTests 规则禁止 Module 层定义 DTO
3. **文档明确**: `dto-architecture` spec 已记录 DTO 统一定义位置规范

### 4.3 AutoMapper 最佳实践

- 优先使用 Shared 层标准 DTO，而非创建"简化版"
- 利用 `.ForMember(opt => opt.Ignore())` 忽略不需要的字段
- 避免为每个使用场景创建专用 DTO

---

## 5. 验证结果

- [x] 编译通过 (0 错误, 1 无关警告)
- [x] 所有模块架构一致 (无 Module 层 Dtos 目录)
- [x] 代码引用更新完成

---

## 6. 相关文档

- `openspec/specs/dto-architecture/spec.md` - DTO 架构规范
- `openspec/changes/archive/2025-11-29-consolidate-medicalcase-dtos/` - DTO 整合归档
