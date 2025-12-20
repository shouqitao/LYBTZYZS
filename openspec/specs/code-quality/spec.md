# code-quality Specification

## Purpose
TBD - created by archiving change consolidate-code-quality. Update Purpose after archive.
## Requirements
### Requirement: CQ-001 圈复杂度标准
所有业务代码方法的圈复杂度(Cyclomatic Complexity)必须(MUST)控制在合理范围内。

**标准**:
- 理想: CC < 10
- 可接受: CC < 20
- 需重构: CC >= 20

系统SHALL确保新提交的代码通过复杂度检查。

#### Scenario: 新增方法复杂度检查
- Given 开发者编写新方法
- When 方法被提交到代码库
- Then 方法的圈复杂度应小于20
- And 如果CC >= 20，Code Review应要求拆分

#### Scenario: 现有高复杂度方法修复
- Given 现有方法CC >= 20
- When 执行重构
- Then 应拆分为多个小方法
- And 每个小方法CC < 15

### Requirement: CQ-002 EF迁移目录规范
EF Core迁移文件MUST统一存放在单一目录。

**规范**:
- 迁移目录: `src/Server/Core/LYBT.Infrastructure/Migrations/`
- 禁止: 多个迁移目录并存

系统SHALL只使用一个迁移目录。

#### Scenario: 迁移文件位置
- Given 开发者创建新的EF迁移
- When 执行`dotnet ef migrations add`
- Then 迁移文件应生成在`Migrations/`目录
- And 不应存在其他迁移目录

### Requirement: CQ-003 代码度量报告
项目MUST定期执行Visual Studio Code Metrics分析，跟踪代码质量趋势。

**指标**:
- 可维护性指数(MI): 目标 >= 40
- 圈复杂度(CC): 目标 < 20
- 继承深度(DI): 目标 <= 5
- 类耦合度: 根据类型合理控制

系统SHALL生成可追踪的度量报告。

#### Scenario: 定期代码度量
- Given 完成一个Sprint的开发
- When 运行Code Metrics分析
- Then 应生成度量报告
- And 标识所有CC > 20的方法
- And 跟踪与上次报告的对比

