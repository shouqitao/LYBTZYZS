# OpenSpec Proposal: standardize-api-architecture

## Summary

全项目API架构标准化重构，统一Server端和Desktop端的API设计模式、映射策略、DTO规范和错误处理机制。

## Motivation

### 现状分析

#### Server端API现状（7个Controller）

| 模块 | Controller | 端点数 | Mapper | DTO规范 |
|------|------------|--------|--------|---------|
| Auth | AuthController | 8 | 无(手工) | 统一 |
| Users | UsersController | 15 | UserMapper(Mapperly) | 统一 |
| Patients | PatientsController | 10 | PatientMapper(Mapperly) | 统一 |
| Herbs | HerbsController | 14 | HerbMapper(Mapperly) | 统一 |
| Formula | FormulasController | 18 | FormulaMapper(Mapperly) | 统一 |
| MedicalCase | MedicalCaseController | 20 | MedicalCaseMapper(Mapperly) | 统一 |
| Consultation | (内嵌MedicalCase) | - | ConsultationMapper(Mapperly) | 统一 |
| Prescriptions | (内嵌MedicalCase) | - | PrescriptionMapper(Mapperly) | 统一 |

**Server端问题**:
1. 部分模块仍使用IMapper接口注入而非直接实例化Mapper
2. Mapper注册方式不一致（DI vs 直接new）
3. 缺少统一的API版本管理策略
4. 错误响应格式不完全统一

#### Desktop端API现状（6个API接口）

| 模块 | API接口 | 端点数 | Mapper类型 | MappingService |
|------|---------|--------|------------|----------------|
| Auth | IAuthApi | 9 | 无 | 无 |
| Users | IUserApi | 15 | UserMapper(Mapperly) | UserMappingService |
| Patients | IPatientApi | 10 | PatientMapper(Mapperly) | PatientMappingService |
| Herbs | IHerbApi | 14 | HerbMapper(Mapperly) | HerbMappingService |
| Formula | IFormulaApi | 18 | FormulaMapper+FormulaHerbItemMapper | FormulaMappingService+FormulaDetailModelMappingService |
| MedicalCase | IMedicalCaseApi | 22 | 多个Mapper | 多个MappingService |

**Desktop端问题**:
1. **双层映射架构冗余**: Mapper(Mapperly) + MappingService(手工)共存
2. **MappingService职责模糊**: 部分包含业务逻辑，部分仅做简单映射
3. **Item模型不一致**: 部分模块用xxxItem，部分用xxxDetailModel
4. **Mapper粒度不一致**: Formula有3个Mapper，MedicalCase有4个Mapper

### 目标状态

1. **Server端**: 100% Mapperly直接实例化，统一错误响应，API版本v2
2. **Desktop端**: 消除MappingService层，统一Item命名，Mapper职责清晰
3. **全栈统一**: 相同的DTO命名规范，一致的映射策略

## Scope

### In Scope

1. **Server端统一**:
   - Mapper注册方式标准化（移除所有IMapper依赖）
   - 错误响应格式统一（ProblemDetails）
   - 移除所有AutoMapper相关代码和配置

2. **Desktop端彻底重构**:
   - **完全消除**MappingService层（不保留任何手工映射服务）
   - 统一Item命名（xxxItem，移除所有DetailModel命名）
   - Mapper职责单一化（仅做DTO↔Item映射）
   - **删除所有**冗余的Mapper和MappingService文件

3. **DTO规范统一**:
   - 命名规范：`{Entity}ListDto`、`{Entity}DetailDto`、`{Entity}InputDto`
   - 分页响应统一：`PagedResult<T>`
   - 批量操作统一：`BatchOperationResultDto`

4. **文档同步**:
   - 更新API文档
   - 更新架构说明

### Out of Scope

- 业务逻辑变更
- 数据库Schema变更
- 新功能添加
- UI变更

## Proposed Solution

### 重构原则：彻底清理，不留兼容

1. **Server端**: 移除所有IMapper/AutoMapper痕迹，100%使用Mapperly直接实例化
2. **Desktop端**: 删除所有MappingService，合并到单一Mapper
3. **命名统一**: 强制xxxItem命名，彻底移除DetailModel概念
4. **无过渡期**: 一次性完成重构，不保留旧代码路径

### 1. Server端Mapper标准化

**现状**（需完全移除）:
```csharp
// 当前：通过DI注入IMapper（AutoMapper模式）
public class HerbService
{
    private readonly IMapper _mapper;
    public HerbService(IMapper mapper) { _mapper = mapper; }
}

// Module中的AutoMapper注册（需删除）
services.AddAutoMapper(typeof(HerbMappingProfile));
```

**目标**（强制标准）:
```csharp
// 目标：直接实例化Mapperly Mapper（无DI）
public class HerbService
{
    private readonly HerbMapper _mapper = new();
}

// 无需任何DI注册
```

### 2. Desktop端Mapper彻底重构

