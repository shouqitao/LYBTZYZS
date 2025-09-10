---
name: 统一包管理CCPM需求
status: completed
created: 2025-09-04T04:08:52Z
updated: 2025-09-05T01:35:00Z
progress: 100%
prd: .claude/prds/统一包管理CCPM需求.md
github: https://github.com/shouqitao/LYBTZYZS/issues/492
---

# Epic: 统一包管理CCPM需求

## Overview

实施MSBuild Central Package Management（CPM）体系，解决LYBTZYZS项目33个.csproj文件的包管理分散化问题。通过Directory.Packages.props中央版本控制和Directory.Build.props统一构建配置，将维护效率提升90%，同时确保UltraThink双层架构+后端三层架构的完美兼容。

## Architecture Decisions

- **Central Package Management模式**: 采用MSBuild原生CPM特性，避免自定义脚本的维护复杂性
- **分层包分类策略**: 前端WPF包、后端WebAPI包、共享包三层隔离，防止包污染
- **条件引用机制**: 基于$(MSBuildProjectName)模式匹配自动应用对应包组合
- **版本锁定策略**: 生产环境包版本严格锁定，开发环境支持灵活升级
- **零破坏性迁移**: 分阶段迁移策略，每个阶段都可独立回滚

## Technical Approach

### Frontend Components
- **Directory.Packages.props**: 中央化管理所有NuGet包版本，支持包分类标签
- **条件包引用逻辑**: 自动识别LYBT.Desktop.*项目并应用WPF相关包
- **Prism.DryIoc兼容性验证**: 确保模块动态加载机制不受包管理变更影响
- **版本冲突检测脚本**: PowerShell脚本自动扫描并报告版本不一致问题

### Backend Services  
- **后端包隔离策略**: LYBT.Module.*和LYBT.WebAPI项目专用包组合
- **EF Core迁移保护**: 包升级前备份迁移脚本，升级后验证兼容性
- **API契约稳定性**: 包版本变更不影响前后端接口契约
- **传递依赖管理**: 启用CentralPackageTransitivePinningEnabled防止版本漂移

### Infrastructure
- **Visual Studio集成**: 确保IntelliSense和项目模板正常工作
- **CI/CD流程适配**: 更新构建脚本以支持新的包管理结构
- **回滚机制**: Git-based包管理配置版本控制和快速回滚
- **监控告警**: 包版本变更日志和异常检测机制

## Implementation Strategy

### Phase 1: 基础设施建设（1周）
- 创建Directory.Packages.props和Directory.Build.props
- 建立包分类和版本管理策略
- 开发版本冲突检测工具

### Phase 2: 项目迁移验证（2周）
- 选择3个代表性项目（前端+后端+共享）进行试点迁移
- 验证UltraThink双层架构兼容性
- 完善迁移脚本和回滚机制

### Phase 3: 全量迁移（1周）
- 批量迁移剩余30个项目到中央化包管理
- 执行全面编译和功能测试
- 更新CI/CD构建流程

### Phase 4: 优化完善（1周）  
- 性能优化和内存占用监控
- 团队培训和文档完善
- 建立长期包管理维护流程

## Task Breakdown Preview

High-level task categories that will be created:
- [ ] **中央包管理基础设施**: Directory.Packages.props和Directory.Build.props创建
- [ ] **版本冲突解决**: 识别并统一FluentValidation等版本不一致包
- [ ] **UltraThink架构验证**: 确保前端双层架构+Prism.DryIoc兼容性  
- [ ] **项目文件标准化**: 清理33个.csproj文件的重复配置和版本号
- [ ] **条件包引用实现**: 基于项目名称模式的智能包应用逻辑
- [ ] **迁移脚本开发**: 自动化迁移工具和回滚机制
- [ ] **编译验证**: 全项目编译测试和性能基准对比
- [ ] **CI/CD流程更新**: 构建脚本适配和部署流程验证
- [ ] **文档和培训**: 操作手册、最佳实践、故障排除指南

## Dependencies

- **MSBuild 17.0+**: Central Package Management特性支持
- **Visual Studio 2022**: IDE对CPM的完整支持
- **.NET 8 SDK**: 确保所有包与LTS版本兼容
- **现有测试套件**: 验证包升级不影响功能正确性
- **Git版本控制**: 包管理配置的版本管理和回滚支持

## Success Criteria (Technical)

### 性能基准
- 编译时间变化：±5%以内  
- Visual Studio解决方案加载时间：不增加超过5%
- Directory.Packages.props文件大小：<50KB

### 质量门禁
- 33个项目零编译警告零错误
- 所有现有测试用例100%通过
- UltraThink双层架构功能正常
- 包版本一致性：相同包在所有项目中使用统一版本

### 维护效率
- 包版本升级时间：从2小时减少到10分钟
- 新包添加流程：标准化操作，5分钟完成
- 版本冲突发现：自动化检测，零人工遗漏

## Estimated Effort

### 总体时间：5周
- **架构设计和基础设施**: 1周（已完成PRD阶段）
- **开发和实施**: 3周（核心迁移工作）
- **测试和优化**: 1周（验证和完善）

### 资源需求
- **首席架构师**: 全程参与，重点在架构决策和风险控制
- **高级开发工程师**: 2-3人，负责迁移脚本开发和项目迁移
- **DevOps工程师**: 1人，负责CI/CD流程适配

### 关键路径
1. Directory.Packages.props设计和创建（Week 1）
2. UltraThink架构兼容性验证（Week 2）  
3. 全项目迁移和编译验证（Week 3-4）
4. 性能优化和团队培训（Week 5）

**风险缓解**: 每个阶段都有独立的回滚方案，最大程度降低实施风险。

## Tasks Created
- [ ] #493 - 中央包管理基础设施建设 (parallel: false)
- [ ] #494 - 版本冲突检测和解决 (parallel: true) 
- [ ] #495 - 包分类和条件引用策略 (parallel: true)
- [ ] #496 - UltraThink架构兼容性验证 (parallel: false)
- [ ] #497 - 项目文件标准化迁移 (parallel: true)
- [ ] #498 - 迁移脚本和回滚机制开发 (parallel: true)
- [ ] #499 - 全项目编译验证和性能测试 (parallel: false)
- [ ] #500 - CI/CD流程适配和部署验证 (parallel: true)
- [ ] #501 - 文档培训和维护流程建立 (parallel: true)

Total tasks: 9
Parallel tasks: 6
Sequential tasks: 3
Estimated total effort: 44-56 hours (约1.5-2人月)