# Mapperly 警告修复方案

## 问题分析

### 警告统计

| Mapper文件 | 警告数量 | 位置 |
|-----------|---------|------|
| MedicalCaseMapper.cs | 66 | Server/LYBT.Module.MedicalCase |
| FormulaMapper.cs | 43 | Server/LYBT.Module.Formula |
| PrescriptionMapper.cs | 39 | Server/LYBT.Module.Prescriptions |
| UserMapper.cs | 38 | Server + Client/Desktop.Users |
| PatientMapper.cs | 28 | Server/LYBT.Module.Patients |
| ConsultationMapper.cs | 18 | Server/LYBT.Module.Consultation |
| HerbMapper.cs | 15 | Server/LYBT.Module.Herbs |
| **合计** | **247** | |

### 高频未映射字段

| 字段类型 | 字段名 | 出现次数 | 原因 |
|---------|--------|---------|------|
| **审计字段** | UpdatedBy | 16 | Entity独有，DTO不需要 |
| | RowVersion | 16 | 并发控制，DTO不需要 |
| | IsDeleted | 16 | 软删除标记，DTO不需要 |
| | CreatedBy | 11 | Entity独有 |
| | UpdatedAt | 7 | ListDto通常不需要 |
| **业务标识** | Id | 11 | InputDto→Entity时手动处理 |
| | Remark | 11 | 部分DTO不包含 |
| | UserId | 8 | 外键，手动处理 |
| | MedicalCaseId | 8 | 外键，手动处理 |
| | PatientId | 4 | 外键，手动处理 |
| **集合/导航** | Items | 7 | 需手动映射集合 |
| | Herbs | 4 | 需手动映射集合 |
| | Prescription | 4 | 导航属性 |
| | Consultation | 4 | 导航属性 |
| **状态字段** | NeedsPrescription | 8 | 计算/业务字段 |
| **打印相关** | PrintCount/Version/Logs | 9 | Entity独有 |

## 解决方案

### 原则

1. **审计字段统一忽略**: Entity的审计字段(CreatedBy, UpdatedBy, RowVersion, IsDeleted)在映射到DTO时统一添加`[MapperIgnoreSource]`
2. **集合属性手动映射**: Items, Herbs等集合在Core方法忽略，包装方法手动处理
3. **外键字段**: InputDto中的外键(MedicalCaseId, PatientId等)在Service层手动设置
4. **计算字段**: DTO中的计算字段(TotalPrice, Age等)在包装方法中处理

### 修复模式

```csharp
// 模式1: Entity → ListDto/DetailDto (忽略审计字段和内部字段)
[MapperIgnoreSource(nameof(Entity.CreatedBy))]
[MapperIgnoreSource(nameof(Entity.UpdatedBy))]
[MapperIgnoreSource(nameof(Entity.RowVersion))]
[MapperIgnoreSource(nameof(Entity.IsDeleted))]
[MapperIgnoreSource(nameof(Entity.UpdatedAt))]  // 如果ListDto不需要
[MapperIgnoreSource(nameof(Entity.Items))]      // 集合手动映射
public partial XxxDto ToDto(Entity entity);

// 模式2: InputDto → Entity (忽略外键和计算字段)
[MapperIgnoreSource(nameof(InputDto.Id))]           // 手动处理
[MapperIgnoreSource(nameof(InputDto.Items))]        // 集合手动映射
[MapperIgnoreSource(nameof(InputDto.TotalPrice))]   // 计算字段
[MapperIgnoreSource(nameof(InputDto.MedicalCaseId))]// 外键在Service设置
[MapperIgnoreTarget(nameof(Entity.Status))]         // 如果InputDto没有
public partial Entity ToEntity(InputDto dto);
```

## 执行计划

### 阶段1: 服务端Mapper (按警告数量排序)

1. **MedicalCaseMapper.cs** (66条) - 最复杂，包含嵌套映射
2. **FormulaMapper.cs** (43条) - 包含FormulaHerbItem
3. **PrescriptionMapper.cs** (39条) - 包含PrescriptionItem
4. **UserMapper.cs (Server)** (~27条)
5. **PatientMapper.cs** (28条)
6. **ConsultationMapper.cs** (18条)
7. **HerbMapper.cs** (15条)

### 阶段2: 客户端Mapper

1. **UserMapper.cs (Desktop.Users)** (~11条)

## 验证

修复完成后执行:
```bash
dotnet build LYBT.All.sln -c Release --no-restore 2>&1 | grep "warning"
```

目标: 0 Mapperly警告
