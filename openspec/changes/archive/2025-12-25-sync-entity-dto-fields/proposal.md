# Proposal: sync-entity-dto-fields

## Summary
同步业务字段属性，实现从后端Entity到前端UI的全数据流字段一致性。确保ListDto、DetailDto、InputDto、DependencyProperty、XAML标签在内容、属性类型、必填特性等方面保持一致。

## Problem Statement
当前系统存在以下字段同步问题：
1. **类型不一致**: 如`CostPrice`在Entity为`decimal?`，但前端DependencyProperty曾定义为`decimal`
2. **必填特性不一致**: 如`CostPrice`在Entity为可空（非必填），但前端验证要求必填
3. **标签文本不一致**: Entity的`[DisplayName]`与XAML标签文本可能不匹配
4. **DTO字段遗漏**: ListDto可能缺少某些需要显示的字段
5. **验证规则分散**: DataAnnotations、FluentValidation、前端验证各自独立，易产生冲突

## Goals
1. 建立从Entity到UI的字段同步规范
2. 定义ListDto（简略）和DetailDto（完整）的字段选择标准
3. 统一必填/可空字段的处理方式
4. 确保中文标签的一致性

## Non-Goals
- 不改变现有DTO架构模式
- 不引入自动代码生成工具
- 不修改数据库Schema

## Affected Specs
- dto-architecture (MODIFIED)
- viewmodel-conventions (MODIFIED)

## Scope
### In Scope
- Herbs, Users, Patients, Formula, MedicalCase, Prescriptions模块的DTO字段同步
- Entity → ListDto/DetailDto/InputDto 字段映射规范
- InputDto → Validator 验证规则一致性
- ViewModel DependencyProperty 类型与DTO一致性
- XAML标签与Entity DisplayName一致性

### Out of Scope
- 新增DTO类型
- 修改API接口
- 数据库迁移

## Risk Assessment
- **Low Risk**: 仅调整字段属性和标签，不改变业务逻辑
- **Regression**: 需验证所有模块的创建/编辑/查看功能

## Success Criteria
1. 所有模块的DTO字段类型与Entity一致
2. 必填/可空属性从Entity到UI保持一致
3. 中文标签从Entity的DisplayName到XAML保持一致
4. 编译通过，无警告
5. 基本CRUD功能正常
