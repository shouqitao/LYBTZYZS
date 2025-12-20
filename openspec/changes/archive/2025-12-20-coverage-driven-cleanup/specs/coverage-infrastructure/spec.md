# Spec: Coverage Infrastructure

## ADDED Requirements

### Requirement: 代码覆盖率收集基础设施

所有测试项目 MUST 能够收集代码覆盖率数据，生成Cobertura格式报告。

#### Scenario: 单元测试覆盖率收集

**Given** 开发者在项目根目录
**When** 执行 `dotnet test --collect:"XPlat Code Coverage"`
**Then** 在BIN/TestResults目录生成coverage.cobertura.xml文件
**And** 报告包含LYBT.*程序集的覆盖率数据
**And** 排除测试程序集、Designer文件、Generated文件

#### Scenario: 覆盖率报告合并

**Given** 多个测试项目已生成独立的覆盖率报告
**When** 执行覆盖率合并脚本
**Then** 生成统一的Cobertura格式报告
**And** 生成HTML可视化报告

### Requirement: 零覆盖代码识别

系统 MUST 能够识别从未被测试执行的代码，输出零覆盖类列表。

#### Scenario: 识别零覆盖类

**Given** 已生成覆盖率报告
**When** 分析Cobertura XML中line-rate="0"的类
**Then** 输出按模块分组的零覆盖类列表
**And** 排除接口、DTO、配置类等预期零覆盖类型

#### Scenario: 死代码确认

**Given** 零覆盖类候选列表
**When** 对每个候选类执行引用分析
**Then** 确认无引用的类标记为死代码
**And** 有引用的类保留并标注引用来源

## MODIFIED Requirements

### Requirement: 测试项目配置

测试项目 MUST 引用coverlet.collector以支持覆盖率收集。

#### Scenario: 测试项目引用coverlet

**Given** 任意LYBT测试项目
**When** 检查项目引用
**Then** 包含coverlet.collector包引用
**And** 版本与Directory.Packages.props一致(6.0.4)

### Requirement: runsettings配置优化

.runsettings文件 MUST 包含优化的排除规则，过滤噪音代码。

#### Scenario: 排除规则覆盖所有噪音代码

**Given** tests/.runsettings配置文件
**When** 执行覆盖率收集
**Then** 排除*.Tests程序集
**And** 排除*.Designer文件
**And** 排除*.g.cs生成文件
**And** 排除*.Generated.*文件
**And** 排除*.Migrations.*文件
**And** 排除Program入口点

