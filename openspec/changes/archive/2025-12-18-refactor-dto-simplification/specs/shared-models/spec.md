## MODIFIED Requirements

### Requirement: DTO设计规范

系统SHALL使用扁平化DTO设计模式，遵循以下规范:

1. **标准类型**: 每个实体最多4种DTO类型
   - ListDto: 列表视图用，包含必要显示字段
   - DetailDto: 详情视图用，包含所有可读字段
   - InputDto: 创建/编辑用，包含所有可写字段
   - ItemInputDto: 子项输入用(如有需要)

2. **扁平化**: DTO不使用继承链，所有字段直接声明

3. **文件组织**:
   - 一个DTO一个文件
   - 按模块组织到对应文件夹

4. **命名规范**:
   - 列表: `{Entity}ListDto`
   - 详情: `{Entity}DetailDto`
   - 输入: `{Entity}InputDto`

#### Scenario: Prescription模块DTO结构
- **GIVEN** Prescription模块需要DTO支持
- **WHEN** 开发者查看DTO文件
- **THEN** 应只存在4个DTO文件: PrescriptionListDto.cs, PrescriptionDetailDto.cs, PrescriptionInputDto.cs, PrescriptionItemInputDto.cs
- **AND** 每个DTO文件只包含一个类
- **AND** 类不使用继承(不继承BaseDto等基类)

#### Scenario: DTO字段定义
- **GIVEN** 需要创建新DTO
- **WHEN** 定义DTO字段
- **THEN** 所有需要的字段直接在类中声明
- **AND** 不依赖基类提供字段
- **AND** 字段使用DataAnnotation进行验证

#### Scenario: 列表与详情分离
- **GIVEN** API需要返回实体列表
- **WHEN** 调用列表API
- **THEN** 返回ListDto(精简字段)
- **AND** 不返回DetailDto(完整字段)以减少数据传输

## REMOVED Requirements

### Requirement: DTO继承层次
**Reason**: 过度工程化，增加理解成本但无实际复用价值
**Migration**: 将继承字段直接复制到各DTO类中

### Requirement: 多变体DTO
**Reason**: CreateDto/EditDto/QueryDto/SearchDto等变体功能重叠
**Migration**: 合并为统一的InputDto，使用可空字段区分创建/编辑
