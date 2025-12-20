# shared-layer-architecture Specification Delta

## ADDED Requirements

### Requirement: SHARED-LOGGING 共享日志项目

`LYBT.Shared.Logging`项目 **SHALL** 作为Shared层的日志基础设施项目。

**项目定位**:
- 与`LYBT.Shared.ExceptionHandling`平行的基础设施项目
- 提供跨前后端的统一日志能力
- 遵循Shared层的依赖规则

**项目结构**:
```
src/Shared/LYBT.Shared.Logging/
├── Abstractions/           # 接口定义
├── Configuration/          # 配置类
├── Enrichers/              # Serilog Enrichers
├── Masking/                # 敏感数据脱敏
├── Management/             # 日志管理(级别控制等)
└── Extensions/             # 扩展方法
```

#### Scenario: Shared层日志项目存在
- **WHEN** 检查src/Shared目录
- **THEN** **SHALL** 存在LYBT.Shared.Logging项目
- **AND** 项目 **SHALL** 遵循Shared层命名规范
- **AND** 项目 **SHALL** 仅依赖其他Shared层项目和NuGet包

#### Scenario: 日志项目依赖规则
- **WHEN** 检查LYBT.Shared.Logging的项目引用
- **THEN** **MAY** 依赖LYBT.Shared.Primitives
- **AND** **SHALL NOT** 依赖Server层项目
- **AND** **SHALL NOT** 依赖Client层项目
- **AND** **SHALL NOT** 依赖LYBT.Entities项目
