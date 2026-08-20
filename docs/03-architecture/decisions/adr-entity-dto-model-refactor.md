# ADR: Entity/DTO/Model 三层定义重构
> 版本: v1.0 | 日期: 2026-08-20

> **状态**：第一批（DTO 层移除脱节字段 + 命名统一）已实施（2026-08-14，见总账 Entity/DTO/Model 重构第一批行）；第二批（Mapper 简化——PrescriptionItemMapper 共享类 + FormulaHerbItem 通用方法 + PinYinCode/Guid 模式抽取）已实施（2026-08-14，见总账第二批行）；Desktop Model 命名对齐（ADR §4）评估后有意保留（与既有 ViewModel 层同名冲突 + INPC 模型不可从 DTO 派生）；验证器补全/UserId vs DoctorId 文档属后续批次。

## 背景

审计发现 29 个问题（6 高优/17 中等/6 低优），核心痛点：

- Formula DTO 与 Entity 严重脱节（5+ 幻影字段）
- 前端 Model 命名不统一
- Mapper 手动映射占 58%（复杂度高）
- DTO 嵌套越级引用（HerbDetailDto）
- 验证器缺失（RegistrationInputDto）

## 决策

### 方案选择：方案 C（Entity=SSOT）

Entity 是唯一定义源，DTO/Model 通过 Mapperly 自动投影。

### 命名规范

| 层级 | 后缀 | 示例 |
| ------ | ------ | ------ |
| Entity | XxxModel / Xxx（聚合根） | UserModel, MedicalCaseModel |
| DTO | XxxDetailDto / XxxInputDto / XxxListDto | UserDetailDto, UserInputDto |
| Desktop | XxxDetailViewModel / XxxEditViewModel / XxxListViewModel | UserDetailViewModel |

### 转换链

```
Entity → DTO（Mapperly 编译时自动）
  ↓ 手动 Enrich 仅在必要时（如跨聚合字段）
DTO → Desktop ViewModel（Desktop Mapperly）
```

### 具体修改

#### 1. DTO 层（移除脱节字段 + 命名统一）

- FormulaInputDto：移除 Description/Instructions/Preparation/Contraindications/Source
- FormulaDetailDto：同步移除
- FormulaHerbItemDto：移除 Price/SortOrder/SpecialInstructions + Preparation→ProcessingMethod
- FormulaHerbItemInputDto：移除嵌套 HerbDetailDto
- ConsultationDetailDto：移除父聚合字段
- PrescriptionDetailDto：移除伪字段 Status
- 命名：Notes→Remark 统一

#### 2. Mapper 层（简化复杂度）

- PrescriptionItemMapper：抽取共享类（消除 2 处重复，-40行）
- FormulaHerbItem：抽取通用方法（消除 3 处重复，-30行）
- `Id == Guid.Empty ? null`：抽取扩展方法
- MedicalCaseMapper：Enrich 逻辑简化

#### 3. 验证层（补全缺失）

- RegistrationInputDtoValidator：新增
- UserInputDtoValidator：确认完整性

#### 4. Desktop Model 层（对齐 DTO）

- 移除重复定义（从 DTO 派生）
- 命名统一为 XxxViewModel

#### 5. UserId vs DoctorId

- 保留差异（语义不同：医案创建者 vs 挂号指定医生）
- 文档明确两个字段的区别

## 影响范围

- Entity 层：0 改动（不补字段）
- DTO 层：~15 文件修改
- Mapper 层：~10 文件修改（抽取/简化）
- Desktop Model 层：~8 文件修改（命名对齐）
- 验证层：2 个新增/确认
- 数据库：0 迁移（不改 Entity）

## 门禁

- build 0/0 + Server 全量 + 架构 87/87
- Mapperly 编译时检查
- Desktop 测试（ViewModel 测试基线）
