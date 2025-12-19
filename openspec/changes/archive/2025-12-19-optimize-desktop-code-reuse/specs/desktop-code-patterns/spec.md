## ADDED Requirements

### Requirement: Component Directory Convention

Desktop模块 SHALL 遵循统一的组件目录规范，确保代码组织一致性。

#### Scenario: Services目录存放业务组件
- **WHEN** 组件负责业务逻辑处理（如数据管理、命令处理、验证）
- **THEN** 该组件必须放置在模块的 `Services/` 目录下
- **AND** 包括但不限于: DataManager、CommandHandler、Validator、Coordinator

#### Scenario: ViewModels/Components目录存放UI辅助组件
- **WHEN** 组件仅处理视图层辅助逻辑（如计算器、UI状态管理）
- **THEN** 该组件必须放置在模块的 `ViewModels/Components/` 目录下
- **AND** 这些组件不应包含业务规则，仅处理UI展示相关计算

#### Scenario: 禁止混用目录
- **WHEN** 业务组件（如XXXDataManager）被放置在ViewModels/Components/目录
- **THEN** 代码审查时应标记为架构违规
- **AND** 要求迁移至Services/目录

### Requirement: Component Base Class Pattern

业务组件 SHALL 继承统一的基类，减少重复代码。

#### Scenario: Validator组件继承规范
- **WHEN** 创建新的组件验证器（实现IComponentValidator）
- **THEN** 必须继承 `ComponentValidatorBase`
- **AND** 仅实现 `ValidateAsyncCore()` 抽象方法
- **AND** 异常处理和日志记录由基类统一处理

#### Scenario: CommandHandler组件继承规范
- **WHEN** 创建新的命令处理器（实现ICommandHandler）
- **THEN** 必须继承 `CommandHandlerBase`
- **AND** 使用基类提供的 `RegisterCommand()` 方法注册命令
- **AND** 命令执行的异常处理由基类统一处理

#### Scenario: DataManager组件接口规范
- **WHEN** 创建新的数据管理器
- **THEN** 必须实现 `IDataManager<TEntity>` 接口
- **AND** 包含标准方法: `LoadAsync()`, `ReloadAsync()`, `SaveAsync()`
- **AND** 提供 `Current` 属性访问当前实体

### Requirement: Module Boundary Clarity

模块边界 SHALL 清晰定义，避免职责重叠。

#### Scenario: 服务模块职责
- **WHEN** 模块仅提供服务而无独立UI（如Prescriptions）
- **THEN** 模块应明确标注为"服务库"角色
- **AND** Module.cs文件应包含清晰的职责说明注释
- **AND** 考虑将服务迁移至Core层（需评估）

#### Scenario: 依赖模块职责
- **WHEN** 模块主要依赖另一个模块提供功能（如Consultation依赖MedicalCase）
- **THEN** 应评估是否保持独立模块
- **AND** 如保持独立，应明确记录独立存在的理由
- **AND** 如选择合并，应作为子模块保持逻辑分离