**现状**（冗余双层架构，需完全清除）:
```
当前混乱状态：
├── Mapper(Mapperly)       # 部分模块有
├── MappingService(手工)   # 几乎所有模块有
├── DetailModel            # 部分模块有
└── Item                   # 部分模块有

DTO → MappingService(手工) → Item/DetailModel  ← 删除此路径
DTO → Mapper(Mapperly) → Item                  ← 保留此路径
```

**目标**（统一单层架构）:
```
清理后标准：
└── Mapper(Mapperly) → Item

DTO → Mapper(Mapperly) → Item  ← 唯一允许的映射路径
```

**彻底清理策略**:
- **删除所有MappingService文件** - 不保留任何手工映射服务
- **合并所有Mapper为单一文件** - 每模块仅一个xxxMapper.cs
- **强制xxxItem命名** - DetailModel一律重命名为Item
- 计算属性移到Item的getter中 - 不使用MappingService计算

### 3. Item命名标准化（强制执行）

| 模块 | 当前 | 目标 | 操作 |
|------|------|------|------|
| Users | UserItem | UserItem | 保留 |
| Patients | PatientItem | PatientItem | 保留 |
| Herbs | HerbItem | HerbItem | 保留 |
| Formula | FormulaItem / **FormulaDetailModel** | FormulaItem | **删除DetailModel** |
| MedicalCase | MedicalCaseItem / **MedicalCaseDetailModel** | MedicalCaseItem | **删除DetailModel** |
| Consultation | ConsultationItem | ConsultationItem | 保留 |

**强制规则**: 项目中不允许存在`*DetailModel`命名的数据模型类（ViewModel除外）

### 4. 错误响应标准化

**统一使用ProblemDetails（RFC 7807）**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Name": ["Name is required."]
  }
}
```

## Alternatives Considered

### 方案A：保留MappingService层作为兼容（不采纳）

**理由**:
- 增加维护成本
- 职责边界模糊
- 与Server端模式不一致
- **用户明确要求不设计兼容模式**

### 方案B：使用AutoMapper替代Mapperly（不采纳）

**理由**:
- AutoMapper运行时反射性能较差
- Mapperly编译时生成代码，性能更优
- 项目已迁移到Mapperly
- **彻底移除AutoMapper依赖是本提案核心目标**

### 方案C：渐进式迁移，保留旧代码（不采纳）

**理由**:
- 用户明确要求"彻底重构"
- 渐进式迁移会留下技术债务
- 一次性完成更加干净利落

## Implementation Plan

### Phase 1: Server端标准化（1天）

1. 统一Mapper实例化方式
2. 移除IMapper接口依赖
3. 标准化错误响应

### Phase 2: Desktop Mapper整合（2天）

1. Users模块: 删除UserMappingService，扩展UserMapper
2. Patients模块: 删除PatientMappingService，扩展PatientMapper
3. Herbs模块: 删除HerbMappingService，扩展HerbMapper
4. Formula模块: 合并3个Mapper为1个，删除MappingService
5. MedicalCase模块: 合并4个Mapper为2个，删除MappingService

### Phase 3: Item命名统一（0.5天）

1. FormulaDetailModel → FormulaItem
2. MedicalCaseDetailModel → MedicalCaseItem
3. 更新所有引用

### Phase 4: 文档和测试（0.5天）

1. 更新API文档
2. 更新架构说明
3. 运行全量测试

## Success Criteria（硬性标准）

1. **Server端**:
   - 0个IMapper接口引用
   - 0个AutoMapper相关NuGet包引用
   - 0个MappingProfile类
   - 所有Mapper直接实例化

2. **Desktop端**:
   - 0个MappingService类（完全删除）
   - 每模块仅1个xxxMapper.cs文件
   - 所有映射通过Mapper完成

3. **命名统一**:
   - 0个*DetailModel数据模型类
   - 100%模块使用xxxItem命名

4. **代码清洁度**:
   - 0个Obsolete映射方法
   - 0个遗留的手工映射代码

5. **测试通过**: 全量单元测试和集成测试通过
6. **编译成功**: 0错误0警告（Mapperly相关）

## Risks and Mitigations

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 映射逻辑丢失 | 功能异常 | 逐模块迁移，充分测试 |
| 重命名遗漏引用 | 编译错误 | 使用IDE重构工具，全量编译验证 |
| 业务逻辑混入Mapper | 架构污染 | 代码审查，明确Mapper职责边界 |

## Dependencies

- adopt-mapperly-unified-mapping（已完成基础迁移）
- unify-herb-controls-to-herbs-module（Desktop Herbs模块重构）

## References

- [Mapperly Documentation](https://mapperly.riok.app/)
- [RFC 7807 Problem Details](https://tools.ietf.org/html/rfc7807)
- archived/optimize-medicalcase-api - MedicalCase API优化参考
- archived/simplify-medicalcase-api - API简化模式参考

---

**Author**: Claude Code
**Created**: 2026-01-07
**Status**: Draft
