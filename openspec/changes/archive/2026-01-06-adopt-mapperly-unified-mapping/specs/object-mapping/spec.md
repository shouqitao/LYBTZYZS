# Object Mapping Specification

## MODIFIED Requirements

### Requirement: Server-side Entity-DTO Mapping
**Priority**: High
**Rationale**: 将AutoMapper替换为Mapperly，获得编译时安全和更好性能

Server端Entity与DTO之间的映射 **SHALL** 使用Mapperly源生成器实现。系统 **MUST** 在编译时生成映射代码，不得使用运行时反射。

#### Scenario: MedicalCase Entity to DetailDto mapping
**Given** 一个MedicalCaseEntity实例包含完整数据
**When** 使用MedicalCaseMapper.ToDetailDto()映射
**Then** 返回的MedicalCaseDetailDto包含所有映射字段
**And** 关联的Consultation和Prescription也被正确映射
**And** 映射在编译时生成，无运行时反射开销

#### Scenario: MedicalCase InputDto to Entity mapping (Create)
**Given** 一个有效的MedicalCaseInputDto
**When** 使用MedicalCaseMapper.ToEntity()创建新实体
**Then** 返回的MedicalCaseEntity包含所有输入字段
**And** 审计字段(CreatedAt等)保持默认值

#### Scenario: MedicalCase InputDto to Entity mapping (Update)
**Given** 一个现有的MedicalCaseEntity
**And** 一个包含更新数据的MedicalCaseInputDto
**When** 使用MedicalCaseMapper.UpdateEntity()更新实体
**Then** 实体的可编辑字段被更新
**And** 只读字段(Id, CreatedAt)保持不变

---

### Requirement: Desktop-side DTO-Item Mapping
**Priority**: High
**Rationale**: 消除手写映射代码，统一映射实现

Desktop端DTO与Item(ViewModel绑定模型)之间的映射 **SHALL** 使用Mapperly实现。系统 **MUST** 保持与Prism BindableBase的兼容性，正确处理UI状态字段的忽略。

#### Scenario: ConsultationDetailDto to ConsultationItem mapping
**Given** 一个从API返回的ConsultationDetailDto
**When** 使用ConsultationMapper.ToItem()映射
**Then** 返回的ConsultationItem包含所有业务字段
**And** UI状态字段(IsSelected, IsExpanded)保持默认值
**And** 计算属性(IsDiagnosisComplete, DisplayText)可正常访问

#### Scenario: ConsultationItem to InputDto mapping (Save)
**Given** 一个用户已编辑的ConsultationItem
**When** 使用ConsultationMapper.ToInputDto()转换
**Then** 返回的ConsultationInputDto包含所有可保存字段
**And** UI状态字段和计算属性被忽略

#### Scenario: PrescriptionDetailDto to PrescriptionItem mapping
**Given** 一个从API返回的PrescriptionDetailDto
**And** DTO包含药材列表(Items)
**When** 使用PrescriptionMapper.ToItem()映射
**Then** 返回的PrescriptionItem包含所有业务字段
**And** Items集合被正确映射为ObservableCollection<HerbItemDto>
**And** 计算属性(ItemCount, TotalPrice, HasItems)可正常计算

---

### Requirement: Compile-time Mapping Validation
**Priority**: Medium
**Rationale**: 提前发现映射错误，避免运行时异常

所有映射配置 **SHALL** 在编译时验证完整性。编译器 **MUST** 在属性缺失或类型不匹配时产生警告或错误。

#### Scenario: Missing property mapping detection
**Given** 源类型有属性X但目标类型无对应属性
**When** 编译项目
**Then** 编译器产生警告MAPPERLY002
**And** 开发者可选择添加[MapperIgnoreSource]或修复映射

#### Scenario: Type mismatch detection
**Given** 源属性类型与目标属性类型不兼容
**When** 编译项目
**Then** 编译器产生错误MAPPERLY001
**And** 必须修复类型问题或添加自定义转换

---

## REMOVED Requirements

### Requirement: AutoMapper Runtime Configuration
**Rationale**: AutoMapper已商业化，且运行时反射性能较差

移除Server端的AutoMapper Profile配置和运行时映射。

#### Scenario: AutoMapper dependency removal
**Given** 项目当前依赖AutoMapper 12.0.1
**When** 完成Mapperly迁移
**Then** AutoMapper包引用被移除
**And** MappingProfile类被删除
**And** AddAutoMapper()调用被移除

---

### Requirement: Hand-written Mapping Methods in Item Classes
**Rationale**: 手写映射代码冗余，维护成本高

移除Item类中的静态FromDto和实例ToDto/ToInputDto方法。

#### Scenario: ConsultationItem mapping methods removal
**Given** ConsultationItem类包含FromDto/ToDto/ToInputDto方法
**When** 完成Mapperly迁移
**Then** 这三个方法被删除
**And** 映射逻辑移至ConsultationMapper类

#### Scenario: PrescriptionItem mapping methods removal
**Given** PrescriptionItem类包含FromDto/ToDto/ToInputDto方法
**When** 完成Mapperly迁移
**Then** 这三个方法被删除
**And** 映射逻辑移至PrescriptionMapper类
