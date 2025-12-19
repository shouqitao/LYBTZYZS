## ADDED Requirements

### Requirement: Unused Code Cleanup Policy

系统 SHALL 定期清理未使用的代码，确保代码库整洁可维护。

#### Scenario: Dialog组件清理标准
- **WHEN** Dialog组件未在任何Module中通过RegisterDialog注册
- **AND** 未被任何ViewModel直接引用
- **THEN** 该Dialog及其ViewModel应被标记为待清理
- **AND** 确认无使用后删除

#### Scenario: Service类清理标准
- **WHEN** Service类未在Module的RegisterTypes中注册
- **AND** 未被其他类通过构造函数注入或直接实例化使用
- **THEN** 该Service应被标记为待清理
- **AND** 确认无使用后删除

#### Scenario: 接口清理标准
- **WHEN** 接口定义无对应实现类
- **OR** 接口的实现类未被注册使用
- **THEN** 该接口应被评估是否保留
- **AND** 无明确使用计划则删除

### Requirement: Dialog Registration Convention

所有Dialog组件 SHALL 通过Prism的RegisterDialog方式注册，禁止在ViewModel中直接new创建Dialog实例。

#### Scenario: 标准Dialog注册
- **WHEN** 需要在模块中使用Dialog
- **THEN** 在Module.RegisterTypes中调用RegisterDialog<View, ViewModel>
- **AND** 通过IDialogService.ShowDialog方式打开Dialog

#### Scenario: 禁止直接实例化Dialog
- **WHEN** ViewModel需要显示Dialog
- **THEN** 必须通过IDialogService注入
- **AND** 禁止使用new Views.XxxDialog()方式创建
