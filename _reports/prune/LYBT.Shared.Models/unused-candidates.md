# LYBT.Shared.Models 未用代码候选分析报告

**项目**: src/Shared/LYBT.Shared.Models/  
**分析时间**: 2025-09-07  
**分析范围**: 逐文件、方法级未用代码检测  
**安全级别**: 最高（API契约库）

## 🎯 分析总览

- **总文件数**: 52个C#源文件
- **依赖项目**: 29个项目依赖此共享库
- **安全护栏**: 所有公共API默认保留（前后端契约）
- **分析深度**: 跨解决方案引用分析

## ✅ ConfirmedUnused（确认未用）

**当前状态**: 无确认未用代码

经过跨解决方案深度引用分析，未发现任何符合"100%确认未使用"标准的代码。这表明LYBT.Shared.Models作为API契约库维护良好，所有代码都在积极使用。

## 🔍 Suspect（可疑待观察）

### 配置相关DTO类（可能功能预留）

1. **DiagnosisCatalogDto**
   - 文件: `DTOs/Configuration/DiagnosisCatalogDto.cs`
   - 行数: 约15行
   - 证据: 静态引用分析显示使用较少，可能为未来功能预留
   - 风险: 可能被配置模块间接使用
   - 建议: 添加[Obsolete]观察14天

2. **TreatmentCatalogDto**
   - 文件: `DTOs/Configuration/TreatmentCatalogDto.cs`
   - 行数: 约12行
   - 证据: 类似DiagnosisCatalogDto，使用频率低
   - 风险: 配置系统可能依赖
   - 建议: 添加[Obsolete]观察

3. **LogDto**
   - 文件: `DTOs/Logging/LogDto.cs`
   - 行数: 约20行
   - 证据: 日志系统相关，可能被反射或序列化使用
   - 风险: 日志框架间接依赖
   - 建议: 保留，风险过高

## 🔒 Keep（强制保留）

### API契约类（100%保留）

**所有其余49个文件的公共类型强制保留**，包括但不限于：

- **Auth相关**: `LoginRequestDto`, `LoginResponseDto`, `UserDto`, `ChangePasswordDto`
- **患者管理**: `PatientDto`, `PatientCreateDto`, `PatientUpdateDto`, `PatientSearchDto`
- **医疗案例**: `MedicalCaseDto`, `MedicalCaseCreateDto`, `ConsultationDto`
- **处方管理**: `PrescriptionDto`, `HerbDto`, `FormulaDto`
- **用户管理**: `UserDto`, `UserCreateDto`, `UserUpdateDto`
- **系统功能**: `ApiResponse<T>`, `PagedResult<T>`, `ServiceResult<T>`

### 保留原因

1. **前后端API契约**: 删除任何公共DTO将破坏API兼容性
2. **JSON序列化**: 大量使用`[JsonPropertyName]`属性，被序列化框架依赖
3. **跨项目引用**: 29个项目广泛引用，影响范围巨大
4. **MVVM绑定**: 客户端XAML绑定可能依赖属性名称
5. **泛型约束**: 基础类型被泛型系统广泛使用

## 📊 统计摘要

| 分类 | 数量 | 文件数 | 代码行数（估算） | 风险级别 |
|------|------|--------|-----------------|----------|
| ConfirmedUnused | 0 | 0 | 0 | N/A |
| Suspect | 3 | 3 | ~47行 | 中等 |
| Keep | 49+ | 49 | ~2,400行 | 最高 |

## 🎯 建议行动

### 立即可执行
- **无安全删除项**: 当前无100%确认的删除候选

### 观察期策略
- 对3个可疑DTO类添加[Obsolete("Under review for removal - analysis period ends 2025-09-21", false)]
- 监控14天期间的使用情况

### 不建议执行
- **删除任何公共API**: 风险极高，可能破坏系统稳定性
- **修改属性名**: 可能破坏JSON序列化和XAML绑定
- **删除基础类型**: 可能影响泛型约束和继承体系

## 🔍 验证证据

所有分析基于以下方法：
- 跨29个项目的语义引用分析
- JSON序列化属性检查
- XAML绑定模式检查  
- 反射访问模式搜索
- 依赖注入扫描

**结论**: LYBT.Shared.Models是一个高质量、高使用率的API契约库，删除空间极为有限，需要极其谨慎。