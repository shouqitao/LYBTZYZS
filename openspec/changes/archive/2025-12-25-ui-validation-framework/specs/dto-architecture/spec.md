# dto-architecture Specification Delta

本变更向dto-architecture规范添加验证规则同步相关规范。

---

## ADDED Requirements

### Requirement: DTO-010 ValidationConstants统一常量规范

验证规则 MUST 使用统一的ValidationConstants类定义。

**规范**:
- 验证常量 SHALL 定义在`LYBT.Shared.Primitives.Validation.ValidationConstants`（已存在）
- 常量 SHALL 按类型分组（StringLength、Range、Messages）
- 所有层 SHALL 引用相同常量避免硬编码

#### Scenario: 字符串长度常量
- **GIVEN** 需要定义字符串长度验证
- **WHEN** 使用常量
- **THEN** 引用`ValidationConstants.NameMaxLength`等常量
- **AND** 不直接硬编码数字

#### Scenario: 错误消息常量
- **GIVEN** 需要定义验证错误消息
- **WHEN** 使用消息模板
- **THEN** 引用`ValidationConstants.RequiredErrorMessage`等
- **AND** 消息使用中文

---

### Requirement: DTO-011 验证规则同步规范

Entity、DTO、DetailModel三层验证规则 MUST 保持一致。

**规范**:
- 三层 SHALL 使用相同的ValidationConstants
- 规则变更 SHALL 同步更新所有层
- 代码审查 SHALL 检查规则一致性

#### Scenario: 验证规则同步
- **GIVEN** 修改Entity的验证规则
- **WHEN** 完成修改
- **THEN** 同步更新对应DTO的验证特性
- **AND** 同步更新对应DetailModel的验证特性
- **AND** 验证FluentValidation规则一致

#### Scenario: 新增字段验证
- **GIVEN** 新增需要验证的字段
- **WHEN** 添加验证
- **THEN** Entity添加DataAnnotation
- **AND** InputDto添加DataAnnotation
- **AND** DetailModel添加DataAnnotation
- **AND** 如有FluentValidation也添加规则

---

### Requirement: DTO-012 前端验证与后端验证关系规范

前端验证 SHALL NOT 替代后端验证，但 MUST 提供即时反馈。

**规范**:
- 前端验证 SHALL 在用户输入时立即执行
- 后端验证 SHALL 在API请求时执行
- 验证失败消息 SHALL 一致

#### Scenario: 验证层级关系
- **GIVEN** 用户提交表单
- **WHEN** 数据流经各层
- **THEN** UI层先执行即时验证
- **AND** DTO层执行DataAnnotation验证
- **AND** Server层执行FluentValidation验证
- **AND** 任一层失败都阻止后续操作

---

## Rationale

1. **规则统一** - 使用ValidationConstants避免各层规则不一致
2. **即时反馈** - 前端验证提供良好用户体验
3. **安全保障** - 后端验证确保数据安全性
4. **可维护性** - 集中管理验证规则便于修改
