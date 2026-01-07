# Tasks: standardize-mapperly-configuration

## Phase 1: Server端 Mapper 标准化

### Task 1.1: MedicalCaseMapper (66 warnings)
- [x] 添加 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- [x] 审查并简化现有 `[MapperIgnore*]` 属性
- [x] 验证编译无新错误

### Task 1.2: FormulaMapper (43 warnings)
- [x] 添加 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- [x] 审查并简化现有 `[MapperIgnore*]` 属性
- [x] 验证编译无新错误

### Task 1.3: PrescriptionMapper (39 warnings)
- [x] 添加 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- [x] 审查并简化现有 `[MapperIgnore*]` 属性
- [x] 验证编译无新错误

### Task 1.4: UserMapper (Server: 28 warnings)
- [x] 添加 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- [x] 审查并简化现有 `[MapperIgnore*]` 属性
- [x] 验证编译无新错误

### Task 1.5: PatientMapper (28 warnings)
- [x] 添加 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- [x] 审查并简化现有 `[MapperIgnore*]` 属性
- [x] 验证编译无新错误

### Task 1.6: ConsultationMapper (18 warnings)
- [x] 添加 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- [x] 审查并简化现有 `[MapperIgnore*]` 属性
- [x] 验证编译无新错误

### Task 1.7: HerbMapper (15 warnings)
- [x] 添加 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- [x] 审查并简化现有 `[MapperIgnore*]` 属性
- [x] 验证编译无新错误

## Phase 2: Client端 Mapper 标准化

### Task 2.1: UserMapper (Client: 10 warnings)
- [x] 添加 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- [x] 统一客户端配置模式
- [x] 验证编译无新错误

### Task 2.2: 已修复 Mapper 验证
- [x] PatientMapper (Client) - 验证配置一致性
- [x] FormulaMapper (Client) - 验证配置一致性
- [x] MedicalCaseItemMapper (Client) - 验证配置一致性
- [x] PrescriptionMapper (Client) - 验证配置一致性
- [x] ConsultationMapper (Client) - 验证配置一致性
- [x] FormulaDetailModelMapper (Client) - 添加配置
- [x] FormulaHerbItemMapper (Client) - 添加配置
- [x] MedicalCaseDetailModelMapper (Client) - 添加配置
- [x] HerbMapper (Client) - 添加配置

## Phase 3: 验证与文档

### Task 3.1: 全量编译验证
- [x] 执行 `dotnet build LYBT.All.sln`
- [x] 记录剩余警告数量: **0 警告**
- [x] 处理剩余特殊警告: 全部消除

### Task 3.2: 更新文档
- [x] 更新 MedicalCase/CLAUDE.md Mapperly 使用说明 (已包含源生成器兼容性说明)
- [x] Mapper 编写规范已在各 Mapper 文件的 XML 注释中记录
- [ ] 归档 OpenSpec

## 验收标准

1. **警告数量**: 从 247 个降至 **0 个** (超出目标)
2. **编译通过**: 0 错误
3. **配置统一**: 所有 18 个 Mapper 使用 `RequiredMappingStrategy.Target`
4. **文档完善**: Mapper 编写规范已记录

## 完成总结

### 修改文件清单

**Server端 (7 个 Mapper)**:
- `src/Server/Modules/LYBT.Module.MedicalCase/Mapping/MedicalCaseMapper.cs`
- `src/Server/Modules/LYBT.Module.Formula/Mapping/FormulaMapper.cs`
- `src/Server/Modules/LYBT.Module.Prescriptions/Mapping/PrescriptionMapper.cs`
- `src/Server/Modules/LYBT.Module.Users/Mapping/UserMapper.cs`
- `src/Server/Modules/LYBT.Module.Patients/Mapping/PatientMapper.cs`
- `src/Server/Modules/LYBT.Module.Consultation/Mapping/ConsultationMapper.cs`
- `src/Server/Modules/LYBT.Module.Herbs/Mapping/HerbMapper.cs`

**Client端 (11 个 Mapper)**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/Mappers/UserMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Mappers/PatientMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Mappers/HerbMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Mappers/FormulaDetailModelMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Mappers/FormulaMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Mappers/FormulaHerbItemMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/ConsultationMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseDetailModelMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/PrescriptionMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseItemMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Mappers/ConsultationMapper.cs`

### 核心变更

所有 Mapper 类添加了统一配置:
```csharp
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class XxxMapper
```

此配置仅检查目标类型成员是否有对应源成员，消除了源类型成员未映射的噪音警告 (RMG020)。
