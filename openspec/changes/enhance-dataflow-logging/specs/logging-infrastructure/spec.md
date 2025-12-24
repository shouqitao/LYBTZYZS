# logging-infrastructure Spec Delta

## ADDED Requirements

### Requirement: LOG-012 HTTP客户端请求日志

Desktop端HTTP客户端 **SHALL** 自动记录所有API请求和响应的日志。

**日志内容**:
- 请求: Method, URI, CorrelationId
- 响应: StatusCode, Duration, CorrelationId
- 错误: 响应Body(脱敏后)

**实现方式**:
- 通过`DelegatingHandler`拦截Refit请求
- 使用`System.Diagnostics.Activity`传递CorrelationId
- 添加`traceparent` header支持分布式追踪

#### Scenario: 成功API请求日志
- **GIVEN** Desktop客户端发起API请求
- **WHEN** 请求成功(2xx响应)
- **THEN** **SHALL** 记录Information级别请求日志
- **AND** **SHALL** 记录Information级别响应日志
- **AND** 日志 **SHALL** 包含Method、URI、StatusCode、Duration
- **AND** 日志 **SHALL** 包含CorrelationId

#### Scenario: 失败API请求日志
- **GIVEN** Desktop客户端发起API请求
- **WHEN** 请求失败(非2xx响应)
- **THEN** **SHALL** 记录Warning级别响应日志
- **AND** **SHALL** 记录响应Body(脱敏后)
- **AND** 日志 **SHALL** 包含StatusCode和CorrelationId

#### Scenario: API请求异常日志
- **GIVEN** Desktop客户端发起API请求
- **WHEN** 发生网络异常或超时
- **THEN** **SHALL** 记录Error级别日志
- **AND** **SHALL** 包含异常详情和Duration
- **AND** 日志 **SHALL** 包含CorrelationId

---

### Requirement: LOG-013 分布式追踪Header传递

Desktop与Server之间 **SHALL** 通过HTTP Header传递追踪上下文。

**Header规范**:
- 使用W3C Trace Context标准
- 请求头: `traceparent`
- 响应头: `X-Correlation-Id`

#### Scenario: 请求追踪Header
- **GIVEN** Desktop发起HTTP请求
- **WHEN** Activity.Current存在
- **THEN** **SHALL** 添加traceparent header
- **AND** Header值 **SHALL** 为Activity.Id

#### Scenario: 响应追踪Header
- **GIVEN** Server处理HTTP请求
- **WHEN** 返回响应
- **THEN** **SHALL** 添加X-Correlation-Id响应头
- **AND** Header值 **SHALL** 与请求CorrelationId一致

#### Scenario: CorrelationId自动生成
- **GIVEN** 请求未携带traceparent header
- **WHEN** Server接收请求
- **THEN** **SHALL** 自动生成CorrelationId
- **AND** 生成的Id **SHALL** 用于后续日志记录

---

### Requirement: LOG-014 Server端API Action日志

Server端Controller Action **SHALL** 自动记录执行日志。

**日志内容**:
- 开始: Action名称, CorrelationId
- 结束: Action名称, Duration, 执行结果
- 参数: Debug级别记录(脱敏后)

**实现方式**:
- 通过`IAsyncActionFilter`全局拦截
- 复用CorrelationIdMiddleware设置的TraceIdentifier

#### Scenario: Action正常执行日志
- **GIVEN** 请求到达Controller Action
- **WHEN** Action执行成功
- **THEN** **SHALL** 记录Information级别开始日志
- **AND** **SHALL** 记录Information级别结束日志
- **AND** 结束日志 **SHALL** 包含执行Duration

#### Scenario: Action异常日志
- **GIVEN** 请求到达Controller Action
- **WHEN** Action执行抛出异常
- **THEN** **SHALL** 记录Error级别日志
- **AND** **SHALL** 包含异常详情和Duration
- **AND** 异常 **SHALL** 继续向上传播

#### Scenario: Action参数日志
- **GIVEN** Action接收参数
- **WHEN** 日志级别为Debug
- **THEN** **MAY** 记录参数摘要
- **AND** 敏感参数 **SHALL** 被脱敏

---

### Requirement: LOG-015 Repository操作日志

Repository层CRUD操作 **SHALL** 记录Debug级别日志。

**日志内容**:
- 操作类型: GetById, GetAll, Add, Update, Delete
- 实体类型: Entity名称
- 操作结果: 成功/失败, 受影响记录数

#### Scenario: Repository查询日志
- **GIVEN** 调用Repository.GetByIdAsync
- **WHEN** 查询执行
- **THEN** **SHALL** 记录Debug级别日志
- **AND** 日志 **SHALL** 包含实体类型和查询Id
- **AND** 日志 **SHALL** 包含查询结果(Found/NotFound)

#### Scenario: Repository写入日志
- **GIVEN** 调用Repository.AddAsync或UpdateAsync
- **WHEN** 写入执行
- **THEN** **SHALL** 记录Debug级别日志
- **AND** 日志 **SHALL** 包含实体类型
- **AND** 日志 **SHALL NOT** 包含实体详细数据

