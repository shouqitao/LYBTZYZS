# Change: DTO设计简化重构

## Why

当前项目DTO设计存在过度工程化问题:
1. **继承链过深**: BaseDto → TimestampDto → StatusDto → 具体DTO，增加理解成本
2. **接口过度抽象**: IIdentifiable, IAuditable, ICreatorTrackable等接口实际使用率低
3. **DTO变体过多**: 单个模块(如Prescription)有20+个DTO类，功能高度重叠
4. **单文件多类**: 所有DTO堆积在单个大文件中，难以维护

用户原始设计意图(符合Microsoft最佳实践):
- **ListDto**: 列表视图用，仅包含必要字段
- **DetailDto/InputDto**: 详情/编辑用，包含完整字段

## What Changes

### Phase 1: 建立新DTO规范
- 创建DTO设计规范文档
- 定义标准DTO类型: ListDto, DetailDto, InputDto
- 移除继承链，改为扁平化设计

### Phase 2: 重构Prescription模块DTOs
- 简化为4个核心DTO: ListDto, DetailDto, InputDto, ItemInputDto
- 一个DTO一个文件
- 统一放在Prescriptions文件夹

### Phase 3: 重构其他模块DTOs
- Formula模块DTO简化
- Herb模块DTO简化
- Patient模块DTO简化
- MedicalCase模块DTO简化

### Phase 4: 清理遗留代码
- 移除废弃的DtoBase.cs基类
- 移除未使用的接口
- 更新所有引用

## Impact

- **Affected specs**: shared-models
- **Affected code**:
  - `src/Shared/LYBT.Shared.Models/Contracts/` - 所有DTO文件
  - `src/Server/Modules/*/Mapping/` - AutoMapper配置
  - `src/Client/Desktop/*/` - Desktop层DTO使用
  - `tests/` - 相关测试文件

## 设计原则

1. **扁平化**: 每个DTO自包含所有需要的字段，不使用继承
2. **一文件一DTO**: 便于定位、阅读和维护
3. **模块文件夹**: `Contracts/{Module}/` 下按模块组织
4. **最小化变体**: 每个实体最多3-4个DTO(List/Detail/Input/ItemInput)
5. **无嵌套**: DTO之间不嵌套引用，使用扁平结构

## 迁移策略

采用渐进式迁移，保持向后兼容:
1. 新建简化DTO文件
2. 更新Controller/Service使用新DTO
3. 标记旧DTO为Obsolete
4. 确认无引用后删除旧DTO
