# GitHub Issue Mapping - PRD-技术债修复

Epic: #568 - https://github.com/shouqitao/LYBTZYZS/issues/568

## Batch-1: 基础设施和工具链 (Foundation Infrastructure)
- #569: 项目构建和CI环境标准化 - https://github.com/shouqitao/LYBTZYZS/issues/569
- #570: 代码格式化和EditorConfig配置 - https://github.com/shouqitao/LYBTZYZS/issues/570
- #571: NuGet包管理优化 - https://github.com/shouqitao/LYBTZYZS/issues/571
- #572: 测试框架和覆盖率基础设施 - https://github.com/shouqitao/LYBTZYZS/issues/572
- #573: 开发工具链集成 - https://github.com/shouqitao/LYBTZYZS/issues/573

## Batch-2: 低风险快速实现 (Low-Risk Quick Wins)  
- #574: DT-004 基础查询优化 - AsNoTracking实现 - https://github.com/shouqitao/LYBTZYZS/issues/574
- #575: DT-014 数据库备份策略 - 自动化备份脚本 - https://github.com/shouqitao/LYBTZYZS/issues/575
- #576: DT-009 命名约定统一 - Username/UserName一致性 - https://github.com/shouqitao/LYBTZYZS/issues/576
- #577: DT-010 日志配置优化 - Serilog结构化日志 - https://github.com/shouqitao/LYBTZYZS/issues/577
- #578: DT-012 配置验证增强 - 启动时配置验证 - https://github.com/shouqitao/LYBTZYZS/issues/578

## Batch-3: 架构内聚重构 (Architectural Cohesion)
- #579: DT-001 服务接口职责分离 - Auth模块接口重构 - https://github.com/shouqitao/LYBTZYZS/issues/579
- #580: DT-002 依赖注入标准化 - IoC生命周期统一 - https://github.com/shouqitao/LYBTZYZS/issues/580
- #581: DT-003 模块依赖关系梳理 - 8个模块依赖优化 - https://github.com/shouqitao/LYBTZYZS/issues/581

## Batch-4: 服务层增强 (Service Layer Enhancement)
- #582: DT-006 异常处理统一化 - Service层统一异常处理 - https://github.com/shouqitao/LYBTZYZS/issues/582
- #583: DT-013 内存泄漏修复 - ViewModel内存泄漏防护 - https://github.com/shouqitao/LYBTZYZS/issues/583
- #584: DT-011 取消令牌支持 - 长操作取消机制 - https://github.com/shouqitao/LYBTZYZS/issues/584

## Batch-5: UI层重构 (UI Layer Refactoring)
- #585: DT-005 ViewModel职责重构 - MVVM模式优化 - https://github.com/shouqitao/LYBTZYZS/issues/585

## Batch-6: 运营工具完善 (Operational Tooling)
- #586: DT-015 系统监控告警 - 健康检查服务 - https://github.com/shouqitao/LYBTZYZS/issues/586
- #587: DT-007 代码质量自动化 - PowerShell检查脚本 - https://github.com/shouqitao/LYBTZYZS/issues/587

## 项目收尾 (Project Completion)
- #588: Epic总结与文档更新 - 项目收尾 - https://github.com/shouqitao/LYBTZYZS/issues/588

## 统计信息
- **Epic**: 1个 (#568)
- **子任务总数**: 20个 (#569-#588)
- **批次数**: 6个批次 + 1个收尾任务
- **并行任务**: 14个可同时执行
- **串行任务**: 6个有依赖关系

## 依赖关系
- #580 依赖 #579 (DT-002 依赖 DT-001)
- #581 依赖 #579, #580 (DT-003 依赖 DT-001, DT-002)
- #582 依赖 #581 (异常处理依赖架构重构)
- #585 依赖 #582 (UI重构依赖服务层优化)
- #588 依赖 #574-#587 (项目收尾依赖所有实施任务)

Synced: 2025-09-06T15:14:33Z