#### Scenario: Repository删除日志
- **GIVEN** 调用Repository.DeleteAsync
- **WHEN** 删除执行
- **THEN** **SHALL** 记录Debug级别日志
- **AND** 日志 **SHALL** 包含实体类型

---

### Requirement: LOG-016 URI敏感数据脱敏

HTTP请求URI中的敏感参数 **SHALL** 在日志中自动脱敏。

**敏感参数**:
- password, token, key, secret
- credential, apikey, access_token

#### Scenario: URI参数脱敏
- **GIVEN** HTTP请求URI包含敏感参数
- **WHEN** 记录HTTP日志
- **THEN** 敏感参数值 **SHALL** 被替换为"***"
- **AND** 参数名 **SHALL** 保留用于调试

#### Scenario: 非敏感URI参数
- **GIVEN** HTTP请求URI包含非敏感参数
- **WHEN** 记录HTTP日志
- **THEN** 参数 **SHALL** 完整记录

---

### Requirement: LOG-017 ViewModel操作日志

Desktop端ViewModel层 **SHALL** 记录用户操作的完整生命周期日志。

**日志内容**:
- 操作开始: 操作类型, 实体类型
- 操作成功: 操作类型, Duration
- 操作失败: 操作类型, ErrorMessage, Duration

**实现位置**:
- `MasterDetailViewModelBase`: 保存、删除、加载详情
- `UnifiedListViewModelBase`: 刷新、搜索、分页

#### Scenario: 保存操作日志
- **GIVEN** 用户在MasterDetail视图点击保存
- **WHEN** ExecuteSaveAsync执行
- **THEN** **SHALL** 记录Information级别开始日志
- **AND** 成功时 **SHALL** 记录Information级别完成日志
- **AND** 失败时 **SHALL** 记录Warning级别失败日志
- **AND** 日志 **SHALL** 包含实体类型和Duration

#### Scenario: 删除操作日志
- **GIVEN** 用户在MasterDetail视图点击删除
- **WHEN** ExecuteDeleteCurrentAsync执行
- **THEN** **SHALL** 记录Information级别开始日志
- **AND** 成功时 **SHALL** 记录Information级别完成日志
- **AND** 失败时 **SHALL** 记录Warning级别失败日志

#### Scenario: 加载详情日志
- **GIVEN** 用户选择列表项
- **WHEN** LoadDetailForSelectedItemAsync执行
- **THEN** **SHALL** 记录Debug级别开始日志
- **AND** 成功时 **SHALL** 记录Debug级别完成日志
- **AND** 失败时 **SHALL** 记录Error级别失败日志(已有)

#### Scenario: 列表刷新日志
- **GIVEN** 用户点击刷新或搜索
- **WHEN** RefreshAsync或SearchAsync执行
- **THEN** **SHALL** 记录Debug级别开始日志
- **AND** 完成时 **SHALL** 记录Debug级别日志
- **AND** 日志 **SHALL** 包含返回记录数

---

### Requirement: LOG-018 日志格式标准化

所有数据流转日志 **SHALL** 使用统一的前缀格式便于追踪和过滤。

**日志前缀规范**:
| 层级 | 前缀 | 示例 |
|------|------|------|
| ViewModel | [VM] | [VM] Save started |
| CommandHandler | [CMD] | [CMD] CreateUser |
| HTTP Client | [HTTP] | [HTTP] >>> POST /api/users |
| Controller | [API] | [API] >>> UsersController.Create |
| Service | [SVC] | [SVC] CreateAsync |
| Repository | [REPO] | [REPO] User.Add |

#### Scenario: 日志前缀格式
- **GIVEN** 任意层级记录日志
- **WHEN** 日志输出
- **THEN** 日志消息 **SHALL** 以对应层级前缀开头
- **AND** 前缀格式 **SHALL** 为 `[XXX]`

#### Scenario: 端到端日志追踪
- **GIVEN** 一个用户创建操作
- **WHEN** 查看日志
- **THEN** 可通过CorrelationId过滤所有相关日志
- **AND** 日志 **SHALL** 按时间顺序展示完整链路:
  - [VM] Save started
  - [CMD] CreateUser
  - [HTTP] >>> POST /api/users
  - [API] >>> UsersController.Create
  - [SVC] CreateAsync
  - [REPO] User.Add
  - [REPO] User.Add completed
  - [SVC] CreateAsync completed
  - [API] <<< completed
  - [HTTP] <<< 201
  - [CMD] CreateUser completed
  - [VM] Save completed

---

### Requirement: LOG-019 现有日志规范化

现有CommandHandler和Service层日志 **SHALL** 更新以符合LOG-018的前缀规范。

**影响范围**:
- 各模块CommandHandler (Users, Patients, Herbs, Formula, MedicalCase)
- 各模块Service (UserService, PatientService等)

#### Scenario: CommandHandler日志规范化
- **GIVEN** CommandHandler已有日志调用
- **WHEN** 日志输出
- **THEN** 日志消息 **SHALL** 以`[CMD]`前缀开头
- **AND** 现有日志语义 **SHALL** 保持不变

#### Scenario: Service日志规范化
- **GIVEN** Service已有日志调用
- **WHEN** 日志输出
- **THEN** 日志消息 **SHALL** 以`[SVC]`前缀开头
- **AND** 现有日志语义 **SHALL** 保持不变
