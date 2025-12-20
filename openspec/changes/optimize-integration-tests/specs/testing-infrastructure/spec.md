## ADDED Requirements

### Requirement: Integration Test Coverage

集成测试 SHALL 覆盖所有WebAPI Controller的核心端点。

#### Scenario: All Controllers have integration tests
- **WHEN** 检查tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/目录
- **THEN** 每个src/Server/Services/LYBT.WebAPI/Controllers/中的Controller都有对应的测试文件

#### Scenario: CRUD endpoints are tested
- **WHEN** Controller提供CRUD操作(Create, GetById, GetList, Update, Delete)
- **THEN** 对应的测试文件 MUST 包含这5个操作的测试方法

#### Scenario: Batch operations are tested
- **WHEN** Controller提供批量操作(BatchDelete, BatchEnable, BatchDisable等)
- **THEN** 对应的测试文件 MUST 包含批量操作的测试方法

### Requirement: Test Base Class Usage

所有WebAPI集成测试 MUST 继承IntegrationTestBase基类。

#### Scenario: Test class inheritance
- **WHEN** 创建新的集成测试类
- **THEN** 测试类 MUST 继承自LYBT.Tests.Common.IntegrationTestBase

#### Scenario: Authentication is handled
- **WHEN** 测试需要认证的API端点
- **THEN** MUST 使用IntegrationTestBase提供的Client(已包含JWT Token)

### Requirement: Test Naming Convention

测试方法 MUST 遵循统一命名规范。

#### Scenario: Method naming format
- **WHEN** 编写测试方法
- **THEN** 方法名 MUST 遵循格式: [Method]_[Scenario]_Should[Expected]

#### Scenario: File naming format
- **WHEN** 创建测试文件
- **THEN** 文件名 MUST 遵循格式: [Controller]IntegrationTests.cs

### Requirement: Test Data Isolation

每个测试实例 MUST 使用隔离的测试数据。

#### Scenario: Database isolation
- **WHEN** 测试实例初始化
- **THEN** MUST 使用独立的InMemory数据库实例

#### Scenario: Test data seeding
- **WHEN** 测试需要预置数据
- **THEN** SHALL 通过重写SeedBasicTestData方法实现

### Requirement: No Legacy Code

测试目录中 MUST NOT 存在遗留的备份文件。

#### Scenario: No backup files
- **WHEN** 检查测试目录
- **THEN** MUST NOT 存在.bak后缀的文件

#### Scenario: No deprecated patterns
- **WHEN** 检查测试代码
- **THEN** MUST NOT 使用旧的CustomWebApplicationFactory或ShouldHaveStatusCode等废弃模式
