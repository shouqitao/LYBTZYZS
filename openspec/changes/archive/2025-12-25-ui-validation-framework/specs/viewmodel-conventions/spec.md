# viewmodel-conventions Specification Delta

本变更向viewmodel-conventions规范添加UI层验证相关规范。

---

## ADDED Requirements

### Requirement: VM-010 DetailModel验证基类规范

DetailModel类 MUST 使用ValidatableModelBase基类以支持即时验证。

**规范**:
- DetailModel SHALL 继承`ValidatableModelBase`而非`BindableBase`
- 属性 SHALL 使用`SetPropertyAndValidate`方法触发即时验证
- 验证属性 SHALL 使用标准DataAnnotations

#### Scenario: DetailModel继承模式
- **GIVEN** 需要创建新的DetailModel
- **WHEN** 定义类结构
- **THEN** 继承`ValidatableModelBase`
- **AND** 属性setter使用`SetPropertyAndValidate`
- **AND** 添加必要的验证特性

#### Scenario: 验证触发时机
- **GIVEN** 用户修改DetailModel属性
- **WHEN** 属性值变化
- **THEN** 自动执行ValidateProperty验证
- **AND** 验证错误通过INotifyDataErrorInfo通知UI

---

### Requirement: VM-011 验证特性使用规范

DetailModel属性 MUST 使用标准DataAnnotations定义验证规则。

**规范**:
- 必填字段 SHALL 使用`[Required]`特性
- 字符串长度 SHALL 使用`[StringLength]`特性
- 数值范围 SHALL 使用`[Range]`特性
- 验证常量 SHALL 引用`ValidationConstants`类

#### Scenario: 必填字段验证
- **GIVEN** 属性为必填
- **WHEN** 定义属性
- **THEN** 添加`[Required(ErrorMessage = "xxx不能为空")]`
- **AND** 错误消息使用中文

#### Scenario: 字符串长度验证
- **GIVEN** 字符串属性有长度限制
- **WHEN** 定义属性
- **THEN** 使用`ValidationConstants.NameMaxLength`等常量
- **AND** 添加`[StringLength]`特性

#### Scenario: 数值范围验证
- **GIVEN** 数值属性有范围限制
- **WHEN** 定义属性
- **THEN** 使用`ValidationConstants.AgeMinValue/MaxValue`等常量
- **AND** 添加`[Range]`特性

---

### Requirement: VM-012 保存前验证规范

MasterDetailViewModel MUST 在SaveDetailAsync前执行完整验证。

**规范**:
- SaveDetailAsync SHALL 先调用`CurrentDetail.ValidateAll()`
- 验证失败 SHALL 阻止保存并显示错误
- CanSave条件 SHALL 检查`!CurrentDetail.HasErrors`

#### Scenario: 保存前验证流程
- **GIVEN** 用户点击保存按钮
- **WHEN** 执行SaveDetailAsync
- **THEN** 先调用ValidateAll()
- **AND** 如HasErrors为true则显示错误并返回
- **AND** 如无错误则继续保存流程

#### Scenario: 保存按钮状态
- **GIVEN** DetailModel有验证错误
- **WHEN** 检查保存按钮状态
- **THEN** CanSave返回false
- **AND** 保存按钮显示为禁用状态

---

### Requirement: VM-013 XAML验证绑定规范

EditControl中的输入控件 MUST 启用验证通知。

**规范**:
- 绑定 SHALL 包含`ValidatesOnNotifyDataErrors=True`
- 错误消息 SHALL 绑定到`Errors[PropertyName]`
- 必填字段 SHALL 显示红色星号标识
- 验证失败 SHALL 在输入框下方显示错误消息

#### Scenario: 标准验证绑定模式
- **GIVEN** EditControl中的TextBox
- **WHEN** 定义绑定
- **THEN** 使用`{Binding Property, ValidatesOnNotifyDataErrors=True}`
- **AND** 使用`ValidatingTextBoxStyle`样式
- **AND** 添加错误消息TextBlock

#### Scenario: 必填字段标识
- **GIVEN** 字段为必填
- **WHEN** 显示标签
- **THEN** 标签后显示红色星号" *"
- **AND** 使用`RequiredIndicatorStyle`样式

---

## Rationale

1. **即时反馈** - 用户输入时立即看到验证结果，无需等待提交
2. **规则统一** - 使用ValidationConstants确保前后端验证规则一致
3. **渐进迁移** - ValidatableModelBase独立于ViewModelBase，不影响现有代码
4. **标准模式** - 使用WPF原生INotifyDataErrorInfo接口，与框架集成良好
